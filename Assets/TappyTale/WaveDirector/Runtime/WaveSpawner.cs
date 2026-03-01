using System.Collections.Generic;
using UnityEngine;

namespace TappyTale
{
    public sealed class WaveSpawner : MonoBehaviour
    {
        [Header("Spawn Points")]
        [SerializeField] private Transform[] spawnPoints;

        [Header("Selection")]
        [SerializeField] private bool randomizeSpawnPoint = true;

        [Header("Identification")]
        [SerializeField] private string spawnerId = "Spawner";

        private int roundRobinIndex;

        public string SpawnerId => spawnerId;

        public void EnsureSpawnPoints()
        {
            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                spawnPoints = new[] { transform };
            }
        }

        private void OnValidate()
        {
            EnsureSpawnPoints();
        }

        public GameObject Spawn(in WaveTimelineEvent e, in WaveScalingContext scalingContext)
        {
            if (e == null || e.Prefab == null) return null;
            EnsureSpawnPoints();

            var point = PickSpawnPoint(e.IsBoss);
            if (point == null) point = transform;

            var instance = PoolRegistry.Instance != null ? PoolRegistry.Instance.Spawn(e.Prefab) : Instantiate(e.Prefab);
            if (instance == null) return null;

            instance.transform.SetPositionAndRotation(point.position, point.rotation);

            var context = instance.GetComponent<WaveSpawnContext>();
            if (context == null) context = instance.AddComponent<WaveSpawnContext>();
            context.Configure(e.WaveIndex, e.IsBoss, e.Prefab);

            ApplyScaling(instance, scalingContext);

            return instance;
        }

        private Transform PickSpawnPoint(bool isBoss)
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return transform;

            if (isBoss) return spawnPoints[0];

            if (randomizeSpawnPoint)
            {
                int index = Random.Range(0, spawnPoints.Length);
                return spawnPoints[index];
            }

            roundRobinIndex = (roundRobinIndex + 1) % spawnPoints.Length;
            return spawnPoints[roundRobinIndex];
        }

        private static void ApplyScaling(GameObject instance, in WaveScalingContext context)
        {
            var scalables = instance.GetComponentsInChildren<IWaveScalable>(true);
            for (int i = 0; i < scalables.Length; i++)
            {
                scalables[i].ApplyWaveScaling(context);
            }
        }
    }

    public sealed class WaveSpawnContext : MonoBehaviour
    {
        [field: SerializeField] public int WaveIndex { get; private set; }
        [field: SerializeField] public bool IsBoss { get; private set; }
        [field: SerializeField] public string EnemyId { get; private set; }

        private GameObject prefab;

        public GameObject Prefab => prefab;

        public void Configure(int waveIndex, bool isBoss, GameObject prefab)
        {
            WaveIndex = Mathf.Max(0, waveIndex);
            IsBoss = isBoss;
            this.prefab = prefab;
            EnemyId = prefab != null ? prefab.name : string.Empty;
        }
    }
}
