using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LevelsMenuController : MonoBehaviour
{
    [SerializeField][Req] private LevelItem levelMenuItem;
    [SerializeField][Req] Levels Levels;
    
    [SerializeField][Req] private VerticalLayoutGroup contentLayout;

    private List<LevelItem> _levelItems = new List<LevelItem>();

    // События, на которые подпишется MainMenuController
    public event Action<int> OnLevelSelected;
    public event Action OnCloseRequested;

    public void Initialize()
    {
        if (Levels == null) return;
        for (var i = 1; i <= Levels.LevelsCount; i++)
        {
            var parent = contentLayout != null ? contentLayout.transform : this.transform;

            var settings = Levels.GetLevelSettings(i);
            if (settings == null)
                continue;

            var levelItem = Instantiate(levelMenuItem, parent, false);
            levelItem.Fill(settings, this);
            _levelItems.Add(levelItem);
        }
    }

    public void RunLevel(int levelId)
    {
        
        OnLevelSelected?.Invoke(levelId);
    }

    public void RunInfiniteLevel()
    {
        
        OnLevelSelected?.Invoke(-1);
    }

    public void CloseMenu()
    {
        OnCloseRequested?.Invoke();
    }
}