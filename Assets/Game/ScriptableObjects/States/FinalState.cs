using UnityEngine;


[CreateAssetMenu(fileName = "FinalState", menuName = "Scriptable Objects/FinalState")]
public class FinalState : GameState
{
    [SerializeField][Req] private Events Events;

    public override void Enter(FiniteStateMachine machine)
    {
        machine.Blackboard.IsFinalState = true;

        if (!machine.Blackboard.IsWin)
        {
            var finalBusName = Events.GetBusName(GameEvent.Final);
            GameplayEventBus<bool>.Trigger(finalBusName, false);
            return;
        }

        var stepsLeft = machine.Blackboard.MaxSteps - machine.Blackboard.Step;


        if (stepsLeft > 0)
        {
            ConvertRemainingStepsToBonuses(machine, stepsLeft);
        }
        else
        {
            ActivateAllFieldBonuses(machine);
        }
    }

    private void ConvertRemainingStepsToBonuses(FiniteStateMachine machine, int stepsLeft)
    {
        var bounds = machine.Field.GetBounds();
        var snapshot = machine.Field.ToSnapshot();
        var totalCells = bounds.x * bounds.y;

        var startIndex = UnityEngine.Random.Range(0, totalCells);

        var step = Euclide.Run(totalCells);
        var spawnedCount = 0;

        var allBonusTypes = (BonusType[])System.Enum.GetValues(typeof(BonusType));

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
                    Type = TileKind.Bonus(allBonusTypes[UnityEngine.Random.Range(0, allBonusTypes.Length)])
                };

                machine.Field.SetTileAt(new Vector2Int(x, y), newTile);
                spawnedCount++;
            }
        }

        machine.Blackboard.Step += spawnedCount;

        snapshot = machine.Field.ToSnapshot();
        var animBusName = Events.GetBusName(GameEvent.Animation);
        GameplayEventBus<LogicalTile?[,]>.Trigger(animBusName, snapshot);

        machine.Switch(StateEvent.FillUpTiles);
    }

    private void ActivateAllFieldBonuses(FiniteStateMachine machine)
    {
        var bounds = machine.Field.GetBounds();
        var snapshot = machine.Field.ToSnapshot();

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
            machine.Switch(StateEvent.SwapBonus);
            return;
        }

        var finalBusName = Events.GetBusName(GameEvent.Final);
        GameplayEventBus<bool>.Trigger(finalBusName, true);
    }
}