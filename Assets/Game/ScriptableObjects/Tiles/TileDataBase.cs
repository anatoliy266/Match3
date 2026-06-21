using UnityEngine;

[CreateAssetMenu(fileName = "TileBase", menuName = "Tiles/TileData")]
public class TileDataBase : ScriptableObject
{
    [Req] public Color Color;
    [Req] public Color HighlightColor;
    [Req] public Color ShadowColor;

    [Req] public Sprite Sprite;
}
