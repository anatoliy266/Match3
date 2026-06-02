using System;
using System.Buffers;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[CreateAssetMenu(fileName = "BonusState", menuName = "Scriptable Objects/BonusState")]
public class BonusState : GameState
{
    [Req] public MatchRules MatchRules;
    [Req] public SpawnRules SpawnRules;

    public override void Enter(FiniteStateMachine machine)
    {
        var matches = machine.Blackboard.CurrentMatches;

        var snapshot = machine.Field.ToSnapshot();
        var bounds = machine.Field.GetBounds();

        var positionsCache = DictionaryPool<Guid, Vector2Int>.Get();
        positionsCache.Clear();
        machine.Field.ToPositionChache(positionsCache);

        var visited = HashSetPool<Guid>.Get();
        visited.Clear();

        var _bonusQueue = new Queue<Guid>();
        _bonusQueue.Enqueue(machine.Blackboard.SourceDest.SourceId);
        _bonusQueue.Enqueue(machine.Blackboard.SourceDest.DestId);

        visited.Add(machine.Blackboard.SourceDest.SourceId);
        visited.Add(machine.Blackboard.SourceDest.DestId);


        while (_bonusQueue.Count > 0)
        {
            var id = _bonusQueue.Dequeue();
            if (!positionsCache.TryGetValue(id, out var pos)) pos = new Vector2Int(-1, -1);

            var startCount = matches.Count;



            machine.MatchEvaluator.GetBonusGroupAt(snapshot, pos, MatchRules, matches);

            for (var i = startCount; i < matches.Count; i++)
            {
                var match = matches[i];
                for (var j = 0; j < match.Positions.Count; j++)
                {
                    var tileId = match.Positions[j];
                    if (visited.Add(tileId) 
                        && positionsCache.TryGetValue(tileId, out var tilePos) 
                        && snapshot[tilePos.x, tilePos.y] is not null && snapshot[tilePos.x, tilePos.y].Value.Type.KindType == TileKindType.Bonus)
                    {
                        _bonusQueue.Enqueue(tileId);
                    }
                }
            }
        }

        DictionaryPool<Guid, Vector2Int>.Release(positionsCache);
        HashSetPool<Guid>.Release(visited);

        if (matches.Count == 0)
        {
            machine.Switch(StateEvent.NoMatches);
        }
        else
        {
            machine.Switch(StateEvent.MatchesFound);
        }
    }
}
