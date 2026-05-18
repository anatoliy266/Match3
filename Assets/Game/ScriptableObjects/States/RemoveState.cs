using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

[CreateAssetMenu(fileName = "RemoveState", menuName = "Scriptable Objects/RemoveState")]
public class RemoveState : FieldState
{
    public override async void Enter(FieldController field, FiniteStateMachine machine, TransitionContext context = default)
    {
        _field = field;
        _fsm = machine;

        foreach (var match in context.Matches)
        {
            _field.RemoveTiles(match.Positions);
        }
        
        await AnimateDestroy(context);

        _fsm.Switch(StateEvent.DestroyTiles, context);
    }

    private async Task AnimateDestroy(TransitionContext context)
    {
        var destroys = new List<Task>();

        foreach (var group in context.Matches)
        {
            foreach (var tile in group.Positions)
            {
                if (context.Snapshot[tile.x, tile.y]  is not null)
                {
                    var task = _field.AnimationManager.DoDestroyAsync(context.Snapshot[tile.x, tile.y].Value.Transform, 1.0f);
                    destroys.Add(task);
                }
            }
        }
        await Task.WhenAll(destroys);
    }
}
