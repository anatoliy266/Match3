using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class TaskListController : MonoBehaviour
{
    [Header("UI Goals System")]
    [SerializeField][Req] private GoalController goalPrefab;
    
    [SerializeField][Req] private TileTypeData tileTypeData;
    [SerializeField][Req] private Events Events;

    // Словари для быстрого поиска UI-элемента цели по типу нитки/фишки
    private Dictionary<RegularType, GoalController> _regularGoalsDict = new Dictionary<RegularType, GoalController>();
    private Dictionary<BonusType, GoalController> _bonusGoalsDict = new Dictionary<BonusType, GoalController>();
    

    private Queue<(LogicalTile?[,], LogicalTile?[,])> _scoreQueue = new Queue<(LogicalTile?[,], LogicalTile?[,])>();
    private void OnEnable()
    {
        var name = Events.GetBusName(GameEvent.Score);
        GameplayEventBus<(LogicalTile?[,], LogicalTile?[,])>.Register(name, OnScoreCalculation);

        var shaderDestroyTileBusName = Events.GetBusName(GameEvent.ShaderDestroyTile);
        GameplayEventBus<bool>.Register(shaderDestroyTileBusName, HandleMatchDestroyedVisual);
    }

    private void OnDisable()
    {
        var name = Events.GetBusName(GameEvent.Score);
        GameplayEventBus<(LogicalTile?[,], LogicalTile?[,])>.Unregister(name, OnScoreCalculation);

        var shaderDestroyTileBusName = Events.GetBusName(GameEvent.ShaderDestroyTile);
        GameplayEventBus<bool>.Unregister(shaderDestroyTileBusName, HandleMatchDestroyedVisual);
    }

    public void Initialize(LevelSettings settings)
    {
        _scoreQueue.Clear();

        foreach (var goal in _regularGoalsDict.Values) if (goal != null) Destroy(goal.gameObject);
        foreach (var goal in _bonusGoalsDict.Values) if (goal != null) Destroy(goal.gameObject);

        _regularGoalsDict.Clear();
        _bonusGoalsDict.Clear();

        if (settings == null || settings.ropesGoalsList == null) return;

        for (var i = 0; i < settings.ropesGoalsList.Count; i++)
        {
            var goalData = settings.ropesGoalsList[i];
            var kind = goalData.Kind;

            GoalController newGoalUI = Instantiate(goalPrefab, this.transform);

            if (kind.KindType == TileKindType.Regular)
            {
                Color color = tileTypeData.GetColor(kind.RegularType);
                newGoalUI.SetupGoal(color, goalData.count);

                _regularGoalsDict[kind.RegularType] = newGoalUI;
            }
            else if (kind.KindType == TileKindType.Bonus)
            {
                Color color = tileTypeData.GetColor(kind.BonusType);
                newGoalUI.SetupGoal(color, goalData.count);

                _bonusGoalsDict[kind.BonusType] = newGoalUI;
            }
        }
    }

    private void OnScoreCalculation((LogicalTile?[,], LogicalTile?[,]) tuple)
    {
        _scoreQueue.Enqueue(tuple);
    }

    private void HandleMatchDestroyedVisual(bool val)
    {
        if (_scoreQueue.Count == 0) return;

        var data = _scoreQueue.Dequeue();
        var destroyedTiles = ListPool<TileKind>.Get();
        destroyedTiles.Clear();
        GetDestroyedTileKinds(data, destroyedTiles);

        // Бежим по всем уничтоженным на поле фишкам
        for (var i = 0; i < destroyedTiles.Count; i++)
        {
            var tile = destroyedTiles[i];

            if (tile.KindType == TileKindType.Regular)
            {
                // Если такая фишка сейчас есть в целях уровня — обновляем её UI на 1 единицу
                if (_regularGoalsDict.TryGetValue(tile.RegularType, out var goalUI))
                {
                    Color color = tileTypeData.GetColor(tile.RegularType);
                    goalUI.UpdateGoal(color, 1);
                }
            }
            else if (tile.KindType == TileKindType.Bonus)
            {
                // Если такой бонус есть в целях уровня — обновляем её UI на 1 единицу
                if (_bonusGoalsDict.TryGetValue(tile.BonusType, out var goalUI))
                {
                    Color color = tileTypeData.GetColor(tile.BonusType);
                    goalUI.UpdateGoal(color, 1);
                }
            }
        }

        ListPool<TileKind>.Release(destroyedTiles);
    }

    private void GetDestroyedTileKinds((LogicalTile?[,], LogicalTile?[,]) snapData, List<TileKind> outputList)
    {
        var (prevSnapshot, snapshot) = snapData;

        if (snapshot == null || prevSnapshot == null || outputList == null) return;

        outputList.Clear();

        var dictCopy = UnityEngine.Pool.DictionaryPool<System.Guid, TileKind>.Get();
        dictCopy.Clear();

        var (r, c) = (snapshot.GetLength(0), snapshot.GetLength(1));

        for (var i = 0; i < r; i++)
        {
            for (var j = 0; j < c; j++)
            {
                var item = prevSnapshot[i, j];
                if (item is null) continue;

                dictCopy[item.Value.Id] = item.Value.Type;
            }
        }

        for (var i = 0; i < r; i++)
        {
            for (var j = 0; j < c; j++)
            {
                var item = snapshot[i, j];
                if (item is null) continue;

                dictCopy.Remove(item.Value.Id);
            }
        }

        foreach (var kvp in dictCopy)
        {
            outputList.Add(kvp.Value);
        }

        UnityEngine.Pool.DictionaryPool<System.Guid, TileKind>.Release(dictCopy);
    }

    public bool AreAllGoalsCompleted()
    {

        foreach (var goal in _regularGoalsDict.Values)
        {
            if (goal != null && !goal.IsCompleted)
                return false;
        }
        foreach (var goal in _bonusGoalsDict.Values)
        {
            if (goal != null && !goal.IsCompleted)
                return false;
        }
        return true;
    }
}
