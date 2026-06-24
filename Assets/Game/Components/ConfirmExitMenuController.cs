using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using YG;

public class ConfirmExitMenuController : MonoBehaviour
{

    public void OnYes()
    {
        if (YG2.isTimerAdvCompleted)
        {
            YG2.onCloseInterAdv += OnInterstitialClosed;
            YG2.InterstitialAdvShow();
        }
        else
        {
            // Если реклама недоступна, сразу вызываем логику выхода
            this.gameObject.SetActive(false);
            SceneManager.LoadScene("MainMenuScene");
        }


        YG2.onCloseInterAdv += OnInterstitialClosed;
        YG2.InterstitialAdvShow();

    }

    public void OnNo()
    {
        this.gameObject.SetActive(false);
    }

    

    private void OnInterstitialClosed()
    {
        YG2.onCloseInterAdv -= OnInterstitialClosed;

        this.gameObject.SetActive(false);
        SceneManager.LoadScene("MainMenuScene");
    }
}
