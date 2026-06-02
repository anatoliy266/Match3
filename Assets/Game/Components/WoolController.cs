using Newtonsoft.Json;
using PrimeTween;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[RequireComponent(typeof(SpriteRenderer))]
public class WoolController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField][Req] private TileTypeData tileTypeData;
    [SerializeField][Req] private Events Events;

    // Текстура данных для шейдера
    private Texture2D _ropeDataTexture;
    private Color[] _texturePixels;
    private SpriteRenderer _spriteRenderer;
    private MaterialPropertyBlock _propBlock;

    private int _ropeCount = 100;
    private bool _isInitialized = false;
    private Queue<(LogicalTile?[,], LogicalTile?[,])> _snapDataQueue = new Queue<(LogicalTile?[,], LogicalTile?[,])>();

    private void Awake()
    {
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _propBlock = new MaterialPropertyBlock();
    }

    private void OnEnable()
    {
        var shaderBusName = Events.GetBusName(GameEvent.ShaderImpact);
        GameplayEventBus<(LogicalTile?[,], LogicalTile?[,])>.Register(shaderBusName, HandleMatchDestroyedLogical);

        var shaderDestroyTileBusName = Events.GetBusName(GameEvent.ShaderDestroyTile);
        GameplayEventBus<bool>.Register(shaderDestroyTileBusName, HandleMatchDestroyedVisual);

    }

    private void OnDisable()
    {
        var shaderBusName = Events.GetBusName(GameEvent.ShaderImpact);
        GameplayEventBus<(LogicalTile?[,], LogicalTile?[,])>.Unregister(shaderBusName, HandleMatchDestroyedLogical);

        var shaderDestroyTileBusName = Events.GetBusName(GameEvent.ShaderDestroyTile);
        GameplayEventBus<bool>.Unregister(shaderDestroyTileBusName, HandleMatchDestroyedVisual);
    }

    private void HandleMatchDestroyedVisual(bool obj)
    {
        if (!_isInitialized || _texturePixels == null) return;
        if (_snapDataQueue.Count == 0) return;
        
        var snapData = _snapDataQueue.Dequeue();
        var destroyedTiles = UnityEngine.Pool.ListPool<TileKind>.Get();
        destroyedTiles.Clear();

        GetDestroyedTileKinds(snapData, destroyedTiles);

        for (var i = 0; i < destroyedTiles.Count; i++)
        {
            var type = destroyedTiles[i];
            var color = tileTypeData.GetColor(type);

            int chosenIndex = -1;
            int candidateCount = 0;

            // 3. Первый проход: Алгоритм Резервуара (ищет ОДИН случайный индекс без аллокаций)
            for (int j = 0; j < _ropeCount; j++)
            {
                Color pixel = _texturePixels[j];
                Color ropeColor = new Color(pixel.g, pixel.b, pixel.a);
                float currentProgress = pixel.r;

                if (Vector4.Distance(ropeColor, color) < 0.1f && currentProgress < 0.05f)
                {
                    candidateCount++;
                    if (UnityEngine.Random.Range(0, candidateCount) == 0)
                    {
                        chosenIndex = j;
                    }
                }
            }

            if (chosenIndex != -1)
            {
                int targetRopeIndex = chosenIndex;
                _texturePixels[targetRopeIndex].r = 0.01f;
                Tween.Custom(
                    startValue: 0.0f,
                    endValue: 1.0f,
                    duration: 2.0f,
                    ease: Ease.OutQuad,
                    onValueChange: (target, currentProgress) =>
                    {
                        Color c = target._texturePixels[targetRopeIndex];
                        Color updatedColor = new Color(currentProgress, c.g, c.b, c.a);

                        // 1. Запоминаем новое состояние в CPU массиве
                        target._texturePixels[targetRopeIndex] = updatedColor;

                        // 2. Точечно меняем только ОДИН конкретный пиксель на GPU
                        // Предполагается, что текстура одномерная (высота = 1), поэтому Y = 0
                        target._ropeDataTexture.SetPixel(targetRopeIndex, 0, updatedColor);
                        target._ropeDataTexture.Apply();

                        // 3. Обновляем PropertyBlock конкретно этого спрайта
                        target._spriteRenderer.GetPropertyBlock(target._propBlock);
                        target._propBlock.SetTexture("_RopeDataTex", target._ropeDataTexture);
                        target._spriteRenderer.SetPropertyBlock(target._propBlock);
                    },
                    target: this
                );
            }
        }

        UnityEngine.Pool.ListPool<TileKind>.Release(destroyedTiles);
    }

    private void HandleMatchDestroyedLogical((LogicalTile?[,], LogicalTile?[,]) tuple)
    {
        _snapDataQueue.Enqueue(tuple);
    }

    public void InitializeWool(LevelSettings levelData)
    {
        if (_ropeDataTexture != null)
        {
            Destroy(_ropeDataTexture);
        }

        _spriteRenderer.GetPropertyBlock(_propBlock);

        _ropeCount = 0;
        if (levelData.ropesGoalsList != null)
        {
            foreach (var goal in levelData.ropesGoalsList)
            {
                _ropeCount += goal.count;
            }
        }
        _ropeCount = Mathf.Clamp(_ropeCount, 1, 128);

        _ropeDataTexture = new Texture2D(_ropeCount, 1, TextureFormat.RGBAFloat, false);
        _ropeDataTexture.filterMode = FilterMode.Point;
        _ropeDataTexture.wrapMode = TextureWrapMode.Clamp;

        _texturePixels = new Color[_ropeCount];

        int pixelIndex = 0;
        if (levelData.ropesGoalsList != null)
        {
            foreach (var goal in levelData.ropesGoalsList)
            {
                Color ropeColor = tileTypeData.GetColor(goal.Kind);

                for (int k = 0; k < goal.count; k++)
                {
                    if (pixelIndex >= _ropeCount) break;

                    _texturePixels[pixelIndex] = new Color(0.0f, ropeColor.r, ropeColor.g, ropeColor.b);
                    pixelIndex++;
                }
            }
        }

        for (int i = pixelIndex; i < _ropeCount; i++)
        {
            _texturePixels[i] = new Color(0.0f, 1.0f, 1.0f, 1.0f);
        }

        for (int i = _ropeCount - 1; i > 0; i--)
        {
            int rnd = UnityEngine.Random.Range(0, i + 1);
            Color temp = _texturePixels[i];
            _texturePixels[i] = _texturePixels[rnd];
            _texturePixels[rnd] = temp;
        }

        _ropeDataTexture.SetPixels(_texturePixels);
        _ropeDataTexture.Apply();

        _propBlock.SetFloat("Count", _ropeCount);
        _propBlock.SetTexture("_RopeDataTex", _ropeDataTexture);

        int uniqueColorsCount = levelData.ropesGoalsList != null ? levelData.ropesGoalsList.Count : 1;
        _propBlock.SetFloat("_ColorsCount", Mathf.Max(uniqueColorsCount, 1));

        _spriteRenderer.SetPropertyBlock(_propBlock);

        _isInitialized = true;
    }

    /// <summary>
    /// ТЕОРИЯ: Этот метод автоматически срабатывает, когда твой Match-3 движок стреляет событием уничтожения.
    /// Монобех мотни сам обрабатывает пришедшие данные, выбирает нити и запускает анимацию уезда.
    /// </summary>
    //private void HandleMatchDestroyed((LogicalTile?[,], LogicalTile?[,]) snapData)
    //{
    //    if (!_isInitialized || _texturePixels == null) return;

    //    var destroyedTiles = UnityEngine.Pool.ListPool<TileKind>.Get();
    //    destroyedTiles.Clear();

    //    GetDestroyedTileKinds(snapData, destroyedTiles);

    //    bool textureChanged = false;

    //    for (var i = 0; i < destroyedTiles.Count; i++)
    //    {
    //        var type = destroyedTiles[i];
    //        var color = tileTypeData.GetColor(type);

    //        int chosenIndex = -1;
    //        int candidateCount = 0;

    //        // 3. Первый проход: Алгоритм Резервуара (ищет ОДИН случайный индекс без аллокаций)
    //        for (int j = 0; j < _ropeCount; j++)
    //        {
    //            Color pixel = _texturePixels[j];
    //            Color ropeColor = new Color(pixel.g, pixel.b, pixel.a);
    //            float currentProgress = pixel.r;

    //            if (Vector4.Distance(ropeColor, color) < 0.1f && currentProgress < 0.05f)
    //            {
    //                candidateCount++;
    //                if (UnityEngine.Random.Range(0, candidateCount) == 0)
    //                {
    //                    chosenIndex = j;
    //                }
    //            }
    //        }

    //        if (chosenIndex != -1)
    //        {
                
    //            int targetRopeIndex = chosenIndex;
    //            _texturePixels[targetRopeIndex].r = 0.01f;
    //            Tween.Custom(
    //                startValue: 0.0f,
    //                endValue: 1.0f,
    //                duration: 2.0f,
    //                ease: Ease.OutQuad, 
    //                onValueChange: (target, currentProgress) =>
    //                {
    //                    // Каждый шаг твины мы перезаписываем Красный канал конкретного пикселя
    //                    Color c = target._texturePixels[targetRopeIndex];
    //                    target._texturePixels[targetRopeIndex] = new Color(currentProgress, c.g, c.b, c.a);

    //                    target._ropeDataTexture.SetPixels(target._texturePixels);
    //                    target._ropeDataTexture.Apply();
    //                    target._spriteRenderer.GetPropertyBlock(target._propBlock);
    //                    target._propBlock.SetTexture("_RopeDataTex", target._ropeDataTexture);
    //                    target._spriteRenderer.SetPropertyBlock(target._propBlock);
    //                },
    //                target: this
    //            );
    //        }
    //    }

    //    if (textureChanged)
    //    {
    //        _ropeDataTexture.SetPixels(_texturePixels);
    //        _ropeDataTexture.Apply();

    //        _spriteRenderer.GetPropertyBlock(_propBlock);
    //        _propBlock.SetTexture("_RopeDataTex", _ropeDataTexture);
    //        _spriteRenderer.SetPropertyBlock(_propBlock);
    //    }

    //    UnityEngine.Pool.ListPool<TileKind>.Release(destroyedTiles);
    //}

    private void GetDestroyedTileKinds((LogicalTile?[,], LogicalTile?[,]) snapData, List<TileKind> outputList)
    {
        var (prevSnapshot, snapshot) = snapData;

        if (snapshot == null || prevSnapshot == null || outputList == null) return;

        outputList.Clear();

        var dictCopy = UnityEngine.Pool.DictionaryPool<System.Guid, TileKind>.Get();
        dictCopy.Clear();

        var (r, c) = (snapshot.GetLength(0), snapshot.GetLength(1));

        for (var i = 0; i < r; i++)
        {
            for (var j = 0; j < c; j++)
            {
                var item = prevSnapshot[i, j];
                if (item is null) continue;

                dictCopy[item.Value.Id] = item.Value.Type;
            }
        }

        for (var i = 0; i < r; i++)
        {
            for (var j = 0; j < c; j++)
            {
                var item = snapshot[i, j];
                if (item is null) continue;

                dictCopy.Remove(item.Value.Id);
            }
        }

        foreach (var kvp in dictCopy)
        {
            outputList.Add(kvp.Value);
        }

        UnityEngine.Pool.DictionaryPool<System.Guid, TileKind>.Release(dictCopy);
    }
}
