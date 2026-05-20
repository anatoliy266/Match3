using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SwapBackState", menuName = "Scriptable Objects/SwapBackState")]
public class SwapBackState : FieldState
{
    public override void Enter(FieldController field, FiniteStateMachine machine, TransitionContext context = default)
    {
        _field = field;
        _fsm = machine;

        _field.SwapTiles(context.From, context.PositionFrom, context.To, context.PositionTo);

        AnimateSwap(context);

        _fsm.Switch(StateEvent.SwapBack, context);
    }

    private void AnimateSwap(TransitionContext context)
    {
        var animationDataList = new List<AnimationData>();

        // From возвращается на свою родную позицию (PositionFrom)
        var dataFrom = new AnimationData
        {
            Type = AnimationType.Move,
            Target = context.From.transform,
            TargetPosition = _field.GetWorldPos(context.PositionFrom),
            Duration = 1.0f
        };
        animationDataList.Add(dataFrom);

        // To возвращается на свою родную позицию (PositionTo)
        var dataTo = new AnimationData
        {
            Type = AnimationType.Move,
            Target = context.To.transform,
            TargetPosition = _field.GetWorldPos(context.PositionTo),
            Duration = 1.0f
        };
        animationDataList.Add(dataTo);

        var name = _fsm.Events.GetBusName(GameEvent.Animation);
        GameplayEventBus<List<AnimationData>>.Trigger(name, animationDataList);
    }
}
