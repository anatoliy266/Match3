using System.Collections.Generic;
using UnityEngine;

public abstract class BlockerDestroyRuleBase : ScriptableObject
{
    public abstract bool ShouldDestroy(Vector2Int pos, LogicalTile?[,] snapshot, List<MatchInfo> currentMatches);
}
