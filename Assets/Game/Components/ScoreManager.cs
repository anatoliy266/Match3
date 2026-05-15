using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreManager : MonoBehaviour
{
    public static ScoreManager Instance;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public TextMeshProUGUI Score;
    public ScoreData ScoreData;
    private void Awake()
    {
        Instance = this;
    }

    public void AddScore(int score)
    {
        ScoreData.AddScore(score);
        Score.text = $"SCORE: {ScoreData.Score}";
    }
}
