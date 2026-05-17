using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[Serializable]
public class FieldStateMapping
{
    [Tooltip("Состояние")]
    [Req]public FieldState State;

    [Tooltip("Переходы")]
    [Req] public List<Transition> Transitions;
}

[Serializable]
public class Transition
{
    [Tooltip("Событие")]
    [Req] public StateEvent Event;

    [Tooltip("Переход")]
    [Req] public FieldState State;
}

public enum StateEvent
{
    FinishLoading,
    MoveTiles,
    ShowHint,
    Calculate,
    Swap,
    NoMatches,
    MatchesFound,
    DestroyTiles,
    CompactTiles,
    FillEmptyTiles,
    HasMoves,
    NoMovesLeft
}


[CreateAssetMenu(fileName = "FieldStates", menuName = "Scriptable Objects/FieldStates")]
public class FieldStates : ScriptableObject
{
    [SerializeField]
    private List<FieldStateMapping> mappings;

    // Для быстрого доступа можно построить словарь в рантайме
    private Dictionary<FieldState, Dictionary<StateEvent, FieldState>> _lookup;

    private void OnEnable()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<FieldState, Dictionary<StateEvent, FieldState>>();
        if (mappings == null) return;
        foreach (var mapping in mappings)
        {
            if (mapping.Transitions != null && mapping.Transitions.Count > 0)
                _lookup[mapping.State] = mapping.Transitions.ToDictionary(v => v.Event, v => v.State);
        }
    }


    public FieldState GetTransition(FieldState type, StateEvent e)
    {
        if (_lookup == null) BuildLookup();
        _lookup.TryGetValue(type, out var transitions);
        transitions.TryGetValue(e, out var state);
        return state;
    }
}
