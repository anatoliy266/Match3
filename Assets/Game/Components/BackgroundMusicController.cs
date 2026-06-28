using UnityEngine;

public class BackgroundMusicController : MonoBehaviour
{
    [SerializeField] private Events Events;

    [SerializeField] private AudioSource source;
    
    private void OnEnable()
    {
        if (source == null)
            source = GetComponent<AudioSource>();

        SetMusicProperty();

        var name = Events.GetBusName(GameEvent.SettingChanged);
        GameplayEventBus<bool>.Register(name, OnSettingsChanged);
    }

    private void OnDisable()
    {
        var name = Events.GetBusName(GameEvent.SettingChanged);
        GameplayEventBus<bool>.Unregister(name, OnSettingsChanged);
    }

    private void OnSettingsChanged(bool obj)
    {
        SetMusicProperty();
    }

    private void SetMusicProperty()
    {
        if (source == null) return;



        bool isMusicOn = PlayerPrefs.GetInt("Music", 1) == 1;
        source.mute = !isMusicOn;
    }
}
