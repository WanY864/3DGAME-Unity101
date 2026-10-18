using UnityEngine;

namespace Depthbound
{
    public class SoulShard : MonoBehaviour
    {
        private static Material sharedMaterial;
        private static Material sharedTrailMaterial;

        private Transform target;
        private float healAmount;
        private float age;
        private float hoverPhase;
        private Vector3 launchVelocity;
        private Light glow;
        private TrailRenderer trail;
        private bool collected;

        public static void Spawn(Vector3 position, float healAmount, Vector3 launchDirection)
        {
            GameObject shard = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            shard.name = "SoulShard";
            shard.transform.position = position;
            shard.transform.localScale = Vector3.one * 0.18f;

            Collider collider = shard.GetComponent<Collider>();
            if (collider != null) Object.Destroy(collider);

            SoulShard behaviour = shard.AddComponent<SoulShard>();
            behaviour.healAmount = healAmount;
            behaviour.launchVelocity =
                launchDirection.normalized * Random.Range(1.2f, 2.3f) +
                Vector3.up * Random.Range(1.5f, 2.4f);
            behaviour.hoverPhase = Random.Range(0f, Mathf.PI * 2f);
            behaviour.BuildPresentation();
        }

        private void Start()
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                target = player.transform;
            }
        }

        private void Update()
        {
            if (collected) return;

            if (HUDController.Instance != null &&
                (HUDController.Instance.IsManualPause ||
                 HUDController.Instance.IsEndScreenOpen))
            {
                return;
            }

            age += Time.unscaledDeltaTime;
            float attraction = 0f;

            if (age < 0.42f)
            {
                transform.position += launchVelocity * Time.unscaledDeltaTime;
                launchVelocity = Vector3.Lerp(
                    launchVelocity,
                    Vector3.zero,
                    1f - Mathf.Exp(-5.2f * Time.unscaledDeltaTime));
            }
            else if (target != null)
            {
                Vector3 targetPoint = target.position + Vector3.up * 0.95f;
                Vector3 delta = targetPoint - transform.position;
                float distance = delta.magnitude;

                attraction = 1f - Mathf.Clamp01(distance / 6.0f);

                float speed =
                    Mathf.Lerp(
                        4.5f,
                        13.5f,
                        Mathf.Clamp01((age - 0.42f) / 1.4f)) *
                    Mathf.Lerp(1f, 1.32f, attraction);

                transform.position +=
                    delta.normalized *
                    speed *
                    Time.unscaledDeltaTime;

                if (distance < 0.42f)
                {
                    Collect();
                    return;
                }
            }

            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 7f + hoverPhase) * 0.12f;
            transform.localScale = Vector3.one * 0.18f * pulse;
            transform.Rotate(35f * Time.unscaledDeltaTime, 75f * Time.unscaledDeltaTime, 22f * Time.unscaledDeltaTime, Space.Self);

            if (glow != null)
            {
                glow.intensity =
                    0.75f +
                    Mathf.Sin(
                        Time.unscaledTime * 8f + hoverPhase) *
                        0.22f +
                    attraction * 0.65f;
            }

            if (trail != null)
            {
                trail.time = Mathf.Lerp(0.24f, 0.38f, attraction);
                trail.startWidth = Mathf.Lerp(0.065f, 0.095f, attraction);
            }

            if (age > 8f)
            {
                Destroy(gameObject);
            }
        }

        private void Collect()
        {
            if (collected) return;
            collected = true;

            if (target != null)
            {
                PlayerHealth health = target.GetComponent<PlayerHealth>();
                if (health != null)
                {
                    health.Heal(healAmount);
                }
            }

            if (DungeonDirector.Instance != null)
            {
                DungeonDirector.Instance.RecordSoulShard();
            }

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.SoulCollect();
            }

            CombatEffects.SpawnSoulCollect(transform.position);
            Destroy(gameObject);
        }

        private void BuildPresentation()
        {
            Renderer renderer = GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.sharedMaterial = GetSharedMaterial();
            }

            glow = gameObject.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.renderMode = LightRenderMode.ForceVertex;
            glow.range = 2.3f;
            glow.intensity = 0.78f;
            glow.color = new Color(0.22f, 0.84f, 0.88f);

            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.24f;
            trail.startWidth = 0.065f;
            trail.endWidth = 0f;
            trail.minVertexDistance = 0.075f;
            trail.textureMode = LineTextureMode.Tile;
            trail.numCornerVertices = 0;
            trail.numCapVertices = 0;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;
            trail.sharedMaterial = GetTrailMaterial();
            trail.startColor = new Color(0.35f, 0.92f, 0.92f, 0.72f);
            trail.endColor = new Color(0.12f, 0.46f, 0.62f, 0f);
        }

        private static Material GetSharedMaterial()
        {
            if (sharedMaterial != null) return sharedMaterial;

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            sharedMaterial = new Material(shader);
            sharedMaterial.name = "RuntimeSoulShard";
            sharedMaterial.color = new Color(0.13f, 0.72f, 0.76f);

            if (sharedMaterial.HasProperty("_EmissionColor"))
            {
                sharedMaterial.EnableKeyword("_EMISSION");
                sharedMaterial.SetColor("_EmissionColor", new Color(0.08f, 0.90f, 0.92f) * 2.6f);
            }

            if (sharedMaterial.HasProperty("_Smoothness"))
            {
                sharedMaterial.SetFloat("_Smoothness", 0.72f);
            }

            return sharedMaterial;
        }

        private static Material GetTrailMaterial()
        {
            if (sharedTrailMaterial != null) return sharedTrailMaterial;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Standard");

            sharedTrailMaterial = new Material(shader);
            sharedTrailMaterial.name = "RuntimePixelSoulTrail";
            sharedTrailMaterial.color = new Color(0.28f, 0.86f, 0.90f, 0.74f);
            sharedTrailMaterial.mainTexture = CombatEffects.PixelTrailTexture;
            sharedTrailMaterial.mainTextureScale = new Vector2(4f, 1f);
            sharedTrailMaterial.hideFlags = HideFlags.HideAndDontSave;
            return sharedTrailMaterial;
        }
    }
}
