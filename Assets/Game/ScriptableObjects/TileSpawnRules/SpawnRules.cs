using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


[Serializable]
public class TileTypeSpawnRuleMapping
{
    [Tooltip("Тип тайла")]
    public BonusType tileType;

    [Tooltip("Правила, которые применяются к этому типу")]
    public List<TileSpawnRuleBase> rules;
}

[CreateAssetMenu(fileName = "SpawnRules", menuName = "Scriptable Objects/SpawnRules")]
public class SpawnRules : ScriptableObject
{
    [SerializeField]
    private List<TileTypeSpawnRuleMapping> mappings;

    private Dictionary<int, IReadOnlyList<TileSpawnRuleBase>> bonusLookup;
    private readonly IReadOnlyList<TileSpawnRuleBase> EmptyRules = new List<TileSpawnRuleBase>().AsReadOnly();

    private void OnEnable()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        var workingLookup = new Dictionary<int, List<TileSpawnRuleBase>>();

        if (mappings == null) return;

        for (var i = 0; i < mappings.Count; i++)
        {
            var mapping = mappings[i];
            if (mapping.rules is null) continue;

            for (var j = 0; j < mapping.rules.Count; j++)
            {
                var rule = mapping.rules[j];
                if (rule.activeCells is null) continue;

                int count = rule.activeCells.Count;

                if (workingLookup.TryGetValue(count, out var list))
                {
                    if (!list.Contains(rule))
                    {
                        list.Add(rule);
                    }
                }
                else
                {
                    workingLookup[count] = new List<TileSpawnRuleBase> { rule };
                }
            }
        }
        bonusLookup = new Dictionary<int, IReadOnlyList<TileSpawnRuleBase>>();
        foreach (var kvp in workingLookup)
        {
            bonusLookup[kvp.Key] = kvp.Value;
        }
    }


    /// <summary>Возвращает все правила для указанного типа тайла</summary>
    public IReadOnlyList<TileSpawnRuleBase> GetRules(int groupCount)
    {
        if (mappings == null) return EmptyRules;
        if (bonusLookup == null) return EmptyRules;
        if (!bonusLookup.TryGetValue(groupCount, out var rules)) return EmptyRules;
        return rules;
    }
}
