using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[CreateAssetMenu(fileName = "RemoveState", menuName = "Scriptable Objects/RemoveState")]
public class RemoveState : GameState
{
    [Req] public Events Events;
    [Req] public BlockerDestroyRules BlockerDestroyRules;

    public override void Enter(FiniteStateMachine machine)
    {
        var prevSnapshot = machine.Field.ToSnapshot();

        var positionsCache = DictionaryPool<Guid, Vector2Int>.Get();
        positionsCache.Clear();
        machine.Field.ToPositionChache(positionsCache);

        var matches = machine.Blackboard.CurrentMatches;

        ProcessBlockers(machine, prevSnapshot, matches);

        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            for (var j = 0; j < match.Positions.Count; j++)
            {
                var id = match.Positions[j];
                if (positionsCache.TryGetValue(id, out var pos))
                {
                    machine.Field.ClearTileAt(pos);
                }
            }
        }

        var snapshot = machine.Field.ToSnapshot();

        var name = Events.GetBusName(GameEvent.Animation);
        GameplayEventBus<LogicalTile?[,]>.Trigger(name, snapshot);

        var shaderBusName = Events.GetBusName(GameEvent.ShaderImpact);
        GameplayEventBus<(LogicalTile?[,], LogicalTile?[,])>.Trigger(shaderBusName, (prevSnapshot, snapshot));

        var shaderScore = Events.GetBusName(GameEvent.Score);
        GameplayEventBus<(LogicalTile?[,], LogicalTile?[,])>.Trigger(shaderScore, (prevSnapshot, snapshot));

        DictionaryPool<Guid, Vector2Int>.Release(positionsCache);

        machine.Switch(StateEvent.DestroyTiles);
    }

    private void ProcessBlockers(FiniteStateMachine machine, LogicalTile?[,] snapshotBeforeClear, List<MatchInfo> matches)
    {
        var bounds = machine.Field.GetBounds();
        machine.Blackboard.EnsureBlockerList();
        var blockersToRemove = machine.Blackboard.CurrentBlockersToRemove;
        blockersToRemove.Clear();

        //A9 вынес из метода чтобы в цикле не вызывалось
        //var matchedIds = new HashSet<Guid>();
        var matchedIds = HashSetPool<Guid>.Get();
        matchedIds.Clear();

        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            for (var j = 0; j < match.Positions.Count; j++)
            {
                matchedIds.Add(match.Positions[j]);
            }
        }

        for (var i = 0; i < bounds.x; i++)
        {
            for (var j = 0; j < bounds.y; j++)
            {
                var pos = new Vector2Int(i, j);
                var tile = snapshotBeforeClear[i, j];
                if (tile is null) continue;
                if (tile.Value.Type.KindType != TileKindType.Blocker) continue;

                var rule = BlockerDestroyRules.GetRule(tile.Value.Type.BlockerType);
                if (rule == null) continue;



                if (rule.ShouldDestroy(pos, snapshotBeforeClear, matchedIds))
                {
                    blockersToRemove.Add(tile.Value);
                    machine.Field.ClearTileAt(pos);
                }
            }
        }

        HashSetPool<Guid>.Release(matchedIds);
    }
}
