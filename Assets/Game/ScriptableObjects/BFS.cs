using System;
using System.Collections.Generic;
using UnityEngine;

public struct AlgoritmContext
{
    public LogicalTile?[,] Field;
    public IReadOnlyList<TileMatchRuleBase> Rules;
    public Queue<Vector2Int> Queue;
    public bool[] Visited;
}



public static class BFS
{
    private static readonly Vector2Int[] Directions = {
        Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right
    };

    public static void Run(Vector2Int startPos, AlgoritmContext context, List<Guid> groupResult)
    {
        // 1. Проверяем стартовую плитку
        if (context.Field[startPos.x, startPos.y] is not LogicalTile startTile) return;

        if (context.Rules.Count == 0) return;

        int rows = context.Field.GetLength(0);
        int cols = context.Field.GetLength(1);

        // 2. Настраиваем старт
        var sourceSnapshot = new TileSnapshot(startPos, startTile.Type);
        context.Visited[startPos.x * cols + startPos.y] = true;
        context.Queue.Enqueue(startPos);
        groupResult.Add(startTile.Id);

        // 3. Основной цикл волнового поиска
        while (context.Queue.Count > 0)
        {
            Vector2Int currentPos = context.Queue.Dequeue();
            LogicalTile currentTile = context.Field[currentPos.x, currentPos.y].Value;
            var currentSnapshot = new TileSnapshot(currentPos, currentTile.Type);
            for (int d = 0; d < Directions.Length; d++)
            {
                var dir = Directions[d];
                Vector2Int targetPos = currentPos + dir;

                // Проверка границ поля
                if (targetPos.x < 0 || targetPos.x >= rows || targetPos.y < 0 || targetPos.y >= cols)
                    continue;

                // Проверка на посещение и существование плитки
                if (context.Visited[targetPos.x * cols + targetPos.y] ||
                    context.Field[targetPos.x, targetPos.y] is not LogicalTile targetTile)
                    continue;

                var targetSnapshot = new TileSnapshot(targetPos, targetTile.Type);

                // Прогоняем соседа по правилам
                bool isMatch = false;
                for (int i = 0; i < context.Rules.Count; i++)
                {
                    if (context.Rules[i].IsMatch(in sourceSnapshot, in currentSnapshot, in targetSnapshot))
                    {
                        isMatch = true;
                        break;
                    }
                }

                // Если подошел — забираем в группу
                if (isMatch)
                {
                    context.Visited[targetPos.x * cols + targetPos.y] = true;
                    context.Queue.Enqueue(targetPos);
                    groupResult.Add(targetTile.Id);
                }
            }
        }
    }
}