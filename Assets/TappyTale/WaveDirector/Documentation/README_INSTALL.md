# README_INSTALL.md

Version: 1.0.0  
Last updated: 2026-03-01

## 1) Install

Copy into your project:
- `Assets/TappyTale/WaveDirector/`

No additional packages required.

## 2) Create assets

Create a Wave Timeline asset:
- Project -> Create -> TappyTale -> Waves -> Wave Timeline Config

(Optional) Create a Director Config asset:
- Project -> Create -> TappyTale -> Waves -> Wave Director Config

## 3) Scene setup (minimum)

Create a GameObject: `WaveSystem`
- Add `WaveDirector`
  - TimelineConfig = your WaveTimelineConfig
  - DirectorConfig = your WaveDirectorConfig (optional but recommended)
  - ObjectiveTracker = assign if you use objective-based triggers
  - PlayerPerformanceProviderBehaviour = assign if using Adaptive mode with real player metrics
  - RewardHandlerBehaviour = assign to handle wave completion rewards (optional)
- Add (or ensure exists) `PoolRegistry` in the scene
  - If you don’t, WaveDirector will auto-create one at runtime.

Create 1+ GameObject(s): `Spawner_A`, `Spawner_B`, ...
- Add `WaveSpawner`
  - Assign spawnPoints (or leave empty to use the spawner transform as a fallback)

## 4) Open the editor

Menu:
- `TappyTale -> Waves -> Wave Timeline Editor`

Assign your WaveTimelineConfig and start dragging prefabs onto the timeline lanes.

## 5) Press Play

WaveDirector will:
- Load save progress (optional)
- Start from the saved wave (or wave 0)
- Spawn events for each wave
- Wait for completion trigger
- Log analytics + save progress

## Common pitfalls

- Missing PoolRegistry:
  - Runtime still works (it auto-creates a PoolRegistry), but you’ll lose control of prewarm settings.
- KillCount trigger not progressing:
  - Ensure enemies call WaveKillBus (see README_EXTENDING.md “Kill reporting”).
- Objective trigger not progressing:
  - Assign a WaveObjectiveTracker and call MarkCompleted(objectiveId).
- Events not spawning:
  - Ensure the WaveSpawner(s) exist, or WaveDirector will auto-find them at runtime.
