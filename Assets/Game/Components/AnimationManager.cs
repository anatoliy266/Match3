using PrimeTween;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using static UnityEngine.GraphicsBuffer;

public  class AnimationManager : MonoBehaviour
{
    public float AnimDuration;
    public float AnimSpeed;
    public async Task DoDestroyAsync(Transform o, float duration, CancellationToken cancellationToken = default)
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

    
    public async Task DoMoveAsync(Transform o, Vector3 targetPosition, float speed = 1.0f, CancellationToken cancellationToken = default)
    {
        await Tween.PositionAtSpeed(o, targetPosition, AnimSpeed);
        o.position = targetPosition;
    }

    public async Task DoMoveExactTimeAsync(Transform o, Vector3 targetPosition, float duration = 1.0f, CancellationToken cancellationToken = default)
    {
        await Tween.Position(o, targetPosition, AnimDuration);
        o.position = targetPosition;
    }

    public async Task DoSpawnAtPointAsync(Transform o, float duration = 1.0f, CancellationToken cancellationToken = default)
    {
        var start = Vector3.zero;
        var end = o.localScale;
        await Tween.Scale(o, start, end, AnimDuration);
        o.localScale = end;
    }

    //public async Task DoHintWiggleAsync(Transform transform1, Transform transform2)
    //{
    //    Vector3 startPos1 = transform1.position;
    //    Vector3 startPos2 = transform2.position;
    //    Vector3 midPoint = (startPos1 + startPos2) / 2f;

    //    // В PrimeTween можно склеивать твины в цепочки (Sequence) и делать им ToTask()
    //    var sequence = Sequence.Create()
    //        // Движение навстречу
    //        .Group(Tween.Position(transform1, Vector3.Lerp(startPos1, midPoint, 0.4f), 0.15f, Ease.OutQuad))
    //        .Group(Tween.Position(transform2, Vector3.Lerp(startPos2, midPoint, 0.4f), 0.15f, Ease.OutQuad))
    //        // Возврат назад
    //        .Chain(Tween.Position(transform1, startPos1, 0.15f, Ease.InQuad))
    //        .Group(Tween.Position(transform2, startPos2, 0.15f, Ease.InQuad));

    //    // Дожидаемся окончания всей цепочки анимации
    //    await sequence;
    //}

    public async Task DoHintWiggleAsync(Transform transform1, Transform transform2)
    {
        Vector3 startPos1 = transform1.position;
        Vector3 startPos2 = transform2.position;
        Vector3 midPoint = (startPos1 + startPos2) / 2f;

        // Делим общую длительность на 2 фазы (туда и обратно)
        float stepDuration = AnimDuration / 2f;

        var sequence = Sequence.Create()
            // Движение навстречу
            .Group(Tween.Position(transform1, Vector3.Lerp(startPos1, midPoint, 0.4f), stepDuration, Ease.OutQuad))
            .Group(Tween.Position(transform2, Vector3.Lerp(startPos2, midPoint, 0.4f), stepDuration, Ease.OutQuad))
            // Возврат назад
            .Chain(Tween.Position(transform1, startPos1, stepDuration, Ease.InQuad))
            .Group(Tween.Position(transform2, startPos2, stepDuration, Ease.InQuad));

        // Управляем скоростью всей цепочки через глобальный множитель
        sequence.timeScale = AnimSpeed;

        // Дожидаемся окончания всей цепочки анимации
        await sequence;
    }
}
