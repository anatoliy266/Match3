using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class GameState : ScriptableObject 
{
    protected Field _field;
    protected FiniteStateMachine _fsm;
    public virtual void Init() { }
    public abstract void Enter(FiniteStateMachine machine);

}
