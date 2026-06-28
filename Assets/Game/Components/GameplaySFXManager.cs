using UnityEngine;

public class GameplaySFXManager : MonoBehaviour
{
    [SerializeField][Req] private Events Events;
    [SerializeField][Req] private Sounds Sounds;

    [SerializeField][Req] private AudioSource audioSource;


    private void OnEnable()
    {
        var name = Events.GetBusName(GameEvent.PlaySFX);
        GameplayEventBus<GameSound>.Register(name, OnSFX);
    }



    private void OnDisable()
    {
        var name = Events.GetBusName(GameEvent.PlaySFX);
        GameplayEventBus<GameSound>.Unregister(name, OnSFX);
    }

    public void Initialize()
    {

    }

    private void OnSFX(GameSound obj)
    {

        var sound = Sounds.GetSound(obj);
        if (sound != null)
            audioSource.PlayOneShot(sound);

    }
}
