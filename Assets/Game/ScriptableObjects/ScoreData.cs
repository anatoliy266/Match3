using UnityEngine;

[CreateAssetMenu(fileName = "ScoreData", menuName = "Scriptable Objects/ScoreData")]
public class ScoreData : ScriptableObject
{
    public int Score;
    public int RecordScore;

    public void AddScore(int score)
    {
        Score += score;
        if (Score > RecordScore) RecordScore = Score;
    }
}
