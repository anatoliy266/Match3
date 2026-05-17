using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[Serializable]
public class TileTypeSpawnRuleMapping
{
    [Tooltip("Тип тайла")]
    public TileType tileType;

    [Tooltip("Правила, которые применяются к этому типу")]
    public List<TileSpawnRuleBase> rules;
}

[CreateAssetMenu(fileName = "SpawnRules", menuName = "Scriptable Objects/SpawnRules")]
public class SpawnRules : ScriptableObject
{
    [SerializeField]
    private List<TileTypeSpawnRuleMapping> mappings;

    // Для быстрого доступа можно построить словарь в рантайме
    private Dictionary<TileType, List<TileSpawnRuleBase>> lookup;

    private void OnEnable()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        lookup = new Dictionary<TileType, List<TileSpawnRuleBase>>();
        if (mappings == null) return;
        foreach (var mapping in mappings)
        {
            if (mapping.rules != null && mapping.rules.Count > 0)
                lookup[mapping.tileType] = mapping.rules;
        }
    }

    /// <summary>Возвращает все правила для указанного типа тайла</summary>
    public List<TileSpawnRuleBase> GetRules(TileType type)
    {
        if (lookup == null) BuildLookup();
        lookup.TryGetValue(type, out var rules);
        return rules ?? new List<TileSpawnRuleBase>();
    }

    public TileSpawnRuleBase GetRule(TileType type)
    {
        var rules = GetRules(type);
        return rules.Count > 0 ? rules[0] : null;
    }

    
}
