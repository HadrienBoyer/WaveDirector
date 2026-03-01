#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TappyTale.WaveDirector.Runtime;

namespace TappyTale.WaveDirector.Editor
{
    public sealed class WaveTimelineEditorWindow : EditorWindow
    {
        private const float TimelineHeight = 200f;
        private const float TimePerUnit = 0.5f; // requested
        private const float UnitWidth = 40f;
        private const float LeftPadding = 140f;
        private const float TopPadding = 48f;
        private const float LaneHeightMin = 24f;
        private const float EventWidth = 140f;

        private WaveTimelineConfig config;
        private Vector2 scroll;

        private int selectedIndex = -1;
        private bool isDragging;
        private int draggingIndex = -1;
        private Vector2 dragOffset;

        [MenuItem("TappyTale/Wave Director/Wave Timeline Editor")]
        public static void OpenMenu()
        {
            Open(null);
        }

        public static void Open(WaveTimelineConfig cfg)
        {
            var window = GetWindow<WaveTimelineEditorWindow>("Wave Editor - Timeline");
            window.minSize = new Vector2(820, 480);
            window.config = cfg;
            window.Repaint();
        }

        private void OnGUI()
        {
            DrawToolbar();

            if (config == null)
            {
                EditorGUILayout.HelpBox("Assign a WaveTimelineConfig asset to begin.", MessageType.Info);
                return;
            }

            config.EnsureWaveDefinitions();

            Rect timelineRect = new Rect(0, TopPadding, position.width, position.height - TopPadding);
            DrawTimeline(timelineRect);
            HandleDragAndDrop(timelineRect);
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.toolbar))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    config = (WaveTimelineConfig)EditorGUILayout.ObjectField(config, typeof(WaveTimelineConfig), false, GUILayout.Width(420));

                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Reload", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    {
                        if (config != null)
                        {
                            string path = AssetDatabase.GetAssetPath(config);
                            config = AssetDatabase.LoadAssetAtPath<WaveTimelineConfig>(path);
                            selectedIndex = -1;
                            draggingIndex = -1;
                            isDragging = false;
                        }
                    }

                    if (GUILayout.Button("Save", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    {
                        SaveConfig();
                    }

                    if (GUILayout.Button("Ping", EditorStyles.toolbarButton, GUILayout.Width(70)))
                    {
                        if (config != null) EditorGUIUtility.PingObject(config);
                    }
                }

                if (config != null)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        GUILayout.Label("WaveCount: " + config.WaveCount, GUILayout.Width(120));
                        int newCount = EditorGUILayout.IntSlider(config.WaveCount, 1, 32);
                        if (newCount != config.WaveCount)
                        {
                            Undo.RecordObject(config, "Change Wave Count");
                            config.WaveCount = newCount;
                            config.EnsureWaveDefinitions();
                            EditorUtility.SetDirty(config);
                        }

                        GUILayout.FlexibleSpace();

                        bool canPreview = EditorApplication.isPlaying;
                        using (new EditorGUI.DisabledScope(!canPreview))
                        {
                            if (GUILayout.Button("Preview Selected Wave", GUILayout.Width(180)))
                            {
                                if (selectedIndex >= 0 && selectedIndex < config.Events.Count)
                                {
                                    int w = Mathf.Max(0, config.Events[selectedIndex].WaveIndex);
                                    var director = Object.FindFirstObjectByType<global::TappyTale.WaveDirector.Runtime.WaveDirector>(FindObjectsInactive.Include);
                                    if (director != null) director.StartPreviewWave(w);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DrawTimeline(Rect rect)
        {
            float laneHeight = Mathf.Max(LaneHeightMin, TimelineHeight / Mathf.Max(1, config.WaveCount));
            float timelineWidth = ComputeTimelineWidthSeconds() / TimePerUnit * UnitWidth + LeftPadding + 200f;

            Rect contentRect = new Rect(0, 0, timelineWidth, TimelineHeight + 60f);
            scroll = GUI.BeginScrollView(rect, scroll, contentRect);

            Rect gridRect = new Rect(0, 0, contentRect.width, TimelineHeight);
            DrawGrid(gridRect, laneHeight);
            DrawWaveLabels(laneHeight);
            DrawEvents(laneHeight);
            DrawSelectedInspector(new Rect(0, TimelineHeight + 8f, contentRect.width, 52f));

            GUI.EndScrollView();

            HandleMouse(laneHeight);
        }

        private float ComputeTimelineWidthSeconds()
        {
            float max = 0f;

            if (config.Events != null)
            {
                for (int i = 0; i < config.Events.Count; i++)
                {
                    var e = config.Events[i];
                    if (e == null) continue;
                    max = Mathf.Max(max, e.SpawnTime);
                }
            }

            for (int w = 0; w < config.WaveCount; w++)
            {
                max = Mathf.Max(max, config.GetWaveEndTimeSeconds(w));
            }

            return Mathf.Max(5f, max + 2f);
        }

        private void DrawGrid(Rect rect, float laneHeight)
        {
            EditorGUI.DrawRect(rect, new Color(0.11f, 0.11f, 0.11f, 1f));

            for (int w = 0; w < config.WaveCount; w++)
            {
                float y = w * laneHeight;
                var c = (w % 2 == 0) ? new Color(0f, 0f, 0f, 0.08f) : new Color(1f, 1f, 1f, 0.03f);
                EditorGUI.DrawRect(new Rect(0, y, rect.width, laneHeight), c);
            }

            float seconds = ComputeTimelineWidthSeconds();
            int units = Mathf.CeilToInt(seconds / TimePerUnit);

            for (int u = 0; u <= units; u++)
            {
                float x = LeftPadding + u * UnitWidth;
                bool major = (u % 5) == 0;
                var color = major ? new Color(1f, 1f, 1f, 0.20f) : new Color(1f, 1f, 1f, 0.08f);
                EditorGUI.DrawRect(new Rect(x, 0, 1f, rect.height), color);

                if (major)
                {
                    float t = u * TimePerUnit;
                    GUI.Label(new Rect(x + 2f, 2f, 60f, 18f), t.ToString("0.0") + "s", EditorStyles.miniLabel);
                }
            }

            for (int w = 0; w <= config.WaveCount; w++)
            {
                float y = w * laneHeight;
                EditorGUI.DrawRect(new Rect(0, y, rect.width, 1f), new Color(1f, 1f, 1f, 0.10f));
            }
        }

        private void DrawWaveLabels(float laneHeight)
        {
            for (int w = 0; w < config.WaveCount; w++)
            {
                float y = w * laneHeight;
                var labelRect = new Rect(8f, y + 4f, LeftPadding - 16f, laneHeight - 8f);

                string label = (config.Waves != null && w < config.Waves.Count && !string.IsNullOrWhiteSpace(config.Waves[w].Name))
                    ? config.Waves[w].Name
                    : "Wave " + (w + 1);

                GUI.Label(labelRect, label, EditorStyles.boldLabel);
            }
        }

        private void DrawEvents(float laneHeight)
        {
            if (config.Events == null) return;

            for (int i = 0; i < config.Events.Count; i++)
            {
                var e = config.Events[i];
                if (e == null) continue;

                Rect eventRect = GetEventRect(e, laneHeight);

                bool isSelected = i == selectedIndex;
                var baseColor = e.IsBoss ? new Color(0.85f, 0.35f, 0.35f, 0.95f) : new Color(0.35f, 0.75f, 0.95f, 0.95f);
                if (isSelected) baseColor = Color.Lerp(baseColor, Color.white, 0.25f);

                EditorGUI.DrawRect(eventRect, baseColor);

                string name = e.Prefab != null ? e.Prefab.name : "(Missing)";
                string boss = e.IsBoss ? " [B]" : "";
                GUI.Label(new Rect(eventRect.x + 6f, eventRect.y + 4f, eventRect.width - 12f, 18f), name + boss, EditorStyles.whiteMiniLabel);
                GUI.Label(new Rect(eventRect.x + 6f, eventRect.y + 20f, eventRect.width - 12f, 18f), e.SpawnTime.ToString("0.00") + "s", EditorStyles.whiteMiniLabel);
            }
        }

        private Rect GetEventRect(WaveTimelineEvent e, float laneHeight)
        {
            float x = LeftPadding + (e.SpawnTime / TimePerUnit) * UnitWidth;
            float y = Mathf.Clamp(e.WaveIndex, 0, Mathf.Max(0, config.WaveCount - 1)) * laneHeight + 2f;
            float h = Mathf.Max(18f, laneHeight - 4f);
            return new Rect(x, y, EventWidth, h);
        }

        private void DrawSelectedInspector(Rect rect)
        {
            if (selectedIndex < 0 || config.Events == null || selectedIndex >= config.Events.Count)
            {
                GUI.Label(rect, "Select an event to edit. Drag prefabs into the timeline to create events.", EditorStyles.centeredGreyMiniLabel);
                return;
            }

            var e = config.Events[selectedIndex];
            if (e == null) return;

            Rect r = rect;
            r.x += 8f;
            r.width -= 16f;

            GUILayout.BeginArea(r);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();

                int waveIndex = EditorGUILayout.IntField("WaveIndex", e.WaveIndex, GUILayout.Width(240));
                float spawnTime = EditorGUILayout.FloatField("SpawnTime", e.SpawnTime, GUILayout.Width(240));
                bool isBoss = EditorGUILayout.Toggle("IsBoss", e.IsBoss, GUILayout.Width(140));
                var prefab = (GameObject)EditorGUILayout.ObjectField("Prefab", e.Prefab, typeof(GameObject), false);

                if (EditorGUI.EndChangeCheck())
                {
                    Undo.RecordObject(config, "Edit Event");
                    e.WaveIndex = Mathf.Clamp(waveIndex, 0, Mathf.Max(0, config.WaveCount - 1));
                    e.SpawnTime = Mathf.Max(0f, spawnTime);
                    e.IsBoss = isBoss;
                    e.Prefab = prefab;
                    EditorUtility.SetDirty(config);
                }
            }
            GUILayout.EndArea();
        }

        private void HandleMouse(float laneHeight)
        {
            Event ev = Event.current;
            if (ev == null) return;

            Vector2 mouse = ev.mousePosition + scroll;

            if (ev.type == EventType.MouseDown && ev.button == 0)
            {
                int index = HitTestEvent(mouse, laneHeight);
                if (index >= 0)
                {
                    selectedIndex = index;
                    draggingIndex = index;
                    isDragging = true;

                    var er = GetEventRect(config.Events[index], laneHeight);
                    dragOffset = mouse - new Vector2(er.x, er.y);

                    Repaint();
                    ev.Use();
                }
            }

            if (ev.type == EventType.MouseDrag && ev.button == 0 && isDragging && draggingIndex >= 0)
            {
                var e = config.Events[draggingIndex];
                if (e != null)
                {
                    float x = mouse.x - dragOffset.x;
                    float y = mouse.y - dragOffset.y;

                    float time = (x - LeftPadding) / UnitWidth * TimePerUnit;
                    int wave = Mathf.FloorToInt(y / laneHeight);

                    time = Snap(time, TimePerUnit / 5f);

                    Undo.RecordObject(config, "Move Event");
                    e.SpawnTime = Mathf.Max(0f, time);
                    e.WaveIndex = Mathf.Clamp(wave, 0, Mathf.Max(0, config.WaveCount - 1));
                    EditorUtility.SetDirty(config);
                }

                Repaint();
                ev.Use();
            }

            if (ev.type == EventType.MouseUp && ev.button == 0 && isDragging)
            {
                isDragging = false;
                draggingIndex = -1;
                SaveConfig();
                ev.Use();
            }
        }

        private int HitTestEvent(Vector2 mouse, float laneHeight)
        {
            if (config == null || config.Events == null) return -1;

            for (int i = config.Events.Count - 1; i >= 0; i--)
            {
                var e = config.Events[i];
                if (e == null) continue;
                Rect r = GetEventRect(e, laneHeight);
                if (r.Contains(mouse)) return i;
            }

            return -1;
        }

        private void HandleDragAndDrop(Rect timelineRect)
        {
            if (config == null) return;

            Event ev = Event.current;
            if (ev == null) return;

            Rect contentRect = new Rect(timelineRect.x, timelineRect.y, timelineRect.width, TimelineHeight);
            if (!contentRect.Contains(ev.mousePosition)) return;

            if (ev.type == EventType.DragUpdated || ev.type == EventType.DragPerform)
            {
                bool hasPrefab = false;
                foreach (var obj in DragAndDrop.objectReferences)
                {
                    if (obj is GameObject) { hasPrefab = true; break; }
                }
                if (!hasPrefab) return;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (ev.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();

                    Vector2 mouse = ev.mousePosition + scroll;
                    float laneHeightLocal = Mathf.Max(1f, TimelineHeight / Mathf.Max(1, config.WaveCount));

                    int wave = Mathf.FloorToInt(mouse.y / laneHeightLocal);
                    float time = (mouse.x - LeftPadding) / UnitWidth * TimePerUnit;
                    time = Snap(time, TimePerUnit / 5f);

                    foreach (var obj in DragAndDrop.objectReferences)
                    {
                        if (obj is GameObject prefab)
                        {
                            Undo.RecordObject(config, "Add Timeline Event");
                            config.Events.Add(new WaveTimelineEvent
                            {
                                WaveIndex = Mathf.Clamp(wave, 0, Mathf.Max(0, config.WaveCount - 1)),
                                Prefab = prefab,
                                SpawnTime = Mathf.Max(0f, time),
                                IsBoss = false
                            });
                            EditorUtility.SetDirty(config);
                        }
                    }

                    SaveConfig();
                }

                ev.Use();
            }
        }

        private static float Snap(float value, float snap)
        {
            if (snap <= 0f) return value;
            return Mathf.Round(value / snap) * snap;
        }

        private void SaveConfig()
        {
            if (config == null) return;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
        }
    }
}
#endif
