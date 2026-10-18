using System.Collections;
using UnityEngine;

namespace Depthbound
{
    public class RuneHazard : MonoBehaviour
    {
        private static readonly Material[] telegraphMaterials =
            new Material[3];

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticMaterials()
        {
            for (int i = 0;
                 i < telegraphMaterials.Length;
                 i++)
            {
                telegraphMaterials[i] = null;
            }
        }

        private Transform telegraphDisc;
        private Renderer telegraphRenderer;
        private Light warningLight;
        private Transform player;
        private readonly RaycastHit[] coverBuffer =
            new RaycastHit[10];
        private float radius = 2.7f;
        private float damage = 18f;
        private int hazardStyle;
        private float nextPulseTime;
        private bool armed;
        private bool pulsing;

        public void Configure(
            float newRadius,
            float newDamage,
            int style = 0)
        {
            radius = Mathf.Max(0.5f, newRadius);
            damage = Mathf.Max(1f, newDamage);
            hazardStyle = Mathf.Clamp(style, 0, 2);

            if (warningLight != null)
            {
                warningLight.range = radius * 1.8f;
                warningLight.color = HazardColor();
            }

            if (telegraphRenderer != null)
            {
                telegraphRenderer.sharedMaterial =
                    GetTelegraphMaterial(hazardStyle);
            }
        }

        private void Awake()
        {
            BuildVisual();
        }

        private void Start()
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                player = playerObject.transform;
            }

            SetArmed(false);
        }

        private void Update()
        {
            if (!armed || pulsing) return;

            if (player == null)
            {
                GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null) player = playerObject.transform;
                return;
            }

            if (Time.time >= nextPulseTime)
            {
                StartCoroutine(PulseRoutine());
            }
        }

        public void SetArmed(bool value)
        {
            armed = value;

            if (!armed)
            {
                StopAllCoroutines();
                pulsing = false;
                HideTelegraph();
            }
            else
            {
                nextPulseTime =
                    Time.time +
                    (hazardStyle == 1
                        ? 1.45f
                        : hazardStyle == 2
                            ? 2.0f
                            : 1.8f);
            }
        }

        private IEnumerator PulseRoutine()
        {
            pulsing = true;

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.RuneCharge(transform.position);
            }

            if (telegraphDisc != null)
            {
                telegraphDisc.gameObject.SetActive(true);
                telegraphDisc.localScale = new Vector3(0.18f, 0.018f, 0.18f);
            }

            float chargeTime =
                hazardStyle == 1
                    ? 0.72f
                    : hazardStyle == 2
                        ? 1.08f
                        : 0.95f;

            for (float elapsed = 0f; elapsed < chargeTime; elapsed += Time.deltaTime)
            {
                if (!armed)
                {
                    pulsing = false;
                    HideTelegraph();
                    yield break;
                }

                float t = Mathf.Clamp01(elapsed / chargeTime);
                float eased = t * t * (3f - 2f * t);
                float diameter = Mathf.Lerp(0.18f, radius * 2f, eased);
                float pulse = 1f + Mathf.Sin(t * Mathf.PI * 12f) * 0.035f;

                if (telegraphDisc != null)
                {
                    telegraphDisc.localScale = new Vector3(
                        diameter * pulse,
                        0.018f,
                        diameter * pulse);
                }

                if (warningLight != null)
                {
                    warningLight.intensity = Mathf.Lerp(0.25f, 2.6f, eased);
                }

                yield return null;
            }

            Detonate();
            HideTelegraph();

            pulsing = false;
            nextPulseTime =
                Time.time +
                (hazardStyle == 1
                    ? 2.70f
                    : hazardStyle == 2
                        ? 3.65f
                        : 3.35f);
        }

        private void Detonate()
        {
            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.RuneBurst(transform.position);
            }

            CombatEffects.SpawnRuneBurst(transform.position + Vector3.up * 0.05f, radius);

            if (player == null) return;

            Vector3 delta = player.position - transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;

            FirstPersonController controller = player.GetComponent<FirstPersonController>();
            if (controller != null && distance <= radius + 2.0f)
            {
                float rumble = Mathf.Lerp(
                    0.28f,
                    0.07f,
                    Mathf.Clamp01(distance / (radius + 2.0f)));

                controller.AddCameraImpact(rumble, delta.x >= 0f ? 0.18f : -0.18f);
            }

            if (distance > radius) return;

            if (!HasClearBlastPath())
            {
                if (HUDController.Instance != null)
                {
                    HUDController.Instance.ShowCovered();
                }

                return;
            }

            PlayerHealth health =
                player.GetComponent<PlayerHealth>();

            float before =
                health != null
                    ? health.CurrentHealth
                    : 0f;

            if (health != null)
            {
                health.TakeEnvironmentalDamage(
                    damage,
                    transform.position);
            }

            Rigidbody body =
                player.GetComponent<Rigidbody>();

            if (health != null &&
                health.CurrentHealth < before &&
                body != null &&
                delta.sqrMagnitude > 0.01f)
            {
                float force =
                    hazardStyle == 2
                        ? 4.4f
                        : hazardStyle == 1
                            ? 3.4f
                            : 3.8f;

                body.AddForce(
                    delta.normalized * force +
                    Vector3.up * 0.75f,
                    ForceMode.Impulse);
            }
        }

        private bool HasClearBlastPath()
        {
            if (player == null)
            {
                return false;
            }

            Vector3 origin =
                transform.position +
                Vector3.up * 0.14f;

            Vector3 targetPoint =
                player.position +
                Vector3.up * 0.82f;

            Vector3 delta =
                targetPoint -
                origin;

            float distance =
                delta.magnitude;

            if (distance <= 0.05f)
            {
                return true;
            }

            int count =
                Physics.RaycastNonAlloc(
                    origin,
                    delta / distance,
                    coverBuffer,
                    distance,
                    ~(1 << 2),
                    QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider hit =
                    coverBuffer[i].collider;

                if (hit == null)
                {
                    continue;
                }

                if (hit.GetComponentInParent<EnemyController>() != null)
                {
                    continue;
                }

                if (hit.attachedRigidbody != null)
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        private void BuildVisual()
        {
            GameObject disc = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disc.name = "RuneHazardTelegraph";
            disc.transform.SetParent(transform, false);
            disc.transform.localPosition = new Vector3(0f, 0.035f, 0f);
            disc.transform.localScale = new Vector3(0.18f, 0.018f, 0.18f);

            Collider collider = disc.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            Renderer renderer =
                disc.GetComponent<Renderer>();

            if (renderer != null)
            {
                renderer.sharedMaterial =
                    GetTelegraphMaterial(hazardStyle);

                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;

                renderer.receiveShadows = false;
            }

            telegraphRenderer = renderer;
            telegraphDisc = disc.transform;
            telegraphDisc.gameObject.SetActive(false);

            GameObject lightObject = new GameObject("RuneHazardLight", typeof(Light));
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = Vector3.up * 0.35f;

            warningLight = lightObject.GetComponent<Light>();
            warningLight.type = LightType.Point;
            warningLight.range = radius * 1.8f;
            warningLight.intensity = 0f;
            warningLight.color = HazardColor();
        }

        private void HideTelegraph()
        {
            if (telegraphDisc != null)
            {
                telegraphDisc.gameObject.SetActive(false);
            }

            if (warningLight != null)
            {
                warningLight.intensity = 0f;
            }
        }

        private Color HazardColor()
        {
            switch (hazardStyle)
            {
                case 1:
                    return new Color(
                        0.92f,
                        0.52f,
                        0.14f);

                case 2:
                    return new Color(
                        0.78f,
                        0.14f,
                        0.08f);

                default:
                    return new Color(
                        0.72f,
                        0.24f,
                        0.10f);
            }
        }

        private static Material GetTelegraphMaterial(
            int style)
        {
            style = Mathf.Clamp(style, 0, 2);

            if (telegraphMaterials[style] != null)
            {
                return telegraphMaterials[style];
            }

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Lit");

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Color color =
                style == 1
                    ? new Color(
                        0.46f,
                        0.24f,
                        0.055f)
                    : style == 2
                        ? new Color(
                            0.46f,
                            0.055f,
                            0.025f)
                        : new Color(
                            0.42f,
                            0.12f,
                            0.045f);

            Material material =
                new Material(shader);

            material.name =
                "RuntimeRuneHazard_" + style;

            material.color = color;

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor(
                    "_EmissionColor",
                    color * 2.2f);
            }

            telegraphMaterials[style] = material;
            return material;
        }
    }
}
