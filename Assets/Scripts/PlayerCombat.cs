using System.Collections;
using UnityEngine;

namespace Depthbound
{
    public enum WeaponMode
    {
        Dagger,
        SwordShield
    }

    public class PlayerCombat : MonoBehaviour
    {
        public WeaponMode Mode { get; private set; } = WeaponMode.Dagger;
        public bool IsBlocking { get; private set; }
        public bool LastBlockWasPerfect { get; private set; }
        public float DamageMultiplier { get; private set; } = 1f;
        public int MomentumCount { get; private set; }
        public float MomentumBonus => Mathf.Min(Mathf.Max(0, MomentumCount - 1) * 0.035f, 0.175f);
        public float MomentumWindow01 =>
            MomentumCount <= 0 || momentumWindowDuration <= 0f
                ? 0f
                : Mathf.Clamp01((momentumExpires - Time.time) / momentumWindowDuration);
        public float Guard01 => Mathf.Clamp01(currentGuard / MaxGuard);

        private const float MaxGuard = 100f;
        private const float GuardRegenPerSecond = 32f;

        private Camera viewCamera;
        private Transform weaponRig;
        private Transform daggerRoot;
        private Transform swordShieldRoot;
        private TrailRenderer daggerTrail;
        private TrailRenderer swordTrail;
        private Vector3 swordNormalPosition;
        private Vector3 swordBlockPosition;
        private Quaternion swordNormalRotation = Quaternion.identity;
        private float nextAttackTime;
        private float currentGuard = MaxGuard;
        private float lastGuardHitTime = -99f;
        private float guardBrokenUntil = -99f;
        private float blockStartedTime = -99f;
        private float shieldImpact;
        private float momentumExpires = -99f;
        private float momentumWindowDuration = 1f;
        private bool swinging;
        private bool switchingWeapon;
        private bool queuedAttack;
        private bool daggerSwingLeft;
        private int comboStep = -1;
        private WeaponMode comboMode = WeaponMode.Dagger;
        private float comboExpires = -99f;
        private float riposteReadyUntil = -99f;
        private bool swordAlternateSwing;
        private float wallAvoidance;
        private float switchOffset;
        private float weaponImpactKick;
        private bool lastHitEmphasized;
        private bool lastHitKilled;
        private Vector2 sway;

        private void Start()
        {
            viewCamera = Camera.main != null ? Camera.main : GetComponentInChildren<Camera>();
            BuildViewModels();
            RefreshModeVisuals();
            PushWeaponHUD();
            PushGuardHUD();
        }

        private void Update()
        {
            if (HUDController.Instance != null &&
                HUDController.Instance.IsModalOpen)
            {
                IsBlocking = false;
                return;
            }

            UpdateGuard();
            UpdateMomentum();

            if (Input.GetKeyDown(KeyCode.Q) &&
                !swinging &&
                !switchingWeapon)
            {
                StartCoroutine(SwitchWeaponRoutine());
            }

            bool wantsBlock =
                Mode == WeaponMode.SwordShield &&
                Input.GetMouseButton(1) &&
                !swinging &&
                !switchingWeapon &&
                Time.time >= guardBrokenUntil &&
                currentGuard > 0.5f;

            if (wantsBlock && !IsBlocking)
            {
                blockStartedTime = Time.time;
            }

            IsBlocking = wantsBlock;

            shieldImpact = Mathf.MoveTowards(
                shieldImpact,
                0f,
                Time.deltaTime * 5.8f);

            if (swordShieldRoot != null &&
                Mode == WeaponMode.SwordShield &&
                !swinging)
            {
                bool guardBroken = Time.time < guardBrokenUntil;

                Vector3 targetPosition = guardBroken
                    ? swordNormalPosition + new Vector3(0.05f, -0.22f, -0.05f)
                    : IsBlocking
                        ? swordBlockPosition
                        : swordNormalPosition;

                if (IsBlocking)
                {
                    targetPosition += Vector3.forward * shieldImpact * 0.085f;
                }

                Quaternion targetRotation;
                if (guardBroken)
                {
                    targetRotation = Quaternion.Euler(18f, -9f, -16f);
                }
                else if (IsBlocking)
                {
                    targetRotation = Quaternion.Euler(
                        -7f - shieldImpact * 4f,
                        8f,
                        4f + shieldImpact * 3f);
                }
                else
                {
                    targetRotation = swordNormalRotation;
                }

                swordShieldRoot.localPosition = Vector3.Lerp(
                    swordShieldRoot.localPosition,
                    targetPosition,
                    1f - Mathf.Exp(-14f * Time.deltaTime));

                swordShieldRoot.localRotation = Quaternion.Slerp(
                    swordShieldRoot.localRotation,
                    targetRotation,
                    1f - Mathf.Exp(-15f * Time.deltaTime));
            }

            UpdateWeaponSway();
            UpdateAimFeedback();

            if (Input.GetMouseButton(0) && !IsBlocking && !switchingWeapon)
            {
                if (swinging)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        queuedAttack = true;
                    }
                }
                else
                {
                    TryAttack();
                }
            }
        }

        public bool TryBlock(Vector3 worldSource, float incomingDamage)
        {
            LastBlockWasPerfect = false;
            if (!IsBlocking) return false;

            Vector3 towardSource = worldSource - transform.position;
            towardSource.y = 0f;

            bool inFront =
                towardSource.sqrMagnitude < 0.001f ||
                Vector3.Dot(transform.forward, towardSource.normalized) > 0.15f;

            if (!inFront) return false;

            LastBlockWasPerfect = Time.time - blockStartedTime <= 0.18f;
            shieldImpact = LastBlockWasPerfect ? 1.35f : 0.82f;

            float guardCost = Mathf.Max(22f, incomingDamage * 1.35f);
            if (LastBlockWasPerfect)
            {
                guardCost *= 0.28f;
                nextAttackTime = Time.time;
                riposteReadyUntil = Time.time + 1.20f;

                if (HUDController.Instance != null)
                {
                    HUDController.Instance.ShowTechnique(
                        "RIPOSTE READY",
                        true);
                }
            }

            currentGuard = Mathf.Max(0f, currentGuard - guardCost);

            if (LastBlockWasPerfect)
            {
                float moralityGuard =
                    RunMorality.Instance != null
                        ? RunMorality.Instance
                            .PerfectBlockGuardBonus
                        : 0f;

                currentGuard = Mathf.Min(
                    MaxGuard,
                    currentGuard +
                    8f +
                    moralityGuard);
            }

            lastGuardHitTime = Time.time;

            if (currentGuard <= 0.01f)
            {
                guardBrokenUntil = Time.time + 1.15f;
                IsBlocking = false;

                if (HUDController.Instance != null)
                {
                    HUDController.Instance.ShowGuardBreak();
                }

                if (ProceduralAudio.Instance != null)
                {
                    ProceduralAudio.Instance.GuardBreak();
                }
            }

            PushGuardHUD();
            return true;
        }

        private void UpdateGuard()
        {
            if (currentGuard < MaxGuard &&
                !IsBlocking &&
                Time.time > lastGuardHitTime + 0.72f)
            {
                currentGuard = Mathf.Min(
                    MaxGuard,
                    currentGuard + GuardRegenPerSecond * Time.deltaTime);

                PushGuardHUD();
            }
        }

        private void PushGuardHUD()
        {
            if (HUDController.Instance == null) return;

            HUDController.Instance.SetGuard(
                currentGuard,
                MaxGuard,
                Mode == WeaponMode.SwordShield,
                Time.time < guardBrokenUntil);
        }

        public void AddDamage(float amount)
        {
            DamageMultiplier += amount;
            PushWeaponHUD();
        }

        public void BreakMomentum()
        {
            if (MomentumCount <= 0) return;

            MomentumCount = 0;
            momentumExpires = -99f;

            if (HUDController.Instance != null)
            {
                HUDController.Instance.SetMomentum(0, 0f);
            }
        }

        private void UpdateMomentum()
        {
            if (MomentumCount > 0 && Time.time > momentumExpires)
            {
                BreakMomentum();
            }
        }

        private void RegisterMomentumHit(WeaponMode attackMode)
        {
            MomentumCount = Mathf.Clamp(MomentumCount + 1, 1, 6);
            momentumWindowDuration = attackMode == WeaponMode.Dagger ? 1.05f : 1.30f;
            momentumExpires = Time.time + momentumWindowDuration;

            if (HUDController.Instance != null)
            {
                HUDController.Instance.SetMomentum(MomentumCount, MomentumBonus);
            }

            if (DungeonDirector.Instance != null)
            {
                DungeonDirector.Instance.RecordMomentum(MomentumCount);
            }
        }

        private void UpdateAimFeedback()
        {
            if (HUDController.Instance == null || viewCamera == null)
            {
                return;
            }

            float range = Mode == WeaponMode.Dagger ? 2.35f : 2.90f;
            int mask = ~(1 << 2);

            bool onTarget = false;

            if (Physics.SphereCast(
                viewCamera.transform.position,
                0.10f,
                viewCamera.transform.forward,
                out RaycastHit hit,
                range,
                mask,
                QueryTriggerInteraction.Ignore))
            {
                EnemyController enemy =
                    hit.collider.GetComponentInParent<EnemyController>();

                MercyCompanion companion =
                    hit.collider.GetComponentInParent<MercyCompanion>();

                onTarget =
                    enemy != null ||
                    (companion != null &&
                     !companion.IsFriendly);
            }

            HUDController.Instance.SetCrosshairTarget(onTarget);
        }

        private void TryAttack()
        {
            if (Time.time < nextAttackTime ||
                swinging ||
                switchingWeapon)
            {
                return;
            }

            if (Mode == comboMode &&
                Time.time <= comboExpires)
            {
                comboStep = (comboStep + 1) % 3;
            }
            else
            {
                comboStep = 0;
            }

            comboMode = Mode;
            comboExpires =
                Time.time +
                (Mode == WeaponMode.Dagger ? 0.92f : 1.18f);

            bool riposte =
                Mode == WeaponMode.SwordShield &&
                Time.time <= riposteReadyUntil;

            if (riposte)
            {
                riposteReadyUntil = -99f;
            }

            float interval =
                Mode == WeaponMode.Dagger
                    ? 0.28f
                    : 0.54f;

            if (comboStep == 2)
            {
                interval *= 1.16f;
            }

            if (RunMorality.Instance != null)
            {
                interval *=
                    RunMorality.Instance
                        .AttackIntervalMultiplier;
            }

            nextAttackTime = Time.time + interval;
            StartCoroutine(
                AttackRoutine(
                    Mode,
                    comboStep,
                    riposte));
        }

        private IEnumerator AttackRoutine(
            WeaponMode attackMode,
            int attackStep,
            bool riposte)
        {
            swinging = true;
            IsBlocking = false;

            Transform root =
                attackMode == WeaponMode.Dagger
                    ? daggerRoot
                    : swordShieldRoot;

            TrailRenderer trail =
                attackMode == WeaponMode.Dagger
                    ? daggerTrail
                    : swordTrail;

            if (root == null)
            {
                swinging = false;
                yield break;
            }

            Quaternion startRotation = root.localRotation;
            Vector3 startPosition = root.localPosition;
            Quaternion windup;
            Quaternion strike;
            Vector3 recoilPosition;

            if (attackMode == WeaponMode.Dagger)
            {
                if (attackStep == 0)
                {
                    windup = Quaternion.Euler(-16f, -24f, -31f);
                    strike = Quaternion.Euler(24f, 15f, 72f);
                    recoilPosition = startPosition + new Vector3(0.05f, 0.02f, -0.07f);
                }
                else if (attackStep == 1)
                {
                    windup = Quaternion.Euler(-12f, 25f, 28f);
                    strike = Quaternion.Euler(30f, -18f, -76f);
                    recoilPosition = startPosition + new Vector3(-0.05f, 0.01f, -0.08f);
                }
                else
                {
                    windup = Quaternion.Euler(31f, -7f, -5f);
                    strike = Quaternion.Euler(-58f, 2f, 4f);
                    recoilPosition = startPosition + new Vector3(0f, -0.04f, -0.15f);
                }
            }
            else
            {
                if (riposte)
                {
                    windup = Quaternion.Euler(-38f, 2f, -12f);
                    strike = Quaternion.Euler(6f, 0f, 2f);
                    recoilPosition = startPosition + new Vector3(0f, 0.04f, -0.20f);
                }
                else if (attackStep == 0)
                {
                    windup = Quaternion.Euler(-8f, 18f, 10f);
                    strike = Quaternion.Euler(8f, -8f, -24f);
                    recoilPosition = startPosition + new Vector3(-0.08f, 0.02f, -0.05f);
                }
                else if (attackStep == 1)
                {
                    windup = Quaternion.Euler(-28f, -23f, -26f);
                    strike = Quaternion.Euler(24f, 20f, 68f);
                    recoilPosition = startPosition + new Vector3(0.04f, 0.06f, -0.13f);
                }
                else
                {
                    windup = Quaternion.Euler(-52f, 4f, 3f);
                    strike = Quaternion.Euler(48f, 0f, -5f);
                    recoilPosition = startPosition + new Vector3(0f, 0.10f, -0.18f);
                }
            }

            float speedScale =
                RunMorality.Instance != null
                    ? RunMorality.Instance.AttackIntervalMultiplier
                    : 1f;

            float windupTime =
                (attackMode == WeaponMode.Dagger ? 0.055f : 0.105f) *
                (attackStep == 2 ? 1.18f : 1f) *
                speedScale;

            float strikeTime =
                (attackMode == WeaponMode.Dagger ? 0.085f : 0.135f) *
                speedScale;

            float returnTime =
                (attackMode == WeaponMode.Dagger ? 0.095f : 0.18f) *
                speedScale;

            for (float t = 0f; t < 1f; t += Time.deltaTime / windupTime)
            {
                root.localRotation =
                    Quaternion.Slerp(
                        startRotation,
                        windup,
                        Smooth01(t));
                root.localPosition =
                    Vector3.Lerp(
                        startPosition,
                        recoilPosition,
                        Smooth01(t));
                yield return null;
            }

            if (trail != null)
            {
                trail.Clear();
                trail.emitting = true;
            }

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.Attack(attackMode);
            }

            bool hitResolved = false;

            for (float t = 0f; t < 1f; t += Time.deltaTime / strikeTime)
            {
                float eased =
                    1f -
                    Mathf.Pow(
                        1f - Mathf.Clamp01(t),
                        3f);

                root.localRotation =
                    Quaternion.Slerp(
                        windup,
                        strike,
                        eased);
                root.localPosition =
                    Vector3.Lerp(
                        recoilPosition,
                        startPosition + Vector3.forward * 0.025f,
                        eased);

                if (!hitResolved && eased >= 0.42f)
                {
                    hitResolved = true;
                    bool dashStrike = ConsumeDashStrike();
                    bool confirmedHit =
                        PerformHit(
                            attackMode,
                            attackStep,
                            riposte,
                            dashStrike);

                    EmitTechniqueWave(
                        attackMode,
                        attackStep,
                        riposte,
                        dashStrike);

                    if (confirmedHit)
                    {
                        float impactHold =
                            attackMode == WeaponMode.Dagger
                                ? 0.018f
                                : 0.036f;

                        if (lastHitEmphasized)
                        {
                            impactHold += 0.012f;
                        }

                        if (lastHitKilled)
                        {
                            impactHold += 0.016f;
                        }

                        yield return new WaitForSecondsRealtime(
                            impactHold);
                    }
                }

                yield return null;
            }

            if (!hitResolved)
            {
                bool dashStrike = ConsumeDashStrike();
                PerformHit(
                    attackMode,
                    attackStep,
                    riposte,
                    dashStrike);
                EmitTechniqueWave(
                    attackMode,
                    attackStep,
                    riposte,
                    dashStrike);
            }

            if (trail != null)
            {
                trail.emitting = false;
            }

            for (float t = 0f; t < 1f; t += Time.deltaTime / returnTime)
            {
                root.localRotation =
                    Quaternion.Slerp(
                        strike,
                        startRotation,
                        Smooth01(t));
                root.localPosition =
                    Vector3.Lerp(
                        startPosition + Vector3.forward * 0.025f,
                        startPosition,
                        Smooth01(t));
                yield return null;
            }

            root.localRotation = startRotation;
            root.localPosition = startPosition;
            swinging = false;

            // 连点输入缓冲与长按连击：完成一刀后再检查能否接下一刀。
            bool hadClickQueued = queuedAttack;
            queuedAttack = false;

            if (hadClickQueued || Input.GetMouseButton(0))
            {
                float delay = Mathf.Max(0f, nextAttackTime - Time.time);
                if (delay > 0f)
                {
                    yield return new WaitForSeconds(delay);
                }

                bool menuOpen = HUDController.Instance != null &&
                    HUDController.Instance.IsModalOpen;
                if (!menuOpen && !IsBlocking && !switchingWeapon &&
                    (hadClickQueued || Input.GetMouseButton(0)))
                {
                    TryAttack();
                }
            }
        }

        private bool ConsumeDashStrike()
        {
            FirstPersonController controller =
                GetComponent<FirstPersonController>();

            return controller != null &&
                   controller.TryConsumeBoostAttack();
        }

        private void EmitTechniqueWave(
            WeaponMode attackMode,
            int attackStep,
            bool riposte,
            bool dashStrike)
        {
            bool finisher = attackStep == 2;

            if (!finisher &&
                !riposte &&
                !dashStrike)
            {
                return;
            }

            if (viewCamera == null)
            {
                return;
            }

            float moralityDamage =
                RunMorality.Instance != null
                    ? RunMorality.Instance.OutgoingDamageMultiplier
                    : 1f;

            float waveDamage =
                (attackMode == WeaponMode.Dagger
                    ? 17f
                    : 22f) *
                DamageMultiplier *
                moralityDamage;

            if (riposte)
            {
                waveDamage *= 1.45f;
            }
            else if (dashStrike)
            {
                waveDamage *= 1.28f;
            }

            float impact =
                attackMode == WeaponMode.Dagger
                    ? 0.64f
                    : 0.92f;

            if (riposte || dashStrike)
            {
                impact += 0.42f;
            }

            BladeWaveProjectile.Spawn(
                viewCamera.transform.position +
                    viewCamera.transform.forward * 0.75f -
                    Vector3.up * 0.08f,
                viewCamera.transform.forward,
                attackMode,
                waveDamage,
                impact,
                riposte || dashStrike);

            if (HUDController.Instance != null)
            {
                string technique =
                    riposte
                        ? "WARDEN RIPOSTE"
                        : dashStrike
                            ? "GALE BREAK"
                            : attackMode == WeaponMode.Dagger
                                ? "WIND REND"
                                : "BULWARK CLEAVE";

                HUDController.Instance.ShowTechnique(
                    technique,
                    riposte || dashStrike);
            }
        }

        private bool PerformHit(
            WeaponMode attackMode,
            int attackStep,
            bool riposte,
            bool dashStrike)
        {
            lastHitEmphasized = false;
            lastHitKilled = false;

            float damage =
                (attackMode == WeaponMode.Dagger ? 24f : 39f) *
                DamageMultiplier;

            float comboMultiplier =
                attackStep == 0
                    ? 1f
                    : attackStep == 1
                        ? 1.12f
                        : attackMode == WeaponMode.Dagger
                            ? 1.34f
                            : 1.46f;

            damage *= comboMultiplier;

            if (riposte)
            {
                damage *= 1.65f;
            }

            if (RunMorality.Instance != null)
            {
                damage *=
                    RunMorality.Instance
                        .OutgoingDamageMultiplier;
            }

            float range =
                attackMode == WeaponMode.Dagger
                    ? 2.60f
                    : 2.95f;
            float radius =
                attackMode == WeaponMode.Dagger
                    ? 0.34f
                    : 0.46f;

            int mask = ~(1 << 2);

            if (viewCamera == null ||
                !Physics.SphereCast(
                    viewCamera.transform.position,
                    radius,
                    viewCamera.transform.forward,
                    out RaycastHit hit,
                    range,
                    mask,
                    QueryTriggerInteraction.Ignore))
            {
                return false;
            }

            EnemyController enemy =
                hit.collider.GetComponentInParent<EnemyController>();

            MercyCompanion companion =
                hit.collider.GetComponentInParent<MercyCompanion>();

            if (enemy == null &&
                (companion == null || companion.IsFriendly))
            {
                return false;
            }

            RegisterMomentumHit(attackMode);
            damage *= 1f + MomentumBonus;

            if (dashStrike)
            {
                damage *=
                    attackMode == WeaponMode.Dagger
                        ? 1.50f
                        : 1.34f;

                if (HUDController.Instance != null)
                {
                    HUDController.Instance.ShowDashStrike();
                }
            }

            bool emphasizedHit =
                dashStrike ||
                riposte ||
                attackStep == 2 ||
                MomentumCount >= 5;

            float impactStrength =
                attackMode == WeaponMode.Dagger
                    ? 0.58f
                    : 1.0f;

            if (attackStep == 2)
            {
                impactStrength += 0.28f;
            }

            if (riposte)
            {
                impactStrength += 0.62f;
            }

            if (dashStrike)
            {
                impactStrength += 0.52f;
            }

            if (MomentumCount >= 5)
            {
                impactStrength += 0.22f;
            }

            if (RunMorality.Instance != null)
            {
                impactStrength *=
                    RunMorality.Instance
                        .ImpactMultiplier;
            }

            if (enemy != null)
            {
                enemy.TakeDamage(
                    damage,
                    viewCamera.transform.forward,
                    impactStrength);

                lastHitKilled = enemy.IsDead;
                emphasizedHit =
                    emphasizedHit ||
                    lastHitKilled;
            }
            else
            {
                companion.TakeDamage(
                    damage,
                    viewCamera.transform.forward);
            }

            lastHitEmphasized = emphasizedHit;

            CombatEffects.SpawnWeaponHit(
                hit.point,
                hit.normal,
                attackMode,
                emphasizedHit);

            CombatEffects.SpawnDamageNumber(
                hit.point,
                damage,
                emphasizedHit);

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.Hit(
                    attackMode,
                    emphasizedHit);
            }

            if (HUDController.Instance != null)
            {
                HUDController.Instance.ShowHitmarker();
            }

            FirstPersonController controller =
                GetComponent<FirstPersonController>();

            weaponImpactKick =
                Mathf.Max(
                    weaponImpactKick,
                    lastHitKilled
                        ? 1.12f
                        : dashStrike || riposte
                            ? (attackMode == WeaponMode.Dagger ? 0.82f : 1f)
                            : (attackMode == WeaponMode.Dagger ? 0.46f : 0.76f));

            if (controller != null)
            {
                controller.AddCameraImpact(
                    lastHitKilled
                        ? (attackMode == WeaponMode.Dagger ? 0.76f : 0.92f)
                        : dashStrike || riposte
                            ? (attackMode == WeaponMode.Dagger ? 0.66f : 0.80f)
                            : (attackMode == WeaponMode.Dagger ? 0.38f : 0.62f),
                    attackMode == WeaponMode.Dagger ? -0.20f : -0.42f);
            }

            return true;
        }

        private void UpdateWeaponSway()
        {
            if (weaponRig == null) return;

            Vector2 mouse = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y"));
            Vector2 targetSway = Vector2.ClampMagnitude(mouse * 1.4f, 4f);
            sway = Vector2.Lerp(sway, targetSway, 1f - Mathf.Exp(-12f * Time.deltaTime));

            float moveX = Input.GetAxisRaw("Horizontal");
            float moveY = Input.GetAxisRaw("Vertical");

            float movementAmount = Mathf.Clamp01(Mathf.Abs(moveX) + Mathf.Abs(moveY));
            float walkBob = movementAmount > 0.1f
                ? Mathf.Sin(Time.time * 9f) * 0.008f * movementAmount
                : 0f;

            weaponImpactKick = Mathf.Lerp(
                weaponImpactKick,
                0f,
                1f - Mathf.Exp(-15f * Time.deltaTime));

            weaponRig.localRotation = Quaternion.Euler(
                -sway.y * 1.1f - weaponImpactKick * 3.8f,
                sway.x * 1.3f,
                -sway.x * 1.8f + weaponImpactKick * 0.8f);

            float targetWallAvoidance = 0f;

            if (viewCamera != null &&
                Physics.Raycast(
                    viewCamera.transform.position,
                    viewCamera.transform.forward,
                    out RaycastHit wallHit,
                    0.88f,
                    ~(1 << 2),
                    QueryTriggerInteraction.Ignore))
            {
                EnemyController enemy = wallHit.collider.GetComponentInParent<EnemyController>();
                if (enemy == null)
                {
                    targetWallAvoidance = 1f - Mathf.Clamp01(wallHit.distance / 0.88f);
                }
            }

            wallAvoidance = Mathf.Lerp(
                wallAvoidance,
                targetWallAvoidance,
                1f - Mathf.Exp(-14f * Time.deltaTime));

            Vector3 rigTarget = new Vector3(
                -sway.x * 0.0025f,
                walkBob +
                sway.y * 0.002f -
                wallAvoidance * 0.075f -
                switchOffset,
                -wallAvoidance * 0.12f -
                switchOffset * 0.18f -
                weaponImpactKick * 0.075f);

            weaponRig.localPosition = Vector3.Lerp(
                weaponRig.localPosition,
                rigTarget,
                1f - Mathf.Exp(-12f * Time.deltaTime));
        }

        private IEnumerator SwitchWeaponRoutine()
        {
            switchingWeapon = true;
            IsBlocking = false;

            const float lowerTime = 0.10f;
            const float raiseTime = 0.13f;
            const float maxOffset = 0.34f;

            for (float elapsed = 0f; elapsed < lowerTime; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / lowerTime);
                switchOffset = Mathf.Lerp(0f, maxOffset, Smooth01(t));
                yield return null;
            }

            switchOffset = maxOffset;

            Mode = Mode == WeaponMode.Dagger
                ? WeaponMode.SwordShield
                : WeaponMode.Dagger;

            comboStep = -1;
            comboMode = Mode;
            comboExpires = -99f;
            queuedAttack = false;

            RefreshModeVisuals();
            PushWeaponHUD();
            PushGuardHUD();

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.SwitchWeapon();
            }

            for (float elapsed = 0f; elapsed < raiseTime; elapsed += Time.deltaTime)
            {
                float t = Mathf.Clamp01(elapsed / raiseTime);
                switchOffset = Mathf.Lerp(maxOffset, 0f, Smooth01(t));
                yield return null;
            }

            switchOffset = 0f;
            switchingWeapon = false;
        }

        private void BuildViewModels()
        {
            if (viewCamera == null) return;

            weaponRig = new GameObject("WeaponRig").transform;
            weaponRig.gameObject.layer = 2;
            weaponRig.SetParent(viewCamera.transform, false);

            BuildDagger();
            BuildSwordShield();
        }

        private void BuildDagger()
        {
            daggerRoot =
                new GameObject(
                    "DaggerView").transform;

            daggerRoot.gameObject.layer = 2;
            daggerRoot.SetParent(
                weaponRig,
                false);

            daggerRoot.localPosition =
                new Vector3(
                    0.34f,
                    -0.36f,
                    0.78f);

            GameObject spriteObject =
                new GameObject(
                    "DaggerPixelSprite",
                    typeof(SpriteRenderer));

            spriteObject.layer = 2;
            spriteObject.transform.SetParent(
                daggerRoot,
                false);

            spriteObject.transform.localPosition =
                new Vector3(
                    0f,
                    0.04f,
                    0f);

            spriteObject.transform.localScale =
                Vector3.one * 1.08f;

            SpriteRenderer renderer =
                spriteObject.GetComponent<SpriteRenderer>();

            renderer.sprite =
                PixelWeaponFactory.Dagger();

            renderer.color = Color.white;
            renderer.sortingOrder = 12;
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            daggerTrail = CreateTrail(
                daggerRoot,
                "DaggerTrail",
                new Vector3(
                    0.22f,
                    0.48f,
                    0.02f),
                new Color(
                    0.68f,
                    0.78f,
                    0.82f),
                0.035f,
                0.075f);
        }

        private void BuildSwordShield()
        {
            swordShieldRoot =
                new GameObject(
                    "SwordShieldView").transform;

            swordShieldRoot.gameObject.layer = 2;
            swordShieldRoot.SetParent(
                weaponRig,
                false);

            swordNormalPosition =
                new Vector3(
                    0.03f,
                    -0.39f,
                    0.78f);

            swordBlockPosition =
                new Vector3(
                    0.00f,
                    -0.20f,
                    0.58f);

            swordShieldRoot.localPosition =
                swordNormalPosition;

            swordNormalRotation =
                swordShieldRoot.localRotation;

            GameObject spriteObject =
                new GameObject(
                    "SwordShieldPixelSprite",
                    typeof(SpriteRenderer));

            spriteObject.layer = 2;
            spriteObject.transform.SetParent(
                swordShieldRoot,
                false);

            spriteObject.transform.localPosition =
                new Vector3(
                    0f,
                    0.06f,
                    0f);

            spriteObject.transform.localScale =
                Vector3.one * 1.08f;

            SpriteRenderer renderer =
                spriteObject.GetComponent<SpriteRenderer>();

            renderer.sprite =
                PixelWeaponFactory.SwordShield();

            renderer.color = Color.white;
            renderer.sortingOrder = 12;
            renderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            swordTrail = CreateTrail(
                swordShieldRoot,
                "SwordTrail",
                new Vector3(
                    0.34f,
                    0.62f,
                    0.02f),
                new Color(
                    0.72f,
                    0.80f,
                    0.84f),
                0.045f,
                0.10f);
        }

        private TrailRenderer CreateTrail(
            Transform parent,
            string name,
            Vector3 localPosition,
            Color color,
            float width,
            float lifetime)
        {
            GameObject tip = new GameObject(name);
            tip.layer = 2;
            tip.transform.SetParent(parent, false);
            tip.transform.localPosition = localPosition;

            TrailRenderer trail = tip.AddComponent<TrailRenderer>();
            trail.time = lifetime;
            trail.startWidth = width;
            trail.endWidth = 0f;
            trail.minVertexDistance = Mathf.Max(0.04f, width * 1.15f);
            trail.textureMode = LineTextureMode.Tile;
            trail.numCornerVertices = 0;
            trail.numCapVertices = 0;
            trail.emitting = false;
            trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            trail.receiveShadows = false;

            Shader shader = Shader.Find("Sprites/Default");
            if (shader == null) shader = Shader.Find("Standard");

            if (shader != null)
            {
                Material material = new Material(shader);
                material.name = "RuntimePixelWeaponTrail_" + name;
                material.color = color;
                material.mainTexture = CombatEffects.PixelTrailTexture;
                material.mainTextureScale = new Vector2(3f, 1f);
                material.hideFlags = HideFlags.HideAndDontSave;
                trail.sharedMaterial = material;
            }

            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[]
                {
                    new GradientColorKey(color, 0f),
                    new GradientColorKey(Color.white, 1f)
                },
                new[]
                {
                    new GradientAlphaKey(0.75f, 0f),
                    new GradientAlphaKey(0f, 1f)
                });

            trail.colorGradient = gradient;
            return trail;
        }

        private static Transform CreatePart(
            Transform parent,
            PrimitiveType type,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Color color)
        {
            GameObject go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.layer = 2;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPosition;
            go.transform.localScale = localScale;

            Collider collider = go.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            Renderer partRenderer = go.GetComponent<Renderer>();
            partRenderer.shadowCastingMode =
                UnityEngine.Rendering.ShadowCastingMode.Off;
            partRenderer.receiveShadows = false;

            Shader shader = Shader.Find("Universal Render Pipeline/Simple Lit");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");

            if (shader != null)
            {
                Material material = new Material(shader);
                material.color = color;

                if (material.HasProperty("_Metallic"))
                {
                    float metallic =
                        name.Contains("Guard") ||
                        name.Contains("Shield") ||
                        name.Contains("Brace")
                            ? 0.28f
                            : 0.06f;
                    material.SetFloat("_Metallic", metallic);
                }

                if (material.HasProperty("_Glossiness"))
                {
                    material.SetFloat("_Glossiness", 0.34f);
                }

                if (material.HasProperty("_Smoothness"))
                {
                    material.SetFloat("_Smoothness", 0.34f);
                }

                partRenderer.sharedMaterial = material;
            }

            return go.transform;
        }

        private void RefreshModeVisuals()
        {
            if (daggerRoot != null)
            {
                daggerRoot.gameObject.SetActive(Mode == WeaponMode.Dagger);
            }

            if (swordShieldRoot != null)
            {
                swordShieldRoot.gameObject.SetActive(Mode == WeaponMode.SwordShield);
                swordShieldRoot.localPosition = swordNormalPosition;
            }

            if (daggerTrail != null) daggerTrail.emitting = false;
            if (swordTrail != null) swordTrail.emitting = false;
        }

        private void PushWeaponHUD()
        {
            if (HUDController.Instance == null) return;

            string label = Mode == WeaponMode.Dagger
                ? "SINGLE SWORD  COMBO"
                : "SWORD + SHIELD  COMBO";
            HUDController.Instance.SetWeapon(label, DamageMultiplier);
        }

        private static float Smooth01(float value)
        {
            value = Mathf.Clamp01(value);
            return value * value * (3f - 2f * value);
        }
    }
}

