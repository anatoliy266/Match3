using System;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.InputSystem;

public struct IdleTransitionData
{
    public Vector2Int FromPos {  get; set; }
    public Vector2Int FromStartPos { get; set; }
    public Vector2Int ToPos { get; set; }
    public Vector2Int ToStartPos { get; set; }
}

public struct SwapInfo
{
    public Guid SourceId {  get; set; }
    public Guid DestId { get; set; }
}




[CreateAssetMenu(fileName = "IdleState", menuName = "Scriptable Objects/IdleState")]
public class IdleState : GameState
{
    [Req] public Events Events;
    public override void Enter(FiniteStateMachine machine)
    {
        _fsm = machine;
        var name = Events.GetBusName(GameEvent.Input);
        GameplayEventBus<bool>.Trigger(name, true);
        GameplayEventBus<SwapInfo>.Register(name, OnFieldEvent);

        var fieldSettledBusName = Events.GetBusName(GameEvent.FieldSettled);
        GameplayEventBus<int>.Trigger(fieldSettledBusName, _fsm.Blackboard.Step);
    }

    public void OnFieldEvent(SwapInfo eventData)
    {
        var name = Events.GetBusName(GameEvent.Input);
        GameplayEventBus<bool>.Trigger(name, false);
        GameplayEventBus<SwapInfo>.Unregister(name, OnFieldEvent);
        _fsm.Blackboard.Step++;
        _fsm.Blackboard.SourceDest = eventData;

        _fsm.Switch(StateEvent.MoveTiles);
    }
}
