using System;
using UnityEngine;

[CreateAssetMenu(fileName = "TileBombMatchRule", menuName = "Rules/Bomb")]
[Serializable]
public class TileBombMatchRule : TileMatchRuleBase
{
    [SerializeField]
    private int _explosionRadius = 1;

    public override bool IsMatch(in TileSnapshot source, in TileSnapshot current, in TileSnapshot target)
    {
        // Вычисляем расстояние от источника взрыва (Source) до проверяемой плитки (Target)
        Vector2Int diff = target.Position - source.Position;

        // Чебышёвское расстояние (радиус квадрата вокруг бомбы)
        int distance = Mathf.Max(Mathf.Abs(diff.x), Mathf.Abs(diff.y));

        return distance <= _explosionRadius;
    }
}
