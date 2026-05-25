//using System.Collections.Generic;
//using UnityEngine;
//using UnityEngine.Pool;

//public class ObjectPool : MonoBehaviour
//{
//    public static ObjectPool SharedInstance;
//    private Queue<Tile> _pool;
//    public Tile Prefab;
//    public int amountToPool;

//    void Awake()
//    {
//        if (SharedInstance != null && SharedInstance != this)
//        {
//            Destroy(gameObject);
//            return;
//        }
//        SharedInstance = this;

//        _pool = new Queue<Tile>();
//    }
//    void Start()
//    {
//        for (int i = 0; i < amountToPool; i++)
//        {
//            CreateNewInstance(false);
//        }
//    }

//    public Tile GetObject() 
//    {
//        if (_pool.Count > 0)
//        {
//            var obj = _pool.Dequeue();
//            obj.gameObject.SetActive(true);
//            return obj;
//        }
//        return CreateNewInstance(true);
//    }

//    public void ReturnObject(Tile tile)
//    {
//        tile.Reset();
//        tile.gameObject.SetActive(false);
//        _pool.Enqueue(tile);
//    }

    

//    private Tile CreateNewInstance(bool activate = false)
//    {
//        var instance = Instantiate(Prefab, transform);
//        instance.gameObject.SetActive(activate);

//        if (!activate)
//        {
//            _pool.Enqueue(instance);
//        }

//        return instance;
//    }
//}
