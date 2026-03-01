# README_WAVE_DESIGNER.md

Version: 1.0.0  
Last updated: 2026-03-01

## Mental model

A “wave” is:
- A lane (WaveIndex) on the timeline
- A set of timeline events (spawn a prefab at time T)
- A completion trigger (Timer / KillCount / Objective)
- Optional balancing multipliers (spawn cadence + enemy scaling + rewards)

Timeline events do NOT decide when a wave ends.  
WaveDefinition.TriggerType decides that.

## Authoring workflow

1) Set WaveCount on WaveTimelineConfig.
2) For each wave (Waves[i]):
   - Choose TriggerType
   - Set trigger parameters
   - Tune balancing multipliers
3) Drag prefabs onto the timeline lane for that wave.
4) Play and iterate.

## Balancing knobs (WaveDefinition)

- SpawnTimeScale
  - Multiplies the spawn times for events in this wave.
  - > 1.0 stretches time (slower spawns).
  - < 1.0 compresses time (faster spawns).

- EnemySpeedScale / EnemyHealthScale
  - Passed to enemies via IWaveScalable (recommended).
  - Also multiplied by director difficulty (WaveDirector.CurrentDifficulty).

- RewardScale
  - Passed to IWaveRewardHandler as: RewardScale * Difficulty

## Completion triggers

Timer
- Ends after TriggerTimeSeconds from wave start.
- Useful for “survive X seconds” waves.

KillCount
- Ends once killsThisWave >= TriggerKillCount.
- Requires kill reporting (see README_EXTENDING.md).

Objective
- Ends when WaveObjectiveTracker.IsCompleted(TriggerObjectiveId) == true.
- Good for “defuse bomb”, “hack terminal”, “escort finished”.

Tip:
- You can spawn objectives from timeline events too (e.g., spawn a “Terminal” prefab at 3.0s).

## Boss events

WaveTimelineEvent.IsBoss:
- Mostly for UX + scaling flags (WaveScalingContext.IsBoss).
- Your spawner can interpret bosses specially (e.g., spawn point 0).
- You can also use it for reward multipliers or music stingers.

## Testing & iteration

Fast testing loop:
- Open Wave Timeline Editor
- Add/adjust events
- Hit play
- Use “Preview Selected Wave” button (in play mode) to jump to a specific wave
