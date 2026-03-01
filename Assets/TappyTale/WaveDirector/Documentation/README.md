# TappyTale Wave Director Tool

Version: 1.0.0  
Last updated: 2026-03-01

This package lets designers build, preview, and balance enemy waves inside Unity with zero code:
- A ScriptableObject timeline (WaveTimelineConfig) that stores events per wave
- A Wave Timeline Editor Window (drag & drop prefabs onto a grid timeline)
- A runtime WaveDirector with difficulty modes (Aggressive / Balanced / Adaptive)
- Performance-friendly spawning (pooling + coroutine pacing)
- Flexible wave completion triggers (timer / kill-count / objective)
- Save + analytics logging (JSON / JSONL)
- Clean extension points (interfaces) for enemies, player metrics, rewards

Documentation map:
- README_INSTALL.md — installation, scene setup, quick start
- README_WAVE_DESIGNER.md — how to author waves + balancing knobs
- README_TIMELINE_EDITOR.md — editor window controls + workflow
- README_AI_DIRECTOR.md — how adaptive difficulty works + tuning
- README_RUNTIME_API.md — main runtime scripts and public API
- README_SAVE_ANALYTICS.md — save format, paths, telemetry
- README_EXTENDING.md — interfaces, custom enemies, multiplayer hooks
- README_VERSIONING.md — how to maintain docs per release
- CHANGELOG.md — version history (Added/Changed/Fixed/Breaking)

Recommended folder layout:
- Assets/TappyTale/WaveDirector/Runtime
- Assets/TappyTale/WaveDirector/Editor
- Assets/TappyTale/WaveDirector/Documentation
