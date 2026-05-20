using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
public struct CompactInfo
{
    public TileController Tile;
    public Vector2Int TargetPos;
}


[CreateAssetMenu(fileName = "CompactState", menuName = "Scriptable Objects/CompactState")]
public class CompactState : FieldState
{
    public override void Enter(FieldController field, FiniteStateMachine machine, TransitionContext context = default)
    {
        _field = field;
        _fsm = machine;

        var compacts = _field.CompactBoard();

        context.Compacts = compacts;

        if (compacts.Count == 0)
        {
            _fsm.Switch(StateEvent.CompactTiles, context);
            return;
        }
        AnimateFalls(context);

        _fsm.Switch(StateEvent.CompactTiles, context);
    }


    private void AnimateFalls(TransitionContext context)
    {
        var fallsAnimList = new List<AnimationData>();

        foreach (var compact in context.Compacts)
        {
            var data = new AnimationData
            {
                Type = AnimationType.Move,
                Target = compact.Tile.transform,
                TargetPosition = _field.GetWorldPos(compact.TargetPos),
                Duration = 1.0f
            };
            fallsAnimList.Add(data);
        }

        var name = _fsm.Events.GetBusName(GameEvent.Animation);
        GameplayEventBus<List<AnimationData>>.Trigger(name, fallsAnimList);
    }
}
