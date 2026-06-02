

using System;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Pool;

using static UnityEngine.Rendering.DebugUI;

public struct LogicalTile
{
    public Guid Id {  get; set; }
    public TileKind Type { get; set; }
}

public class Field : MonoBehaviour
{
    private int _rows = 1;
    private int _cols = 1;

    private LogicalTile?[,] _logicalTiles;

    public void Initialize(LevelSettings data)
    {
        _rows = data.Rows;
        _cols = data.Columns;
        _logicalTiles = new LogicalTile?[_rows, _cols];
    }


    public LogicalTile?[,] ToSnapshot()
    {
        var board = new LogicalTile?[_rows, _cols];
        for (var r = 0; r < _rows; r++)
            for (var c = 0; c < _cols; c++)
                board[r, c] = _logicalTiles[r, c];
        return board;
    }

    public void ToPositionChache(Dictionary<Guid, Vector2Int> positionsCache)
    {
        for (int r = 0; r < _rows; r++)
        {
            for (int c = 0; c < _cols; c++)
            {
                if (_logicalTiles[r,c] is not null)
                {
                    positionsCache[_logicalTiles[r, c].Value.Id] = new Vector2Int(r,c);
                }
            }
        }
    }

    public Vector2Int GetBounds() => new Vector2Int(_rows, _cols);

    public bool IsInBounds(Vector2Int pos) => pos.x >= 0 && pos.x < _rows && pos.y >= 0 && pos.y < _cols;

    public LogicalTile? GetTileAt(Vector2Int pos) => _logicalTiles[pos.x, pos.y];

    public LogicalTile? GetTileAt(Guid sourceId)
    {
        for (var i = 0 ; i < _rows; i++)
        {
            for (var j = 0 ; j < _cols; j++)
            {
                if (_logicalTiles[i, j] is not null && _logicalTiles[i, j].Value.Id == sourceId) return _logicalTiles[i, j];
            }
        }
        return null;
    }

    public bool TryGetPosition(Guid sourceId, out Vector2Int pos)
    {
        for (var i = 0; i < _rows; i++)
        {
            for (var j = 0; j < _cols; j++)
            {
                if (_logicalTiles[i, j] is not null && _logicalTiles[i, j].Value.Id == sourceId)
                {
                    pos = new Vector2Int(i, j);
                    return true;
                }
            }
        }
        pos = default;
        return false;
    }


    public void SetTileAt(Vector2Int pos, LogicalTile? tile) => _logicalTiles[pos.x, pos.y] = tile;

    public void ClearTileAt(Vector2Int pos) => _logicalTiles[pos.x, pos.y] = null;

    public Guid GenerateUniqueId() => Guid.NewGuid();

    public Vector3 GetWorldPos(int row, int col) => new Vector3(col - (_cols - 1) / 2f, row - (_rows - 1) / 2f, 0);

    public Vector3 GetWorldPos(Vector2Int v) => new Vector3(v.y - (_cols - 1) / 2f, v.x - (_rows - 1) / 2f, 0);
}

