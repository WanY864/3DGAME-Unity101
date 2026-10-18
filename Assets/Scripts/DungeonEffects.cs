using System.Collections;
using UnityEngine;

namespace Depthbound
{
    public class TorchFlicker : MonoBehaviour
    {
        private Light torchLight;
        private float baseIntensity;
        private float seed;

        private void Start()
        {
            torchLight = GetComponent<Light>();
            if (torchLight != null) baseIntensity = torchLight.intensity;
            seed = Random.Range(0f, 100f);
        }

        private void Update()
        {
            if (torchLight == null) return;

            float slow = Mathf.PerlinNoise(seed, Time.time * 1.7f) - 0.5f;
            float fast = Mathf.PerlinNoise(seed + 12.7f, Time.time * 5.5f) - 0.5f;
            torchLight.intensity = baseIntensity + slow * 0.28f + fast * 0.08f;
        }
    }

    public class RunePulse : MonoBehaviour
    {
        private Renderer targetRenderer;
        private Light pulseLight;
        private MaterialPropertyBlock propertyBlock;
        private Vector3 baseScale;
        private Color baseColor;

        private void Start()
        {
            targetRenderer = GetComponent<Renderer>();
            pulseLight = GetComponentInChildren<Light>();
            baseScale = transform.localScale;

            if (targetRenderer != null)
            {
                Material shared = targetRenderer.sharedMaterial;
                baseColor = shared != null
                    ? shared.color
                    : Color.white;

                propertyBlock = new MaterialPropertyBlock();
            }
        }

        private void Update()
        {
            transform.Rotate(0f, 3.5f * Time.deltaTime, 0f, Space.Self);

            float wave = 0.5f + 0.5f * Mathf.Sin(Time.time * 2.1f);
            transform.localScale = baseScale * Mathf.Lerp(0.995f, 1.015f, wave);

            if (pulseLight != null)
            {
                pulseLight.intensity = Mathf.Lerp(0.45f, 0.85f, wave);
            }

            if (targetRenderer != null &&
                propertyBlock != null &&
                targetRenderer.sharedMaterial != null &&
                targetRenderer.sharedMaterial.HasProperty("_EmissionColor"))
            {
                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(
                    "_EmissionColor",
                    baseColor * Mathf.Lerp(0.8f, 2.1f, wave));
                targetRenderer.SetPropertyBlock(propertyBlock);
            }
        }
    }

    public class BoonFocusVisual : MonoBehaviour
    {
        private Light glow;
        private Vector3 basePosition;
        private float phase;

        private void Start()
        {
            glow = GetComponentInChildren<Light>();
            basePosition = transform.position;
            phase = Random.Range(0f, Mathf.PI * 2f);
        }

        private void Update()
        {
            float time = Time.unscaledTime;
            float dt = Time.unscaledDeltaTime;

            transform.position =
                basePosition +
                Vector3.up * (0.06f + Mathf.Sin(time * 2.2f + phase) * 0.055f);

            transform.Rotate(
                0f,
                42f * dt,
                0f,
                Space.World);

            if (glow != null)
            {
                glow.intensity =
                    1.35f +
                    Mathf.Sin(time * 3.6f + phase) * 0.28f;
            }
        }
    }

    public class DungeonDoor : MonoBehaviour
    {
        private Vector3 closedPosition;
        private Coroutine motionRoutine;
        private GameObject unlockGuide;
        private Light unlockGuideLight;
        private bool initialized;

        private void Awake()
        {
            CaptureClosedPosition();
        }

        private void Start()
        {
            ResolveUnlockGuide();
        }

        private void ResolveUnlockGuide()
        {
            if (unlockGuide != null) return;

            Transform guide = transform.Find("UnlockGuide");
            if (guide == null) return;

            unlockGuide = guide.gameObject;
            unlockGuideLight = guide.GetComponentInChildren<Light>();
        }

        private void CaptureClosedPosition()
        {
            if (initialized) return;
            closedPosition = transform.localPosition;
            initialized = true;
        }

        public void Open(bool highlightGuide = true)
        {
            CaptureClosedPosition();
            ResolveUnlockGuide();

            if (!gameObject.activeInHierarchy)
            {
                gameObject.SetActive(true);
            }

            StartMotion(OpenRoutine(highlightGuide));
        }

        public void CloseFromAbove()
        {
            CaptureClosedPosition();
            ResolveUnlockGuide();

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            StartMotion(CloseRoutine());
        }

        public void SnapClosed(bool active)
        {
            CaptureClosedPosition();
            ResolveUnlockGuide();

            if (motionRoutine != null)
            {
                StopCoroutine(motionRoutine);
                motionRoutine = null;
            }

            transform.localPosition = closedPosition;

            if (unlockGuide != null)
            {
                unlockGuide.SetActive(false);
            }

            gameObject.SetActive(active);
        }

        private void StartMotion(IEnumerator routine)
        {
            if (motionRoutine != null)
            {
                StopCoroutine(motionRoutine);
            }

            motionRoutine = StartCoroutine(routine);
        }

        private IEnumerator OpenRoutine(bool highlightGuide)
        {
            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.GateOpen(transform.position);
            }

            if (unlockGuide != null)
            {
                unlockGuide.SetActive(highlightGuide);
            }

            Vector3 start = transform.localPosition;
            Vector3 end = closedPosition + Vector3.up * 4.5f;
            const float duration = 0.60f;

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = SmoothStep(t);
                transform.localPosition = Vector3.Lerp(start, end, eased);

                if (highlightGuide && unlockGuideLight != null)
                {
                    float pulse =
                        0.5f +
                        0.5f * Mathf.Sin(t * Mathf.PI * 4f);

                    unlockGuideLight.intensity =
                        Mathf.Lerp(0.45f, 2.2f, pulse) *
                        (1f - t * 0.35f);
                }

                yield return null;
            }

            transform.localPosition = end;

            if (unlockGuide != null)
            {
                unlockGuide.SetActive(false);
            }

            gameObject.SetActive(false);
            motionRoutine = null;
        }

        private IEnumerator CloseRoutine()
        {
            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.GateClose(transform.position);
            }

            Vector3 start = closedPosition + Vector3.up * 4.5f;
            transform.localPosition = start;

            const float duration = 0.48f;

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                transform.localPosition = Vector3.Lerp(start, closedPosition, eased);
                yield return null;
            }

            transform.localPosition = closedPosition;
            motionRoutine = null;
        }

        private static float SmoothStep(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }
    }
}
