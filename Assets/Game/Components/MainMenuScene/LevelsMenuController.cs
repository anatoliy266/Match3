using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LevelsMenuController : MonoBehaviour
{
    [SerializeField][Req] private LevelItem levelMenuItem;
    [SerializeField][Req] Levels Levels;
    [SerializeField][Req] SessionData SessionData;

    [SerializeField][Req] private VerticalLayoutGroup contentLayout;

    private List<LevelItem> _levelItems = new List<LevelItem>();

    public void Initialize()
    {
        if (Levels == null) return;
        for (var i = 1; i <= Levels.LevelsCount; i++)
        {
            var parent = contentLayout != null ? contentLayout.transform : this.transform;

            var levelItem = Instantiate(levelMenuItem, parent);
            levelItem.Fill(Levels.GetLevelSettings(i), this);
            _levelItems.Add(levelItem);
        }
    }

    public void RunLevel(int levelId)
    {
        this.gameObject.SetActive(false);

        SessionData.currentLevelId = levelId;
        SceneManager.LoadScene("LevelScene");
    }

    public void CloseMenu()
    {
        this.gameObject.SetActive(false);
    }
}
