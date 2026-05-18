using System;
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

    //public readonly struct Snapshot
    //{
    //    public TileType Type { get; }
    //    public Vector2Int GridPosition { get; }
    //    public Transform Transform { get; }
    //    public bool IsBonus { get; }
    //    public Snapshot(TileController tile)
    //    {
    //        Type = tile.Type;
    //        GridPosition = tile.GridPosition;
    //        IsBonus = tile.IsBonus; 
    //        Transform = tile.transform;
    //    }

    //    public Snapshot(TileType type, Vector2Int gridPos, Transform transform, bool isbonus)
    //    {
    //        Type = type;
    //        GridPosition = gridPos;
    //        IsBonus = isbonus;
    //        Transform = transform;
    //    }
    //    public static implicit operator Snapshot?(TileController tile) =>
    //        tile != null ? new Snapshot(tile) : null;

    //    public Snapshot WithType(TileType newType) =>
    //    new Snapshot
    //    {
    //        Type = newType,
    //        GridPosition = GridPosition,
    //        Transform = Transform,
    //        IsBonus = IsBonus
    //    };
    //}
    public readonly struct Snapshot   // ключевое слово readonly – гарант неизменности
    {
        public TileType Type { get; }
        public Vector2Int GridPosition { get; }
        public Transform Transform { get; }
        public bool IsBonus { get; }

        // Конструктор для создания нового снепшота
        public Snapshot(TileType type, Vector2Int gridPosition, Transform transform, bool isBonus)
        {
            Type = type;
            GridPosition = gridPosition;
            Transform = transform;
            IsBonus = isBonus;
        }

        // Фабричный метод из TileController (альтернатива публичному конструктору)
        public static Snapshot FromTile(TileController tile) =>
            new Snapshot(tile.Type, tile.GridPosition, tile.transform, tile.IsBonus);

        // Вместо мутации – создаём копию с новым типом
        public Snapshot WithType(TileType newType) =>
            new Snapshot(newType, GridPosition, Transform, IsBonus);


        public static implicit operator Snapshot(TileController tile)
        {
            if (tile == null) return default;
            return new Snapshot(tile.Type, tile.GridPosition, tile.transform, tile.IsBonus);
        }
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
