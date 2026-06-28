using System.Collections.Generic;
using UnityEngine;

public struct TileTransitionData
{
    public Vector2Int From;
    public Vector2Int To;
}

public static class TwoPointers
{
    public static void Run(Vector2Int startPos, AlgoritmContext context, List<TileTransitionData> groupResult)
    {
        var r = context.Snapshot.GetLength(0);
        var col = startPos.y;
        var segmentStart = 0;

        for (var read = 0; read < r; read++)
        {
            if (context.Snapshot[read, col] is null) continue;

            var tile = context.Snapshot[read, col].Value;
            if (tile.Type.IsAnchored)
            {
                CompactSegment(segmentStart, read, col, context, groupResult);
                segmentStart = read + 1;
            }
        }

        CompactSegment(segmentStart, r, col, context, groupResult);
    }

    private static void CompactSegment(int segmentStart, int segmentEnd, int col, AlgoritmContext context, List<TileTransitionData> groupResult)
    {
        var write = segmentStart;
        for (var read = segmentStart; read < segmentEnd; read++)
        {
            if (context.Snapshot[read, col] is null) continue;
            if (read != write)
            {
                groupResult.Add(new TileTransitionData
                {
                    From = new Vector2Int(read, col),
                    To = new Vector2Int(write, col)
                });
            }
            write++;
        }
    }
}
