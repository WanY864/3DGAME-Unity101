using System.Collections.Generic;
using UnityEngine;

namespace Depthbound
{
    public static class CombatEffects
    {
        private const int DamageNumberPoolLimit = 12;

        private static readonly Dictionary<int, Material> ParticleMaterials =
            new Dictionary<int, Material>();

        private static readonly Dictionary<int, Material> ImpactMaterials =
            new Dictionary<int, Material>();

        private static readonly Stack<GameObject> DamageNumberPool =
            new Stack<GameObject>();

        private static Texture2D pixelParticleTexture;
        private static Texture2D pixelTrailTexture;

        public static Texture2D PixelTrailTexture => GetPixelTrailTexture();

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetRuntimeCaches()
        {
            ParticleMaterials.Clear();
            ImpactMaterials.Clear();
            DamageNumberPool.Clear();
            pixelParticleTexture = null;
            pixelTrailTexture = null;
        }

        public static void SpawnWeaponHit(
            Vector3 position,
            Vector3 normal,
            WeaponMode mode,
            bool emphasized = false)
        {
            Color color = mode == WeaponMode.Dagger
                ? new Color(0.45f, 0.78f, 1f)
                : new Color(0.86f, 0.90f, 0.96f);

            SpawnBurst(
                "WeaponHit",
                position + normal * 0.035f,
                color,
                mode == WeaponMode.Dagger ? 9 : 14,
                mode == WeaponMode.Dagger ? 2.4f : 3.2f,
                mode == WeaponMode.Dagger ? 0.045f : 0.065f,
                normal);

            SpawnPixelImpact(
                position,
                normal,
                color,
                mode,
                emphasized);

            if (emphasized)
            {
                SpawnBurst(
                    "MomentumHit",
                    position + normal * 0.055f,
                    new Color(1f, 0.72f, 0.24f),
                    mode == WeaponMode.Dagger ? 8 : 11,
                    mode == WeaponMode.Dagger ? 3.4f : 4.2f,
                    mode == WeaponMode.Dagger ? 0.050f : 0.072f,
                    normal);
            }
        }

        public static void SpawnBlock(Vector3 position, Vector3 forward)
        {
            SpawnBurst(
                "ShieldSpark",
                position,
                new Color(0.55f, 0.86f, 1f),
                18,
                3.6f,
                0.055f,
                -forward);
        }

        public static void SpawnParry(Vector3 position, Vector3 forward)
        {
            SpawnBurst(
                "PerfectBlockBurst",
                position,
                new Color(0.82f, 0.96f, 1f),
                32,
                5.2f,
                0.075f,
                -forward);

            GameObject flash = new GameObject("ParryFlash", typeof(Light));
            flash.transform.position = position;

            Light light = flash.GetComponent<Light>();
            light.type = LightType.Point;
            light.range = 4.5f;
            light.intensity = 4.2f;
            light.color = new Color(0.62f, 0.88f, 1f);

            Object.Destroy(flash, 0.10f);
        }

        public static void SpawnSoulCollect(Vector3 position)
        {
            SpawnBurst(
                "SoulCollect",
                position,
                new Color(0.30f, 0.92f, 0.88f),
                14,
                2.1f,
                0.055f,
                Vector3.up);
        }

        public static void SpawnRuneBurst(Vector3 position, float radius)
        {
            GameObject burst = new GameObject("RuneBurst");
            burst.transform.position = position;

            ParticleSystem particles = burst.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.16f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.24f, 0.42f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 5.0f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.14f);
            main.startColor = new Color(1f, 0.12f, 0.035f);
            main.gravityModifier = 0.18f;
            main.maxParticles = 64;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)42)
            });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius * 0.82f;
            shape.radiusThickness = 1f;

            ApplyParticleMaterial(particles, new Color(1f, 0.14f, 0.035f));
            particles.Play();
            Object.Destroy(burst, 0.85f);
        }

        public static void SpawnEvade(Vector3 position)
        {
            GameObject burst = new GameObject("EvadeMist");
            burst.transform.position = position + Vector3.up * 0.8f;

            ParticleSystem particles = burst.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.16f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.16f, 0.32f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(0.25f, 0.75f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.12f);
            main.startColor = new Color(0.25f, 0.70f, 0.82f, 0.58f);
            main.maxParticles = 28;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)18)
            });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.38f;

            ApplyParticleMaterial(particles, new Color(0.34f, 0.76f, 0.88f, 0.72f));
            particles.Play();
            Object.Destroy(burst, 0.75f);
        }

        private static void SpawnPixelImpact(
            Vector3 position,
            Vector3 normal,
            Color color,
            WeaponMode mode,
            bool emphasized)
        {
            GameObject root =
                new GameObject(
                    emphasized
                        ? "PixelImpactHeavy"
                        : "PixelImpact");

            root.transform.position =
                position +
                normal * 0.045f;

            Vector3 outward =
                normal.sqrMagnitude > 0.001f
                    ? normal.normalized
                    : Vector3.up;

            Vector3 tangent =
                Vector3.Cross(
                    outward,
                    Vector3.up);

            if (tangent.sqrMagnitude < 0.01f)
            {
                tangent = Vector3.right;
            }

            tangent.Normalize();

            Vector3 bitangent =
                Vector3.Cross(
                    outward,
                    tangent).normalized;

            int count =
                mode == WeaponMode.Dagger
                    ? (emphasized ? 8 : 5)
                    : (emphasized ? 12 : 8);

            Material material =
                GetImpactMaterial(color);

            for (int i = 0; i < count; i++)
            {
                GameObject block =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Cube);

                block.name =
                    "ImpactPixel_" + i;

                block.transform.SetParent(
                    root.transform,
                    false);

                float size =
                    mode == WeaponMode.Dagger
                        ? Random.Range(0.045f, 0.075f)
                        : Random.Range(0.060f, 0.105f);

                if (emphasized)
                {
                    size *= 1.18f;
                }

                block.transform.localScale =
                    Vector3.one * size;

                Collider collider =
                    block.GetComponent<Collider>();

                if (collider != null)
                {
                    Object.Destroy(collider);
                }

                Renderer renderer =
                    block.GetComponent<Renderer>();

                if (renderer != null)
                {
                    renderer.sharedMaterial = material;
                    renderer.shadowCastingMode =
                        UnityEngine.Rendering
                            .ShadowCastingMode.Off;
                    renderer.receiveShadows = false;
                }

                Vector3 velocity =
                    outward *
                    Random.Range(0.55f, 1.25f) +
                    tangent *
                    Random.Range(-1.0f, 1.0f) +
                    bitangent *
                    Random.Range(-0.75f, 0.75f);

                velocity *=
                    mode == WeaponMode.Dagger
                        ? 1.45f
                        : 1.90f;

                if (emphasized)
                {
                    velocity *= 1.28f;
                }

                block.AddComponent<PixelImpactBlock>()
                    .Configure(
                        velocity,
                        emphasized ? 0.34f : 0.26f);
            }

            Object.Destroy(root, 0.48f);
        }

        private static Material GetImpactMaterial(
            Color color)
        {
            int key =
                ((Color32)color).GetHashCode();

            if (ImpactMaterials.TryGetValue(
                    key,
                    out Material cached) &&
                cached != null)
            {
                return cached;
            }

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Unlit");

            if (shader == null)
            {
                shader = Shader.Find("Unlit/Color");
            }

            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material =
                new Material(shader);

            material.name =
                "RuntimePixelImpact_" + key;
            material.color = color;

            if (material.HasProperty("_BaseColor"))
            {
                material.SetColor(
                    "_BaseColor",
                    color);
            }

            ImpactMaterials[key] = material;
            return material;
        }

        private static void SpawnBurst(
            string name,
            Vector3 position,
            Color color,
            int count,
            float speed,
            float size,
            Vector3 direction)
        {
            GameObject burst = new GameObject(name);
            burst.transform.position = position;

            if (direction.sqrMagnitude > 0.001f)
            {
                burst.transform.rotation = Quaternion.LookRotation(direction.normalized);
            }

            ParticleSystem particles = burst.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.12f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.10f, 0.24f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(speed * 0.55f, speed);
            main.startSize = new ParticleSystem.MinMaxCurve(size * 0.55f, size);
            main.startColor = color;
            main.gravityModifier = 0.20f;
            main.maxParticles = Mathf.Max(count, 24);

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)count)
            });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 28f;
            shape.radius = 0.02f;

            ApplyParticleMaterial(particles, color);
            particles.Play();
            Object.Destroy(burst, 0.65f);
        }

        public static void SpawnBossAwakening(Vector3 position)
        {
            GameObject root = new GameObject("BossAwakeningColumn");
            root.transform.position = position;

            ParticleSystem particles = root.AddComponent<ParticleSystem>();

            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.72f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.42f, 0.72f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.2f, 5.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.055f, 0.13f);
            main.startColor = new Color(1f, 0.18f, 0.055f, 0.90f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 120;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)72)
            });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 1.15f;
            shape.radiusThickness = 0.72f;

            ParticleSystem.VelocityOverLifetimeModule velocity = particles.velocityOverLifetime;
            velocity.enabled = true;
            velocity.space = ParticleSystemSimulationSpace.World;
            velocity.y = new ParticleSystem.MinMaxCurve(2.8f, 5.4f);

            ApplyParticleMaterial(
                particles,
                new Color(1f, 0.16f, 0.035f, 0.84f));

            GameObject lightObject = new GameObject(
                "BossAwakeningLight",
                typeof(Light),
                typeof(TransientLightPulse));

            lightObject.transform.SetParent(root.transform, false);
            lightObject.transform.localPosition = Vector3.up * 1.5f;

            Light light = lightObject.GetComponent<Light>();
            light.type = LightType.Point;
            light.range = 9f;
            light.intensity = 6.2f;
            light.color = new Color(1f, 0.12f, 0.04f);

            lightObject.GetComponent<TransientLightPulse>().Configure(
                6.2f,
                0.80f);

            particles.Play();
            Object.Destroy(root, 1.15f);
        }

        public static void SpawnBoon(Vector3 position, UpgradeChoice choice)
        {
            Color color;
            switch (choice)
            {
                case UpgradeChoice.Vitality:
                    color = new Color(0.95f, 0.22f, 0.26f);
                    break;
                case UpgradeChoice.Brutality:
                    color = new Color(1f, 0.58f, 0.18f);
                    break;
                default:
                    color = new Color(0.20f, 0.78f, 0.92f);
                    break;
            }

            SpawnBurst(
                "BoonBurst",
                position + Vector3.up * 0.85f,
                color,
                28,
                3.2f,
                0.075f,
                Vector3.up);

            GameObject glow = new GameObject("BoonGlow", typeof(Light));
            glow.transform.position = position + Vector3.up * 1.0f;

            Light light = glow.GetComponent<Light>();
            light.type = LightType.Point;
            light.range = 5.2f;
            light.intensity = 3.4f;
            light.color = color;

            Object.Destroy(glow, 0.18f);
        }

        public static void SpawnDamageNumber(
            Vector3 position,
            float damage,
            bool emphasized)
        {
            GameObject root = GetDamageNumberObject();
            root.name = "DamageNumber";
            root.transform.position = position + Vector3.up * 0.28f;
            root.transform.rotation = Quaternion.identity;
            root.transform.localScale = Vector3.one;
            root.SetActive(true);

            TextMesh text = root.GetComponent<TextMesh>();
            if (text == null)
            {
                text = root.AddComponent<TextMesh>();
            }

            text.text = Mathf.RoundToInt(damage).ToString();
            text.font = UIFactory.Font;
            text.fontSize = 64;
            text.characterSize = emphasized ? 0.034f : 0.028f;
            text.anchor = TextAnchor.MiddleCenter;
            text.alignment = TextAlignment.Center;
            text.color = emphasized
                ? new Color(1f, 0.73f, 0.30f, 0.96f)
                : new Color(0.90f, 0.94f, 1f, 0.88f);

            MeshRenderer renderer = root.GetComponent<MeshRenderer>();
            if (renderer != null && text.font != null)
            {
                renderer.sharedMaterial = text.font.material;
                renderer.sortingOrder = 30;
            }

            FloatingCombatText floating =
                root.GetComponent<FloatingCombatText>();

            if (floating == null)
            {
                floating = root.AddComponent<FloatingCombatText>();
            }

            floating.Initialize(text, emphasized);
        }

        private static GameObject GetDamageNumberObject()
        {
            while (DamageNumberPool.Count > 0)
            {
                GameObject pooled = DamageNumberPool.Pop();
                if (pooled != null)
                {
                    return pooled;
                }
            }

            return new GameObject("DamageNumber");
        }

        public static void ReleaseDamageNumber(GameObject root)
        {
            if (root == null) return;

            if (DamageNumberPool.Count < DamageNumberPoolLimit)
            {
                root.SetActive(false);
                DamageNumberPool.Push(root);
            }
            else
            {
                Object.Destroy(root);
            }
        }

        private static Texture2D GetPixelParticleTexture()
        {
            if (pixelParticleTexture != null) return pixelParticleTexture;

            pixelParticleTexture = new Texture2D(
                4,
                4,
                TextureFormat.RGBA32,
                false);

            pixelParticleTexture.name = "RuntimePixelParticle";
            pixelParticleTexture.filterMode = FilterMode.Point;
            pixelParticleTexture.wrapMode = TextureWrapMode.Clamp;
            pixelParticleTexture.anisoLevel = 0;
            pixelParticleTexture.hideFlags = HideFlags.HideAndDontSave;

            Color[] pixels = new Color[16];
            for (int index = 0; index < pixels.Length; index++)
            {
                pixels[index] = Color.white;
            }

            pixelParticleTexture.SetPixels(pixels);
            pixelParticleTexture.Apply(false, true);
            return pixelParticleTexture;
        }

        private static Texture2D GetPixelTrailTexture()
        {
            if (pixelTrailTexture != null) return pixelTrailTexture;

            pixelTrailTexture = new Texture2D(
                8,
                1,
                TextureFormat.RGBA32,
                false);

            pixelTrailTexture.name = "RuntimePixelTrail";
            pixelTrailTexture.filterMode = FilterMode.Point;
            pixelTrailTexture.wrapMode = TextureWrapMode.Repeat;
            pixelTrailTexture.anisoLevel = 0;
            pixelTrailTexture.hideFlags = HideFlags.HideAndDontSave;

            Color clear = new Color(1f, 1f, 1f, 0f);
            pixelTrailTexture.SetPixels(new[]
            {
                Color.white,
                Color.white,
                Color.white,
                clear,
                clear,
                Color.white,
                clear,
                clear
            });

            pixelTrailTexture.Apply(false, true);
            return pixelTrailTexture;
        }

        public static void ApplyParticleMaterial(ParticleSystem particles, Color color)
        {
            ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

            int key = ((Color32)color).GetHashCode();

            if (!ParticleMaterials.TryGetValue(key, out Material material) || material == null)
            {
                Shader shader = Shader.Find("Particles/Standard Unlit");

                if (shader == null)
                {
                    shader = Shader.Find("Legacy Shaders/Particles/Additive");
                }

                if (shader == null) return;

                material = new Material(shader);
                material.name = "RuntimeCombatParticle_" + key;
                material.color = color;
                material.hideFlags = HideFlags.HideAndDontSave;
                ParticleMaterials[key] = material;
            }

            Texture2D pixelTexture = GetPixelParticleTexture();
            material.mainTexture = pixelTexture;

            if (material.HasProperty("_BaseMap"))
            {
                material.SetTexture("_BaseMap", pixelTexture);
            }

            if (material.HasProperty("_MainTex"))
            {
                material.SetTexture("_MainTex", pixelTexture);
            }

            renderer.sharedMaterial = material;
        }
    }

    public class PixelImpactBlock : MonoBehaviour
    {
        private Vector3 velocity;
        private Vector3 startScale;
        private float lifetime;
        private float elapsed;

        public void Configure(
            Vector3 initialVelocity,
            float duration)
        {
            velocity = initialVelocity;
            lifetime = Mathf.Max(0.08f, duration);
            startScale = transform.localScale;
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            elapsed += dt;
            velocity += Vector3.down * 4.5f * dt;

            Vector3 next =
                transform.position +
                velocity * dt;

            const float grid = 0.025f;

            transform.position =
                new Vector3(
                    Mathf.Round(next.x / grid) * grid,
                    Mathf.Round(next.y / grid) * grid,
                    Mathf.Round(next.z / grid) * grid);

            float t =
                Mathf.Clamp01(
                    elapsed /
                    lifetime);

            float stepped =
                Mathf.Floor(t * 5f) / 5f;

            transform.localScale =
                startScale *
                Mathf.Lerp(
                    1f,
                    0.18f,
                    stepped);

            if (elapsed >= lifetime)
            {
                Destroy(gameObject);
            }
        }
    }

    public class TransientLightPulse : MonoBehaviour
    {
        private Light target;
        private float startIntensity;
        private float duration = 0.8f;
        private float elapsed;

        public void Configure(float intensity, float lifetime)
        {
            startIntensity = intensity;
            duration = Mathf.Max(0.05f, lifetime);
        }

        private void Awake()
        {
            target = GetComponent<Light>();
            if (target != null && startIntensity <= 0f)
            {
                startIntensity = target.intensity;
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);

            if (target != null)
            {
                float pulse = 0.86f + Mathf.Sin(t * Mathf.PI * 8f) * 0.14f;
                target.intensity =
                    startIntensity *
                    (1f - t) *
                    pulse;
            }
        }
    }

    public class FloatingCombatText : MonoBehaviour
    {
        private TextMesh text;
        private Camera viewCamera;
        private float lifetime;
        private float duration;
        private Vector3 drift;

        public void Initialize(TextMesh targetText, bool emphasized)
        {
            text = targetText;
            lifetime = 0f;
            duration = emphasized ? 0.72f : 0.58f;
            drift = new Vector3(
                Random.Range(-0.10f, 0.10f),
                emphasized ? 0.78f : 0.62f,
                Random.Range(-0.04f, 0.04f));
            viewCamera = Camera.main;
            transform.localScale = Vector3.one;
        }

        private void LateUpdate()
        {
            float dt = Time.deltaTime;
            lifetime += dt;
            transform.position += drift * dt;

            if (viewCamera == null)
            {
                viewCamera = Camera.main;
            }

            if (viewCamera != null)
            {
                Vector3 toCamera = transform.position - viewCamera.transform.position;
                if (toCamera.sqrMagnitude > 0.001f)
                {
                    transform.rotation = Quaternion.LookRotation(toCamera.normalized, Vector3.up);
                }
            }

            if (text != null)
            {
                float t = Mathf.Clamp01(lifetime / Mathf.Max(0.01f, duration));
                Color color = text.color;
                color.a = 1f - t;
                text.color = color;

                float pop = t < 0.18f
                    ? Mathf.Lerp(0.72f, 1.08f, t / 0.18f)
                    : Mathf.Lerp(1.08f, 0.92f, (t - 0.18f) / 0.82f);

                transform.localScale = Vector3.one * pop;
            }

            if (lifetime >= duration)
            {
                CombatEffects.ReleaseDamageNumber(gameObject);
            }
        }
    }
}
