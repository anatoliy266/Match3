using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Video;

public struct MatchInfo
{
    public TileType Type;
    public HashSet<Vector2Int> Positions;
    public int Count => Positions.Count;
}

[CreateAssetMenu(fileName = "MatchEvaluator", menuName = "Scriptable Objects/MatchEvaluator")]
public class MatchEvaluator : ScriptableObject
{
    public MatchRules MatchRules;
    [ThreadStatic] private static Queue<Vector2Int> _queue;


    public List<MatchInfo> FindAll(TileController.Snapshot?[,] board)
    {
        var (rows, cols) = (board.GetLength(0), board.GetLength(1));

        var groups = new List<MatchInfo>();

        var visited = ArrayPool<bool>.Shared.Rent(rows * cols);
        Array.Clear(visited, 0, rows * cols);

        for (var r = 0; r < rows; r++)
        {
            for (var c = 0; c < cols; c++)
            {
                if (board[r, c] is null || visited[r * cols + c]) continue;
                if (board[r, c] is not null && board[r, c].Value.IsBonus) continue;

                var group = GroupAt(board, board[r, c].Value.GridPosition);

                foreach (var pos in group)
                {
                    visited[pos.x * cols + pos.y] = true;
                }

                if (group.Count > 2)
                {
                    var matchInfo = new MatchInfo
                    {
                        Type = board[r, c].Value.Type,
                        Positions = group
                    };
                    groups.Add(matchInfo);
                }

            }
        }
        ArrayPool<bool>.Shared.Return(visited);
        return groups;
    }

    public List<MatchInfo> FindAllBonuses(TileController.Snapshot?[,] board, Vector2Int startPos)
    {
        var (rows, cols) = (board.GetLength(0), board.GetLength(1));
        //ищем по бонусу и его правилу все совпадения, если в совпадениях бонуса был найден другой бонус - ищем его тоже но
        //добавляем только ячейки которые не были до этого задеты другим бонусом
        var type = board[startPos.x, startPos.y].Value.Type;
        
        var groups = new List<MatchInfo>();
        if (type != TileType.Bomb && type != TileType.VerticalBomb && type != TileType.HorizontalBomb) return groups;

        var visited = ArrayPool<bool>.Shared.Rent(rows * cols);
        Array.Clear(visited, 0, visited.Length);

        var queue = new Queue<Vector2Int>();
        queue.Enqueue(startPos);

        while (queue.Count > 0)
        {
            var currPos = queue.Dequeue();
            var group = GroupAt(board, currPos);
            var cleanGroup = new MatchInfo { Type = board[startPos.x, startPos.y].Value.Type, Positions = new HashSet<Vector2Int>()};
            foreach (var pos in group)
            {
                if (!visited[pos.x * cols + pos.y])
                {
                    if (board[pos.x, pos.y].Value.IsBonus) queue.Enqueue(pos);
                    cleanGroup.Positions.Add(pos);
                    visited[pos.x * cols + pos.y] = true;
                }

            }
            groups.Add(cleanGroup);
        }
        ArrayPool<bool>.Shared.Return(visited);
        return groups;
    }

    public (Vector2Int pos1, Vector2Int pos2)? FindAvailableHint(TileController.Snapshot?[,] snapshot)
    {
        var (rows, cols) = (snapshot.GetLength(0), snapshot.GetLength(1));

        for (var i = 0; i < rows; i++)
        {
            for (var j = 0; j < cols; j++)
            {
                if (snapshot[i, j] == null) continue;

                // ВПРАВО (j + 1)
                if (j + 1 < cols && snapshot[i, j + 1] != null)
                {
                    (snapshot[i, j], snapshot[i, j + 1]) = (snapshot[i, j + 1], snapshot[i, j]);

                    if (GroupSizeAt(snapshot, new Vector2Int(i, j)) >= 3 ||
                        GroupSizeAt(snapshot, new Vector2Int(i, j + 1)) >= 3)
                    {
                        return (new Vector2Int(i, j), new Vector2Int(i, j + 1));
                    }

                    (snapshot[i, j], snapshot[i, j + 1]) = (snapshot[i, j + 1], snapshot[i, j]);
                }

                // ВНИЗ (i + 1)
                if (i + 1 < rows && snapshot[i + 1, j] != null)
                {
                    (snapshot[i, j], snapshot[i + 1, j]) = (snapshot[i + 1, j], snapshot[i, j]);

                    if (GroupSizeAt(snapshot, new Vector2Int(i, j)) >= 3 ||
                        GroupSizeAt(snapshot, new Vector2Int(i + 1, j)) >= 3)
                    {
                        return (new Vector2Int(i, j), new Vector2Int(i + 1, j));
                    }

                    (snapshot[i, j], snapshot[i + 1, j]) = (snapshot[i + 1, j], snapshot[i, j]);
                }
            }
        }
        return null; 
    }


    public HashSet<Vector2Int> GroupAt(TileController.Snapshot?[,] board, Vector2Int start)
    {
        var rule = MatchRules.GetRule(board[start.x, start.y].Value.Type);
        Debug.Log($"rule is null{rule is null}");
        var group = BFS.Run(board, board[start.x, start.y].Value.GridPosition, rule.IsMatch);
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

        var count = BFS.Run(board, start, rule.IsMatch).Count;


        return count;
    }

    //private HashSet<Vector2Int> BFS(TileController.Snapshot?[,] board,
    //    Vector2Int start,
    //    Func<TileController.Snapshot?[,], Vector2Int, Vector2Int, Vector2Int, bool> canConnect)
    //{
    //    var (rows, cols) = (board.GetLength(0), board.GetLength(1));

    //    var visited = ArrayPool<bool>.Shared.Rent(rows * cols);
    //    Array.Clear(visited, 0, rows * cols);

    //    _queue ??= new Queue<Vector2Int>();
    //    _queue.Clear();

    //    visited[start.x * cols + start.y] = true;

    //    _queue.Enqueue(start);

    //    var group = new HashSet<Vector2Int>();
    //    while (_queue.Count > 0)
    //    {
    //        var pos = _queue.Dequeue();
    //        group.Add(pos);

    //        for (var i = pos.x - 1; i <= pos.x + 1; i++)
    //        {
    //            for (var j = pos.y - 1; j <= pos.y + 1; j++)
    //            {

    //                if (i < 0 || i >= rows || j < 0 || j >= cols) continue;
    //                if (visited[i * cols + j]) continue;
    //                if (board[i, j] is null) continue;

    //                if (canConnect(board, start, pos, board[i, j].Value.GridPosition))
    //                {
    //                    visited[i * cols + j] = true;
    //                    _queue.Enqueue(board[i, j].Value.GridPosition);
    //                }
    //            }
    //        }
    //    }
    //    ArrayPool<bool>.Shared.Return(visited);
    //    return group;
    //}
}


