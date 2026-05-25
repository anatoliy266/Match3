using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
public struct CompactInfo
{
    public Tile Tile;
    public Vector2Int TargetPos;
}


[CreateAssetMenu(fileName = "CompactState", menuName = "Scriptable Objects/CompactState")]
public class CompactState : GameState
{
    [Req] public Events Events;
    public override void Enter(FiniteStateMachine machine)
    {
        // падение плиток 

        var name = Events.GetBusName(GameEvent.Animation);
        var snapshot = machine.Field.ToSnapshot();
        GameplayEventBus<LogicalTile?[,]>.Trigger(name, snapshot);

        _fsm.Switch(StateEvent.FillUpTiles);
    }

    //private void AnimateFalls(TransitionContext context)
    //{
    //    var fallsAnimList = new List<AnimationData>();

    //    foreach (var compact in context.Compacts)
    //    {
    //        var data = new AnimationData
    //        {
    //            Type = AnimationType.Move,
    //            Target = compact.Tile.transform,
    //            TargetPosition = _field.GetWorldPos(compact.TargetPos),
    //            Duration = 1.0f
    //        };
    //        fallsAnimList.Add(data);
    //    }

    //    var name = _fsm.Events.GetBusName(GameEvent.Animation);
    //    GameplayEventBus<List<AnimationData>>.Trigger(name, fallsAnimList);
    //}
}
