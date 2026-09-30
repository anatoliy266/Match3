using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockerAdjacentMatchRule", menuName = "Scriptable Objects/BlockerDestroyRules/AdjacentMatch")]
public class BlockerAdjacentMatchRule : BlockerDestroyRuleBase
{
    private static readonly Vector2Int[] Directions = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    public override bool ShouldDestroy(Vector2Int pos, LogicalTile?[,] snapshot, HashSet<Guid> matchedIds)
    {
        if (matchedIds == null || matchedIds.Count == 0)
            return false;

        for (int d = 0; d < Directions.Length; d++)
        {
            var neighbor = pos + Directions[d];
            if (neighbor.x < 0 || neighbor.x >= snapshot.GetLength(0) ||
                neighbor.y < 0 || neighbor.y >= snapshot.GetLength(1))
                continue;

            var tile = snapshot[neighbor.x, neighbor.y];
            if (tile is null) continue;
            if (matchedIds.Contains(tile.Value.Id))
                return true;
        }

        return false;
    }
}
