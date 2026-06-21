using System.Collections.Generic;
using UnityEngine;

public enum GameEvent
{
    Input,
    Animation,
    AnimationEnd,
    Score,
    ShaderImpact,
    ShaderDestroyTile,
    FieldSettled,
    Final,
    StepIncreased,
    AnimationSync,
    PlaySwapSFX,
    PlayDestroyRegularSFX,
    PlaySFX,
    SettingChanged
}


[CreateAssetMenu(fileName = "Events", menuName = "EventBus/Mapper")]
public class Events : ScriptableObject
{
    [System.Serializable]
    public struct EventConfig
    {
        public GameEvent eventID; 
        public string busName; 
    }

    [SerializeField] private List<EventConfig> events = new List<EventConfig>();

    private Dictionary<GameEvent, string> _eventLookup;

    private void OnEnable()
    {
        BuildLookup();
    }

    private void BuildLookup()
    {
        _eventLookup = new Dictionary<GameEvent, string>();

        if (events == null) return;

        for (int i = 0; i < events.Count; i++)
        {
            EventConfig config = events[i];

            if (string.IsNullOrEmpty(config.busName)) continue;

            if (!_eventLookup.ContainsKey(config.eventID))
            {
                _eventLookup.Add(config.eventID, config.busName);
            }
        }
    }

    public string GetBusName(GameEvent id)
    {
        if (_eventLookup == null) return string.Empty;

        if (_eventLookup.TryGetValue(id, out var busName))
        {
            return busName;
        }

        return string.Empty;
    }
}

