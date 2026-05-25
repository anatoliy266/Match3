using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;

[CreateAssetMenu(fileName = "SwapState", menuName = "Scriptable Objects/SwapState")]
public class SwapState : GameState
{
    public override void Enter(FiniteStateMachine machine)
    {
        _fsm = machine;

        var sourceId = machine.Blackboard.SourceDest.SourceId;
        var destId = machine.Blackboard.SourceDest.DestId;

        var positionsCache = DictionaryPool<Guid, Vector2Int>.Get();
        positionsCache.Clear();
        machine.Field.ToPositionChache(positionsCache);


        if (positionsCache.TryGetValue(sourceId, out var sourcePos) &&
        positionsCache.TryGetValue(destId, out var destPos))
        {
            var source = machine.Field.GetTileAt(sourceId);
            var dest = machine.Field.GetTileAt(destId);

            _fsm.Field.SetTileAt(sourcePos, dest);
            _fsm.Field.SetTileAt(destPos, source);
        }

        DictionaryPool<Guid, Vector2Int>.Release(positionsCache);

        _fsm.Switch(StateEvent.Swap);
    }
}



