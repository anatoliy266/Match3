using UnityEngine;
using UnityEngine.SceneManagement;

public class WinPanelController : MonoBehaviour
{
    private LevelController _levelController;

    [SerializeField][Req] private SessionData sessionData;
    [SerializeField][Req] private Levels Levels;


    public void Init(LevelController levelController)
    {
        _levelController = levelController;
    }

    public void NextLevel()
    {
        sessionData.currentLevelId++;
        _levelController.StartLevel(Levels.GetLevelSettings(sessionData.currentLevelId));
    }

    public void Back()
    {
        sessionData.currentLevelId++;
        SceneManager.LoadScene("MainMenuScene");
    }
}
