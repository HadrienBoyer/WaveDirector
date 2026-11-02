#if UNITY_EDITOR
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace TappyTale.WaveDirector.Editor
{
    internal static class WaveDirectorDemoAssetGenerator
    {
        internal readonly struct Result
        {
            public readonly UnityEngine.Object MainAssetToSelect;
            public readonly string Summary;

            public Result(UnityEngine.Object main, string summary)
            {
                MainAssetToSelect = main;
                Summary = summary;
            }
        }

        private const string Root = "Assets/TappyTale/WaveDirector";
        private const string DemoGenerated = Root + "/Demo/Generated";
        private const string PrefabsFolder = DemoGenerated + "/Prefabs";
        private const string ConfigsFolder = DemoGenerated + "/Configs";
        private const string ScenesFolder = DemoGenerated + "/Scenes";

        public static Result CreateDemoAssets(bool overwriteExisting, bool createDemoScene)
        {
            EnsureFolders();

            AssetDatabase.StartAssetEditing();
            try
            {
                var enemyPrefab = CreateOrLoadPrimitivePrefab(PrefabsFolder, "WD_Demo_Enemy", PrimitiveType.Capsule, overwriteExisting);
                var bossPrefab = CreateOrLoadPrimitivePrefab(PrefabsFolder, "WD_Demo_Boss", PrimitiveType.Sphere, overwriteExisting);

                var timeline = CreateOrLoadSO("WaveTimelineConfig", ConfigsFolder + "/WD_Demo_Timeline.asset", overwriteExisting);
                var directorCfg = CreateOrLoadSO("WaveDirectorConfig", ConfigsFolder + "/WD_Demo_DirectorConfig.asset", overwriteExisting);

                FillTimelineIfPossible(timeline, enemyPrefab, bossPrefab);

                UnityEngine.Object main = timeline;
                string scenePath = null;

                if (createDemoScene)
                {
                    scenePath = CreateDemoScene(ScenesFolder + "/WaveDirector_Demo.unity", timeline, directorCfg, overwriteExisting);
                    main = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(scenePath);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                string summary =
                    "- " + AssetDatabase.GetAssetPath(timeline) + "\n" +
                    "- " + AssetDatabase.GetAssetPath(directorCfg) + "\n" +
                    "- " + AssetDatabase.GetAssetPath(enemyPrefab) + "\n" +
                    "- " + AssetDatabase.GetAssetPath(bossPrefab) + "\n" +
                    (scenePath != null ? "- " + scenePath + "\n" : string.Empty);

                return new Result(main, summary);
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
            }
        }

        public static void RevealGeneratedFolderInProject()
        {
            EnsureFolders();
            var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(DemoGenerated);
            if (obj != null) { Selection.activeObject = obj; EditorGUIUtility.PingObject(obj); }
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "TappyTale");
            EnsureFolder("Assets/TappyTale", "WaveDirector");
            EnsureFolder(Root, "Demo");
            EnsureFolder(Root + "/Demo", "Generated");
            EnsureFolder(DemoGenerated, "Prefabs");
            EnsureFolder(DemoGenerated, "Configs");
            EnsureFolder(DemoGenerated, "Scenes");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string full = parent + "/" + name;
            if (AssetDatabase.IsValidFolder(full)) return;
            AssetDatabase.CreateFolder(parent, name);
        }

        private static GameObject CreateOrLoadPrimitivePrefab(string folder, string prefabName, PrimitiveType primitive, bool overwrite)
        {
            string path = folder + "/" + prefabName + ".prefab";
            if (overwrite && AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) AssetDatabase.DeleteAsset(path);

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var temp = GameObject.CreatePrimitive(primitive);
            temp.name = prefabName;

            var waveEnemyType = WaveDirectorTypeUtil.FindTypeByName("WaveEnemy");
            if (waveEnemyType != null && typeof(MonoBehaviour).IsAssignableFrom(waveEnemyType))
                temp.AddComponent(waveEnemyType);

            var saved = PrefabUtility.SaveAsPrefabAsset(temp, path);
            UnityEngine.Object.DestroyImmediate(temp);
            return saved;
        }

        private static ScriptableObject CreateOrLoadSO(string typeName, string path, bool overwrite)
        {
            var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (overwrite && existing != null) { AssetDatabase.DeleteAsset(path); existing = null; }
            if (existing != null) return existing;

            var t = WaveDirectorTypeUtil.FindTypeByName(typeName);
            if (t == null || !typeof(ScriptableObject).IsAssignableFrom(t))
                throw new InvalidOperationException("Type not found or not ScriptableObject: " + typeName);

            var so = ScriptableObject.CreateInstance(t);
            AssetDatabase.CreateAsset(so, path);
            EditorUtility.SetDirty(so);
            return so;
        }

        private static void FillTimelineIfPossible(ScriptableObject timeline, GameObject enemyPrefab, GameObject bossPrefab)
        {
            if (timeline == null) return;

            TrySet(timeline, "WaveCount", 3);
            TrySet(timeline, "DefaultWaveDurationSeconds", 18f);
            TryInvoke(timeline, "EnsureWaveDefinitions");

            var eventsList = GetList(timeline, "Events");
            if (eventsList == null) return;

            eventsList.Clear();

            AddEvent(eventsList, 0, enemyPrefab, false, 0f);
            AddEvent(eventsList, 0, enemyPrefab, false, 2f);
            AddEvent(eventsList, 0, enemyPrefab, false, 4f);

            AddEvent(eventsList, 1, enemyPrefab, false, 0f);
            AddEvent(eventsList, 1, enemyPrefab, false, 1.5f);
            AddEvent(eventsList, 1, enemyPrefab, false, 3f);

            AddEvent(eventsList, 2, enemyPrefab, false, 0f);
            AddEvent(eventsList, 2, bossPrefab, true, 6f);

            EditorUtility.SetDirty(timeline);
        }

        private static void AddEvent(IList list, int waveIndex, GameObject prefab, bool isBoss, float spawnTime)
        {
            var elemType = GetListElementType(list);
            if (elemType == null) return;
            object e = Activator.CreateInstance(elemType);

            TrySet(e, "WaveIndex", waveIndex);
            TrySet(e, "Prefab", prefab);
            TrySet(e, "IsBoss", isBoss);
            TrySet(e, "SpawnTime", spawnTime);

            list.Add(e);
        }

        private static string CreateDemoScene(string path, ScriptableObject timeline, ScriptableObject directorCfg, bool overwrite)
        {
            if (overwrite && AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) AssetDatabase.DeleteAsset(path);
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null) return path;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var waveSystem = new GameObject("WaveSystem");
            var directorType = WaveDirectorTypeUtil.FindTypeByName("WaveDirector");
            var spawnerType = WaveDirectorTypeUtil.FindTypeByName("WaveSpawner");
            var poolType = WaveDirectorTypeUtil.FindTypeByName("PoolRegistry");

            var director = directorType != null ? waveSystem.AddComponent(directorType) : null;

            if (director != null)
            {
                var so = new SerializedObject(director);
                Assign(so, "timelineConfig", timeline);
                Assign(so, "directorConfig", directorCfg);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            if (spawnerType != null)
            {
                var a = new GameObject("Spawner_A"); a.transform.position = new Vector3(-3,0,0); a.AddComponent(spawnerType);
                var b = new GameObject("Spawner_B"); b.transform.position = new Vector3(3,0,0); b.AddComponent(spawnerType);
            }

            if (poolType != null)
            {
                var pr = new GameObject("PoolRegistry");
                pr.AddComponent(poolType);
            }

            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.ImportAsset(path);
            return path;
        }

        private static void Assign(SerializedObject so, string prop, UnityEngine.Object value)
        {
            var p = so.FindProperty(prop);
            if (p == null) return;
            if (p.propertyType != SerializedPropertyType.ObjectReference) return;
            p.objectReferenceValue = value;
        }

        private static void TryInvoke(object obj, string method)
        {
            var m = obj.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m == null || m.GetParameters().Length != 0) return;
            try { m.Invoke(obj, null); } catch { }
        }

        private static void TrySet(object obj, string member, object value)
        {
            if (obj == null) return;
            var t = obj.GetType();

            var f = t.GetField(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
            {
                try { f.SetValue(obj, ConvertTo(value, f.FieldType)); } catch { }
                return;
            }

            var p = t.GetProperty(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanWrite)
            {
                try { p.SetValue(obj, ConvertTo(value, p.PropertyType)); } catch { }
            }
        }

        private static object ConvertTo(object value, Type t)
        {
            if (value == null) return null;
            if (t.IsInstanceOfType(value)) return value;

            if (t == typeof(float) && value is int i) return (float)i;
            if (t == typeof(int) && value is float f) return Mathf.RoundToInt(f);
            return value;
        }

        private static IList GetList(object owner, string member)
        {
            var t = owner.GetType();
            var f = t.GetField(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null) return f.GetValue(owner) as IList;

            var p = t.GetProperty(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanRead) return p.GetValue(owner, null) as IList;

            return null;
        }

        private static Type GetListElementType(IList list)
        {
            var lt = list.GetType();
            if (lt.IsArray) return lt.GetElementType();
            if (lt.IsGenericType) return lt.GetGenericArguments().FirstOrDefault();
            return null;
        }
    }
}
#endif
