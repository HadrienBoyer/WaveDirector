# README_RUNTIME_API.md

Version: 1.0.0  
Last updated: 2026-03-01

## Main runtime scripts

WaveTimelineConfig (ScriptableObject)
- WaveCount
- DefaultWaveDurationSeconds
- Waves: List<WaveDefinition>
- Events: List<WaveTimelineEvent>

WaveDirector (MonoBehaviour)
- Starts wave loop automatically in Start()
- Supports previewing a wave in Play Mode
- Computes difficulty using WaveDirectorConfig
- Spawns timeline events with pacing (coroutine)
- Waits for completion (Timer / KillCount / Objective)
- Emits events:
  - WaveStarted(int waveIndex)
  - WaveCompleted(int waveIndex)

WaveSpawner (MonoBehaviour)
- SpawnPoints (or fallback to its own transform)
- Spawn(WaveTimelineEvent, WaveScalingContext)

PoolRegistry (MonoBehaviour singleton)
- Prefab keyed pools
- Spawn(prefab) / Despawn(prefab, instance)

WaveObjectiveTracker (MonoBehaviour)
- MarkCompleted(objectiveId)
- IsCompleted(objectiveId)

WaveEnemy / WaveKillNotifier
- Reference implementation for kill reporting

## Public integration points

- SpawnBroadcastMode
  - RoundRobin (default)
  - SpawnOnAllSpawners

- Preview
  - WaveDirector.StartPreviewWave(waveIndex)

- Spawner sync
  - WaveDirector.RegisterSpawner(spawner)
  - WaveDirector auto-finds spawners if list is empty

## Performance notes

- Spawning is spread across frames:
  - After each spawn, the director yields one frame
- Pooling avoids instantiate spikes on mobile
- If your enemies do heavy init work:
  - Move it into IPoolable.OnSpawned()
