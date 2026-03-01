using System;
using System.Collections.Generic;
using UnityEngine;

namespace TappyTale
{
    [CreateAssetMenu(menuName = "TappyTale/Waves/Wave Timeline Config", fileName = "WaveTimelineConfig")]
    public sealed class WaveTimelineConfig : ScriptableObject
    {
        [Min(1)] public int WaveCount = 5;

        [Tooltip("Used by the editor window to visualize the end of the timeline if there are no events past this time.")]
        [Min(1f)] public float DefaultWaveDurationSeconds = 20f;

        [Tooltip("Per-wave balancing and triggers. Events reference waves by WaveIndex.")]
        public List<WaveDefinition> Waves = new List<WaveDefinition>();

        [Tooltip("Timeline events: WaveIndex, Prefab, SpawnTime, IsBoss.")]
        public List<WaveTimelineEvent> Events = new List<WaveTimelineEvent>();

        public void EnsureWaveDefinitions()
        {
            if (WaveCount < 1) WaveCount = 1;
            if (Waves == null) Waves = new List<WaveDefinition>();
            while (Waves.Count < WaveCount) Waves.Add(WaveDefinition.CreateDefault(Waves.Count));
            while (Waves.Count > WaveCount) Waves.RemoveAt(Waves.Count - 1);
        }

        public float GetWaveEndTimeSeconds(int waveIndex)
        {
            float max = 0f;
            if (Events == null) return DefaultWaveDurationSeconds;

            for (int i = 0; i < Events.Count; i++)
            {
                var e = Events[i];
                if (e == null) continue;
                if (e.WaveIndex != waveIndex) continue;
                if (e.SpawnTime > max) max = e.SpawnTime;
            }

            return Mathf.Max(DefaultWaveDurationSeconds, max);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            EnsureWaveDefinitions();
        }
#endif
    }

    public enum WaveTriggerType
    {
        Timer = 0,
        KillCount = 1,
        Objective = 2
    }

    [Serializable]
    public sealed class WaveDefinition
    {
        public string Name;

        public WaveTriggerType TriggerType = WaveTriggerType.Timer;

        [Tooltip("Timer trigger: seconds after wave start to auto-complete the wave (if not using kill/objective).")]
        [Min(0f)] public float TriggerTimeSeconds = 20f;

        [Tooltip("KillCount trigger: required kills to complete the wave.")]
        [Min(0)] public int TriggerKillCount = 10;

        [Tooltip("Objective trigger: objective id to complete to finish the wave.")]
        public string TriggerObjectiveId;

        [Header("Balancing Multipliers (Designer)")]
        [Min(0.1f)] public float SpawnTimeScale = 1f;
        [Min(0.1f)] public float EnemySpeedScale = 1f;
        [Min(0.1f)] public float EnemyHealthScale = 1f;
        [Min(0.1f)] public float RewardScale = 1f;

        public static WaveDefinition CreateDefault(int index)
        {
            return new WaveDefinition
            {
                Name = $"Wave {index + 1}",
                TriggerType = WaveTriggerType.Timer,
                TriggerTimeSeconds = 20f,
                TriggerKillCount = 10,
                TriggerObjectiveId = string.Empty,
                SpawnTimeScale = 1f,
                EnemySpeedScale = 1f,
                EnemyHealthScale = 1f,
                RewardScale = 1f
            };
        }
    }

    [Serializable]
    public sealed class WaveTimelineEvent
    {
        [Min(0)] public int WaveIndex = 0;
        public GameObject Prefab;
        [Min(0f)] public float SpawnTime = 0f;
        public bool IsBoss = false;
    }
}
