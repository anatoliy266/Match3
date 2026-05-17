using Mono.Cecil.Cil;
using UnityEngine;

public class FiniteStateMachine : MonoBehaviour
{
    [Tooltip("Ссылка на управляемый обьект поля")]
    [Req] public FieldController Field;


    [Tooltip("Состояния")]
    [Req] public FieldStates States;

    [Req] public FieldState State;


    private void Start()
    {
        if (State != null) State.Enter(Field, this);
    }

    public void Switch(StateEvent e, TransitionContext context = default)
    {
        var nextState = States.GetTransition(State, e);
        if (nextState is not null)
        {
            State = nextState;
            State.Enter(Field, this, context);
        }
    }
}
