using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Pool;
using static UnityEngine.Rendering.DebugUI.Table;

[CreateAssetMenu(fileName = "Evaluatiion", menuName = "Scriptable Objects/Evaluatiion")]
public class Evaluation : FieldState
{
    
    public override void Enter(FieldController field, FiniteStateMachine machine, TransitionContext context = default)
    {
        Debug.Log("Evaluation state enter");
        _field = field;
        _fsm = machine;
        var snapshot = _field.ToSnapshot();
        //context.From, context.PositionTo, context.To, context.PositionFrom

        var foundMatches = new List<MatchInfo>();

        if (context.From.IsBonus || context.To.IsBonus)
        {
            if (context.From.IsBonus)
            {
                var group = _field.MatchEvaluator.FindAllBonuses(snapshot, context.From.GridPosition);
                if (group.Count > 0) foundMatches.AddRange(group);
            }
            if (context.To.IsBonus)
            {
                var group = _field.MatchEvaluator.FindAllBonuses(snapshot, context.To.GridPosition);
                if (group.Count > 0) foundMatches.AddRange(group);
            }
        }
        else
        {
            var group = _field.MatchEvaluator.FindAll(snapshot);
            if (group.Count > 0) foundMatches.AddRange(group);
        }

        if (foundMatches.Count == 0)
        {
            _fsm.Switch(StateEvent.NoMatches, context);
            return;
        }
        
        context.Matches = foundMatches;
        //_field.ScoreManager.CalculateScore(context.Matches, context.CascadeIteration);

        context.Snapshot = snapshot;

        context.CascadeIteration += 1;
        _fsm.Switch(StateEvent.MatchesFound, context);
    }
}


