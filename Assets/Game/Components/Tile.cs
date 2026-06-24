using System;
using UnityEngine;
using UnityEngine.UIElements;

public class Tile : MonoBehaviour
{
    public TileKind Type;
    public bool IsBonus;
    public Guid Id;

    [SerializeField] private Sprite _defaultSquareSprite;

    [SerializeField] private TileTypeData _tileType;

    [SerializeField] private SpriteRenderer shadow;
    private SpriteRenderer _spriteRenderer;
    private MaterialPropertyBlock _propBlock;

    private int _baseColorId;
    private int _highlightColorId;
    private int _shadowColorId;

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();

        _propBlock = new MaterialPropertyBlock();

        _baseColorId = Shader.PropertyToID("_BaseColor");
        _highlightColorId = Shader.PropertyToID("_HighlightColor");
        _shadowColorId = Shader.PropertyToID("_ShadowColor");
    }

    public void SetData(TileKind type)
    {
        Type = type;
        var data = _tileType.GetData(type);

        _spriteRenderer.GetPropertyBlock(_propBlock);

        _propBlock.SetColor(_baseColorId, data.Color);
        _propBlock.SetColor(_highlightColorId, data.HighlightColor);
        _propBlock.SetColor(_shadowColorId, data.ShadowColor);

        _spriteRenderer.SetPropertyBlock(_propBlock);

        if (data.Sprite != null)
        {
            _spriteRenderer.sprite = data.Sprite;
            shadow.sprite = data.Sprite;
        }
    }

    internal void CleanData()
    {
        _spriteRenderer.color = Color.white;
        _spriteRenderer.sprite = _defaultSquareSprite;
        shadow.sprite = _defaultSquareSprite;
    }
}
