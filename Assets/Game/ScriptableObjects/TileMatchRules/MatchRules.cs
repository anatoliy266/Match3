using Assets.Game.Components;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class TileTypeRuleMapping
{
    [Tooltip("Тип тайла")]
    public TileType tileType;

    [Tooltip("Правила, которые применяются к этому типу")]
    public List<TileMatchRuleBase> rules;
}


[CreateAssetMenu(fileName = "MatchRules", menuName = "Scriptable Objects/MatchRules")]
public class MatchRules : ScriptableObject
{
    [SerializeField]
    private List<TileTypeRuleMapping> mappings;

    // Для быстрого доступа можно построить словарь в рантайме
    private Dictionary<TileType, List<TileMatchRuleBase>> lookup;

    private void OnEnable()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        lookup = new Dictionary<TileType, List<TileMatchRuleBase>>();
        if (mappings == null) return;
        foreach (var mapping in mappings)
        {
            if (mapping.rules != null && mapping.rules.Count > 0)
                lookup[mapping.tileType] = mapping.rules;
        }
    }

    /// <summary>Возвращает все правила для указанного типа тайла</summary>
    public List<TileMatchRuleBase> GetRules(TileType type)
    {
        if (lookup == null) BuildLookup();
        lookup.TryGetValue(type, out var rules);
        return rules ?? new List<TileMatchRuleBase>();
    }

    public TileMatchRuleBase GetRule(TileType type)
    {
        var rules = GetRules(type);
        return rules.Count > 0 ? rules[0] : null;
    }
}

