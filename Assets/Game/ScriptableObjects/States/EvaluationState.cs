using Mono.Cecil.Cil;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.AI.MCP.Editor.Tools;
using UnityEngine;
using UnityEngine.Pool;
using static UnityEngine.Rendering.DebugUI.Table;

[CreateAssetMenu(fileName = "Evaluatiion", menuName = "Scriptable Objects/Evaluatiion")]
public class Evaluation : GameState
{
    [Req] public MatchRules MatchRules;
    [Req] public SpawnRules SpawnRules;

    public override void Enter(FiniteStateMachine machine)
    {
        var matches = machine.Blackboard.CurrentMatches;
        var bonuses = machine.Blackboard.CurrentBonuses;

        matches?.Clear();
        bonuses?.Clear();

        //берет поле и пекредает в валидатор
        //валидатор ищет на поле есть ли группы одинакового цвета > 3 элементов
        // возвращает в руку список списков гуидов элементов по группам
        var snapshot = machine.Field.ToSnapshot();
        //var groups = new List<MatchInfo>();
        machine.MatchEvaluator.Evaluate(snapshot, MatchRules, matches);

        //проверка групп на совпадение с бонусом
        var positionsCache = DictionaryPool<Guid, Vector2Int>.Get();
        positionsCache.Clear();
        machine.Field.ToPositionChache(positionsCache);

        var posGroup = CollectionPool<List<Vector2Int>, Vector2Int>.Get();
        posGroup.Clear();
        //var spawns = CollectionPool<List<SpawnInfo>, SpawnInfo>.Get();
        //spawns.Clear();

        //var posSource = positionsCache[machine.Blackboard.SourceDest.SourceId];
        //var posDest = positionsCache[machine.Blackboard.SourceDest.DestId];
        if (!positionsCache.TryGetValue(machine.Blackboard.SourceDest.SourceId, out var posSource)) posSource = new Vector2Int(-1, -1);
        if (!positionsCache.TryGetValue(machine.Blackboard.SourceDest.DestId, out var posDest)) posDest = new Vector2Int(-1, -1);

        for (var i = 0; i < matches.Count; i++)
        {
            posGroup.Clear();

            for (var j = 0; j < matches[i].Positions.Count; j++)
            {
                if (positionsCache.TryGetValue(matches[i].Positions[j], out var pos)) posGroup.Add(pos);
            }

            var targetSpawnPos = posGroup.Contains(posDest) ? posDest
                                  : posGroup.Contains(posSource) ? posSource
                                  : posGroup[0]; 

            machine.SpawnEvaluator.EvaluateBonusSpawn(SpawnRules, posGroup, targetSpawnPos, bonuses);
        }

        //bonuses = spawns;

        CollectionPool<List<Vector2Int>, Vector2Int>.Release(posGroup);
        DictionaryPool<Guid, Vector2Int>.Release(positionsCache);

        if (matches.Count == 0)
        {
            machine.Switch(StateEvent.NoMatches);
        } else
        {
            // записывает в блекборд список групп 
            //matches = matches;
            //переъод к удалению
            machine.Switch(StateEvent.MatchesFound);
        }

        
    }
}


