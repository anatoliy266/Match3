using Mono.Cecil.Cil;
using UnityEngine;

public class FiniteStateMachine : MonoBehaviour
{
    //[Tooltip("Ссылка на управляемый обьект поля")]
    //[Req] public FieldController Field;


    [Tooltip("Состояния")]
    [Req] public FieldStates States;
    [Tooltip("Точка старта")]
    [Req] public FieldState State;

    [Req] public Events Events;


    [Req] public FieldController _field;
    private void Awake()
    {
        // Машина автоматически находит соседа-контроллера на этом же префабе
        _field = GetComponent<FieldController>();
    }

    //public void Initialize(FieldController field)
    //{
    //    _field = field;
    //}


    private void Start()
    {
        if (State != null) State.Enter(_field, this, new TransitionContext { CascadeIteration = 1});
    }

    public void Switch(StateEvent e, TransitionContext context = default)
    {
        var nextState = States.GetTransition(State, e);
        if (nextState is not null)
        {
            State = nextState;
            State.Enter(_field, this, context);
        }
    }
}
