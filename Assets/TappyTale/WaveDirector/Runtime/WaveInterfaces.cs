using UnityEngine;

namespace TappyTale.WaveDirector.Runtime
{
    public interface IWaveScalable { void ApplyWaveScaling(in WaveScalingContext context); }

    public readonly struct WaveScalingContext
    {
        public readonly int WaveIndex;
        public readonly float Difficulty;
        public readonly float EnemySpeedScale;
        public readonly float EnemyHealthScale;
        public readonly float RewardScale;
        public readonly bool IsBoss;

        public WaveScalingContext(int waveIndex, float difficulty, float enemySpeedScale, float enemyHealthScale, float rewardScale, bool isBoss)
        {
            WaveIndex = waveIndex;
            Difficulty = difficulty;
            EnemySpeedScale = enemySpeedScale;
            EnemyHealthScale = enemyHealthScale;
            RewardScale = rewardScale;
            IsBoss = isBoss;
        }
    }

    public interface IPlayerPerformanceProvider { PlayerPerformanceSnapshot GetSnapshot(); }

    public readonly struct PlayerPerformanceSnapshot
    {
        public readonly float DamageTakenThisWave;
        public readonly int DeathsThisWave;

        public PlayerPerformanceSnapshot(float damageTakenThisWave, int deathsThisWave)
        {
            DamageTakenThisWave = Mathf.Max(0f, damageTakenThisWave);
            DeathsThisWave = Mathf.Max(0, deathsThisWave);
        }
    }

    public interface IWaveRewardHandler { void OnWaveCompleted(int waveIndex, float rewardScale); }

    public readonly struct WaveKillEvent
    {
        public readonly int WaveIndex;
        public readonly bool IsBoss;
        public readonly string EnemyId;

        public WaveKillEvent(int waveIndex, bool isBoss, string enemyId)
        {
            WaveIndex = waveIndex;
            IsBoss = isBoss;
            EnemyId = enemyId ?? string.Empty;
        }
    }
}
