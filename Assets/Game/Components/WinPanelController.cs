using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class WinPanelController : MonoBehaviour
{
    private LevelController _levelController;

    [SerializeField][Req] private SessionData sessionData;
    [SerializeField][Req] private Levels Levels;

    [SerializeField][Req] private Image bgImage;


    public void Init(LevelController levelController, Sprite levelBackground)
    {
        _levelController = levelController;
        bgImage.sprite = levelBackground;
    }

    public void NextLevel()
    {
        this.gameObject.SetActive(false);
        sessionData.currentLevelId++;
        _levelController.StartLevel(Levels.GetLevelSettings(sessionData.currentLevelId));
    }

    public void Back()
    {
        this.gameObject.SetActive(false);
        sessionData.currentLevelId++;
        SceneManager.LoadScene("MainMenuScene");
    }
}
