using NUnit.Framework;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public struct RopeGoal
{
    // Ссылка на единый тип плитки твоего движка (и для обычных, и для бонусных)
    public TileKind Kind;

    // Сколько именно нитей этого типа должно быть сгенерировано на уровне
    public int count;
}

[CreateAssetMenu(fileName = "Level", menuName = "Levels/New Level")]
public class LevelSettings : ScriptableObject
{
    public int levelNumber;
    public int Rows;
    public int Columns;

    public int Steps;

    public List<RegularType> RegiularTilesList;
    public List<BonusType> BonusTilesList;

    public Sprite backgroundSprite;

    [Header("Level Goals")]
    public List<RopeGoal> ropesGoalsList;
}
