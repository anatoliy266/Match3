using System.Collections.Generic;
using System.Linq;
using UnityEngine;


//public abstract class TileSpawnRuleBase : ScriptableObject
//{
//    [Tooltip("Размер сетки (ширина, высота)")]
//    public Vector2Int gridSize;
//    public List<Vector2Int> activeCells;
//    [Req]public TileType Type;
//    public abstract bool IsMatch(IEnumerable<Vector2Int> cells);
//}

public abstract class TileSpawnRuleBase : ScriptableObject
{
    [Tooltip("Размер сетки (ширина, высота)")]
    public Vector2Int gridSize;
    public List<Vector2Int> activeCells;
    public bool CheckRotations;

    // [Req] сохраняем ваш кастомный атрибут, если он необходим
    public TileType Type;

    /// <summary>
    /// Универсальный метод нормализации координат любой фигуры.
    /// </summary>
    public HashSet<Vector2Int> Normalize(HashSet<Vector2Int> cells)
    {
        if (cells == null || cells.Count == 0) return new HashSet<Vector2Int>();

        // ИСПРАВЛЕНО: Теперь минимумы ищутся строго в переданной коллекции cells
        int minX = int.MaxValue, minY = int.MaxValue;
        foreach (var c in cells)
        {
            if (c.x < minX) minX = c.x;
            if (c.y < minY) minY = c.y;
        }

        var normalized = new HashSet<Vector2Int>();
        foreach (var c in cells)
            normalized.Add(new Vector2Int(c.x - minX, c.y - minY));

        return normalized;
    }

    /// <summary>
    /// Сделан virtual вместо abstract, чтобы содержать общую для всех бонусов логику сопоставления.
    /// </summary>
    public virtual bool IsMatch(IEnumerable<Vector2Int> cells)
    {
        if (cells == null) return false;

        var inputCells = new HashSet<Vector2Int>(cells);
        var inputNormalized = Normalize(inputCells);

        var referenceNormalized = Normalize(activeCells.ToHashSet());
        if (referenceNormalized.Count != inputNormalized.Count) return false;

        // Проверяем совпадение с учетом всех 4 поворотов
        if (CheckRotations)
            return GetAllRotations(referenceNormalized).Any(rotated => rotated.SetEquals(inputNormalized));
        return referenceNormalized.SetEquals(inputNormalized);
    }

    private IEnumerable<HashSet<Vector2Int>> GetAllRotations(HashSet<Vector2Int> shape)
    {
        HashSet<Vector2Int> current = shape;
        yield return current;

        for (int i = 0; i < 3; i++)
        {
            current = Rotate90Clockwise(current);
            yield return current;
        }
    }

    private HashSet<Vector2Int> Rotate90Clockwise(HashSet<Vector2Int> shape)
    {
        var rotated = new HashSet<Vector2Int>();
        foreach (var p in shape)
            rotated.Add(new Vector2Int(p.y, -p.x));

        return Normalize(rotated);
    }
}