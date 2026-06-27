using Mono.Cecil.Cil;
using UnityEngine;

public class FiniteStateMachine : MonoBehaviour
{

    [Tooltip("Состояния")]
    [Req] public FieldStates States;
    [Tooltip("Точка старта")]
    [Req] public GameState State;
    [Req] public Field Field;

    public FieldBlackboard Blackboard;
    public MatchEvaluator MatchEvaluator;
    public SpawnEvaluator SpawnEvaluator;

    private GameState _state;
    //private void Awake()
    //{
    //    Field = GetComponent<Field>();
    //}

    public void Init(LevelSettings settings, Field currentFieldInstance)
    {
        _state = State;
        Field = currentFieldInstance;
        Blackboard = new FieldBlackboard();
        MatchEvaluator = new MatchEvaluator();
        SpawnEvaluator = new SpawnEvaluator();
        Blackboard.MaxSteps = settings.Steps;
        Blackboard.Step = 0;
        Blackboard.CascadeIteration = 0;
        Blackboard.IsFinalState = false;
        Blackboard.LevelSettings = settings;
    }

    public void Run()
    {
        if (_state != null) _state.Enter(this);
    }

    public GameState GetState() => _state;

    public void Switch(StateEvent e)
    {
        var nextState = States.GetTransition(_state, e);

        Debug.Log($"переключается из {_state?.GetType().Name ?? "Null"} в {nextState?.GetType().Name ?? "Null"}");
        if (nextState is not null)
        {
            _state = nextState;
            _state.Enter(this);
        }
    }
}
