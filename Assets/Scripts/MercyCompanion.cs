using System.Collections;
using UnityEngine;

namespace Depthbound
{
    public class MercyCompanion : MonoBehaviour
    {
        private EnemyArchetype archetype;
        private bool friendly;
        private EnemyController boss;
        private PlayerHealth player;
        private Rigidbody body;
        private SpriteRenderer spriteRenderer;
        private Transform visualTransform;

        private float nextAttackTime;
        private float phase;
        private float health;
        private float moveSpeed;
        private float attackRange;
        private float attackCooldown;
        private float attackDamage;
        private float weaveStrength;
        private float weaveSpeed;
        private float retreatImpulse;
        private float knockbackResistance;
        private float attackWindup;
        private int attackCounter;
        private bool combatReady;
        private bool attacking;
        private Vector3 baseVisualScale;

        public bool IsFriendly => friendly;

        public void Configure(
            EnemyArchetype archetypeValue,
            bool isFriendly,
            EnemyController bossTarget,
            PlayerHealth playerTarget)
        {
            archetype = archetypeValue;
            friendly = isFriendly;
            boss = bossTarget;
            player = playerTarget;

            phase =
                (int)archetype * 1.73f +
                (friendly ? 0.4f : 2.2f);

            ConfigureArchetypeStats();

            Collider ownCollider =
                GetComponent<Collider>();

            if (friendly &&
                ownCollider != null)
            {
                ownCollider.enabled = false;
            }
        }

        private void Awake()
        {
            body =
                gameObject.AddComponent<Rigidbody>();

            body.mass = 0.7f;
            body.useGravity = false;

            body.constraints =
                RigidbodyConstraints.FreezeRotation |
                RigidbodyConstraints.FreezePositionY;

            body.interpolation =
                RigidbodyInterpolation.Interpolate;

            body.collisionDetectionMode =
                CollisionDetectionMode.ContinuousSpeculative;

            SphereCollider collider =
                gameObject.AddComponent<SphereCollider>();

            collider.radius = 0.42f;
            collider.center =
                new Vector3(
                    0f,
                    0.55f,
                    0f);
        }

        private void Start()
        {
            BuildVisual();
            StartCoroutine(SpawnRoutine());
        }

        private void FixedUpdate()
        {
            if (!combatReady)
            {
                if (body != null)
                {
                    body.linearVelocity =
                        Vector3.zero;
                }

                return;
            }

            if (boss == null)
            {
                Destroy(gameObject);
                return;
            }

            Transform target =
                friendly
                    ? boss.transform
                    : player != null
                        ? player.transform
                        : null;

            if (target == null)
            {
                Destroy(gameObject);
                return;
            }

            Vector3 delta =
                target.position -
                transform.position;

            delta.y = 0f;

            float distance =
                delta.magnitude;

            Vector3 forward =
                distance > 0.01f
                    ? delta / distance
                    : transform.forward;

            if (attacking)
            {
                body.linearVelocity =
                    new Vector3(
                        0f,
                        body.linearVelocity.y,
                        0f);
                return;
            }

            if (distance > attackRange)
            {
                Vector3 steering =
                    BuildSteering(
                        forward);

                Vector3 velocity =
                    steering *
                    moveSpeed;

                body.linearVelocity =
                    new Vector3(
                        velocity.x,
                        body.linearVelocity.y,
                        velocity.z);

                if (steering.sqrMagnitude >
                    0.01f)
                {
                    transform.rotation =
                        Quaternion.Slerp(
                            transform.rotation,
                            Quaternion.LookRotation(
                                steering,
                                Vector3.up),
                            1f -
                            Mathf.Exp(
                                -10f *
                                Time.fixedDeltaTime));
                }

                return;
            }

            body.linearVelocity =
                new Vector3(
                    0f,
                    body.linearVelocity.y,
                    0f);

            if (Time.time <
                nextAttackTime)
            {
                return;
            }

            StartCoroutine(
                AttackRoutine());

        }

        private void Update()
        {
            if (archetype !=
                    EnemyArchetype.Bat ||
                visualTransform == null)
            {
                return;
            }

            Vector3 p =
                visualTransform.localPosition;

            p.y =
                0.62f +
                Mathf.Sin(
                    Time.time *
                    5.6f +
                    phase) *
                0.16f;

            visualTransform.localPosition = p;
        }

        public void TakeDamage(
            float amount,
            Vector3 hitDirection)
        {
            if (friendly ||
                !combatReady ||
                amount <= 0f)
            {
                return;
            }

            health =
                Mathf.Max(
                    0f,
                    health - amount);

            if (body != null)
            {
                body.AddForce(
                    hitDirection *
                    (1.8f /
                     Mathf.Max(
                         0.65f,
                         knockbackResistance)),
                    ForceMode.Impulse);
            }

            if (health <= 0f)
            {
                Destroy(gameObject);
            }
        }

        private void ConfigureArchetypeStats()
        {
            switch (archetype)
            {
                case EnemyArchetype.Mole:
                    health = 28f;
                    moveSpeed = 3.85f;
                    attackRange = 1.22f;
                    attackCooldown = 0.76f;
                    attackDamage =
                        friendly
                            ? 5.4f
                            : 7.2f;
                    weaveStrength = 0.12f;
                    weaveSpeed = 2.4f;
                    retreatImpulse = 0.30f;
                    knockbackResistance = 0.82f;
                    attackWindup = 0.18f;
                    break;

                case EnemyArchetype.Armored:
                    health = 48f;
                    moveSpeed = 2.25f;
                    attackRange = 1.55f;
                    attackCooldown = 1.34f;
                    attackDamage =
                        friendly
                            ? 7.0f
                            : 9.2f;
                    weaveStrength = 0f;
                    weaveSpeed = 0f;
                    retreatImpulse = 0f;
                    knockbackResistance = 1.35f;
                    attackWindup = 0.34f;
                    break;

                case EnemyArchetype.Mech:
                    health = 58f;
                    moveSpeed = 2.02f;
                    attackRange = 1.68f;
                    attackCooldown = 1.48f;
                    attackDamage =
                        friendly
                            ? 8.6f
                            : 11.4f;
                    weaveStrength = 0f;
                    weaveSpeed = 0f;
                    retreatImpulse = 0f;
                    knockbackResistance = 1.60f;
                    attackWindup = 0.42f;
                    break;

                case EnemyArchetype.Skeleton:
                    health = 34f;
                    moveSpeed = 3.10f;
                    attackRange = 1.42f;
                    attackCooldown = 0.98f;
                    attackDamage =
                        friendly
                            ? 6.1f
                            : 8.0f;
                    weaveStrength = 0.26f;
                    weaveSpeed = 2.2f;
                    retreatImpulse = 0.72f;
                    knockbackResistance = 0.95f;
                    attackWindup = 0.26f;
                    break;

                case EnemyArchetype.Bat:
                    health = 24f;
                    moveSpeed = 4.45f;
                    attackRange = 1.18f;
                    attackCooldown = 0.66f;
                    attackDamage =
                        friendly
                            ? 4.8f
                            : 6.4f;
                    weaveStrength = 0.58f;
                    weaveSpeed = 3.4f;
                    retreatImpulse = 1.35f;
                    knockbackResistance = 0.70f;
                    attackWindup = 0.15f;
                    break;

                default:
                    health = 42f;
                    moveSpeed = 2.38f;
                    attackRange = 1.40f;
                    attackCooldown = 1.14f;
                    attackDamage =
                        friendly
                            ? 6.3f
                            : 8.3f;
                    weaveStrength = 0.05f;
                    weaveSpeed = 1.6f;
                    retreatImpulse = 0.10f;
                    knockbackResistance = 1.18f;
                    attackWindup = 0.30f;
                    break;
            }
        }

        private Vector3 BuildSteering(
            Vector3 forward)
        {
            if (weaveStrength <= 0f)
            {
                return forward;
            }

            Vector3 tangent =
                new Vector3(
                    -forward.z,
                    0f,
                    forward.x);

            float weave =
                Mathf.Sin(
                    Time.time *
                    weaveSpeed +
                    phase);

            Vector3 steering =
                forward +
                tangent *
                weave *
                weaveStrength;

            return steering.sqrMagnitude >
                0.001f
                    ? steering.normalized
                    : forward;
        }

        private float CurrentAttackDamage()
        {
            if (archetype ==
                    EnemyArchetype.Mech &&
                attackCounter % 2 == 0)
            {
                return attackDamage *
                    1.36f;
            }

            if (archetype ==
                    EnemyArchetype.Armored &&
                attackCounter % 3 == 0)
            {
                return attackDamage *
                    1.24f;
            }

            return attackDamage;
        }

        private IEnumerator AttackRoutine()
        {
            if (attacking)
            {
                yield break;
            }

            attacking = true;
            attackCounter++;

            Color baseColor =
                spriteRenderer != null
                    ? spriteRenderer.color
                    : Color.white;

            Color warningColor =
                friendly
                    ? Color.Lerp(
                        baseColor,
                        new Color(
                            0.82f,
                            0.82f,
                            0.56f,
                            baseColor.a),
                        0.36f)
                    : Color.Lerp(
                        baseColor,
                        new Color(
                            0.88f,
                            0.42f,
                            0.24f,
                            baseColor.a),
                        0.42f);

            const int steps = 3;

            for (int i = 0;
                 i < steps;
                 i++)
            {
                if (visualTransform != null)
                {
                    float scale =
                        i % 2 == 0
                            ? 1.06f
                            : 0.98f;

                    visualTransform.localScale =
                        baseVisualScale *
                        scale;
                }

                if (spriteRenderer != null)
                {
                    spriteRenderer.color =
                        i == steps - 1
                            ? warningColor
                            : baseColor;
                }

                yield return new WaitForSeconds(
                    attackWindup /
                    steps);
            }

            Transform target =
                friendly
                    ? boss != null
                        ? boss.transform
                        : null
                    : player != null
                        ? player.transform
                        : null;

            if (target != null)
            {
                Vector3 delta =
                    target.position -
                    transform.position;

                delta.y = 0f;

                float distance =
                    delta.magnitude;

                if (distance <=
                    attackRange + 0.38f)
                {
                    Vector3 direction =
                        distance > 0.01f
                            ? delta / distance
                            : transform.forward;

                    float damage =
                        CurrentAttackDamage();

                    if (friendly)
                    {
                        if (boss != null)
                        {
                            boss.TakeDamage(
                                damage,
                                direction);
                        }
                    }
                    else if (player != null)
                    {
                        player.TakeDamage(
                            damage,
                            transform.position);
                    }

                    if (retreatImpulse > 0f &&
                        body != null)
                    {
                        body.AddForce(
                            -direction *
                            retreatImpulse,
                            ForceMode.Impulse);
                    }
                }
            }

            if (visualTransform != null)
            {
                visualTransform.localScale =
                    baseVisualScale;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color =
                    baseColor;
            }

            nextAttackTime =
                Time.time +
                attackCooldown;

            attacking = false;
        }

        private IEnumerator SpawnRoutine()
        {
            combatReady = false;

            if (spriteRenderer == null)
            {
                combatReady = true;
                yield break;
            }

            Transform visual =
                spriteRenderer.transform;

            Vector3 baseScale =
                visual.localScale;

            Color targetColor =
                spriteRenderer.color;

            const int steps = 5;
            const float stepTime = 0.055f;

            for (int i = 1;
                 i <= steps;
                 i++)
            {
                float t =
                    i /
                    (float)steps;

                visual.localScale =
                    baseScale *
                    Mathf.Lerp(
                        0.20f,
                        1f,
                        t);

                Color tint =
                    targetColor;

                tint.a = t;
                spriteRenderer.color =
                    tint;

                yield return new WaitForSeconds(
                    stepTime);
            }

            visual.localScale =
                baseScale;

            spriteRenderer.color =
                targetColor;

            combatReady = true;
        }

        private void BuildVisual()
        {
            GameObject spriteObject =
                new GameObject(
                    friendly
                        ? "SparedAllySprite"
                        : "SparedBetrayerSprite",
                    typeof(SpriteRenderer),
                    typeof(PixelBillboard));

            spriteObject.transform.SetParent(
                transform,
                false);

            visualTransform =
                spriteObject.transform;

            spriteObject.transform.localPosition =
                archetype ==
                    EnemyArchetype.Bat
                    ? new Vector3(
                        0f,
                        0.62f,
                        0f)
                    : Vector3.zero;

            float spriteScale =
                archetype == EnemyArchetype.Mech
                    ? 1.35f
                    : archetype == EnemyArchetype.Armored
                        ? 1.32f
                        : archetype == EnemyArchetype.Skeleton
                            ? 1.28f
                            : archetype == EnemyArchetype.Bat
                                ? 1.10f
                                : archetype == EnemyArchetype.Slime
                                    ? 0.92f
                                    : 1.00f;

            spriteObject.transform.localScale =
                Vector3.one *
                spriteScale;

            baseVisualScale =
                spriteObject.transform.localScale;

            spriteRenderer =
                spriteObject.GetComponent<SpriteRenderer>();

            spriteRenderer.sprite =
                PixelSpriteFactory.GetEnemy(
                    archetype,
                    false);

            spriteRenderer.color =
                friendly
                    ? new Color(
                        0.76f,
                        0.88f,
                        0.72f,
                        1f)
                    : new Color(
                        0.88f,
                        0.66f,
                        0.58f,
                        1f);

            spriteRenderer.shadowCastingMode =
                UnityEngine.Rendering
                    .ShadowCastingMode.Off;

            spriteRenderer.receiveShadows =
                false;

            spriteRenderer.sortingOrder =
                friendly
                    ? 5
                    : 3;
        }
    }
}
