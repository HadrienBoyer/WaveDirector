using UnityEngine;

namespace TappyTale.WaveDirector.Runtime
{
    public enum DifficultyMode { Aggressive = 0, Balanced = 1, Adaptive = 2 }

    [CreateAssetMenu(menuName = "Tappy Tale/Wave Editor/Wave Director Config", fileName = "WaveDirectorConfig")]
    public sealed class WaveDirectorConfig : ScriptableObject
    {
        public DifficultyMode Mode = DifficultyMode.Adaptive;

        [Min(0.1f)] public float BaseDifficulty = 1f;
        [Min(0.1f)] public float MinDifficulty = 0.5f;
        [Min(0.1f)] public float MaxDifficulty = 3.0f;

        [Min(0f)] public float AggressiveWaveRamp = 0.15f;

        [Min(1f)] public float TargetClearTimeSeconds = 25f;
        [Min(0f)] public float TargetDamageTaken = 15f;
        [Min(0f)] public float TargetDeaths = 0f;

        [Range(0.01f, 2f)] public float AdaptiveGain = 0.35f;
        [Range(0f, 0.99f)] public float AdaptiveSmoothing = 0.75f;
        [Min(0f)] public float MaxDeltaPerWave = 0.35f;
    }
}
