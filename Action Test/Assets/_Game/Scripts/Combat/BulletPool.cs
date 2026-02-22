using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance { get; private set; }

    private readonly Dictionary<int, Queue<BulletProjectile>> _pool = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public BulletProjectile Get(BulletProjectile prefab)
    {
        int key = prefab.GetInstanceID();

        if (_pool.TryGetValue(key, out var q) && q.Count > 0)
        {
            var b = q.Dequeue();
            b.gameObject.SetActive(true);
            return b;
        }

        var created = Instantiate(prefab);
        created.SetPoolKey(key);
        return created;
    }

    public void Return(BulletProjectile bullet)
    {
        bullet.gameObject.SetActive(false);

        if (!_pool.TryGetValue(bullet.PoolKey, out var q))
        {
            q = new Queue<BulletProjectile>(64);
            _pool.Add(bullet.PoolKey, q);
        }

        q.Enqueue(bullet);
    }
}