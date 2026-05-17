using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "IdleState", menuName = "Scriptable Objects/IdleState")]
public class IdleState : FieldState
{
    public override void Enter(FieldController field, FiniteStateMachine machine, TransitionContext context = default)
    {
        _field = field;
        _fsm = machine;
        _field.DragManager.OnDragCompleted += OnFieldEvent;
    }

    public override void OnFieldEvent(TransitionContext eventData)
    {
        _field.DragManager.OnDragCompleted -= OnFieldEvent;
        base.OnFieldEvent(eventData);
        _fsm.Switch(eventData.Type);
    }
}
