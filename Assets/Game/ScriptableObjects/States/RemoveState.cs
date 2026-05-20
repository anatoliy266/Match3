using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

[CreateAssetMenu(fileName = "RemoveState", menuName = "Scriptable Objects/RemoveState")]
public class RemoveState : FieldState
{
    public override void Enter(FieldController field, FiniteStateMachine machine, TransitionContext context = default)
    {
        _field = field;
        _fsm = machine;

        foreach (var match in context.Matches)
        {
            _field.RemoveTiles(match.Positions);
        }
        
        AnimateDestroy(context);

        _fsm.Switch(StateEvent.DestroyTiles, context);
    }

    private void AnimateDestroy(TransitionContext context)
    {
        var destroyAnimList = new List<AnimationData>();

        foreach (var group in context.Matches)
        {
            foreach (var tilePos in group.Positions)
            {
                if (context.Snapshot[tilePos.x, tilePos.y]  is not null)
                {
                    //var tile = _field.GetTileAt(tilePos);
                    var tile = context.Snapshot[tilePos.x, tilePos.y].Value.Transform;

                    var data = new AnimationData
                    {
                        Type = AnimationType.Destroy,
                        Target = tile,
                        Duration = 1.0f,
                    };
                    destroyAnimList.Add(data);
                }
            }
        }

        var name = _fsm.Events.GetBusName(GameEvent.Animation);
        GameplayEventBus<List<AnimationData>>.Trigger(name, destroyAnimList);
    }
}
