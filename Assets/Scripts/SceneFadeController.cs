using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Depthbound
{
    public class SceneFadeController : MonoBehaviour
    {
        public static SceneFadeController Instance { get; private set; }

        private const float FadeInDuration = 0.48f;
        private const float FadeOutDuration = 0.38f;

        private bool transitioning;
        private Canvas fadeCanvas;
        private Image fadeImage;

        private void Awake()
        {
            Instance = this;
            CreateOverlay(1f);
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Start()
        {
            StartCoroutine(FadeInRoutine());
        }

        public static void Load(string sceneName)
        {
            if (Instance != null)
            {
                Instance.BeginLoad(sceneName);
            }
            else
            {
                Time.timeScale = 1f;
                SceneManager.LoadScene(sceneName);
            }
        }

        private void BeginLoad(string sceneName)
        {
            if (transitioning) return;

            transitioning = true;

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.SetMusicDucked(true);
            }

            StartCoroutine(FadeOutAndLoadRoutine(sceneName));
        }

        private IEnumerator FadeInRoutine()
        {
            for (float elapsed = 0f; elapsed < FadeInDuration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / FadeInDuration);
                SetAlpha(1f - SmoothStep(t));
                yield return null;
            }

            SetAlpha(0f);

            if (fadeImage != null)
            {
                fadeImage.raycastTarget = false;
            }
        }

        private IEnumerator FadeOutAndLoadRoutine(string sceneName)
        {
            if (fadeImage == null)
            {
                CreateOverlay(0f);
            }

            fadeImage.raycastTarget = true;

            for (float elapsed = 0f; elapsed < FadeOutDuration; elapsed += Time.unscaledDeltaTime)
            {
                float t = Mathf.Clamp01(elapsed / FadeOutDuration);
                SetAlpha(SmoothStep(t));
                yield return null;
            }

            SetAlpha(1f);
            Time.timeScale = 1f;
            SceneManager.LoadScene(sceneName);
        }

        private void CreateOverlay(float alpha)
        {
            GameObject canvasObject = new GameObject(
                "SceneFadeCanvas",
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));

            canvasObject.transform.SetParent(transform, false);

            fadeCanvas = canvasObject.GetComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 1000;

            CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);

            GameObject imageObject = new GameObject(
                "Fade",
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(Image));

            imageObject.transform.SetParent(canvasObject.transform, false);

            RectTransform rect = imageObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            fadeImage = imageObject.GetComponent<Image>();
            fadeImage.color = new Color(0.004f, 0.006f, 0.010f, alpha);
            fadeImage.raycastTarget = true;
        }

        private void SetAlpha(float alpha)
        {
            if (fadeImage == null) return;

            Color color = fadeImage.color;
            color.a = Mathf.Clamp01(alpha);
            fadeImage.color = color;
        }

        private static float SmoothStep(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }
    }
}
