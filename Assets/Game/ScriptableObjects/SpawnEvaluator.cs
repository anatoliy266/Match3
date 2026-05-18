using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "SpawnEvaluator", menuName = "Scriptable Objects/SpawnEvaluator")]
public class SpawnEvaluator : ScriptableObject
{
    [Req] public SpawnRules spawnRules;
    [Req] public MatchRules matchRules;

    //выбор обычной фишки
    //надо както предиктивно считать какой тип выбрать чтобы не было бесконечных совпадений и доска "усложнялась" после каждой итерации
    public TileType GetPredictedWeightedTile(TileController.Snapshot?[,] snapshot, Vector2Int pos, int iterationCnt)
    {
        var chance = GetMatchChance(iterationCnt);
        var match = new bool[7];
        for (var i = 1; i < 7; i++)
        {
            var typeForCheck = (TileType)i;
            var original = snapshot[pos.x, pos.y];
            if (original is null) return TileType.Neutral;

            var hypothetical = original.Value.WithType(typeForCheck);
            snapshot[pos.x, pos.y] = hypothetical;

            var rule = matchRules.GetRule(typeForCheck);
            if (rule is null) return TileType.Neutral;
            //поменять в снепшоте ячейку на тип
            var group = BFS.Run(snapshot, pos, rule.IsMatch);
            match[i] = group.Count > 2;
            //вернуть в снепшоте ячейку на старый тип
            snapshot[pos.x, pos.y] = original;
        }
        //подкрутка, если попали в процент - берем рандомный создавший матч из списка, если не попал - то не создавший матч
        var prediction = UnityEngine.Random.Range(0, 100);
        bool isMatching = false;
        if (prediction < chance) isMatching = true;

        var tile = TileType.Neutral;
        if (isMatching) {
            var i = 1;
            for (var m = 1; m < 7; m++)
            {
                if (!match[m]) continue;
                var predict = UnityEngine.Random.Range(0,i++);
                if (predict == 0) tile = (TileType)m;
            }
        } else
        {
            var i = 1;
            for (var m = 1; m < 7; m++)
            {
                if (match[m]) continue;
                var predict = UnityEngine.Random.Range(0, i++);
                if (predict == 0) tile = (TileType)m;
            }
        }

        return tile;
    }

    //выбор бонусной фишки
    //сравниваем группу с правилами, если совпало - ворзвращаем тип
    public TileType GetMatchedBonusTile(IEnumerable<Vector2Int> group)
    {
        for (var i = 7; i < 10; i++)
        {
            var rule = spawnRules.GetRule((TileType)i);
            if (rule.IsMatch(group)) return rule.Type;
        }
        return TileType.Neutral;
    }


    //подкрутка
    //подкрутка шанса на то что спавнящаяся фишка создаст матч. уменьшаться както должно с каждым каскадом

    public float GetMatchChance(int iterationCnt)
    {
        return (float) 1 / (float) iterationCnt * 100;
    }
}
