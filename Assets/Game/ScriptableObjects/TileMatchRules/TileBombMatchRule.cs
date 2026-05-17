using System;
using UnityEngine;

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
