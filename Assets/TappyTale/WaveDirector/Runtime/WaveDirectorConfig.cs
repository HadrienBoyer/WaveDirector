using UnityEngine;

namespace TappyTale
{
    public enum DifficultyMode
    {
        Aggressive = 0,
        Balanced = 1,
        Adaptive = 2
    }

    [CreateAssetMenu(menuName = "TappyTale/Waves/Wave Director Config", fileName = "WaveDirectorConfig")]
    public sealed class WaveDirectorConfig : ScriptableObject
    {
        public DifficultyMode Mode = DifficultyMode.Adaptive;

        [Header("Global Difficulty")]
        [Min(0.1f)] public float BaseDifficulty = 1f;
        [Min(0.1f)] public float MinDifficulty = 0.5f;
        [Min(0.1f)] public float MaxDifficulty = 3.0f;

        [Tooltip("How quickly difficulty changes over time/waves in Aggressive mode.")]
        [Min(0f)] public float AggressiveWaveRamp = 0.15f;

        [Header("Adaptive Targets")]
        [Tooltip("Target clear time per wave (seconds). Faster clears => ramp up, slower => ease off.")]
        [Min(1f)] public float TargetClearTimeSeconds = 25f;

        [Tooltip("Expected damage taken per wave. More damage => ease off, less => ramp up.")]
        [Min(0f)] public float TargetDamageTaken = 15f;

        [Tooltip("Expected player deaths per wave. Deaths => strongly ease off.")]
        [Min(0f)] public float TargetDeaths = 0f;

        [Header("Adaptive Response")]
        [Tooltip("How strongly the adaptive controller reacts.")]
        [Range(0.01f, 2f)] public float AdaptiveGain = 0.35f;

        [Tooltip("Smoothing factor for difficulty changes (0 = no smoothing, 1 = very slow).")]
        [Range(0f, 0.99f)] public float AdaptiveSmoothing = 0.75f;

        [Tooltip("Clamp the difficulty change per wave to avoid spikes.")]
        [Min(0f)] public float MaxDeltaPerWave = 0.35f;
    }
}
