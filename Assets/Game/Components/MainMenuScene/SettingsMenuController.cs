using UnityEngine;

public class SettingsMenuController : MonoBehaviour
{
    [SerializeField] private ToggleManager togglerMusic;
    [SerializeField] private ToggleManager togglerSound;
    [SerializeField] private ToggleManager togglerVibro;

    

    public void Initialize()
    {
        return;
    }

    public void Close()
    {
        this.gameObject.SetActive(false);
    }
}
