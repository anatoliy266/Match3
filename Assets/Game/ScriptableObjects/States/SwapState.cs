using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "SwapState", menuName = "Scriptable Objects/SwapState")]
public class SwapState : FieldState
{
    public override void Enter(FieldController field, FiniteStateMachine machine, TransitionContext context = default)
    {
        _field = field;
        _fsm = machine;

        _field.SwapTiles(context.From, context.PositionTo, context.To, context.PositionFrom);

        AnimateSwap(context);


        _fsm.Switch(StateEvent.Swap, context);
    }


    private void AnimateSwap(TransitionContext context)
    {
        var animationDataList = new List<AnimationData>();

        var dataFrom = new AnimationData
        {
            Type = AnimationType.Move,
            Target = context.From.transform,
            TargetPosition = _field.GetWorldPos(context.PositionTo),
            Duration = 1.0f
        };
        animationDataList.Add(dataFrom);

        var dataTo = new AnimationData
        {
            Type = AnimationType.Move,
            Target = context.To.transform,
            TargetPosition = _field.GetWorldPos(context.PositionFrom),
            Duration = 1.0f
        };
        animationDataList.Add(dataTo);

        var name = _fsm.Events.GetBusName(GameEvent.Animation);
        GameplayEventBus<List<AnimationData>>.Trigger(name, animationDataList);
    }
}



