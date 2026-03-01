using UnityEngine;

namespace TappyTale.WaveDirector.Runtime
{
    public sealed class WaveSpawner : MonoBehaviour
    {
        [SerializeField] private Transform[] spawnPoints;
        [SerializeField] private bool randomizeSpawnPoint = true;

        private int roundRobinIndex;

        public void EnsureSpawnPoints()
        {
            if (spawnPoints == null || spawnPoints.Length == 0) spawnPoints = new[] { transform };
        }

        private void OnValidate() => EnsureSpawnPoints();

        public GameObject Spawn(in WaveTimelineEvent e, in WaveScalingContext scalingContext)
        {
            if (e == null || e.Prefab == null) return null;
            EnsureSpawnPoints();

            Transform point = PickSpawnPoint(e.IsBoss);

            var instance = PoolRegistry.Instance != null ? PoolRegistry.Instance.Spawn(e.Prefab) : Instantiate(e.Prefab);
            if (instance == null) return null;

            instance.transform.SetPositionAndRotation(point.position, point.rotation);

            var ctx = instance.GetComponent<WaveSpawnContext>();
            if (ctx == null) ctx = instance.AddComponent<WaveSpawnContext>();
            ctx.Configure(e.WaveIndex, e.IsBoss, e.Prefab);

            var scalables = instance.GetComponentsInChildren<IWaveScalable>(true);
            for (int i = 0; i < scalables.Length; i++) scalables[i].ApplyWaveScaling(scalingContext);

            return instance;
        }

        private Transform PickSpawnPoint(bool isBoss)
        {
            if (spawnPoints == null || spawnPoints.Length == 0) return transform;
            if (isBoss) return spawnPoints[0];

            if (randomizeSpawnPoint) return spawnPoints[Random.Range(0, spawnPoints.Length)];

            roundRobinIndex = (roundRobinIndex + 1) % spawnPoints.Length;
            return spawnPoints[roundRobinIndex];
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
