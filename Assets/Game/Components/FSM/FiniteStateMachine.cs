using Mono.Cecil.Cil;
using UnityEngine;

public class FiniteStateMachine : MonoBehaviour
{

    [Tooltip("Состояния")]
    [Req] public FieldStates States;
    [Tooltip("Точка старта")]
    [Req] public GameState State;
    [Req] public Field Field;

    public FieldBlackboard Blackboard {  get; set; }
    public MatchEvaluator MatchEvaluator { get; set; }
    public SpawnEvaluator SpawnEvaluator { get; set; }
    private void Awake()
    {
        Field = GetComponent<Field>();
        Blackboard = new FieldBlackboard();
        MatchEvaluator = new MatchEvaluator();
        SpawnEvaluator = new SpawnEvaluator();
    }

    private void Start()
    {
        if (State != null) State.Enter(this);
    }

    public void Switch(StateEvent e)
    {
        var nextState = States.GetTransition(State, e);
        if (nextState is not null)
        {
            State = nextState;
            State.Enter(this);
        }
    }
}
