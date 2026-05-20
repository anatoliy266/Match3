

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

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
    private int _rows = 1;
    private int _cols = 1;
    [Req] public TileController Tile;
    [Req] public TileTypeData TileTypeData;
    [Req] public MatchEvaluator MatchEvaluator;
    [Req] public SpawnEvaluator SpawnEvaluator;


    private TileController[,] _tiles;
    private UnityEngine.Pool.ObjectPool<TileController> _tilePool;


    public void Initialize(LevelSettings data)
    {
        // 1. Принимаем данные из ScriptableObject
        _rows = data.Rows;
        _cols = data.Columns;

        _tilePool = new ObjectPool<TileController>(createFunc: () => Instantiate(Tile),
            actionOnGet: (tile) => tile.gameObject.SetActive(true),
            actionOnRelease: (tile) => tile.gameObject.SetActive(false),
            actionOnDestroy: (tile) => Destroy(tile.gameObject),
            collectionCheck: true,
            defaultCapacity: 10,
            maxSize: _rows * _cols);

        _tiles = new TileController[_rows, _cols];
    }


    public TileController.Snapshot?[,] ToSnapshot()
    {
        var board = new TileController.Snapshot?[_rows, _cols];
        for (var r = 0; r < _rows; r++)
            for (var c = 0; c < _cols; c++)
                board[r, c] = _tiles[r, c];
        return board;
    }


    public Vector3 GetWorldPos(int row, int col)
    {
        return new Vector3(col - _cols / 2, row - _rows / 2, 0);
    }
    public Vector3 GetWorldPos(Vector2Int v)
    {
        return new Vector3(v.y - _cols / 2, v.x - _rows / 2, 0);
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

        for (var col = 0; col < _cols; col++)
        {
            var (slow, fast) = (0, 0);
            while (fast < _rows)
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
        //var tile = ObjectPool.SharedInstance.GetObject();
        var tile = _tilePool.Get();
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
            var topCellPos = GetWorldPos(_rows - 1, info.Position.y);
            spawnWorldPos = new Vector3(topCellPos.x, topCellPos.y + info.Offset, 0);

            //нужно выбрать тип таким образом чтобы он с уменьшающимся от cycle шансом мог заспаунить бонус
            type = (TileType)UnityEngine.Random.Range(0, 6);
        }
        return SpawnTileAt(info.Position, spawnWorldPos, type, info.IsBonus);
    }


    public void FillEmptyTilesOnGrid(int cycle)
    {
        var snapshot = ToSnapshot();


        for (var col = 0; col < _cols; col++)
        {
            int spawnOffset = 1;

            for (var row = 0; row < _rows; row++)
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

    internal Vector2Int GetBounds() => new Vector2Int(_rows, _cols);
}

