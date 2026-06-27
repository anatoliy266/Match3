using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Pool;
using static UnityEngine.Rendering.DebugUI.Table;

[CreateAssetMenu(fileName = "LoadingState", menuName = "Scriptable Objects/LoadingState")]
public class LoadingState : GameState
{
    [Req] public MatchRules MatchRules;
    [Req] public SpawnRules SpawnRules;
    [Req] public Events Events;
    public override void Enter(FiniteStateMachine machine)
    {
        var name = Events.GetBusName(GameEvent.Input);
        GameplayEventBus<bool>.Trigger(name, false);

        var snapshot = machine.Field.ToSnapshot();

        var spawns = CollectionPool<List<SpawnInfo>, SpawnInfo>.Get();
        spawns.Clear();
        machine.SpawnEvaluator.Evaluate(machine, snapshot, MatchRules, spawns, 1000);

        for (var i = 0; i < spawns.Count; i++)
        {
            var tile = new LogicalTile
            {
                Id = machine.Field.GenerateUniqueId(),
                Type = spawns[i].Type,
            };
            machine.Field.SetTileAt(spawns[i].Position, tile);

            snapshot[spawns[i].Position.x, spawns[i].Position.y] = tile;
        }
        CollectionPool<List<SpawnInfo>, SpawnInfo>.Release(spawns);

        PlaceBlockers(machine);

        snapshot = machine.Field.ToSnapshot();

        var animname = Events.GetBusName(GameEvent.Animation);
        GameplayEventBus<LogicalTile?[,]>.Trigger(animname, snapshot);

        machine.Switch(StateEvent.FinishLoading);
    }

    private void PlaceBlockers(FiniteStateMachine machine)
    {
        var settings = machine.Blackboard.LevelSettings;
        if (settings.Blockers == null || settings.Blockers.Count == 0) return;

        var bounds = machine.Field.GetBounds();
        for (int i = 0; i < settings.Blockers.Count; i++)
        {
            var placement = settings.Blockers[i];
            if (placement.Position.x >= bounds.x || placement.Position.y >= bounds.y)
                continue;

            if (placement.Type == BlockerType.Invisible)
            {
                machine.Field.SetInvisibleCell(placement.Position);
                continue;
            }

            var tile = new LogicalTile
            {
                Id = machine.Field.GenerateUniqueId(),
                Type = TileKind.Blocker(placement.Type),
            };
            machine.Field.SetTileAt(placement.Position, tile);
        }
    }
}
