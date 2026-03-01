#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using TappyTale.WaveDirector.Runtime;

namespace TappyTale.WaveDirector.Editor
{
    [CustomEditor(typeof(WaveTimelineConfig))]
    public sealed class WaveTimelineConfigInspector : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(8);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Open Wave Timeline Editor", GUILayout.Height(28)))
                {
                    WaveTimelineEditorWindow.Open((WaveTimelineConfig)target);
                }

                if (GUILayout.Button("Validate", GUILayout.Height(28)))
                {
                    var cfg = (WaveTimelineConfig)target;
                    cfg.EnsureWaveDefinitions();
                    EditorUtility.SetDirty(cfg);
                    AssetDatabase.SaveAssets();
                }
            }
        }
    }
}
#endif
