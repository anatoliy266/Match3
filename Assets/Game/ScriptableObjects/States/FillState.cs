using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

[CreateAssetMenu(fileName = "FillState", menuName = "Scriptable Objects/FillState")]
public class FillState : GameState
{
    public override void Enter(FiniteStateMachine machine)
    {
        //_field = field;
        //_fsm = machine;

        //_field.FillEmptyTilesOnGrid(context.CascadeIteration);

        //AnimateFillEmpty();

        //_fsm.Switch(StateEvent.FillEmptyTiles, context);
    }

    //private void AnimateFillEmpty()
    //{
    //    var fillAnimList = new List<AnimationData>();

    //    var bounds = _field.GetBounds();

    //    for (var i = 0; i < bounds.x; i++)
    //    {
    //        for (var j = 0; j < bounds.y; j++)
    //        {
    //            var tile = _field.GetTileAt(new Vector2Int(i, j));
    //            if (tile == null) continue;

    //            Vector3 targetWorldPosition = _field.GetWorldPos(tile.GridPosition);

    //            if (tile.transform.position != targetWorldPosition)
    //            {
    //                var data = new AnimationData
    //                {
    //                    Type = AnimationType.Move,
    //                    Target = tile.transform,
    //                    TargetPosition = targetWorldPosition,
    //                    Duration = 1.0f
    //                };
    //                fillAnimList.Add(data);
    //            }
    //        }
    //    }

    //    var name = _fsm.Events.GetBusName(GameEvent.Animation);
    //    GameplayEventBus<List<AnimationData>>.Trigger(name, fillAnimList);
    //}
}
