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
    //private Tile?[] _visualTiles;
    private Dictionary<Guid, Tile> _visualTiles;


    public void Initialize(LevelSettings data)
    {
        _rows = data.Rows;
        _cols = data.Columns;
        //_visualTiles = new Tile[_rows * _cols];
        _visualTiles = new Dictionary<Guid, Tile>();
        //_pool = new ObjectPool<Tile>(
        //    () => Instantiate(Tile),
        //    (tile) => tile.gameObject.SetActive(true),
        //    (tile) => tile.gameObject.SetActive(false),
        //    (tile) => Destroy(tile.gameObject),
        //    true, 100, 1000
        //);
        _pool = new ObjectPool<Tile>(
            // Передаем transform текущего FieldView в качестве родителя
            () => Instantiate(Tile, this.transform),
            (tile) => tile.gameObject.SetActive(true),
            (tile) => tile.gameObject.SetActive(false),
            (tile) => Destroy(tile.gameObject),
            true, 100, 1000
        );
    }


    public Tile? GetVisualTileAt(Guid id)
    {
        //for (var i = 0; i < _visualTiles.Length; i++)
        //{
        //    if (_visualTiles[i] is not null && _visualTiles[i].Id == id) return _visualTiles[i];
        //}
        //return null;
        if (_visualTiles.TryGetValue(id, out var tile)) return tile;
        return null;
    }

    public Tile CreateVisualTile(Guid id, TileKind type, Vector2Int from, Vector2Int to)
    {
        var tile = _pool.Get();
        tile.Id = id;
        tile.SetType(type);
        tile.transform.position = GetWorldPos(from);
        //_visualTiles[to.x * _cols + to.y] = tile;
        tile.transform.localScale = Tile.transform.localScale;
        _visualTiles[id] = tile;
        return tile;
    }

    public void ClearVisualTile(Guid id)
    {
        if (_visualTiles.TryGetValue(id, out var tile))
        {
            _pool.Release(tile);
            _visualTiles.Remove(id);
        }
    }

    public Vector3 GetWorldPos(Vector2Int v)
    {
        return new Vector3(v.y - (_cols - 1) / 2f, v.x - (_rows - 1) / 2f, 0);
    }
}
