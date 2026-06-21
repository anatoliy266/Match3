using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
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
    [SerializeField][Req] private FiniteStateMachine fsm;
    [SerializeField][Req] private FieldView fieldViewPrefab;


    [Header("UI Goals System")]
    [SerializeField][Req] private TaskListController taskList;
    [SerializeField][Req] private StepsController steps;

    [SerializeField][Req] private SpriteRenderer backgroundRenderer;
    [SerializeField][Req] private SpriteRenderer cellingRenderer;

    [SerializeField][Req] private TileTypeData tileTypeData;
    [SerializeField][Req] private Events Events;

    [SerializeField][Req] private Canvas Canvas;
    [SerializeField][Req] private WinPanelController WinPanel;

    [SerializeField][Req] private Levels Levels;
    [SerializeField][Req] private SessionData SessionData;

    private Field _currentFieldInstance;
    private FieldView _currentFieldViewInstance;
    private LevelSettings _currentLevelSettings;
    private WinPanelController _winPanel;
    private bool _isLevelEnded;

    private void OnEnable()
    {
        // Подписываемся на сигнал покоя поля
        var fieldSettledEvent = Events.GetBusName(GameEvent.FieldSettled);
        GameplayEventBus<int>.Register(fieldSettledEvent, OnFieldSettled);

        var finalEvent = Events.GetBusName(GameEvent.Final);
        GameplayEventBus<bool>.Register(finalEvent, OnFinished);
    }

    

    private void OnDisable()
    {
        var fieldSettledEvent = Events.GetBusName(GameEvent.FieldSettled);
        GameplayEventBus<int>.Unregister(fieldSettledEvent, OnFieldSettled);

        var finalEvent = Events.GetBusName(GameEvent.Final);
        GameplayEventBus<bool>.Unregister(finalEvent, OnFinished);
    }

    private void Start()
    {
        var level = Levels.GetLevelSettings(SessionData.currentLevelId);
        StartLevel(level);
    }

    public void StartLevel(LevelSettings levelSettings)
    {
        _isLevelEnded = false;

        _currentLevelSettings = levelSettings;

        backgroundRenderer.sprite = _currentLevelSettings.backgroundSprite;

        if (cellingRenderer != null)
        {
            cellingRenderer.size = new Vector2(_currentLevelSettings.Columns, _currentLevelSettings.Rows);
            cellingRenderer.sharedMaterial.SetVector("_GridSize", new Vector4(_currentLevelSettings.Columns, _currentLevelSettings.Rows, 0, 0));
        }

        if (_currentFieldInstance != null)
        {
            Destroy(_currentFieldInstance.gameObject);
        }
        if (_currentFieldViewInstance != null)
        {
            Destroy(_currentFieldViewInstance.gameObject);
        }

        _currentFieldInstance = Instantiate(fieldPrefab, Vector3.zero, Quaternion.identity, this.transform);
        _currentFieldInstance.Initialize(_currentLevelSettings);

        _currentFieldViewInstance = Instantiate(fieldViewPrefab, Vector3.zero, Quaternion.identity, this.transform);
        _currentFieldViewInstance.Initialize(_currentLevelSettings);

        taskList.Initialize(_currentLevelSettings);
        steps.Initialize(_currentLevelSettings);

        fsm.Init(_currentLevelSettings, _currentFieldInstance);
        fsm.Run();

        Debug.Log($"[LevelManager] Уровень {levelSettings.levelNumber} успешно запущен!");
    }

    private void OnFieldSettled(int step)
    {
        var name = Events.GetBusName(GameEvent.FieldSettled);

        bool isTargetReached = _isLevelEnded
            || step > _currentLevelSettings.Steps
            || taskList.AreAllGoalsCompleted();
        if (isTargetReached) Debug.Log("[LevelController] цели достигнуты");

        GameplayEventBus<bool>.Trigger(name, isTargetReached);
    }

    private void OnFinished(bool isWin)
    {
        _isLevelEnded = true;

        if (isWin)
        {
            Debug.Log("[LevelController] ПОБЕДА! Поле успокоилось, все анимации завершены, цели достигнуты.");

            //показать картинку с бекграунда как финал уровня на весь экран
            //и какие нибудь звездочки типа рарность картинки.
            if (_winPanel == null)
            {
                _winPanel = Instantiate(WinPanel, Canvas.transform);
                _winPanel.transform.SetAsLastSibling();
            } else
            {
                _winPanel.gameObject.SetActive(true);
            }
            _winPanel.Init(this, _currentLevelSettings.backgroundSprite);
        }
        else
        {
            Debug.Log("[LevelController] ПОРАЖЕНИЕ!");
            // порказывать какойто элемент типа "проиграл, попробуй еще раз"
        }
    }
}


