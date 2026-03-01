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

        private readonly Dictionary<SBF_Health, Slider> entityBars = new Dictionary<SBF_Health, Slider>(64);
        private readonly Dictionary<SBF_Health, Text> entityLabels = new Dictionary<SBF_Health, Text>(64);

        private RectTransform entitiesRoot;
        private Slider playerBar;
        private Text playerLabel;
        private float nextRefreshAt;

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
            }

            var toRemove = new List<SBF_Health>();
            foreach (var kv in entityBars)
            {
                if (!liveSet.Contains(kv.Key))
                {
                    if (kv.Value != null) Destroy(kv.Value.transform.parent.gameObject);
                    toRemove.Add(kv.Key);
                }
            }

            for (int i = 0; i < toRemove.Count; i++)
            {
                entityBars.Remove(toRemove[i]);
                entityLabels.Remove(toRemove[i]);
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
            t.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            t.fontSize = size;
            t.fontStyle = style;
            t.alignment = TextAnchor.MiddleLeft;
            t.color = new Color(1f, 1f, 1f, 0.95f);
            t.text = text;
            return t;
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
