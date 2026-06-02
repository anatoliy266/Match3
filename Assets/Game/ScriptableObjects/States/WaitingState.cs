using System;
using UnityEngine;

[CreateAssetMenu(fileName = "WaitingState", menuName = "Scriptable Objects/WaitingState")]
public class WaitingState : GameState
{
    [Req] public Events Events;
    public override void Enter(FiniteStateMachine machine)
    {
        _fsm = machine;
        var name = Events.GetBusName(GameEvent.AnimationEnd);
        GameplayEventBus<bool>.Register(name, OnAnimationEnd);
    }

    private void OnAnimationEnd(bool obj)
    {
        var name = Events.GetBusName(GameEvent.AnimationEnd);
        GameplayEventBus<bool>.Unregister(name, OnAnimationEnd);

        _fsm.Switch(StateEvent.AnimationEnd);
    }
}
