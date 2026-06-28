using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using YG;

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
    [SerializeField][Req] private LosePanelController LosePanel;

    [SerializeField][Req] private Levels Levels;

    [SerializeField][Req] private SettingsMenuController settings;
    [SerializeField][Req] private ConfirmExitMenuController confirm;
    [SerializeField][Req] private EndOfLevelsController endOfLevels;
    [SerializeField][Req] private SuccessNotificationController sucessNotification;


    private Field _currentFieldInstance;
    private FieldView _currentFieldViewInstance;
    private LevelSettings _currentLevelSettings;
    private WinPanelController _winPanel;
    private LosePanelController _losePanel;
    private bool _isLevelEnded;

    private SettingsMenuController _settingsMenu;
    private ConfirmExitMenuController _confirmMenu;

    private SuccessNotificationController _successNotification;
    private EndOfLevelsController _endOfLevels;

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
        var level = Levels.GetLevelSettings(YG2.saves.currentLevelId);
        StartLevel(level);
    }

    public void StartLevel(LevelSettings levelSettings)
    {
        if (levelSettings == null)
        {
            if (_endOfLevels == null)
            {
                _endOfLevels = Instantiate(endOfLevels, Canvas.transform);
            }
            else _endOfLevels.gameObject.SetActive(true);
            return;
        }

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
    }

    private void OnFieldSettled(int step)
    {
        var name = Events.GetBusName(GameEvent.FieldSettled);
        if (_currentLevelSettings.Steps < 0)
        {
            GameplayEventBus<bool>.Trigger(name, false);
            return;
        }

        bool isTargetReached = _isLevelEnded
            || step >= _currentLevelSettings.Steps
            || taskList.AreAllGoalsCompleted();


        if (!_isLevelEnded)
            fsm.Blackboard.IsWin = taskList.AreAllGoalsCompleted()
                || fsm.Blackboard.MaxSteps - fsm.Blackboard.Step > 0;

        if (_isLevelEnded) GameplayEventBus<bool>.Trigger(name, isTargetReached);
        else if (isTargetReached)
        {
            _isLevelEnded = true;

            if (_successNotification == null)
            {
                _successNotification = Instantiate(sucessNotification, Canvas.transform);
            }
            _successNotification.Initialize(isTargetReached);
            _successNotification.Show();
        }
        else
        {
            GameplayEventBus<bool>.Trigger(name, isTargetReached);
        }
    }

    private void OnFinished(bool isWin)
    {
        _isLevelEnded = true;

        if (isWin)
        {
            if (_winPanel == null)
            {
                _winPanel = Instantiate(WinPanel, Canvas.transform);
                _winPanel.transform.SetAsLastSibling();
            }
            else
            {
                _winPanel.gameObject.SetActive(true);
            }
            _winPanel.Init(this, _currentLevelSettings.backgroundSprite);
        }
        else
        {
            if (_losePanel == null)
            {
                _losePanel = Instantiate(LosePanel, Canvas.transform);
                _losePanel.transform.SetAsLastSibling();
            }
            else
            {
                _losePanel.gameObject.SetActive(true);
            }
            _losePanel.Init(this, _currentLevelSettings.backgroundSprite);
        }
    }

    public void OpenConfirmMenu()
    {
        if (_confirmMenu == null)
        {
            _confirmMenu = Instantiate(confirm, Canvas.transform);
        }
        else
        {
            _confirmMenu.gameObject.SetActive(true);
        }
    }

    public void OpenSettingsMenu()
    {
        if (_confirmMenu == null)
        {
            _settingsMenu = Instantiate(settings, Canvas.transform);
        }
        else
        {
            _settingsMenu.gameObject.SetActive(true);
        }
    }
}


