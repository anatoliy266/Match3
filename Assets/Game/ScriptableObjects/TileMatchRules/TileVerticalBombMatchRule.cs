using System;
using UnityEngine;

[CreateAssetMenu(fileName = "TileVerticalBombMatchRule", menuName = "Rules/Vertical Bomb")]
public class TileVerticalBombMatchRule : TileMatchRuleBase
{
    public override bool IsMatch(in TileSnapshot source, in TileSnapshot current, in TileSnapshot target)
    {
        // Цель находится на той же вертикальной линии (в том же столбце), что и источник
        return target.Position.x == source.Position.x;
    }
}