using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

[CreateAssetMenu(fileName = "LoadingState", menuName = "Scriptable Objects/LoadingState")]
public class LoadingState : FieldState
{

    //Размер и форма поля(например, сетка 8х8, или поле с вырезами / «дырами»).
    //Геометрия ячеек(наличие стен, заблокированных клеток, порталов).
    //Пул фишек(какие цвета конфет/самоцветов разрешены на этом уровне).
    //Цели уровня(набрать 1000 очков, уничтожить 20 клеток желе, опустить 3 ингредиента вниз).
    //Лимиты(количество ходов или таймер).
    public async override void Enter(FieldController field, FiniteStateMachine machine, TransitionContext context = default)
    {
        _field = field;
        _fsm = machine;

        _field.DragManager.ClickingAction.action.Disable();
        _field.DragManager.TrackingAction.action.Disable();

        _field.FillBoard();

        await AnimateLoading();

        _fsm.Switch(StateEvent.FinishLoading, context);
    }

    private async Task AnimateLoading()
    {
        var loadings = new List<Task>();

        for (var i = 0; i < _field.Rows; i++)
        {
            for (var j = 0; j < _field.Cols; j++)
            {
                var tile = _field.GetTileAt(new Vector2Int(i, j));
                if (tile == null) continue;

                Vector3 targetWorldPosition = _field.GetWorldPos(tile.GridPosition);

                var task = _field.AnimationManager.DoMoveExactTimeAsync(tile.transform, targetWorldPosition, 1.0f);
                loadings.Add(task);
            }
        }

        await Task.WhenAll(loadings);
    }
}
