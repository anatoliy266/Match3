using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField][Req] Canvas mainMenuCanvas;
    [SerializeField][Req] LevelsMenuController levelsMenu;
    [SerializeField][Req] SettingsMenuController settingsMenu;



    private LevelsMenuController _levelsMenu;
    private SettingsMenuController _settingsMenu;

    public void OpenLevelsWindow()
    {
        if (_levelsMenu is null)
        {
            _levelsMenu = Instantiate(levelsMenu, mainMenuCanvas.transform);
            _levelsMenu.Initialize();
        }
        _levelsMenu.gameObject.SetActive(true);
    }

    public void OpenSettingsWindow()
    {
        if (_settingsMenu is null)
        {
            _settingsMenu = Instantiate(settingsMenu, mainMenuCanvas.transform);
            _settingsMenu.Initialize();
        }
        _settingsMenu.gameObject.SetActive(true);
    }
}
