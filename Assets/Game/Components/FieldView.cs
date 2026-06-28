using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;

public class FieldView : MonoBehaviour
{
    [Req] public Tile Tile;

    private int _rows;
    private int _cols;

    private ObjectPool<Tile> _pool;
    private Dictionary<Guid, Tile> _visualTiles;

    public void Initialize(LevelSettings data)
    {
        _rows = data.Rows;
        _cols = data.Columns;
        _visualTiles = new Dictionary<Guid, Tile>();

        _pool = new ObjectPool<Tile>(
            () => Instantiate(Tile, this.transform),
            (tile) => tile.gameObject.SetActive(true),
            (tile) => tile.gameObject.SetActive(false),
            (tile) => Destroy(tile.gameObject),
            true, 100, 1000
        );
    }


    public Tile GetVisualTileAt(Guid id)
    {
        if (_visualTiles.TryGetValue(id, out var tile)) return tile;
        return null;
    }

    public Tile CreateVisualTile(Guid id, TileKind type, Vector2Int from, Vector2Int to)
    {
        var tile = _pool.Get();
        tile.Id = id;
        tile.SetData(type);
        tile.transform.position = GetWorldPos(from);
        tile.transform.localScale = Tile.transform.localScale;
        _visualTiles[id] = tile;
        return tile;
    }

    public void ClearVisualTile(Guid id)
    {
        if (_visualTiles.TryGetValue(id, out var tile))
        {
            tile.CleanData();
            _pool.Release(tile);
            _visualTiles.Remove(id);
        }
    }

    public Vector3 GetWorldPos(Vector2Int v)
    {
        return new Vector3(v.y - (_cols - 1) / 2f, v.x - (_rows - 1) / 2f, 0);
    }
}
