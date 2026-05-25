using Assets.Game.Components;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class RegularTileTypeRuleMapping
{
    [Tooltip("Тип тайла")]
    public RegularType tileType;

    [Tooltip("Правила, которые применяются к этому типу")]
    public List<TileMatchRuleBase> rules;
}

[Serializable]
public class BonusTileTypeRuleMapping
{
    [Tooltip("Тип тайла")]
    public BonusType tileType;

    [Tooltip("Правила, которые применяются к этому типу")]
    public List<TileMatchRuleBase> rules;
}


[CreateAssetMenu(fileName = "MatchRules", menuName = "Scriptable Objects/MatchRules")]
public class MatchRules : ScriptableObject
{
    [SerializeField]
    private List<RegularTileTypeRuleMapping> regularMappings;

    [SerializeField]
    private List<BonusTileTypeRuleMapping> bonusMappings;

    // Для быстрого доступа можно построить словарь в рантайме
    private Dictionary<RegularType, IReadOnlyList<TileMatchRuleBase>> regularLookup;
    private Dictionary<BonusType, IReadOnlyList<TileMatchRuleBase>> bonusLookup;

    private readonly IReadOnlyList<TileMatchRuleBase> EmptyRules = new List<TileMatchRuleBase>().AsReadOnly();

    private void OnEnable()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        regularLookup = new Dictionary<RegularType, IReadOnlyList<TileMatchRuleBase>>();
        if (regularMappings is not null)
        {
            foreach (var mapping in regularMappings)
            {
                if (mapping.rules != null && mapping.rules.Count > 0)
                {
                    // Используем индексатор, чтобы безопасно перезаписать данные, если тип продублирован в инспекторе
                    regularLookup[mapping.tileType] = mapping.rules;
                }
            }
        }

        bonusLookup = new Dictionary<BonusType, IReadOnlyList<TileMatchRuleBase>>();
        if (bonusMappings is not null)
        {
            foreach (var mapping in bonusMappings)
            {
                if (mapping.rules != null && mapping.rules.Count > 0)
                {
                    bonusLookup[mapping.tileType] = mapping.rules;
                }
            }
        }
    }

    /// <summary>Возвращает все правила для указанного типа тайла</summary>


    public IReadOnlyList<TileMatchRuleBase> GetRules(TileKind type)
    {
        return type.KindType switch
        {
            TileKindType.Regular => RegularRule(type.RegularType),
            TileKindType.Bonus => BonusRule(type.BonusType),
            _ => EmptyRules
        };
    }

    private IReadOnlyList<TileMatchRuleBase> RegularRule(RegularType type)
    {
        if (regularLookup != null && regularLookup.TryGetValue(type, out var rules))
            return rules;

        return EmptyRules;
    }

    private IReadOnlyList<TileMatchRuleBase> BonusRule(BonusType type)
    {
        if (bonusLookup != null && bonusLookup.TryGetValue(type, out var rules))
            return rules;

        return EmptyRules;
    }
}

