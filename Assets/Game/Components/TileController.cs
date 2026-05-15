using UnityEngine;
using UnityEngine.UIElements;

public class TileController : MonoBehaviour
{
    [SerializeField]
    public TileType Type;
    public Vector2Int GridPosition;
    public bool IsBonus;
    public bool IsMoving { get; set; }

    [SerializeField] private TileTypeData _tileType;
    [SerializeField] private Vector3 _scale;
    private SpriteRenderer _spriteRenderer;

    public readonly struct Snapshot
    {
        public TileType Type { get; }
        public Vector2Int GridPosition { get; }
        public bool IsBonus { get; }
        public Snapshot(TileController tile)
        {
            Type = tile.Type;
            GridPosition = tile.GridPosition;
            IsBonus = tile.IsBonus;
        }
        public static implicit operator Snapshot?(TileController tile) =>
            tile != null ? new Snapshot(tile) : null;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _scale = transform.localScale;
    }

    public void SetType(TileType type)
    {
        Type = type;
        _spriteRenderer.color = _tileType.GetColor(type);
    }

    public void Reset()
    {
        transform.localScale = _scale;
        IsMoving = false;
        IsBonus = false;
        GridPosition = new Vector2Int(-1, -1);
    }
}
