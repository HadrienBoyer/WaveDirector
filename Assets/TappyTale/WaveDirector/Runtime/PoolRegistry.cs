using System.Collections.Generic;
using UnityEngine;

namespace TappyTale
{
    public sealed class PoolRegistry : MonoBehaviour
    {
        [SerializeField] private Transform poolRoot;
        [SerializeField, Min(0)] private int defaultPrewarmCount = 8;

        private readonly Dictionary<GameObject, ObjectPool> pools = new Dictionary<GameObject, ObjectPool>(64);
        private Transform inactivePoolRoot;

        public static PoolRegistry Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (poolRoot == null)
            {
                var rootGo = new GameObject("Pools");
                rootGo.transform.SetParent(transform, false);
                poolRoot = rootGo.transform;
            }

            if (inactivePoolRoot == null)
            {
                inactivePoolRoot = poolRoot.Find("Pools_Inactive");
                if (inactivePoolRoot == null)
                {
                    var inactiveGo = new GameObject("Pools_Inactive");
                    inactiveGo.transform.SetParent(poolRoot, false);
                    inactiveGo.SetActive(false);
                    inactivePoolRoot = inactiveGo.transform;
                }
            }
        }

        public GameObject Spawn(GameObject prefab)
        {
            if (prefab == null) return null;
            var pool = GetOrCreatePool(prefab);
            return pool.Get();
        }

        public void Despawn(GameObject prefab, GameObject instance)
        {
            if (prefab == null || instance == null)
            {
                if (instance != null) Destroy(instance);
                return;
            }

            if (!pools.TryGetValue(prefab, out var pool))
            {
                Destroy(instance);
                return;
            }

            pool.Release(instance);
        }

        public void Prewarm(GameObject prefab, int count)
        {
            if (prefab == null) return;
            if (count <= 0) return;
            GetOrCreatePool(prefab, count);
        }

        private ObjectPool GetOrCreatePool(GameObject prefab, int overridePrewarm = -1)
        {
            if (pools.TryGetValue(prefab, out var existing)) return existing;

            int prewarm = overridePrewarm >= 0 ? overridePrewarm : defaultPrewarmCount;
            var root = inactivePoolRoot != null ? inactivePoolRoot : poolRoot;
            var pool = new ObjectPool(prefab, prewarm, root);
            pools.Add(prefab, pool);
            return pool;
        }
    }
}
