using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockerIndestructibleRule", menuName = "Scriptable Objects/BlockerDestroyRules/Indestructible")]
public class BlockerIndestructibleRule : BlockerDestroyRuleBase
{
    public override bool ShouldDestroy(Vector2Int pos, LogicalTile?[,] snapshot, List<MatchInfo> currentMatches)
    {
        return false;
    }
}
