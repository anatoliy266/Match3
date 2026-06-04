using UnityEngine;
using UnityEngine.Pool;

[CreateAssetMenu(fileName = "FinalState", menuName = "Scriptable Objects/FinalState")]
public class FinalState : GameState
{
    public override void Enter(FiniteStateMachine machine)
    {

        
        // посмотреть сколдько ходов осталось и наспавнить бонусов по количеству оставшихся ходов
        var snapshot = machine.Field.ToSnapshot();
        var bounds = machine.Field.GetBounds();
        var total = bounds.x * bounds.y;

        machine.Blackboard.IsFinalState = true;

        var stepsLeft = machine.Blackboard.MaxSteps - machine.Blackboard.Step;
        var bonusesToSpawn = Mathf.Min(stepsLeft, bounds.x * bounds.y);

        var positions = machine.Blackboard.FinalBonuses;

        var map = DictionaryPool<int, int>.Get();

        for (int i = total - bonusesToSpawn; i < total; i++)
        {
            int r = UnityEngine.Random.Range(0, i + 1);
            int actualElement = map.ContainsKey(r) ? map[r] : r;
            map[r] = map.ContainsKey(i) ? map[i] : i;

            int row = actualElement / bounds.y;
            int col = actualElement % bounds.y;


            positions.Add(new Vector2Int(row, col));
        }

        DictionaryPool<int, int>.Release(map);

        machine.Switch(StateEvent.Final);
        // записать плитки бонусы в массив, и по 1 отправлять через цикл - идл - свап - чек - делит - филап - чек - идл
        // проверять если какието плитки были уничтожены исключать их из массива, 
        // если были наспавнены новые бонусы - добавить в список.

        // после того как все бонусы будут уничтожены - отправить событие в левелконтроллер
    }
}
