using Assets.Game.Components;
using System;
using System.Collections.Generic;
using UnityEngine;

public enum MatchSource
{
    Common,
    Bomb,
    VerticalBomb,
    HorisontalBomb
}


[CreateAssetMenu(fileName = "MatchRules", menuName = "Scriptable Objects/MatchRules")]
public class MatchRules : ScriptableObject
{
    [SerializeField]
    public List<TileMatchRuleBase> Rules;


    public TileMatchRuleBase GetRule(TileType type)
    {
        foreach (var rule in Rules)
        {
            if (rule is TileCommonMatchRule && type is
                TileType.Orange or TileType.Purple or TileType.Blue or
                TileType.Yellow or TileType.Red or TileType.Green or
                TileType.Neutral)
            {
                return rule;
            }
            if (rule is TileBombMatchRule && type is TileType.Bomb)
            {
                return rule;
            }
            if (rule is TileHorizontalBombMatchRule && type is TileType.HorizontalBomb)
            {
                return rule;
            }
            if (rule is TileVerticalBombMatchRule && type is TileType.VerticalBomb)
            {
                return rule;
            }
        }

        return null;
    }
}

