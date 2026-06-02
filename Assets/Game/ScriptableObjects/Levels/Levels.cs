using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class LevelMapping
{
    [Tooltip("номер уровня")]
    public int levelId;

    [Tooltip("Настройки уровня")]
    public LevelSettings settings;
}

[CreateAssetMenu(fileName = "Levels", menuName = "Scriptable Objects/Levels")]
public class Levels : ScriptableObject
{
    [SerializeField]
    private List<LevelMapping> levelsMap;

    private Dictionary<int, LevelSettings> _settings;

    public int LevelsCount => levelsMap.Count;

    private void OnEnable()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        _settings = new Dictionary<int, LevelSettings>();
        if (levelsMap is not null)
        {
            foreach (var mapping in levelsMap)
            {
                if (mapping.settings is not null)
                {
                    // Используем индексатор, чтобы безопасно перезаписать данные, если тип продублирован в инспекторе
                    _settings[mapping.levelId] = mapping.settings;
                }
            }
        }
    }

    public LevelSettings GetLevelSettings(int id)
    {
        return _settings[id];
    }
}
