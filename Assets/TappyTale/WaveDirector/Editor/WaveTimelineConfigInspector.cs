#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace TappyTale
{
    [CustomEditor(typeof(WaveTimelineConfig))]
    public sealed class WaveTimelineConfigInspector : Editor
    {
        public override void OnInspectorGUI()
        {
            var previousSkin = GUI.skin;
            GUI.skin = EditorGUIUtility.GetBuiltinSkin(EditorSkin.Inspector);
            try
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
            finally
            {
                GUI.skin = previousSkin;
            }
        }
    }
}
#endif
