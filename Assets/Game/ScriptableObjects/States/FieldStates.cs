using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[Serializable]
public class FieldStateMapping
{
    [Tooltip("Состояние")]
    [Req]public GameState State;

    [Tooltip("Переходы")]
    public List<Transition> Transitions;
}

[Serializable]
public class Transition
{
    [Tooltip("Событие")]
    [Req] public StateEvent Event;

    [Tooltip("Переход")]
    [Req] public GameState State;
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
    FillUpTiles,
    FillEmptyTiles,
    HasMoves,
    NoMovesLeft,
    SwapBack,
    SpawnBonus
}


[CreateAssetMenu(fileName = "FieldStates", menuName = "Scriptable Objects/FieldStates")]
public class FieldStates : ScriptableObject
{
    [SerializeField]
    private List<FieldStateMapping> mappings;

    // Для быстрого доступа можно построить словарь в рантайме
    private Dictionary<GameState, Dictionary<StateEvent, GameState>> _lookup;

    private void OnEnable()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<GameState, Dictionary<StateEvent, GameState>>();
        if (mappings == null) return;
        foreach (var mapping in mappings)
        {
            if (mapping?.State == null || mapping.Transitions == null || mapping.Transitions.Count == 0) continue;
            if (mapping.Transitions != null && mapping.Transitions.Count > 0)
                _lookup[mapping.State] = mapping.Transitions.ToDictionary(v => v.Event, v => v.State);
        }
    }


    public GameState GetTransition(GameState type, StateEvent e)
    {
        if (_lookup == null) BuildLookup();
        if (_lookup.TryGetValue(type, out var transitions) && transitions.TryGetValue(e, out var state)) return state;
        return null;
    }
}
