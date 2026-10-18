using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Depthbound
{
    public static class UIFactory
    {
        private static Font cachedFont;
        private static Sprite cachedVignette;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeCache()
        {
            cachedFont = null;
            cachedVignette = null;
        }

        public static Canvas CreateCanvas(string name)
        {
            GameObject go = new GameObject(name);
            Canvas canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.pixelPerfect = true;
            canvas.sortingOrder = 20;

            CanvasScaler scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        public static Font Font
        {
            get
            {
                if (cachedFont == null)
                {
                    cachedFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

                    if (cachedFont != null &&
                        cachedFont.material != null &&
                        cachedFont.material.mainTexture != null)
                    {
                        cachedFont.material.mainTexture.filterMode =
                            FilterMode.Point;

                        cachedFont.material.mainTexture.anisoLevel = 0;
                    }
                }

                return cachedFont;
            }
        }

        public static Sprite VignetteSprite
        {
            get
            {
                if (cachedVignette != null) return cachedVignette;

                const int size = 96;
                Texture2D texture = new Texture2D(
                    size,
                    size,
                    TextureFormat.RGBA32,
                    false);

                texture.name = "GeneratedVignette";
                texture.wrapMode = TextureWrapMode.Clamp;
                texture.filterMode = FilterMode.Point;

                Color[] pixels = new Color[size * size];

                for (int y = 0; y < size; y++)
                {
                    for (int x = 0; x < size; x++)
                    {
                        float nx = Mathf.Abs((x + 0.5f) / size * 2f - 1f);
                        float ny = Mathf.Abs((y + 0.5f) / size * 2f - 1f);
                        float edge = Mathf.Max(nx, ny);
                        float corner = Mathf.Sqrt(nx * nx + ny * ny) / 1.4142f;

                        float alpha =
                            Mathf.SmoothStep(0.52f, 1f, edge) * 0.16f +
                            Mathf.SmoothStep(0.68f, 1f, corner) * 0.12f;

                        alpha =
                            Mathf.Round(alpha * 5f) / 5f;

                        pixels[y * size + x] = new Color(
                            0.005f,
                            0.007f,
                            0.010f,
                            Mathf.Clamp01(alpha));
                    }
                }

                texture.SetPixels(pixels);
                texture.Apply(false, true);

                cachedVignette = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, size, size),
                    new Vector2(0.5f, 0.5f),
                    100f);

                cachedVignette.name = "GeneratedVignetteSprite";
                return cachedVignette;
            }
        }

        public static Image CreatePanel(
            Transform parent,
            string name,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Image image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        public static void AddPixelFrame(
            Transform parent,
            string name,
            Color color,
            float thickness = 2f)
        {
            Image top = CreatePanel(
                parent,
                name + "_Top",
                color,
                new Vector2(0f, 1f),
                new Vector2(1f, 1f),
                new Vector2(0f, -thickness),
                Vector2.zero);

            Image bottom = CreatePanel(
                parent,
                name + "_Bottom",
                color,
                new Vector2(0f, 0f),
                new Vector2(1f, 0f),
                Vector2.zero,
                new Vector2(0f, thickness));

            Image left = CreatePanel(
                parent,
                name + "_Left",
                color,
                new Vector2(0f, 0f),
                new Vector2(0f, 1f),
                Vector2.zero,
                new Vector2(thickness, 0f));

            Image right = CreatePanel(
                parent,
                name + "_Right",
                color,
                new Vector2(1f, 0f),
                new Vector2(1f, 1f),
                new Vector2(-thickness, 0f),
                Vector2.zero);

            top.raycastTarget = false;
            bottom.raycastTarget = false;
            left.raycastTarget = false;
            right.raycastTarget = false;
        }

        public static void AddBarTicks(
            Transform parent,
            string name,
            int count,
            Color color)
        {
            count = Mathf.Max(2, count);

            for (int i = 1; i < count; i++)
            {
                float x = i / (float)count;

                Image tick = CreatePanel(
                    parent,
                    name + "_" + i,
                    color,
                    new Vector2(x, 0f),
                    new Vector2(x, 1f),
                    new Vector2(-1f, 1f),
                    new Vector2(1f, -1f));

                tick.raycastTarget = false;
            }
        }

        public static Text CreateText(
            Transform parent,
            string name,
            string value,
            int fontSize,
            TextAnchor alignment,
            Color color,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 offsetMin,
            Vector2 offsetMax)
        {
            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Text));

            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            Text text = go.GetComponent<Text>();
            text.font = Font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = color;
            text.supportRichText = true;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;

            return text;
        }

        public static Button CreateButton(
            Transform parent,
            string name,
            string label,
            Vector2 anchor,
            Vector2 position,
            Vector2 size,
            UnityAction onClick,
            Color? background = null)
        {
            GameObject go = new GameObject(
                name,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image),
                typeof(Button));

            go.transform.SetParent(parent, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = anchor;
            rect.anchorMax = anchor;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = position;
            rect.sizeDelta = size;

            Image image = go.GetComponent<Image>();
            image.color =
                background ??
                new Color(
                    0.075f,
                    0.078f,
                    0.060f,
                    0.98f);

            AddPixelFrame(
                go.transform,
                "ButtonFrame",
                new Color(
                    0.42f,
                    0.39f,
                    0.28f,
                    0.92f),
                2f);

            Button button = go.GetComponent<Button>();
            button.transition = Selectable.Transition.ColorTint;
            go.AddComponent<UIButtonMotion>();

            button.onClick.AddListener(() =>
            {
                if (ProceduralAudio.Instance != null)
                {
                    ProceduralAudio.Instance.UIClick();
                }
            });

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor =
                new Color(
                    1.06f,
                    1.04f,
                    0.94f,
                    1f);
            colors.pressedColor =
                new Color(
                    0.72f,
                    0.69f,
                    0.58f,
                    1f);
            colors.selectedColor =
                new Color(
                    1.02f,
                    1.00f,
                    0.90f,
                    1f);
            colors.fadeDuration = 0.02f;
            button.colors = colors;

            if (onClick != null)
            {
                button.onClick.AddListener(onClick);
            }

            Text labelText = CreateText(
                go.transform,
                "Label",
                label,
                23,
                TextAnchor.MiddleCenter,
                Color.white,
                Vector2.zero,
                Vector2.one,
                new Vector2(12f, 6f),
                new Vector2(-12f, -6f));

            labelText.fontStyle = FontStyle.Bold;
            labelText.raycastTarget = false;

            return button;
        }
    }

    public class UIButtonMotion :
        MonoBehaviour,
        IPointerEnterHandler,
        IPointerExitHandler,
        IPointerDownHandler,
        IPointerUpHandler
    {
        private Vector3 targetScale = Vector3.one;
        private bool hovered;

        private void Update()
        {
            transform.localScale =
                targetScale;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            hovered = true;
            targetScale = Vector3.one * 1.02f;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            hovered = false;
            targetScale = Vector3.one;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            targetScale = Vector3.one * 0.98f;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            targetScale = hovered
                ? Vector3.one * 1.02f
                : Vector3.one;
        }
    }
}
