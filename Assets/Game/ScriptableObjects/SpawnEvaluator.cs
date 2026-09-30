using System;
using System.Buffers;
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
    private RegularType[] _regularTypes;
    public SpawnEvaluator()
    {
        _regularTypes = (RegularType[])System.Enum.GetValues(typeof(RegularType));
    }

    //выбор обычной фишки
    //надо както предиктивно считать какой тип выбрать чтобы не было бесконечных совпадений и доска "усложнялась" после каждой итерации
    public void Evaluate(FiniteStateMachine machine, LogicalTile?[,] snapshot, MatchRules rules, List<SpawnInfo> spawns, int difficultModified)
    {
        var (r, c) = (snapshot.GetLength(0), snapshot.GetLength(1));

        //var values = System.Enum.GetValues(typeof(RegularType));

        var group = ListPool<Guid>.Get();
        var visited = ArrayPool<bool>.Shared.Rent(r * c);

        for (var i = 0; i < r; i++)
        {
            for (var j = 0; j < c; j++)
            {
                if (snapshot[i, j] is not null) continue;

                var totalWeight = 0.0f;
                var choosen = 0;

                for (var val = 0; val < _regularTypes.Length; val++)
                {
                    var type = (RegularType)val;
                    group.Clear();
                    Array.Clear(visited, 0, visited.Length);

                    machine.MatchEvaluator.EvaluateTile(snapshot, new Vector2Int(i, j), TileKind.Regular(type), rules, group, visited);

                    var currentweight = group.Count > 2 ? 1.0f / (1.0f + difficultModified) : 1.0f;
                    totalWeight += currentweight;

                    if (UnityEngine.Random.Range(0, totalWeight) <= currentweight) choosen = val;
                }

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
        ListPool<Guid>.Release(group);
        ArrayPool<bool>.Shared.Return(visited);
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
