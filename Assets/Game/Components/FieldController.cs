

using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

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
    [Req] public SpawnRules SpawnRules;
    [Req] public DragManager DragManager;
    [Req] public AnimationManager AnimationManager;
    [Req] public MatchEvaluator MatchEvaluator;
    [Req] public SpawnEvaluator SpawnEvaluator;
    [Req] public ScoreManager ScoreManager;

    private TileController[,] _tiles;


    private void Awake()
    {
        _tiles = new TileController[Rows, Cols];
    }


    public TileController.Snapshot?[,] ToSnapshot()
    {
        var board = new TileController.Snapshot?[Rows, Cols];
        for (var r = 0; r < Rows; r++)
            for (var c = 0; c < Cols; c++)
                board[r, c] = _tiles[r, c];
        return board;
    }


    public Vector3 GetWorldPos(int row, int col)
    {
        return new Vector3(col - Cols / 2, row - Rows / 2, 0);
    }
    public Vector3 GetWorldPos(Vector2Int v)
    {
        return new Vector3(v.y - Cols / 2, v.x - Rows / 2, 0);
    }

    
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


    public void RemoveTiles(IEnumerable<Vector2Int> group)
    {
        foreach (var tile in group)
        {
            RemoveTileAt(tile);
        }
    }


    public List<CompactInfo> CompactBoard()
    {
        var fallCommands = new List<CompactInfo>();

        for (var col = 0; col < Cols; col++)
        {
            var (slow, fast) = (0, 0);
            while (fast < Rows)
            {
                if (_tiles[fast, col] != null)
                {
                    if (fast > slow)
                    {
                        var tile = _tiles[fast, col];
                        var targetPos = new Vector2Int(slow, col);

                        _tiles[slow, col] = tile;
                        _tiles[fast, col] = null;
                        tile.GridPosition = targetPos;

                        fallCommands.Add(new CompactInfo
                        {
                            Tile = tile,
                            TargetPos = targetPos
                        });
                    }
                    slow++;
                }
                fast++;
            }
        }
        return fallCommands; 
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


    public TileController SpawnTile(SpawnInfo info, TileController.Snapshot?[,] snapshot, int cycle)
    {
        Vector3 spawnWorldPos;
        TileType type;
        if (info.IsBonus)
        {
            spawnWorldPos = GetWorldPos(info.Position.x, info.Position.y);
            type = info.Type;
        }
        else
        {
            var topCellPos = GetWorldPos(Rows - 1, info.Position.y);
            spawnWorldPos = new Vector3(topCellPos.x, topCellPos.y + info.Offset, 0);

            //нужно выбрать тип таким образом чтобы он с уменьшающимся от cycle шансом мог заспаунить бонус
            type = (TileType)UnityEngine.Random.Range(0, 6);
        }
        return SpawnTileAt(info.Position, spawnWorldPos, type, info.IsBonus);
    }


    public void FillEmptyTilesOnGrid(int cycle)
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

                    _tiles[row, col] = SpawnTile(info, snapshot, cycle);
                    snapshot[row, col] = _tiles[row, col];
                    spawnOffset++;
                }
            }
        }
    }

    public void UpdateTileOnGrid(Vector2Int pos, TileController tile)
    {
        _tiles[pos.x, pos.y] = tile;
    }
}

