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
        var (r, c) = (context.Field.GetLength(0), context.Field.GetLength(1));

        var (read, write) = (0, 0);
        while (read < r)
        {
            while (context.Field[write, startPos.y] is not null && write < r) write++;

            if (context.Field[read, startPos.y] is not null && read > write)
            {
                var data = new TileTransitionData
                {
                    From = new Vector2Int(read, startPos.y),
                    To = new Vector2Int(write, startPos.y)
                };
                groupResult.Add(data);
                write++;
            }
            read++;
        }
    }
}
