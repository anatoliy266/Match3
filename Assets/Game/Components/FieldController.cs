using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

public struct SpawnInfo
{
    public TileType Type { get; internal set; }
    public Vector2Int Position { get; internal set; }
    public int Offset { get; internal set; }
    public bool IsBonus { get; internal set; }
}


public class FieldController : MonoBehaviour
{
    public int Rows;
    public int Cols;
    [Req] public TileController Tile;
    [Req] public TileTypeData TileTypeData;
    [Req] public TileDistributor TileDistributor;
    [Req] public SpawnRules SpawnRules;

    [Req] public DragManager DragManager;
    [Req] public AnimationManager AnimationManager;
    [Req] public MatchEvaluator MatchEvaluator;
    [Req] public ScoreManager ScoreManager;

    private TileController[,] _tiles;
    private Queue<(Transform, Vector3)> _animQueue;
    [SerializeField]
    private float _animSpeed = 0.5f;
    [SerializeField]
    private int _cycle = 0;
    private bool _isHintPlaying;
    private float _hintTimer;

    public float TimeToHint;
    private bool _isBusy = false;

    public event Action<List<TileController>> OnTilesCleared;



    private void Awake()
    {
        _tiles = new TileController[Rows, Cols];
    }

    void Start()
    {
        //var tasks = Fill();
        //if (tasks.Count > 0)
        //{
        //    Task.WhenAll(tasks);
        //}
    }

    void Update()
    {
        //if (_isHintPlaying)
        //{
        //    _hintTimer = 0f;
        //}
        //_hintTimer += Time.deltaTime;
        //if (_hintTimer >= TimeToHint)
        //{
        //    _hintTimer = 0f;
        //    _ = Hint();
        //}
    }

    //public async Task<bool> Evaluate(Func<List<HashSet<Vector2Int>>> getGroup)
    //{
    //    DragManager.Instance.SetBusy(true);
    //    var groups = getGroup();
    //    if (groups.Count == 0) { _cycle = 0; return false; }
    //    _cycle++;

    //    await RemoveMatches(groups);
    //    await SpawnBonuses(groups);
    //    await CompactBoard();
    //    await FillEmpty();
    //    await Evaluate(() => MatchEvaluator.FindAll(ToSnapshot()));
    //    DragManager.Instance.SetBusy(false);
    //    return true;
    //}

    //private async Task SpawnBonuses(List<HashSet<Vector2Int>> groups)
    //{
    //    try
    //    {
    //        var tasks = new List<Task>();
    //        for (var i = 0; i < groups.Count; i++)
    //        {
    //            //по каждой группе находим соответствует ли ее геометрия какомуто бонусу и если соответствует - спавнить бонус на строго указанную позицию в геометрии
    //            if (groups[i].Count > 3)
    //            {
    //                var rules = SpawnRules.GetRules(TileType.Neutral);
    //                foreach (var rule in rules)
    //                {
    //                    if (rule.IsMatch(groups[i]))
    //                    {
    //                        var pos = groups[i].First();
    //                        var (tile, task) = SpawnBonusTile(new Bonus { Position = pos, Type = rule.Type });
    //                        _tiles[pos.x, pos.y] = tile;
    //                        tasks.Add(task);
    //                    }
    //                }
    //            }
    //        }

    //        if (tasks.Count > 0)
    //        {
    //            await Task.WhenAll(tasks);
    //        }
    //    }
    //    catch (Exception ex) { Debug.LogException(ex); }

    //}

    public TileController.Snapshot?[,] ToSnapshot()
    {
        var board = new TileController.Snapshot?[Rows, Cols];
        for (var r = 0; r < Rows; r++)
            for (var c = 0; c < Cols; c++)
                board[r, c] = _tiles[r, c];
        return board;
    }



    //private async Task RemoveMatches(List<HashSet<Vector2Int>> groups)
    //{
    //    try
    //    {
    //        var tasks = new List<Task>();

    //        for (var i = 0; i < groups.Count; i++)
    //        {
    //            var score = 0;
    //            foreach (var pos in groups[i])
    //            {
    //                if (_tiles[pos.x, pos.y] != null)
    //                {
    //                    score += TileTypeData.GetScore(_tiles[pos.x, pos.y].Type);
    //                    var task = AnimationManager.DoDestroyAsync(_tiles[pos.x, pos.y].transform, _animSpeed);
    //                    tasks.Add(task);
    //                    _tiles[pos.x, pos.y] = null;
    //                }
    //            }
    //            ScoreManager.Instance.AddScore(score);
    //        }
    //        if (tasks.Count > 0)
    //        {
    //            await Task.WhenAll(tasks);
    //        }
    //    } catch (Exception ex)
    //    {
    //        Debug.LogException(ex);
    //    }

    //}


    //private async Task CompactBoard()
    //{
    //    var tasks = new List<Task>();
    //    for (var col = 0; col < cols; col++)
    //    {
    //        var (slow, fast) = (0, 0);
    //        while (fast < _tiles.GetLength(0))
    //        {
    //            if (_tiles[fast, col] != null)
    //            {
    //                if (fast > slow)
    //                {
    //                    tasks.AddRange(SwapTiles(_tiles[fast, col], new Vector2Int(slow, col), null, new Vector2Int(-1, -1)));
    //                }
    //                slow++;
    //            }
    //            fast++;
    //        }
    //    }
    //    if (tasks.Count > 0)
    //    {
    //        await Task.WhenAll(tasks);
    //    }
    //    Debug.Log("Compact tiles worked fine");
    //}

    //public List<Task> SwapTiles(TileController tile1, Vector2Int pos1, TileController tile2, Vector2Int pos2)
    //{
    //    var moves = new List<Task>();

    //    _tiles[tile1.GridPosition.x, tile1.GridPosition.y] = null;
    //    if (tile2 != null) _tiles[tile2.GridPosition.x, tile2.GridPosition.y] = null;

    //    _tiles[pos1.x, pos1.y] = tile1;
    //    tile1.GridPosition = pos1;
    //    moves.Add(AnimationHelper.DoMoveExactTimeAsync(tile1.transform, GetWorldPos(pos1.x, pos1.y), _animSpeed));

    //    if (tile2 != null)
    //    {
    //        _tiles[pos2.x, pos2.y] = tile2;
    //        tile2.GridPosition = pos2;
    //        moves.Add(AnimationHelper.DoMoveExactTimeAsync(tile2.transform, GetWorldPos(pos2.x, pos2.y), _animSpeed));
    //    }

    //    return moves;
    //}

    //private async Task FillEmpty()
    //{
    //    try
    //    {
    //        var snapshot = ToSnapshot();
    //        var tasks = new List<Task>();
    //        for (var row = 0; row < Rows; row++)
    //        {
    //            for (var col = 0; col < Cols; col++)
    //            {
    //                if (_tiles[row, col] == null)
    //                {
    //                    var (tile, task) = SpawnTile(row, col, 1, snapshot);
    //                    _tiles[row, col] = tile;
    //                    tasks.Add(task);
    //                }
    //            }
    //        }
    //        Debug.Log("fil empty anim started");
    //        if (tasks.Count > 0)
    //        {
    //            await Task.WhenAll(tasks);
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        Debug.LogException(ex);
    //    }

    //}

    public Vector3 GetWorldPos(int row, int col)
    {
        return new Vector3(col - Cols / 2, row - Rows / 2, 0);
    }
    public Vector3 GetWorldPos(Vector2Int v)
    {
        return new Vector3(v.y - Cols / 2, v.x - Rows / 2, 0);
    }

    //public async Task TryPlayerMove(TileController t1, Vector2Int p1, TileController t2, Vector2Int p2)
    //{
    //    try
    //    {
    //        Debug.Log("TryPlayerMove");

    //        _hintTimer = 0f;
    //        var startP1 = t1.GridPosition;
    //        var startP2 = t2.GridPosition;
    //        SwapTiles(t1, p1, t2, p2);

    //        bool isBonusMove = t1.IsBonus || t2.IsBonus;
    //        bool isNewChanges = false;

    //        if (isBonusMove)
    //        {
    //            Vector2Int bonusPos = t1.IsBonus ? p1 : p2;
    //            isNewChanges = await Evaluate(() =>
    //            {
    //                var board = ToSnapshot();
    //                var groups = MatchEvaluator.Instance.FindAllBonuses(board, bonusPos);
    //                return groups;
    //            });
    //        }
    //        else
    //        {
    //            isNewChanges = await Evaluate(() =>
    //                MatchEvaluator.Instance.FindAll(ToSnapshot())
    //            );
    //        }

    //        if (!isNewChanges)
    //        {
    //            SwapTiles(t1, startP1, t2, startP2);
    //        }
    //    } catch (Exception ex) { Debug.LogException(ex); }
    //}

    //private TileController SpawnTileAt(int i, int j, Vector3 worldPosition, TileType type, bool isBonus)
    //{
    //    var tile = ObjectPool.SharedInstance.GetObject();
    //    tile.SetType(type);

    //    tile.GridPosition = new Vector2Int(i, j);
    //    tile.transform.SetParent(this.transform);
    //    tile.transform.position = worldPosition;
    //    tile.IsBonus = isBonus;

    //    return tile;
    //}

    //private (TileController tile, Task moveTask) SpawnNormalTile(int i, int j, int spawnOffset, TileController.Snapshot?[,] snapshot)
    //{
    //    var topCellPos = GetWorldPos(rows - 1, j);
    //    var spawnPosition = new Vector3(topCellPos.x, topCellPos.y + spawnOffset, 0);
    //    var type = TileDistributor.GetRandomWeightedTileType(snapshot, i, j, _cycle);

    //    var tile = SpawnTileAt(i, j, spawnPosition, type, false);

    //    var targetWorldPosition = GetWorldPos(i, j);
    //    var task = AnimationManager.DoMoveExactTimeAsync(tile.transform, targetWorldPosition, _animSpeed);

    //    return (tile, task);
    //}

    //private (TileController tile, Task moveTask) SpawnBonusTile(Bonus bonus)
    //{
    //    var targetWorldPosition = GetWorldPos(bonus.Position.x, bonus.Position.y);
    //    var tile = SpawnTileAt(bonus.Position.x, bonus.Position.y, targetWorldPosition, bonus.Type, true);
    //    var task = AnimationManager.DoSpawnAtPointAsync(tile.transform, _animSpeed);
    //    return (tile, task);
    //}


    //public List<Task> Fill()
    //{
    //    var snapshot = new TileController.Snapshot?[rows, cols];
    //    var tasks = new List<Task>();
    //    for (var i = 0; i < _tiles.GetLength(0); i++)
    //    {
    //        for (var j = 0; j < _tiles.GetLength(1); j++)
    //        {
    //            var (tile, task) = SpawnNormalTile(i, j, 1, snapshot);
    //            _tiles[i, j] = tile;
    //            snapshot[i, j] = tile;
    //            tasks.Add(task);
    //        }
    //    }
    //    return tasks;
    //}

    //public async Task Hint()
    //{
    //    if (_isHintPlaying) return;

    //    var snapshot = ToSnapshot();
    //    for (var i = 0; i < Rows; i++)
    //    {
    //        for (var j = 0; j < Cols; j++)
    //        {
    //            if (snapshot[i, j] == null) continue;

    //            // ВПРАВО (j + 1)
    //            if (j + 1 < Cols && snapshot[i, j + 1] != null)
    //            {

    //                (snapshot[i, j], snapshot[i, j + 1]) = (snapshot[i, j + 1], snapshot[i, j]);

    //                if (MatchEvaluator.GroupSizeAt(snapshot, new Vector2Int(i, j)) >= 3 ||
    //                    MatchEvaluator.GroupSizeAt(snapshot, new Vector2Int(i, j + 1)) >= 3)
    //                {
    //                    await AnimateHintAsync(_tiles[i, j], _tiles[i, j + 1]);
    //                    return;
    //                }

    //                (snapshot[i, j], snapshot[i, j + 1]) = (snapshot[i, j + 1], snapshot[i, j]);
    //            }

    //            // ВНИЗ (i + 1)
    //            if (i + 1 < Rows && snapshot[i + 1, j] != null)
    //            {
    //                (snapshot[i, j], snapshot[i + 1, j]) = (snapshot[i + 1, j], snapshot[i, j]);

    //                if (MatchEvaluator.GroupSizeAt(snapshot, new Vector2Int(i, j)) >= 3 ||
    //                    MatchEvaluator.GroupSizeAt(snapshot, new Vector2Int(i + 1, j)) >= 3)
    //                {
    //                    await AnimateHintAsync(_tiles[i, j], _tiles[i + 1, j]);
    //                    return;
    //                }

    //                (snapshot[i, j], snapshot[i + 1, j]) = (snapshot[i + 1, j], snapshot[i, j]);
    //            }
    //        }
    //    }
    //}

    //private async Task AnimateHintAsync(TileController tile1, TileController tile2)
    //{
    //    _isHintPlaying = true;
    //    var pos1 = tile1.GridPosition;
    //    var pos2 = tile2.GridPosition;

    //    SwapTiles(tile1, pos2, tile2, pos1);


    //    await Task.Delay(300);

    //    SwapTiles(tile1, pos1, tile2, pos2);

    //    _isHintPlaying = false;
    //}






    //////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    /////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    ///

    public void SwapTiles(TileController tile1, Vector2Int pos1, TileController tile2, Vector2Int pos2)
    {
        _tiles[tile1.GridPosition.x, tile1.GridPosition.y] = null;
        if (tile2 != null) _tiles[tile2.GridPosition.x, tile2.GridPosition.y] = null;

        _tiles[pos1.x, pos1.y] = tile1;
        tile1.GridPosition = pos1;

        if (tile2 != null)
        {
            _tiles[pos2.x, pos2.y] = tile2;
            tile2.GridPosition = pos2;
        }
    }

    public TileController GetTileAt(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= _tiles.GetLength(0) ||
        pos.y < 0 || pos.y >= _tiles.GetLength(1))
        {
            return null;
        }
        return _tiles[pos.x, pos.y];
    }

    public void RemoveTileAt(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= _tiles.GetLength(0) ||
        pos.y < 0 || pos.y >= _tiles.GetLength(1))
        {
            return;
        }
        _tiles[pos.x, pos.y] = null;
    }

    public void RemoveTiles(List<MatchInfo> groups)
    {
        foreach (var group in groups)
        {
            foreach (var tile in group.Positions)
            {
                RemoveTileAt(tile);
            }
        }
    }

    public void CompactBoard(TransitionContext context)
    {
        var fallCommands = new List<CompactInfo>();
        int rows = _tiles.GetLength(0);
        int cols = _tiles.GetLength(1);

        for (var col = 0; col < cols; col++)
        {
            var (slow, fast) = (0, 0);
            while (fast < rows)
            {
                if (_tiles[fast, col] != null)
                {
                    if (fast > slow)
                    {
                        var tile = _tiles[fast, col];
                        var targetPos = new Vector2Int(slow, col);

                        // 1. Двигаем данные внутри логического массива поля
                        _tiles[slow, col] = tile;
                        _tiles[fast, col] = null;
                        tile.GridPosition = targetPos;

                        // 2. Запоминаем команду для аниматора
                        fallCommands.Add(new CompactInfo { Tile = tile, TargetPos = targetPos });
                    }
                    slow++;
                }
                fast++;
            }
        }
        context.Compacts = fallCommands;
    }

    public void FillBoard()
    {
        var snapshot = new TileController.Snapshot?[Rows, Cols];

        for (var i = 0; i < _tiles.GetLength(0); i++)
        {
            for (var j = 0; j < _tiles.GetLength(1); j++)
            {
                var info = new SpawnInfo
                {
                    Position = new Vector2Int(i, j),
                    Offset = 1,
                    IsBonus = false
                };

                // Фабрика просто создает фишку и кладет в массив. Всё!
                _tiles[i, j] = SpawnTile(info, snapshot);
                snapshot[i, j] = _tiles[i, j];
            }
        }
    }

    private TileController SpawnTileAt(Vector2Int pos, Vector3 worldPosition, TileType type, bool isBonus)
    {
        var tile = ObjectPool.SharedInstance.GetObject();
        tile.SetType(type);

        tile.GridPosition = pos;
        tile.transform.SetParent(this.transform);
        tile.transform.position = worldPosition;
        tile.IsBonus = isBonus;

        return tile;
    }


    public TileController SpawnTile(SpawnInfo info, TileController.Snapshot?[,] snapshot)
    {
        Vector3 spawnWorldPos;

        if (info.IsBonus)
        {
            spawnWorldPos = GetWorldPos(info.Position.x, info.Position.y);
        }
        else
        {
            var topCellPos = GetWorldPos(Rows - 1, info.Position.y);
            spawnWorldPos = new Vector3(topCellPos.x, topCellPos.y + info.Offset, 0);

            info.Type = TileDistributor.GetRandomWeightedTileType(snapshot, info.Position.x, info.Position.y, _cycle);
        }
        return SpawnTileAt(info.Position, spawnWorldPos, info.Type, info.IsBonus);
    }

    public void FillEmptyTilesOnGrid()
    {
        var snapshot = ToSnapshot();


        for (var col = 0; col < Cols; col++)
        {
            int spawnOffset = 1;

            for (var row = 0; row < Rows; row++)
            {
                if (_tiles[row, col] == null)
                {
                    var info = new SpawnInfo
                    {
                        Position = new Vector2Int(row, col),
                        Offset = spawnOffset,
                        IsBonus = false
                    };

                    _tiles[row, col] = SpawnTile(info, snapshot);

                    spawnOffset++;
                }
            }
        }
    }
}

