using PrimeTween;
using System;
using System.Collections.Generic;
using UnityEngine;
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
}


public class AnimationManager : MonoBehaviour
{
    [Req] public FieldView View;
    [Req] public Events Events;

    private Queue<LogicalTile?[,]> _queue = new Queue<LogicalTile?[,]>();
    private bool _isPlaying;

    private LogicalTile?[,] _prevSnapshot;
    

    public void Initialize(LogicalTile?[,] snapshot) => _prevSnapshot = snapshot;


    private void OnEnable()
    {
        var name = Events.GetBusName(GameEvent.Animation);
        GameplayEventBus<LogicalTile?[,]>.Register(name, OnPackageReceived);
    }

    private void OnDisable()
    {
        var name = Events.GetBusName(GameEvent.Animation);
        GameplayEventBus<LogicalTile?[,]>.Unregister(name, OnPackageReceived);
    }

    private void OnPackageReceived(LogicalTile?[,] field)
    {
        _queue.Enqueue(field);
        if (_isPlaying) return;

        PlayNext();
    }

    private List<AnimationData> MatchField(LogicalTile?[,] snapshot)
    {
        var animData = new List<AnimationData>();

        var dict = new Dictionary<Guid, Vector2Int>();
        var (r, c) = (snapshot.GetLength(0), snapshot.GetLength(1));

        for (var i = 0; i < r; i++)
        {
            for (var j = 0; j < c; j++)
            {
                var item = _prevSnapshot[i, j];
                if (item is null) continue;
                var pos = new Vector2Int(i, j);
                dict[item.Value.Id] = new Vector2Int(i, j);
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
                    if (p == pos) dict.Remove(item.Value.Id);
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
                        };
                        animData.Add(anim);

                        dict.Remove(item.Value.Id);
                    }
                }
                else
                {
                    var from = new Vector2Int(r+1, pos.y);
                    //спавн новых, те что были в новом снимке и отсутствовали в старом
                    var data = new AnimationData
                    {
                        Id = item.Value.Id,
                        Kind = item.Value.Type,
                        From = from,
                        To = pos,
                        Action = AnimateAction.Spawn
                    };
                    animData.Add(data);
                }
            }
        }
        //те что остались - на удаление
        foreach (var kvp in dict)
        {
            var data = new AnimationData
            {
                Id = kvp.Key,
                From = kvp.Value,
                Action = AnimateAction.Destroy,
            };
            animData.Add(data);
        }

        return animData;
    }

    //todo: разделить както чтобы падало по 1 линии типа за раз
    private void PlayNext()
    {
        if (_queue.Count == 0)
        {
            _isPlaying = false;
            var name = Events.GetBusName(GameEvent.Animation);
            GameplayEventBus<bool>.Trigger(name, true);
            return;
        }
        _isPlaying = true;

        var snapshot = _queue.Dequeue();
        var sequence = Sequence.Create();

        var data = MatchField(snapshot);


        foreach (var item in data)
        {
            switch (item.Action)
            {
                case AnimateAction.Spawn:
                    sequence.Chain(Spawn(item));
                    break;
                case AnimateAction.Move:
                    sequence.Chain(Move(item));
                    break;
                case AnimateAction.Destroy:
                    sequence.Chain(Destroy(item));
                    break;
            }
        }

        
        sequence.OnComplete(() => PlayNext());
    }

    private Tween Move(AnimationData dataItem)
    {
        return Tween.Position(View.GetVisualTileAt(dataItem.Id).transform, View.GetWorldPos(dataItem.From), View.GetWorldPos(dataItem.To), 1.0f);
    }

    private Tween Spawn(AnimationData dataItem)
    {
        var target = View.CreateVisualTile(dataItem.Id, dataItem.Kind, dataItem.From);
        return Tween.Scale(target.transform, 0.0f, 1.0f, 1.0f);
    }

    private Tween Destroy(AnimationData dataItem)
    {
        var target = View.GetVisualTileAt(dataItem.Id);

        return Tween.Scale(target.transform, 1.0f, 0.0f, 1.0f).OnComplete(() =>
        {
            View.ClearVisualTile(dataItem.Id);
        });
    }
}
