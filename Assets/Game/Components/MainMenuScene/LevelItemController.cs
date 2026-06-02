using System;
using TMPro;
using UnityEngine;

public class LevelItem : MonoBehaviour
{
    private LevelsMenuController _controller;
    private int _levelId;

    [SerializeField][Req] private TextMeshProUGUI levelText;


    internal void Fill(LevelSettings levelSettings, LevelsMenuController controller)
    {
        _controller = controller;
        _levelId = levelSettings.levelNumber;
        levelText.text = $"Уровень {_levelId}";
    }

    public void OnItemPressed()
    {
        _controller.RunLevel(_levelId);
    }
}
