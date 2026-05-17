using NUnit.Framework;
using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;
using UnityEngine.UI;
using static UnityEngine.Rendering.DebugUI;

public class ScoreManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public TextMeshProUGUI Score;
    public ScoreData ScoreData;

    

    public void AddScore(int score)
    {
        
    }

    internal void CalculateScore(List<MatchInfo> matches, int cascadeIteration)
    {
        foreach (var match in matches)
        {
            ScoreData.AddScore(1 * cascadeIteration);
        }
        
        Score.text = $"SCORE: {ScoreData.Score}";
    }
}
