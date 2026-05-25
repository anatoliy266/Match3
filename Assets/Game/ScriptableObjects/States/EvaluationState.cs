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
        _fsm = machine;

        //берет поле и пекредает в валидатор
        //валидатор ищет на поле есть ли группы одинакового цвета > 3 элементов
        // возвращает в руку список списков гуидов элементов по группам
        var snapshot = machine.Field.ToSnapshot();
        var groups = new List<MatchInfo>();
        machine.MatchEvaluator.Evaluate(snapshot, MatchRules, groups);

        //проверка групп на совпадение с бонусом
        var positionsCache = DictionaryPool<Guid, Vector2Int>.Get();
        machine.Field.ToPositionChache(positionsCache);

        var posGroup = CollectionPool<List<Vector2Int>, Vector2Int>.Get();
        var spawns = CollectionPool<List<SpawnInfo>, SpawnInfo>.Get();

        var posSource = positionsCache[machine.Blackboard.SourceDest.SourceId];
        var posDest = positionsCache[machine.Blackboard.SourceDest.DestId];

        for (var i = 0; i < groups.Count; i++)
        {
            posGroup.Clear();

            for (var j = 0; j < groups[i].Positions.Count; j++)
            {
                if (positionsCache.TryGetValue(groups[i].Positions[j], out var pos)) posGroup.Add(pos);
            }

            var targetSpawnPos = posGroup.Contains(posDest) ? posDest
                                  : posGroup.Contains(posSource) ? posSource
                                  : posGroup[0]; 

            machine.SpawnEvaluator.EvaluateBonusSpawn(SpawnRules, posGroup, targetSpawnPos, spawns);
        }

        machine.Blackboard.CurrentBonuses = spawns;

        CollectionPool<List<Vector2Int>, Vector2Int>.Release(posGroup);
        CollectionPool<List<SpawnInfo>, SpawnInfo>.Release(spawns);
        DictionaryPool<Guid, Vector2Int>.Release(positionsCache);
        // записывает в блекборд список групп 
        machine.Blackboard.CurrentMatches = groups;

        //переъод к удалению
        machine.Switch(StateEvent.MatchesFound);
    }
}


