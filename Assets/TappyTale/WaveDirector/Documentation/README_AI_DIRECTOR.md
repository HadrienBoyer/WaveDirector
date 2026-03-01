# README_AI_DIRECTOR.md

Version: 1.0.0  
Last updated: 2026-03-01

## Difficulty modes

WaveDirectorConfig.Mode:

Aggressive
- Difficulty ramps up by wave index:
  - difficulty = BaseDifficulty + waveIndex * AggressiveWaveRamp

Balanced
- Difficulty = BaseDifficulty (clamped)

Adaptive
- Difficulty adjusts based on player performance each wave.

All modes clamp difficulty between MinDifficulty and MaxDifficulty.

## Adaptive model (what it does)

At the end of each wave, Adaptive mode:
- Reads player performance snapshot (damage taken + deaths)
- Compares clear time vs TargetClearTimeSeconds
- Computes a score and applies a small delta to difficulty
- Smooths the result to avoid spikes

Signals used:
- Clear time error: faster clears => increase difficulty
- Damage error: less damage taken => increase difficulty
- Death error: deaths => strongly decrease difficulty

Tuning:
- AdaptiveGain: reaction strength
- AdaptiveSmoothing: how “sticky” the difficulty is
- MaxDeltaPerWave: clamps per-wave change

## Feeding performance data

If you want true Adaptive behavior, assign:
- WaveDirector.playerPerformanceProviderBehaviour = a MonoBehaviour implementing IPlayerPerformanceProvider

Included helper:
- BasicPlayerPerformanceProvider
  - AddDamageTaken(amount)
  - AddDeath()
  - ResetWave()

You decide where to call these (player health / respawn system).

## Practical tuning values (starting points)

Casual mobile (forgiving):
- BaseDifficulty: 1.0
- MinDifficulty: 0.6
- MaxDifficulty: 2.0
- TargetClearTime: 30–45s
- TargetDamageTaken: 20–40
- AdaptiveGain: 0.25–0.35
- Smoothing: 0.75–0.85
- MaxDeltaPerWave: 0.25

Hardcore:
- BaseDifficulty: 1.0
- MinDifficulty: 0.8
- MaxDifficulty: 3.0
- TargetClearTime: 15–25s
- TargetDamageTaken: 5–15
- Gain: 0.35–0.5
- Smoothing: 0.6–0.75
- MaxDeltaPerWave: 0.35–0.5
