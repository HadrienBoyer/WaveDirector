# README_TIMELINE_EDITOR.md

Version: 1.0.0  
Last updated: 2026-03-01

## Overview

Window:
- `TappyTale -> Waves -> Wave Timeline Editor`

Design goals:
- Visual authoring in minutes
- Drag-and-drop prefabs
- Drag events along time and between wave lanes
- Safe serialization (ScriptableObject save)

## Timeline layout

- Height: 200 px
- timePerUnit: 0.5 seconds
- unitWidth: 40 px
- Snap: 0.1 seconds (timePerUnit / 5)

Wave lanes:
- One lane per wave index
- Alternating lane shading for readability

Grid:
- Minor vertical lines each unit
- Major lines each 5 units (2.5s), with time labels

## Interactions

Drag & Drop
- Drag prefab(s) from Project window into the timeline
- Drop position determines:
  - WaveIndex (lane)
  - SpawnTime (x coordinate)

Select event
- Left click event box

Move event
- Click + drag event box
- X changes SpawnTime (snapped)
- Y changes WaveIndex

Toggle boss
- Small “B” checkbox on the event box

Context menu (right click event)
- Duplicate
- Delete

Inline inspector (bottom)
- Edit: WaveIndex / SpawnTime / IsBoss / Prefab
- Duplicate / Delete buttons

## Save / Reload

Save:
- Marks the asset dirty and saves via AssetDatabase

Reload:
- Re-loads the asset from disk (useful if you edited it elsewhere)

## Troubleshooting

- Events “jump” while dragging:
  - This is snapping. Change snapping in code if you want finer control.
- Lane selection feels off when scrolling:
  - The window uses scroll offset for hit-testing; if you customize layout, keep that consistent.
