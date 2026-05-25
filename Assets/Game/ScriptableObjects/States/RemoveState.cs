using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using static UnityEditor.PlayerSettings;
using static UnityEngine.Rendering.DebugUI;

[CreateAssetMenu(fileName = "RemoveState", menuName = "Scriptable Objects/RemoveState")]
public class RemoveState : GameState
{
    [Req] public Events Events;

    public override void Enter(FiniteStateMachine machine)
    {
        var positionsCache = DictionaryPool<Guid, Vector2Int>.Get();
        machine.Field.ToPositionChache(positionsCache);


        var matches = machine.Blackboard.CurrentMatches;
        for (var i = 0; i < matches.Count; i++)
        {
            var match = matches[i];
            for (var j = 0; j < match.Positions.Count; j++)
            {
                var id = match.Positions[j];
                if (positionsCache.TryGetValue(id, out var pos))
                {
                    //todo как будто не хватает очистки словаря на всякий случай
                    machine.Field.ClearTileAt(pos);
                }
            }
        }

        var name = Events.GetBusName(GameEvent.Animation);
        var snapshot = machine.Field.ToSnapshot();
        GameplayEventBus<LogicalTile?[,]>.Trigger(name, snapshot);

        DictionaryPool<Guid, Vector2Int>.Release(positionsCache);

        machine.Switch(StateEvent.DestroyTiles);
    }
}
