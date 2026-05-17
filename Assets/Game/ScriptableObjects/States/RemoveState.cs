using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

[CreateAssetMenu(fileName = "NewScriptableObjectScript", menuName = "Scriptable Objects/NewScriptableObjectScript")]
public class NewScriptableObjectScript : FieldState
{
    public override async void Enter(FieldController field, FiniteStateMachine machine, TransitionContext context = default)
    {
        _field = field;
        _fsm = machine;


        _field.RemoveTiles(context.Matches);
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
