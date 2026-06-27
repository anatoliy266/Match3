using UnityEngine;
using UnityEngine.SceneManagement;
using YG;

public class EndOfLevelsController : MonoBehaviour
{
    public void Exit()
    {
        if (YG2.isTimerAdvCompleted)
        {
            YG2.onCloseInterAdv += OnInterstitialClosed;
            YG2.InterstitialAdvShow();
        }
        else
        {
            this.gameObject.SetActive(false);
            SceneManager.LoadScene("MainMenuScene");
        }
    }

    private void OnInterstitialClosed()
    {
        YG2.onCloseInterAdv -= OnInterstitialClosed;

        this.gameObject.SetActive(false);
        SceneManager.LoadScene("MainMenuScene");
    }
}
