#if UNITY_EDITOR
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TappyTale.WaveDirector.Editor
{
    public static class SimpleBossFightNavMeshBaker
    {
        private const string ScenePath = "Assets/TappyTale/WaveDirector/Samples/Simple Boss Fight/Scenes/SimpleBossFight.unity";

        [MenuItem("TappyTale/Waves/Bake SBF Arena NavMesh", priority = 31)]
        public static void BakeArenaNavMesh()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                Debug.LogError("[WaveDirector] Could not open SimpleBossFight scene.");
                return;
            }

            var arena = GameObject.Find("SBF_Arena");
            if (arena == null)
            {
                Debug.LogError("[WaveDirector] Could not find SBF_Arena in scene.");
                return;
            }

            var surfaceType = WaveDirectorTypeUtil.FindTypeByFullName("Unity.AI.Navigation.NavMeshSurface");
            if (surfaceType == null)
            {
                Debug.LogError("[WaveDirector] Unity.AI.Navigation.NavMeshSurface not found. Install AI Navigation package first.");
                return;
            }

            var surface = arena.GetComponent(surfaceType) ?? arena.AddComponent(surfaceType);
            var buildMethod = surfaceType.GetMethod("BuildNavMesh", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (buildMethod == null)
            {
                Debug.LogError("[WaveDirector] NavMeshSurface.BuildNavMesh() not found.");
                return;
            }

            buildMethod.Invoke(surface, null);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[WaveDirector] Baked NavMesh for SBF_Arena and saved scene.");
        }
    }
}
#endif
