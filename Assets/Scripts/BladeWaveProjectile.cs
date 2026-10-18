using System.Collections.Generic;
using UnityEngine;

namespace Depthbound
{
    /// Short-lived, multi-target pixel blade wave used by combo finishers,
    /// Boost attacks and Perfect-Block ripostes.
    public class BladeWaveProjectile : MonoBehaviour
    {
        private static Material singleSwordMaterial;
        private static Material shieldSwordMaterial;
        private static Material riposteMaterial;

        private readonly HashSet<EntityId> hitTargets = new HashSet<EntityId>();

        private Vector3 direction;
        private float damage;
        private float impact;
        private float speed;
        private float remainingDistance;
        private float radius;
        private WeaponMode mode;
        private bool empowered;
        private Vector3 baseScale;

        public static void Spawn(
            Vector3 position,
            Vector3 direction,
            WeaponMode mode,
            float damage,
            float impact,
            bool empowered)
        {
            GameObject waveObject =
                new GameObject(
                    empowered
                        ? "PixelBladeWave_Empowered"
                        : "PixelBladeWave");

            BladeWaveProjectile wave =
                waveObject.AddComponent<BladeWaveProjectile>();

            wave.Initialize(
                position,
                direction,
                mode,
                damage,
                impact,
                empowered);
        }

        private void Initialize(
            Vector3 position,
            Vector3 forward,
            WeaponMode attackMode,
            float waveDamage,
            float waveImpact,
            bool isEmpowered)
        {
            transform.position = position;

            direction = forward.sqrMagnitude > 0.001f
                ? forward.normalized
                : Vector3.forward;

            transform.rotation =
                Quaternion.LookRotation(direction, Vector3.up);

            mode = attackMode;
            damage = Mathf.Max(1f, waveDamage);
            impact = Mathf.Max(0.25f, waveImpact);
            empowered = isEmpowered;
            speed = empowered ? 15.5f : 12.5f;
            remainingDistance =
                mode == WeaponMode.Dagger
                    ? (empowered ? 9.0f : 7.0f)
                    : (empowered ? 7.5f : 5.8f);
            radius =
                mode == WeaponMode.Dagger
                    ? 0.62f
                    : 0.78f;

            BuildVisual();
            baseScale = transform.localScale;
        }

        private void Update()
        {
            float distance =
                Mathf.Min(
                    remainingDistance,
                    speed * Time.deltaTime);

            if (distance <= 0f)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 start = transform.position;
            Vector3 end = start + direction * distance;

            RaycastHit[] hits =
                Physics.SphereCastAll(
                    start,
                    radius,
                    direction,
                    distance,
                    ~(1 << 2),
                    QueryTriggerInteraction.Ignore);

            for (int i = 0; i < hits.Length; i++)
            {
                ResolveHit(hits[i]);
            }

            transform.position = end;
            remainingDistance -= distance;

            float pulse =
                0.90f +
                Mathf.Sin(Time.time * 28f) * 0.10f;

            transform.localScale =
                baseScale *
                pulse;

            if (remainingDistance <= 0.01f)
            {
                Destroy(gameObject);
            }
        }

        private void ResolveHit(RaycastHit hit)
        {
            EnemyController enemy =
                hit.collider.GetComponentInParent<EnemyController>();

            MercyCompanion companion =
                hit.collider.GetComponentInParent<MercyCompanion>();

            if (enemy == null &&
                (companion == null || companion.IsFriendly))
            {
                return;
            }

            // A finisher wave must never auto-resolve a surrender choice.
            if (enemy != null &&
                (enemy.IsDead || enemy.IsAwaitingMercy))
            {
                return;
            }

            EntityId id = enemy != null
                ? enemy.GetEntityId()
                : companion.GetEntityId();

            if (!hitTargets.Add(id))
            {
                return;
            }

            if (enemy != null)
            {
                enemy.TakeDamage(
                    damage,
                    direction,
                    impact);
            }
            else
            {
                companion.TakeDamage(
                    damage,
                    direction);
            }

            Vector3 point =
                hit.point == Vector3.zero
                    ? transform.position
                    : hit.point;

            CombatEffects.SpawnWeaponHit(
                point,
                -direction,
                mode,
                empowered);

            CombatEffects.SpawnDamageNumber(
                point,
                damage,
                empowered);

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.Hit(
                    mode,
                    empowered);
            }
        }

        private void BuildVisual()
        {
            Material material =
                GetMaterial(
                    mode,
                    empowered);

            int blocks = empowered ? 11 : 9;
            float spacing = empowered ? 0.18f : 0.15f;

            for (int i = 0; i < blocks; i++)
            {
                float centered =
                    i - (blocks - 1) * 0.5f;

                GameObject block =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Cube);

                block.name = "WaveBlock_" + i;
                block.transform.SetParent(
                    transform,
                    false);

                block.transform.localPosition =
                    new Vector3(
                        centered * spacing,
                        -Mathf.Abs(centered) * 0.025f,
                        0f);

                block.transform.localScale =
                    new Vector3(
                        empowered ? 0.20f : 0.16f,
                        empowered ? 0.16f : 0.12f,
                        0.07f);

                Collider blockCollider =
                    block.GetComponent<Collider>();

                if (blockCollider != null)
                {
                    blockCollider.enabled = false;
                }

                Renderer renderer =
                    block.GetComponent<Renderer>();

                renderer.sharedMaterial = material;
                renderer.shadowCastingMode =
                    UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
        }

        private static Material GetMaterial(
            WeaponMode mode,
            bool empowered)
        {
            if (empowered)
            {
                if (riposteMaterial == null)
                {
                    riposteMaterial =
                        MakeMaterial(
                            new Color(
                                1.0f,
                                0.78f,
                                0.30f,
                                0.92f));
                }

                return riposteMaterial;
            }

            if (mode == WeaponMode.Dagger)
            {
                if (singleSwordMaterial == null)
                {
                    singleSwordMaterial =
                        MakeMaterial(
                            new Color(
                                0.45f,
                                0.82f,
                                0.92f,
                                0.88f));
                }

                return singleSwordMaterial;
            }

            if (shieldSwordMaterial == null)
            {
                shieldSwordMaterial =
                    MakeMaterial(
                        new Color(
                            0.90f,
                            0.57f,
                            0.24f,
                            0.90f));
            }

            return shieldSwordMaterial;
        }

        private static Material MakeMaterial(Color color)
        {
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                shader = Shader.Find("Sprites/Default");
            }
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Material material =
                new Material(shader);

            material.color = color;
            material.enableInstancing = true;
            return material;
        }
    }
}
