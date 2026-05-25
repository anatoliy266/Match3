using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

[CreateAssetMenu(fileName = "FillUpState", menuName = "Scriptable Objects/FillUpState")]
public class FillUpState : GameState
{
    [Req] public Events Events;
    [Req] public SpawnRules SpawnRules;
    [Req] public MatchRules MatchRules;

    public override void Enter(FiniteStateMachine machine)
    {
        
        var bounds = machine.Field.GetBounds();
        var snapshot = machine.Field.ToSnapshot();

        //спавним бонусы если есть
        if (machine.Blackboard.CurrentBonuses.Count > 0)
        {
            var bonuses = machine.Blackboard.CurrentBonuses;
            for (var i = 0; i < bonuses.Count; i++)
            {
                var tile = new LogicalTile
                {
                    Id = machine.Field.GenerateUniqueId(),
                    Type = bonuses[i].Type,
                };
                machine.Field.SetTileAt(bonuses[i].Position, tile);
                snapshot[bonuses[i].Position.x, bonuses[i].Position.y] = tile;
            }
        }
        


        //считаем какие плитки упадут на какие места
        var transitions = CollectionPool<List<TileTransitionData>, TileTransitionData>.Get();
        transitions.Clear();
        for (var j = 0; j < bounds.y; j++)
        {
            var ctx = new AlgoritmContext
            {
                Field = snapshot,
            };
            TwoPointers.Run(new Vector2Int(0, j), ctx, transitions);
        }

        for (var i = 0; i < transitions.Count; i++)
        {
            var transition = transitions[i];

            //var from = machine.Field.GetTileAt(transition.From);
            //var to = machine.Field.GetTileAt(transition.To);
            var from = snapshot[transition.From.x, transition.From.y];
            var to = snapshot[transition.To.x, transition.To.y];

            machine.Field.SetTileAt(transition.From, to);
            machine.Field.SetTileAt(transition.To, from);

            snapshot[transition.From.x, transition.From.y] = to;
            snapshot[transition.To.x, transition.To.y] = from;
        }
        CollectionPool<List<TileTransitionData>, TileTransitionData>.Release(transitions);

        //заполнение пустых
        var spawns = CollectionPool<List<SpawnInfo>, SpawnInfo>.Get();
        machine.SpawnEvaluator.Evaluate(snapshot, SpawnRules, spawns);

        for (var i = 0; i < spawns.Count; i++)
        {
            //подумать будет ли случай, когда на позиции бонуса будет не нулл и что с этим делать

            var tile = new LogicalTile
            {
                Id = machine.Field.GenerateUniqueId(),
                Type = spawns[i].Type,
            };
            machine.Field.SetTileAt(spawns[i].Position, tile);

            snapshot[spawns[i].Position.x, spawns[i].Position.y] = tile;
        }
        CollectionPool<List<SpawnInfo>, SpawnInfo>.Release(spawns);

        var name = Events.GetBusName(GameEvent.Animation);
        GameplayEventBus<LogicalTile?[,]>.Trigger(name, snapshot);

        _fsm.Switch(StateEvent.FillUpTiles);
    }
}
