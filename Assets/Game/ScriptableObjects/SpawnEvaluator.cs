using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;

public struct SpawnInfo
{
    public TileKind Type;
    public Vector2Int Position;
}


public class SpawnEvaluator : ScriptableObject
{
    //выбор обычной фишки
    //надо както предиктивно считать какой тип выбрать чтобы не было бесконечных совпадений и доска "усложнялась" после каждой итерации
    public void Evaluate(LogicalTile?[,] snapshot, SpawnRules rules, List<SpawnInfo> spawns)
    {
        var (r, c) = (snapshot.GetLength(0), snapshot.GetLength(1));
        for (var i = 0; i < r; i++)
        {
            for (var j = 0; j < c; j++)
            {
                if (snapshot[i, j] is not null) continue;

                // както по умному выбирает тип фишки для спавна по коммон правилу спавна
                // в коммон правиле - что должно быть?
                var type = RegularType.Yellow;

                var spawn = new SpawnInfo
                {
                    Type = TileKind.Regular(type),
                    Position = new Vector2Int(i, j)
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
