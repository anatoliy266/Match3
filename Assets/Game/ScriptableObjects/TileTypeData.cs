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

    public Color GetColor(RegularType type)
    {
        return type switch
        {
            RegularType.Red => _red,
            RegularType.Green => _green,
            RegularType.Blue => _blue,
            RegularType.Yellow => _yellow,
            RegularType.Orange => _orange,
            RegularType.Purple => _purple,
            _ => _neutral

        };
    }

    public Color GetColor(BonusType type)
    {
        return type switch
        {
            BonusType.Bomb => _black,
            BonusType.VerticalBomb => _gray,
            BonusType.HorizontalBomb => _coral,
            _ => _neutral

        };
    }

    public Color GetColor(TileKind kind)
    {
        if (kind.KindType == TileKindType.Regular)
            return GetColor(kind.RegularType);
        else
            return GetColor(kind.BonusType);
    }
}
