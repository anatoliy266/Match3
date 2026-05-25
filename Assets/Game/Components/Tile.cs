using System;
using UnityEngine;
using UnityEngine.UIElements;

public class Tile : MonoBehaviour
{
    public TileKind Type;
    public bool IsBonus;
    public Guid Id;

    [SerializeField] private TileTypeData _tileType;
    private SpriteRenderer _spriteRenderer;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void SetType(TileKind type)
    {
        Type = type;
        _spriteRenderer.color = _tileType.GetColor(type);
    }
}
