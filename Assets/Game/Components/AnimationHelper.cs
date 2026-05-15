using PrimeTween;
using System;
using System.Collections.Generic;
using System.Linq;
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

    //public static async Task DoDestroyGroupAsync(HashSet<Vector2Int> group, float duration, TileController[,] tiles)
    //{
    //    if (group == null || group.Count == 0) return;

    //    // 1. Считаем геометрический центр группы
    //    Vector2 center = Vector2.zero;
    //    foreach (var pos in group)
    //    {
    //        center += pos;
    //    }
    //    center /= group.Count;

    //    // 2. Группируем элементы по расстоянию до центра
    //    var layers = group
    //        .GroupBy(pos => Mathf.RoundToInt(Vector2.Distance(pos, center)))
    //        .OrderBy(g => g.Key)
    //        .ToList();

    //    const float delayBetweenLayers = 0.05f;

    //    // 3. Создаем Sequence
    //    var seq = Sequence.Create();

    //    for (int layerIndex = 0; layerIndex < layers.Count; layerIndex++)
    //    {
    //        var currentLayer = layers[layerIndex].ToList();

    //        // Если это не первый слой, добавляем задержку ПЕРЕД началом анимации этого слоя
    //        if (layerIndex > 0)
    //        {
    //            seq.ChainDelay(delayBetweenLayers);
    //        }

    //        bool isFirstInLayer = true;

    //        foreach (var pos in currentLayer)
    //        {
    //            var tile = tiles[pos.x, pos.y];
    //            if (tile == null) continue;

    //            var tileTransform = tile.transform;

    //            // Создаем твин для текущей плитки
    //            var scaleTween = Tween.Scale(tileTransform, 0.0f, duration)
    //                .OnComplete(() =>
    //                {
    //                    // Безопасная проверка: уничтожаем/возвращаем только если объект еще существует
    //                    if (tileTransform == null) return;

    //                    if (tileTransform.TryGetComponent<TileController>(out var tc))
    //                    {
    //                        ObjectPool.SharedInstance.ReturnObject(tc);
    //                    }
    //                    else
    //                    {
    //                        UnityEngine.Object.Destroy(tileTransform.gameObject);
    //                    }
    //                });

    //            // Первый твин слоя ставит временной маркер (Chain), остальные выполняются параллельно (Group)
    //            if (isFirstInLayer)
    //            {
    //                seq.Chain(scaleTween);
    //                isFirstInLayer = false;
    //            }
    //            else
    //            {
    //                seq.Group(scaleTween);
    //            }
    //        }
    //    }

    //    // 4. Ждем завершения всей последовательности
    //    await seq;
    //}

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
