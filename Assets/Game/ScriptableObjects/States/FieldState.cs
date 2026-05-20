using System;
using System.Collections.Generic;
using UnityEngine;

public struct TransitionContext
{
    public StateEvent Type;
    public TileController From;
    public TileController To;
    public Vector2Int PositionFrom;
    public Vector2Int PositionTo;

    public HintInfo CurrentHint { get; set; }
    public bool HasAvailableMoves { get; set; }

    public List<CompactInfo> Compacts {  get; internal set; }
    public List<MatchInfo> Matches { get; internal set; }
    public int CascadeIteration { get; internal set; }
    public TileController.Snapshot?[,] Snapshot { get; internal set; }
    public bool DragNDropEnable { get; internal set; }
}

public abstract class FieldState : ScriptableObject 
{
    protected FieldController _field;
    protected FiniteStateMachine _fsm;
    public virtual void Init() { }
    public abstract void Enter(FieldController field, FiniteStateMachine machine, TransitionContext context = default);

    public virtual void OnFieldEvent(TransitionContext context) { }
}
