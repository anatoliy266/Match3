using PrimeTween;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

public static class AnimationHelper
{
    public static async Task DoDestroyAsync(Transform o, float duration, CancellationToken cancellationToken = default)
    {
        await Sequence.Create(0).Group(Tween.Scale(o, 0.0f, duration)).OnComplete(() =>
        {
            if (o != null)
            {
                if (o.TryGetComponent<TileController>(out var tile))
                {
                    ObjectPool.SharedInstance.ReturnObject(tile);
                }
                else
                {
                    UnityEngine.Object.Destroy(o.gameObject);
                }
            }
        });
    }

    public static async Task DoMoveAsync(Transform o, Vector3 targetPosition, float speed, CancellationToken cancellationToken = default)
    {
        await Tween.PositionAtSpeed(o, targetPosition, speed);
        o.position = targetPosition;
    }

    public static async Task DoMoveExactTimeAsync(Transform o, Vector3 targetPosition, float duration, CancellationToken cancellationToken = default)
    {
        await Tween.Position(o, targetPosition, duration);
        o.position = targetPosition;
    }

    public static async Task DoSpawnAtPointAsync(Transform o, float duration, CancellationToken cancellationToken = default)
    {
        var start = Vector3.zero;
        var end = o.localScale;
        await Tween.Scale(o, start, end, duration);
        o.localScale = end;
    }
}
