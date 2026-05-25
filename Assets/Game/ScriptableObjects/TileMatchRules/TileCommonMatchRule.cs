using System;
using UnityEngine;
using static Tile;

[CreateAssetMenu(fileName = "TileCommonMatchRule", menuName = "Rules/Common")]
[Serializable]
public class TileCommonMatchRule : TileMatchRuleBase
{
    public override bool IsMatch(in TileSnapshot source, in TileSnapshot current, in TileSnapshot target)
    {
        int manhattanDistance = Mathf.Abs(target.Position.x - current.Position.x) +
                               Mathf.Abs(target.Position.y - current.Position.y);

        if (manhattanDistance != 1) return false;

        // 2. Проверяем логику: обе плитки должны быть обычными (Regular) и одного цвета
        if (current.Kind.KindType == TileKindType.Regular && target.Kind.KindType == TileKindType.Regular)
        {
            return current.Kind.RegularType == target.Kind.RegularType;
        }

        return false;
    }
}