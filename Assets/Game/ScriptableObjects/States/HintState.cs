//using System.Threading.Tasks;
//using UnityEngine;
//using static UnityEngine.Rendering.DebugUI.Table;

//public struct HintInfo
//{
//    public Tile Tile1;
//    public Tile Tile2;
//}

//[CreateAssetMenu(fileName = "HintState", menuName = "Scriptable Objects/HintState")]
//public class HintState : GameState
//{
//    public override void Enter(Field field, FiniteStateMachine machine, TransitionContext context)
//    {
//        _field = field;
//        _fsm = machine;

//        var snapshot = _field.ToSnapshot();

//        var coords = _field.MatchEvaluator.FindAvailableHint(snapshot);

//        if (coords == null)
//        {
//            _fsm.Switch(StateEvent.NoMovesLeft, context); 
//            return;
//        }

//        context.CurrentHint = new HintInfo
//        {
//            Tile1 = _field.GetTileAt(coords.Value.pos1),
//            Tile2 = _field.GetTileAt(coords.Value.pos2)
//        };

//        AnimateHint(context);

//        _fsm.Switch(StateEvent.HasMoves, context);
//    }

//    private void AnimateHint(TransitionContext context)
//    {
//        var hint = context.CurrentHint;
//        if (hint.Tile1 == null || hint.Tile2 == null) return;

//        // Чистая таска анимации покачивания на экране
//        //await _field.AnimationManager.DoHintWiggleAsync(hint.Tile1.transform, hint.Tile2.transform);
//    }
//}
