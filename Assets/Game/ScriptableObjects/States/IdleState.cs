using System;
using UnityEngine;

public struct IdleTransitionData
{
    public Vector2Int FromPos { get; set; }
    public Vector2Int FromStartPos { get; set; }
    public Vector2Int ToPos { get; set; }
    public Vector2Int ToStartPos { get; set; }
}

public struct SwapInfo
{
    public Guid SourceId { get; set; }
    public Guid DestId { get; set; }
}




[CreateAssetMenu(fileName = "IdleState", menuName = "Scriptable Objects/IdleState")]
public class IdleState : GameState
{
    [Req] public Events Events;
    public override void Enter(FiniteStateMachine machine)
    {
        _fsm = machine;
        var inputBusName = Events.GetBusName(GameEvent.Input);
        GameplayEventBus<SwapInfo>.Register(inputBusName, OnFieldEvent);

        var fieldSettledBusName = Events.GetBusName(GameEvent.FieldSettled);
        GameplayEventBus<bool>.Register(fieldSettledBusName, OnFinalEvent);

        GameplayEventBus<int>.Trigger(fieldSettledBusName, _fsm.Blackboard.Step);
    }

    private void OnFinalEvent(bool isLevelEnd)
    {
        var fieldSettledBusName = Events.GetBusName(GameEvent.FieldSettled);
        GameplayEventBus<bool>.Unregister(fieldSettledBusName, OnFinalEvent);

        if (isLevelEnd)
        {
            var inputBusName = Events.GetBusName(GameEvent.Input);
            GameplayEventBus<SwapInfo>.Register(inputBusName, OnFieldEvent);

            var name = Events.GetBusName(GameEvent.Input);
            GameplayEventBus<bool>.Trigger(name, false);

            _fsm.Switch(StateEvent.Final);
        }
        else
        {
            _fsm.Blackboard.Step++;

            var stepName = Events.GetBusName(GameEvent.StepIncreased);
            GameplayEventBus<int>.Trigger(stepName, _fsm.Blackboard.Step);

            var inputBusName = Events.GetBusName(GameEvent.Input);
            GameplayEventBus<bool>.Trigger(inputBusName, true);
        }
    }

    public void OnFieldEvent(SwapInfo eventData)
    {
        var fieldSettledBusName = Events.GetBusName(GameEvent.FieldSettled);
        GameplayEventBus<bool>.Unregister(fieldSettledBusName, OnFinalEvent);

        var name = Events.GetBusName(GameEvent.Input);
        GameplayEventBus<bool>.Trigger(name, false);
        GameplayEventBus<SwapInfo>.Unregister(name, OnFieldEvent);


        _fsm.Blackboard.SourceDest = eventData;

        _fsm.Switch(StateEvent.MoveTiles);
    }
}
