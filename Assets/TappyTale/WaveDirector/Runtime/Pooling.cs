using System.Collections.Generic;
using UnityEngine;

namespace TappyTale.WaveDirector.Runtime
{
    public interface IPoolable { void OnSpawned(); void OnDespawned(); }

    public sealed class ObjectPool
    {
        private readonly GameObject prefab;
        private readonly Transform root;
        private readonly Queue<GameObject> pool = new Queue<GameObject>(64);

        public ObjectPool(GameObject prefab, int prewarmCount, Transform root)
        {
            this.prefab = prefab;
            this.root = root;

            if (prefab == null) return;
            for (int i = 0; i < prewarmCount; i++)
            {
                var go = Object.Instantiate(prefab, root);
                go.SetActive(false);
                pool.Enqueue(go);
            }
        }

        public GameObject Get()
        {
            if (prefab == null) return null;

            GameObject go = null;
            while (pool.Count > 0 && go == null) go = pool.Dequeue();
            if (go == null) go = Object.Instantiate(prefab, root);

            go.transform.SetParent(null, true);
            go.SetActive(true);

            var poolables = go.GetComponentsInChildren<IPoolable>(true);
            for (int i = 0; i < poolables.Length; i++) poolables[i].OnSpawned();

            return go;
        }

        public void Release(GameObject go)
        {
            if (go == null) return;

            var poolables = go.GetComponentsInChildren<IPoolable>(true);
            for (int i = 0; i < poolables.Length; i++) poolables[i].OnDespawned();

            go.SetActive(false);
            go.transform.SetParent(root, false);
            pool.Enqueue(go);
        }
    }

    [DisallowMultipleComponent]
    public sealed class PoolRegistry : MonoBehaviour
    {
        [SerializeField] private Transform poolRoot;
        [SerializeField, Min(0)] private int defaultPrewarmCount = 8;

        private readonly Dictionary<GameObject, ObjectPool> pools = new Dictionary<GameObject, ObjectPool>(64);
        public static PoolRegistry Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (poolRoot == null)
            {
                var rootGo = new GameObject("Pools");
                rootGo.transform.SetParent(transform, false);
                poolRoot = rootGo.transform;
            }
        }

        public GameObject Spawn(GameObject prefab)
        {
            if (prefab == null) return null;
            return GetOrCreatePool(prefab).Get();
        }

        public void Despawn(GameObject prefab, GameObject instance)
        {
            if (prefab == null || instance == null) { if (instance != null) Destroy(instance); return; }
            if (!pools.TryGetValue(prefab, out var pool)) { Destroy(instance); return; }
            pool.Release(instance);
        }

        private ObjectPool GetOrCreatePool(GameObject prefab)
        {
            if (pools.TryGetValue(prefab, out var existing)) return existing;
            var pool = new ObjectPool(prefab, defaultPrewarmCount, poolRoot);
            pools.Add(prefab, pool);
            return pool;
        }
    }
}
