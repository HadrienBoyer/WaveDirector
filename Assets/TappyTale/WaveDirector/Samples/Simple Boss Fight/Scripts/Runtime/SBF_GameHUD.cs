using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace TappyTale.Samples.SimpleBossFight
{
    [DisallowMultipleComponent]
    public sealed class SBF_GameHUD : MonoBehaviour
    {
        [SerializeField] private Transform player;
        [SerializeField] private SBF_Health playerHealth;
        [SerializeField] private float targetIconHeightOffset = 1.2f;
        [SerializeField] private float enemyHeadBarExtraHeight = 0.25f;
        [SerializeField] private Vector2 enemyHeadBarSize = new Vector2(86f, 10f);

        private readonly Dictionary<SBF_Health, Slider> entityBars = new Dictionary<SBF_Health, Slider>(64);
        private readonly Dictionary<SBF_Health, Text> entityLabels = new Dictionary<SBF_Health, Text>(64);
        private readonly Dictionary<SBF_Health, Slider> enemyHeadBars = new Dictionary<SBF_Health, Slider>(64);
        private readonly List<SBF_Health> reusableRemoval = new List<SBF_Health>(64);
        private static Font cachedHudFont;

        private RectTransform entitiesRoot;
        private RectTransform enemyHeadBarsRoot;
        private RectTransform canvasRoot;
        private RectTransform targetIcon;
        private Slider playerBar;
        private Text playerLabel;
        private float nextRefreshAt;
        private SBF_Gun playerGun;

        private void Awake()
        {
            BuildUi();
        }

        private void Update()
        {
            if (playerHealth == null) TryResolvePlayerRefs();
            if (Time.unscaledTime >= nextRefreshAt)
            {
                nextRefreshAt = Time.unscaledTime + 0.25f;
                RefreshEntityRows();
            }

            UpdateBars();
            UpdateEnemyHeadBars();
            UpdateTargetIcon();
        }

        public void SetPlayer(Transform playerTransform)
        {
            player = playerTransform;
            TryResolvePlayerRefs();
        }

        private void BuildUi()
        {
            var canvas = GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                canvas = gameObject.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            }

            if (canvas.GetComponent<CanvasScaler>() == null)
            {
                var scaler = canvas.gameObject.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.matchWidthOrHeight = 0.5f;
            }

            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }

            var root = canvas.transform as RectTransform;
            if (root == null) return;
            canvasRoot = root;

            var overlay = CreateUiObject("FullScreenOverlay", root);
            Stretch(overlay);
            var overlayImage = overlay.gameObject.AddComponent<Image>();
            overlayImage.color = new Color(0f, 0f, 0f, 0.03f);
            overlayImage.raycastTarget = false;

            var crosshair = CreateUiObject("Crosshair", root);
            crosshair.anchorMin = crosshair.anchorMax = new Vector2(0.5f, 0.5f);
            crosshair.pivot = new Vector2(0.5f, 0.5f);
            crosshair.sizeDelta = new Vector2(8f, 8f);
            var crosshairImage = crosshair.gameObject.AddComponent<Image>();
            crosshairImage.color = new Color(1f, 1f, 1f, 0.85f);
            crosshairImage.raycastTarget = false;

            targetIcon = CreateTargetIcon(root);
            enemyHeadBarsRoot = CreateUiObject("EnemyHeadBars", root);
            Stretch(enemyHeadBarsRoot);

            entitiesRoot = CreateUiObject("EntityHealthPanel", root);
            entitiesRoot.anchorMin = new Vector2(0f, 1f);
            entitiesRoot.anchorMax = new Vector2(0f, 1f);
            entitiesRoot.pivot = new Vector2(0f, 1f);
            entitiesRoot.anchoredPosition = new Vector2(20f, -20f);
            entitiesRoot.sizeDelta = new Vector2(420f, 520f);
            var layout = entitiesRoot.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;
            layout.childForceExpandWidth = true;
            layout.spacing = 6f;

            var playerRoot = CreateUiObject("PlayerHealth", root);
            playerRoot.anchorMin = new Vector2(0.5f, 0f);
            playerRoot.anchorMax = new Vector2(0.5f, 0f);
            playerRoot.pivot = new Vector2(0.5f, 0f);
            playerRoot.anchoredPosition = new Vector2(0f, 28f);
            playerRoot.sizeDelta = new Vector2(520f, 78f);

            playerLabel = CreateText("PlayerLabel", playerRoot, "PLAYER HP", 20, FontStyle.Bold);
            playerLabel.alignment = TextAnchor.MiddleCenter;
            StretchHorizontal(playerLabel.rectTransform, 0f, 0f, 48f, 24f);

            playerBar = CreateSlider("PlayerBar", playerRoot, new Color(0.18f, 0.88f, 0.42f, 0.95f), new Color(0.08f, 0.08f, 0.08f, 0.72f));
            StretchHorizontal(playerBar.GetComponent<RectTransform>(), 24f, 24f, 8f, 16f);
        }

        private void RefreshEntityRows()
        {
            var liveSet = new HashSet<SBF_Health>();
            var all = SBF_Health.ActiveEntities;
            for (int i = 0; i < all.Count; i++)
            {
                var h = all[i];
                if (h == null) continue;
                if (!h.gameObject.activeInHierarchy) continue;
                liveSet.Add(h);

                if (!entityBars.ContainsKey(h))
                {
                    CreateEntityRow(h);
                }

                if (h.Team == SBF_Team.Enemy && !enemyHeadBars.ContainsKey(h))
                {
                    CreateEnemyHeadBar(h);
                }
            }

            reusableRemoval.Clear();
            foreach (var kv in entityBars)
            {
                if (!liveSet.Contains(kv.Key))
                {
                    if (kv.Value != null) Destroy(kv.Value.transform.parent.gameObject);
                    reusableRemoval.Add(kv.Key);
                }
            }

            for (int i = 0; i < reusableRemoval.Count; i++)
            {
                entityBars.Remove(reusableRemoval[i]);
                entityLabels.Remove(reusableRemoval[i]);
            }

            reusableRemoval.Clear();
            foreach (var kv in enemyHeadBars)
            {
                var h = kv.Key;
                bool invalid = h == null || !liveSet.Contains(h) || h.Team != SBF_Team.Enemy;
                if (!invalid) continue;
                if (kv.Value != null) Destroy(kv.Value.gameObject);
                reusableRemoval.Add(h);
            }

            for (int i = 0; i < reusableRemoval.Count; i++)
            {
                enemyHeadBars.Remove(reusableRemoval[i]);
            }
        }

        private void CreateEntityRow(SBF_Health h)
        {
            if (entitiesRoot == null || h == null) return;

            var row = CreateUiObject("Entity_" + h.name, entitiesRoot);
            var image = row.gameObject.AddComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.38f);

            var rowLayout = row.gameObject.AddComponent<VerticalLayoutGroup>();
            rowLayout.padding = new RectOffset(8, 8, 6, 6);
            rowLayout.childAlignment = TextAnchor.UpperLeft;
            rowLayout.childControlHeight = true;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandHeight = false;
            rowLayout.childForceExpandWidth = true;
            rowLayout.spacing = 4f;

            var label = CreateText("Label", row, BuildEntityLabel(h), 15, FontStyle.Bold);
            label.alignment = TextAnchor.MiddleLeft;
            label.color = TeamColor(h.Team);

            var bar = CreateSlider("Bar", row, TeamColor(h.Team), new Color(0.1f, 0.1f, 0.1f, 0.85f));
            var barRt = bar.GetComponent<RectTransform>();
            barRt.sizeDelta = new Vector2(0f, 14f);

            entityBars[h] = bar;
            entityLabels[h] = label;
        }

        private void CreateEnemyHeadBar(SBF_Health h)
        {
            if (enemyHeadBarsRoot == null || h == null) return;

            var bar = CreateSlider("HeadBar_" + h.name, enemyHeadBarsRoot, TeamColor(h.Team), new Color(0.08f, 0.08f, 0.08f, 0.82f));
            var rt = bar.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = enemyHeadBarSize;
            bar.gameObject.SetActive(false);
            enemyHeadBars[h] = bar;
        }

        private void UpdateBars()
        {
            if (playerHealth != null && playerBar != null)
            {
                playerBar.SetValueWithoutNotify(playerHealth.Normalized);
                playerLabel.text = $"PLAYER HP  {playerHealth.Hp:0}/{playerHealth.MaxHp:0}";
            }

            foreach (var kv in entityBars)
            {
                var health = kv.Key;
                var slider = kv.Value;
                if (health == null || slider == null) continue;

                slider.SetValueWithoutNotify(health.Normalized);
                if (entityLabels.TryGetValue(health, out var label) && label != null)
                {
                    label.text = BuildEntityLabel(health);
                }
            }
        }

        private void UpdateEnemyHeadBars()
        {
            if (canvasRoot == null) return;
            var cam = Camera.main;
            if (cam == null)
            {
                foreach (var kv in enemyHeadBars)
                {
                    if (kv.Value != null) kv.Value.gameObject.SetActive(false);
                }
                return;
            }

            foreach (var kv in enemyHeadBars)
            {
                var health = kv.Key;
                var bar = kv.Value;
                if (health == null || bar == null) continue;

                bar.SetValueWithoutNotify(health.Normalized);

                var worldPos = GetEnemyHeadWorldPosition(health);
                var screenPos = cam.WorldToScreenPoint(worldPos);
                bool visible = screenPos.z > 0f &&
                               screenPos.x >= 0f && screenPos.x <= Screen.width &&
                               screenPos.y >= 0f && screenPos.y <= Screen.height;

                if (!visible)
                {
                    bar.gameObject.SetActive(false);
                    continue;
                }

                Vector2 localPos;
                RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, screenPos, null, out localPos);
                var rt = bar.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = localPos;
                bar.gameObject.SetActive(true);
            }
        }

        private void TryResolvePlayerRefs()
        {
            if (player == null)
            {
                var playerGo = GameObject.FindWithTag("Player");
                if (playerGo != null) player = playerGo.transform;
            }

            if (playerHealth == null && player != null)
            {
                playerHealth = player.GetComponentInChildren<SBF_Health>(true);
            }

            if (playerGun == null && player != null)
            {
                playerGun = player.GetComponentInChildren<SBF_Gun>(true);
            }
        }

        private void UpdateTargetIcon()
        {
            if (targetIcon == null) return;

            if (playerGun == null) TryResolvePlayerRefs();
            var cam = Camera.main;
            var target = playerGun != null ? playerGun.CurrentTarget : null;

            if (cam == null || target == null || target.Hp <= 0f || !target.gameObject.activeInHierarchy)
            {
                targetIcon.gameObject.SetActive(false);
                return;
            }

            Vector3 worldPos = target.transform.position + Vector3.up * targetIconHeightOffset;
            Vector3 screenPos = cam.WorldToScreenPoint(worldPos);
            if (screenPos.z <= 0f)
            {
                targetIcon.gameObject.SetActive(false);
                return;
            }

            if (canvasRoot == null)
            {
                targetIcon.gameObject.SetActive(false);
                return;
            }

            Vector2 localPos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, screenPos, null, out localPos);

            targetIcon.gameObject.SetActive(true);
            targetIcon.anchoredPosition = localPos;
        }

        private Vector3 GetEnemyHeadWorldPosition(SBF_Health health)
        {
            var t = health.transform;
            var col = t.GetComponentInChildren<Collider>();
            if (col != null)
            {
                var b = col.bounds;
                return new Vector3(b.center.x, b.max.y + enemyHeadBarExtraHeight, b.center.z);
            }

            return t.position + Vector3.up * (1.2f + enemyHeadBarExtraHeight);
        }

        private static string BuildEntityLabel(SBF_Health h)
        {
            if (h == null) return "Unknown";
            return $"{h.name} ({h.Team})  {h.Hp:0}/{h.MaxHp:0}";
        }

        private static Color TeamColor(SBF_Team team)
        {
            switch (team)
            {
                case SBF_Team.Player: return new Color(0.22f, 0.82f, 0.36f, 0.95f);
                case SBF_Team.Enemy: return new Color(0.87f, 0.26f, 0.26f, 0.95f);
                default: return new Color(0.8f, 0.8f, 0.8f, 0.95f);
            }
        }

        private static RectTransform CreateUiObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rt = go.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            return rt;
        }

        private static Text CreateText(string name, Transform parent, string text, int size, FontStyle style)
        {
            var rt = CreateUiObject(name, parent);
            var t = rt.gameObject.AddComponent<Text>();
            var hudFont = GetHudFont();
            if (hudFont != null) t.font = hudFont;
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = new Color(1f, 1f, 1f, 0.95f);
            t.text = text;
            return t;
        }

        private static RectTransform CreateTargetIcon(Transform parent)
        {
            var icon = CreateUiObject("TargetLockIcon", parent);
            icon.anchorMin = new Vector2(0.5f, 0.5f);
            icon.anchorMax = new Vector2(0.5f, 0.5f);
            icon.pivot = new Vector2(0.5f, 0.5f);
            icon.sizeDelta = new Vector2(28f, 28f);

            var color = new Color(1f, 0.3f, 0.25f, 0.95f);
            CreateReticleSegment(icon, "Top", new Vector2(0f, 13f), new Vector2(2f, 8f), color);
            CreateReticleSegment(icon, "Bottom", new Vector2(0f, -13f), new Vector2(2f, 8f), color);
            CreateReticleSegment(icon, "Left", new Vector2(-13f, 0f), new Vector2(8f, 2f), color);
            CreateReticleSegment(icon, "Right", new Vector2(13f, 0f), new Vector2(8f, 2f), color);
            CreateReticleSegment(icon, "Center", Vector2.zero, new Vector2(4f, 4f), color);

            icon.gameObject.SetActive(false);
            return icon;
        }

        private static void CreateReticleSegment(RectTransform parent, string name, Vector2 anchoredPos, Vector2 size, Color color)
        {
            var rt = CreateUiObject(name, parent);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
        }

        private static Font GetHudFont()
        {
            if (cachedHudFont != null) return cachedHudFont;

            cachedHudFont = TryGetBuiltinFont("LegacyRuntime.ttf");
            if (cachedHudFont != null) return cachedHudFont;

            cachedHudFont = TryGetBuiltinFont("Arial.ttf");
            return cachedHudFont;
        }

        private static Font TryGetBuiltinFont(string fontPath)
        {
            try
            {
                return Resources.GetBuiltinResource<Font>(fontPath);
            }
            catch (System.ArgumentException)
            {
                return null;
            }
        }

        private static Slider CreateSlider(string name, Transform parent, Color fillColor, Color backgroundColor)
        {
            var sliderRt = CreateUiObject(name, parent);
            var slider = sliderRt.gameObject.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.direction = Slider.Direction.LeftToRight;
            slider.interactable = false;

            var bgRt = CreateUiObject("Background", sliderRt);
            Stretch(bgRt);
            var bgImage = bgRt.gameObject.AddComponent<Image>();
            bgImage.color = backgroundColor;

            var fillAreaRt = CreateUiObject("Fill Area", sliderRt);
            Stretch(fillAreaRt);
            fillAreaRt.offsetMin = new Vector2(2f, 2f);
            fillAreaRt.offsetMax = new Vector2(-2f, -2f);

            var fillRt = CreateUiObject("Fill", fillAreaRt);
            Stretch(fillRt);
            var fillImage = fillRt.gameObject.AddComponent<Image>();
            fillImage.color = fillColor;

            slider.targetGraphic = bgImage;
            slider.fillRect = fillRt;

            return slider;
        }

        private static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        private static void StretchHorizontal(RectTransform rt, float left, float right, float top, float bottom)
        {
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.offsetMin = new Vector2(left, bottom);
            rt.offsetMax = new Vector2(-right, -top);
        }
    }
}
