using System.Collections.Generic;
using System.Linq;
using UnityEngine;


public abstract class TileSpawnRuleBase : ScriptableObject
{
    [Tooltip("Размер сетки (ширина, высота)")]
    public Vector2Int gridSize;
    public List<Vector2Int> activeCells;
    [Req]public TileType Type;
    public abstract bool IsMatch(IEnumerable<Vector2Int> cells);
}