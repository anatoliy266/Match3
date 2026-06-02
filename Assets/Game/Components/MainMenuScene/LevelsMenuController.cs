using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelsMenuController : MonoBehaviour
{
    [SerializeField][Req] private LevelItem levelMenuItem;
    [SerializeField][Req] Levels Levels;
    [SerializeField][Req] SessionData SessionData;


    //[SerializeField][Req] private List<LevelSettings> allLevels;

    //private Dictionary<int, LevelSettings> levelMenuItems = new Dictionary<int, LevelSettings>();
    private List<LevelItem> _levelItems = new List<LevelItem>();

    public void Initialize()
    {
        if (Levels is null) return;
        for (var i = 0; i < Levels.LevelsCount; i++)
        {
            var levelItem = Instantiate(levelMenuItem, this.transform);
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
