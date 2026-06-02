using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Mathematics;
using UnityEngine;
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
                machine.Field.SetTileAt(new Vector2Int(i, j), tile);
            }
        }

        var animname = Events.GetBusName(GameEvent.AnimationEnd);
        var snapshot = machine.Field.ToSnapshot();

        

        GameplayEventBus<LogicalTile?[,]>.Trigger(animname, snapshot);

        machine.Switch(StateEvent.FinishLoading);
    }
}
