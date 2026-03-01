using System;
using UnityEngine;

namespace TappyTale.WaveDirector.Runtime
{
    public sealed class WaveEnemy : MonoBehaviour, IWaveScalable, IPoolable
    {
        [SerializeField, Min(1f)] private float baseHealth = 10f;
        [SerializeField, Min(0f)] private float baseMoveSpeed = 3f;

        [SerializeField] private float currentHealth;
        [SerializeField] private float currentMoveSpeed;

        private WaveSpawnContext spawnContext;

        public event Action Died;

        private void Awake()
        {
            spawnContext = GetComponent<WaveSpawnContext>();
            ResetStats();
        }

        public void ApplyWaveScaling(in WaveScalingContext context)
        {
            float d = Mathf.Max(0.1f, context.Difficulty);
            currentMoveSpeed = Mathf.Max(0f, baseMoveSpeed * context.EnemySpeedScale * d);
            currentHealth = Mathf.Max(1f, baseHealth * context.EnemyHealthScale * d);
        }

        public void Damage(float amount)
        {
            if (amount <= 0f) return;
            currentHealth -= amount;
            if (currentHealth <= 0f) Die();
        }

        public void Die()
        {
            WaveKillBus.RaiseKilled(new WaveKillEvent(
                spawnContext != null ? spawnContext.WaveIndex : 0,
                spawnContext != null && spawnContext.IsBoss,
                spawnContext != null ? spawnContext.EnemyId : name));

            Died?.Invoke();

            if (PoolRegistry.Instance != null && spawnContext != null && spawnContext.Prefab != null)
                PoolRegistry.Instance.Despawn(spawnContext.Prefab, gameObject);
            else
                Destroy(gameObject);
        }

        private void ResetStats()
        {
            currentHealth = baseHealth;
            currentMoveSpeed = baseMoveSpeed;
        }

        public void OnSpawned() => ResetStats();
        public void OnDespawned() { }
    }

    public static class WaveKillBus
    {
        public static event Action<WaveKillEvent> Killed;
        public static void RaiseKilled(WaveKillEvent e) => Killed?.Invoke(e);
    }
}
