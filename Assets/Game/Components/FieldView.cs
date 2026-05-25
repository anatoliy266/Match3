using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using static UnityEditor.PlayerSettings;
using static UnityEngine.InputManagerEntry;

public class FieldView : MonoBehaviour
{
    [Req] public Tile Tile;


    private int _rows;
    private int _cols;

    private ObjectPool<Tile> _pool;
    private Tile?[] _visualTiles;


    public void Initialize(LevelSettings data)
    {
        _rows = data.Rows;
        _cols = data.Columns;
        _visualTiles = new Tile[_rows * _cols];
        _pool = new ObjectPool<Tile>(
            () => Instantiate(Tile),
            (tile) => tile.gameObject.SetActive(true),
            (tile) => tile.gameObject.SetActive(false),
            (tile) => Destroy(tile.gameObject),
            true, 100, 1000
        );
    }


    public Tile? GetVisualTileAt(Guid id)
    {
        for (var i = 0; i < _visualTiles.Length; i++)
        {
            if (_visualTiles[i].Id == id) return _visualTiles[i];
        }
        return null;
    }


    public Tile CreateVisualTile(Guid id, TileKind type, Vector2Int pos)
    {
        var tile = _pool.Get();
        tile.Id = id;
        tile.SetType(type);
        tile.transform.position = GetWorldPos(pos);
        _visualTiles[pos.x * _cols + pos.y] = tile;
        return tile;
    }


    public void ClearVisualTile(Guid id)
    {
        for (var i = 0; i < _visualTiles.Length; i++)
        {
            if (id == _visualTiles[i].Id)
            {
                _pool.Release(_visualTiles[i]);
                _visualTiles[i] = null;
            }
        }
    }

    public Vector3 GetWorldPos(Vector2Int v)
    {
        return new Vector3(v.y - _cols / 2, v.x - _rows / 2, 0);
    }
}
