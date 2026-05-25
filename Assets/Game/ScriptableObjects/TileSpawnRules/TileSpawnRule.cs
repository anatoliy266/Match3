using System;
using System.Buffers; // Обязательно для работы с ArrayPool
using System.Collections.Generic;
using UnityEngine;

public abstract class TileSpawnRuleBase : ScriptableObject
{
    [Tooltip("Эталонная фигура из инспектора")]
    public List<Vector2Int> activeCells;

    [Tooltip("Крутить ли фигуру на 90, 180, 270 градусов при проверке")]
    public bool CheckRotations;

    [Tooltip("Какой бонус спавнить, если геометрия совпала")]
    public BonusType BonusType;

    // Сигнатура под твой List<Vector2Int> из BFS
    public virtual bool IsMatch(List<Vector2Int> groupCells)
    {
        if (groupCells == null || activeCells == null) return false;

        int count = activeCells.Count;
        if (groupCells.Count != count) return false;

        if (CheckShapeMatch(groupCells, activeCells)) return true;

        if (CheckRotations)
        {
            for (int rotation = 1; rotation <= 3; rotation++)
            {
                if (CheckShapeMatchRotated(groupCells, activeCells, rotation)) return true;
            }
        }
        return false;
    }

    private bool CheckShapeMatch(List<Vector2Int> input, List<Vector2Int> reference)
    {
        Vector2Int inputMin = GetMinBounds(input);
        Vector2Int refMin = GetMinBounds(reference);

        for (int i = 0; i < input.Count; i++)
        {
            Vector2Int normInput = input[i] - inputMin;
            bool found = false;

            for (int j = 0; j < reference.Count; j++)
            {
                Vector2Int normRef = reference[j] - refMin;
                if (normInput == normRef)
                {
                    found = true;
                    break;
                }
            }
            if (!found) return false;
        }
        return true;
    }

    private bool CheckShapeMatchRotated(List<Vector2Int> input, List<Vector2Int> reference, int rotationSteps)
    {
        int count = reference.Count;
        Vector2Int inputMin = GetMinBounds(input);

        Vector2Int[] rotatedRef = ArrayPool<Vector2Int>.Shared.Rent(count);

        int minX = int.MaxValue;
        int minY = int.MaxValue;

        for (int i = 0; i < count; i++)
        {
            Vector2Int p = reference[i];

            for (int r = 0; r < rotationSteps; r++)
                p = new Vector2Int(p.y, -p.x);

            rotatedRef[i] = p;

            if (p.x < minX) minX = p.x;
            if (p.y < minY) minY = p.y;
        }
        Vector2Int refMin = new Vector2Int(minX, minY);

        bool isMatch = true;
        for (int i = 0; i < input.Count; i++)
        {
            Vector2Int normInput = input[i] - inputMin;
            bool found = false;

            for (int j = 0; j < count; j++)
            {
                Vector2Int normRef = rotatedRef[j] - refMin;
                if (normInput == normRef)
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                isMatch = false;
                break;
            }
        }

        ArrayPool<Vector2Int>.Shared.Return(rotatedRef);
        return isMatch;
    }

    private Vector2Int GetMinBounds(List<Vector2Int> cells)
    {
        int minX = int.MaxValue;
        int minY = int.MaxValue;
        for (int i = 0; i < cells.Count; i++)
        {
            if (cells[i].x < minX) minX = cells[i].x;
            if (cells[i].y < minY) minY = cells[i].y;
        }
        return new Vector2Int(minX, minY);
    }
}
