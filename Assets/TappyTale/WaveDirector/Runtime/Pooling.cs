using System.Collections.Generic;
using UnityEngine;

namespace TappyTale
{
    public interface IPoolable
    {
        void OnSpawned();
        void OnDespawned();
    }

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
                PrepareForPool(go);
                pool.Enqueue(go);
            }
        }

        public GameObject Get()
        {
            if (prefab == null) return null;

            GameObject go = null;
            while (pool.Count > 0 && go == null)
            {
                go = pool.Dequeue();
            }

            if (go == null)
            {
                go = Object.Instantiate(prefab, root);
                PrepareForPool(go);
            }

            go.transform.SetParent(null, true);
            go.SetActive(true);

            var poolables = go.GetComponentsInChildren<IPoolable>(true);
            for (int i = 0; i < poolables.Length; i++) poolables[i].OnSpawned();

            return go;
        }

        public void Release(GameObject go)
        {
            if (go == null) return;

            PrepareForPool(go);
            go.transform.SetParent(root, false);
            pool.Enqueue(go);
        }

        private static void PrepareForPool(GameObject go)
        {
            if (go == null) return;

            var poolables = go.GetComponentsInChildren<IPoolable>(true);
            for (int i = 0; i < poolables.Length; i++) poolables[i].OnDespawned();

            go.SetActive(false);
        }
    }
}
