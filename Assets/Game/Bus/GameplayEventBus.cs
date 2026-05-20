using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameplayEventBus<T>
{
    private static readonly Dictionary<string, Action<T>> Events = new();

    public static void Register(string eventName, Action<T> listener)
    {
        if (!Events.TryAdd(eventName, listener))
        {
            Events[eventName] += listener;
        }
    }

    public static void Unregister(string eventName, Action<T> listener)
    {
        if (Events.ContainsKey(eventName))
        {
            Events[eventName] -= listener;
            if (Events[eventName] == null)
            {
                Events.Remove(eventName);
            }
        }
    }

    public static void Trigger(string eventName, T data)
    {
        if (Events.TryGetValue(eventName, out var thisEvent))
        {
            thisEvent?.Invoke(data);
        }
    }
}