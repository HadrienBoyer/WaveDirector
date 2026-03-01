# CHANGELOG

All notable changes will be documented in this file.

Format:
- Each release uses: Added / Changed / Fixed / Breaking
- “Breaking” must contain migration steps

## [1.0.0] - 2026-03-01

Added
- WaveTimelineConfig (ScriptableObject) holding Waves + Events
- Wave Timeline Editor Window:
  - height=200, timePerUnit=0.5
  - grid + lane shading
  - drag & drop prefab events
  - drag to move with snapping
  - inline inspector + duplicate/delete
  - save/reload
- WaveDirector runtime:
  - Aggressive / Balanced / Adaptive difficulty modes
  - timer/kill-count/objective wave completion
  - multi-spawner sync (round-robin or spawn-on-all)
  - coroutine pacing for mobile
- PoolRegistry prefab-keyed object pooling
- Save system (JSON) + analytics (JSONL)
- Extensibility interfaces: IWaveScalable, IPlayerPerformanceProvider, IWaveRewardHandler

Changed
- N/A

Fixed
- N/A

Breaking
- N/A
