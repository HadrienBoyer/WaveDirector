#if UNITY_EDITOR
using System;
using System.Collections;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;

namespace TappyTale.WaveDirector.Editor
{
    internal static class SimpleBossFightSampleGenerator
    {
        public const string RootFolder = "Assets/TappyTale/WaveDirector/Samples/Simple Boss Fight";
        private const string PrefabsFolder = RootFolder + "/Prefabs";
        private const string ScenesFolder = RootFolder + "/Scenes";
        private const string ConfigsFolder = RootFolder + "/Configs";
        private const string ScenePath = ScenesFolder + "/SimpleBossFight.unity";

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

        public static Result GenerateOrUpdate(bool overwrite)
        {
            EnsureFolders();

            if (overwrite)
            {
                TryDelete(ScenePath);
                TryDelete(ConfigsFolder + "/SBF_Timeline.asset");
                TryDelete(ConfigsFolder + "/SBF_DirectorConfig.asset");
                TryDelete(PrefabsFolder + "/SBF_Player.prefab");
                TryDelete(PrefabsFolder + "/SBF_Minion.prefab");
                TryDelete(PrefabsFolder + "/SBF_Boss.prefab");
                TryDelete(PrefabsFolder + "/SBF_Spawner_A.prefab");
                TryDelete(PrefabsFolder + "/SBF_Spawner_B.prefab");
                TryDelete(PrefabsFolder + "/SBF_Arena.prefab");
            }

            var minionPrefab = CreateMinionPrefab(PrefabsFolder + "/SBF_Minion.prefab");
            var bossPrefab = CreateBossPrefab(PrefabsFolder + "/SBF_Boss.prefab");
            var arenaPrefab = CreateArenaPrefab(PrefabsFolder + "/SBF_Arena.prefab");
            var playerPrefab = CreatePlayerPrefab(PrefabsFolder + "/SBF_Player.prefab");

            var timeline = CreateTimeline(ConfigsFolder + "/SBF_Timeline.asset", minionPrefab, bossPrefab);
            var directorCfg = CreateDirectorConfig(ConfigsFolder + "/SBF_DirectorConfig.asset");

            var spawnerA = CreateSpawnerPrefab(PrefabsFolder + "/SBF_Spawner_A.prefab", new Vector3(-6f, 0f, 0f));
            var spawnerB = CreateSpawnerPrefab(PrefabsFolder + "/SBF_Spawner_B.prefab", new Vector3(6f, 0f, 0f));

            var sceneAsset = CreateScene(timeline, directorCfg, arenaPrefab, playerPrefab, spawnerA, spawnerB);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            return new Result(sceneAsset,
                "- Scene: " + ScenePath + "\n" +
                "- Timeline: " + AssetDatabase.GetAssetPath(timeline) + "\n" +
                "- DirectorConfig: " + AssetDatabase.GetAssetPath(directorCfg));
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "TappyTale");
            EnsureFolder("Assets/TappyTale", "WaveDirector");
            EnsureFolder("Assets/TappyTale/WaveDirector", "Samples");
            EnsureFolder("Assets/TappyTale/WaveDirector/Samples", "Simple Boss Fight");
            EnsureFolder(RootFolder, "Prefabs");
            EnsureFolder(RootFolder, "Scenes");
            EnsureFolder(RootFolder, "Configs");
            EnsureFolder(RootFolder, "Scripts");
            EnsureFolder(RootFolder + "/Scripts", "Runtime");
            EnsureFolder(RootFolder + "/Scripts", "Editor");
        }

        private static void EnsureFolder(string parent, string name)
        {
            string full = parent + "/" + name;
            if (AssetDatabase.IsValidFolder(full)) return;
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void TryDelete(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(path) != null)
                AssetDatabase.DeleteAsset(path);
        }

        private static ScriptableObject CreateTimeline(string path, GameObject minionPrefab, GameObject bossPrefab)
        {
            var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (existing != null) return existing;

            var timelineType = WaveDirectorTypeUtil.FindTypeByName("WaveTimelineConfig");
            if (timelineType == null) throw new InvalidOperationException("WaveTimelineConfig type not found.");

            var t = ScriptableObject.CreateInstance(timelineType);
            AssetDatabase.CreateAsset(t, path);

            TrySet(t, "WaveCount", 2);
            TrySet(t, "DefaultWaveDurationSeconds", 20f);
            TryInvoke(t, "EnsureWaveDefinitions");

            // Waves
            var waves = GetList(t, "Waves");
            if (waves != null && waves.Count >= 2)
            {
                var w0 = waves[0];
                var w1 = waves[1];

                TrySet(w0, "Name", "Survive (Minions)");
                TrySetEnum(w0, "TriggerType", "Timer", 0);
                TrySet(w0, "TriggerTimeSeconds", 18f);

                TrySet(w1, "Name", "Boss");
                TrySetEnum(w1, "TriggerType", "Objective", 2);
                TrySet(w1, "TriggerObjectiveId", "BossDead");
                TrySet(w1, "TriggerTimeSeconds", 60f);
            }

            // Events
            var eventsList = GetList(t, "Events");
            if (eventsList != null)
            {
                eventsList.Clear();

                float[] times = { 0f, 1.5f, 3f, 4.5f, 6f, 7.5f, 9f, 10.5f, 12f, 13.5f, 15f };
                for (int i = 0; i < times.Length; i++)
                    AddTimelineEvent(eventsList, 0, minionPrefab, false, times[i]);

                AddTimelineEvent(eventsList, 1, bossPrefab, true, 0.5f);
            }

            EditorUtility.SetDirty(t);
            return t;
        }

        private static ScriptableObject CreateDirectorConfig(string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
            if (existing != null) return existing;

            var cfgType = WaveDirectorTypeUtil.FindTypeByName("WaveDirectorConfig");
            if (cfgType == null) throw new InvalidOperationException("WaveDirectorConfig type not found.");

            var cfg = ScriptableObject.CreateInstance(cfgType);
            AssetDatabase.CreateAsset(cfg, path);

            TrySetEnum(cfg, "Mode", "Balanced", 1);
            TrySet(cfg, "BaseDifficulty", 1f);
            TrySet(cfg, "MinDifficulty", 0.8f);
            TrySet(cfg, "MaxDifficulty", 1.6f);

            EditorUtility.SetDirty(cfg);
            return cfg;
        }

        private static GameObject CreateArenaPrefab(string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var root = new GameObject("SBF_Arena");

            var floor = GameObject.CreatePrimitive(PrimitiveType.Plane);
            floor.name = "Floor";
            floor.transform.SetParent(root.transform, false);
            floor.transform.localScale = new Vector3(8f, 1f, 8f);

            CreateWall(root.transform, new Vector3(0f, 1f, 20f), new Vector3(40f, 2f, 1f));
            CreateWall(root.transform, new Vector3(0f, 1f, -20f), new Vector3(40f, 2f, 1f));
            CreateWall(root.transform, new Vector3(20f, 1f, 0f), new Vector3(1f, 2f, 40f));
            CreateWall(root.transform, new Vector3(-20f, 1f, 0f), new Vector3(1f, 2f, 40f));

            TryAddNavMeshSurfaceAndBake(root);

            var prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
            UnityEngine.Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateWall(Transform parent, Vector3 pos, Vector3 scale)
        {
            var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
            w.transform.SetParent(parent, false);
            w.transform.localPosition = pos;
            w.transform.localScale = scale;
            w.name = "Wall";
        }

        private static void TryAddNavMeshSurfaceAndBake(GameObject root)
        {
            var t = WaveDirectorTypeUtil.FindTypeByFullName("Unity.AI.Navigation.NavMeshSurface");
            if (t == null) return;

            var comp = root.AddComponent(t);
            var build = t.GetMethod("BuildNavMesh", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (build != null) { try { build.Invoke(comp, null); } catch { } }
        }

        private static GameObject CreatePlayerPrefab(string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("SBF_Player");
            go.tag = "Player";

            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            go.AddComponent<TappyTale.Samples.SimpleBossFight.SBF_Health>();
            go.AddComponent<TappyTale.Samples.SimpleBossFight.SBF_PlayerController>();
            go.AddComponent<TappyTale.Samples.SimpleBossFight.SBF_Gun>();

            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.position = new Vector3(0f, 2.0f, -4.0f);
            camGo.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
            camGo.AddComponent<Camera>();

            var follow = camGo.AddComponent<TappyTale.Samples.SimpleBossFight.SBF_CameraFollow>();
            follow.Target = go.transform;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(camGo);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateMinionPrefab(string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            go.name = "SBF_Minion";
            go.transform.localScale = Vector3.one * 1.1f;

            var agent = go.AddComponent<NavMeshAgent>();
            agent.speed = 3.5f;
            agent.angularSpeed = 720f;
            agent.acceleration = 18f;
            agent.stoppingDistance = 1.2f;
            agent.enabled = false;

            go.AddComponent<TappyTale.Samples.SimpleBossFight.SBF_Health>();
            go.AddComponent<TappyTale.Samples.SimpleBossFight.SBF_EnemyNavAI>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateBossPrefab(string path)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "SBF_Boss";
            go.transform.localScale = Vector3.one * 2.4f;

            var agent = go.AddComponent<NavMeshAgent>();
            agent.speed = 2.7f;
            agent.angularSpeed = 540f;
            agent.acceleration = 14f;
            agent.stoppingDistance = 1.8f;
            agent.enabled = false;

            go.AddComponent<TappyTale.Samples.SimpleBossFight.SBF_Health>();
            go.AddComponent<TappyTale.Samples.SimpleBossFight.SBF_EnemyNavAI>();
            go.AddComponent<TappyTale.Samples.SimpleBossFight.SBF_BossController>();

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateSpawnerPrefab(string path, Vector3 position)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
            go.transform.position = position;

            var spawnerType = WaveDirectorTypeUtil.FindTypeByName("WaveSpawner");
            if (spawnerType != null) go.AddComponent(spawnerType);

            for (int i = 0; i < 5; i++)
            {
                var p = new GameObject("SpawnPoint_" + i);
                p.transform.SetParent(go.transform, false);

                float angle = i / 5f * Mathf.PI * 2f;
                p.transform.localPosition = new Vector3(Mathf.Cos(angle) * 8f, 0f, Mathf.Sin(angle) * 8f);
            }

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static SceneAsset CreateScene(ScriptableObject timeline, ScriptableObject directorCfg, GameObject arenaPrefab, GameObject playerPrefab, GameObject spawnerAPrefab, GameObject spawnerBPrefab)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            PrefabUtility.InstantiatePrefab(arenaPrefab);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(0f, 0f, -6f);
            EnsureSceneCamera(player.transform);
            EnsureSceneEventSystem();

            var waveSystem = new GameObject("WaveSystem");

            var directorType = WaveDirectorTypeUtil.FindTypeByName("WaveDirector");
            var trackerType = WaveDirectorTypeUtil.FindTypeByName("WaveObjectiveTracker");
            var poolType = WaveDirectorTypeUtil.FindTypeByName("PoolRegistry");

            var director = directorType != null ? waveSystem.AddComponent(directorType) : null;
            var tracker = trackerType != null ? waveSystem.AddComponent(trackerType) : null;

            if (poolType != null)
            {
                var pool = new GameObject("PoolRegistry");
                pool.AddComponent(poolType);
            }

            if (director != null)
            {
                var so = new SerializedObject(director);
                Assign(so, "timelineConfig", timeline);
                Assign(so, "directorConfig", directorCfg);
                if (tracker != null) Assign(so, "objectiveTracker", tracker);
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            PrefabUtility.InstantiatePrefab(spawnerAPrefab);
            PrefabUtility.InstantiatePrefab(spawnerBPrefab);

            // Small runtime wiring helper
            waveSystem.AddComponent<SBF_SceneBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.ImportAsset(ScenePath);
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        }

        private static void EnsureSceneCamera(Transform target)
        {
            Camera cam = Camera.main;
            if (cam == null)
            {
                var camGo = new GameObject("Main Camera");
                camGo.tag = "MainCamera";
                camGo.transform.position = new Vector3(0f, 2.0f, -4.0f);
                camGo.transform.rotation = Quaternion.Euler(10f, 0f, 0f);
                cam = camGo.AddComponent<Camera>();
            }

            if (cam != null && cam.GetComponent<AudioListener>() == null)
            {
                cam.gameObject.AddComponent<AudioListener>();
            }

            if (cam != null)
            {
                var follow = cam.GetComponent<TappyTale.Samples.SimpleBossFight.SBF_CameraFollow>();
                if (follow == null) follow = cam.gameObject.AddComponent<TappyTale.Samples.SimpleBossFight.SBF_CameraFollow>();
                follow.Target = target;
            }
        }

        private static void EnsureSceneEventSystem()
        {
            var eventSystemType = WaveDirectorTypeUtil.FindTypeByFullName("UnityEngine.EventSystems.EventSystem");
            if (eventSystemType == null) return;

            var existing = Resources.FindObjectsOfTypeAll(eventSystemType);
            for (int i = 0; i < existing.Length; i++)
            {
                var c = existing[i] as Component;
                if (c == null) continue;
                if (!c.gameObject.scene.IsValid()) continue;
                return;
            }

            var go = new GameObject("EventSystem");
            go.AddComponent(eventSystemType);

            var standaloneInputModuleType = WaveDirectorTypeUtil.FindTypeByFullName("UnityEngine.EventSystems.StandaloneInputModule");
            if (standaloneInputModuleType != null)
            {
                go.AddComponent(standaloneInputModuleType);
                return;
            }

            var inputSystemModuleType = WaveDirectorTypeUtil.FindTypeByFullName("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
            if (inputSystemModuleType != null)
            {
                go.AddComponent(inputSystemModuleType);
            }
        }

        private static void Assign(SerializedObject so, string prop, UnityEngine.Object value)
        {
            var p = so.FindProperty(prop);
            if (p == null) return;
            if (p.propertyType != SerializedPropertyType.ObjectReference) return;
            p.objectReferenceValue = value;
        }

        private static IList GetList(object obj, string member)
        {
            if (obj == null || string.IsNullOrEmpty(member)) return null;

            var t = obj.GetType();

            var f = t.GetField(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && typeof(IList).IsAssignableFrom(f.FieldType))
            {
                try { return f.GetValue(obj) as IList; } catch { }
            }

            var p = t.GetProperty(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanRead && typeof(IList).IsAssignableFrom(p.PropertyType))
            {
                try { return p.GetValue(obj, null) as IList; } catch { }
            }

            return null;
        }

        private static void AddTimelineEvent(IList eventsList, int waveIndex, GameObject prefab, bool isBoss, float spawnTime)
        {
            var elemType = eventsList.GetType().IsGenericType ? eventsList.GetType().GetGenericArguments()[0] : null;
            if (elemType == null) return;

            object e = Activator.CreateInstance(elemType);
            TrySet(e, "WaveIndex", waveIndex);
            TrySet(e, "Prefab", prefab);
            TrySet(e, "IsBoss", isBoss);
            TrySet(e, "SpawnTime", spawnTime);
            eventsList.Add(e);
        }

        private static void TryInvoke(object obj, string method)
        {
            var m = obj.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m == null || m.GetParameters().Length != 0) return;
            try { m.Invoke(obj, null); } catch { }
        }

        private static void TrySetEnum(object obj, string member, string enumName, int fallbackInt)
        {
            var t = obj.GetType();
            var f = t.GetField(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null && f.FieldType.IsEnum)
            {
                try { f.SetValue(obj, Enum.Parse(f.FieldType, enumName, true)); }
                catch { f.SetValue(obj, Enum.ToObject(f.FieldType, fallbackInt)); }
                return;
            }

            var p = t.GetProperty(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanWrite && p.PropertyType.IsEnum)
            {
                try { p.SetValue(obj, Enum.Parse(p.PropertyType, enumName, true)); }
                catch { p.SetValue(obj, Enum.ToObject(p.PropertyType, fallbackInt)); }
            }
        }

        private static void TrySet(object obj, string member, object value)
        {
            var t = obj.GetType();

            var f = t.GetField(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
            {
                try { f.SetValue(obj, value); } catch { }
                return;
            }

            var p = t.GetProperty(member, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (p != null && p.CanWrite)
            {
                try { p.SetValue(obj, value); } catch { }
            }
        }
    }

    // Scene runtime wiring helper: set enemy targets + hook boss objective tracker if present.
    [DisallowMultipleComponent]
    internal sealed class SBF_SceneBootstrap : MonoBehaviour
    {
        private void Start()
        {
            var player = GameObject.FindWithTag("Player")?.transform;

            MonoBehaviour objectiveTracker = null;
            var all = FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] == null) continue;
                if (all[i].GetType().Name == "WaveObjectiveTracker") { objectiveTracker = all[i]; break; }
            }

            var enemies = FindObjectsByType<TappyTale.Samples.SimpleBossFight.SBF_EnemyNavAI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++) enemies[i].SetTarget(player);

            var bosses = FindObjectsByType<TappyTale.Samples.SimpleBossFight.SBF_BossController>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < bosses.Length; i++)
            {
                bosses[i].SetTarget(player);
                if (objectiveTracker != null) bosses[i].SetObjectiveTracker(objectiveTracker);
            }
        }
    }
}
#endif
