TappyTale Wave Director Tool (v1)
=================================

What you get
- WaveTimelineConfig (ScriptableObject) with a Wave Timeline (events: waveIndex, prefab, spawnTime, isBoss)
- Wave Timeline Editor Window (drag & drop prefabs, grid, markers, shading, load/save/reload)
- Runtime WaveDirector (Aggressive / Balanced / Adaptive) + multi-spawner sync
- Object pooling + coroutine-based async spawning (60+ FPS friendly on mobile)
- Flexible triggers: timer, kill-count, objective
- Built-in save + analytics logging (JSON in persistentDataPath)
- Extendable interfaces for custom enemies, bosses, rewards, and performance inputs

Install
1) Copy the folder: Assets/TappyTale/WaveDirector into your Unity project.
2) Create a WaveTimelineConfig asset:
   - Project window -> Create -> TappyTale -> Waves -> Wave Timeline Config
3) (Optional) Create a WaveDirectorConfig asset:
   - Project window -> Create -> TappyTale -> Waves -> Wave Director Config
4) Add these components to a scene:
   - WaveDirector (assign TimelineConfig + DirectorConfig)
   - 1+ WaveSpawner(s) (add spawn points)
   - (Optional) WaveObjectiveTracker if you want objective-based triggers
5) Open the editor:
   - Menu: TappyTale -> Waves -> Wave Timeline Editor
   - Assign your WaveTimelineConfig, then drag prefabs into the timeline.

How spawning works
- Each event spawns a prefab at event.SpawnTime seconds from the wave start.
- If WaveDirector.SpawnBroadcastMode == SpawnOnAllSpawners, each event spawns on all spawners.
- Otherwise the director round-robins across registered spawners.

How kills are counted
- If your enemy prefab has WaveEnemy, it will auto-report kills on death.
- If you have your own enemy health/death system, add WaveKillNotifier and call NotifyKilled() when it dies.

Save & analytics
- Save file: Application.persistentDataPath/TappyTale_WaveSaves/<timelineAssetName>_<guid>.json
- Analytics file: Application.persistentDataPath/TappyTale_WaveAnalytics/<timelineAssetName>_<guid>.jsonl
  (JSONL = one JSON object per line; good for telemetry pipelines)

Extending
- Implement IWaveScalable to receive scaling multipliers (health/speed/etc).
- Implement IPlayerPerformanceProvider to feed custom performance metrics into Adaptive mode.
- Implement IWaveRewardHandler to handle rewards on wave completion.

Notes
- This package avoids brittle internal APIs. The editor uses standard IMGUI.
- Works with Unity 6 (URP or Built-in). No extra packages required.
