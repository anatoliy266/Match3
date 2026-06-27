using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;


[Serializable]
public class RegularTileDataMapping
{
    [Tooltip("Тип тайла")]
    public RegularType tileType;

    [Tooltip("Data")]
    public TileDataBase data;
    
}

[Serializable]
public class BonusTileDataMapping
{
    [Tooltip("Тип тайла")]
    public BonusType tileType;

    [Tooltip("Data")]
    public TileDataBase data;
}

[Serializable]
public class BlockerTileDataMapping
{
    [Tooltip("Тип блокиратора")]
    public BlockerType tileType;

    [Tooltip("Data")]
    public TileDataBase data;
}

[CreateAssetMenu(fileName = "TileTypeData", menuName = "Scriptable Objects/TileTypeData")]
public class TileTypeData : ScriptableObject
{
    [SerializeField] List<RegularTileDataMapping> RegularTiles;
    [SerializeField] List<BonusTileDataMapping> BonusTiles;
    [SerializeField] List<BlockerTileDataMapping> BlockerTiles;

    private Dictionary<RegularType, TileDataBase> _regularLookup;
    private Dictionary<BonusType, TileDataBase> _bonusLookup;
    private Dictionary<BlockerType, TileDataBase> _blockerLookup;

    private void OnEnable()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        if (_regularLookup == null)
            _regularLookup = new Dictionary<RegularType, TileDataBase>(RegularTiles.Count);
        else
            _regularLookup.Clear();

        for (int i = 0; i < RegularTiles.Count; i++)
        {
            var mapping = RegularTiles[i];
            if (mapping != null && mapping.data != null)
            {
                _regularLookup[mapping.tileType] = mapping.data;
            }
        }

        if (_bonusLookup == null)
            _bonusLookup = new Dictionary<BonusType, TileDataBase>(BonusTiles.Count);
        else
            _bonusLookup.Clear();

        for (int i = 0; i < BonusTiles.Count; i++)
        {
            var mapping = BonusTiles[i];
            if (mapping != null && mapping.data != null)
            {
                _bonusLookup[mapping.tileType] = mapping.data;
            }
        }

        if (_blockerLookup == null)
            _blockerLookup = new Dictionary<BlockerType, TileDataBase>(BlockerTiles.Count);
        else
            _blockerLookup.Clear();

        for (int i = 0; i < BlockerTiles.Count; i++)
        {
            var mapping = BlockerTiles[i];
            if (mapping != null && mapping.data != null)
            {
                _blockerLookup[mapping.tileType] = mapping.data;
            }
        }
    }

    public TileDataBase GetData(TileKind kind)
    {
        switch (kind.KindType)
        {
            case TileKindType.Regular:
                return _regularLookup[kind.RegularType];
            case TileKindType.Bonus:
                return _bonusLookup[kind.BonusType];
            case TileKindType.Blocker:
                return _blockerLookup[kind.BlockerType];
            default:
                return null;
        }
    }
}
