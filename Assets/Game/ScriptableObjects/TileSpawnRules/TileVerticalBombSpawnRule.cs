using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[CreateAssetMenu(fileName = "TileVerticalBombSpawnRule", menuName = "Tile Spawn Rules/TileVerticalBombSpawnRule")]
public class TileVerticalBombSpawnRule : TileSpawnRuleBase
{
    /// <summary>
    /// Возвращает HashSet для быстрого сравнения в ShapeBonusRule
    /// </summary>
    public HashSet<Vector2Int> Normalize(HashSet<Vector2Int> cells)
    {
        // Нормализуем: вычитаем минимальные координаты, чтобы форма не зависела от положения в сетке
        if (cells.Count == 0) return new HashSet<Vector2Int>();
        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var c in activeCells)
        {
            if (c.x < minX) minX = c.x;
            if (c.y < minY) minY = c.y;
        }
        var normalized = new HashSet<Vector2Int>();
        foreach (var c in cells)
            normalized.Add(new Vector2Int(c.x - minX, c.y - minY));
        return normalized;
    }


    public override bool IsMatch(IEnumerable<Vector2Int> cells)
    {
        if (cells == null) return false;

        // Нормализуем входящую фигуру
        var inputCells = new HashSet<Vector2Int>(cells);

        var inputNormalized = Normalize(inputCells);

        // Нормализованная эталонная фигура
        var reference = Normalize(activeCells.ToHashSet());
        if (reference.Count != inputNormalized.Count) return false;

        // Проверяем все повороты эталона (0°, 90°, 180°, 270°)
        return GetAllRotations(reference).Any(rotated => rotated.SetEquals(inputNormalized));
    }

    /// <summary>
    /// Возвращает все 4 уникальных поворота (без отражений) нормализованной фигуры.
    /// </summary>
    private IEnumerable<HashSet<Vector2Int>> GetAllRotations(HashSet<Vector2Int> shape)
    {
        HashSet<Vector2Int> current = shape;
        yield return current;

        // 90° по часовой: (x, y) -> (y, -x), затем снова нормализуем
        for (int i = 0; i < 3; i++)
        {
            current = Rotate90Clockwise(current);
            yield return current;
        }
    }

    /// <summary>
    /// Поворот на 90° по часовой стрелке вокруг (0,0) и повторная нормализация.
    /// </summary>
    private HashSet<Vector2Int> Rotate90Clockwise(HashSet<Vector2Int> shape)
    {
        var rotated = new HashSet<Vector2Int>();
        foreach (var p in shape)
            rotated.Add(new Vector2Int(p.y, -p.x));
        var normalized = Normalize(rotated);
        return normalized;
    }
}