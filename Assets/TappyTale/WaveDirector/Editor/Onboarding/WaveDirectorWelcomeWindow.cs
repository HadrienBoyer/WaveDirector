// File: Assets/TappyTale/WaveDirector/Editor/Onboarding/WaveDirectorWelcomeWindow.cs
#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace TappyTale.WaveDirector.Editor
{
    public sealed class WaveDirectorWelcomeWindow : EditorWindow
    {
        private const string Version = "1.0.2";
        private const string PrefKey = "TT_WaveDirector_WelcomeShown_";

        private Vector2 scroll;
        private bool createScene = true;
        private bool overwriteExisting = false;

        [MenuItem("TappyTale/Waves/Wave Director Welcome", priority = 0)]
        public static void ShowFromMenu()
        {
            ShowWindow(focus: true);
        }

        internal static void ShowWindow(bool focus)
        {
            var w = GetWindow<WaveDirectorWelcomeWindow>(utility: true, title: "Wave Director");
            w.minSize = new Vector2(560, 520);
            if (focus) w.Focus();
            w.Show();
        }

        private void OnGUI()
        {
            DrawHeader();

            scroll = EditorGUILayout.BeginScrollView(scroll);

            DrawIntro();
            EditorGUILayout.Space(12);
            DrawQuickStart();
            EditorGUILayout.Space(12);
            DrawDemoSection();
            EditorGUILayout.Space(12);
            DrawLinks();

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(6);
            DrawFooter();
        }

        private void DrawHeader()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var titleStyle = new GUIStyle(EditorStyles.boldLabel) { fontSize = 16 };
                EditorGUILayout.LabelField("Wave Director - Enemy AI and Boss Rush Wave Spawner Tools", titleStyle);
                EditorGUILayout.LabelField($"Version {Version}", EditorStyles.miniLabel);
            }
        }

        private void DrawIntro()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Welcome!", EditorStyles.boldLabel);

                EditorGUILayout.LabelField(
                    "Wave Director is a designer-friendly wave authoring and runtime system:\n" +
                    "• Author waves visually with a timeline\n" +
                    "• Spawn via one or many spawners (round-robin or broadcast)\n" +
                    "• End waves by timer, kill-count, or objectives\n" +
                    "• Optional adaptive difficulty + local save/analytics\n\n" +
                    "Use this window to generate a tiny demo setup (ScriptableObjects + optional scene). " +
                    "You’ll be able to press Play immediately and see spawns.",
                    EditorStyles.wordWrappedLabel
                );
            }
        }

        private void DrawQuickStart()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("On-boarding (2 minutes)", EditorStyles.boldLabel);

                EditorGUILayout.LabelField(
                    "1) Create a WaveTimelineConfig\n" +
                    "   Project → Create → TappyTale → Waves → Wave Timeline Config\n" +
                    "2) (Recommended) Create a WaveDirectorConfig\n" +
                    "   Project → Create → TappyTale → Waves → Wave Director Config\n" +
                    "3) Add to a scene: WaveDirector + 1+ WaveSpawner\n" +
                    "4) Open: TappyTale → Waves → Wave Timeline Editor\n" +
                    "5) Drag prefabs onto the timeline lanes, then press Play",
                    EditorStyles.wordWrappedLabel
                );

                EditorGUILayout.Space(6);

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Open Wave Timeline Editor", GUILayout.Height(28)))
                        OpenTimelineEditorWindow();

                    if (GUILayout.Button("Open Install README", GUILayout.Height(28)))
                        SelectReadme("README_INSTALL.md");
                }
            }
        }

        private void DrawDemoSection()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Generate Sample Demo Assets", EditorStyles.boldLabel);

                EditorGUILayout.LabelField(
                    "This creates:\n" +
                    "• A sample WaveTimelineConfig (3 waves)\n" +
                    "• A sample WaveDirectorConfig\n" +
                    "• Two primitive prefabs (Enemy + Boss) for timeline events\n" +
                    (createScene ? "• A ready-to-play demo scene (optional)\n" : string.Empty) +
                    "\nAll assets are generated inside:\nAssets/TappyTale/WaveDirector/Demo/Generated/",
                    EditorStyles.wordWrappedLabel
                );

                EditorGUILayout.Space(8);

                createScene = EditorGUILayout.ToggleLeft("Also generate a demo scene", createScene);
                overwriteExisting = EditorGUILayout.ToggleLeft("Overwrite existing generated assets", overwriteExisting);

                EditorGUILayout.Space(8);

                using (new EditorGUILayout.HorizontalScope())
                {
                    GUI.enabled = !EditorApplication.isCompiling;

                    if (GUILayout.Button("Create Demo Assets", GUILayout.Height(32)))
                    {
                        try
                        {
                            var result = WaveDirectorDemoAssetGenerator.CreateDemoAssets(
                                overwriteExisting: overwriteExisting,
                                createDemoScene: createScene);

                            EditorPrefs.SetBool(PrefKey + Version, true);

                            if (result.MainAssetToSelect != null)
                            {
                                Selection.activeObject = result.MainAssetToSelect;
                                EditorGUIUtility.PingObject(result.MainAssetToSelect);
                            }

                            EditorUtility.DisplayDialog(
                                "Wave Director",
                                $"Done!\n\nGenerated assets:\n{result.Summary}",
                                "Nice");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                            EditorUtility.DisplayDialog("Wave Director", $"Failed to generate demo assets:\n{ex.Message}", "OK");
                        }
                    }

                    GUI.enabled = true;

                    if (GUILayout.Button("Locate Generated Folder", GUILayout.Height(32)))
                        WaveDirectorDemoAssetGenerator.RevealGeneratedFolderInProject();
                }

                if (EditorApplication.isCompiling)
                {
                    EditorGUILayout.HelpBox("Unity is compiling scripts. Demo generation is disabled until compilation finishes.", MessageType.Info);
                }
            }
        }

        private void DrawLinks()
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                EditorGUILayout.LabelField("Next steps", EditorStyles.boldLabel);

                EditorGUILayout.LabelField(
                    "• Read: Wave Designer mental model and balancing knobs\n" +
                    "• Read: Timeline Editor controls and workflow\n" +
                    "• Read: Runtime API and extension points\n",
                    EditorStyles.wordWrappedLabel
                );

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Open Wave Designer README", GUILayout.Height(28)))
                        SelectReadme("README_WAVE_DESIGNER.md");

                    if (GUILayout.Button("Open Timeline Editor README", GUILayout.Height(28)))
                        SelectReadme("README_TIMELINE_EDITOR.md");
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    if (GUILayout.Button("Open Runtime API README", GUILayout.Height(28)))
                        SelectReadme("README_RUNTIME_API.md");

                    if (GUILayout.Button("Open Extending README", GUILayout.Height(28)))
                        SelectReadme("README_EXTENDING.md");
                }
            }
        }

        private void DrawFooter()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                bool dontShowAgain = EditorPrefs.GetBool(PrefKey + Version, false);
                bool newDontShowAgain = GUILayout.Toggle(dontShowAgain, "Don’t show this again", GUILayout.Width(170));
                if (newDontShowAgain != dontShowAgain)
                    EditorPrefs.SetBool(PrefKey + Version, newDontShowAgain);

                if (GUILayout.Button("Close", GUILayout.Width(90)))
                {
                    EditorPrefs.SetBool(PrefKey + Version, true);
                    Close();
                }
            }
        }

        private static void OpenTimelineEditorWindow()
        {
            // Robust: scan loaded assemblies by type name.
            var type = WaveDirectorDemoAssetGenerator.FindTypeByName("WaveTimelineEditorWindow", mustBeEditorType: true);
            if (type == null)
            {
                EditorUtility.DisplayDialog("Wave Director", "WaveTimelineEditorWindow type not found. Make sure the editor scripts are imported.", "OK");
                return;
            }

            EditorWindow.GetWindow(type);
        }

        private static void SelectReadme(string filename)
        {
            // AssetDatabase.FindAssets expects a search filter (keywords), not a full filename.
            // We search by base name, then verify the extension/path ends with the expected filename.
            string needle = Path.GetFileNameWithoutExtension(filename);

            // Unity defaults to searching both Assets and Packages when no folders are specified,
            // but we do a 2-pass search to be extra robust across versions.
            if (TrySelectBySearch(needle, filename, new[] { "Assets" })) return;
            if (TrySelectBySearch(needle, filename, new[] { "Packages" })) return;
            if (TrySelectBySearch(needle, filename, null)) return;

            // Fallback: try a few common fixed locations.
            var commonPaths = new[]
            {
                $"Assets/TappyTale/WaveDirector/Documentation/{filename}",
                $"Assets/TappyTale/WaveDirector/{filename}",
            };

            foreach (var p in commonPaths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p);
                if (asset == null) continue;

                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
                return;
            }

            EditorUtility.DisplayDialog(
                "Wave Director",
                $"Could not find {filename} in the project.\n\n" +
                "If you installed WaveDirector as a UPM package, check the Project window's 'Packages' view.\n" +
                "Otherwise, import the Documentation folder into Assets (recommended: Assets/TappyTale/WaveDirector/Documentation/).",
                "OK");
        }

        private static bool TrySelectBySearch(string needle, string expectedFilename, string[] searchInFolders)
        {
            string[] guids = searchInFolders == null
                ? AssetDatabase.FindAssets(needle)
                : AssetDatabase.FindAssets(needle, searchInFolders);

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(path)) continue;
                if (!path.EndsWith(expectedFilename, StringComparison.OrdinalIgnoreCase)) continue;

                var asset = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path);
                if (asset == null) continue;

                Selection.activeObject = asset;
                EditorGUIUtility.PingObject(asset);
                return true;
            }

            return false;
        }

    }
}
#endif
