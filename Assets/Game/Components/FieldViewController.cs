using PrimeTween;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public enum AnimateAction
{
    Spawn,
    Move,
    Destroy
}

public struct AnimationBatch
{
    public LogicalTile?[,] Data;
}
public struct AnimationData
{
    public Guid Id;
    public TileKind Kind;
    public Vector2Int From;
    public Vector2Int To;
    public AnimateAction Action;
    public float Delay;
    public bool IsBonusAffected;
    public BonusType SourceBonusType;
    public Vector2Int SourceBonusPos;
}

readonly struct BonusSource
{
    public readonly Vector2Int Position;
    public readonly BonusType Type;
    public BonusSource(Vector2Int pos, BonusType type) { Position = pos; Type = type; }
}


public class FieldViewController : MonoBehaviour
{
    [Req] public FieldView View;
    [Req] public Events Events;
    [Req] public MatchRules BonusMatchRules;

    private Queue<LogicalTile?[,]> _queue = new Queue<LogicalTile?[,]>();
    private bool _isPlaying;

    private LogicalTile?[,] _prevSnapshot;


    public void Initialize(LogicalTile?[,] snapshot)
    {
        _prevSnapshot = snapshot;
        var (r, c) = (snapshot.GetLength(0), snapshot.GetLength(1));
        for (var i = 0; i < r; i++)
        {
            for (var j = 0; j < c; j++)
            {
                if (snapshot[i, j] is null) continue;
                var tile = snapshot[i, j].Value;
                View.CreateVisualTile(tile.Id, tile.Type, new Vector2Int(i, j), new Vector2Int(i, j));
            }
        }
    }


    private void OnEnable()
    {
        var name = Events.GetBusName(GameEvent.Animation);
        GameplayEventBus<LogicalTile?[,]>.Register(name, OnPackageReceived);

        var syncname = Events.GetBusName(GameEvent.AnimationSync);
        GameplayEventBus<LogicalTile?[,]>.Register(syncname, SyncSnapshot);
    }

    private void OnDisable()
    {

        var name = Events.GetBusName(GameEvent.Animation);
        GameplayEventBus<LogicalTile?[,]>.Unregister(name, OnPackageReceived);

        var syncname = Events.GetBusName(GameEvent.AnimationSync);
        GameplayEventBus<LogicalTile?[,]>.Unregister(syncname, SyncSnapshot);
    }

    private void SyncSnapshot(LogicalTile?[,] obj)
    {
        _prevSnapshot = obj;
    }

    private void OnPackageReceived(LogicalTile?[,] snapshot)
    {
        if (_prevSnapshot is null) Initialize(snapshot);
        _queue.Enqueue(snapshot);
        if (_isPlaying) return;

        PlayNext();
    }


    private void MatchField(LogicalTile?[,] snapshot, List<AnimationData> animData)
    {
        var dict = UnityEngine.Pool.DictionaryPool<Guid, Vector2Int>.Get();
        dict.Clear();
        var dictCopy = UnityEngine.Pool.DictionaryPool<Guid, Vector2Int>.Get();
        dictCopy.Clear();




        var (r, c) = (snapshot.GetLength(0), snapshot.GetLength(1));

        for (var i = 0; i < r; i++)
        {
            for (var j = 0; j < c; j++)
            {
                var item = _prevSnapshot[i, j];
                if (item is null) continue;
                var pos = new Vector2Int(i, j);
                dict[item.Value.Id] = new Vector2Int(i, j);
                dictCopy[item.Value.Id] = new Vector2Int(i, j);
            }
        }

        for (var i = 0; i < r; i++)
        {
            for (var j = 0; j < c; j++)
            {
                var item = snapshot[i, j];
                if (item is null) continue;
                var pos = new Vector2Int(i, j);

                //float randomOffset = UnityEngine.Random.Range(0f, 0.04f);
                //float waveDelay = (pos.x + pos.y) * 0.03f;
                float waveDelay = (pos.x * 0.12f) + (pos.y * 0.12f);

                // Добавляем более сильный рандом, чтобы разбить синхронность даже внутри одной линии
                float randomOffset = UnityEngine.Random.Range(0f, 0.16f);

                if (dict.TryGetValue(item.Value.Id, out var p))
                {
                    //если позиция не поменялась - скип
                    if (p == pos)
                    {
                        dictCopy.Remove(item.Value.Id);
                    }
                    else
                    {


                        //если поменялась создаем обьект
                        var anim = new AnimationData
                        {
                            Id = item.Value.Id,
                            Kind = item.Value.Type,
                            From = p,
                            To = pos,
                            Action = AnimateAction.Move,
                            //Delay = pos.x * 0.05f
                            //Delay = pos.x * 0.08f + pos.y * 0.02f
                            Delay = waveDelay + randomOffset
                        };
                        animData.Add(anim);

                        dictCopy.Remove(item.Value.Id);
                    }
                }
                else
                {
                    bool isBonus = item.Value.Type.KindType == TileKindType.Bonus;

                    // Если бонус — спавним на месте (pos), если обычная — за экраном (r+1)
                    var from = isBonus ? pos : new Vector2Int(r + 1, pos.y);

                    var data = new AnimationData
                    {
                        Id = item.Value.Id,
                        Kind = item.Value.Type,
                        From = from,
                        To = pos,
                        Action = AnimateAction.Spawn,
                        //Delay = pos.x * 0.05f
                        //Delay = pos.x * 0.08f + pos.y * 0.02f
                        Delay = waveDelay + randomOffset
                    };
                    animData.Add(data);
                }
            }
        }

        //те что остались - на удаление (идем по безопасной копии, которую не трогали в TryValue)
        var bonusSources = ListPool<BonusSource>.Get();

        foreach (var kvp in dictCopy)
        {
            var pos = kvp.Value;
            var tile = _prevSnapshot[pos.x, pos.y];

            bool isBonus = tile is not null && tile.Value.Type.KindType == TileKindType.Bonus;

            if (isBonus)
                bonusSources.Add(new BonusSource(pos, tile.Value.Type.BonusType));

            var data = new AnimationData
            {
                Id = kvp.Key,
                From = pos,
                Action = AnimateAction.Destroy,
                IsBonusAffected = isBonus,
                SourceBonusType = tile is not null ? tile.Value.Type.BonusType : default,
                SourceBonusPos = pos,
            };
            animData.Add(data);
        }

        if (bonusSources.Count > 0)
        {
            var animItems = animData;
            for (int i = 0; i < animItems.Count; i++)
            {
                var item = animItems[i];
                if (item.Action != AnimateAction.Destroy || item.IsBonusAffected)
                    continue;

                var bs = CheckIsBonusAffected(item.From, _prevSnapshot, bonusSources);
                if (bs.HasValue)
                {
                    item.IsBonusAffected = true;
                    item.SourceBonusType = bs.Value.Type;
                    item.SourceBonusPos = bs.Value.Position;
                    animItems[i] = item;
                }
            }
        }

        ListPool<BonusSource>.Release(bonusSources);

        // Возвращаем временные словари в пул
        UnityEngine.Pool.DictionaryPool<Guid, Vector2Int>.Release(dict);
        UnityEngine.Pool.DictionaryPool<Guid, Vector2Int>.Release(dictCopy);

        //return animData;
    }


    //todo: разделить както чтобы падало по 1 линии типа за раз
    private void PlayNext()
    {
        if (_queue.Count == 0)
        {
            _isPlaying = false;
            var name = Events.GetBusName(GameEvent.AnimationEnd);
            GameplayEventBus<bool>.Trigger(name, true);
            return;
        }
        _isPlaying = true;

        var snapshot = _queue.Dequeue();
        var sequence = Sequence.Create();
        var animData = ListPool<AnimationData>.Get();

        MatchField(snapshot, animData);

        for (int i = 0; i < animData.Count; i++)
        {
            var item = animData[i];

            Sequence tileSeq;
            switch (item.Action)
            {
                case AnimateAction.Spawn: tileSeq = GetSpawnSequence(item); break;
                case AnimateAction.Move: tileSeq = GetMoveSequence(item); break;
                case AnimateAction.Destroy:
                    tileSeq = item.IsBonusAffected
                        ? GetBonusDestroySequence(item, item.SourceBonusType)
                        : GetDestroySequence(item);
                    break;
                default: tileSeq = Sequence.Create(); break;
            }

            int rowIndex = Mathf.RoundToInt(item.To.x);

            float rowDelay = rowIndex * 0.05f;

            var tileTimeline = Sequence.Create();

            tileTimeline.Chain(Tween.Delay(rowDelay));

            tileTimeline.Chain(tileSeq);

            sequence.Group(tileTimeline);
        }

        ListPool<AnimationData>.Release(animData);

        sequence.OnComplete(() =>
        {
            _prevSnapshot = snapshot;

            var name = Events.GetBusName(GameEvent.ShaderDestroyTile);
            GameplayEventBus<bool>.Trigger(name, true);

            PlayNext();
        });
    }


    private BonusSource? CheckIsBonusAffected(Vector2Int tilePos, LogicalTile?[,] snapshot, List<BonusSource> sources)
    {
        for (int i = 0; i < sources.Count; i++)
        {
            var src = sources[i];
            var tileKind = TileKind.Bonus(src.Type);
            var rules = BonusMatchRules.GetRules(tileKind);
            if (rules.Count == 0) continue;

            var tile = snapshot[tilePos.x, tilePos.y];
            if (tile is null) continue;

            var sourceSnap = new TileSnapshot(src.Position, snapshot[src.Position.x, src.Position.y].Value.Type);
            var targetSnap = new TileSnapshot(tilePos, tile.Value.Type);

            for (int r = 0; r < rules.Count; r++)
            {
                if (rules[r].IsMatch(in sourceSnap, in sourceSnap, in targetSnap))
                    return src;
            }
        }
        return null;
    }

    private const float StretchY = 1.15f;
    private const float SquashY = 0.85f;

    private Sequence GetMoveSequence(AnimationData dataItem)
    {
        var target = View.GetVisualTileAt(dataItem.Id);
        if (target == null) return Sequence.Create();

        var startPos = View.GetWorldPos(dataItem.From);
        var endPos = View.GetWorldPos(dataItem.To);

        var seq = Sequence.Create();
        float duration = 0.35f + UnityEngine.Random.Range(0f, 0.2f);


        seq.Chain(Tween.Scale(target.transform, new Vector3(0.9f, 1.1f, 1f), duration, Ease.OutQuad));
        seq.Group(Tween.Position(target.transform, startPos, endPos, duration, Ease.OutQuad));

        float tileHeight = 1f;

        Vector3 squashedPos = endPos + new Vector3(0f, -(tileHeight * (1f - 0.85f) / 2f), 0f);

        // Удар о землю (сплющивание)
        seq.Chain(Tween.Scale(target.transform, new Vector3(1.15f, 0.85f, 1f), 0.15f, Ease.Linear));
        seq.Group(Tween.Position(target.transform, endPos, squashedPos, 0.15f, Ease.Linear));

        // Желейный отскок
        seq.Chain(Tween.Scale(target.transform, Vector3.one, 0.15f, Ease.Linear));
        seq.Group(Tween.Position(target.transform, squashedPos, endPos, 0.15f, Ease.Linear));


        return seq;
    }

    private Sequence GetSpawnSequence(AnimationData dataItem)
    {
        var target = View.CreateVisualTile(dataItem.Id, dataItem.Kind, dataItem.From, dataItem.To);

        if (dataItem.From == dataItem.To)
        {
            var seq = Sequence.Create();

            seq.Chain(Tween.Scale(target.transform, Vector3.zero, new Vector3(1.1f, 1.1f, 1f), 0.1f, Ease.OutBack));
            seq.Chain(Tween.Scale(target.transform, Vector3.one, 0.15f, Ease.Linear));
            return seq;
        }
        else
        {
            return GetMoveSequence(dataItem);
        }
    }

    private Sequence GetDestroySequence(AnimationData dataItem)
    {
        var target = View.GetVisualTileAt(dataItem.Id);
        if (target == null) return Sequence.Create();

        var seq = Sequence.Create();

        if (dataItem.Kind.KindType == TileKindType.Regular)
        {
            seq.ChainCallback(() =>
            {
                Debug.Log("колбек на дестрой тригернулся");
                var destroysfxname = Events.GetBusName(GameEvent.PlaySFX);
                GameplayEventBus<GameSound>.Trigger(destroysfxname, GameSound.Destroy);
            });
        } else
        {
            /// для бонусов
        }
        

        seq.Chain(Tween.Scale(target.transform, new Vector3(1.2f, 1.2f, 1f), 0.05f, Ease.OutQuad));
        seq.Chain(Tween.Scale(target.transform, Vector3.zero, 0.15f, Ease.InBack).OnComplete(() =>
        {
            View.ClearVisualTile(dataItem.Id);
        }));

        return seq;
    }

    private Sequence GetBonusDestroySequence(AnimationData dataItem, BonusType bonusType)
    {
        var target = View.GetVisualTileAt(dataItem.Id);
        if (target == null) return Sequence.Create();

        var seq = Sequence.Create();

        if (bonusType == BonusType.HorizontalBomb)
        {
            float distance = Mathf.Abs(dataItem.From.x - dataItem.SourceBonusPos.x);
            float waveDelay = distance * 1.12f;
            float randomOffset = UnityEngine.Random.Range(0f, 3.16f);
            seq.Chain(Tween.Delay(waveDelay + randomOffset));
        }

        seq.ChainCallback(() =>
        {
            Debug.Log("колбек на дестрой тригернулся");
            var destroysfxname = Events.GetBusName(GameEvent.PlaySFX);
            GameplayEventBus<GameSound>.Trigger(destroysfxname, GameSound.Destroy);
        });

        seq.Chain(Tween.Scale(target.transform, new Vector3(1.2f, 1.2f, 1f), 0.05f, Ease.OutQuad));
        seq.Chain(Tween.Scale(target.transform, Vector3.zero, 0.15f, Ease.InBack).OnComplete(() =>
        {
            View.ClearVisualTile(dataItem.Id);
        }));

        return seq;
    }
}
