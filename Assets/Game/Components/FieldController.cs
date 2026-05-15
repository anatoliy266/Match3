using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

public struct Bonus
{
    public Vector2Int Position {  get; set; }
    public TileType Type { get; set; }
}


public class FieldController : MonoBehaviour
{
    public int rows;
    public int cols;
    public TileController Tile;
    public TileTypeData TileType;
    public TileDistributor TileDistributor;

    private TileController[,] _tiles;
    private Queue<(Transform, Vector3)> _animQueue;
    private float _animSpeed = 0.2f;
    [SerializeField]
    private int _cycle = 0;
    private bool _isHintPlaying;
    private float _hintTimer;

    public float TimeToHint;

    private void Awake()
    {
        _tiles = new TileController[rows, cols];
    }

    void Start()
    {
        var tasks = Fill();
        if (tasks.Count > 0)
        {
            Task.WhenAll(tasks);
        }
    }

    void Update()
    {
        if (_isHintPlaying)
        {
            _hintTimer = 0f;
        }
        _hintTimer += Time.deltaTime;
        if (_hintTimer >= TimeToHint)
        {
            _hintTimer = 0f;
            _ = Hint();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Debug.Log("space pressed");
            _ = Evaluate();
        }
    }

    private async Task<bool> Evaluate()
    {
        var groups = MatchEvaluator.Instance.FindAll(ToSnapshot());
        Debug.Log($"group found: {groups.Count}");
        if (groups.Count == 0) { _cycle = 0; return false; }
        _cycle++;

        await RemoveMatches(groups);
        await CompactBoard();
        await FillEmpty();
        _ = Evaluate();
        return true;
    }

    private TileController.Snapshot?[,] ToSnapshot()
    {
        var board = new TileController.Snapshot?[rows, cols];
        for (var r = 0; r < rows; r++)
            for (var c = 0; c < cols; c++)
                board[r, c] = _tiles[r, c];
        return board;
    }

    private async Task RemoveMatches(List<HashSet<Vector2Int>> groups)
    {
        var tasks = new List<Task>();
        var bonusSpawnPoints = ArrayPool<Bonus>.Shared.Rent(groups.Count);
        for (var i = 0; i < groups.Count; i++)
        {
            var center = FindCenterPoint(groups[i]);
            bonusSpawnPoints[i].Position = center;

            var bonusType = TileDistributor.GetBonusTileType(groups[i]);
            bonusSpawnPoints[i].Type = bonusType;

            var score = 0;
            foreach (var pos in groups[i])
            {
                if (_tiles[pos.x, pos.y] != null)
                {
                    score += TileType.GetScore(_tiles[pos.x, pos.y].Type);
                    var task = AnimationHelper.DoDestroyAsync(_tiles[pos.x, pos.y].transform, _animSpeed);
                    tasks.Add(task);
                    _tiles[pos.x, pos.y] = null;
                }
            }
            ScoreManager.Instance.AddScore(score);
        }
        
        for (var i = 0;i < groups.Count; i++)
        {
            
            var bonus = bonusSpawnPoints[i];
            if (bonus.Type == global::TileType.Neutral) continue;
            var (tile, task) = SpawnBonusTile(bonus);
            _tiles[bonus.Position.x, bonus.Position.y] = tile;
            tasks.Add(task);
        }
        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
        ArrayPool<Bonus>.Shared.Return(bonusSpawnPoints);
        Debug.Log("remove tiles worked fine");
    }

    

    private Vector2Int FindCenterPoint(HashSet<Vector2Int> group)
    {
        var centerPoint = new Vector2Int(0, 0);
        var maxNeighbours = -1;
        foreach (var pos in group)
        {
            var neighbourCnt = 0;
            if (group.Contains(new Vector2Int(pos.x - 1, pos.y))) neighbourCnt++;
            if (group.Contains(new Vector2Int(pos.x + 1, pos.y))) neighbourCnt++;
            if (group.Contains(new Vector2Int(pos.x, pos.y - 1))) neighbourCnt++;
            if (group.Contains(new Vector2Int(pos.x, pos.y + 1))) neighbourCnt++;
            if (neighbourCnt > maxNeighbours) { 
                maxNeighbours = neighbourCnt;
                centerPoint = pos;
                if (maxNeighbours == 4) break;
            }
        }
        return centerPoint;
    }

    private async Task CompactBoard()
    {
        var tasks = new List<Task>();
        for (var col = 0; col < cols; col++)
        {
            var (slow, fast) = (0, 0);
            while (fast < _tiles.GetLength(0))
            {
                if (_tiles[fast, col] != null)
                {
                    if (fast > slow)
                    {
                        tasks.AddRange(SwapTiles(_tiles[fast, col], new Vector2Int(slow, col), null, new Vector2Int(-1, -1)));
                    }
                    slow++;
                }
                fast++;
            }
        }
        if (tasks.Count > 0)
        {
            await Task.WhenAll(tasks);
        }
        Debug.Log("Compact tiles worked fine");
    }

    public List<Task> SwapTiles(TileController tile1, Vector2Int pos1, TileController tile2, Vector2Int pos2)
    {
        var moves = new List<Task>();

        _tiles[tile1.GridPosition.x, tile1.GridPosition.y] = null;
        if (tile2 != null) _tiles[tile2.GridPosition.x, tile2.GridPosition.y] = null;

        _tiles[pos1.x, pos1.y] = tile1;
        tile1.GridPosition = pos1;
        moves.Add(AnimationHelper.DoMoveExactTimeAsync(tile1.transform, GetWorldPos(pos1.x, pos1.y), _animSpeed));

        if (tile2 != null)
        {
            _tiles[pos2.x, pos2.y] = tile2;
            tile2.GridPosition = pos2;
            moves.Add(AnimationHelper.DoMoveExactTimeAsync(tile2.transform, GetWorldPos(pos2.x, pos2.y), _animSpeed));
        }

        return moves;
    }

    private async Task FillEmpty()
    {
        try
        {
            var snapshot = ToSnapshot();
            var tasks = new List<Task>();
            for (var row = 0; row < rows; row++)
            {
                for (var col = 0; col < cols; col++)
                {
                    if (_tiles[row, col] == null)
                    {
                        var (tile, task) = SpawnNormalTile(row, col, 1, snapshot);
                        _tiles[row, col] = tile;
                        tasks.Add(task);
                    }
                }
            }
            Debug.Log("fil empty anim started");
            if (tasks.Count > 0)
            {
                await Task.WhenAll(tasks);
            }
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
        }

    }

    private Vector3 GetWorldPos(int row, int col)
    {
        return new Vector3(col - cols / 2, row - rows / 2, 0);
    }

    public async Task TryPlayerMove(TileController t1, Vector2Int p1, TileController t2, Vector2Int p2)
    {
        Debug.Log("TryPlayerMove");
        _hintTimer = 0f;
        var startP1 = t1.GridPosition;
        var startP2 = t2.GridPosition;
        SwapTiles(t1, p1, t2, p2);
        try
        {
            var isNewChanges = await Evaluate();
            if (!isNewChanges)
            {
                SwapTiles(t1, startP1, t2, startP2);
            }
        }
        catch (Exception ex) { 
            Debug.LogException(ex);
        }
         
        
    }

    private TileController SpawnTileAt(int i, int j, Vector3 worldPosition, TileType type, bool isBonus)
    {
        var tile = ObjectPool.SharedInstance.GetObject();
        tile.SetType(type);

        tile.GridPosition = new Vector2Int(i, j);
        tile.transform.SetParent(this.transform);
        tile.transform.position = worldPosition;
        tile.IsBonus = isBonus;

        return tile;
    }

    private (TileController tile, Task moveTask) SpawnNormalTile(int i, int j, int spawnOffset, TileController.Snapshot?[,] snapshot)
    {
        var topCellPos = GetWorldPos(rows - 1, j);
        var spawnPosition = new Vector3(topCellPos.x, topCellPos.y + spawnOffset, 0);
        var type = TileDistributor.GetRandomWeightedTileType(snapshot, i, j, _cycle);

        var tile = SpawnTileAt(i, j, spawnPosition, type, false);

        var targetWorldPosition = GetWorldPos(i, j);
        var task = AnimationHelper.DoMoveExactTimeAsync(tile.transform, targetWorldPosition, _animSpeed);

        return (tile, task);
    }

    private (TileController tile, Task moveTask) SpawnBonusTile(Bonus bonus)
    {
        var targetWorldPosition = GetWorldPos(bonus.Position.x, bonus.Position.y);
        var tile = SpawnTileAt(bonus.Position.x, bonus.Position.y, targetWorldPosition, bonus.Type, true);
        var task = AnimationHelper.DoSpawnAtPointAsync(tile.transform, _animSpeed);
        return (tile, task);
    }

    private (TileController tile, Task moveTask) SpawnTile(int i, int j, int spawnOffset, TileController.Snapshot?[,] snapshot)
    {
        var topCellPos = GetWorldPos(rows - 1, j);
        var spawnPosition = new Vector3(topCellPos.x, topCellPos.y + spawnOffset, 0);

        var tile = ObjectPool.SharedInstance.GetObject();
        try
        {
            var type = TileDistributor.GetRandomWeightedTileType(snapshot, i, j, _cycle);
            tile.SetType(type);

        }
        catch (Exception e)
        {
            Debug.LogException(e);
        }

        tile.IsMoving = true;
        tile.GridPosition = new Vector2Int(i, j);
        tile.transform.SetParent(this.transform);
        tile.transform.position = spawnPosition;

        var targetWorldPosition = GetWorldPos(i, j);
        var task = AnimationHelper.DoMoveExactTimeAsync(tile.transform, targetWorldPosition, _animSpeed);
        return (tile, task);
    }

    private List<Task> Fill()
    {
        var snapshot = new TileController.Snapshot?[rows, cols];
        var tasks = new List<Task>();
        for (var i = 0; i < _tiles.GetLength(0); i++)
        {
            for (var j = 0; j < _tiles.GetLength(1); j++)
            {
                var (tile, task) = SpawnNormalTile(i, j, 1, snapshot);
                _tiles[i, j] = tile;
                snapshot[i, j] = tile;
                tasks.Add(task);
            }
        }
        return tasks;
    }

    public async Task Hint()
    {
        if (_isHintPlaying) return;

        var snapshot = ToSnapshot();
        for (var i = 0; i < rows; i++)
        {
            for (var j = 0; j < cols; j++)
            {
                if (snapshot[i, j] == null) continue;

                // ÂÏÐÀÂÎ (j + 1)
                if (j + 1 < cols && snapshot[i, j + 1] != null)
                {
                    
                    (snapshot[i, j], snapshot[i, j + 1]) = (snapshot[i, j + 1], snapshot[i, j]);

                    if (MatchEvaluator.Instance.GroupSizeAt(snapshot, new Vector2Int(i,j)) >= 3 ||
                        MatchEvaluator.Instance.GroupSizeAt(snapshot, new Vector2Int(i, j+1)) >= 3)
                    {
                        await AnimateHintAsync(_tiles[i, j], _tiles[i, j + 1]);
                        return;
                    }

                    (snapshot[i, j], snapshot[i, j + 1]) = (snapshot[i, j + 1], snapshot[i, j]);
                }

                // ÂÍÈÇ (i + 1)
                if (i + 1 < rows && snapshot[i + 1, j] != null)
                {
                    (snapshot[i, j], snapshot[i + 1, j]) = (snapshot[i + 1, j], snapshot[i, j]);

                    if (MatchEvaluator.Instance.GroupSizeAt(snapshot, new Vector2Int(i, j)) >= 3 ||
                        MatchEvaluator.Instance.GroupSizeAt(snapshot, new Vector2Int(i+1, j)) >= 3)
                    {
                        await AnimateHintAsync(_tiles[i, j], _tiles[i + 1, j]);
                        return;
                    }

                    (snapshot[i, j], snapshot[i + 1, j]) = (snapshot[i + 1, j], snapshot[i, j]);
                }
            }
        }
    }

    private async Task AnimateHintAsync(TileController tile1, TileController tile2)
    {
        _isHintPlaying = true;
        var pos1 = tile1.GridPosition;
        var pos2 = tile2.GridPosition;

        var forwardMoves = SwapTiles(tile1, pos2, tile2, pos1);
        await Task.WhenAll(forwardMoves);

        await Task.Delay(300);

        var backwardMoves = SwapTiles(tile1, pos1, tile2, pos2);
        await Task.WhenAll(backwardMoves);
        _isHintPlaying = false;
    }

}
