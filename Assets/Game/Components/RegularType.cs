using UnityEngine;

public enum RegularType { Red, Green, Blue, Yellow, Orange, Purple }
public enum BonusType { Bomb, VerticalBomb, HorizontalBomb, ColorBomb }
public enum TileKindType { Regular, Bonus }

public struct TileKind
{
    public TileKindType KindType;
    public RegularType RegularType;
    public BonusType BonusType;
    public RegularType? TargetColor;

    public static TileKind Regular(RegularType type) => new TileKind
    {
        KindType = TileKindType.Regular,
        RegularType = type
    };

    public static TileKind Bonus(BonusType type, RegularType? target = null) => new TileKind
    {
        KindType = TileKindType.Bonus,
        BonusType = type,
        TargetColor = target
    };
}