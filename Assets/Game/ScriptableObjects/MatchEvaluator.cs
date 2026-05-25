using System;
using System.Buffers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Video;

public struct MatchInfo
{
    public TileKind GroupType;
    public List<Guid> Positions;
    public int Count => Positions.Count;
}

//проверяет поле на наличие совпадений по правилам
public class MatchEvaluator
{
    [ThreadStatic] private static Queue<Vector2Int> _queue = new Queue<Vector2Int>();

    //идет по полю и проверяет плитки на совпадение по цвету через бфс, возвращает список групп
    public void Evaluate(LogicalTile?[,] snapshot, MatchRules rules, List<MatchInfo> matches)
    {

        var (r, c) = (snapshot.GetLength(0), snapshot.GetLength(1));

        var visited = ArrayPool<bool>.Shared.Rent(r * c);

        Array.Clear(visited, 0, visited.Length);
        _queue.Clear();

        for (var i = 0; i < r; i++)
        {
            for (var j = 0; j < c; j++)
            {
                if (snapshot[i, j] is null) continue;
                if (snapshot[i, j].Value.Type.KindType == TileKindType.Bonus) continue;
                if (visited[i * c + j]) continue;

                var pos = new Vector2Int(i, j);
                var type = snapshot[i, j].Value.Type;
                var tileRules = rules.GetRules(type);
                var group = new List<Guid>();
                var data = new AlgoritmContext
                {
                    Field = snapshot,
                    Queue = _queue,
                    Visited = visited,
                    Rules = tileRules,
                };
                BFS.Run(pos, data, group);

                if (group.Count > 2)
                {
                    matches.Add(new MatchInfo { GroupType = type, Positions = group });
                }
            }
        }
        ArrayPool<bool>.Shared.Return(visited);
    }


    public void GetBonusGroupAt(LogicalTile?[,] snapshot, Vector2Int position, MatchRules rules, List<MatchInfo> groups)
    {
        var (r, c) = (snapshot.GetLength(0), snapshot.GetLength(1));


        var visited = ArrayPool<bool>.Shared.Rent(r * c);
        Array.Clear(visited, 0, visited.Length);

        if (snapshot[position.x, position.y] is null) return;
        var type = snapshot[position.x, position.y].Value.Type;
        if (type.KindType == TileKindType.Regular) return;

        var tileRules = rules.GetRules(type);
        var group = new List<Guid>();
        var data = new AlgoritmContext
        {
            Field = snapshot,
            Queue = _queue,
            Visited = visited,
            Rules = tileRules,
        };
        BFS.Run(position, data, group);

        ArrayPool<bool>.Shared.Return(visited);
        groups.Add(new MatchInfo { GroupType = type, Positions = group });
    }
}

