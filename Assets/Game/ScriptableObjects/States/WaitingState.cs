using System;
using UnityEngine;

[CreateAssetMenu(fileName = "WaitingState", menuName = "Scriptable Objects/WaitingState")]
public class WaitingState : GameState
{
    [Req] public Events Events;
    public override void Enter(FiniteStateMachine machine)
    {
        Debug.Log("заходит вы вейтинг");
        _fsm = machine;
        var name = Events.GetBusName(GameEvent.AnimationEnd);
        GameplayEventBus<bool>.Register(name, OnAnimationEnd);
    }

    private void OnAnimationEnd(bool obj)
    {
        Debug.Log("дождался конца анимации");
        var name = Events.GetBusName(GameEvent.AnimationEnd);
        GameplayEventBus<bool>.Unregister(name, OnAnimationEnd);

        _fsm.Switch(StateEvent.AnimationEnd);
    }
}
