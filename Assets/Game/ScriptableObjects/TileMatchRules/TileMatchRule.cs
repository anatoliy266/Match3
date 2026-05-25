using UnityEngine;

public readonly struct TileSnapshot
{
    public readonly Vector2Int Position;
    public readonly TileKind Kind;

    public TileSnapshot(Vector2Int position, TileKind kind)
    {
        Position = position;
        Kind = kind;
    }
}

public abstract class TileMatchRuleBase : ScriptableObject
{
    public abstract bool IsMatch(in TileSnapshot source, in TileSnapshot current, in TileSnapshot target);
}
