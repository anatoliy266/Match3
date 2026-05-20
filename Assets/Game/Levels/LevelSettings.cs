using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "Levels/New Level")]
public class LevelSettings : ScriptableObject
{
    public int levelNumber;
    public int Rows;
    public int Columns;
}
