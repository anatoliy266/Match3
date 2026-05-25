using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

[CreateAssetMenu(fileName = "LoadingState", menuName = "Scriptable Objects/LoadingState")]
public class LoadingState : GameState
{
    [Req] public Events Events;
    public override void Enter(FiniteStateMachine machine)
    {
        var name = Events.GetBusName(GameEvent.Input);
        GameplayEventBus<bool>.Trigger(name, false);


        var bounds = machine.Field.GetBounds();

        for (var i = 0; i < bounds.x; i++)
        {
            for (var j = 0; j < bounds.y; j++)
            {
                var tile = new LogicalTile
                {
                    Id = machine.Field.GenerateUniqueId(),
                    //todo: сделать неслучайный спавн
                    Type = TileKind.Regular((RegularType)UnityEngine.Random.Range(0,6))
                };
                _fsm.Field.SetTileAt(new Vector2Int(i, j), tile);
            }
        }

        var animname = Events.GetBusName(GameEvent.Animation);
        var snapshot = _fsm.Field.ToSnapshot();

        

        GameplayEventBus<LogicalTile?[,]>.Trigger(animname, snapshot);

        _fsm.Switch(StateEvent.FinishLoading);
    }
}
