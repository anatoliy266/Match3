using System;
using UnityEngine;
using static TileController;

[CreateAssetMenu(fileName = "TileCommonMatchRule", menuName = "Rules/Common")]
[Serializable]
public class TileCommonMatchRule : TileMatchRuleBase
{

    public override bool IsMatch(TileController.Snapshot?[,] board, Vector2Int source, Vector2Int current, Vector2Int target)
    {
        return Mathf.Abs(target.x - current.x) + Mathf.Abs(target.y - current.y) == 1 &&
            board[current.x, current.y] is Snapshot c && board[target.x, target.y] is Snapshot t &&
            (c.Type == t.Type);
    }
}