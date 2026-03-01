using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TappyTale
{
    public enum SpawnBroadcastMode
    {
        RoundRobin = 0,
        SpawnOnAllSpawners = 1
    }

    [DisallowMultipleComponent]
    public sealed class WaveDirector : MonoBehaviour
    {
        [Header("Configs")]
        [SerializeField] private WaveTimelineConfig timelineConfig;
        [SerializeField] private WaveDirectorConfig directorConfig;

        [Header("Spawners")]
        [SerializeField] private List<WaveSpawner> spawners = new List<WaveSpawner>();
        [SerializeField] private SpawnBroadcastMode spawnBroadcastMode = SpawnBroadcastMode.RoundRobin;

        [Header("Optional Hooks")]
        [SerializeField] private MonoBehaviour playerPerformanceProviderBehaviour;
        [SerializeField] private MonoBehaviour rewardHandlerBehaviour;
        [SerializeField] private WaveObjectiveTracker objectiveTracker;

        [Header("Save & Analytics")]
        [SerializeField] private bool enableSave = true;
        [SerializeField] private bool enableAnalytics = true;

        [Header("Runtime (Read Only)")]
        [SerializeField] private int currentWaveIndex;
        [SerializeField] private float currentDifficulty = 1f;
        [SerializeField] private int killsThisWave;
        [SerializeField] private float waveStartTime;

        public event Action<int> WaveStarted;
        public event Action<int> WaveCompleted;

        private IPlayerPerformanceProvider playerPerformanceProvider;
        private IWaveRewardHandler rewardHandler;

        private string timelineGuid;
        private string savePath;
        private string analyticsPath;

        private int spawnerRoundRobin;
        private float adaptiveDifficulty;

        private bool isRunning;
        private Coroutine runRoutine;

        public WaveTimelineConfig TimelineConfig => timelineConfig;
        public WaveDirectorConfig DirectorConfig => directorConfig;
        public int CurrentWaveIndex => currentWaveIndex;
        public float CurrentDifficulty => currentDifficulty;

        private void Awake()
        {
            if (timelineConfig != null) timelineConfig.EnsureWaveDefinitions();

            playerPerformanceProvider = playerPerformanceProviderBehaviour as IPlayerPerformanceProvider;
            rewardHandler = rewardHandlerBehaviour as IWaveRewardHandler;

            EnsurePoolRegistry();

            timelineGuid = TimelineGuidUtility.GetOrCreateGuid(timelineConfig);
            savePath = WaveSaveSystem.GetSavePath(timelineConfig != null ? timelineConfig.name : "Timeline", timelineGuid);
            analyticsPath = WaveAnalyticsLogger.GetAnalyticsPath(timelineConfig != null ? timelineConfig.name : "Timeline", timelineGuid);

            adaptiveDifficulty = directorConfig != null ? directorConfig.BaseDifficulty : 1f;
        }

        private void OnEnable()
        {
            WaveKillBus.Killed += OnKilled;
        }

        private void OnDisable()
        {
            WaveKillBus.Killed -= OnKilled;
        }

        private void Start()
        {
            if (timelineConfig == null)
            {
                Debug.LogError("[WaveDirector] Missing TimelineConfig.");
                return;
            }

            if (directorConfig == null)
            {
                Debug.LogWarning("[WaveDirector] Missing DirectorConfig, using defaults.");
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

        public void RegisterSpawner(WaveSpawner spawner)
        {
            if (spawner == null) return;
            if (spawners.Contains(spawner)) return;
            spawners.Add(spawner);
        }

        public void UnregisterSpawner(WaveSpawner spawner)
        {
            if (spawner == null) return;
            spawners.Remove(spawner);
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
            currentWaveIndex = Mathf.Clamp(currentWaveIndex, 0, timelineConfig.WaveCount - 1);

            while (isRunning && currentWaveIndex < timelineConfig.WaveCount)
            {
                yield return RunWave(currentWaveIndex);
                currentWaveIndex++;
            }
        }

        private IEnumerator RunSingleWavePreview(int waveIndex)
        {
            yield return RunWave(waveIndex);
        }

        private IEnumerator RunWave(int waveIndex)
        {
            if (timelineConfig == null) yield break;

            var waveDef = timelineConfig.Waves != null && waveIndex < timelineConfig.Waves.Count
                ? timelineConfig.Waves[waveIndex]
                : WaveDefinition.CreateDefault(waveIndex);

            killsThisWave = 0;
            waveStartTime = Time.time;

            currentDifficulty = ComputeDifficultyForWave(waveIndex);

            WaveStarted?.Invoke(waveIndex);

            // Spawn events for this wave.
            yield return StartCoroutine(SpawnWaveEventsAsync(waveIndex, waveDef));

            // Wait for wave completion condition.
            yield return StartCoroutine(WaitForWaveCompletion(waveIndex, waveDef));

            // Wrap up.
            float clearTime = Mathf.Max(0.01f, Time.time - waveStartTime);

            ApplyAdaptiveUpdate(clearTime);

            if (rewardHandler != null)
            {
                float rewardScale = waveDef.RewardScale * currentDifficulty;
                rewardHandler.OnWaveCompleted(waveIndex, rewardScale);
            }

            WaveCompleted?.Invoke(waveIndex);

            if (enableAnalytics)
            {
                var snapshot = playerPerformanceProvider != null ? playerPerformanceProvider.GetSnapshot() : new PlayerPerformanceSnapshot(0f, 0);
                var entry = new WaveAnalyticsEntry
                {
                    TimelineGuid = timelineGuid,
                    WaveIndex = waveIndex,
                    Difficulty = currentDifficulty,
                    Kills = killsThisWave,
                    ClearTimeSeconds = clearTime,
                    DamageTaken = snapshot.DamageTakenThisWave,
                    Deaths = snapshot.DeathsThisWave
                };
                WaveAnalyticsLogger.Append(analyticsPath, entry);
            }

            if (enableSave) SaveProgress();
        }

        private IEnumerator SpawnWaveEventsAsync(int waveIndex, WaveDefinition waveDef)
        {
            if (timelineConfig.Events == null) yield break;
            EnsureSpawners();

            var list = GatherEventsForWave(waveIndex);
            list.Sort((a, b) => a.SpawnTime.CompareTo(b.SpawnTime));

            float waveTimeScale = Mathf.Max(0.01f, waveDef.SpawnTimeScale);
            float lastTime = 0f;

            for (int i = 0; i < list.Count; i++)
            {
                var e = list[i];
                if (e == null || e.Prefab == null) continue;

                float t = Mathf.Max(0f, e.SpawnTime) * waveTimeScale;
                float wait = Mathf.Max(0f, t - lastTime);
                lastTime = t;

                // "Async" spawning: we yield frames while waiting (coroutine-friendly for mobile).
                float end = Time.time + wait;
                while (Time.time < end)
                {
                    yield return null;
                }

                SpawnEvent(e, waveDef);
                yield return null; // small yield to avoid spikes on mobile
            }
        }

        private IEnumerator WaitForWaveCompletion(int waveIndex, WaveDefinition waveDef)
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
                        Debug.LogWarning("[WaveDirector] Objective trigger used but no WaveObjectiveTracker assigned. Falling back to timer.");
                        float end = Time.time + Mathf.Max(0f, waveDef.TriggerTimeSeconds);
                        while (Time.time < end) yield return null;
                        yield break;
                    }

                    string id = waveDef.TriggerObjectiveId;
                    while (!objectiveTracker.IsCompleted(id)) yield return null;
                    yield break;
                }
                default:
                    yield break;
            }
        }

        private List<WaveTimelineEvent> GatherEventsForWave(int waveIndex)
        {
            var results = new List<WaveTimelineEvent>(32);
            for (int i = 0; i < timelineConfig.Events.Count; i++)
            {
                var e = timelineConfig.Events[i];
                if (e == null) continue;
                if (e.WaveIndex != waveIndex) continue;
                results.Add(e);
            }
            return results;
        }

        private void SpawnEvent(WaveTimelineEvent e, WaveDefinition waveDef)
        {
            if (e == null || e.Prefab == null) return;

            var ctx = new WaveScalingContext(
                e.WaveIndex,
                currentDifficulty,
                waveDef.EnemySpeedScale,
                waveDef.EnemyHealthScale,
                waveDef.RewardScale,
                e.IsBoss);

            if (spawnBroadcastMode == SpawnBroadcastMode.SpawnOnAllSpawners)
            {
                for (int i = 0; i < spawners.Count; i++)
                {
                    var s = spawners[i];
                    if (s == null) continue;
                    s.Spawn(e, ctx);
                }
                return;
            }

            // Round-robin across spawners.
            if (spawners.Count == 0) return;
            spawnerRoundRobin = (spawnerRoundRobin + 1) % spawners.Count;
            var spawner = spawners[spawnerRoundRobin];
            if (spawner == null) return;
            spawner.Spawn(e, ctx);
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

            // Weight deaths heavily (dies => difficulty should drop).
            float score = (0.6f * timeErr) + (0.3f * dmgErr) + (0.9f * deathErr);

            float delta = Mathf.Clamp(score * cfg.AdaptiveGain, -cfg.MaxDeltaPerWave, cfg.MaxDeltaPerWave);

            float target = adaptiveDifficulty + delta;

            // Smoothly converge toward target.
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
            // Remove nulls + auto-find if empty.
            spawners.RemoveAll(s => s == null);

            if (spawners.Count == 0)
            {
                var found = FindObjectsOfType<WaveSpawner>(true);
                spawners.AddRange(found);
            }

            for (int i = 0; i < spawners.Count; i++) if (spawners[i] != null) spawners[i].EnsureSpawnPoints();
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
            if (!string.Equals(data.TimelineGuid, timelineGuid, StringComparison.Ordinal)) return;

            currentWaveIndex = Mathf.Clamp(data.CurrentWaveIndex, 0, Mathf.Max(0, timelineConfig.WaveCount - 1));
        }

        private void SaveProgress()
        {
            if (timelineConfig == null) return;

            var data = new WaveSaveData
            {
                TimelineGuid = timelineGuid,
                CurrentWaveIndex = currentWaveIndex,
                TotalKills = dataTotalKills + killsThisWave,
                TotalTimeSeconds = dataTotalTime + (Time.time - waveStartTime)
            };

            WaveSaveSystem.Save(savePath, data);

            // Update cached totals for the next save.
            dataTotalKills = data.TotalKills;
            dataTotalTime = data.TotalTimeSeconds;
        }

        private int dataTotalKills;
        private float dataTotalTime;
    }

    public static class TimelineGuidUtility
    {
        private const string Prefix = "TappyTale.WaveTimeline.Guid.";

        public static string GetOrCreateGuid(WaveTimelineConfig config)
        {
            if (config == null) return "NoConfig";

            string key = Prefix + config.name;
            if (PlayerPrefs.HasKey(key))
            {
                return PlayerPrefs.GetString(key);
            }

            string guid = System.Guid.NewGuid().ToString("N");
            PlayerPrefs.SetString(key, guid);
            PlayerPrefs.Save();
            return guid;
        }
    }
}
