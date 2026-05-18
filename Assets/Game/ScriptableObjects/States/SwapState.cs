using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "SwapState", menuName = "Scriptable Objects/SwapState")]
public class SwapState : FieldState
{
    public override async void Enter(FieldController field, FiniteStateMachine machine, TransitionContext context = default)
    {
        _field = field;
        _fsm = machine;
        
        _field.SwapTiles(context.From, context.PositionTo, context.To, context.PositionFrom);

        await AnimateMovement(_field, context);


        _fsm.Switch(StateEvent.Swap, context);
    }


    private async Task AnimateMovement(FieldController field, TransitionContext context)
    {
        var moves = new List<Task>();
        
        var p1 = _field.AnimationManager.DoMoveExactTimeAsync(context.From.transform, field.GetWorldPos(context.PositionTo));
        moves.Add(p1);
        var p2 = _field.AnimationManager.DoMoveExactTimeAsync(context.To.transform, field.GetWorldPos(context.PositionFrom));
        moves.Add(p2);

        await Task.WhenAll(moves);
    }
}



