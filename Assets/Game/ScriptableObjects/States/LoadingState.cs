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
    public override void Enter(FieldController field, FiniteStateMachine machine, TransitionContext context = default)
    {
        _field = field;
        _fsm = machine;

        var name = _fsm.Events.GetBusName(GameEvent.Input);
        GameplayEventBus<bool>.Trigger(name, false);

        _field.FillEmptyTilesOnGrid(context.CascadeIteration);

        AnimateLoading();

        _fsm.Switch(StateEvent.FinishLoading, context);
    }

    private void AnimateLoading()
    {
        var animationDataList = new List<AnimationData>();
        var bounds = _field.GetBounds();

        for (var i = 0; i < bounds.x; i++)
        {
            for (var j = 0; j < bounds.y; j++)
            {
                var gridPos = new Vector2Int(i, j);
                var tile = _field.GetTileAt(gridPos);
                var targetWorldPosition = _field.GetWorldPos(tile.GridPosition);
                //todo: var duration - тоже както надо рассчитывать, по разнице положений на поле, хз.

                var data = new AnimationData
                {
                    Type = AnimationType.Move,
                    Target = tile.transform,
                    TargetPosition = targetWorldPosition,
                    Duration = 1.0f
                };
                animationDataList.Add(data);
            }
        }
        var name = _fsm.Events.GetBusName(GameEvent.Animation);
        GameplayEventBus<List<AnimationData>>.Trigger(name, animationDataList);
    }
}
