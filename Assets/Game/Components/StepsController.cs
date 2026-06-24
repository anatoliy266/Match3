using System;
using Unity.VisualScripting;
using UnityEngine;

public class StepsController : MonoBehaviour
{
    [SerializeField][Req] private Events Events;
    [SerializeField][Req] private StepsUnitController StepsPrefab;

    private StepsUnitController _currentUnit;

    
    private void OnEnable()
    {
        var stepName = Events.GetBusName(GameEvent.StepIncreased);
        GameplayEventBus<int>.Register(stepName, OnStepChanged);
    }

    private void OnDisable()
    {
        var stepName = Events.GetBusName(GameEvent.StepIncreased);
        GameplayEventBus<int>.Unregister(stepName, OnStepChanged);
    }
    
    public void Initialize(LevelSettings settings)
    {
        if (settings == null || settings.ropesGoalsList == null) return;
        if (_currentUnit != null)
        {
            Destroy(_currentUnit.gameObject);
        }
        _currentUnit = Instantiate(StepsPrefab, this.transform);
        _currentUnit.Initialize(settings.Steps);
    }

    private void OnStepChanged(int step)
    {
        Debug.Log("пришел ивент что степ++");
        if (_currentUnit != null)
        {
            _currentUnit.UpdateSteps(step);
        }
    }
}
