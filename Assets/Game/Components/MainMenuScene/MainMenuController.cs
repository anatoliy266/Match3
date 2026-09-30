using UnityEngine;
using UnityEngine.UI;
using YG;

public class MainMenuController : MonoBehaviour
{
    [SerializeField][Req] Canvas mainMenuCanvas;
    [SerializeField][Req] LevelsMenuController levelsMenu;
    [SerializeField][Req] SettingsMenuController settingsMenu;
    [SerializeField][Req] LoadingController loadingScreen;

    private LevelsMenuController _levelsMenu;
    private SettingsMenuController _settingsMenu;
    private LoadingController _loadingScreen;

    private void Start()
    {
        _levelsMenu = Instantiate(levelsMenu, mainMenuCanvas.transform);
        _levelsMenu.Initialize();
        _levelsMenu.gameObject.SetActive(false);

        _settingsMenu = Instantiate(settingsMenu, mainMenuCanvas.transform);
        _settingsMenu.gameObject.SetActive(false);
    }

    public void OpenLevelsWindow()
    {
        if (_levelsMenu != null && _levelsMenu.gameObject.activeSelf) return;

        _levelsMenu.OnLevelSelected += HandleLevelSelected;
        _levelsMenu.OnCloseRequested += HandleCloseLevelsMenu;

        _levelsMenu.gameObject.SetActive(true);
        LayoutRebuilder.ForceRebuildLayoutImmediate(
            _levelsMenu.GetComponent<RectTransform>()
        );
    }

    public void OpenSettingsWindow()
    {
        _settingsMenu.gameObject.SetActive(true);
    }

    private void HandleLevelSelected(int levelId)
    {
        if (_levelsMenu != null)
        {
            _levelsMenu.OnLevelSelected -= HandleLevelSelected;
            _levelsMenu.OnCloseRequested -= HandleCloseLevelsMenu;

            _levelsMenu.gameObject.SetActive(false);
        }

        if (_loadingScreen is null)
        {
            _loadingScreen = Instantiate(loadingScreen, mainMenuCanvas.transform);
        } else
        {
            _loadingScreen.gameObject.SetActive(true);
        }

        YG2.saves.currentLevelId = levelId;
        YG2.SaveProgress();

        _loadingScreen.LoadingScene("LevelScene");
    }

    private void HandleCloseLevelsMenu()
    {
        if (_levelsMenu != null)
        {
            _levelsMenu.OnLevelSelected -= HandleLevelSelected;
            _levelsMenu.OnCloseRequested -= HandleCloseLevelsMenu;

            _levelsMenu.gameObject.SetActive(false);
        }
    }
}
