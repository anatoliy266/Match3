using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;

[CreateAssetMenu(fileName = "SwapState", menuName = "Scriptable Objects/SwapState")]
public class SwapState : GameState
{
    [Req] public Events Events;
    public override void Enter(FiniteStateMachine machine)
    {
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

            bool hasBonus = (source.HasValue && source.Value.Type.KindType == TileKindType.Bonus) ||
                        (dest.HasValue && dest.Value.Type.KindType == TileKindType.Bonus);

            machine.Field.SetTileAt(sourcePos, dest);
            machine.Field.SetTileAt(destPos, source);

            DictionaryPool<Guid, Vector2Int>.Release(positionsCache);

            var snapshot = machine.Field.ToSnapshot();
            var name = Events.GetBusName(GameEvent.AnimationEnd);
            GameplayEventBus<LogicalTile?[,]>.Trigger(name, snapshot);

            if (hasBonus)
            {
                machine.Switch(StateEvent.SwapBonus); 
            }
            else
            {
                machine.Switch(StateEvent.Swap);       
            }
        } else
        {
            DictionaryPool<Guid, Vector2Int>.Release(positionsCache);

            var snapshot = machine.Field.ToSnapshot();
            var name = Events.GetBusName(GameEvent.AnimationEnd);
            GameplayEventBus<LogicalTile?[,]>.Trigger(name, snapshot);

            machine.Switch(StateEvent.SwapBack);
        }
    }
}



