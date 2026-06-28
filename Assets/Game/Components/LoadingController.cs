using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadingController : MonoBehaviour
{
    [SerializeField] private Slider slider;
    public void Initialize()
    {
        //this.gameObject.SetActive(false);
    }

    public void LoadingScene(string SceneName)
    {
        //this.gameObject.SetActive(true);
        slider.value = 0;
        StartCoroutine(LoadSceneCoroutine(SceneName));

    }

    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            slider.value = progress;
            yield return null;
        }
    }
}
