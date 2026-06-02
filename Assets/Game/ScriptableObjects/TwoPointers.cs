using System;
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
        var write = 0;
        for (var read = 0; read < r; read++)
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
