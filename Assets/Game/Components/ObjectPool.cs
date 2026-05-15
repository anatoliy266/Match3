using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool SharedInstance;
    private Queue<TileController> _pool;
    public TileController Prefab;
    public int amountToPool;

    void Awake()
    {
        if (SharedInstance != null && SharedInstance != this)
        {
            Destroy(gameObject);
            return;
        }
        SharedInstance = this;

        _pool = new Queue<TileController>();
    }
    void Start()
    {
        for (int i = 0; i < amountToPool; i++)
        {
            CreateNewInstance(false);
        }
    }

    public TileController GetObject() 
    {
        if (_pool.Count > 0)
        {
            var obj = _pool.Dequeue();
            obj.gameObject.SetActive(true);
            return obj;
        }
        return CreateNewInstance(true);
    }

    public void ReturnObject(TileController tile)
    {
        tile.Reset();
        tile.gameObject.SetActive(false);
        _pool.Enqueue(tile);
    }

    

    private TileController CreateNewInstance(bool activate = false)
    {
        var instance = Instantiate(Prefab, transform);
        instance.gameObject.SetActive(activate);

        if (!activate)
        {
            _pool.Enqueue(instance);
        }

        return instance;
    }
}
