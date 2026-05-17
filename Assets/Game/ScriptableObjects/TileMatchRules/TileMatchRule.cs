using NUnit.Framework;
using System;
using Unity.VisualScripting;
using UnityEngine;
using static TileController;

public abstract class TileMatchRuleBase: ScriptableObject
{
    public abstract bool IsMatch(TileController.Snapshot?[,] board, Vector2Int source, Vector2Int current, Vector2Int target);
}
