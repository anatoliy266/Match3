
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "TileHorizontalBombMatchRule", menuName = "Rules/HorizontalBomb")]
[Serializable]
public class TileHorizontalBombMatchRule : TileMatchRuleBase
{
    [SerializeField] private Vector2Int Axis = Vector2Int.left;

    public override bool IsMatch(TileController.Snapshot?[,] board, Vector2Int source, Vector2Int current, Vector2Int target)
    {
        return target.y == source.y;
    }
}