using Assets.Game.Components;
using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "TileDistributor", menuName = "Scriptable Objects/TileDistributor")]
public class TileDistributor : ScriptableObject
{
    [SerializeField]
    public TileSpawnChance[] chances;

    private void OnEnable()
    {
        // Сбрасываем рабочие веса на стартовые настройки из конфига
        foreach (var item in chances)
        {
            item.currentChance = item.SpawnChance;
        }
    }
    
    public TileType GetRandomWeightedTileType(TileController.Snapshot?[,] snapshot, int r, int c, int currentCycle)
    {
        var tempWeights = new float[chances.Length];
        var totalChance = 0f;
        var penalty = Mathf.Pow(10, currentCycle + 1);

        for (int i = 0; i < chances.Length; i++)
        {
            var w = chances[i].currentChance;
            if (MatchEvaluator.GroupSizeAt(snapshot, new Vector2Int(r,c)) > 2)
            {
                w /= penalty;
            }

            tempWeights[i] = w;
            totalChance += w;
        }

        var rnd = UnityEngine.Random.Range(0f, totalChance);
        var cumulativeChance = 0f;

        for (int i = 0; i < chances.Length; i++)
        {
            cumulativeChance += tempWeights[i];
            if (cumulativeChance >= rnd)
            {
                RecalculateChances(chances[i].Type);
                return chances[i].Type;
            }
        }

        return TileType.Neutral;
    }

    public TileType GetBonusTileType(HashSet<Vector2Int> group)
    {
        if (group.Count < 4) return TileType.Neutral;

        if (group.Count >= 5) return TileType.Bomb;

        int minX = int.MaxValue, maxX = int.MinValue;
        int minY = int.MaxValue, maxY = int.MinValue;

        foreach (var pos in group)
        {
            if (pos.x < minX) minX = pos.x;
            if (pos.x > maxX) maxX = pos.x;
            if (pos.y < minY) minY = pos.y;
            if (pos.y > maxY) maxY = pos.y;
        }

        int width = maxX - minX + 1;
        int height = maxY - minY + 1;
        
        if (width > height)
        {
            return TileType.HorizontalBomb;
        }
        return TileType.VerticalBomb;
    }


    public void RecalculateChances(TileType spawnedType)
    {
        foreach (var item in chances)
        {
            if (item.Type == spawnedType)
            {
                item.currentChance = item.SpawnChance * 0.1f;
            }
            else
            {
                item.currentChance += 0.1f;
                item.currentChance = Mathf.Min(item.currentChance, item.SpawnChance * 2f);
            }
        }
    }
}


