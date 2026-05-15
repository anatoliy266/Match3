using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using static UnityEngine.Rendering.DebugUI.Table;

public class MatchEvaluator : MonoBehaviour
{
    public MatchRules MatchRules;
    public static MatchEvaluator Instance { get; private set; }
    [ThreadStatic] private static Queue<Vector2Int> _queue;

    private void Awake()
    {
        Instance = this;
    }

    public List<HashSet<Vector2Int>> FindAll(TileController.Snapshot?[,] board)
    {
        var (rows, cols) = (board.GetLength(0), board.GetLength(1));

        var groups = new List<HashSet<Vector2Int>>();

        var visited = ArrayPool<bool>.Shared.Rent(rows * cols);
        Array.Clear(visited, 0, rows * cols);

        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                if (board[r, c] is null || visited[r * cols + c]) continue;
                if (board[r, c] is not null && board[r, c].Value.IsBonus) continue;


                //var rule = MatchRules.GetRule(board[r, c].Value.Type);
                //var group = BFS(board, board[r, c].Value.GridPosition, rule.IsMatch);
                var group = GroupAt(board, board[r, c].Value.GridPosition);

                foreach (var pos in group)
                {
                    visited[pos.x * cols + pos.y] = true;
                }

                if (group.Count > 2)
                {
                    groups.Add(group);
                }

            }
        }
        return groups;
    }

    //public List<HashSet<Vector2Int>> FindAllBonuses(TileController.Snapshot?[,] board, Vector2Int startPos)
    //{
    //    var (rows, cols) = (board.GetLength(0), board.GetLength(1));
    //    var groups = new List<HashSet<Vector2Int>>();

    //    var visited = ArrayPool<bool>.Shared.Rent(rows * cols);
    //    Array.Clear(visited, 0, rows * cols);

    //    var bonusQueue = new Queue<Vector2Int>();
    //    bonusQueue.Enqueue(startPos);

    //    while (bonusQueue.Count > 0)
    //    {
    //        var currentBonusPos = bonusQueue.Dequeue();

    //        int bonusIndex = currentBonusPos.x * cols + currentBonusPos.y;
    //        if (visited[bonusIndex]) continue;

    //        visited[bonusIndex] = true;
    //        var rawGroup = GroupAt(board, currentBonusPos);
    //        var uniqueBonusGroup = new HashSet<Vector2Int>();

    //        foreach (var pos in rawGroup)
    //        {
    //            int index = pos.x * cols + pos.y;
    //            if (visited[index]) continue;
    //            visited[index] = true;
    //            uniqueBonusGroup.Add(pos);
    //            if (board[pos.x, pos.y] is not null
    //                && board[pos.x, pos.y].Value.IsBonus)
    //            {
    //                bonusQueue.Enqueue(pos);
    //            }
    //        }
    //        if (uniqueBonusGroup.Count > 0)
    //        {
    //            groups.Add(uniqueBonusGroup);
    //        }
    //    }

    //    ArrayPool<bool>.Shared.Return(visited);
    //    return groups;
    //}

    public List<HashSet<Vector2Int>> FindAllBonuses(TileController.Snapshot?[,] board, Vector2Int startPos)
    {
        var (rows, cols) = (board.GetLength(0), board.GetLength(1));
        var groups = new List<HashSet<Vector2Int>>();

        var visited = ArrayPool<bool>.Shared.Rent(rows * cols);
        Array.Clear(visited, 0, rows * cols);

        var bonusQueue = new Queue<Vector2Int>();
        bonusQueue.Enqueue(startPos);

        var enqueuedBonuses = new HashSet<Vector2Int> { startPos };

        while (bonusQueue.Count > 0)
        {
            var currentBonusPos = bonusQueue.Dequeue();
            int bonusIndex = currentBonusPos.x * cols + currentBonusPos.y;

            if (visited[bonusIndex]) continue;

            var rawGroup = GroupAt(board, currentBonusPos);
            var uniqueBonusGroup = new HashSet<Vector2Int>();

            foreach (var pos in rawGroup)
            {
                if (board[pos.x, pos.y] is not null && board[pos.x, pos.y].Value.IsBonus)
                {
                    if (!enqueuedBonuses.Contains(pos) && !visited[pos.x * cols + pos.y])
                    {
                        enqueuedBonuses.Add(pos);
                        bonusQueue.Enqueue(pos); 
                    }
                    uniqueBonusGroup.Add(pos);
                    continue;
                }
                int index = pos.x * cols + pos.y;
                if (visited[index]) continue;

                visited[index] = true;
                uniqueBonusGroup.Add(pos);
            }

            visited[bonusIndex] = true;

            if (uniqueBonusGroup.Count > 0)
            {
                groups.Add(uniqueBonusGroup);
            }
        }

        ArrayPool<bool>.Shared.Return(visited);
        return groups;
    }


    public HashSet<Vector2Int> GroupAt(TileController.Snapshot?[,] board, Vector2Int start)
    {
        var rule = MatchRules.GetRule(board[start.x, start.y].Value.Type);
        var group = BFS(board, board[start.x, start.y].Value.GridPosition, rule.IsMatch);
        return group;
    }

    public int GroupSizeAt(TileController.Snapshot?[,] board, Vector2Int start)
    {
        if (start.x < 0 || start.x >= board.GetLength(0) ||
            start.y < 0 || start.y >= board.GetLength(1))
            return 0;
        if (board[start.x, start.y] == null) return 0;

        var rows = board.GetLength(0);
        var cols = board.GetLength(1);

        var rule = MatchRules.GetRule(board[start.x, start.y].Value.Type);

        var count = BFS(board, start, rule.IsMatch).Count;


        return count;
    }

    private HashSet<Vector2Int> BFS(TileController.Snapshot?[,] board,
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
                        Debug.Log("rume match");
                        visited[i * cols + j] = true;
                        _queue.Enqueue(board[i, j].Value.GridPosition);
                    }
                    Debug.Log("rume not match");
                }
            }
        }
        ArrayPool<bool>.Shared.Return(visited);
        return group;
    }
}


