# README_EXTENDING.md

Version: 1.0.0  
Last updated: 2026-03-01

## Key interfaces

IWaveScalable
- Implement on enemies (or child components) to receive scaling:
  - Difficulty
  - EnemySpeedScale / EnemyHealthScale
  - RewardScale
  - IsBoss flag
- Called by WaveSpawner right after spawn.

IPlayerPerformanceProvider
- Used by Adaptive mode
- Return PlayerPerformanceSnapshot { DamageTakenThisWave, DeathsThisWave }

IWaveRewardHandler
- Called when a wave completes:
  - OnWaveCompleted(waveIndex, rewardScale)

## Kill reporting (required for KillCount waves)

WaveDirector increments kills on WaveKillBus.Killed events where:
- killEvent.WaveIndex == CurrentWaveIndex

Options:
A) Use WaveEnemy (reference enemy)
- Call WaveEnemy.Damage() and it reports kill on Die()

B) Use your own enemy system
- Add WaveKillNotifier and call NotifyKilled() when your enemy dies

C) Manual
- Call WaveKillBus.RaiseKilled(new WaveKillEvent(waveIndex, isBoss, enemyId))

## Custom boss handling

Use WaveTimelineEvent.IsBoss:
- Spawn bosses at a special spawn point
- Apply different scaling
- Trigger special music/VFX
- Gate rewards

## Multiplayer considerations (future-proofing)

Recommended direction:
- Treat WaveTimelineConfig as deterministic content.
- Drive spawning from an authority (server/host).
- Replicate only the spawn commands (waveIndex, prefabId, time, boss flag, spawnerId).
- Keep Adaptive logic on authority side and sync difficulty multiplier to clients.
