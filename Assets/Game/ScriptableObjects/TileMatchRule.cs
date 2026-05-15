using NUnit.Framework;
using System;
using Unity.VisualScripting;
using UnityEngine;
using static TileController;

public abstract class TileMatchRuleBase: ScriptableObject
{
    public abstract bool IsMatch(TileController.Snapshot?[,] board, Vector2Int source, Vector2Int current, Vector2Int target);
}

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

[CreateAssetMenu(fileName = "TileBombMatchRule", menuName = "Rules/Bomb")]
[Serializable]
public class TileBombMatchRule : TileMatchRuleBase
{
    [SerializeField] private int ExplosionRadius = 1;

    public override bool IsMatch(TileController.Snapshot?[,] board, Vector2Int source, Vector2Int current, Vector2Int target)
    {
        Vector2Int diff = target - source;
        int distance = Mathf.Max(Mathf.Abs(diff.x), Mathf.Abs(diff.y));
        return distance <= ExplosionRadius;
    }
}

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