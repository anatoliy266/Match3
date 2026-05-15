using UnityEngine;

[CreateAssetMenu(fileName = "TileTypeData", menuName = "Scriptable Objects/TileTypeData")]
public class TileTypeData : ScriptableObject
{
    [SerializeField] private Color _blue = Color.blue;
    [SerializeField] private Color _red = Color.red;
    [SerializeField] private Color _yellow = Color.yellow;
    [SerializeField] private Color _green = Color.green;
    [SerializeField] private Color _orange = Color.orange;
    [SerializeField] private Color _purple = Color.purple;
    [SerializeField] private Color _neutral = Color.white;

    [SerializeField] private Color _black = Color.black;
    [SerializeField] private Color _gray = Color.gray;
    [SerializeField] private Color _coral = Color.coral;

    public Color Black => _black;
    public Color Gray => _gray;
    public Color Coral => _coral;
    public Color Blue => _blue;
    public Color Red => _red;
    public Color Yellow => _yellow;
    public Color Green => _green;
    public Color Orange => _orange;
    public Color Purple => _purple;

    public Color Neutral => _neutral;

    public Color GetColor(TileType type)
    {
        return type switch
        {
            TileType.Red => _red,
            TileType.Green => _green,
            TileType.Blue => _blue,
            TileType.Yellow => _yellow,
            TileType.Orange => _orange,
            TileType.Purple => _purple,
            TileType.Bomb => _black,
            TileType.VerticalBomb => _gray,
            TileType.HorizontalBomb => _coral,
            _ => _neutral

        };
    }
    public int GetScore(TileType type) => type switch
    {
        TileType.Red or TileType.Green or TileType.Blue
            or TileType.Yellow or TileType.Purple or TileType.Orange => 1,
        TileType.Bomb => 9,
        TileType.VerticalBomb or TileType.HorizontalBomb => 4,
        _ => 0
    };

}
