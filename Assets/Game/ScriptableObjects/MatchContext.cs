using UnityEngine;
public struct MatchContext
{
    public TileType MatchingType { get; set; }
    public Vector2Int[] Epicenters { get; set; }
}