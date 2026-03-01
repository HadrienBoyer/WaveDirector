using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TappyTale.WaveDirector.Runtime
{
    public enum SpawnBroadcastMode { RoundRobin = 0, SpawnOnAllSpawners = 1 }

    [DisallowMultipleComponent]
    public sealed class WaveDirector : MonoBehaviour
    {
        [Header("Configs")]
        [SerializeField] private WaveTimelineConfig timelineConfig;
        [SerializeField] private WaveDirectorConfig directorConfig;

        [Header("Spawners")]
        [SerializeField] private List<WaveSpawner> spawners = new List<WaveSpawner>();
        [SerializeField] private SpawnBroadcastMode spawnBroadcastMode = SpawnBroadcastMode.RoundRobin;

        [Header("Objective Trigger (Optional)")]
        [SerializeField] private WaveObjectiveTracker objectiveTracker;

        [Header("Adaptive Input (Optional)")]
        [SerializeField] private MonoBehaviour playerPerformanceProviderBehaviour;

        [Header("Rewards (Optional)")]
        [SerializeField] private MonoBehaviour rewardHandlerBehaviour;

        [Header("Save & Analytics")]
        [SerializeField] private bool enableSave = true;
        [SerializeField] private bool enableAnalytics = true;

        [Header("Runtime (Read Only)")]
        [SerializeField] private int currentWaveIndex;
        [SerializeField] private float currentDifficulty = 1f;
        [SerializeField] private int killsThisWave;

        public event Action<int> WaveStarted;
        public event Action<int> WaveCompleted;

        private IPlayerPerformanceProvider playerPerformanceProvider;
        private IWaveRewardHandler rewardHandler;

        private string timelineGuid;
        private string savePath;
        private string analyticsPath;

        private float adaptiveDifficulty;
        private bool isRunning;
        private Coroutine runRoutine;

        private int spawnerRoundRobin;
        private float waveStartTime;

        private int dataTotalKills;
        private float dataTotalTime;

        private void Awake()
        {
            if (timelineConfig != null) timelineConfig.EnsureWaveDefinitions();

            playerPerformanceProvider = playerPerformanceProviderBehaviour as IPlayerPerformanceProvider;
            rewardHandler = rewardHandlerBehaviour as IWaveRewardHandler;

            EnsurePoolRegistry();

            timelineGuid = timelineConfig != null ? timelineConfig.TimelineGuid : "NoTimeline";
            savePath = WaveSaveSystem.GetSavePath(timelineConfig != null ? timelineConfig.name : "Timeline", timelineGuid);
            analyticsPath = WaveAnalyticsLogger.GetAnalyticsPath(timelineConfig != null ? timelineConfig.name : "Timeline", timelineGuid);

            adaptiveDifficulty = directorConfig != null ? directorConfig.BaseDifficulty : 1f;
        }

        private void OnEnable() => WaveKillBus.Killed += OnKilled;
        private void OnDisable() => WaveKillBus.Killed -= OnKilled;

        private void Start()
        {
            if (timelineConfig == null)
            {
                Debug.LogError("[WaveDirector] Missing TimelineConfig.");
                return;
            }

            if (enableSave) TryLoadProgress();
            StartRunning();
        }

        public void StartRunning()
        {
            if (isRunning) return;
            isRunning = true;
            runRoutine = StartCoroutine(RunWaves());
        }

        public void StopRunning()
        {
            if (!isRunning) return;
            isRunning = false;
            if (runRoutine != null) StopCoroutine(runRoutine);
            runRoutine = null;
        }

        public void StartPreviewWave(int waveIndex)
        {
            if (!Application.isPlaying) return;
            StopAllCoroutines();
            currentWaveIndex = Mathf.Clamp(waveIndex, 0, Mathf.Max(0, timelineConfig.WaveCount - 1));
            isRunning = true;
            runRoutine = StartCoroutine(RunSingleWavePreview(currentWaveIndex));
        }

        private IEnumerator RunWaves()
        {
            currentWaveIndex = Mathf.Clamp(currentWaveIndex, 0, timelineConfig.WaveCount);

            while (isRunning && currentWaveIndex < timelineConfig.WaveCount)
            {
                int wave = currentWaveIndex;
                yield return RunWave(wave);
                currentWaveIndex = wave + 1;
            }
        }

        private IEnumerator RunSingleWavePreview(int waveIndex) => RunWave(waveIndex);

        private IEnumerator RunWave(int waveIndex)
        {
            if (timelineConfig == null) yield break;

            var waveDef = (timelineConfig.Waves != null && waveIndex < timelineConfig.Waves.Count)
                ? timelineConfig.Waves[waveIndex]
                : WaveDefinition.CreateDefault(waveIndex);

            killsThisWave = 0;
            waveStartTime = Time.time;

            if (playerPerformanceProviderBehaviour is BasicPlayerPerformanceProvider perf) perf.ResetWave();

            currentDifficulty = ComputeDifficultyForWave(waveIndex);

            WaveStarted?.Invoke(waveIndex);

            yield return SpawnWaveEventsAsync(waveIndex, waveDef);
            yield return WaitForWaveCompletion(waveDef);

            float clearTime = Mathf.Max(0.01f, Time.time - waveStartTime);

            ApplyAdaptiveUpdate(clearTime);

            rewardHandler?.OnWaveCompleted(waveIndex, waveDef.RewardScale * currentDifficulty);
            WaveCompleted?.Invoke(waveIndex);

            if (enableAnalytics)
            {
                var snapshot = playerPerformanceProvider != null ? playerPerformanceProvider.GetSnapshot() : new PlayerPerformanceSnapshot(0f, 0);
                WaveAnalyticsLogger.Append(analyticsPath, new WaveAnalyticsEntry
                {
                    TimelineGuid = timelineGuid,
                    WaveIndex = waveIndex,
                    Difficulty = currentDifficulty,
                    Kills = killsThisWave,
                    ClearTimeSeconds = clearTime,
                    DamageTaken = snapshot.DamageTakenThisWave,
                    Deaths = snapshot.DeathsThisWave
                });
            }

            if (enableSave) SaveProgress(waveIndex + 1, clearTime);
        }

        private IEnumerator SpawnWaveEventsAsync(int waveIndex, WaveDefinition waveDef)
        {
            EnsureSpawners();

            var events = new List<WaveTimelineEvent>(32);
            if (timelineConfig.Events != null)
            {
                for (int i = 0; i < timelineConfig.Events.Count; i++)
                {
                    var e = timelineConfig.Events[i];
                    if (e != null && e.WaveIndex == waveIndex) events.Add(e);
                }
            }

            events.Sort((a, b) => a.SpawnTime.CompareTo(b.SpawnTime));

            float timeScale = Mathf.Max(0.01f, waveDef.SpawnTimeScale);
            float last = 0f;

            for (int i = 0; i < events.Count; i++)
            {
                var e = events[i];
                if (e.Prefab == null) continue;

                float t = Mathf.Max(0f, e.SpawnTime) * timeScale;
                float wait = Mathf.Max(0f, t - last);
                last = t;

                float end = Time.time + wait;
                while (Time.time < end) yield return null;

                SpawnEvent(e, waveDef);
                yield return null; // pacing
            }
        }

        private IEnumerator WaitForWaveCompletion(WaveDefinition waveDef)
        {
            switch (waveDef.TriggerType)
            {
                case WaveTriggerType.Timer:
                {
                    float end = Time.time + Mathf.Max(0f, waveDef.TriggerTimeSeconds);
                    while (Time.time < end) yield return null;
                    yield break;
                }
                case WaveTriggerType.KillCount:
                {
                    int target = Mathf.Max(0, waveDef.TriggerKillCount);
                    while (killsThisWave < target) yield return null;
                    yield break;
                }
                case WaveTriggerType.Objective:
                {
                    if (objectiveTracker == null)
                    {
                        float end = Time.time + Mathf.Max(0f, waveDef.TriggerTimeSeconds);
                        while (Time.time < end) yield return null;
                        yield break;
                    }

                    while (!objectiveTracker.IsCompleted(waveDef.TriggerObjectiveId)) yield return null;
                    yield break;
                }
                default:
                    yield break;
            }
        }

        private void SpawnEvent(WaveTimelineEvent e, WaveDefinition waveDef)
        {
            var ctx = new WaveScalingContext(
                e.WaveIndex,
                currentDifficulty,
                waveDef.EnemySpeedScale,
                waveDef.EnemyHealthScale,
                waveDef.RewardScale,
                e.IsBoss);

            if (spawnBroadcastMode == SpawnBroadcastMode.SpawnOnAllSpawners)
            {
                for (int i = 0; i < spawners.Count; i++) spawners[i]?.Spawn(e, ctx);
                return;
            }

            if (spawners.Count == 0) return;
            spawnerRoundRobin = (spawnerRoundRobin + 1) % spawners.Count;
            spawners[spawnerRoundRobin]?.Spawn(e, ctx);
        }

        private float ComputeDifficultyForWave(int waveIndex)
        {
            var cfg = directorConfig;
            if (cfg == null) return 1f;

            float baseD = Mathf.Max(0.1f, cfg.BaseDifficulty);

            switch (cfg.Mode)
            {
                case DifficultyMode.Balanced:
                    return Mathf.Clamp(baseD, cfg.MinDifficulty, cfg.MaxDifficulty);

                case DifficultyMode.Aggressive:
                {
                    float d = baseD + (waveIndex * Mathf.Max(0f, cfg.AggressiveWaveRamp));
                    return Mathf.Clamp(d, cfg.MinDifficulty, cfg.MaxDifficulty);
                }

                case DifficultyMode.Adaptive:
                default:
                    return Mathf.Clamp(adaptiveDifficulty, cfg.MinDifficulty, cfg.MaxDifficulty);
            }
        }

        private void ApplyAdaptiveUpdate(float clearTimeSeconds)
        {
            var cfg = directorConfig;
            if (cfg == null) return;
            if (cfg.Mode != DifficultyMode.Adaptive) return;

            var snapshot = playerPerformanceProvider != null ? playerPerformanceProvider.GetSnapshot() : new PlayerPerformanceSnapshot(0f, 0);

            float timeErr = (cfg.TargetClearTimeSeconds - clearTimeSeconds) / Mathf.Max(1f, cfg.TargetClearTimeSeconds);
            float dmgErr = (cfg.TargetDamageTaken - snapshot.DamageTakenThisWave) / Mathf.Max(1f, cfg.TargetDamageTaken);
            float deathErr = (cfg.TargetDeaths - snapshot.DeathsThisWave) / Mathf.Max(1f, Mathf.Max(1f, cfg.TargetDeaths));

            float score = (0.6f * timeErr) + (0.3f * dmgErr) + (0.9f * deathErr);
            float delta = Mathf.Clamp(score * cfg.AdaptiveGain, -cfg.MaxDeltaPerWave, cfg.MaxDeltaPerWave);

            float target = adaptiveDifficulty + delta;
            adaptiveDifficulty = Mathf.Lerp(target, adaptiveDifficulty, cfg.AdaptiveSmoothing);
            adaptiveDifficulty = Mathf.Clamp(adaptiveDifficulty, cfg.MinDifficulty, cfg.MaxDifficulty);
        }

        private void OnKilled(WaveKillEvent e)
        {
            if (e.WaveIndex != currentWaveIndex) return;
            killsThisWave++;
        }

        private void EnsureSpawners()
        {
            spawners.RemoveAll(s => s == null);
            if (spawners.Count == 0)
            {
                var found = FindObjectsOfType<WaveSpawner>(true);
                spawners.AddRange(found);
            }
            for (int i = 0; i < spawners.Count; i++) spawners[i]?.EnsureSpawnPoints();
        }

        private void EnsurePoolRegistry()
        {
            if (PoolRegistry.Instance != null) return;
            var go = new GameObject("PoolRegistry");
            go.AddComponent<PoolRegistry>();
        }

        private void TryLoadProgress()
        {
            if (!WaveSaveSystem.TryLoad(savePath, out var data)) return;
            if (data == null) return;
            if (data.TimelineGuid != timelineGuid) return;

            currentWaveIndex = Mathf.Clamp(data.CurrentWaveIndex, 0, timelineConfig.WaveCount);
            dataTotalKills = Mathf.Max(0, data.TotalKills);
            dataTotalTime = Mathf.Max(0f, data.TotalTimeSeconds);
        }

        private void SaveProgress(int nextWaveIndex, float lastWaveClearTimeSeconds)
        {
            var data = new WaveSaveData
            {
                TimelineGuid = timelineGuid,
                CurrentWaveIndex = Mathf.Clamp(nextWaveIndex, 0, timelineConfig.WaveCount),
                TotalKills = dataTotalKills + killsThisWave,
                TotalTimeSeconds = dataTotalTime + Mathf.Max(0f, lastWaveClearTimeSeconds)
            };

            WaveSaveSystem.Save(savePath, data);

            dataTotalKills = data.TotalKills;
            dataTotalTime = data.TotalTimeSeconds;
        }
    }
}
