# README_SAVE_ANALYTICS.md

Version: 1.0.0  
Last updated: 2026-03-01

## Save system

Path:
- `Application.persistentDataPath/TappyTale_WaveSaves/<timelineName>_<guid>.json`

Data:
- TimelineGuid
- CurrentWaveIndex
- TotalKills
- TotalTimeSeconds
- LastUpdatedUnixMs

Behavior:
- On Start(), WaveDirector tries to load and resumes at CurrentWaveIndex
- On wave completion, WaveDirector saves progress

Tip:
- If you need multiple saves per timeline (slots), extend GetSavePath() with a slot id.

## Analytics (JSONL)

Path:
- `Application.persistentDataPath/TappyTale_WaveAnalytics/<timelineName>_<guid>.jsonl`

Format:
- One JSON object per line (append-only)
- Good for batching uploads to your backend

Fields:
- TimelineGuid
- WaveIndex
- Difficulty
- Kills
- ClearTimeSeconds
- DamageTaken
- Deaths
- TimestampUnixMs

## Privacy / production notes

- JSONL is local-only here; you decide if/when to upload.
- For mobile, consider writing analytics less frequently if storage is a concern.
