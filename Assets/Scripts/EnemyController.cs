using System.Collections;
using UnityEngine;

namespace Depthbound
{
    public class EnemyController : MonoBehaviour
    {
        private static Material wardenSpawnMaterial;
        private static Material stalkerSpawnMaterial;
        private static Material bossSpawnMaterial;
        private static Material bossSlamTelegraphMaterial;
        private static Material wardenTelegraphMaterial;
        private static Material stalkerTelegraphMaterial;
        private static Material bossTelegraphMaterial;
        private static Material sharedHealthBarBackgroundMaterial;
        private static Material sharedHealthBarForegroundMaterial;
        private static Material sharedPixelShadowMaterial;

        [RuntimeInitializeOnLoadMethod(
            RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStaticMaterials()
        {
            wardenSpawnMaterial = null;
            stalkerSpawnMaterial = null;
            bossSpawnMaterial = null;
            bossSlamTelegraphMaterial = null;
            wardenTelegraphMaterial = null;
            stalkerTelegraphMaterial = null;
            bossTelegraphMaterial = null;
            sharedHealthBarBackgroundMaterial = null;
            sharedHealthBarForegroundMaterial = null;
            sharedPixelShadowMaterial = null;
        }

        public int RoomIndex { get; private set; }
        public bool IsBoss { get; private set; }
        public EnemyArchetype Archetype { get; private set; }
        public bool IsAwaitingMercy => awaitingMercy;
        public bool IsEnraged => bossEnraged;

        private int BossActIndex =>
            Mathf.Clamp(
                RoomIndex / 3,
                0,
                2);
        public bool IsDead => dead;

        private int variantIndex;
        private float maxHealth = 55f;
        private float health = 55f;
        public float monsterThickness = 0.5f;
        private float moveSpeed = 2.6f;
        private float damage = 14f;
        private float attackRange = 1.55f;
        private float nextAttackTime;
        private float staggeredUntil = -99f;
        private int attackCounter;
        private int attackSequence;
        private bool attacking;
        private bool spawning = true;
        private bool bossEnraged;
        private bool bossEnragePending;
        private bool bossMechanicControlled;
        private bool awaitingMercy;
        private bool moralityResolved;
        private bool dead;

        private readonly Collider[] steeringBuffer = new Collider[10];
        private readonly RaycastHit[] areaPathBuffer = new RaycastHit[10];

        private Rigidbody body;
        private CapsuleCollider capsule;
        private Transform target;
        private Camera viewCamera;
        private Transform visualRoot;
        private SpriteRenderer pixelSpriteRenderer;
        private Transform attackMarker;
        private Light attackMarkerLight;
        private Transform healthBarRoot;
        private Transform healthBarFill;
        private float healthBarVisibleUntil;
        private Material bodyMaterial;
        private Material armorMaterial;
        private Material bossEyeMaterial;
        private Light bossEyeGlow;
        private Color baseColor;
        private Color armorColor;
        private float visualPhase;
        private float attackPose;
        private float attackPoseTarget;
        private float slamPose;
        private float slamPoseTarget;
        private float hitReact;
        private float hitReactSide;
        private float parryReact;

        private bool UsesSkirmisherPattern =>
            !IsBoss &&
            (Archetype == EnemyArchetype.Mole ||
             Archetype == EnemyArchetype.Bat ||
             Archetype == EnemyArchetype.Skeleton);

        private bool UsesHeavyPattern =>
            !IsBoss &&
            (Archetype == EnemyArchetype.Armored ||
             Archetype == EnemyArchetype.Mech);

        private int HeavyAttackCadence
        {
            get
            {
                if (IsBoss)
                {
                    return 0;
                }

                if (Archetype == EnemyArchetype.Mech)
                {
                    return 2;
                }

                return Archetype == EnemyArchetype.Armored
                    ? 3
                    : 0;
            }
        }

        private float NormalAttackCooldown
        {
            get
            {
                switch (Archetype)
                {
                    case EnemyArchetype.Mole:
                        return 0.95f;
                    case EnemyArchetype.Armored:
                        return 1.55f;
                    case EnemyArchetype.Mech:
                        return 1.75f;
                    case EnemyArchetype.Skeleton:
                        return 1.08f;
                    case EnemyArchetype.Bat:
                        return 0.78f;
                    default:
                        return 1.32f;
                }
            }
        }

        private float NormalTelegraphTime
        {
            get
            {
                switch (Archetype)
                {
                    case EnemyArchetype.Mole:
                        return 0.26f;
                    case EnemyArchetype.Armored:
                        return 0.46f;
                    case EnemyArchetype.Mech:
                        return 0.58f;
                    case EnemyArchetype.Skeleton:
                        return 0.30f;
                    case EnemyArchetype.Bat:
                        return 0.22f;
                    default:
                        return 0.48f;
                }
            }
        }

        private float NormalLungeImpulse
        {
            get
            {
                switch (Archetype)
                {
                    case EnemyArchetype.Mole:
                        return 3.45f;
                    case EnemyArchetype.Armored:
                        return 2.10f;
                    case EnemyArchetype.Mech:
                        return 2.35f;
                    case EnemyArchetype.Skeleton:
                        return 3.05f;
                    case EnemyArchetype.Bat:
                        return 3.70f;
                    default:
                        return 1.85f;
                }
            }
        }

        private float RetreatImpulse
        {
            get
            {
                switch (Archetype)
                {
                    case EnemyArchetype.Mole:
                        return 0.80f;
                    case EnemyArchetype.Skeleton:
                        return 1.45f;
                    case EnemyArchetype.Bat:
                        return 2.20f;
                    default:
                        return 0f;
                }
            }
        }

        private float SkirmisherSideBias
        {
            get
            {
                switch (Archetype)
                {
                    case EnemyArchetype.Mole:
                        return 0.16f;
                    case EnemyArchetype.Bat:
                        return 0.50f;
                    default:
                        return 0.30f;
                }
            }
        }

        private float SkirmisherWeaveStrength
        {
            get
            {
                switch (Archetype)
                {
                    case EnemyArchetype.Mole:
                        return 0.24f;
                    case EnemyArchetype.Bat:
                        return 0.64f;
                    default:
                        return 0.40f;
                }
            }
        }

        private float HeavyDamageMultiplier =>
            Archetype == EnemyArchetype.Mech
                ? 1.50f
                : 1.35f;

        public void Configure(int roomIndex, int variant, bool boss)
        {
            RoomIndex = roomIndex;
            IsBoss = boss;
            variantIndex = variant;
            Archetype = (EnemyArchetype)Mathf.Abs(variant % 6);
            visualPhase = variant * 1.71f + roomIndex * 0.93f;

            if (boss)
            {
                int act = roomIndex / 3;

                switch (act)
                {
                    case 0:
                        maxHealth = 500f;
                        moveSpeed = 2.65f;
                        damage = 17f;
                        attackRange = 1.72f;
                        break;

                    case 1:
                        maxHealth = 1000f;
                        moveSpeed = 1.45f;
                        damage = 26f;
                        attackRange = 2.05f;
                        break;

                    default:
                        maxHealth = 2732f;
                        moveSpeed = 2.45f;
                        damage = 24f;
                        attackRange = 1.82f;
                        break;
                }
            }
            else
            {
                int roomAct =
                    roomIndex / 3;

                int localRoom =
                    roomIndex % 3;

                float roomHealth =
                    roomAct * 18f +
                    localRoom * 18f;

                float roomDamage =
                    roomAct * 3.5f +
                    localRoom * 1.25f;

                switch (Archetype)
                {
                    case EnemyArchetype.Mole:
                        maxHealth = 38f + roomHealth;
                        moveSpeed = 3.35f;
                        damage = 9f + roomDamage;
                        attackRange = 1.25f;
                        break;

                    case EnemyArchetype.Armored:
                        maxHealth = 70f + roomHealth;
                        moveSpeed = 2.20f;
                        damage = 15f + roomDamage;
                        attackRange = 1.70f;
                        break;

                    case EnemyArchetype.Mech:
                        maxHealth = 82f + roomHealth;
                        moveSpeed = 1.95f;
                        damage = 18f + roomDamage;
                        attackRange = 1.75f;
                        break;

                    case EnemyArchetype.Skeleton:
                        maxHealth = 48f + roomHealth;
                        moveSpeed = 2.75f;
                        damage = 12f + roomDamage;
                        attackRange = 1.55f;
                        break;

                    case EnemyArchetype.Bat:
                        maxHealth = 34f + roomHealth;
                        moveSpeed = 3.65f;
                        damage = 9f + roomDamage;
                        attackRange = 1.30f;
                        break;

                    default:
                        maxHealth = 44f + roomHealth;
                        moveSpeed = 2.55f;
                        damage = 10f + roomDamage;
                        attackRange = 1.45f;
                        break;
                }
            }

            health = maxHealth;

            BestiaryCatalog.Discover(
                Archetype,
                IsBoss,
                BossActIndex);
        }

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            if (body == null) body = gameObject.AddComponent<Rigidbody>();
            body.mass = 1.2f;
            body.constraints = RigidbodyConstraints.FreezeRotation;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

            capsule = GetComponent<CapsuleCollider>();
            if (capsule == null) capsule = gameObject.AddComponent<CapsuleCollider>();
            capsule.height = 2f;
            capsule.radius = 0.48f;
            capsule.center = new Vector3(0f, 0.95f, 0f);
        }

        private void Start()
        {
            if (IsBoss)
            {
                body.mass = 2.6f;
                capsule.height = 2.9f;
                capsule.radius = 0.78f;
                capsule.center = new Vector3(0f, 1.42f, 0f);
            }
            else
            {
                switch (Archetype)
                {
                    case EnemyArchetype.Mole:
                    case EnemyArchetype.Slime:
                        body.mass = 0.75f;
                        capsule.height = 1.15f;
                        capsule.radius = 0.46f;
                        capsule.center = new Vector3(0f, 0.52f, 0f);
                        break;

                    case EnemyArchetype.Bat:
                        body.mass = 0.55f;
                        capsule.height = 1.35f;
                        capsule.radius = 0.40f;
                        capsule.center = new Vector3(0f, 0.82f, 0f);
                        break;

                    case EnemyArchetype.Mech:
                        body.mass = 1.7f;
                        capsule.height = 2.15f;
                        capsule.radius = 0.54f;
                        capsule.center = new Vector3(0f, 1.02f, 0f);
                        break;

                    default:
                        body.mass = 1.0f;
                        capsule.height = 1.85f;
                        capsule.radius = 0.43f;
                        capsule.center = new Vector3(0f, 0.88f, 0f);
                        break;
                }
            }

            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) target = player.transform;
            viewCamera = Camera.main;

            BuildVisual();
            UpdateHealthBar();
            StartCoroutine(SpawnRoutine());
        }

        private void Update()
        {
            if (visualRoot != null && !dead && !spawning)
            {
                float dt = Time.deltaTime;
                float speedFactor = body != null
                    ? Mathf.Clamp01(
                        new Vector3(
                            body.linearVelocity.x,
                            0f,
                            body.linearVelocity.z).magnitude / 3f)
                    : 0f;

                attackPose = Mathf.Lerp(
                    attackPose,
                    attackPoseTarget,
                    1f - Mathf.Exp(-13f * dt));

                slamPose = Mathf.Lerp(
                    slamPose,
                    slamPoseTarget,
                    1f - Mathf.Exp(-9f * dt));

                hitReact = Mathf.MoveTowards(hitReact, 0f, dt * 5.8f);
                parryReact = Mathf.MoveTowards(parryReact, 0f, dt * 3.8f);

                float bob =
                    Mathf.Sin(Time.time * 7f + visualPhase) *
                    0.035f *
                    speedFactor;

                float gaitRoll =
                    Mathf.Sin(Time.time * 5.5f + visualPhase) *
                    1.8f *
                    speedFactor;

                float attackPitch = Mathf.Lerp(0f, -13f, attackPose);
                float slamPitch = Mathf.Lerp(0f, 18f, slamPose);
                float hitPitch = hitReact * 15f;
                float parryRoll = parryReact * (IsBoss ? 7f : 13f);
                float crouch =
                    attackPose * (IsBoss ? 0.10f : 0.065f) +
                    slamPose * 0.20f;

                visualRoot.localPosition = new Vector3(
                    0f,
                    bob - crouch + hitReact * 0.035f,
                    attackPose * 0.045f);

                visualRoot.localRotation = Quaternion.Euler(
                    attackPitch + slamPitch + hitPitch,
                    0f,
                    gaitRoll +
                    parryRoll +
                    hitReactSide *
                    hitReact *
                    (IsBoss ? 5f : 11f));

                float squash = 1f - slamPose * 0.055f;
                visualRoot.localScale = new Vector3(
                    1f + slamPose * 0.035f,
                    squash,
                    1f + slamPose * 0.035f);
            }

            if (healthBarRoot != null &&
                healthBarRoot.gameObject.activeSelf &&
                viewCamera != null)
            {
                healthBarRoot.rotation = Quaternion.LookRotation(
                    healthBarRoot.position - viewCamera.transform.position,
                    Vector3.up);

                float remaining = healthBarVisibleUntil - Time.time;

                if (remaining <= 0f)
                {
                    healthBarRoot.gameObject.SetActive(false);
                }
                else
                {
                    float visibility = remaining < 0.24f
                        ? Mathf.Clamp01(remaining / 0.24f)
                        : 1f;

                    healthBarRoot.localScale = Vector3.Lerp(
                        healthBarRoot.localScale,
                        Vector3.one * visibility,
                        1f - Mathf.Exp(-16f * Time.deltaTime));
                }
            }
        }

        private void FixedUpdate()
        {
            if (dead ||
                awaitingMercy ||
                spawning ||
                target == null ||
                attacking ||
                bossMechanicControlled ||
                Time.time < staggeredUntil)
            {
                return;
            }

            Vector3 delta = target.position - transform.position;
            delta.y = 0f;
            float distance = delta.magnitude;

            bool clearAttackPath = HasClearAttackPath();

            if (distance > attackRange || !clearAttackPath)
            {
                Vector3 direction = BuildSteeringDirection(
                    delta.sqrMagnitude > 0.001f
                        ? delta.normalized
                        : transform.forward,
                    distance);
                Vector3 desired = direction * moveSpeed;
                Vector3 velocity = body.linearVelocity;
                body.linearVelocity = new Vector3(desired.x, velocity.y, desired.z);

                if (direction.sqrMagnitude > 0.01f)
                {
                    Quaternion look = Quaternion.LookRotation(direction, Vector3.up);
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        look,
                        1f - Mathf.Exp(-10f * Time.fixedDeltaTime));
                }
            }
            else
            {
                body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
                if (Time.time >= nextAttackTime)
                {
                    StartCoroutine(AttackRoutine());
                }
            }
        }

        private bool HasClearAreaPath(
            Vector3 origin,
            Vector3 targetPoint)
        {
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
                    areaPathBuffer,
                    distance,
                    ~(1 << 2),
                    QueryTriggerInteraction.Ignore);

            for (int i = 0; i < count; i++)
            {
                Collider hit =
                    areaPathBuffer[i].collider;

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

        private bool HasClearAttackPath()
        {
            if (target == null)
            {
                return false;
            }

            Vector3 targetPoint =
                target.position +
                Vector3.up * 0.92f;

            Vector3 flat =
                targetPoint -
                transform.position;

            flat.y = 0f;

            Vector3 forward =
                flat.sqrMagnitude > 0.001f
                    ? flat.normalized
                    : transform.forward;

            float selfClearance =
                capsule != null
                    ? capsule.radius + 0.14f
                    : 0.62f;

            Vector3 origin =
                transform.position +
                Vector3.up * 1.02f +
                forward * selfClearance;

            Vector3 toTarget =
                targetPoint -
                origin;

            float distance =
                toTarget.magnitude;

            if (distance <= 0.05f)
            {
                return true;
            }

            if (!Physics.Raycast(
                    origin,
                    toTarget / distance,
                    out RaycastHit hit,
                    distance,
                    ~(1 << 2),
                    QueryTriggerInteraction.Ignore))
            {
                return true;
            }

            PlayerHealth player =
                hit.collider.GetComponentInParent<PlayerHealth>();

            return player != null &&
                   player.transform == target;
        }

        private Vector3 BuildSteeringDirection(Vector3 towardPlayer, float distance)
        {
            Vector3 steering = towardPlayer;

            int count = Physics.OverlapSphereNonAlloc(
                transform.position,
                1.20f,
                steeringBuffer,
                ~0,
                QueryTriggerInteraction.Ignore);

            Vector3 separation = Vector3.zero;

            for (int i = 0; i < count; i++)
            {
                Collider nearby = steeringBuffer[i];
                if (nearby == null) continue;

                EnemyController other = nearby.GetComponentInParent<EnemyController>();
                if (other == null || other == this || other.dead) continue;

                Vector3 away = transform.position - other.transform.position;
                away.y = 0f;

                float sqrDistance = away.sqrMagnitude;
                if (sqrDistance < 0.001f) continue;

                float strength = 1f - Mathf.Clamp01(Mathf.Sqrt(sqrDistance) / 1.20f);
                separation += away.normalized * strength;
            }

            steering += separation * (IsBoss ? 0.35f : 0.82f);

            if (UsesSkirmisherPattern && distance < 5.2f)
            {
                Vector3 tangent = Vector3.Cross(Vector3.up, towardPlayer);
                float weave =
                    Mathf.Sin(
                        Time.time *
                        (Archetype == EnemyArchetype.Bat
                            ? 3.1f
                            : 2.15f) +
                        visualPhase);

                steering +=
                    tangent *
                    weave *
                    SkirmisherWeaveStrength;
            }

            Vector3 castOrigin = transform.position + Vector3.up * 0.78f;
            if (Physics.SphereCast(
                castOrigin,
                IsBoss ? 0.62f : 0.34f,
                towardPlayer,
                out RaycastHit obstacle,
                IsBoss ? 1.35f : 1.15f,
                ~(1 << 2),
                QueryTriggerInteraction.Ignore))
            {
                EnemyController hitEnemy = obstacle.collider.GetComponentInParent<EnemyController>();

                if (hitEnemy == null)
                {
                    Vector3 right = Vector3.Cross(Vector3.up, towardPlayer).normalized;
                    float side = Vector3.Dot(
                        obstacle.normal,
                        right) >= 0f ? -1f : 1f;

                    steering += right * side * (IsBoss ? 0.70f : 1.20f);
                    steering += obstacle.normal * 0.45f;
                }
            }

            steering.y = 0f;

            if (steering.sqrMagnitude < 0.001f)
            {
                return towardPlayer;
            }

            return steering.normalized;
        }

        public void ApplyParryStagger(Vector3 playerPosition)
        {
            if (dead) return;

            staggeredUntil = Mathf.Max(
                staggeredUntil,
                Time.time + (IsBoss ? 0.22f : 0.48f));

            Vector3 away = transform.position - playerPosition;
            away.y = 0f;

            Vector3 velocity = body.linearVelocity;
            body.linearVelocity = new Vector3(velocity.x * 0.15f, velocity.y, velocity.z * 0.15f);

            parryReact = 1f;
            InterruptAttack();

            if (away.sqrMagnitude > 0.001f)
            {
                body.AddForce(
                    away.normalized * (IsBoss ? 1.4f : 2.8f),
                    ForceMode.Impulse);
            }
        }

        private void InterruptAttack()
        {
            attackSequence++;
            attacking = false;
            attackPoseTarget = 0f;
            slamPoseTarget = 0f;

            if (attackMarker != null)
            {
                attackMarker.gameObject.SetActive(false);
            }

            if (attackMarkerLight != null)
            {
                attackMarkerLight.intensity = 0f;
            }

            RestoreCombatColor();
        }

        public void TakeDamage(
            float amount,
            Vector3 hitDirection,
            float impactStrength = 0.55f)
        {
            if (dead)
            {
                return;
            }

            if (awaitingMercy)
            {
                ResolveMercy(false);
                return;
            }

            float nextHealth = Mathf.Max(0f, health - amount);
            bool entersMercy =
                !IsBoss &&
                RunMorality.Instance != null &&
                nextHealth <= maxHealth * 0.18f;

            health = entersMercy
                ? Mathf.Max(1f, nextHealth)
                : nextHealth;

            if (!IsBoss)
            {
                healthBarVisibleUntil = Time.time + 3.0f;

                if (healthBarRoot != null &&
                    !healthBarRoot.gameObject.activeSelf &&
                    health > 0f)
                {
                    healthBarRoot.localScale = Vector3.one * 0.82f;
                    healthBarRoot.gameObject.SetActive(true);
                }
            }

            impactStrength =
                Mathf.Clamp(
                    impactStrength,
                    0.25f,
                    2f);

            Vector3 flatHit = hitDirection;
            flatHit.y = 0f;

            if (flatHit.sqrMagnitude < 0.001f)
            {
                flatHit =
                    transform.forward;
            }

            flatHit.Normalize();

            float resistance =
                IsBoss
                    ? 1.85f
                    : Archetype == EnemyArchetype.Mech
                        ? 1.45f
                        : Archetype == EnemyArchetype.Armored
                            ? 1.25f
                            : Archetype == EnemyArchetype.Bat
                                ? 0.72f
                                : 1f;

            float knockback =
                (IsBoss ? 1.10f : 2.45f) *
                impactStrength /
                resistance;

            body.AddForce(
                flatHit * knockback,
                ForceMode.Impulse);

            hitReactSide =
                Mathf.Clamp(
                    Vector3.Dot(
                        transform.right,
                        flatHit),
                    -1f,
                    1f);

            float staggerDuration =
                (IsBoss ? 0.025f : 0.055f) +
                impactStrength *
                (IsBoss ? 0.035f : 0.085f);

            staggeredUntil =
                Mathf.Max(
                    staggeredUntil,
                    Time.time +
                    staggerDuration);

            bool interruptsAttack =
                attacking &&
                ((!IsBoss &&
                  impactStrength >= 0.90f) ||
                 (IsBoss &&
                  impactStrength >= 1.65f));

            if (interruptsAttack)
            {
                InterruptAttack();
            }

            UpdateHealthBar();

            if (IsBoss && HUDController.Instance != null)
            {
                HUDController.Instance.SetBossHealth(health, maxHealth);
            }

            if (IsBoss &&
                !bossEnraged &&
                health > 0f &&
                health <= maxHealth * 0.50f)
            {
                if (attacking)
                {
                    bossEnragePending = true;
                }
                else
                {
                    EnterBossEnrage();
                }
            }

            hitReact = 1f;
            StartCoroutine(Flash());

            if (entersMercy)
            {
                EnterMercyState();
                return;
            }

            if (health <= 0f)
            {
                Die();
            }
        }

        private void EnterMercyState()
        {
            if (IsBoss || dead || awaitingMercy)
            {
                return;
            }

            awaitingMercy = true;
            attacking = false;
            attackPoseTarget = 0f;
            slamPoseTarget = 0f;
            staggeredUntil = float.MaxValue;

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            if (attackMarker != null)
            {
                attackMarker.gameObject.SetActive(false);
            }

            if (attackMarkerLight != null)
            {
                attackMarkerLight.intensity = 0f;
            }

            if (healthBarRoot != null)
            {
                healthBarRoot.gameObject.SetActive(false);
            }

            if (capsule != null &&
                target != null)
            {
                Collider playerCollider =
                    target.GetComponent<Collider>();

                if (playerCollider != null)
                {
                    Physics.IgnoreCollision(
                        capsule,
                        playerCollider,
                        true);
                }
            }

            if (visualRoot != null)
            {
                visualRoot.localScale = new Vector3(1.04f, 0.78f, 1.04f);
                visualRoot.localPosition = new Vector3(0f, -0.16f, 0f);
            }

            SetCombatColor(Color.Lerp(baseColor, Color.gray, 0.48f));

            if (RunMorality.Instance != null)
            {
                RunMorality.Instance.RegisterMercyCandidate(this);
            }
        }

        public void ResolveMercy(bool spare)
        {
            if (!awaitingMercy || dead)
            {
                return;
            }

            awaitingMercy = false;
            moralityResolved = true;

            if (RunMorality.Instance != null)
            {
                RunMorality.Instance.UnregisterMercyCandidate(this);

                if (spare)
                {
                    RunMorality.Instance.RecordSpare(Archetype);
                }
                else
                {
                    RunMorality.Instance.RecordKill(Archetype);
                }
            }

            if (!spare)
            {
                health = 0f;
                Die();
                return;
            }

            BestiaryCatalog.RecordSpare(Archetype);
            dead = true;

            if (capsule != null)
            {
                capsule.enabled = false;
            }

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }

            if (DungeonDirector.Instance != null)
            {
                DungeonDirector.Instance.OnEnemyDefeated(RoomIndex);
            }

            StartCoroutine(MercyDepartRoutine());
        }

        private IEnumerator MercyDepartRoutine()
        {
            if (visualRoot == null)
            {
                Destroy(gameObject);
                yield break;
            }

            Vector3 startScale = visualRoot.localScale;
            Vector3 startPosition = visualRoot.localPosition;
            const float duration = 0.34f;

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t * (3f - 2f * t);

                visualRoot.localScale = Vector3.Lerp(
                    startScale,
                    Vector3.one * 0.12f,
                    eased);

                visualRoot.localPosition = Vector3.Lerp(
                    startPosition,
                    startPosition + Vector3.down * 0.42f,
                    eased);

                yield return null;
            }

            Destroy(gameObject);
        }

        private IEnumerator SpawnRoutine()
        {
            if (visualRoot == null)
            {
                spawning = false;
                yield break;
            }

            visualRoot.localScale =
                Vector3.one * 0.18f;

            if (healthBarRoot != null)
            {
                healthBarRoot.gameObject.SetActive(false);
            }

            if (body != null)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.isKinematic = true;
            }

            if (capsule != null)
            {
                capsule.enabled = false;
            }

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.EnemySpawn(
                    transform.position +
                    Vector3.up * 0.4f,
                    IsBoss,
                    UsesSkirmisherPattern);
            }

            GameObject spawnSigil =
                CreatePixelSpawnSigil();

            Color spawnColor =
                IsBoss
                    ? new Color(
                        0.72f,
                        0.22f,
                        0.10f)
                    : new Color(
                        0.50f,
                        0.36f,
                        0.20f);

            float duration =
                IsBoss
                    ? 0.48f
                    : 0.30f;

            for (float t = 0f;
                 t < 1f;
                 t += Time.deltaTime /
                      duration)
            {
                float p =
                    Mathf.Clamp01(t);

                float eased =
                    1f -
                    Mathf.Pow(
                        1f - p,
                        3f);

                float stepped =
                    Mathf.Floor(
                        eased * 8f) /
                    8f;

                visualRoot.localScale =
                    Vector3.one *
                    Mathf.Lerp(
                        0.18f,
                        1f,
                        stepped);

                if (spawnSigil != null)
                {
                    float sigilScale =
                        Mathf.Lerp(
                            0.28f,
                            IsBoss
                                ? 2.2f
                                : 1.15f,
                            stepped);

                    spawnSigil.transform.localScale =
                        new Vector3(
                            sigilScale,
                            1f,
                            sigilScale);

                    int rotationStep =
                        Mathf.FloorToInt(
                            p * 5f);

                    spawnSigil.transform.rotation =
                        Quaternion.Euler(
                            0f,
                            rotationStep *
                            (IsBoss
                                ? 18f
                                : 12f),
                            0f);
                }

                if (bodyMaterial != null)
                {
                    bodyMaterial.color =
                        Color.Lerp(
                            spawnColor,
                            baseColor,
                            stepped);
                }

                if (armorMaterial != null)
                {
                    armorMaterial.color =
                        Color.Lerp(
                            Color.black,
                            armorColor,
                            stepped);
                }

                if (pixelSpriteRenderer != null)
                {
                    Color tint =
                        Color.Lerp(
                            spawnColor,
                            Color.white,
                            stepped);

                    tint.a = stepped;
                    pixelSpriteRenderer.color =
                        tint;
                }

                yield return null;
            }

            visualRoot.localScale =
                Vector3.one;

            if (spawnSigil != null)
            {
                Destroy(spawnSigil);
            }

            RestoreCombatColor();

            if (capsule != null)
            {
                capsule.enabled = true;
            }

            if (body != null)
            {
                body.isKinematic = false;
                body.linearVelocity =
                    Vector3.zero;
                body.angularVelocity =
                    Vector3.zero;
            }

            spawning = false;
            UpdateHealthBar();

            if (IsBoss &&
                HUDController.Instance != null)
            {
                HUDController.Instance.ShowBoss(
                    BossDisplayName(),
                    health,
                    maxHealth);
            }
        }

        private GameObject CreatePixelSpawnSigil()
        {
            GameObject root =
                new GameObject(
                    "PixelSpawnSigil");

            root.transform.position =
                new Vector3(
                    transform.position.x,
                    0.028f,
                    transform.position.z);

            Material material =
                GetSpawnSigilMaterial();

            int blocks =
                IsBoss
                    ? 12
                    : 8;

            float radius =
                IsBoss
                    ? 0.72f
                    : 0.58f;

            for (int i = 0;
                 i < blocks;
                 i++)
            {
                float angle =
                    i /
                    (float)blocks *
                    Mathf.PI *
                    2f;

                GameObject block =
                    GameObject.CreatePrimitive(
                        PrimitiveType.Cube);

                block.name =
                    "SigilBlock_" + i;

                block.transform.SetParent(
                    root.transform,
                    false);

                block.transform.localPosition =
                    new Vector3(
                        Mathf.Cos(angle) * radius,
                        0f,
                        Mathf.Sin(angle) * radius);

                float size =
                    i % 2 == 0
                        ? 0.24f
                        : 0.17f;

                block.transform.localScale =
                    new Vector3(
                        size,
                        0.024f,
                        size);

                Collider collider =
                    block.GetComponent<Collider>();

                if (collider != null)
                {
                    Destroy(collider);
                }

                Renderer renderer =
                    block.GetComponent<Renderer>();

                renderer.sharedMaterial =
                    material;

                renderer.shadowCastingMode =
                    UnityEngine.Rendering
                        .ShadowCastingMode.Off;

                renderer.receiveShadows =
                    false;
            }

            return root;
        }

        public void SetBossMechanicControl(
            bool controlled)
        {
            if (!IsBoss || dead)
            {
                return;
            }

            bossMechanicControlled = controlled;

            if (controlled)
            {
                InterruptAttack();

                if (body != null)
                {
                    Vector3 velocity =
                        body.linearVelocity;

                    body.linearVelocity =
                        new Vector3(
                            0f,
                            Mathf.Min(
                                velocity.y,
                                0f),
                            0f);
                }

                RestoreCombatColor();
            }
        }

        private IEnumerator AttackRoutine()
        {
            if (bossMechanicControlled)
            {
                yield break;
            }

            int sequence =
                ++attackSequence;

            attacking = true;
            attackPoseTarget = 1f;
            attackCounter++;

            int heavyCadence =
                HeavyAttackCadence;

            bool wardenHeavy =
                UsesHeavyPattern &&
                heavyCadence > 0 &&
                attackCounter % heavyCadence == 0;

            if (wardenHeavy)
            {
                attackPoseTarget = 1.28f;

                if (HUDController.Instance != null)
                {
                    HUDController.Instance.ShowCrushingBlowWarning();
                }
            }

            int slamCadence = bossEnraged ? 2 : 3;
            if (IsBoss &&
                BossActIndex == 1 &&
                attackCounter % slamCadence == 0)
            {
                nextAttackTime = Time.time + 2.0f;
                attackPoseTarget = 0f;
                yield return BossSlamRoutine();
                attacking = false;
                ResolvePendingBossEnrage();
                yield break;
            }

            Vector3 committedAttackDirection =
                transform.forward;

            if (target != null)
            {
                Vector3 towardTarget =
                    target.position -
                    transform.position;

                towardTarget.y = 0f;

                if (towardTarget.sqrMagnitude > 0.01f)
                {
                    committedAttackDirection =
                        towardTarget.normalized;

                    if (!IsBoss &&
                        UsesSkirmisherPattern)
                    {
                        Vector3 tangent = Vector3.Cross(
                            Vector3.up,
                            committedAttackDirection);

                        float side =
                            attackCounter % 2 == 0
                                ? 1f
                                : -1f;

                        committedAttackDirection =
                            (committedAttackDirection +
                             tangent *
                             side *
                             SkirmisherSideBias).normalized;
                    }

                    transform.rotation =
                        Quaternion.LookRotation(
                            committedAttackDirection,
                            Vector3.up);
                }
            }

            nextAttackTime =
                Time.time +
                (IsBoss
                    ? BossAttackCooldown()
                    : wardenHeavy
                        ? (Archetype == EnemyArchetype.Mech
                            ? 1.95f
                            : 1.78f)
                        : NormalAttackCooldown);

            SetCombatColor(
                wardenHeavy
                    ? new Color(1f, 0.42f, 0.08f)
                    : new Color(1f, 0.20f, 0.10f));

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.EnemyAttack(
                    transform.position + Vector3.up,
                    IsBoss,
                    UsesSkirmisherPattern,
                    wardenHeavy);
            }

            float telegraph =
                IsBoss
                    ? BossTelegraphTime()
                    : wardenHeavy
                        ? (Archetype == EnemyArchetype.Mech
                            ? 0.76f
                            : 0.68f)
                        : NormalTelegraphTime;

            if (attackMarker != null)
            {
                attackMarker.gameObject.SetActive(true);
                attackMarker.localScale = new Vector3(0.20f, 0.018f, 0.20f);
            }

            for (float elapsed = 0f; elapsed < telegraph; elapsed += Time.deltaTime)
            {
                if (dead ||
                    bossMechanicControlled ||
                    sequence != attackSequence)
                {
                    if (attackMarker != null)
                    {
                        attackMarker.gameObject.SetActive(false);
                    }

                    if (attackMarkerLight != null)
                    {
                        attackMarkerLight.intensity = 0f;
                    }

                    attackPoseTarget = 0f;
                    attacking = false;
                    RestoreCombatColor();
                    yield break;
                }

                float t = Mathf.Clamp01(elapsed / telegraph);
                float eased = t * t * (3f - 2f * t);
                float diameter = Mathf.Lerp(
                    0.20f,
                    attackRange *
                    (IsBoss
                        ? 1.75f
                        : wardenHeavy ? 1.95f : 1.55f),
                    eased);

                if (attackMarker != null)
                {
                    float pulse =
                        1f +
                        Mathf.Sin(t * Mathf.PI * 8f) * 0.035f;

                    bool skirmisherShape =
                        !IsBoss &&
                        UsesSkirmisherPattern;

                    float widthFactor = 1f;
                    float lengthFactor = 1f;

                    if (skirmisherShape)
                    {
                        if (Archetype == EnemyArchetype.Bat)
                        {
                            widthFactor = 0.54f;
                            lengthFactor = 1.46f;
                        }
                        else if (Archetype == EnemyArchetype.Mole)
                        {
                            widthFactor = 0.82f;
                            lengthFactor = 1.16f;
                        }
                        else
                        {
                            widthFactor = 0.70f;
                            lengthFactor = 1.28f;
                        }
                    }

                    float width =
                        diameter * widthFactor;

                    float length =
                        diameter * lengthFactor;

                    attackMarker.localScale = new Vector3(
                        width * pulse,
                        0.018f,
                        length * pulse);
                }

                if (attackMarkerLight != null)
                {
                    attackMarkerLight.intensity = Mathf.Lerp(0.15f, 1.4f, eased);
                }

                yield return null;
            }

            if (attackMarker != null)
            {
                attackMarker.gameObject.SetActive(false);
            }

            if (attackMarkerLight != null)
            {
                attackMarkerLight.intensity = 0f;
            }

            if (!dead &&
                !bossMechanicControlled &&
                sequence == attackSequence)
            {
                float impulse;

                if (IsBoss)
                {
                    impulse = BossLungeImpulse();
                }
                else
                {
                    impulse =
                        wardenHeavy
                            ? (Archetype == EnemyArchetype.Mech
                                ? 2.65f
                                : 2.85f)
                            : NormalLungeImpulse;
                }

                body.AddForce(
                    committedAttackDirection * impulse,
                    ForceMode.Impulse);
            }

            yield return new WaitForSeconds(IsBoss ? 0.10f : 0.07f);

            if (!dead &&
                !bossMechanicControlled &&
                sequence == attackSequence &&
                target != null)
            {
                Vector3 toTarget =
                    target.position -
                    transform.position;

                toTarget.y = 0f;

                float distanceToTarget =
                    toTarget.magnitude;

                float directionalThreshold =
                    IsBoss
                        ? 0.30f
                        : UsesSkirmisherPattern
                            ? 0.58f
                            : wardenHeavy
                                ? 0.38f
                                : 0.46f;

                bool withinArc =
                    distanceToTarget <= 0.05f ||
                    Vector3.Dot(
                        committedAttackDirection,
                        toTarget.normalized) >=
                    directionalThreshold;

                if (distanceToTarget <= attackRange + 0.70f &&
                    withinArc &&
                    HasClearAttackPath())
                {
                    PlayerHealth healthComponent =
                        target.GetComponent<PlayerHealth>();

                    if (healthComponent != null)
                    {
                        healthComponent.TakeDamage(
                            damage *
                            (wardenHeavy
                                ? HeavyDamageMultiplier
                                : 1f),
                            transform.position,
                            this);
                    }
                }
            }

            attackPoseTarget = 0f;
            if (!dead &&
                !IsBoss &&
                UsesSkirmisherPattern &&
                RetreatImpulse > 0f &&
                target != null)
            {
                Vector3 retreat =
                    transform.position -
                    target.position;

                retreat.y = 0f;

                if (retreat.sqrMagnitude > 0.01f)
                {
                    body.AddForce(
                        retreat.normalized *
                        RetreatImpulse,
                        ForceMode.Impulse);
                }
            }

            RestoreCombatColor();
            yield return new WaitForSeconds(IsBoss ? 0.24f : 0.16f);

            if (sequence == attackSequence)
            {
                attacking = false;
                ResolvePendingBossEnrage();
            }
        }

        private float BossAttackCooldown()
        {
            switch (BossActIndex)
            {
                case 0:
                    return bossEnraged
                        ? 0.72f
                        : 0.92f;

                case 1:
                    return bossEnraged
                        ? 1.45f
                        : 1.72f;

                default:
                    return bossEnraged
                        ? 0.82f
                        : 1.02f;
            }
        }

        private float BossTelegraphTime()
        {
            switch (BossActIndex)
            {
                case 0:
                    return bossEnraged
                        ? 0.28f
                        : 0.36f;

                case 1:
                    return bossEnraged
                        ? 0.58f
                        : 0.72f;

                default:
                    return bossEnraged
                        ? 0.32f
                        : 0.42f;
            }
        }

        private float BossLungeImpulse()
        {
            switch (BossActIndex)
            {
                case 0:
                    return bossEnraged
                        ? 5.0f
                        : 4.4f;

                case 1:
                    return bossEnraged
                        ? 3.0f
                        : 2.6f;

                default:
                    return bossEnraged
                        ? 4.8f
                        : 4.2f;
            }
        }

        private void ResolvePendingBossEnrage()
        {
            if (!bossEnragePending ||
                bossEnraged ||
                dead ||
                health <= 0f)
            {
                return;
            }

            bossEnragePending = false;
            EnterBossEnrage();
        }

        private IEnumerator BossSlamRoutine()
        {
            body.linearVelocity = new Vector3(0f, body.linearVelocity.y, 0f);
            slamPoseTarget = 1f;
            SetCombatColor(new Color(0.98f, 0.11f, 0.035f));

            GameObject telegraph = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            telegraph.name = "BruteSlamTelegraph";
            telegraph.transform.position = new Vector3(
                transform.position.x,
                0.035f,
                transform.position.z);
            telegraph.transform.localScale = new Vector3(0.20f, 0.018f, 0.20f);

            Collider telegraphCollider = telegraph.GetComponent<Collider>();
            if (telegraphCollider != null) Destroy(telegraphCollider);

            Renderer telegraphRenderer = telegraph.GetComponent<Renderer>();
            telegraphRenderer.sharedMaterial = GetBossSlamTelegraphMaterial();
            telegraphRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            telegraphRenderer.receiveShadows = false;

            Light warningLight = telegraph.AddComponent<Light>();
            warningLight.type = LightType.Point;
            warningLight.range = 5.2f;
            warningLight.intensity = 0.6f;
            warningLight.color = new Color(1f, 0.08f, 0.02f);

            float radius = bossEnraged ? 3.65f : 3.35f;
            float chargeTime = bossEnraged ? 0.66f : 0.82f;

            for (float elapsed = 0f; elapsed < chargeTime; elapsed += Time.deltaTime)
            {
                if (dead ||
                    bossMechanicControlled)
                {
                    if (telegraph != null)
                    {
                        Destroy(telegraph);
                    }

                    slamPoseTarget = 0f;
                    RestoreCombatColor();
                    yield break;
                }

                float t = Mathf.Clamp01(elapsed / chargeTime);
                float pulse = 1f + Mathf.Sin(t * Mathf.PI * 10f) * 0.05f;
                float scale = Mathf.Lerp(0.20f, radius * 2f, t) * pulse;

                telegraph.transform.localScale = new Vector3(scale, 0.018f, scale);
                warningLight.intensity = Mathf.Lerp(0.6f, 3.0f, t);
                yield return null;
            }

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.BossSlam(transform.position);
            }

            SpawnSlamBurst(radius);

            if (target != null)
            {
                Vector3 flatDelta = target.position - transform.position;
                flatDelta.y = 0f;
                float slamDistance = flatDelta.magnitude;

                FirstPersonController controller = target.GetComponent<FirstPersonController>();
                if (controller != null && slamDistance <= radius + 3.0f)
                {
                    float rumble = Mathf.Lerp(
                        0.42f,
                        0.10f,
                        Mathf.Clamp01(slamDistance / (radius + 3.0f)));

                    controller.AddCameraImpact(rumble, flatDelta.x >= 0f ? 0.25f : -0.25f);
                }

                bool slamPathClear =
                    HasClearAreaPath(
                        transform.position + Vector3.up * 0.38f,
                        target.position + Vector3.up * 0.82f);

                if (slamDistance <= radius &&
                    !slamPathClear &&
                    HUDController.Instance != null)
                {
                    HUDController.Instance.ShowCovered();
                }

                if (slamDistance <= radius &&
                    slamPathClear)
                {
                    PlayerHealth healthComponent = target.GetComponent<PlayerHealth>();
                    if (healthComponent != null)
                    {
                        healthComponent.TakeEnvironmentalDamage(
                            damage * 1.35f,
                            transform.position);
                    }

                    Rigidbody playerBody = target.GetComponent<Rigidbody>();
                    if (playerBody != null && flatDelta.sqrMagnitude > 0.01f)
                    {
                        playerBody.AddForce(
                            flatDelta.normalized * 5.0f + Vector3.up * 1.25f,
                            ForceMode.Impulse);
                    }
                }
            }

            if (telegraph != null) Destroy(telegraph);

            slamPoseTarget = 0f;
            RestoreCombatColor();
            yield return new WaitForSeconds(0.45f);
        }

        private void EnterBossEnrage()
        {
            bossEnragePending = false;
            bossEnraged = true;
            moveSpeed *=
                BossActIndex == 0
                    ? 1.15f
                    : BossActIndex == 1
                        ? 1.08f
                        : 1.26f;

            damage *=
                BossActIndex == 1
                    ? 1.16f
                    : 1.10f;
            nextAttackTime = Mathf.Max(
                nextAttackTime,
                Time.time + 0.82f);

            if (body != null)
            {
                Vector3 velocity = body.linearVelocity;
                body.linearVelocity = new Vector3(
                    velocity.x * 0.22f,
                    Mathf.Min(velocity.y, 0f),
                    velocity.z * 0.22f);
            }

            baseColor = new Color(0.62f, 0.045f, 0.025f);
            armorColor = new Color(0.20f, 0.08f, 0.055f);
            RestoreCombatColor();

            if (bossEyeMaterial != null &&
                bossEyeMaterial.HasProperty("_EmissionColor"))
            {
                bossEyeMaterial.SetColor(
                    "_EmissionColor",
                    new Color(1f, 0.16f, 0.025f) * 4.8f);
            }

            if (bossEyeGlow != null)
            {
                bossEyeGlow.color = new Color(1f, 0.12f, 0.025f);
                bossEyeGlow.range = 5.2f;
                bossEyeGlow.intensity = 2.5f;
            }
            BuildBossEnrageAura();

            CombatEffects.SpawnRuneBurst(
                transform.position + Vector3.up * 0.08f,
                2.4f);

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.BossEnrage();
                ProceduralAudio.Instance.SetBossEnraged();
            }

            if (DungeonDirector.Instance != null)
            {
                DungeonDirector.Instance.OnBossEnraged();
            }

            if (HUDController.Instance != null)
            {
                HUDController.Instance.ShowBoss(
                    BossDisplayName() + " - ENRAGED",
                    health,
                    maxHealth);
                HUDController.Instance.ShowBossPhase();
                HUDController.Instance.SetObjective(
                    "SURVIVE THE ENRAGED WARDEN",
                    new Color(0.72f, 0.30f, 0.16f));
            }
        }

        private void BuildBossEnrageAura()
        {
            if (!IsBoss ||
                visualRoot == null)
            {
                return;
            }

            foreach (Light eyeLight in
                     visualRoot.GetComponentsInChildren<Light>())
            {
                eyeLight.intensity *= 1.18f;
                eyeLight.range *= 1.08f;
                eyeLight.color =
                    new Color(
                        0.82f,
                        0.22f,
                        0.08f);
            }

            GameObject auraObject =
                new GameObject(
                    "PixelEnrageAura",
                    typeof(PixelBossAura));

            auraObject.transform.SetParent(
                visualRoot,
                false);

            auraObject.transform.localPosition =
                Vector3.zero;

            auraObject
                .GetComponent<PixelBossAura>()
                .Configure(
                    new Color(
                        0.68f,
                        0.20f,
                        0.08f));
        }

        private void SpawnSlamBurst(float radius)
        {
            GameObject burst = new GameObject("BruteSlamBurst");
            burst.transform.position = transform.position + Vector3.up * 0.08f;

            ParticleSystem particles = burst.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.18f;
            main.loop = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.30f, 0.52f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(3.2f, 6.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.20f);
            main.startColor = new Color(0.90f, 0.12f, 0.035f);
            main.gravityModifier = 0.35f;
            main.maxParticles = 70;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)48)
            });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius * 0.72f;
            shape.radiusThickness = 1f;

            CombatEffects.ApplyParticleMaterial(
                particles,
                new Color(0.96f, 0.16f, 0.045f));

            particles.Play();
            Destroy(burst, 1.1f);
        }

        private IEnumerator Flash()
        {
            SetCombatColor(
                new Color(
                    1f,
                    0.68f,
                    0.58f,
                    1f));

            yield return new WaitForSeconds(0.07f);

            if (!dead)
            {
                RestoreCombatColor();
            }
        }

        private void BuildVisual()
        {
            visualRoot =
                new GameObject(
                    "VisualRoot").transform;

            visualRoot.SetParent(
                transform,
                false);

            Shader shader =
                Shader.Find(
                    "Universal Render Pipeline/Simple Lit");

            if (shader == null)
            {
                shader =
                    Shader.Find(
                        "Universal Render Pipeline/Lit");
            }

            if (shader == null)
            {
                shader =
                    Shader.Find("Standard");
            }

            if (IsBoss)
            {
                baseColor =
                    new Color(
                        0.46f,
                        0.14f,
                        0.11f);

                armorColor =
                    new Color(
                        0.20f,
                        0.16f,
                        0.16f);
            }
            else
            {
                switch (Archetype)
                {
                    case EnemyArchetype.Mole:
                        baseColor =
                            new Color(
                                0.31f,
                                0.20f,
                                0.14f);
                        break;

                    case EnemyArchetype.Armored:
                        baseColor =
                            new Color(
                                0.40f,
                                0.41f,
                                0.40f);
                        break;

                    case EnemyArchetype.Mech:
                        baseColor =
                            new Color(
                                0.34f,
                                0.26f,
                                0.16f);
                        break;

                    case EnemyArchetype.Skeleton:
                        baseColor =
                            new Color(
                                0.66f,
                                0.63f,
                                0.55f);
                        break;

                    case EnemyArchetype.Bat:
                        baseColor =
                            new Color(
                                0.25f,
                                0.14f,
                                0.22f);
                        break;

                    default:
                        baseColor =
                            new Color(
                                0.30f,
                                0.48f,
                                0.25f);
                        break;
                }

                armorColor =
                    Color.Lerp(
                        baseColor,
                        Color.black,
                        0.30f);
            }

            GameObject spriteObject =
                new GameObject(
                    "PixelSprite",
                    typeof(SpriteRenderer),
                    typeof(PixelBillboard));

            spriteObject.transform.SetParent(
                visualRoot,
                false);

            spriteObject.transform.localPosition =
                Archetype == EnemyArchetype.Bat &&
                !IsBoss
                    ? new Vector3(
                        0f,
                        0.62f,
                        0f)
                    : Vector3.zero;

            float spriteScale =
                IsBoss
                    ? 1.15f
                    : Archetype == EnemyArchetype.Mech
                        ? 1.35f
                        : Archetype == EnemyArchetype.Armored
                            ? 1.32f
                            : Archetype == EnemyArchetype.Skeleton
                                ? 1.28f
                                : Archetype == EnemyArchetype.Bat
                                    ? 1.10f
                                    : Archetype == EnemyArchetype.Slime
                                        ? 0.92f
                                        : 1.00f;

            spriteObject.transform.localScale =
                Vector3.one * spriteScale;

            pixelSpriteRenderer =
                spriteObject.GetComponent<SpriteRenderer>();

            pixelSpriteRenderer.sprite =
                PixelSpriteFactory.GetEnemy(
                    Archetype,
                    IsBoss);

            pixelSpriteRenderer.color =
                Color.white;

            pixelSpriteRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;

            pixelSpriteRenderer.receiveShadows =
                false;

            pixelSpriteRenderer.sortingOrder =
                IsBoss ? 4 : 2;

            AddMonsterThickness(spriteObject);

            BuildPixelShadow();

            BuildAttackMarker(shader);

            if (!IsBoss)
            {
                BuildHealthBar(shader);
            }
        }


        private void AddMonsterThickness(GameObject spriteObject)
        {
            int layers = 5;

            for (int i = 1; i <= layers; i++)
            {
                GameObject layer = new GameObject("DepthLayer_" + i);

                layer.transform.SetParent(
                    spriteObject.transform, false);

                float depth = monsterThickness * i / layers;
                layer.transform.localPosition =
                    new Vector3(0f, 0f, -depth);

                layer.transform.localScale = Vector3.one;

                SpriteRenderer render =
                    layer.AddComponent<SpriteRenderer>();

                render.sprite = pixelSpriteRenderer.sprite;

                render.color = new Color(0.25f, 0.22f, 0.20f, 1f);

                render.sortingOrder =
                    pixelSpriteRenderer.sortingOrder - 1;
            }
        }


        private void BuildPixelShadow()
        {
            GameObject shadow =
                GameObject.CreatePrimitive(
                    PrimitiveType.Cylinder);

            shadow.name = "PixelGroundShadow";
            shadow.transform.SetParent(
                transform,
                false);

            shadow.transform.localPosition =
                new Vector3(
                    0f,
                    0.025f,
                    0f);

            float size =
                IsBoss
                    ? 1.85f
                    : Archetype == EnemyArchetype.Mech
                        ? 1.20f
                        : Archetype == EnemyArchetype.Armored
                            ? 1.08f
                            : Archetype == EnemyArchetype.Bat
                                ? 0.82f
                                : Archetype == EnemyArchetype.Slime
                                    ? 0.78f
                                    : 0.94f;

            shadow.transform.localScale =
                new Vector3(
                    size,
                    0.015f,
                    size * 0.58f);

            Collider collider =
                shadow.GetComponent<Collider>();

            if (collider != null)
            {
                Destroy(collider);
            }

            Renderer renderer =
                shadow.GetComponent<Renderer>();

            renderer.sharedMaterial =
                GetPixelShadowMaterial();

            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;

            renderer.receiveShadows = false;
        }

        private static Material GetPixelShadowMaterial()
        {
            if (sharedPixelShadowMaterial != null)
            {
                return sharedPixelShadowMaterial;
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

            sharedPixelShadowMaterial =
                new Material(shader);

            sharedPixelShadowMaterial.name =
                "RuntimePixelGroundShadow";

            Color color =
                new Color(
                    0.035f,
                    0.038f,
                    0.030f,
                    1f);

            sharedPixelShadowMaterial.color = color;

            if (sharedPixelShadowMaterial.HasProperty(
                "_BaseColor"))
            {
                sharedPixelShadowMaterial.SetColor(
                    "_BaseColor",
                    color);
            }

            return sharedPixelShadowMaterial;
        }

        private void BuildWardenVisual(Shader shader)
        {
            CreatePart(PrimitiveType.Capsule, "Torso", new Vector3(0f, 1.02f, 0f),
                new Vector3(0.72f, 0.88f, 0.72f), bodyMaterial);
            CreatePart(PrimitiveType.Sphere, "Head", new Vector3(0f, 1.88f, 0f),
                new Vector3(0.48f, 0.48f, 0.48f), bodyMaterial);
            CreatePart(PrimitiveType.Cube, "ChestPlate", new Vector3(0f, 1.22f, -0.31f),
                new Vector3(0.76f, 0.50f, 0.14f), armorMaterial);
            CreatePart(PrimitiveType.Cube, "ShoulderL", new Vector3(-0.48f, 1.36f, 0f),
                new Vector3(0.28f, 0.22f, 0.42f), armorMaterial);
            CreatePart(PrimitiveType.Cube, "ShoulderR", new Vector3(0.48f, 1.36f, 0f),
                new Vector3(0.28f, 0.22f, 0.42f), armorMaterial);

            CreatePart(PrimitiveType.Cube, "ArmL", new Vector3(-0.48f, 0.98f, -0.03f),
                new Vector3(0.24f, 0.62f, 0.24f), bodyMaterial);
            CreatePart(PrimitiveType.Cube, "ArmR", new Vector3(0.48f, 0.98f, -0.03f),
                new Vector3(0.24f, 0.62f, 0.24f), bodyMaterial);

            Transform handle = CreatePart(
                PrimitiveType.Cube,
                "MaceHandle",
                new Vector3(0.58f, 0.66f, -0.24f),
                new Vector3(0.10f, 0.78f, 0.10f),
                armorMaterial);
            handle.localRotation = Quaternion.Euler(18f, 0f, -8f);

            CreatePart(PrimitiveType.Cube, "MaceHead", new Vector3(0.66f, 0.33f, -0.30f),
                new Vector3(0.34f, 0.30f, 0.34f), armorMaterial);

            CreateEye(shader, new Vector3(0f, 1.92f, -0.43f), new Color(1f, 0.26f, 0.12f), 0.12f);
        }

        private void BuildStalkerVisual(Shader shader)
        {
            CreatePart(PrimitiveType.Capsule, "Torso", new Vector3(0f, 0.91f, 0f),
                new Vector3(0.56f, 0.78f, 0.56f), bodyMaterial);
            CreatePart(PrimitiveType.Sphere, "Head", new Vector3(0f, 1.62f, -0.03f),
                new Vector3(0.40f, 0.38f, 0.44f), bodyMaterial);

            Transform hornL = CreatePart(PrimitiveType.Cube, "HornL", new Vector3(-0.22f, 1.92f, 0.05f),
                new Vector3(0.10f, 0.34f, 0.10f), armorMaterial);
            hornL.localRotation = Quaternion.Euler(0f, 0f, -18f);
            Transform hornR = CreatePart(PrimitiveType.Cube, "HornR", new Vector3(0.22f, 1.92f, 0.05f),
                new Vector3(0.10f, 0.34f, 0.10f), armorMaterial);
            hornR.localRotation = Quaternion.Euler(0f, 0f, 18f);

            Transform clawL = CreatePart(
                PrimitiveType.Cube,
                "ClawL",
                new Vector3(-0.28f, 0.86f, -0.36f),
                new Vector3(0.08f, 0.50f, 0.08f),
                armorMaterial);
            clawL.localRotation = Quaternion.Euler(32f, 0f, -18f);

            Transform clawR = CreatePart(
                PrimitiveType.Cube,
                "ClawR",
                new Vector3(0.28f, 0.86f, -0.36f),
                new Vector3(0.08f, 0.50f, 0.08f),
                armorMaterial);
            clawR.localRotation = Quaternion.Euler(32f, 0f, 18f);

            CreateEye(shader, new Vector3(-0.10f, 1.66f, -0.38f), new Color(0.72f, 0.22f, 1f), 0.075f);
            CreateEye(shader, new Vector3(0.10f, 1.66f, -0.38f), new Color(0.72f, 0.22f, 1f), 0.075f);
        }

        private void BuildBossVisual(Shader shader)
        {
            CreatePart(PrimitiveType.Cube, "Torso", new Vector3(0f, 1.28f, 0f),
                new Vector3(1.35f, 1.72f, 0.92f), bodyMaterial);
            CreatePart(PrimitiveType.Sphere, "Head", new Vector3(0f, 2.42f, -0.08f),
                new Vector3(0.78f, 0.70f, 0.76f), bodyMaterial);
            CreatePart(PrimitiveType.Cube, "ChestArmor", new Vector3(0f, 1.55f, -0.53f),
                new Vector3(1.48f, 0.72f, 0.18f), armorMaterial);
            CreatePart(PrimitiveType.Cube, "ShoulderL", new Vector3(-0.88f, 1.82f, 0f),
                new Vector3(0.55f, 0.42f, 0.72f), armorMaterial);
            CreatePart(PrimitiveType.Cube, "ShoulderR", new Vector3(0.88f, 1.82f, 0f),
                new Vector3(0.55f, 0.42f, 0.72f), armorMaterial);
            CreatePart(PrimitiveType.Cube, "GauntletL", new Vector3(-0.86f, 0.88f, -0.08f),
                new Vector3(0.48f, 0.68f, 0.48f), armorMaterial);
            CreatePart(PrimitiveType.Cube, "GauntletR", new Vector3(0.86f, 0.88f, -0.08f),
                new Vector3(0.48f, 0.68f, 0.48f), armorMaterial);

            Transform spikeL = CreatePart(
                PrimitiveType.Cube,
                "GauntletSpikeL",
                new Vector3(-0.88f, 0.74f, -0.45f),
                new Vector3(0.14f, 0.16f, 0.55f),
                armorMaterial);
            spikeL.localRotation = Quaternion.Euler(0f, -8f, 0f);

            Transform spikeR = CreatePart(
                PrimitiveType.Cube,
                "GauntletSpikeR",
                new Vector3(0.88f, 0.74f, -0.45f),
                new Vector3(0.14f, 0.16f, 0.55f),
                armorMaterial);
            spikeR.localRotation = Quaternion.Euler(0f, 8f, 0f);

            CreateEye(shader, new Vector3(0f, 2.46f, -0.67f), new Color(1f, 0.55f, 0.08f), 0.18f);
        }

        private void BuildAttackMarker(Shader shader)
        {
            GameObject marker = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            marker.name = "AttackTelegraph";
            marker.transform.SetParent(transform, false);
            marker.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            marker.transform.localScale = new Vector3(0.20f, 0.018f, 0.20f);

            Collider collider = marker.GetComponent<Collider>();
            if (collider != null)
            {
                Destroy(collider);
            }

            Color markerColor = IsBoss
                ? new Color(0.70f, 0.14f, 0.07f)
                : UsesSkirmisherPattern
                    ? new Color(0.60f, 0.34f, 0.15f)
                    : new Color(0.66f, 0.27f, 0.10f);

            Renderer markerRenderer = marker.GetComponent<Renderer>();
            markerRenderer.sharedMaterial =
                GetAttackTelegraphMaterial(shader, markerColor);
            markerRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            markerRenderer.receiveShadows = false;
            attackMarker = marker.transform;

            GameObject lightObject = new GameObject("AttackTelegraphLight", typeof(Light));
            lightObject.transform.SetParent(transform, false);
            lightObject.transform.localPosition = new Vector3(0f, 0.28f, 0f);

            attackMarkerLight = lightObject.GetComponent<Light>();
            attackMarkerLight.type = LightType.Point;
            attackMarkerLight.renderMode = LightRenderMode.ForceVertex;
            attackMarkerLight.range = IsBoss ? 4.2f : 2.5f;
            attackMarkerLight.intensity = 0f;
            attackMarkerLight.color = markerColor;

            marker.SetActive(false);
        }

        private void BuildHealthBar(Shader shader)
        {
            healthBarRoot = new GameObject("HealthBar").transform;
            healthBarRoot.SetParent(transform, false);
            healthBarRoot.localPosition = new Vector3(0f, IsBoss ? 3.18f : 2.42f, 0f);

            Material bg = GetHealthBarBackgroundMaterial(shader);
            Material fg = GetHealthBarForegroundMaterial(shader);

            Transform back = CreateBarPart("BarBack", new Vector3(0f, 0f, 0f),
                new Vector3(IsBoss ? 2.0f : 1.35f, 0.10f, 0.055f), bg);
            back.SetParent(healthBarRoot, false);

            healthBarFill = CreateBarPart("BarFill", new Vector3(0f, 0f, -0.035f),
                new Vector3(IsBoss ? 1.92f : 1.29f, 0.065f, 0.035f), fg);
            healthBarFill.SetParent(healthBarRoot, false);
        }

        private Transform CreateBarPart(string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = name;
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            Collider col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);

            Renderer renderer = go.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            return go.transform;
        }

        private Transform CreatePart(PrimitiveType type, string name, Vector3 position, Vector3 scale, Material material)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(visualRoot, false);
            go.transform.localPosition = position;
            go.transform.localScale = scale;
            Collider col = go.GetComponent<Collider>();
            if (col != null) Destroy(col);
            go.GetComponent<Renderer>().sharedMaterial = material;
            return go.transform;
        }

        private void CreateEye(Shader shader, Vector3 position, Color color, float size)
        {
            Material eyeMaterial = CreateMaterial(shader, color, 0f, 0.35f);
            if (eyeMaterial.HasProperty("_EmissionColor"))
            {
                eyeMaterial.EnableKeyword("_EMISSION");
                eyeMaterial.SetColor("_EmissionColor", color * 3f);
            }

            Transform eye = CreatePart(
                PrimitiveType.Sphere,
                "Eye",
                position,
                Vector3.one * size,
                eyeMaterial);

            Light glow = eye.gameObject.AddComponent<Light>();
            glow.type = LightType.Point;
            glow.renderMode = LightRenderMode.ForceVertex;
            glow.range = IsBoss ? 3.5f : 1.8f;
            glow.intensity = IsBoss ? 1.3f : 0.35f;
            glow.color = color;

            if (IsBoss)
            {
                bossEyeMaterial = eyeMaterial;
                bossEyeGlow = glow;
            }
        }

        private Material GetAttackTelegraphMaterial(
            Shader shader,
            Color markerColor)
        {
            Material cached = IsBoss
                ? bossTelegraphMaterial
                : UsesSkirmisherPattern
                    ? stalkerTelegraphMaterial
                    : wardenTelegraphMaterial;

            if (cached != null)
            {
                return cached;
            }

            Material material = CreateMaterial(
                shader,
                markerColor,
                0f,
                0.30f);

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor(
                    "_EmissionColor",
                    markerColor * 2.2f);
            }

            material.name = IsBoss
                ? "RuntimeBossAttackTelegraph"
                : UsesSkirmisherPattern
                    ? "RuntimeStalkerAttackTelegraph"
                    : "RuntimeWardenAttackTelegraph";

            if (IsBoss)
            {
                bossTelegraphMaterial = material;
            }
            else if (UsesSkirmisherPattern)
            {
                stalkerTelegraphMaterial = material;
            }
            else
            {
                wardenTelegraphMaterial = material;
            }

            return material;
        }

        private static Material GetHealthBarBackgroundMaterial(
            Shader shader)
        {
            if (sharedHealthBarBackgroundMaterial == null)
            {
                sharedHealthBarBackgroundMaterial = CreateMaterial(
                    shader,
                    new Color(0.025f, 0.03f, 0.04f),
                    0f,
                    0f);

                sharedHealthBarBackgroundMaterial.name =
                    "RuntimeEnemyHealthBarBackground";
            }

            return sharedHealthBarBackgroundMaterial;
        }

        private static Material GetHealthBarForegroundMaterial(
            Shader shader)
        {
            if (sharedHealthBarForegroundMaterial == null)
            {
                sharedHealthBarForegroundMaterial = CreateMaterial(
                    shader,
                    new Color(0.72f, 0.10f, 0.12f),
                    0f,
                    0.10f);

                sharedHealthBarForegroundMaterial.name =
                    "RuntimeEnemyHealthBarForeground";
            }

            return sharedHealthBarForegroundMaterial;
        }

        private static Material GetBossSlamTelegraphMaterial()
        {
            if (bossSlamTelegraphMaterial != null)
            {
                return bossSlamTelegraphMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            bossSlamTelegraphMaterial = new Material(shader);
            bossSlamTelegraphMaterial.name = "RuntimeBossSlamTelegraph";
            bossSlamTelegraphMaterial.color = new Color(
                0.62f,
                0.025f,
                0.015f);

            if (bossSlamTelegraphMaterial.HasProperty("_EmissionColor"))
            {
                bossSlamTelegraphMaterial.EnableKeyword("_EMISSION");
                bossSlamTelegraphMaterial.SetColor(
                    "_EmissionColor",
                    new Color(1.0f, 0.025f, 0.005f) * 2.4f);
            }

            return bossSlamTelegraphMaterial;
        }

        private Material GetSpawnSigilMaterial()
        {
            if (IsBoss && bossSpawnMaterial != null)
            {
                return bossSpawnMaterial;
            }

            if (UsesSkirmisherPattern && stalkerSpawnMaterial != null)
            {
                return stalkerSpawnMaterial;
            }

            if (!IsBoss && !UsesSkirmisherPattern && wardenSpawnMaterial != null)
            {
                return wardenSpawnMaterial;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            Color sigilColor = IsBoss
                ? new Color(0.68f, 0.13f, 0.06f)
                : UsesSkirmisherPattern
                    ? new Color(0.46f, 0.34f, 0.17f)
                    : new Color(0.58f, 0.25f, 0.10f);

            Material material = new Material(shader);
            material.name = IsBoss
                ? "RuntimeBossSpawnSigil"
                : UsesSkirmisherPattern
                    ? "RuntimeStalkerSpawnSigil"
                    : "RuntimeWardenSpawnSigil";

            material.color = sigilColor;

            if (material.HasProperty("_EmissionColor"))
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor(
                    "_EmissionColor",
                    sigilColor * 2.2f);
            }

            if (IsBoss)
            {
                bossSpawnMaterial = material;
            }
            else if (UsesSkirmisherPattern)
            {
                stalkerSpawnMaterial = material;
            }
            else
            {
                wardenSpawnMaterial = material;
            }

            return material;
        }

        private static Material CreateMaterial(Shader shader, Color color, float metallic, float smoothness)
        {
            Material material = new Material(shader);
            material.color = color;
            if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
            if (material.HasProperty("_Glossiness")) material.SetFloat("_Glossiness", smoothness);
            if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
            return material;
        }

        private void UpdateHealthBar()
        {
            if (healthBarFill == null) return;

            float ratio = maxHealth <= 0f ? 0f : Mathf.Clamp01(health / maxHealth);
            float fullWidth = IsBoss ? 1.92f : 1.29f;
            Vector3 scale = healthBarFill.localScale;
            scale.x = fullWidth * ratio;
            healthBarFill.localScale = scale;

            Vector3 pos = healthBarFill.localPosition;
            pos.x = -fullWidth * (1f - ratio) * 0.5f;
            healthBarFill.localPosition = pos;

            if (healthBarRoot != null)
            {
                bool shouldShow =
                    ratio < 0.999f &&
                    ratio > 0f &&
                    Time.time < healthBarVisibleUntil;

                healthBarRoot.gameObject.SetActive(shouldShow);
            }
        }

        private void SetCombatColor(Color color)
        {
            if (bodyMaterial != null)
            {
                bodyMaterial.color = color;
            }

            if (armorMaterial != null)
            {
                armorMaterial.color =
                    Color.Lerp(
                        color,
                        Color.black,
                        0.36f);
            }

            if (pixelSpriteRenderer != null)
            {
                pixelSpriteRenderer.color = color;
            }
        }

        private void RestoreCombatColor()
        {
            if (bodyMaterial != null)
            {
                bodyMaterial.color = baseColor;
            }

            if (armorMaterial != null)
            {
                armorMaterial.color = armorColor;
            }

            if (pixelSpriteRenderer != null)
            {
                pixelSpriteRenderer.color =
                    IsBoss && bossEnraged
                        ? new Color(
                            1f,
                            0.76f,
                            0.68f,
                            1f)
                        : Color.white;
            }
        }

        private string BossDisplayName()
        {
            int act = RoomIndex / 3;

            switch (act)
            {
                case 0:
                    return "BURROW WARDEN";
                case 1:
                    return "IRON WARDEN";
                default:
                    return "ASHEN WARDEN";
            }
        }

        private void Die()
        {
            if (dead) return;
            dead = true;

            BestiaryCatalog.RecordDefeat(
                Archetype,
                IsBoss,
                BossActIndex);

            if (!IsBoss && !moralityResolved && RunMorality.Instance != null)
            {
                moralityResolved = true;
                RunMorality.Instance.UnregisterMercyCandidate(this);
                RunMorality.Instance.RecordKill(Archetype);
            }

            attackPoseTarget = 0f;
            slamPoseTarget = 0f;

            if (healthBarRoot != null)
            {
                healthBarRoot.gameObject.SetActive(false);
            }

            if (capsule != null)
            {
                capsule.enabled = false;
            }

            if (attackMarker != null)
            {
                attackMarker.gameObject.SetActive(false);
            }

            if (attackMarkerLight != null)
            {
                attackMarkerLight.intensity = 0f;
            }

            if (body != null)
            {
                body.linearVelocity *= 0.18f;
                body.isKinematic = true;
            }

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.EnemyDeath(
                    transform.position + Vector3.up,
                    IsBoss);
            }

            if (IsBoss && HUDController.Instance != null)
            {
                HUDController.Instance.HideBoss();
            }

            SpawnDeathBurst();
            SpawnSoulShards();

            if (DungeonDirector.Instance != null)
            {
                DungeonDirector.Instance.OnEnemyDefeated(RoomIndex);
            }

            StartCoroutine(DeathRoutine());
        }

        private IEnumerator DeathRoutine()
        {
            if (visualRoot == null)
            {
                Destroy(gameObject);
                yield break;
            }

            Vector3 startScale = visualRoot.localScale;
            Vector3 startPosition = visualRoot.localPosition;
            Quaternion startRotation = visualRoot.localRotation;

            float duration = IsBoss ? 0.42f : 0.24f;
            float fallDirection =
                ((int)Archetype % 2 == 0)
                    ? 1f
                    : -1f;

            for (float elapsed = 0f; elapsed < duration; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);

                visualRoot.localScale = Vector3.Lerp(
                    startScale,
                    startScale * (IsBoss ? 0.42f : 0.12f),
                    eased);

                visualRoot.localPosition = Vector3.Lerp(
                    startPosition,
                    startPosition + new Vector3(
                        fallDirection * (IsBoss ? 0.22f : 0.12f),
                        IsBoss ? -0.22f : -0.38f,
                        0.05f),
                    eased);

                visualRoot.localRotation = Quaternion.Slerp(
                    startRotation,
                    Quaternion.Euler(
                        IsBoss ? 18f : 34f,
                        0f,
                        fallDirection * (IsBoss ? 18f : 42f)),
                    eased);

                yield return null;
            }

            Destroy(gameObject);
        }

        private void SpawnSoulShards()
        {
            int count = IsBoss ? 3 : 1;
            float healPerShard = IsBoss ? 5f : 4f;

            for (int i = 0; i < count; i++)
            {
                float angle = (Mathf.PI * 2f * i / count) + Random.Range(-0.35f, 0.35f);
                Vector3 launch = new Vector3(
                    Mathf.Cos(angle),
                    0f,
                    Mathf.Sin(angle));

                SoulShard.Spawn(
                    transform.position + Vector3.up * (IsBoss ? 1.4f : 0.9f),
                    healPerShard,
                    launch);
            }
        }

        private void SpawnDeathBurst()
        {
            GameObject burst = new GameObject("EnemyDeathBurst");
            burst.transform.position = transform.position + Vector3.up;
            ParticleSystem particles = burst.AddComponent<ParticleSystem>();
            ParticleSystem.MainModule main = particles.main;
            main.duration = 0.28f;
            main.startLifetime = IsBoss ? 0.75f : 0.48f;
            main.startSpeed = IsBoss ? 5.4f : 3.2f;
            main.startSize = IsBoss ? 0.24f : 0.14f;
            main.startColor = baseColor;
            main.loop = false;
            main.gravityModifier = 0.55f;

            ParticleSystem.EmissionModule emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)(IsBoss ? 38 : 16)) });

            ParticleSystem.ShapeModule shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = IsBoss ? 0.9f : 0.45f;

            CombatEffects.ApplyParticleMaterial(
                particles,
                IsBoss
                    ? new Color(1f, 0.22f, 0.055f)
                    : baseColor);

            particles.Play();
            Destroy(burst, 1.5f);
        }
    }
}

