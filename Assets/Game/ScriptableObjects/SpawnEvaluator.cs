using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public struct SpawnInfo
{
    public TileKind Type;
    public Vector2Int Position;
}


public class SpawnEvaluator
{
    //выбор обычной фишки
    //надо както предиктивно считать какой тип выбрать чтобы не было бесконечных совпадений и доска "усложнялась" после каждой итерации
    public void Evaluate(FiniteStateMachine machine, LogicalTile?[,] snapshot, MatchRules rules, List<SpawnInfo> spawns, int difficultModified)
    {
        var (r, c) = (snapshot.GetLength(0), snapshot.GetLength(1));

        var values = System.Enum.GetValues(typeof(RegularType));

        for (var i = 0; i < r; i++)
        {
            for (var j = 0; j < c; j++)
            {
                if (snapshot[i, j] is not null) continue;

                var group = ListPool<Guid>.Get();

                var totalWeight = 0.0f;
                var choosen = 0;

                for (var val = 0; val < values.Length; val++)
                {
                    var type = (RegularType)val;
                    group.Clear();

                    machine.MatchEvaluator.EvaluateTile(snapshot, new Vector2Int(i, j), TileKind.Regular(type), rules, group);

                    var currentweight = group.Count > 2 ? 1.0f / (1.0f + difficultModified) : 1.0f;
                    totalWeight += currentweight;

                    if (UnityEngine.Random.Range(0, totalWeight) <= currentweight) choosen = val;
                }

                ListPool<Guid>.Release(group);

                var finalType = (RegularType)choosen;

                var spawn = new SpawnInfo
                {
                    Type = TileKind.Regular(finalType),
                    Position = new Vector2Int(i, j)
                };
                snapshot[i, j] = new LogicalTile
                {
                    Id = Guid.NewGuid(),
                    Type = TileKind.Regular(finalType)
                };

                spawns.Add(spawn);
            }
        }
    }


    public void EvaluateBonusSpawn(SpawnRules spawnRules, List<Vector2Int> group, Vector2Int targetSpawnPos, List<SpawnInfo> spawns)
    {
        //ищем правила по размеру группы
        var rules = spawnRules.GetRules(group.Count);

        for (var i = 0; i < rules.Count; i++)
        {
            if (rules[i] is not null && rules[i].IsMatch(group))
            {
                var spawnInfo = new SpawnInfo
                {
                    Type = TileKind.Bonus(rules[i].BonusType),
                    Position = targetSpawnPos
                };
                spawns.Add(spawnInfo);
            }
        }
    }
}
