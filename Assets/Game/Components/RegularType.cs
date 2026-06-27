using System;
using UnityEngine;

public enum RegularType { Red, Green, Blue, Yellow, Orange, Purple }
public enum BonusType { Bomb, VerticalBomb, HorizontalBomb }
public enum TileKindType { Regular, Bonus, Blocker }

[Serializable]
public struct TileKind
{
    public TileKindType KindType;
    public RegularType RegularType;
    public BonusType BonusType;
    public BlockerType BlockerType;
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

    public static TileKind Blocker(BlockerType type) => new TileKind
    {
        KindType = TileKindType.Blocker,
        BlockerType = type
    };

    public bool IsAnchored => KindType == TileKindType.Blocker && BlockerType == BlockerType.Box;
}