// File: Assets/TappyTale/WaveDirector/Editor/Onboarding/WaveDirectorDemoAssetGenerator.cs
#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
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

            public Result(UnityEngine.Object mainAssetToSelect, string summary)
            {
                MainAssetToSelect = mainAssetToSelect;
                Summary = summary;
            }
        }

        private const string Root = "Assets/TappyTale/WaveDirector";
        private const string DemoGenerated = Root + "/Demo/Generated";
        private const string PrefabsFolder = DemoGenerated + "/Prefabs";
        private const string ConfigsFolder = DemoGenerated + "/Configs";
        private const string ScenesFolder = DemoGenerated + "/Scenes";

        private const string TimelineAssetName = "WD_Demo_Timeline";
        private const string DirectorConfigAssetName = "WD_Demo_DirectorConfig";
        private const string EnemyPrefabName = "WD_Demo_Enemy";
        private const string BossPrefabName = "WD_Demo_Boss";
        private const string SceneName = "WaveDirector_Demo";

        // ---------------------------
        // Public API
        // ---------------------------

        public static Result CreateDemoAssets(bool overwriteExisting, bool createDemoScene)
        {
            EnsureFolders();

            AssetDatabase.StartAssetEditing();
            try
            {
                var enemyPrefab = CreateOrLoadPrimitivePrefab(
                    PrefabsFolder,
                    EnemyPrefabName,
                    PrimitiveType.Capsule,
                    overwriteExisting);

                var bossPrefab = CreateOrLoadPrimitivePrefab(
                    PrefabsFolder,
                    BossPrefabName,
                    PrimitiveType.Sphere,
                    overwriteExisting);

                var timeline = CreateOrLoadTimeline(ConfigsFolder, TimelineAssetName, enemyPrefab, bossPrefab, overwriteExisting);
                var directorCfg = CreateOrLoadDirectorConfig(ConfigsFolder, DirectorConfigAssetName, overwriteExisting);

                UnityEngine.Object mainToSelect = timeline;

                string scenePath = null;
                if (createDemoScene)
                {
                    scenePath = CreateOrUpdateDemoScene(ScenesFolder, SceneName, timeline, directorCfg, overwriteExisting);
                    mainToSelect = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(scenePath);
                }

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                string summary =
                    $"- {AssetDatabase.GetAssetPath(timeline)}\n" +
                    $"- {AssetDatabase.GetAssetPath(directorCfg)}\n" +
                    $"- {AssetDatabase.GetAssetPath(enemyPrefab)}\n" +
                    $"- {AssetDatabase.GetAssetPath(bossPrefab)}\n" +
                    (scenePath != null ? $"- {scenePath}\n" : string.Empty);

                return new Result(mainToSelect, summary);
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
            if (obj != null)
            {
                Selection.activeObject = obj;
                EditorGUIUtility.PingObject(obj);
                return;
            }

            var any = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(ConfigsFolder);
            if (any != null) Selection.activeObject = any;
        }

        /// <summary>
        /// Utility used by the welcome window too.
        /// Finds a type by simple name across loaded assemblies.
        /// </summary>
        internal static Type FindTypeByName(string typeName, bool mustBeEditorType)
        {
            return FindType(typeName, mustBeScriptableObject: false, mustBeMonoBehaviour: false, mustBeEditorType: mustBeEditorType);
        }

        // ---------------------------
        // Folder setup
        // ---------------------------

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
            string full = $"{parent}/{name}";
            if (AssetDatabase.IsValidFolder(full)) return;
            AssetDatabase.CreateFolder(parent, name);
        }

        // ---------------------------
        // Prefab creation
        // ---------------------------

        private static GameObject CreateOrLoadPrimitivePrefab(string folder, string prefabName, PrimitiveType primitive, bool overwrite)
        {
            string path = $"{folder}/{prefabName}.prefab";

            if (overwrite && AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                AssetDatabase.DeleteAsset(path);

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var temp = GameObject.CreatePrimitive(primitive);
            temp.name = prefabName;

            // Add WaveEnemy if present (namespace may vary).
            var waveEnemyType = FindType("WaveEnemy", mustBeScriptableObject: false, mustBeMonoBehaviour: true, mustBeEditorType: false);
            if (waveEnemyType != null)
                temp.AddComponent(waveEnemyType);

            temp.transform.localScale = primitive == PrimitiveType.Sphere ? Vector3.one * 1.6f : Vector3.one;

            var saved = PrefabUtility.SaveAsPrefabAsset(temp, path);
            UnityEngine.Object.DestroyImmediate(temp);

            return saved;
        }

        // ---------------------------
        // ScriptableObjects creation (reflection based, namespace-safe)
        // ---------------------------

        private static ScriptableObject CreateOrLoadTimeline(
            string folder,
            string assetName,
            GameObject enemyPrefab,
            GameObject bossPrefab,
            bool overwrite)
        {
            string path = $"{folder}/{assetName}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (overwrite && existing != null)
            {
                AssetDatabase.DeleteAsset(path);
                existing = null;
            }

            if (existing != null) return existing;

            var timelineType = FindType("WaveTimelineConfig", mustBeScriptableObject: true, mustBeMonoBehaviour: false, mustBeEditorType: false);
            if (timelineType == null)
                throw new InvalidOperationException("Could not find a ScriptableObject type named 'WaveTimelineConfig' in loaded assemblies.");

            var timeline = ScriptableObject.CreateInstance(timelineType);
            AssetDatabase.CreateAsset(timeline, path);

            // Fill with friendly demo values if members exist.
            TrySetNumber(timeline, "WaveCount", 3);
            TrySetNumber(timeline, "DefaultWaveDurationSeconds", 18f);

            TryInvoke(timeline, "EnsureWaveDefinitions");

            // Waves list
            var wavesList = GetOrCreateList(timeline, "Waves");
            if (wavesList != null)
            {
                EnsureListSize(wavesList, 3);

                ConfigureWave(wavesList, 0, "Warmup (Survive)", "Timer", triggerTime: 12f, enemyHpScale: null, enemySpeedScale: null);
                ConfigureWave(wavesList, 1, "Pressure (Survive)", "Timer", triggerTime: 15f, enemyHpScale: 1.1f, enemySpeedScale: 1.05f);
                ConfigureWave(wavesList, 2, "Boss (Survive)", "Timer", triggerTime: 18f, enemyHpScale: 1.25f, enemySpeedScale: null);
            }

            // Events list
            var eventsList = GetOrCreateList(timeline, "Events");
            if (eventsList != null)
            {
                eventsList.Clear();

                AddSpawns(eventsList, waveIndex: 0, enemyPrefab, isBoss: false, 0f, 2f, 4f, 6f, 8f);
                AddSpawns(eventsList, waveIndex: 1, enemyPrefab, isBoss: false, 0f, 1.5f, 3f, 4.5f, 6f, 7.5f, 9f);
                AddSpawns(eventsList, waveIndex: 2, enemyPrefab, isBoss: false, 0f, 2f, 4f, 6f);
                AddSpawns(eventsList, waveIndex: 2, bossPrefab, isBoss: true, 7f);
            }

            EditorUtility.SetDirty(timeline);
            AssetDatabase.ImportAsset(path);

            return timeline;
        }

        private static ScriptableObject CreateOrLoadDirectorConfig(string folder, string assetName, bool overwrite)
        {
            string path = $"{folder}/{assetName}.asset";

            var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (overwrite && existing != null)
            {
                AssetDatabase.DeleteAsset(path);
                existing = null;
            }

            if (existing != null) return existing;

            var configType = FindType("WaveDirectorConfig", mustBeScriptableObject: true, mustBeMonoBehaviour: false, mustBeEditorType: false);
            if (configType == null)
                throw new InvalidOperationException("Could not find a ScriptableObject type named 'WaveDirectorConfig' in loaded assemblies.");

            var cfg = ScriptableObject.CreateInstance(configType);
            AssetDatabase.CreateAsset(cfg, path);

            // Attempt to set demo-friendly values (safe if fields differ).
            TrySetEnum(cfg, "Mode", "Adaptive", fallbackInt: 2);
            TrySetNumber(cfg, "BaseDifficulty", 1f);
            TrySetNumber(cfg, "MinDifficulty", 0.6f);
            TrySetNumber(cfg, "MaxDifficulty", 2.0f);
            TrySetNumber(cfg, "TargetClearTimeSeconds", 20f);
            TrySetNumber(cfg, "TargetDamageTaken", 20f);
            TrySetNumber(cfg, "TargetDeaths", 0f);
            TrySetNumber(cfg, "AdaptiveGain", 0.3f);
            TrySetNumber(cfg, "AdaptiveSmoothing", 0.8f);
            TrySetNumber(cfg, "MaxDeltaPerWave", 0.25f);

            EditorUtility.SetDirty(cfg);
            AssetDatabase.ImportAsset(path);

            return cfg;
        }

        // ---------------------------
        // Scene wiring (reflection based)
        // ---------------------------

        private static string CreateOrUpdateDemoScene(
            string folder,
            string sceneName,
            ScriptableObject timeline,
            ScriptableObject directorCfg,
            bool overwrite)
        {
            string path = $"{folder}/{sceneName}.unity";

            if (overwrite && AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                AssetDatabase.DeleteAsset(path);

            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) != null)
                return path;

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var waveDirectorType = FindType("WaveDirector", mustBeScriptableObject: false, mustBeMonoBehaviour: true, mustBeEditorType: false);
            if (waveDirectorType == null)
                throw new InvalidOperationException("Could not find a MonoBehaviour type named 'WaveDirector'.");

            var spawnerType = FindType("WaveSpawner", mustBeScriptableObject: false, mustBeMonoBehaviour: true, mustBeEditorType: false);
            var poolRegistryType = FindType("PoolRegistry", mustBeScriptableObject: false, mustBeMonoBehaviour: true, mustBeEditorType: false);

            var perfProviderType = FindType("BasicPlayerPerformanceProvider", mustBeScriptableObject: false, mustBeMonoBehaviour: true, mustBeEditorType: false);
            var rewardHandlerType = FindType("BasicWaveRewardHandler", mustBeScriptableObject: false, mustBeMonoBehaviour: true, mustBeEditorType: false);

            var waveSystem = new GameObject("WaveSystem");
            var director = waveSystem.AddComponent(waveDirectorType);

            Component perf = null;
            Component rewards = null;

            if (perfProviderType != null) perf = waveSystem.AddComponent(perfProviderType);
            if (rewardHandlerType != null) rewards = waveSystem.AddComponent(rewardHandlerType);

            // Assign serialized references if those fields exist.
            var so = new SerializedObject(director);
            TryAssignObjectRef(so, "timelineConfig", timeline);
            TryAssignObjectRef(so, "directorConfig", directorCfg);
            if (perf != null) TryAssignObjectRef(so, "playerPerformanceProviderBehaviour", perf);
            if (rewards != null) TryAssignObjectRef(so, "rewardHandlerBehaviour", rewards);
            so.ApplyModifiedPropertiesWithoutUndo();

            if (spawnerType != null)
            {
                var spawnerA = new GameObject("Spawner_A");
                spawnerA.transform.position = new Vector3(-3f, 0f, 0f);
                spawnerA.AddComponent(spawnerType);

                var spawnerB = new GameObject("Spawner_B");
                spawnerB.transform.position = new Vector3(3f, 0f, 0f);
                spawnerB.AddComponent(spawnerType);
            }

            if (poolRegistryType != null)
            {
                var poolGo = new GameObject("PoolRegistry");
                poolGo.AddComponent(poolRegistryType);
            }

            EditorSceneManager.SaveScene(scene, path);
            AssetDatabase.ImportAsset(path);

            return path;
        }

        private static void TryAssignObjectRef(SerializedObject so, string propertyName, UnityEngine.Object value)
        {
            var p = so.FindProperty(propertyName);
            if (p == null) return;
            if (p.propertyType != SerializedPropertyType.ObjectReference) return;

            p.objectReferenceValue = value;
        }

        // ---------------------------
        // Timeline population helpers
        // ---------------------------

        private static void ConfigureWave(IList wavesList, int index, string name, string triggerEnumName, float triggerTime, float? enemyHpScale, float? enemySpeedScale)
        {
            if (wavesList == null) return;
            if (index < 0 || index >= wavesList.Count) return;

            var wave = wavesList[index];
            if (wave == null) return;

            TrySetString(wave, "Name", name);
            TrySetEnum(wave, "TriggerType", triggerEnumName, fallbackInt: 0);
            TrySetNumber(wave, "TriggerTimeSeconds", triggerTime);

            if (enemyHpScale.HasValue) TrySetNumber(wave, "EnemyHealthScale", enemyHpScale.Value);
            if (enemySpeedScale.HasValue) TrySetNumber(wave, "EnemySpeedScale", enemySpeedScale.Value);
        }

        private static void AddSpawns(IList eventsList, int waveIndex, GameObject prefab, bool isBoss, params float[] times)
        {
            if (eventsList == null || times == null || prefab == null) return;

            var elemType = GetListElementType(eventsList);
            if (elemType == null) return;

            for (int i = 0; i < times.Length; i++)
            {
                object e = CreateListElementInstance(elemType);
                if (e == null) continue;

                TrySetNumber(e, "WaveIndex", waveIndex);
                TrySetObject(e, "Prefab", prefab);
                TrySetNumber(e, "SpawnTime", Mathf.Max(0f, times[i]));
                TrySetBool(e, "IsBoss", isBoss);

                eventsList.Add(e);
            }
        }

        // ---------------------------
        // Reflection helpers
        // ---------------------------

        private static Type FindType(string typeName, bool mustBeScriptableObject, bool mustBeMonoBehaviour, bool mustBeEditorType)
        {
            Type best = null;
            int bestScore = int.MinValue;

            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (asm == null) continue;
                if (asm.IsDynamic) continue;

                Type[] types = null;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types; }
                catch { continue; }

                if (types == null) continue;

                for (int i = 0; i < types.Length; i++)
                {
                    var t = types[i];
                    if (t == null) continue;

                    if (!string.Equals(t.Name, typeName, StringComparison.Ordinal) &&
                        !string.Equals(t.FullName, typeName, StringComparison.Ordinal))
                        continue;

                    if (mustBeScriptableObject && !typeof(ScriptableObject).IsAssignableFrom(t)) continue;
                    if (mustBeMonoBehaviour && !typeof(MonoBehaviour).IsAssignableFrom(t)) continue;
                    if (mustBeEditorType && !typeof(EditorWindow).IsAssignableFrom(t)) continue;

                    // Prefer TappyTale.* (especially TappyTale.WaveDirector.*), but allow any match.
                    int score = 0;
                    if (t.FullName != null && string.Equals(t.FullName, typeName, StringComparison.Ordinal)) score += 200;
                    if (t.Namespace != null && t.Namespace.StartsWith("TappyTale.WaveDirector", StringComparison.Ordinal)) score += 100;
                    else if (t.Namespace != null && t.Namespace.StartsWith("TappyTale", StringComparison.Ordinal)) score += 50;

                    // Prefer non-editor assemblies for runtime types.
                    if (!mustBeEditorType)
                    {
                        string asmName = asm.GetName().Name ?? string.Empty;
                        if (asmName.IndexOf("Editor", StringComparison.OrdinalIgnoreCase) >= 0) score -= 10;
                    }

                    if (score > bestScore)
                    {
                        bestScore = score;
                        best = t;
                    }
                }
            }

            return best;
        }

        private static void TryInvoke(object obj, string methodName)
        {
            if (obj == null) return;
            var t = obj.GetType();
            var m = t.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m == null) return;
            if (m.GetParameters().Length != 0) return;

            try { m.Invoke(obj, null); }
            catch { /* intentionally swallowed */ }
        }

        private static void TrySetString(object obj, string member, string value)
        {
            TrySetValue(obj, member, value);
        }

        private static void TrySetBool(object obj, string member, bool value)
        {
            TrySetValue(obj, member, value);
        }

        private static void TrySetObject(object obj, string member, UnityEngine.Object value)
        {
            TrySetValue(obj, member, value);
        }

        private static void TrySetNumber(object obj, string member, float value)
        {
            TrySetValue(obj, member, value);
        }

        private static void TrySetNumber(object obj, string member, int value)
        {
            TrySetValue(obj, member, value);
        }

        private static void TrySetEnum(object obj, string member, string enumName, int fallbackInt)
        {
            if (obj == null) return;

            var t = obj.GetType();
            var f = t.GetField(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType.IsEnum)
            {
                try
                {
                    var v = Enum.Parse(f.FieldType, enumName, ignoreCase: true);
                    f.SetValue(obj, v);
                }
                catch
                {
                    try { f.SetValue(obj, Enum.ToObject(f.FieldType, fallbackInt)); } catch { }
                }
                return;
            }

            var p = t.GetProperty(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanWrite && p.PropertyType.IsEnum)
            {
                try
                {
                    var v = Enum.Parse(p.PropertyType, enumName, ignoreCase: true);
                    p.SetValue(obj, v);
                }
                catch
                {
                    try { p.SetValue(obj, Enum.ToObject(p.PropertyType, fallbackInt)); } catch { }
                }
            }
        }

        private static void TrySetValue(object obj, string member, object value)
        {
            if (obj == null) return;

            var t = obj.GetType();

            var f = t.GetField(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
            {
                try
                {
                    object converted = ConvertIfNeeded(value, f.FieldType);
                    f.SetValue(obj, converted);
                }
                catch { }
                return;
            }

            var p = t.GetProperty(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanWrite)
            {
                try
                {
                    object converted = ConvertIfNeeded(value, p.PropertyType);
                    p.SetValue(obj, converted);
                }
                catch { }
            }
        }

        private static object ConvertIfNeeded(object value, Type targetType)
        {
            if (value == null) return null;

            if (targetType.IsInstanceOfType(value)) return value;

            try
            {
                if (targetType == typeof(float))
                {
                    if (value is int i) return (float)i;
                    if (value is double d) return (float)d;
                    if (value is string s && float.TryParse(s, out var f)) return f;
                }

                if (targetType == typeof(int))
                {
                    if (value is float f) return Mathf.RoundToInt(f);
                    if (value is double d) return (int)Math.Round(d);
                    if (value is string s && int.TryParse(s, out var i)) return i;
                }

                if (targetType == typeof(string))
                    return value.ToString();

                if (targetType == typeof(bool))
                {
                    if (value is string s && bool.TryParse(s, out var b)) return b;
                }
            }
            catch { }

            return value;
        }

        private static IList GetOrCreateList(object owner, string memberName)
        {
            if (owner == null) return null;
            var t = owner.GetType();

            // Field
            var f = t.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
            {
                var current = f.GetValue(owner) as IList;
                if (current != null) return current;

                var elemType = GetListElementType(f.FieldType);
                if (elemType == null) return null;

                var listType = typeof(System.Collections.Generic.List<>).MakeGenericType(elemType);
                var list = (IList)Activator.CreateInstance(listType);
                f.SetValue(owner, list);
                return list;
            }

            // Property
            var p = t.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanRead && p.CanWrite)
            {
                var current = p.GetValue(owner, null) as IList;
                if (current != null) return current;

                var elemType = GetListElementType(p.PropertyType);
                if (elemType == null) return null;

                var listType = typeof(System.Collections.Generic.List<>).MakeGenericType(elemType);
                var list = (IList)Activator.CreateInstance(listType);
                p.SetValue(owner, list, null);
                return list;
            }

            return null;
        }

        private static void EnsureListSize(IList list, int size)
        {
            if (list == null) return;

            var elemType = GetListElementType(list);
            if (elemType == null) return;

            while (list.Count < size)
            {
                list.Add(CreateListElementInstance(elemType));
            }

            while (list.Count > size)
            {
                list.RemoveAt(list.Count - 1);
            }
        }

        private static Type GetListElementType(IList list)
        {
            if (list == null) return null;
            return GetListElementType(list.GetType());
        }

        private static Type GetListElementType(Type listType)
        {
            if (listType == null) return null;

            if (listType.IsArray) return listType.GetElementType();

            if (listType.IsGenericType)
            {
                var args = listType.GetGenericArguments();
                if (args != null && args.Length == 1) return args[0];
            }

            // Try IList<T> interfaces
            var ilistT = listType.GetInterfaces()
                .FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(System.Collections.Generic.IList<>));

            if (ilistT != null)
            {
                var args = ilistT.GetGenericArguments();
                if (args != null && args.Length == 1) return args[0];
            }

            return null;
        }

        private static object CreateListElementInstance(Type elemType)
        {
            if (elemType == null) return null;

            try
            {
                if (elemType.IsValueType) return Activator.CreateInstance(elemType);
                return Activator.CreateInstance(elemType);
            }
            catch
            {
                return null;
            }
        }
    }
}
#endif
