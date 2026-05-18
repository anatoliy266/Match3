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
    public override async void Enter(FieldController field, FiniteStateMachine machine, TransitionContext context = default)
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
        await AnimateFalls(context);

        _fsm.Switch(StateEvent.CompactTiles, context);
    }


    private async Task AnimateFalls(TransitionContext context)
    {
        var falls = new List<Task>();

        foreach (var compact in context.Compacts)
        {
            Vector3 worldTargetPos = _field.GetWorldPos(compact.TargetPos);

            var task = _field.AnimationManager.DoMoveExactTimeAsync(compact.Tile.transform, worldTargetPos);
            falls.Add(task);
        }

        await Task.WhenAll(falls);
    }
}
