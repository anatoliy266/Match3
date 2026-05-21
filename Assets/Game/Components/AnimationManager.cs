using PrimeTween;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Pool;
using UnityEngine.Rendering;
using static UnityEditor.PlayerSettings;
using static UnityEngine.GraphicsBuffer;


public enum AnimationType
{
    Spawn,
    SpawnAtPoint,
    Move,
    Destroy,
    Wiggle
}

public struct AnimationData
{
    public AnimationType Type;
    public Transform Target;
    public Vector3 TargetPosition;
    public float Duration;
}
public  class AnimationManager : MonoBehaviour
{
    public float AnimDuration;
    public float AnimSpeed;

    [Req] public Events Events;

    private Queue<List<AnimationData>> _queue = new Queue<List<AnimationData>>();
    private Sequence _currentSequence;

    private void Awake()
    {
        var evname = Events.GetBusName(GameEvent.Animation);
        GameplayEventBus<List<AnimationData>>.Register(evname, OnAnimationRequested);
    }

    private void OnAnimationRequested(List<AnimationData> data)
    {
        _queue.Enqueue(data);
        if (!_currentSequence.isAlive)
        {
            PlayNext();
        }
    }

    private void PlayNext()
    {
        if (_queue.Count == 0) return;
        var data = _queue.Dequeue();
        _currentSequence = Sequence.Create();

        foreach (var animation in data)
        {
            if (!animation.Target.gameObject.activeSelf) { continue; }
            switch (animation.Type)
            {
                case AnimationType.Spawn:
                    break;
                case AnimationType.SpawnAtPoint:
                    var spawnAtTween = Tween.Scale(animation.Target, Vector3.zero, animation.Target.localScale, animation.Duration);
                    _currentSequence.Group(spawnAtTween);
                    break;
                case AnimationType.Destroy:
                    var destroyTween = Tween.Scale(animation.Target, 0.0f, animation.Duration).OnComplete(() => {
                        var tile = animation.Target.GetComponent<TileController>();
                        ObjectPool.SharedInstance.ReturnObject(tile);
                    });
                    _currentSequence.Group(destroyTween);
                    break;

                case AnimationType.Move:
                    var moveTween = Tween.Position(animation.Target, animation.TargetPosition, animation.Duration);
                    _currentSequence.Group(moveTween);
                    break;

                case AnimationType.Wiggle:
                    break;
            }
        }
        _currentSequence.OnComplete(this, target => target.PlayNext());
    }



    private void OnDestroy()
    {
        var evname = Events.GetBusName(GameEvent.Animation);
        GameplayEventBus<List<AnimationData>>.Unregister(evname, OnAnimationRequested);
    }
}
