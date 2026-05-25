using System.Collections.Generic;
using UnityEngine;

//Размер и форма поля(например, сетка 8х8, или поле с вырезами / «дырами»).
//Геометрия ячеек(наличие стен, заблокированных клеток, порталов).
//Пул фишек(какие цвета конфет/самоцветов разрешены на этом уровне).
//Цели уровня(набрать 1000 очков, уничтожить 20 клеток желе, опустить 3 ингредиента вниз).
//Лимиты(количество ходов или таймер).


public class LevelController : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private Field fieldPrefab;

    [Header("Level Data Storage")]
    [SerializeField] private List<LevelSettings> allLevels;

    private Field _currentFieldInstance;

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

