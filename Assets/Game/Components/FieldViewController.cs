using PrimeTween;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Timeline;

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
}


public class FieldViewController : MonoBehaviour
{
    [Req] public FieldView View;
    [Req] public Events Events;

    private Queue<LogicalTile?[,]> _queue = new Queue<LogicalTile?[,]>();
    private bool _isPlaying;

    private LogicalTile?[,] _prevSnapshot;
    

    public void Initialize(LogicalTile?[,] snapshot)
    {
        _prevSnapshot = snapshot;
        var (r, c) = (snapshot.GetLength(0), snapshot.GetLength(1));
        for (var i = 0; i < r; i++)
        {
            for (var  j = 0; j < c; j++)
            {
                if (snapshot[i, j] is null) continue;
                var tile = snapshot[i,j].Value;
                View.CreateVisualTile(tile.Id, tile.Type, new Vector2Int(i,j), new Vector2Int(i, j));
            }
        }
    }


    private void OnEnable()
    {
        var name = Events.GetBusName(GameEvent.AnimationEnd);
        GameplayEventBus<LogicalTile?[,]>.Register(name, OnPackageReceived);
    }

    private void OnDisable()
    {
        
        var name = Events.GetBusName(GameEvent.AnimationEnd);
        GameplayEventBus<LogicalTile?[,]>.Unregister(name, OnPackageReceived);
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
                            Delay = pos.x * 0.05f
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
                        Delay = pos.x * 0.05f
                    };
                    animData.Add(data);
                }
            }
        }

        //те что остались - на удаление (идем по безопасной копии, которую не трогали в TryValue)
        foreach (var kvp in dictCopy)
        {
            var data = new AnimationData
            {
                Id = kvp.Key,
                From = kvp.Value,
                Action = AnimateAction.Destroy,
            };
            animData.Add(data);
        }

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
        //animData.Clear();
        MatchField(snapshot, animData);

        foreach (var item in animData)
        {
            Tween tween = Tween.Delay(1.0f); 

            switch (item.Action)
            {
                case AnimateAction.Spawn:
                    tween = Spawn(item);
                    break;

                case AnimateAction.Move:
                    tween = Move(item);
                    break;

                case AnimateAction.Destroy:
                    tween = Destroy(item);
                    break;
            }
            sequence.Group(tween);
        }
        ListPool<AnimationData>.Release(animData);
        sequence.OnComplete(() => {
            _prevSnapshot = snapshot;

            var name = Events.GetBusName(GameEvent.ShaderDestroyTile);
            GameplayEventBus<bool>.Trigger(name, true);
            
            PlayNext();
        });
    }

    private Tween Move(AnimationData dataItem)
    {
        //return Tween.Position(View.GetVisualTileAt(dataItem.Id).transform, View.GetWorldPos(dataItem.From), View.GetWorldPos(dataItem.To), 1.0f);
        var target = View.GetVisualTileAt(dataItem.Id);

        // Защита: если плитка уже уничтожена на каскаде, просто пропускаем
        if (target == null)
        {
            return Tween.Delay(0f);
        }

        return Tween.Position(target.transform, View.GetWorldPos(dataItem.From), View.GetWorldPos(dataItem.To), 1.0f, startDelay: dataItem.Delay);
    }

    private Tween Spawn(AnimationData dataItem)
    {
        var target = View.CreateVisualTile(dataItem.Id, dataItem.Kind, dataItem.From, dataItem.To);
        if (dataItem.From == dataItem.To)
        {
            return Tween.Scale(target.transform, 0.0f, 1.0f, 1.0f, startDelay: dataItem.Delay);
        }
        else
        {
            return Tween.Position(target.transform, View.GetWorldPos(dataItem.From), View.GetWorldPos(dataItem.To), 1.0f, startDelay: dataItem.Delay);
        }

    }

    private Tween Destroy(AnimationData dataItem)
    {
        var target = View.GetVisualTileAt(dataItem.Id);

        if (target == null)
        {
            return Tween.Delay(0f);
        }

        return Tween.Scale(target.transform, 1.0f, 0.0f, 1.0f, startDelay: dataItem.Delay).OnComplete(() =>
        {
            View.ClearVisualTile(dataItem.Id);
        });
    }
}
