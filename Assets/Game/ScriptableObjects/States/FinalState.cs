using System;
using System.Buffers;
using System.Linq;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.UIElements;
using static UnityEditor.PlayerSettings;

//[CreateAssetMenu(fileName = "FinalState", menuName = "Scriptable Objects/FinalState")]
//public class FinalState : GameState
//{
//    [SerializeField][Req] Events Events;

//    public override void Enter(FiniteStateMachine machine)
//    {
//        // посмотреть сколдько ходов осталось и наспавнить бонусов по количеству оставшихся ходов

//        // проверять если какието плитки были уничтожены исключать их из массива, \
//        Debug.Log("заходит в финал стейт");
//        var bounds = machine.Field.GetBounds();

//        machine.Blackboard.IsFinalState = true;

//        var stepsLeft = machine.Blackboard.MaxSteps - machine.Blackboard.Step;

//        var snapshot = machine.Field.ToSnapshot();
//        Debug.Log($"Ходы {machine.Blackboard.Step}");
//        Debug.Log($"оставшиеся ходы {machine.Blackboard.Step}");

//        if (stepsLeft > 0)
//        {
//            var totalCells = bounds.x * bounds.y;

//            var startIndex = UnityEngine.Random.Range(0, totalCells);

//            // Шаг должен быть взаимно прост с totalCells. 
//            var step = Euclide.Run(totalCells);

//            var spawnedCount = 0;

//            for (var i = 0; i < totalCells && spawnedCount < stepsLeft; i++)
//            {
//                var currentIndex = (startIndex + i * step) % totalCells;

//                var x = currentIndex % bounds.x;
//                var y = currentIndex / bounds.x;

//                if (snapshot[x, y] != null && snapshot[x, y].Value.Type.KindType == TileKindType.Regular)
//                {
//                    var newTile = new LogicalTile { Id = machine.Field.GenerateUniqueId(), Type = TileKind.Bonus(BonusType.Bomb) };
//                    machine.Field.SetTileAt(new Vector2Int(x, y), newTile);

//                    spawnedCount++;
//                }
//            }

//            machine.Blackboard.Step += spawnedCount;

//            snapshot = machine.Field.ToSnapshot();

//            var animBusName = Events.GetBusName(GameEvent.Animation);
//            GameplayEventBus<LogicalTile?[,]>.Trigger(animBusName, snapshot);
//            machine.Switch(StateEvent.FillUpTiles);
//            return;
//        }


//        // по 1 отправлять через цикл - свап - чек - делит - филап - чек - идл - финал. 
//        Debug.Log("оставшихся шагов 0, начинается уничтожение бонусов");


//        machine.Blackboard.BonusesToActivate.Clear();
//        for (var i = 0; i < bounds.x; i++)
//        {
//            for (var j = 0; j < bounds.y; j++)
//            {
//                if (snapshot[i, j] is not null && snapshot[i,j].Value.Type.KindType == TileKindType.Bonus)
//                {
//                    ////var inputBusName = Events.GetBusName(GameEvent.Input);
//                    //var info = new SwapInfo { SourceId = snapshot[i,j].Value.Id, DestId = snapshot[i,j].Value.Id };
//                    ////GameplayEventBus<SwapInfo>.Trigger(inputBusName, info);
//                    //machine.Blackboard.SourceDest = info;
//                    machine.Blackboard.BonusesToActivate.Add(snapshot[i, j].Value.Id);
//                }
//            }
//        }

//        if (machine.Blackboard.BonusesToActivate.Count > 0)
//        {
//            machine.Blackboard.CascadeIteration = 1000;
//            machine.Switch(StateEvent.SwapBonus);
//            return;
//        }

//        var name = Events.GetBusName(GameEvent.Final);
//        GameplayEventBus<bool>.Trigger(name, true);
//    }
//}

[CreateAssetMenu(fileName = "FinalState", menuName = "Scriptable Objects/FinalState")]
public class FinalState : GameState
{
    [SerializeField][Req] private Events Events;

    public override void Enter(FiniteStateMachine machine)
    {
        Debug.Log("[FinalState] Вход в финальное состояние игры.");

        machine.Blackboard.IsFinalState = true;

        var stepsLeft = machine.Blackboard.MaxSteps - machine.Blackboard.Step;

        // ВЫСШИЙ УРОВЕНЬ АБСТРАКЦИИ: Диспетчеризация фаз финала
        if (stepsLeft > 0)
        {
            ConvertRemainingStepsToBonuses(machine, stepsLeft);
        }
        else
        {
            ActivateAllFieldBonuses(machine);
        }
    }

    /// <summary>
    /// Фаза 1: Конвертация неиспользованных ходов в случайные бомбы на поле
    /// </summary>
    private void ConvertRemainingStepsToBonuses(FiniteStateMachine machine, int stepsLeft)
    {
        var bounds = machine.Field.GetBounds();
        var snapshot = machine.Field.ToSnapshot();
        var totalCells = bounds.x * bounds.y;

        Debug.Log($"[FinalState] Конвертация ходов. Осталось ходов: {stepsLeft}");

        var startIndex = UnityEngine.Random.Range(0, totalCells);

        // Шаг по алгоритму Евклида для идеального псевдослучайного обхода
        var step = Euclide.Run(totalCells);
        var spawnedCount = 0;

        for (var i = 0; i < totalCells && spawnedCount < stepsLeft; i++)
        {
            var currentIndex = (startIndex + i * step) % totalCells;
            var x = currentIndex % bounds.x;
            var y = currentIndex / bounds.x;

            if (snapshot[x, y] != null && snapshot[x, y].Value.Type.KindType == TileKindType.Regular)
            {
                var newTile = new LogicalTile
                {
                    Id = machine.Field.GenerateUniqueId(),
                    Type = TileKind.Bonus(BonusType.Bomb)
                };

                machine.Field.SetTileAt(new Vector2Int(x, y), newTile);
                spawnedCount++;
            }
        }

        machine.Blackboard.Step += spawnedCount;

        // Берем свежий снапшот поля после расстановки бомб и шлем в анимацию
        snapshot = machine.Field.ToSnapshot();
        var animBusName = Events.GetBusName(GameEvent.Animation);
        GameplayEventBus<LogicalTile?[,]>.Trigger(animBusName, snapshot);

        // Отправляем поле падать, чтобы бомбы встали на свои места
        machine.Switch(StateEvent.FillUpTiles);
    }

    /// <summary>
    /// Фаза 2: Массовый одновременный сбор и детонация всех бонусов на игровом поле
    /// </summary>
    private void ActivateAllFieldBonuses(FiniteStateMachine machine)
    {
        Debug.Log("[FinalState] Ходов нет. Сбор бонусов для массового взрыва.");

        var bounds = machine.Field.GetBounds();
        var snapshot = machine.Field.ToSnapshot();

        // Безопасно очищаем общую шину данных в Блэкборде перед заполнением
        machine.Blackboard.BonusesToActivate.Clear();

        for (var i = 0; i < bounds.x; i++)
        {
            for (var j = 0; j < bounds.y; j++)
            {
                if (snapshot[i, j] is not null && snapshot[i, j].Value.Type.KindType == TileKindType.Bonus)
                {
                    machine.Blackboard.BonusesToActivate.Add(snapshot[i, j].Value.Id);
                }
            }
        }

        // Если нашли хоть один бонус — едем на рельсы детонации
        if (machine.Blackboard.BonusesToActivate.Count > 0)
        {
            machine.Blackboard.CascadeIteration++;
            machine.Switch(StateEvent.SwapBonus); // Передаем список в универсальный BonusState
            return;
        }

        // Если поле полностью зачищено от бомб — уровень официально завершен победой
        Debug.Log("[FinalState] Все бонусы уничтожены, поле чистое. Конец уровня.");
        var finalBusName = Events.GetBusName(GameEvent.Final);
        GameplayEventBus<bool>.Trigger(finalBusName, true);
    }
}