using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

[CreateAssetMenu(fileName = "FillState", menuName = "Scriptable Objects/FillState")]
public class FillState : FieldState
{
    public override async void Enter(FieldController field, FiniteStateMachine machine, TransitionContext context = default)
    {
        _field = field;
        _fsm = machine;

        _field.FillEmptyTilesOnGrid();

        await AnimateFillEmpty();

        _fsm.Switch(StateEvent.FillEmptyTiles, context);
    }

    private async Task AnimateFillEmpty()
    {
        var fallTasks = new List<Task>();

        for (var i = 0; i < _field.Rows; i++)
        {
            for (var j = 0; j < _field.Cols; j++)
            {
                var tile = _field.GetTileAt(new Vector2Int(i, j));
                if (tile == null) continue;

                Vector3 targetWorldPosition = _field.GetWorldPos(tile.GridPosition);

                if (tile.transform.position != targetWorldPosition)
                {
                    var task = _field.AnimationManager.DoMoveExactTimeAsync(tile.transform, targetWorldPosition, 1.0f);
                    fallTasks.Add(task);
                }
            }
        }

        await Task.WhenAll(fallTasks);
    }
}
