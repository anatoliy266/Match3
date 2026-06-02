using System;
using System.Collections.Generic;
using TMPro;
using Unity.Mathematics;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UI;

//Размер и форма поля(например, сетка 8х8, или поле с вырезами / «дырами»).
//Геометрия ячеек(наличие стен, заблокированных клеток, порталов).
//Пул фишек(какие цвета конфет/самоцветов разрешены на этом уровне).
//Цели уровня(набрать 1000 очков, уничтожить 20 клеток желе, опустить 3 ингредиента вниз).
//Лимиты(количество ходов или таймер).

public class LevelController : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField][Req] private Field fieldPrefab;
    [SerializeField][Req] private FieldView fieldViewPrefab;

    [Header("UI Goals System")]
    [SerializeField][Req] private TaskListController taskList;

    [SerializeField][Req] private SpriteRenderer backgroundRenderer;
    [SerializeField][Req] private SpriteRenderer woolRenderer;
    [SerializeField][Req] private SpriteRenderer cellingRenderer;

    [SerializeField][Req] private TileTypeData tileTypeData;
    [SerializeField][Req] private Events Events;

    [SerializeField][Req] private Canvas Canvas;
    [SerializeField][Req] private GameObject WinPanel;

    [SerializeField][Req] private Levels Levels;
    [SerializeField][Req] private SessionData SessionData;

    private Field _currentFieldInstance;
    private FieldView _currentFieldViewInstance;
    private LevelSettings _currentLevelSettings;
    private bool _isLevelEnded;

    private void OnEnable()
    {
        // Подписываемся на сигнал покоя поля
        var fieldSettledEvent = Events.GetBusName(GameEvent.FieldSettled);
        GameplayEventBus<int>.Register(fieldSettledEvent, OnFieldSettled);
    }

    private void OnDisable()
    {
        var fieldSettledEvent = Events.GetBusName(GameEvent.FieldSettled);
        GameplayEventBus<int>.Unregister(fieldSettledEvent, OnFieldSettled);
    }

    private void Start()
    {
        var level = Levels.GetLevelSettings(SessionData.currentLevelId);
        StartLevel(level);
    }

    public void StartLevel(LevelSettings levelSettings)
    {
        _currentLevelSettings = levelSettings;

        backgroundRenderer.sprite = _currentLevelSettings.backgroundSprite;

        if (woolRenderer != null)
        {
            var woolCtrl = woolRenderer.GetComponent<WoolController>();
            if (woolCtrl != null)
            {
                woolCtrl.InitializeWool(_currentLevelSettings);
            }
            else
            {
                Debug.LogError("[LevelController] Компонент WoolController не найден на объекте woolRenderer!");
            }
        }

        if (cellingRenderer != null)
        {
            cellingRenderer.size = new Vector2(_currentLevelSettings.Columns, _currentLevelSettings.Rows);
            cellingRenderer.sharedMaterial.SetVector("_GridSize", new Vector4(_currentLevelSettings.Columns, _currentLevelSettings.Rows, 0, 0));
        }

        if (_currentFieldInstance != null)
        {
            Destroy(_currentFieldInstance.gameObject);
        }

        _currentFieldInstance = Instantiate(fieldPrefab, Vector3.zero, Quaternion.identity, this.transform);
        _currentFieldInstance.Initialize(_currentLevelSettings);

        _currentFieldViewInstance = Instantiate(fieldViewPrefab, Vector3.zero, Quaternion.identity, this.transform);
        _currentFieldViewInstance.Initialize(_currentLevelSettings);

        taskList.Initialize(_currentLevelSettings);

        Debug.Log($"[LevelManager] Уровень {levelSettings.levelNumber} успешно запущен!");
    }

    private void OnFieldSettled(int step)
    {
        if (_isLevelEnded) return;

        if (step > _currentLevelSettings.Steps) EndLevel(isWin: false);

        if (taskList.AreAllGoalsCompleted())
        {
            EndLevel(isWin: true);
            return;
        }
    }

    private void EndLevel(bool isWin)
    {
        _isLevelEnded = true;

        if (isWin)
        {
            Debug.Log("[LevelController] ПОБЕДА! Поле успокоилось, все анимации завершены, цели достигнуты.");

            //показать картинку с бекграунда как финал уровня на весь экран
            //и какие нибудь звездочки типа рарность картинки.
            var spawnedUI = Instantiate(WinPanel, Canvas.transform);
            spawnedUI.transform.SetAsLastSibling(); 

            //сформировать снапшот у которого заменить бонусами рандомные ячейки в зависимости от оставшихся ходов

            // 
        }
        else
        {
            Debug.Log("[LevelController] ПОРАЖЕНИЕ!");
            // порказывать какойто элемент типа "проиграл, попробуй еще раз"
        }

        


    }
}


