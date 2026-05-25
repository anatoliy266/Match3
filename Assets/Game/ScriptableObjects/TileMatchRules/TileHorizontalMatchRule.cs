
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "TileHorizontalBombMatchRule", menuName = "Rules/Horizontal Bomb")]
public class TileHorizontalBombMatchRule : TileMatchRuleBase
{
    public override bool IsMatch(in TileSnapshot source, in TileSnapshot current, in TileSnapshot target)
    {
        // Цель находится на той же горизонтальной линии (в той же строке), что и источник
        return target.Position.y == source.Position.y;
    }
}
