using System;
using UnityEngine;
using UnityEngine.InputSystem;

[CreateAssetMenu(fileName = "IdleState", menuName = "Scriptable Objects/IdleState")]
public class IdleState : FieldState
{
    public override void Enter(FieldController field, FiniteStateMachine machine, TransitionContext context = default)
    {
        _field = field;
        _fsm = machine;
        var name = _fsm.Events.GetBusName(GameEvent.Input);
        GameplayEventBus<bool>.Trigger(name, true);
        GameplayEventBus<TransitionContext>.Register(name, OnFieldEvent);
    }

    public override void OnFieldEvent(TransitionContext eventData)
    {
        
        try
        {
            var name = _fsm.Events.GetBusName(GameEvent.Input);
            GameplayEventBus<bool>.Trigger(name, false);
            GameplayEventBus<TransitionContext>.Unregister(name, OnFieldEvent);

            _fsm.Switch(eventData.Type, eventData);
        } catch (Exception e)
        {
            Debug.LogException(e);
        }
        
    }
}
