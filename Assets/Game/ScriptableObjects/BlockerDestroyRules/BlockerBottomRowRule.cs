using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BlockerBottomRowRule", menuName = "Scriptable Objects/BlockerDestroyRules/BottomRow")]
public class BlockerBottomRowRule : BlockerDestroyRuleBase
{
    public override bool ShouldDestroy(Vector2Int pos, LogicalTile?[,] snapshot, List<MatchInfo> currentMatches)
    {
        //int rows = snapshot.GetLength(0);
        return pos.x <= 0;
    }
}
