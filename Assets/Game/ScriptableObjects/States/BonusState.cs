using System.Collections.Generic;
using System.Threading.Tasks;
using System.Xml.Linq;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "BonusState", menuName = "Scriptable Objects/BonusState")]
public class BonusState : FieldState
{
    public override void Enter(FieldController field, FiniteStateMachine machine, TransitionContext context = default)
    {
        Debug.Log("bonus state");
        _field = field;
        _fsm = machine;
        var spawnedBonuses = new HashSet<TileController>();

        foreach (var match in context.Matches)
        {
            var type = _field.SpawnEvaluator.GetMatchedBonusTile(match.Positions);
            if (type is TileType.Neutral) continue;

            var i = 1;
            var p = new Vector2Int(0, 0);
            foreach (var pos in match.Positions)
            {
                if (UnityEngine.Random.Range(0, i++) == 0)
                {
                    p=pos;
                }
            }
            var info = new SpawnInfo { Type = type, IsBonus = true, Offset = 0, Position = p };
            var tile = _field.SpawnTile(info, context.Snapshot, context.CascadeIteration);
            _field.UpdateTileOnGrid(p, tile);
            spawnedBonuses.Add(tile);
        }

        AnimateBonusSpawn(spawnedBonuses);

        _fsm.Switch(StateEvent.SpawnBonus, context);
    }

    private void AnimateBonusSpawn(HashSet<TileController> bonuses)
    {
        var bonusAnimList = new List<AnimationData>();        
        foreach (var bonus in bonuses)
        {
            var data = new AnimationData
            {
                Type = AnimationType.SpawnAtPoint,
                Target = bonus.transform,
                Duration = 1.0f

            };
            bonusAnimList.Add(data);
        }

        var name = _fsm.Events.GetBusName(GameEvent.Animation);
        GameplayEventBus<List<AnimationData>>.Trigger(name, bonusAnimList);
    }
}
