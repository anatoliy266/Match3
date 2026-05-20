using System.Collections.Generic;
using UnityEngine;

public enum GameEvent
{
    Input,
    Animation,
    Score
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

    public string GetBusName(GameEvent id)
    {
        return events.Find(e => e.eventID == id).busName;
    }
}
