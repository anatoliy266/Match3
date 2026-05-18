using UnityEngine;

[CreateAssetMenu(fileName = "SwapBackState", menuName = "Scriptable Objects/SwapBackState")]
public class SwapBackState : FieldState
{
    public override void Enter(FieldController field, FiniteStateMachine machine, TransitionContext context = default)
    {
        _field = field;
        _fsm = machine;

        _fsm.Switch(StateEvent.SwapBack);
    }
}
