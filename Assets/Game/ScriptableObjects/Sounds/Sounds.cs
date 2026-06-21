using System.Collections.Generic;
using UnityEngine;

public enum GameSound
{
    Swap,
    Destroy,
    Spawn,
    DestroyBomb,
    DestroyVerticalBomb,
    DestroyHorizontalBomb
}


[CreateAssetMenu(fileName = "Sounds", menuName = "Scriptable Objects/Sounds")]
public class Sounds : ScriptableObject
{
    [System.Serializable]
    public struct SoundConfig
    {
        public GameSound soundID;
        public AudioClip audio;
    }

    [SerializeField] private List<SoundConfig> sounds = new List<SoundConfig>();

    private Dictionary<GameSound, AudioClip> _soundLookup;

    private void OnEnable()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        _soundLookup = new Dictionary<GameSound, AudioClip>();

        if (sounds == null) return;

        // Быстрый проход обычным циклом for вместо foreach (избегаем аллокации итератора)
        for (int i = 0; i < sounds.Count; i++)
        {
            var config = sounds[i];
            if (config.audio == null) continue;

            if (!_soundLookup.ContainsKey(config.soundID))
            {
                _soundLookup.Add(config.soundID, config.audio);
            }
        }
    }

    public AudioClip GetSound(GameSound id)
    {
        if (_soundLookup == null) return null;

        if (_soundLookup.TryGetValue(id, out var audioClip))
        {
            return audioClip;
        }

        return null;
    }
}
