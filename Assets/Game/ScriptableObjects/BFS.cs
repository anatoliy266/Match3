using System;
using System.Buffers;
using System.Collections.Generic;
using UnityEngine;

public static class BFS
{
    [ThreadStatic]
    private static Queue<Vector2Int> _queue;
    public static HashSet<Vector2Int> Run(TileController.Snapshot?[,] board,
        Vector2Int start,
        Func<TileController.Snapshot?[,], Vector2Int, Vector2Int, Vector2Int, bool> canConnect)
    {
        var (rows, cols) = (board.GetLength(0), board.GetLength(1));

        var visited = ArrayPool<bool>.Shared.Rent(rows * cols);
        Array.Clear(visited, 0, rows * cols);

        _queue ??= new Queue<Vector2Int>();
        _queue.Clear();

        visited[start.x * cols + start.y] = true;

        _queue.Enqueue(start);

        var group = new HashSet<Vector2Int>();
        while (_queue.Count > 0)
        {
            var pos = _queue.Dequeue();
            group.Add(pos);

            for (var i = pos.x - 1; i <= pos.x + 1; i++)
            {
                for (var j = pos.y - 1; j <= pos.y + 1; j++)
                {

                    if (i < 0 || i >= rows || j < 0 || j >= cols) continue;
                    if (visited[i * cols + j]) continue;
                    if (board[i, j] is null) continue;

                    if (canConnect(board, start, pos, board[i, j].Value.GridPosition))
                    {
                        visited[i * cols + j] = true;
                        _queue.Enqueue(board[i, j].Value.GridPosition);
                    }
                }
            }
        }
        ArrayPool<bool>.Shared.Return(visited);
        return group;
    }
}
