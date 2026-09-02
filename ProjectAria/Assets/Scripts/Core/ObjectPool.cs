// ============================================================
// ObjectPool.cs
// Generic GameObject pool. Zero-allocation instantiation for
// bullets, particles, items, projectiles, etc.
// Mobile-friendly: prewarmed, expandable, shrinkable.
// ============================================================
using System.Collections.Generic;
using UnityEngine;

namespace ProjectAria.Core
{
    public class ObjectPool : IService
    {
        private readonly Transform _root;
        private readonly Dictionary<int, Queue<GameObject>> _pools = new();
        private readonly Dictionary<int, GameObject> _prefabs = new();
        private readonly Dictionary<int, int> _maxSizes = new();

        public ObjectPool(Transform root = null)
        {
            _root = root != null ? root : new GameObject("[ObjectPool]").transform;
            Object.DontDestroyOnLoad(_root.gameObject);
        }

        /// <summary>Register a prefab and pre-warm a number of instances.</summary>
        public void Register(GameObject prefab, int prewarm = 8, int maxSize = 256)
        {
            if (prefab == null) return;
            int id = prefab.GetInstanceID();
            if (_pools.ContainsKey(id)) return;
            _prefabs[id] = prefab;
            _maxSizes[id] = maxSize;
            _pools[id] = new Queue<GameObject>(prewarm);

            for (int i = 0; i < prewarm; i++)
            {
                var obj = Object.Instantiate(prefab, _root);
                obj.SetActive(false);
                _pools[id].Enqueue(obj);
            }
        }

        public GameObject Spawn(GameObject prefab, Vector3 pos, Quaternion rot, Transform parent = null)
        {
            int id = prefab.GetInstanceID();
            if (!_pools.ContainsKey(id)) Register(prefab);

            var queue = _pools[id];
            GameObject obj = null;
            while (queue.Count > 0)
            {
                obj = queue.Dequeue();
                if (obj == null) continue; // destroyed externally
                break;
            }
            if (obj == null)
            {
                obj = Object.Instantiate(prefab);
            }

            obj.transform.SetParent(parent != null ? parent : _root, false);
            obj.transform.SetPositionAndRotation(pos, rot);
            obj.SetActive(true);
            return obj;
        }

        public void Despawn(GameObject obj)
        {
            if (obj == null) return;
            // Find which pool it belongs to by matching prefab instance
            // (Faster: store pool id on the instance via Pooled component.)
            var pooled = obj.GetComponent<Pooled>();
            if (pooled == null)
            {
                obj.SetActive(false);
                obj.transform.SetParent(_root, false);
                return;
            }
            int id = pooled.PoolId;
            if (_pools.TryGetValue(id, out var queue))
            {
                if (queue.Count >= _maxSizes[id])
                {
                    Object.Destroy(obj);
                    return;
                }
                obj.SetActive(false);
                obj.transform.SetParent(_root, false);
                queue.Enqueue(obj);
            }
        }

        /// <summary>Despawn after a delay.</summary>
        public void DespawnAfter(GameObject obj, float seconds)
        {
            if (obj == null) return;
            var runner = obj.GetComponent<PoolDespawnTimer>();
            if (runner == null) runner = obj.AddComponent<PoolDespawnTimer>();
            runner.Init(this, seconds);
        }

        public void Clear()
        {
            foreach (var kv in _pools)
                while (kv.Value.Count > 0)
                {
                    var o = kv.Value.Dequeue();
                    if (o != null) Object.Destroy(o);
                }
            _pools.Clear();
            _prefabs.Clear();
        }
    }

    /// <summary>Tag a pooled object with its source pool id for fast Despawn.</summary>
    public class Pooled : MonoBehaviour
    {
        public int PoolId { get; private set; }
        public void Bind(int id) => PoolId = id;
    }

    /// <summary>Auto-despawn helper.</summary>
    public class PoolDespawnTimer : MonoBehaviour
    {
        private ObjectPool _pool;
        private float _expiresAt;
        public void Init(ObjectPool pool, float seconds) { _pool = pool; _expiresAt = Time.time + seconds; }
        private void Update() { if (Time.time >= _expiresAt) _pool?.Despawn(gameObject); }
    }
}
