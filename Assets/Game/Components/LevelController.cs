using System.Collections.Generic;
using UnityEngine;

public class LevelController : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private FieldController fieldPrefab;

    [Header("Level Data Storage")]
    [SerializeField] private List<LevelSettings> allLevels;

    private FieldController _currentFieldInstance;

    private void Start()
    {
        StartLevel(1);
    }

    /// <summary>
    /// Метод для запуска уровня по его номеру
    /// </summary>
    public void StartLevel(int levelNumber)
    {
        LevelSettings data = allLevels.Find(l => l.levelNumber == levelNumber);

        if (data == null)
        {
            Debug.LogError($"[LevelManager] Данные для уровня {levelNumber} не найдены!");
            return;
        }
        if (_currentFieldInstance != null)
        {
            Destroy(_currentFieldInstance.gameObject);
        }
        _currentFieldInstance = Instantiate(fieldPrefab, Vector3.zero, Quaternion.identity);
        _currentFieldInstance.Initialize(data);

        Debug.Log($"[LevelManager] Уровень {levelNumber} успешно запущен!");
    }
}

