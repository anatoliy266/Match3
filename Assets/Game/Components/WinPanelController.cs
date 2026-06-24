using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using YG;
namespace YG
{
    public partial class SavesYG
    {
        public int currentLevelId;
    }
}
    

public class WinPanelController : MonoBehaviour
{
    private LevelController _levelController;

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
        YG2.saves.currentLevelId++;
        YG2.SaveProgress();


        _levelController.StartLevel(Levels.GetLevelSettings(YG2.saves.currentLevelId));
    }

    public void Back()
    {
        if (YG2.isTimerAdvCompleted)
        {
            YG2.onCloseInterAdv += OnInterstitialClosed;
            YG2.InterstitialAdvShow();
        } else
        {
            this.gameObject.SetActive(false);
            YG2.saves.currentLevelId++;
            YG2.SaveProgress();

            SceneManager.LoadScene("MainMenuScene");
        }
        
    }

    private void OnInterstitialClosed()
    {
        YG2.onCloseInterAdv -= OnInterstitialClosed;

        this.gameObject.SetActive(false);
        YG2.saves.currentLevelId++;
        YG2.SaveProgress();

        SceneManager.LoadScene("MainMenuScene");
    }
}
