using UnityEngine;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    [SerializeField][Req] Canvas mainMenuCanvas;
    [SerializeField][Req] LevelsMenuController levelsMenu;

    

    private LevelsMenuController _levelsMenu;

    public void OpenLevelsWindow()
    {
        if (_levelsMenu is null)
        {
            _levelsMenu = Instantiate(levelsMenu, mainMenuCanvas.transform);
            _levelsMenu.Initialize();
        }
        _levelsMenu.gameObject.SetActive(true);
    }
}
