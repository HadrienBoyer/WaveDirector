#if UNITY_EDITOR
using System;
using System.Collections;
using System.Collections.Generic;
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
        private const string MaterialsFolder = RootFolder + "/Materials";
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
            EnsureInputSystemDefine();

            if (overwrite)
            {
                TryDelete(ScenePath);
                TryDelete(ConfigsFolder + "/SBF_Timeline.asset");
                TryDelete(ConfigsFolder + "/SBF_DirectorConfig.asset");
                TryDelete(MaterialsFolder + "/SBF_Player.mat");
                TryDelete(MaterialsFolder + "/SBF_Minion.mat");
                TryDelete(MaterialsFolder + "/SBF_Boss.mat");
                TryDelete(MaterialsFolder + "/SBF_Projectile.mat");
                TryDelete(PrefabsFolder + "/SBF_Player.prefab");
                TryDelete(PrefabsFolder + "/SBF_Minion.prefab");
                TryDelete(PrefabsFolder + "/SBF_Boss.prefab");
                TryDelete(PrefabsFolder + "/SBF_Projectile.prefab");
                TryDelete(PrefabsFolder + "/SBF_Spawner_A.prefab");
                TryDelete(PrefabsFolder + "/SBF_Spawner_B.prefab");
                TryDelete(PrefabsFolder + "/SBF_Arena.prefab");
            }

            var playerMaterial = CreateEntityMaterial(
                MaterialsFolder + "/SBF_Player.mat",
                new[]
                {
                    "Assets/Samples/Shader Graph/17.3.0/Production Ready Shaders/Lit/URP Lit Emissive Detail.mat",
                    "Assets/Samples/Shader Graph/17.3.0/Production Ready Shaders/Lit/URP Lit.mat"
                },
                new Color(0.15f, 0.75f, 1f, 1f),
                true);
            var minionMaterial = CreateEntityMaterial(
                MaterialsFolder + "/SBF_Minion.mat",
                new[]
                {
                    "Assets/Samples/Shader Graph/17.3.0/Production Ready Shaders/Lit/URP Lit.mat",
                    "Assets/Samples/Shader Graph/17.3.0/Production Ready Shaders/Environment/Rock/Materials/Rock_Small.mat"
                },
                new Color(0.26f, 0.88f, 0.40f, 1f),
                false);
            var bossMaterial = CreateEntityMaterial(
                MaterialsFolder + "/SBF_Boss.mat",
                new[]
                {
                    "Assets/Samples/Shader Graph/17.3.0/Production Ready Shaders/Lit/URP Lit Emissive Detail.mat",
                    "Assets/Samples/Shader Graph/17.3.0/Production Ready Shaders/Lit/URP Lit.mat"
                },
                new Color(0.96f, 0.26f, 0.26f, 1f),
                true);
            var projectileMaterial = CreateEntityMaterial(
                MaterialsFolder + "/SBF_Projectile.mat",
                new[]
                {
                    "Assets/Samples/Shader Graph/17.3.0/Production Ready Shaders/Environment/Particles/WaterfallSplash.mat",
                    "Assets/Samples/Shader Graph/17.3.0/Production Ready Shaders/Lit/URP Lit Emissive Detail.mat"
                },
                new Color(1f, 0.9f, 0.25f, 1f),
                true);

            var projectilePrefab = CreateProjectilePrefab(PrefabsFolder + "/SBF_Projectile.prefab", projectileMaterial);
            var minionPrefab = CreateMinionPrefab(PrefabsFolder + "/SBF_Minion.prefab", minionMaterial);
            var bossPrefab = CreateBossPrefab(PrefabsFolder + "/SBF_Boss.prefab", bossMaterial);
            var arenaPrefab = CreateArenaPrefab(PrefabsFolder + "/SBF_Arena.prefab");
            var playerPrefab = CreatePlayerPrefab(PrefabsFolder + "/SBF_Player.prefab", playerMaterial, projectilePrefab);

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
            EnsureFolder(RootFolder, "Materials");
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
            if (root == null) return;

            var t = WaveDirectorTypeUtil.FindTypeByFullName("Unity.AI.Navigation.NavMeshSurface");
            if (t == null) return;

            var comp = root.GetComponent(t);
            if (comp == null) comp = root.AddComponent(t);
            var build = t.GetMethod("BuildNavMesh", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (build != null) { try { build.Invoke(comp, null); } catch { } }
        }

        private static GameObject CreatePlayerPrefab(string path, Material playerMaterial, GameObject projectilePrefab)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject("SBF_Player");
            go.tag = "Player";

            var cc = go.AddComponent<CharacterController>();
            cc.height = 1.8f;
            cc.radius = 0.35f;
            cc.center = new Vector3(0f, 0.9f, 0f);

            var playerHealth = go.AddComponent<TappyTale.Samples.SimpleBossFight.SBF_Health>();
            TrySet(playerHealth, "team", TappyTale.Samples.SimpleBossFight.SBF_Team.Player);
            go.AddComponent<TappyTale.Samples.SimpleBossFight.SBF_PlayerController>();
            var gun = go.AddComponent<TappyTale.Samples.SimpleBossFight.SBF_Gun>();
            TrySet(gun, "projectilePrefab", projectilePrefab);
            TrySet(gun, "projectileDamage", 22f);
            TrySet(gun, "projectileSpeed", 52f);
            TrySet(gun, "projectileLifetime", 2.2f);

            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(go.transform, false);
            body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            body.transform.localScale = new Vector3(0.78f, 0.95f, 0.78f);
            var bodyCollider = body.GetComponent<Collider>();
            if (bodyCollider != null) UnityEngine.Object.DestroyImmediate(bodyCollider);
            ApplyMaterial(body, playerMaterial);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateMinionPrefab(string path, Material minionMaterial)
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

            var health = go.AddComponent<TappyTale.Samples.SimpleBossFight.SBF_Health>();
            TrySet(health, "team", TappyTale.Samples.SimpleBossFight.SBF_Team.Enemy);
            var minionAi = go.AddComponent<TappyTale.Samples.SimpleBossFight.SBF_EnemyNavAI>();
            var minionAiSo = new SerializedObject(minionAi);
            TrySet(minionAi, "keepFixedY", true);
            TrySet(minionAi, "fixedY", 1f);
            minionAiSo.ApplyModifiedPropertiesWithoutUndo();
            ApplyMaterial(go, minionMaterial);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateBossPrefab(string path, Material bossMaterial)
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

            var health = go.AddComponent<TappyTale.Samples.SimpleBossFight.SBF_Health>();
            TrySet(health, "team", TappyTale.Samples.SimpleBossFight.SBF_Team.Enemy);
            var bossAi = go.AddComponent<TappyTale.Samples.SimpleBossFight.SBF_EnemyNavAI>();
            var bossAiSo = new SerializedObject(bossAi);
            TrySet(bossAi, "keepFixedY", false);
            bossAiSo.ApplyModifiedPropertiesWithoutUndo();
            go.AddComponent<TappyTale.Samples.SimpleBossFight.SBF_BossController>();
            ApplyMaterial(go, bossMaterial);

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            UnityEngine.Object.DestroyImmediate(go);
            return prefab;
        }

        private static GameObject CreateProjectilePrefab(string path, Material projectileMaterial)
        {
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "SBF_Projectile";
            go.transform.localScale = Vector3.one * 0.24f;

            var collider = go.GetComponent<SphereCollider>();
            if (collider != null) collider.isTrigger = true;

            var rb = go.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.constraints = RigidbodyConstraints.FreezeRotation;

            var projectile = go.AddComponent<TappyTale.Samples.SimpleBossFight.SBF_Projectile>();
            TrySet(projectile, "speed", 52f);
            TrySet(projectile, "damage", 22f);
            TrySet(projectile, "lifeTime", 2.2f);
            TrySet(projectile, "sourceTeam", TappyTale.Samples.SimpleBossFight.SBF_Team.Player);

            ApplyMaterial(go, projectileMaterial);

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

            var arena = PrefabUtility.InstantiatePrefab(arenaPrefab) as GameObject;
            if (arena != null) TryAddNavMeshSurfaceAndBake(arena);

            var player = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            player.transform.position = new Vector3(0f, 0f, -6f);
            var cam = EnsureSceneCamera(player.transform);
            AssignPlayerGunCamera(player, cam);
            EnsureSceneEventSystem();
            EnsureSceneHud(player);

            var waveSystem = new GameObject("WaveSystem");

            var directorType = WaveDirectorTypeUtil.FindTypeByName("WaveDirector");
            var trackerType = WaveDirectorTypeUtil.FindTypeByName("WaveObjectiveTracker");
            var poolType = WaveDirectorTypeUtil.FindTypeByName("PoolRegistry");
            var spawnerType = WaveDirectorTypeUtil.FindTypeByName("WaveSpawner");

            var director = directorType != null ? waveSystem.AddComponent(directorType) : null;
            var tracker = trackerType != null ? waveSystem.AddComponent(trackerType) : null;
            Component poolRegistry = null;

            if (poolType != null)
            {
                var pool = new GameObject("PoolRegistry");
                poolRegistry = pool.AddComponent(poolType);

                var poolsRoot = new GameObject("Pools");
                poolsRoot.transform.SetParent(pool.transform, false);

                var poolSo = new SerializedObject(poolRegistry);
                Assign(poolSo, "poolRoot", poolsRoot.transform);
                poolSo.ApplyModifiedPropertiesWithoutUndo();
            }

            var spawnedSpawners = new List<UnityEngine.Object>(2);
            var spawnerA = (GameObject)PrefabUtility.InstantiatePrefab(spawnerAPrefab);
            var spawnerB = (GameObject)PrefabUtility.InstantiatePrefab(spawnerBPrefab);
            if (spawnerType != null)
            {
                var a = spawnerA != null ? spawnerA.GetComponent(spawnerType) : null;
                var b = spawnerB != null ? spawnerB.GetComponent(spawnerType) : null;
                if (a != null) spawnedSpawners.Add(a);
                if (b != null) spawnedSpawners.Add(b);
            }

            if (director != null)
            {
                var so = new SerializedObject(director);
                Assign(so, "timelineConfig", timeline);
                Assign(so, "directorConfig", directorCfg);
                if (tracker != null) Assign(so, "objectiveTracker", tracker);
                AssignObjectList(so, "spawners", spawnedSpawners);
                so.ApplyModifiedPropertiesWithoutUndo();

                // Fallback for Unity versions/import states where SerializedObject binding can fail silently.
                TrySet(director, "timelineConfig", timeline);
                TrySet(director, "directorConfig", directorCfg);
                if (tracker != null) TrySet(director, "objectiveTracker", tracker);
                TrySetTypedObjectList(director, "spawners", spawnerType, spawnedSpawners);
                EditorUtility.SetDirty(director);
            }

            // Small runtime wiring helper
            waveSystem.AddComponent<TappyTale.Samples.SimpleBossFight.SBF_SceneBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.ImportAsset(ScenePath);
            return AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        }

        private static Camera EnsureSceneCamera(Transform target)
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

            if (cam != null) cam.allowHDR = true;

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

            EnablePostProcessingOnCamera(cam);
            return cam;
        }

        private static void EnsureSceneEventSystem()
        {
            var eventSystemType = WaveDirectorTypeUtil.FindTypeByFullName("UnityEngine.EventSystems.EventSystem");
            if (eventSystemType == null) return;

            GameObject go = FindSceneObjectWithComponent(eventSystemType);
            if (go == null) go = new GameObject("EventSystem");
            if (go.GetComponent(eventSystemType) == null) go.AddComponent(eventSystemType);

            var inputSystemModuleType = WaveDirectorTypeUtil.FindTypeByFullName("UnityEngine.InputSystem.UI.InputSystemUIInputModule");
            if (inputSystemModuleType != null)
            {
                if (go.GetComponent(inputSystemModuleType) == null) go.AddComponent(inputSystemModuleType);

                var standaloneInputModuleType = WaveDirectorTypeUtil.FindTypeByFullName("UnityEngine.EventSystems.StandaloneInputModule");
                if (standaloneInputModuleType != null)
                {
                    var old = go.GetComponents(standaloneInputModuleType);
                    for (int i = 0; i < old.Length; i++) UnityEngine.Object.DestroyImmediate(old[i]);
                }
                return;
            }

            var fallbackStandaloneType = WaveDirectorTypeUtil.FindTypeByFullName("UnityEngine.EventSystems.StandaloneInputModule");
            if (fallbackStandaloneType != null && go.GetComponent(fallbackStandaloneType) == null)
                go.AddComponent(fallbackStandaloneType);
        }

        private static void EnsureSceneHud(GameObject player)
        {
            if (player == null) return;

            var hudGo = new GameObject("SBF_UI");
            var canvas = hudGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            hudGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            hudGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var hud = hudGo.AddComponent<TappyTale.Samples.SimpleBossFight.SBF_GameHUD>();
            var hudSo = new SerializedObject(hud);
            Assign(hudSo, "player", player.transform);

            var playerHealth = player.GetComponent<TappyTale.Samples.SimpleBossFight.SBF_Health>();
            if (playerHealth != null) Assign(hudSo, "playerHealth", playerHealth);
            hudSo.ApplyModifiedPropertiesWithoutUndo();

            hud.SetPlayer(player.transform);
        }

        private static void Assign(SerializedObject so, string prop, UnityEngine.Object value)
        {
            var p = so.FindProperty(prop);
            if (p == null) return;
            if (p.propertyType != SerializedPropertyType.ObjectReference) return;
            p.objectReferenceValue = value;
        }

        private static void AssignObjectList(SerializedObject so, string prop, IList<UnityEngine.Object> values)
        {
            var p = so.FindProperty(prop);
            if (p == null || !p.isArray) return;

            int count = values != null ? values.Count : 0;
            p.arraySize = count;
            for (int i = 0; i < count; i++)
            {
                var e = p.GetArrayElementAtIndex(i);
                if (e != null && e.propertyType == SerializedPropertyType.ObjectReference)
                    e.objectReferenceValue = values[i];
            }
        }

        private static void TrySetTypedObjectList(object owner, string member, Type elementType, IList<UnityEngine.Object> values)
        {
            if (owner == null) return;
            if (string.IsNullOrEmpty(member)) return;
            if (elementType == null) return;

            var listType = typeof(List<>).MakeGenericType(elementType);
            var typedList = Activator.CreateInstance(listType) as IList;
            if (typedList == null) return;

            if (values != null)
            {
                for (int i = 0; i < values.Count; i++)
                {
                    var v = values[i];
                    if (v == null) continue;
                    if (!elementType.IsInstanceOfType(v)) continue;
                    typedList.Add(v);
                }
            }

            TrySet(owner, member, typedList);
        }

        private static void AssignPlayerGunCamera(GameObject player, Camera cam)
        {
            if (player == null || cam == null) return;

            var gun = player.GetComponent<TappyTale.Samples.SimpleBossFight.SBF_Gun>();
            if (gun == null) return;

            var so = new SerializedObject(gun);
            Assign(so, "cam", cam);
            so.ApplyModifiedPropertiesWithoutUndo();
            gun.SetCamera(cam);
        }

        private static GameObject FindSceneObjectWithComponent(Type componentType)
        {
            if (componentType == null) return null;

            var all = Resources.FindObjectsOfTypeAll(componentType);
            for (int i = 0; i < all.Length; i++)
            {
                var c = all[i] as Component;
                if (c == null) continue;
                if (!c.gameObject.scene.IsValid()) continue;
                return c.gameObject;
            }
            return null;
        }

        private static void EnablePostProcessingOnCamera(Camera cam)
        {
            if (cam == null) return;

            var urpCameraDataType = WaveDirectorTypeUtil.FindTypeByFullName("UnityEngine.Rendering.Universal.UniversalAdditionalCameraData");
            if (urpCameraDataType == null) return;

            var data = cam.GetComponent(urpCameraDataType);
            if (data == null) data = cam.gameObject.AddComponent(urpCameraDataType);
            TrySet(data, "renderPostProcessing", true);
        }

        private static Material CreateEntityMaterial(string path, string[] sourceCandidates, Color tint, bool emissive)
        {
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null) return existing;

            string sourcePath = FindFirstExistingAssetPath(sourceCandidates);
            if (!string.IsNullOrEmpty(sourcePath))
            {
                if (AssetDatabase.CopyAsset(sourcePath, path))
                {
                    var copied = AssetDatabase.LoadAssetAtPath<Material>(path);
                    if (copied != null)
                    {
                        TintMaterial(copied, tint, emissive);
                        EditorUtility.SetDirty(copied);
                        return copied;
                    }
                }
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            if (shader == null) return null;

            var mat = new Material(shader);
            TintMaterial(mat, tint, emissive);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static string FindFirstExistingAssetPath(string[] paths)
        {
            if (paths == null) return null;
            for (int i = 0; i < paths.Length; i++)
            {
                var p = paths[i];
                if (string.IsNullOrEmpty(p)) continue;
                if (AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(p) != null) return p;
            }
            return null;
        }

        private static void TintMaterial(Material mat, Color tint, bool emissive)
        {
            if (mat == null) return;

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", tint);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", tint);
            if (mat.HasProperty("_Tint")) mat.SetColor("_Tint", tint);

            if (emissive)
            {
                var e = tint * 0.5f;
                if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", e);
                mat.EnableKeyword("_EMISSION");
            }
        }

        private static void ApplyMaterial(GameObject go, Material mat)
        {
            if (go == null || mat == null) return;
            var renderers = go.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] == null) continue;
                renderers[i].sharedMaterial = mat;
            }
        }

        private static void EnsureInputSystemDefine()
        {
            const string symbol = "ENABLE_INPUT_SYSTEM";
            var inputSystemType = WaveDirectorTypeUtil.FindTypeByFullName("UnityEngine.InputSystem.PlayerInput");
            if (inputSystemType == null) return;

            var group = EditorUserBuildSettings.selectedBuildTargetGroup;
            if (group == BuildTargetGroup.Unknown) return;

            if (TryEnsureDefineWithNamedBuildTarget(group, symbol)) return;
            TryEnsureDefineWithLegacyBuildTargetGroup(group, symbol);
        }

        private static bool TryEnsureDefineWithNamedBuildTarget(BuildTargetGroup group, string symbol)
        {
            var namedBuildTargetType = Type.GetType("UnityEditor.Build.NamedBuildTarget, UnityEditor.CoreModule");
            if (namedBuildTargetType == null) return false;

            var fromGroup = namedBuildTargetType.GetMethod("FromBuildTargetGroup", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(BuildTargetGroup) }, null);
            if (fromGroup == null) return false;

            var get = typeof(PlayerSettings).GetMethod("GetScriptingDefineSymbols", BindingFlags.Public | BindingFlags.Static, null, new[] { namedBuildTargetType }, null);
            var set = typeof(PlayerSettings).GetMethod("SetScriptingDefineSymbols", BindingFlags.Public | BindingFlags.Static, null, new[] { namedBuildTargetType, typeof(string) }, null);
            if (get == null || set == null) return false;

            object namedBuildTarget = fromGroup.Invoke(null, new object[] { group });
            string current = get.Invoke(null, new[] { namedBuildTarget }) as string ?? string.Empty;
            string updated = AppendDefineSymbol(current, symbol);
            if (!string.Equals(updated, current, StringComparison.Ordinal))
            {
                set.Invoke(null, new object[] { namedBuildTarget, updated });
            }
            return true;
        }

        private static void TryEnsureDefineWithLegacyBuildTargetGroup(BuildTargetGroup group, string symbol)
        {
            var get = typeof(PlayerSettings).GetMethod("GetScriptingDefineSymbolsForGroup", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(BuildTargetGroup) }, null);
            var set = typeof(PlayerSettings).GetMethod("SetScriptingDefineSymbolsForGroup", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(BuildTargetGroup), typeof(string) }, null);
            if (get == null || set == null) return;

            string current = get.Invoke(null, new object[] { group }) as string ?? string.Empty;
            string updated = AppendDefineSymbol(current, symbol);
            if (!string.Equals(updated, current, StringComparison.Ordinal))
            {
                set.Invoke(null, new object[] { group, updated });
            }
        }

        private static string AppendDefineSymbol(string defines, string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol)) return defines ?? string.Empty;

            string current = defines ?? string.Empty;
            var parts = current.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                if (string.Equals(parts[i].Trim(), symbol, StringComparison.Ordinal))
                    return current;
            }

            if (string.IsNullOrWhiteSpace(current)) return symbol;
            return current.TrimEnd(';') + ";" + symbol;
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

}
#endif
