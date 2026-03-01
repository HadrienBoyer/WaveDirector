#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TappyTale.WaveDirector.Editor
{
    public sealed class WaveDirectorWelcomeWindow : EditorWindow
    {
        private const string Version = "1.1.0";
        private const string PrefKey = "TT_WaveDirector_WelcomeShown_";

        [MenuItem("TappyTale/Waves/Wave Director Welcome", priority = 0)]
        public static void OpenFromMenu() => ShowWindow(true);

        internal static void ShowWindow(bool focus)
        {
            var w = GetWindow<WaveDirectorWelcomeWindow>(utility: true, title: "Wave Director");
            w.minSize = new Vector2(760, 680);
            if (focus) w.Focus();
            w.Show();
        }

        public void CreateGUI()
        {
            var root = rootVisualElement;
            root.style.paddingLeft = 12;
            root.style.paddingRight = 12;
            root.style.paddingTop = 12;
            root.style.paddingBottom = 12;

            root.Add(BuildHeader());
            root.Add(BuildScroll());
            root.Add(BuildFooter());
        }

        private VisualElement BuildHeader()
        {
            var header = new VisualElement();
            header.style.flexDirection = FlexDirection.Row;
            header.style.justifyContent = Justify.SpaceBetween;
            header.style.alignItems = Align.Center;
            header.style.marginBottom = 10;

            var left = new VisualElement();
            left.style.flexDirection = FlexDirection.Column;

            var title = new Label("Wave Director - Enemy AI and Boss Rush Wave Spawner Tools");
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.fontSize = 16;

            var sub = new Label($"Version {Version} • Author: Tappy Tale • Docs name: Wave Editor");
            sub.style.opacity = 0.75f;

            left.Add(title);
            left.Add(sub);

            var right = new VisualElement();
            right.style.flexDirection = FlexDirection.Row;
            var openTimelineButton = MakeButton("Open Timeline Editor", () =>
            {
                var t = WaveDirectorTypeUtil.FindTypeByName("WaveTimelineEditorWindow");
                if (t == null)
                {
                    EditorUtility.DisplayDialog("Wave Director", "WaveTimelineEditorWindow not found.", "OK");
                    return;
                }
                EditorWindow.GetWindow(t);
            });
            openTimelineButton.style.marginRight = 8;
            right.Add(openTimelineButton);

            right.Add(MakeButton("Open Install README", () => SelectReadme("README_INSTALL.md")));

            header.Add(left);
            header.Add(right);
            return header;
        }

        private VisualElement BuildScroll()
        {
            var scroll = new ScrollView();
            scroll.style.flexGrow = 1;

            scroll.Add(Card("Welcome",
                "Wave Director lets designers build waves on a timeline (spawn prefab at time T on lane waveIndex). " +
                "Runtime plays waves with completion triggers (Timer / KillCount / Objective), optional Adaptive difficulty, pooling, and save/analytics."));

            scroll.Add(Card("Quick start",
                "1) Create WaveTimelineConfig\n" +
                "2) Optional: WaveDirectorConfig\n" +
                "3) Scene: WaveDirector + 1+ WaveSpawner(s) + PoolRegistry\n" +
                "4) Open: TappyTale → Waves → Wave Timeline Editor\n" +
                "5) Drag prefabs into lanes, press Play"));

            bool hasSurface = WaveDirectorTypeUtil.FindTypeByFullName("Unity.AI.Navigation.NavMeshSurface") != null;
            scroll.Add(Card("Environment checks",
                $"AI Navigation NavMeshSurface: {(hasSurface ? "Detected" : "Not detected")}\n" +
                "Sample uses NavMeshAgent; if NavMeshSurface exists, we bake automatically."));

            var demo = CardContainer("Generate demo assets");
            var demoDescription = new Label("Creates demo configs + prefabs, and optionally a demo scene.");
            demoDescription.style.whiteSpace = WhiteSpace.Normal;
            demoDescription.style.marginTop = 4;
            demo.Add(demoDescription);

            var demoCreateScene = new Toggle("Also generate a demo scene") { value = true };
            var demoOverwrite = new Toggle("Overwrite existing generated assets") { value = false };
            demo.Add(demoCreateScene);
            demo.Add(demoOverwrite);

            var demoRow = new VisualElement();
            demoRow.style.flexDirection = FlexDirection.Row;
            demoRow.style.marginTop = 6;

            var createDemoAssetsButton = MakeButton("Create Demo Assets", () =>
            {
                try
                {
                    var result = WaveDirectorDemoAssetGenerator.CreateDemoAssets(demoOverwrite.value, demoCreateScene.value);
                    if (result.MainAssetToSelect != null)
                    {
                        Selection.activeObject = result.MainAssetToSelect;
                        EditorGUIUtility.PingObject(result.MainAssetToSelect);
                    }
                    EditorPrefs.SetBool(PrefKey + Version, true);
                    EditorUtility.DisplayDialog("Wave Director", "Done!\n\n" + result.Summary, "OK");
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    EditorUtility.DisplayDialog("Wave Director", "Failed:\n" + ex.Message, "OK");
                }
            });
            createDemoAssetsButton.style.marginRight = 8;
            demoRow.Add(createDemoAssetsButton);

            demoRow.Add(MakeButton("Locate Demo Folder", WaveDirectorDemoAssetGenerator.RevealGeneratedFolderInProject));
            demo.Add(demoRow);
            scroll.Add(demo);

            var sample = CardContainer("Install sample: Simple Boss Fight");
            var sampleDescription = new Label(
                "Generates a plug-and-play mini-game into:\n" +
                "Assets/TappyTale/WaveDirector/Samples/Simple Boss Fight/\n\n" +
                "Includes: scene, prefabs, configs, and scripts.");
            sampleDescription.style.whiteSpace = WhiteSpace.Normal;
            sampleDescription.style.marginTop = 4;
            sample.Add(sampleDescription);

            var sampleRow = new VisualElement();
            sampleRow.style.flexDirection = FlexDirection.Row;
            sampleRow.style.marginTop = 6;

            var generateSampleButton = MakeButton("Generate / Reinstall Sample", () =>
            {
                try
                {
                    var res = SimpleBossFightSampleGenerator.GenerateOrUpdate(overwrite: true);
                    if (res.MainAssetToSelect != null)
                    {
                        Selection.activeObject = res.MainAssetToSelect;
                        EditorGUIUtility.PingObject(res.MainAssetToSelect);
                    }
                    EditorUtility.DisplayDialog("Wave Director", "Sample generated!\n\n" + res.Summary, "OK");
                }
                catch (Exception ex)
                {
                    Debug.LogException(ex);
                    EditorUtility.DisplayDialog("Wave Director", "Sample generation failed:\n" + ex.Message, "OK");
                }
            });
            generateSampleButton.style.marginRight = 8;
            sampleRow.Add(generateSampleButton);

            sampleRow.Add(MakeButton("Open Sample Folder", () =>
            {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(SimpleBossFightSampleGenerator.RootFolder);
                if (obj != null) { Selection.activeObject = obj; EditorGUIUtility.PingObject(obj); }
            }));

            sample.Add(sampleRow);
            scroll.Add(sample);

            scroll.Add(Card("Docs map",
                "• README_INSTALL.md\n• README_WAVE_DESIGNER.md\n• README_TIMELINE_EDITOR.md\n• README_RUNTIME_API.md\n• README_EXTENDING.md\n• README_SAVE_ANALYTICS.md\n• README_AI_DIRECTOR.md\n• README_VERSIONING.md + CHANGELOG.md"));

            return scroll;
        }

        private VisualElement BuildFooter()
        {
            var footer = new VisualElement();
            footer.style.flexDirection = FlexDirection.Row;
            footer.style.justifyContent = Justify.SpaceBetween;
            footer.style.alignItems = Align.Center;
            footer.style.marginTop = 10;

            var dontShow = new Toggle("Don’t show this again");
            dontShow.value = EditorPrefs.GetBool(PrefKey + Version, false);
            dontShow.RegisterValueChangedCallback(evt => EditorPrefs.SetBool(PrefKey + Version, evt.newValue));

            var close = MakeButton("Close", () =>
            {
                EditorPrefs.SetBool(PrefKey + Version, true);
                Close();
            });

            footer.Add(dontShow);
            footer.Add(close);
            return footer;
        }

        private static Button MakeButton(string text, Action onClick)
        {
            var b = new Button(onClick) { text = text };
            b.style.height = 28;
            b.style.paddingLeft = 12;
            b.style.paddingRight = 12;
            return b;
        }

        private static VisualElement Card(string title, string body)
        {
            var card = CardContainer(title);
            var label = new Label(body);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginTop = 4;
            card.Add(label);
            return card;
        }

        private static VisualElement CardContainer(string title)
        {
            var box = new VisualElement();
            box.style.borderTopWidth = 1;
            box.style.borderBottomWidth = 1;
            box.style.borderLeftWidth = 1;
            box.style.borderRightWidth = 1;
            box.style.borderTopColor = new Color(0, 0, 0, 0.25f);
            box.style.borderBottomColor = new Color(0, 0, 0, 0.25f);
            box.style.borderLeftColor = new Color(0, 0, 0, 0.25f);
            box.style.borderRightColor = new Color(0, 0, 0, 0.25f);
            box.style.borderTopLeftRadius = 6;
            box.style.borderTopRightRadius = 6;
            box.style.borderBottomLeftRadius = 6;
            box.style.borderBottomRightRadius = 6;
            box.style.paddingLeft = 12;
            box.style.paddingRight = 12;
            box.style.paddingTop = 10;
            box.style.paddingBottom = 10;
            box.style.marginBottom = 10;

            var h = new Label(title);
            h.style.unityFontStyleAndWeight = FontStyle.Bold;
            h.style.fontSize = 13;
            box.Add(h);

            return box;
        }

        private static void SelectReadme(string filename)
        {
            string needle = Path.GetFileNameWithoutExtension(filename);

            if (TrySelectBySearch(needle, filename, new[] { "Assets" })) return;
            if (TrySelectBySearch(needle, filename, new[] { "Packages" })) return;
            if (TrySelectBySearch(needle, filename, null)) return;

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
                $"Could not find {filename}.\n\n" +
                "If installed as a UPM package, check the Project window's 'Packages' view.\n" +
                "Otherwise, import Documentation into Assets/TappyTale/WaveDirector/Documentation/.",
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
