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
    [Req] public BlockerDestroyRules BlockerDestroyRules;

    public override void Enter(FiniteStateMachine machine)
    {
        
        var bounds = machine.Field.GetBounds();
        var snapshot = machine.Field.ToSnapshot();

        //спавним бонусы если есть
        if (machine.Blackboard.CurrentBonuses != null && machine.Blackboard.CurrentBonuses.Count > 0)
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
                Snapshot = snapshot,
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

        // выталкиваем фишки из невидимых клеток вниз
        EjectFromInvisibleCells(machine, snapshot);

        // проверка Safe-блокираторов на нижнем ряду после гравитации
        ProcessSafeBlockers(machine, snapshot);

        //заполнение пустых
        var spawns = CollectionPool<List<SpawnInfo>, SpawnInfo>.Get();
        spawns.Clear();

        machine.SpawnEvaluator.Evaluate(machine, snapshot, MatchRules, spawns, machine.Blackboard.CascadeIteration);

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

        machine.Switch(StateEvent.FillUpTiles);
    }

    private void EjectFromInvisibleCells(FiniteStateMachine machine, LogicalTile?[,] snapshot)
    {
        var rows = snapshot.GetLength(0);
        var cols = snapshot.GetLength(1);

        for (var i = 0; i < rows; i++)
        {
            for (var j = 0; j < cols; j++)
            {
                var pos = new Vector2Int(i, j);
                if (!machine.Field.IsInvisibleCell(pos)) continue;
                if (snapshot[i, j] is null) continue;

                var tile = snapshot[i, j].Value;
                machine.Field.ClearTileAt(pos);
                snapshot[i, j] = null;

                for (var r = i + 1; r < rows; r++)
                {
                    var below = new Vector2Int(r, j);
                    if (snapshot[r, j] is null && !machine.Field.IsInvisibleCell(below))
                    {
                        machine.Field.SetTileAt(below, tile);
                        snapshot[r, j] = tile;
                        break;
                    }
                }
            }
        }
    }

    private void ProcessSafeBlockers(FiniteStateMachine machine, LogicalTile?[,] snapshot)
    {
        var bounds = machine.Field.GetBounds();
        machine.Blackboard.EnsureBlockerList();
        var blockersToRemove = machine.Blackboard.CurrentBlockersToRemove;
        var rows = snapshot.GetLength(0);

        for (var i = 0; i < bounds.x; i++)
        {
            for (var j = 0; j < bounds.y; j++)
            {
                var pos = new Vector2Int(i, j);
                var tile = snapshot[i, j];
                if (tile is null) continue;
                if (tile.Value.Type.KindType != TileKindType.Blocker) continue;

                var rule = BlockerDestroyRules.GetRule(tile.Value.Type.BlockerType);
                if (rule == null) continue;

                if (rule.ShouldDestroy(pos, snapshot, null))
                {
                    blockersToRemove.Add(tile.Value);
                    machine.Field.ClearTileAt(pos);
                    snapshot[i, j] = null;
                }
            }
        }
    }
}
