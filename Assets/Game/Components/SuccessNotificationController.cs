using PrimeTween;
using System;
using UnityEngine;

public class SuccessNotificationController : MonoBehaviour
{

    [SerializeField] private float duration = 3.0f;

    [SerializeField] private Events Events;

    private bool IsTargetReached = true;
    public void Show()
    {
        this.gameObject.SetActive(true);

        Tween.Delay(duration).OnComplete(OnTimeout);
    }

    internal void Initialize(bool isTargetReached)
    {
        IsTargetReached = isTargetReached;

    }

    private void OnTimeout()
    {
        var name = Events.GetBusName(GameEvent.FieldSettled);
        GameplayEventBus<bool>.Trigger(name, IsTargetReached);

        this.gameObject.SetActive(false);
    }
}
