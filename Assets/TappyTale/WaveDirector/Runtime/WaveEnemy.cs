using System;
using UnityEngine;

namespace TappyTale
{
    [DisallowMultipleComponent]
    public sealed class WaveEnemy : MonoBehaviour, IWaveScalable, IPoolable
    {
        [Header("Base Stats")]
        [SerializeField, Min(1f)] private float baseHealth = 10f;
        [SerializeField, Min(0f)] private float baseMoveSpeed = 3f;

        [Header("Runtime (Read Only)")]
        [SerializeField] private float currentHealth;
        [SerializeField] private float currentMoveSpeed;

        public event Action Died;

        private WaveSpawnContext spawnContext;

        private void Awake()
        {
            spawnContext = GetComponent<WaveSpawnContext>();
            ResetStats();
        }

        public void ApplyWaveScaling(in WaveScalingContext context)
        {
            // Multipliers stack: designer multipliers * director difficulty.
            float difficulty = Mathf.Max(0.1f, context.Difficulty);
            currentMoveSpeed = Mathf.Max(0f, baseMoveSpeed * context.EnemySpeedScale * difficulty);
            currentHealth = Mathf.Max(1f, baseHealth * context.EnemyHealthScale * difficulty);
        }

        public void Damage(float amount)
        {
            if (amount <= 0f) return;
            currentHealth -= amount;
            if (currentHealth <= 0f) Die();
        }

        public void Die()
        {
            if (!gameObject.activeInHierarchy) return;
            currentHealth = 0f;

            WaveKillBus.RaiseKilled(new WaveKillEvent(
                spawnContext != null ? spawnContext.WaveIndex : 0,
                spawnContext != null && spawnContext.IsBoss,
                spawnContext != null ? spawnContext.EnemyId : name));

            Died?.Invoke();

            if (PoolRegistry.Instance != null && spawnContext != null && spawnContext.Prefab != null)
            {
                PoolRegistry.Instance.Despawn(spawnContext.Prefab, gameObject);
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void ResetStats()
        {
            currentHealth = baseHealth;
            currentMoveSpeed = baseMoveSpeed;
        }

        public void OnSpawned()
        {
            if (spawnContext == null) spawnContext = GetComponent<WaveSpawnContext>();
            ResetStats();
        }

        public void OnDespawned()
        {
            // Nothing required; kept for extensibility.
        }
    }

    public sealed class WaveKillNotifier : MonoBehaviour
    {
        private WaveSpawnContext spawnContext;

        private void Awake()
        {
            spawnContext = GetComponent<WaveSpawnContext>();
        }

        public void NotifyKilled()
        {
            WaveKillBus.RaiseKilled(new WaveKillEvent(
                spawnContext != null ? spawnContext.WaveIndex : 0,
                spawnContext != null && spawnContext.IsBoss,
                spawnContext != null ? spawnContext.EnemyId : name));
        }
    }

    public static class WaveKillBus
    {
        public static event Action<WaveKillEvent> Killed;

        public static void RaiseKilled(WaveKillEvent e)
        {
            Killed?.Invoke(e);
        }
    }
}
