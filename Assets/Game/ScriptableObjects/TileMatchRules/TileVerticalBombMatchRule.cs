using System;
using UnityEngine;

[CreateAssetMenu(fileName = "TileVerticalBombMatchRule", menuName = "Rules/VerticalBomb")]
[Serializable]
public class TileVerticalBombMatchRule : TileMatchRuleBase
{
    [SerializeField] private Vector2Int Axis = Vector2Int.down;

    public override bool IsMatch(TileController.Snapshot?[,] board, Vector2Int source, Vector2Int current, Vector2Int target)
    {
        return target.x == source.x;
    }
}
