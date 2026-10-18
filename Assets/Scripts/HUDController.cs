using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Depthbound
{
    public enum UpgradeChoice
    {
        Vitality,
        Brutality,
        Mobility
    }

    public class HUDController : MonoBehaviour
    {
        public static HUDController Instance { get; private set; }
        public bool IsPaused { get; private set; }

        public bool IsManualPause =>
            pausePanel != null &&
            pausePanel.activeSelf;

        public bool IsBoonScreenOpen =>
            upgradePanel != null &&
            upgradePanel.activeSelf;

        public bool IsEndScreenOpen =>
            endPanel != null &&
            endPanel.activeSelf;

        public bool IsBestiaryOpen =>
            bestiaryPanel != null &&
            bestiaryPanel.activeSelf;

        public bool IsModalOpen =>
            IsPaused ||
            IsBoonScreenOpen ||
            IsEndScreenOpen ||
            IsBestiaryOpen;

        private Canvas canvas;
        private RectTransform healthFill;
        private RectTransform healthTrailFill;
        private Text healthText;
        private Text roomText;
        private Text runTimerText;
        private Text weaponText;
        private Text momentumText;
        private GameObject momentumTimerBar;
        private RectTransform momentumFill;
        private Image momentumFillImage;
        private Text boostText;
        private RectTransform boostFill;
        private Image boostFillImage;
        private GameObject guardPanel;
        private RectTransform guardFill;
        private Image guardFillImage;
        private Text guardText;
        private Text crosshairText;
        private Text hitmarkerText;
        private Text damageDirectionText;
        private Text feedbackText;
        private Text roomBannerText;
        private Text objectiveText;
        private Text hintText;
        private Image damageOverlay;
        private GameObject bossPanel;
        private Image bossPanelImage;
        private RectTransform bossFill;
        private RectTransform bossTrailFill;
        private Image bossFillImage;
        private Text bossText;
        private GameObject moralityPanel;
        private RectTransform mercyFill;
        private RectTransform killFill;
        private Text mercyText;
        private Text killText;
        private Text moralityBonusText;
        private GameObject mercyPromptPanel;
        private Text mercyPromptText;
        private GameObject pauseBackdrop;
        private GameObject pausePanel;
        private Text settingsSensitivityText;
        private Text settingsVolumeText;
        private Text settingsDisplayText;
        private Text settingsShakeText;
        private GameObject upgradeBackdrop;
        private GameObject upgradePanel;
        private GameObject endBackdrop;
        private Image endBackdropImage;
        private GameObject endPanel;
        private Text endTitle;
        private Text endBody;
        private Image endAccent;
        private GameObject bestiaryBackdrop;
        private GameObject bestiaryPanel;
        private Text bestiaryBody;
        private Action<UpgradeChoice> upgradeCallback;
        private FirstPersonController playerController;
        private PlayerCombat playerCombat;

        private int lastRunTimerSecond = -1;
        private int lastBoostPercent = -1;
        private bool lastBoostReady;

        private float targetHealthRatio = 1f;
        private float shownHealthRatio = 1f;
        private float shownHealthTrailRatio = 1f;
        private float damageOverlayStrength;
        private float heartbeatTimer;
        private float hitmarkerTimer;
        private float damageDirectionTimer;
        private float feedbackTimer;
        private float roomBannerTimer;
        private float roomBannerIntro;
        private float objectivePulse;
        private Vector2 roomBannerBasePosition;
        private Vector2 objectiveBasePosition;
        private Color objectiveTargetColor = Color.white;
        private float hintTimer = 9.5f;
        private bool crosshairOnTarget;
        private bool crushingBlowHintShown;
        private float targetBossRatio = 1f;
        private float shownBossRatio = 1f;
        private float shownBossTrailRatio = 1f;

        private void Awake()
        {
            Instance = this;
            EnsureEventSystem();
            BuildHUD();
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;

            if (Input.GetKeyDown(KeyCode.B) &&
                !IsBoonScreenOpen &&
                !IsEndScreenOpen)
            {
                ToggleBestiary();
            }

            if (playerController == null)
            {
                playerController = UnityEngine.Object.FindFirstObjectByType<FirstPersonController>();
            }

            if (playerCombat == null)
            {
                playerCombat = UnityEngine.Object.FindFirstObjectByType<PlayerCombat>();
            }

            if (runTimerText != null &&
                DungeonDirector.Instance != null)
            {
                int totalSeconds = Mathf.FloorToInt(
                    DungeonDirector.Instance.ElapsedRunTime);

                if (totalSeconds != lastRunTimerSecond)
                {
                    lastRunTimerSecond = totalSeconds;

                    int minutes = totalSeconds / 60;
                    int seconds = totalSeconds % 60;

                    runTimerText.text =
                        "RUN  " +
                        minutes.ToString("00") +
                        ":" +
                        seconds.ToString("00");
                }
            }

            if (boostText != null && playerController != null)
            {
                float boostRatio = playerController.BoostReady01;

                bool boostReady =
                    boostRatio >= 0.99f;

                int boostPercent = boostReady
                    ? 100
                    : Mathf.Clamp(
                        Mathf.RoundToInt(boostRatio * 20f) * 5,
                        0,
                        95);

                if (boostReady != lastBoostReady ||
                    boostPercent != lastBoostPercent)
                {
                    lastBoostReady = boostReady;
                    lastBoostPercent = boostPercent;

                    boostText.text = boostReady
                        ? "BOOST  [SPACE]  READY"
                        : "BOOST  " +
                          boostPercent +
                          "%";
                }

                if (boostFill != null)
                {
                    Vector2 maxAnchor = boostFill.anchorMax;
                    maxAnchor.x = boostRatio;
                    boostFill.anchorMax = maxAnchor;
                }

                if (boostFillImage != null)
                {
                    float pulse = boostRatio >= 0.99f
                        ? 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 2.8f)
                        : 0f;

                    boostFillImage.color = Color.Lerp(
                        new Color(0.31f, 0.44f, 0.29f, 0.94f),
                        new Color(0.53f, 0.63f, 0.39f, 1f),
                        pulse);
                }
            }

            if (upgradePanel != null && upgradePanel.activeSelf)
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) ChooseUpgrade(UpgradeChoice.Vitality);
                else if (Input.GetKeyDown(KeyCode.Alpha2)) ChooseUpgrade(UpgradeChoice.Brutality);
                else if (Input.GetKeyDown(KeyCode.Alpha3)) ChooseUpgrade(UpgradeChoice.Mobility);
            }

            if (momentumTimerBar != null && playerCombat != null)
            {
                bool showMomentum = playerCombat.MomentumCount >= 2;
                momentumTimerBar.SetActive(showMomentum);

                if (showMomentum && momentumFill != null)
                {
                    float window = playerCombat.MomentumWindow01;
                    Vector2 maxAnchor = momentumFill.anchorMax;
                    maxAnchor.x = window;
                    momentumFill.anchorMax = maxAnchor;

                    if (momentumFillImage != null)
                    {
                        momentumFillImage.color = Color.Lerp(
                            new Color(0.78f, 0.18f, 0.12f, 0.98f),
                            new Color(0.94f, 0.66f, 0.24f, 0.98f),
                            window);
                    }
                }
            }

            shownHealthRatio = Mathf.Lerp(
                shownHealthRatio,
                targetHealthRatio,
                1f - Mathf.Exp(-10f * dt));

            if (targetHealthRatio >= shownHealthTrailRatio)
            {
                shownHealthTrailRatio = targetHealthRatio;
            }
            else
            {
                shownHealthTrailRatio = Mathf.MoveTowards(
                    shownHealthTrailRatio,
                    targetHealthRatio,
                    dt * 0.34f);
            }

            if (healthTrailFill != null)
            {
                Vector2 trailAnchor = healthTrailFill.anchorMax;
                trailAnchor.x =
                    Mathf.Round(
                        shownHealthTrailRatio * 20f) /
                    20f;
                healthTrailFill.anchorMax = trailAnchor;
            }

            if (healthFill != null)
            {
                Vector2 maxAnchor = healthFill.anchorMax;
                maxAnchor.x =
                    Mathf.Round(
                        shownHealthRatio * 20f) /
                    20f;
                healthFill.anchorMax = maxAnchor;
            }

            shownBossRatio = Mathf.Lerp(
                shownBossRatio,
                targetBossRatio,
                1f - Mathf.Exp(-8f * dt));

            if (targetBossRatio >= shownBossTrailRatio)
            {
                shownBossTrailRatio = targetBossRatio;
            }
            else
            {
                shownBossTrailRatio = Mathf.MoveTowards(
                    shownBossTrailRatio,
                    targetBossRatio,
                    dt * 0.22f);
            }

            if (bossTrailFill != null)
            {
                Vector2 trailAnchor = bossTrailFill.anchorMax;
                trailAnchor.x =
                    Mathf.Round(
                        shownBossTrailRatio * 30f) /
                    30f;
                bossTrailFill.anchorMax = trailAnchor;
            }

            if (bossFill != null)
            {
                Vector2 maxAnchor = bossFill.anchorMax;
                maxAnchor.x =
                    Mathf.Round(
                        shownBossRatio * 30f) /
                    30f;
                bossFill.anchorMax = maxAnchor;
            }

            if (targetHealthRatio < 0.30f &&
                !IsModalOpen &&
                ProceduralAudio.Instance != null)
            {
                heartbeatTimer -= dt;

                if (heartbeatTimer <= 0f)
                {
                    ProceduralAudio.Instance.Heartbeat();
                    heartbeatTimer = Mathf.Lerp(
                        0.95f,
                        0.58f,
                        Mathf.InverseLerp(0.30f, 0.08f, targetHealthRatio));
                }
            }
            else
            {
                heartbeatTimer = 0f;
            }

            damageOverlayStrength = Mathf.MoveTowards(damageOverlayStrength, 0f, dt * 1.7f);
            if (damageOverlay != null)
            {
                float lowHealth = Mathf.InverseLerp(0.34f, 0.08f, targetHealthRatio);
                float lowPulse = lowHealth * (0.055f + (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 4.2f)) * 0.035f);

                Color c = damageOverlay.color;
                c.a = Mathf.Clamp(damageOverlayStrength * 0.32f + lowPulse, 0f, 0.36f);
                damageOverlay.color = c;
            }

            UpdateTimedText(hitmarkerText, ref hitmarkerTimer, dt, 0.12f);
            UpdateTimedText(damageDirectionText, ref damageDirectionTimer, dt, 0.26f);
            UpdateTimedText(feedbackText, ref feedbackTimer, dt, 0.30f);
            UpdateTimedText(roomBannerText, ref roomBannerTimer, dt, 0.55f);

            if (roomBannerText != null)
            {
                roomBannerIntro = Mathf.MoveTowards(
                    roomBannerIntro,
                    0f,
                    dt * 3.8f);

                float steppedIntro =
                    Mathf.Ceil(roomBannerIntro * 4f) /
                    4f;

                roomBannerText.rectTransform.anchoredPosition =
                    roomBannerBasePosition +
                    Vector2.down *
                    steppedIntro *
                    12f;

                float bannerScale =
                    1f +
                    steppedIntro *
                    0.018f;

                roomBannerText.rectTransform.localScale =
                    Vector3.one *
                    bannerScale;
            }

            if (objectiveText != null)
            {
                objectivePulse =
                    Mathf.MoveTowards(
                        objectivePulse,
                        0f,
                        dt * 2.4f);

                float steppedPulse =
                    Mathf.Ceil(
                        objectivePulse * 4f) /
                    4f;

                float scale =
                    1f +
                    steppedPulse *
                    0.016f;

                objectiveText.rectTransform.localScale =
                    Vector3.one * scale;

                objectiveText.rectTransform.anchoredPosition =
                    objectiveBasePosition +
                    Vector2.left *
                    steppedPulse *
                    8f;

                Color objectiveColor =
                    objectiveTargetColor;

                objectiveColor.a =
                    Mathf.Lerp(
                        Mathf.Min(
                            0.22f,
                            objectiveTargetColor.a),
                        objectiveTargetColor.a,
                        1f - steppedPulse);

                objectiveText.color =
                    objectiveColor;
            }

            if (hintText != null)
            {
                hintTimer = Mathf.Max(0f, hintTimer - dt);
                float hintAlpha = hintTimer > 2f
                    ? 0.78f
                    : Mathf.Clamp01(hintTimer / 2f) * 0.78f;

                SetTextAlpha(hintText, hintAlpha);
            }

            if (crosshairText != null)
            {
                float pulse = hitmarkerTimer > 0f ? 1.18f : 1f;
                crosshairText.rectTransform.localScale = Vector3.Lerp(
                    crosshairText.rectTransform.localScale,
                    Vector3.one * pulse,
                    1f - Mathf.Exp(-18f * dt));

                Color targetColor = crosshairOnTarget
                    ? new Color(0.80f, 0.42f, 0.26f, 0.94f)
                    : new Color(0.82f, 0.80f, 0.70f, 0.76f);

                crosshairText.color = Color.Lerp(
                    crosshairText.color,
                    targetColor,
                    1f - Mathf.Exp(-16f * dt));
            }
        }

        public void SetHealth(float current, float max)
        {
            targetHealthRatio = max <= 0f ? 0f : Mathf.Clamp01(current / max);

            if (healthText != null)
            {
                healthText.text = Mathf.CeilToInt(current) + "  /  " + Mathf.CeilToInt(max);
                healthText.color = targetHealthRatio < 0.3f
                    ? new Color(0.82f, 0.38f, 0.28f)
                    : new Color(0.88f, 0.86f, 0.75f);
            }
        }

        public void SetRoom(int roomIndex, int totalRooms, int enemiesRemaining)
        {
            if (roomText == null) return;

            int act = roomIndex / 3 + 1;
            int chamber = roomIndex % 3 + 1;

            string enemyInfo =
                enemiesRemaining >= 0
                    ? "     FOES  " + enemiesRemaining
                    : "";

            roomText.text =
                "CHAMBER  " +
                act +
                "-" +
                chamber +
                enemyInfo;
        }

        public void SetMorality(
            float mercy,
            float kill,
            float defenseBonus,
            float attackSpeedBonus)
        {
            mercy = Mathf.Clamp01(mercy);
            kill = Mathf.Clamp01(kill);

            if (mercyFill != null)
            {
                Vector2 anchor = mercyFill.anchorMax;
                anchor.x = mercy;
                mercyFill.anchorMax = anchor;
            }

            if (killFill != null)
            {
                Vector2 anchor = killFill.anchorMax;
                anchor.x = kill;
                killFill.anchorMax = anchor;
            }

            if (mercyText != null)
            {
                string doctrine =
                    mercy >= 0.70f
                        ? "  RENEW II"
                        : mercy >= 0.35f
                            ? "  RENEW I"
                            : "";

                mercyText.text =
                    "MERCY  " +
                    Mathf.RoundToInt(
                        mercy * 100f) +
                    "%" +
                    doctrine;
            }

            if (killText != null)
            {
                string doctrine =
                    kill >= 0.70f
                        ? "  RUSH II"
                        : kill >= 0.35f
                            ? "  RUSH I"
                            : "";

                killText.text =
                    "SIN  " +
                    Mathf.RoundToInt(
                        kill * 100f) +
                    "%" +
                    doctrine;
            }

            if (moralityBonusText != null)
            {
                int defensePercent = Mathf.RoundToInt(defenseBonus * 100f);
                int speedPercent = Mathf.RoundToInt(attackSpeedBonus * 100f);

                moralityBonusText.text =
                    "DEF +" + defensePercent + "%     ATK SPD +" +
                    speedPercent + "%";
            }
        }

        public void ShowMercyPrompt(EnemyArchetype archetype)
        {
            if (mercyPromptPanel != null)
            {
                mercyPromptPanel.SetActive(true);
            }

            if (mercyPromptText != null)
            {
                mercyPromptText.text =
                    ArchetypeLabel(archetype) +
                    " YIELDS\n<color=#7F9A6E>[Y] SPARE</color>    " +
                    "<color=#B56747>[ATTACK / X] FINISH</color>";
            }
        }

        public void HideMercyPrompt()
        {
            if (mercyPromptPanel != null)
            {
                mercyPromptPanel.SetActive(false);
            }
        }

        public void ShowMoralityFeedback(string message, bool mercy)
        {
            ShowCombatFeedback(
                message,
                mercy
                    ? new Color(0.46f, 0.62f, 0.44f)
                    : new Color(0.70f, 0.31f, 0.20f),
                0.95f);
        }

        public void ShowBossReinforcementOutcome(
            int allies,
            int absent,
            int betrayers)
        {
            if (allies <= 0 && betrayers <= 0)
            {
                ShowCombatFeedback(
                    absent + " SPARED FOE" + (absent == 1 ? "" : "S") +
                    " DID NOT RETURN",
                    new Color(0.70f, 0.70f, 0.66f),
                    2.0f);
                return;
            }

            string message =
                allies + " HELP  /  " +
                absent + " ABSENT  /  " +
                betrayers + " BETRAY";

            ShowCombatFeedback(
                message,
                allies >= betrayers
                    ? new Color(0.46f, 0.62f, 0.44f)
                    : new Color(0.70f, 0.30f, 0.19f),
                2.2f);
        }

        public void ShowActMoralityTransition(
            bool mercyCarries,
            int nextAct)
        {
            ShowCombatFeedback(
                mercyCarries
                    ? "MERCY CARRIES FORWARD - ACT " + nextAct
                    : "MERCY AND SIN RESET - ACT " + nextAct,
                mercyCarries
                    ? new Color(0.48f, 0.64f, 0.46f)
                    : new Color(0.66f, 0.63f, 0.54f),
                2.0f);
        }

        private static string ArchetypeLabel(EnemyArchetype archetype)
        {
            switch (archetype)
            {
                case EnemyArchetype.Mole: return "MOLE";
                case EnemyArchetype.Armored: return "ARMORED SOLDIER";
                case EnemyArchetype.Mech: return "MECH SOLDIER";
                case EnemyArchetype.Skeleton: return "SKELETON";
                case EnemyArchetype.Bat: return "BAT";
                case EnemyArchetype.Slime: return "SLIME";
                default: return "FOE";
            }
        }

        public void SetWeapon(string label, float damageMultiplier)
        {
            if (weaponText != null)
            {
                weaponText.text = label + "\n<color=#9E987F>DMG  x" + damageMultiplier.ToString("0.00") + "</color>";
            }
        }

        public void SetMomentum(int count, float bonus)
        {
            if (momentumText == null) return;

            if (count < 2)
            {
                momentumText.text = "";
                SetTextAlpha(momentumText, 0f);

                if (momentumTimerBar != null)
                {
                    momentumTimerBar.SetActive(false);
                }

                return;
            }

            int bonusPercent = Mathf.RoundToInt(bonus * 100f);
            momentumText.text =
                "MOMENTUM  x" + count +
                "\n<color=#E6B85C>+" + bonusPercent + "% DMG</color>";

            momentumText.color = count >= 5
                ? new Color(1f, 0.72f, 0.30f, 1f)
                : new Color(0.78f, 0.84f, 0.90f, 0.92f);

            SetTextAlpha(momentumText, 1f);
        }

        public void SetCrosshairTarget(bool onTarget)
        {
            crosshairOnTarget = onTarget;
        }

        public void ShowHitmarker()
        {
            hitmarkerTimer = 0.15f;
            if (hitmarkerText != null)
            {
                hitmarkerText.text = "X";
                SetTextAlpha(hitmarkerText, 1f);
            }
        }

        public void ShowDamage(float severity)
        {
            damageOverlayStrength = Mathf.Max(
                damageOverlayStrength,
                Mathf.Lerp(0.45f, 1f, severity));
        }

        public void ShowDamageDirection(
            Vector3 sourcePosition,
            Transform playerTransform)
        {
            if (damageDirectionText == null || playerTransform == null)
            {
                return;
            }

            Vector3 toSource = sourcePosition - playerTransform.position;
            toSource.y = 0f;

            if (toSource.sqrMagnitude < 0.001f)
            {
                return;
            }

            float angle = Vector3.SignedAngle(
                playerTransform.forward,
                toSource.normalized,
                Vector3.up);

            float radians = angle * Mathf.Deg2Rad;
            Vector2 direction = new Vector2(
                Mathf.Sin(radians),
                Mathf.Cos(radians));

            RectTransform rect = damageDirectionText.rectTransform;
            rect.anchoredPosition = direction * 105f;
            rect.localRotation = Quaternion.Euler(0f, 0f, -angle);

            damageDirectionText.text = "^";
            damageDirectionText.color = new Color(1f, 0.34f, 0.24f, 1f);
            SetTextAlpha(damageDirectionText, 1f);
            damageDirectionTimer = 0.62f;
        }

        public void SetGuard(float current, float max, bool visible, bool broken)
        {
            if (guardPanel != null)
            {
                guardPanel.SetActive(visible);
            }

            float ratio = max <= 0f ? 0f : Mathf.Clamp01(current / max);

            if (guardFill != null)
            {
                Vector2 maxAnchor = guardFill.anchorMax;
                maxAnchor.x = ratio;
                guardFill.anchorMax = maxAnchor;
            }

            if (guardFillImage != null)
            {
                guardFillImage.color = broken
                    ? new Color(0.62f, 0.14f, 0.09f, 1f)
                    : Color.Lerp(
                        new Color(0.56f, 0.22f, 0.13f, 0.98f),
                        new Color(0.38f, 0.52f, 0.37f, 0.98f),
                        ratio);
            }

            if (guardText != null)
            {
                guardText.text = broken
                    ? "GUARD BROKEN"
                    : "GUARD  " + Mathf.CeilToInt(current);

                guardText.color = broken
                    ? new Color(0.78f, 0.31f, 0.20f)
                    : new Color(0.66f, 0.69f, 0.56f);
            }
        }

        public void ShowBlock()
        {
            ShowCombatFeedback("BLOCK", new Color(0.55f, 0.86f, 1f), 0.42f);
        }

        public void ShowParry()
        {
            ShowCombatFeedback("PERFECT BLOCK", new Color(0.82f, 0.96f, 1f), 0.68f);
        }

        public void ShowGuardBreak()
        {
            ShowCombatFeedback("GUARD BREAK", new Color(1f, 0.34f, 0.22f), 0.78f);
        }

        public void ShowEvade()
        {
            ShowCombatFeedback("EVADE", new Color(0.48f, 0.90f, 0.88f), 0.48f);
        }

        public void ShowCovered()
        {
            ShowCombatFeedback(
                "COVERED",
                new Color(0.58f, 0.76f, 0.82f),
                0.46f);
        }

        public void ShowDashStrike()
        {
            ShowCombatFeedback("DASH STRIKE", new Color(1f, 0.74f, 0.30f), 0.55f);
        }

        public void ShowBossPhase()
        {
            ShowCombatFeedback("WARDEN ENRAGED", new Color(0.78f, 0.30f, 0.16f), 1.15f);
        }

        public void ShowBossAwakens()
        {
            ShowCombatFeedback("WARDEN AWAKENS", new Color(0.74f, 0.42f, 0.22f), 1.05f);
        }

        public void ShowBossMechanicWarning(
            int actIndex)
        {
            switch (actIndex)
            {
                case 0:
                    ShowCombatFeedback(
                        "BURROW AMBUSH - WATCH THE RING",
                        new Color(0.66f, 0.46f, 0.24f),
                        1.10f);
                    break;

                case 1:
                    ShowCombatFeedback(
                        "IRON LOCKDOWN - LEAVE THE AXES",
                        new Color(0.72f, 0.52f, 0.25f),
                        1.10f);
                    break;

                default:
                    ShowCombatFeedback(
                        "ASHEN PURSUIT - KEEP MOVING",
                        new Color(0.72f, 0.30f, 0.19f),
                        1.10f);
                    break;
            }
        }

        public void ShowRelicAcquired(
            int actIndex)
        {
            switch (actIndex)
            {
                case 0:
                    ShowCombatFeedback(
                        "SUPPLY SATCHEL - MAX HP +5 / HEAL",
                        new Color(0.82f, 0.72f, 0.46f),
                        1.25f);
                    break;

                case 1:
                    ShowCombatFeedback(
                        "SHARPENING GEAR - DAMAGE +10%",
                        new Color(0.86f, 0.58f, 0.30f),
                        1.25f);
                    break;

                default:
                    ShowCombatFeedback(
                        "BONE CHARM - MOBILITY / HEAL",
                        new Color(0.70f, 0.76f, 0.68f),
                        1.25f);
                    break;
            }
        }

        public void ShowHazardWarning(
            int actIndex = 0)
        {
            switch (Mathf.Clamp(actIndex, 0, 2))
            {
                case 1:
                    ShowCombatFeedback(
                        "MACHINE VAULT - GEAR RUNE",
                        new Color(0.92f, 0.62f, 0.22f),
                        1.45f);
                    break;

                case 2:
                    ShowCombatFeedback(
                        "ASH RELIC - WIDE RUNE",
                        new Color(0.92f, 0.34f, 0.20f),
                        1.45f);
                    break;

                default:
                    ShowCombatFeedback(
                        "RELIC CELLAR - BURROW RUNE",
                        new Color(0.84f, 0.52f, 0.22f),
                        1.45f);
                    break;
            }
        }

        public void ShowCrushingBlowWarning()
        {
            if (crushingBlowHintShown) return;

            crushingBlowHintShown = true;
            ShowCombatFeedback(
                "CRUSHING BLOW - PARRY OR EVADE",
                new Color(1f, 0.62f, 0.20f),
                1.05f);
        }

        public void ShowHeal(float amount)
        {
            ShowCombatFeedback("SOUL +" + Mathf.CeilToInt(amount), new Color(0.38f, 0.96f, 0.82f), 0.58f);
        }

        public void ShowBoonAcquired(UpgradeChoice choice)
        {
            switch (choice)
            {
                case UpgradeChoice.Vitality:
                    ShowCombatFeedback("VITALITY AWAKENED", new Color(1f, 0.38f, 0.40f), 0.90f);
                    break;
                case UpgradeChoice.Brutality:
                    ShowCombatFeedback("BRUTALITY AWAKENED", new Color(1f, 0.64f, 0.24f), 0.90f);
                    break;
                default:
                    ShowCombatFeedback("MOBILITY AWAKENED", new Color(0.30f, 0.84f, 1f), 0.90f);
                    break;
            }
        }

        private void ShowCombatFeedback(string message, Color color, float duration)
        {
            feedbackTimer = duration;

            if (feedbackText != null)
            {
                feedbackText.text = message;
                feedbackText.color = new Color(color.r, color.g, color.b, 1f);
            }
        }

        public void ShowActStyleShift(int act)
        {
            string label;
            Color color;

            switch (Mathf.Clamp(act, 0, 2))
            {
                case 1:
                    label = "ACT II - IRON VAULT";
                    color = new Color(0.78f, 0.55f, 0.22f);
                    break;
                case 2:
                    label = "ACT III - ASHEN CRYPT";
                    color = new Color(0.88f, 0.30f, 0.18f);
                    break;
                default:
                    label = "ACT I - MOSSBOUND ARMORY";
                    color = new Color(0.50f, 0.70f, 0.48f);
                    break;
            }

            ShowCombatFeedback(
                label,
                color,
                2.10f);
        }

        public void ShowTechnique(
            string technique,
            bool empowered)
        {
            ShowCombatFeedback(
                technique,
                empowered
                    ? new Color(1.0f, 0.74f, 0.28f)
                    : new Color(0.48f, 0.80f, 0.86f),
                empowered ? 1.35f : 1.05f);
        }

        public void SetObjective(string message, Color color)
        {
            if (objectiveText == null) return;

            objectiveText.text = message;
            objectiveTargetColor = color;
            objectiveText.color = new Color(
                color.r,
                color.g,
                color.b,
                Mathf.Min(0.22f, color.a));
            objectivePulse = 1f;
        }

        public void ShowEncounterRule(
            string label)
        {
            if (string.IsNullOrEmpty(label))
            {
                return;
            }

            ShowCombatFeedback(
                label,
                new Color(
                    0.62f,
                    0.58f,
                    0.43f),
                1.05f);
        }

        public void ShowWaveIncoming(
            int wave,
            int totalWaves)
        {
            ShowCombatFeedback(
                "WAVE " +
                Mathf.Max(1, wave) +
                " / " +
                Mathf.Max(1, totalWaves),
                new Color(
                    0.66f,
                    0.55f,
                    0.34f),
                0.90f);
        }

        public void ShowRoomBanner(int roomIndex, bool boss)
        {
            roomBannerTimer = 2.15f;
            roomBannerIntro = 1f;

            if (roomBannerText == null)
            {
                return;
            }

            int act = roomIndex / 3 + 1;
            int chamber = roomIndex % 3 + 1;

            string chamberName;

            if (boss)
            {
                chamberName = act == 1
                    ? "<color=#D97858>THE BURROW SEAL</color>"
                    : act == 2
                        ? "<color=#D79A58>THE IRON SEAL</color>"
                        : "<color=#D96458>THE ASHEN SEAL</color>";
            }
            else if (act == 1)
            {
                chamberName = chamber == 1
                    ? "<color=#83B19A>OLD ARMORY</color>"
                    : "<color=#91A58A>STORAGE CELLAR</color>";
            }
            else if (act == 2)
            {
                chamberName = chamber == 1
                    ? "<color=#C09A68>GEAR PASSAGE</color>"
                    : "<color=#C48A55>ANCIENT MACHINE VAULT</color>";
            }
            else
            {
                chamberName = chamber == 1
                    ? "<color=#A59B89>BONE CRYPT</color>"
                    : "<color=#9C8F82>ASH CELLAR</color>";
            }

            roomBannerText.text =
                "CHAMBER " +
                act +
                "-" +
                chamber +
                "\n" +
                chamberName;

            SetTextAlpha(roomBannerText, 1f);
        }

        public void ShowRoomClear()
        {
            roomBannerTimer = 1.35f;
            roomBannerIntro = 1f;
            if (roomBannerText != null)
            {
                roomBannerText.text = "<color=#91A57C>CHAMBER CLEARED</color>";
                SetTextAlpha(roomBannerText, 1f);
            }
        }

        public void ShowBoss(string bossName, float current, float max)
        {
            if (bossPanel != null) bossPanel.SetActive(true);
            if (bossText != null) bossText.text = bossName;

            bool enraged = bossName.Contains("ENRAGED");

            if (bossPanelImage != null)
            {
                bossPanelImage.color = enraged
                    ? new Color(0.105f, 0.035f, 0.025f, 0.98f)
                    : new Color(0.060f, 0.045f, 0.034f, 0.96f);
            }

            if (bossFillImage != null)
            {
                bossFillImage.color = enraged
                    ? new Color(0.68f, 0.12f, 0.07f, 1f)
                    : new Color(0.54f, 0.14f, 0.09f, 0.98f);
            }

            if (bossText != null)
            {
                bossText.color = enraged
                    ? new Color(0.86f, 0.42f, 0.25f)
                    : new Color(0.76f, 0.57f, 0.42f);
            }

            SetBossHealth(current, max);
        }

        public void SetBossHealth(float current, float max)
        {
            targetBossRatio = max <= 0f ? 0f : Mathf.Clamp01(current / max);
        }

        public void HideBoss()
        {
            targetBossRatio = 0f;
            shownBossRatio = 0f;
            if (bossPanel != null) bossPanel.SetActive(false);
        }

        public void TogglePause()
        {
            if (IsBestiaryOpen)
            {
                SetBestiary(false);
                return;
            }

            if (upgradePanel != null && upgradePanel.activeSelf) return;
            if (endPanel != null && endPanel.activeSelf) return;
            SetPaused(!IsPaused);
        }

        public void ToggleBestiary()
        {
            if (IsManualPause && !IsBestiaryOpen)
            {
                return;
            }

            SetBestiary(!IsBestiaryOpen);
        }

        private void SetBestiary(bool visible)
        {
            if (bestiaryPanel == null ||
                bestiaryBackdrop == null)
            {
                return;
            }

            bestiaryPanel.SetActive(visible);
            bestiaryBackdrop.SetActive(visible);
            IsPaused = visible;
            Time.timeScale = visible ? 0f : 1f;

            if (visible)
            {
                if (bestiaryBody != null)
                {
                    bestiaryBody.text =
                        BestiaryCatalog.BuildDisplayText();
                }

                QuitUtility.UnlockCursor();
            }
            else
            {
                QuitUtility.LockCursor();
            }

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.SetMusicDucked(visible);
            }
        }

        public void SetPaused(bool paused)
        {
            IsPaused = paused;
            Time.timeScale = paused ? 0f : 1f;

            if (pauseBackdrop != null)
            {
                pauseBackdrop.SetActive(paused);
            }

            if (pausePanel != null)
            {
                pausePanel.SetActive(paused);
            }

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.SetMusicDucked(paused);
            }

            if (paused) QuitUtility.UnlockCursor();
            else QuitUtility.LockCursor();
        }

        public void ShowUpgrade(Action<UpgradeChoice> callback)
        {
            upgradeCallback = callback;
            Time.timeScale = 0f;
            IsPaused = true;

            if (upgradeBackdrop != null)
            {
                upgradeBackdrop.SetActive(true);
            }

            if (upgradePanel != null)
            {
                upgradePanel.SetActive(true);
            }

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.SetMusicDucked(true);
            }

            QuitUtility.UnlockCursor();
        }

        public void ShowEnd(bool victory, string summary = null)
        {
            Time.timeScale = 0f;
            IsPaused = true;
            QuitUtility.UnlockCursor();

            if (endBackdrop != null)
            {
                endBackdrop.SetActive(true);
            }

            if (endBackdropImage != null)
            {
                endBackdropImage.color = victory
                    ? new Color(0.018f, 0.024f, 0.015f, 0.74f)
                    : new Color(0.032f, 0.012f, 0.008f, 0.76f);
            }

            if (endPanel != null)
            {
                endPanel.SetActive(true);
            }

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.SetMusicDucked(true);
            }

            if (endTitle != null)
            {
                endTitle.text = victory ? "RUN COMPLETE" : "YOU FELL";
                endTitle.color = victory
                    ? new Color(0.62f, 0.70f, 0.52f)
                    : new Color(0.78f, 0.34f, 0.23f);
            }

            if (endAccent != null)
            {
                endAccent.color = victory
                    ? new Color(0.40f, 0.58f, 0.38f, 0.95f)
                    : new Color(0.62f, 0.20f, 0.12f, 0.95f);
            }

            if (endBody != null)
            {
                string message = victory
                    ? "The final chamber is silent.\nYou survived this descent."
                    : "The dungeon claimed this run.\nReturn stronger and try again.";

                endBody.text = string.IsNullOrEmpty(summary)
                    ? message
                    : message + "\n\n<color=#AAA48B>" + summary + "</color>";
            }
        }

        private void ChooseUpgrade(UpgradeChoice choice)
        {
            if (upgradeBackdrop != null)
            {
                upgradeBackdrop.SetActive(false);
            }

            if (upgradePanel != null)
            {
                upgradePanel.SetActive(false);
            }

            Time.timeScale = 1f;
            IsPaused = false;
            QuitUtility.LockCursor();

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.SetMusicDucked(false);
                ProceduralAudio.Instance.Boon();
            }

            Action<UpgradeChoice> callback = upgradeCallback;
            upgradeCallback = null;
            if (callback != null) callback(choice);
        }

        private void BuildHUD()
        {
            canvas = UIFactory.CreateCanvas("HUDCanvas");
            canvas.transform.SetParent(transform);

            damageOverlay = UIFactory.CreatePanel(canvas.transform, "DamageOverlay",
                new Color(0.65f, 0.02f, 0.02f, 0f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            damageOverlay.raycastTarget = false;

            Image vignette = UIFactory.CreatePanel(
                canvas.transform,
                "Vignette",
                new Color(1f, 1f, 1f, 0.48f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            vignette.sprite = UIFactory.VignetteSprite;
            vignette.raycastTarget = false;

            Image healthBg = UIFactory.CreatePanel(canvas.transform, "HealthBackground",
                new Color(0.055f, 0.058f, 0.045f, 0.94f),
                new Vector2(0.028f, 0.910f), new Vector2(0.250f, 0.958f), Vector2.zero, Vector2.zero);

            UIFactory.AddPixelFrame(
                healthBg.transform,
                "HealthPixelFrame",
                new Color(0.42f, 0.40f, 0.31f, 0.92f),
                2f);

            UIFactory.CreatePanel(healthBg.transform, "HealthBorder",
                new Color(0.18f, 0.18f, 0.14f, 0.22f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero).raycastTarget = false;

            Image healthTrail = UIFactory.CreatePanel(
                healthBg.transform,
                "HealthTrail",
                new Color(0.62f, 0.39f, 0.24f, 0.92f),
                Vector2.zero,
                Vector2.one,
                new Vector2(5f, 5f),
                new Vector2(-5f, -5f));
            healthTrail.raycastTarget = false;
            healthTrailFill = healthTrail.rectTransform;

            Image fill = UIFactory.CreatePanel(healthBg.transform, "HealthFill",
                new Color(0.56f, 0.13f, 0.10f, 0.98f),
                Vector2.zero, Vector2.one, new Vector2(5f, 5f), new Vector2(-5f, -5f));
            fill.raycastTarget = false;
            healthFill = fill.rectTransform;

            UIFactory.AddBarTicks(
                healthBg.transform,
                "HealthTick",
                8,
                new Color(0.08f, 0.075f, 0.055f, 0.72f));

            healthText = UIFactory.CreateText(healthBg.transform, "HealthText", "100  /  100", 19,
                TextAnchor.MiddleCenter, Color.white, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            healthText.raycastTarget = false;

            roomText = UIFactory.CreateText(canvas.transform, "RoomText", "CHAMBER  I / III", 17,
                TextAnchor.MiddleCenter, new Color(0.74f, 0.72f, 0.62f),
                new Vector2(0.34f, 0.928f), new Vector2(0.66f, 0.968f), Vector2.zero, Vector2.zero);

            runTimerText = UIFactory.CreateText(
                canvas.transform,
                "RunTimer",
                "RUN  00:00",
                13,
                TextAnchor.UpperRight,
                new Color(0.52f, 0.50f, 0.43f, 0.86f),
                new Vector2(0.72f, 0.925f),
                new Vector2(0.91f, 0.965f),
                Vector2.zero,
                Vector2.zero);
            runTimerText.raycastTarget = false;

            Image moralityBackground = UIFactory.CreatePanel(
                canvas.transform,
                "MoralityPanel",
                new Color(0.052f, 0.052f, 0.040f, 0.94f),
                new Vector2(0.790f, 0.735f),
                new Vector2(0.970f, 0.885f),
                Vector2.zero,
                Vector2.zero);
            moralityPanel = moralityBackground.gameObject;
            moralityBackground.raycastTarget = false;

            UIFactory.AddPixelFrame(
                moralityBackground.transform,
                "MoralityPixelFrame",
                new Color(0.42f, 0.39f, 0.29f, 0.88f),
                2f);

            UIFactory.CreatePanel(
                moralityBackground.transform,
                "MoralityBorder",
                new Color(0.20f, 0.19f, 0.14f, 0.18f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero).raycastTarget = false;

            mercyText = UIFactory.CreateText(
                moralityBackground.transform,
                "MercyText",
                "MERCY  0%",
                15,
                TextAnchor.MiddleLeft,
                new Color(0.48f, 0.64f, 0.50f),
                new Vector2(0.05f, 0.59f),
                new Vector2(0.45f, 0.91f),
                Vector2.zero,
                Vector2.zero);
            mercyText.raycastTarget = false;

            Image mercyBar = UIFactory.CreatePanel(
                moralityBackground.transform,
                "MercyBar",
                new Color(0.075f, 0.10f, 0.075f, 0.98f),
                new Vector2(0.45f, 0.64f),
                new Vector2(0.95f, 0.84f),
                Vector2.zero,
                Vector2.zero);
            mercyBar.raycastTarget = false;

            Image mercyBarFill = UIFactory.CreatePanel(
                mercyBar.transform,
                "MercyFill",
                new Color(0.34f, 0.55f, 0.36f, 0.98f),
                Vector2.zero,
                Vector2.one,
                new Vector2(2f, 2f),
                new Vector2(-2f, -2f));
            mercyBarFill.raycastTarget = false;
            mercyFill = mercyBarFill.rectTransform;

            killText = UIFactory.CreateText(
                moralityBackground.transform,
                "KillText",
                "SIN  0%",
                15,
                TextAnchor.MiddleLeft,
                new Color(0.72f, 0.36f, 0.24f),
                new Vector2(0.05f, 0.31f),
                new Vector2(0.45f, 0.59f),
                Vector2.zero,
                Vector2.zero);
            killText.raycastTarget = false;

            Image killBar = UIFactory.CreatePanel(
                moralityBackground.transform,
                "KillBar",
                new Color(0.13f, 0.070f, 0.045f, 0.98f),
                new Vector2(0.45f, 0.36f),
                new Vector2(0.95f, 0.56f),
                Vector2.zero,
                Vector2.zero);
            killBar.raycastTarget = false;

            Image killBarFill = UIFactory.CreatePanel(
                killBar.transform,
                "KillFill",
                new Color(0.62f, 0.24f, 0.14f, 0.98f),
                Vector2.zero,
                Vector2.one,
                new Vector2(2f, 2f),
                new Vector2(-2f, -2f));
            killBarFill.raycastTarget = false;
            killFill = killBarFill.rectTransform;

            moralityBonusText = UIFactory.CreateText(
                moralityBackground.transform,
                "MoralityBonus",
                "MERCY: DEF   SIN: ATK SPD",
                11,
                TextAnchor.MiddleCenter,
                new Color(0.64f, 0.68f, 0.70f, 0.88f),
                new Vector2(0.04f, 0.03f),
                new Vector2(0.96f, 0.29f),
                Vector2.zero,
                Vector2.zero);
            moralityBonusText.raycastTarget = false;

            Image mercyPrompt = UIFactory.CreatePanel(
                canvas.transform,
                "MercyPrompt",
                new Color(0.050f, 0.048f, 0.037f, 0.97f),
                new Vector2(0.36f, 0.17f),
                new Vector2(0.64f, 0.265f),
                Vector2.zero,
                Vector2.zero);
            mercyPromptPanel = mercyPrompt.gameObject;

            UIFactory.AddPixelFrame(
                mercyPrompt.transform,
                "MercyPromptFrame",
                new Color(0.50f, 0.45f, 0.31f, 0.92f),
                2f);

            mercyPromptText = UIFactory.CreateText(
                mercyPrompt.transform,
                "MercyPromptText",
                "",
                18,
                TextAnchor.MiddleCenter,
                new Color(0.94f, 0.92f, 0.82f),
                Vector2.zero,
                Vector2.one,
                new Vector2(8f, 4f),
                new Vector2(-8f, -4f));
            mercyPromptText.raycastTarget = false;
            mercyPromptPanel.SetActive(false);

            weaponText = UIFactory.CreateText(canvas.transform, "WeaponText", "DAGGER", 16,
                TextAnchor.LowerRight, new Color(0.78f, 0.76f, 0.66f),
                new Vector2(0.72f, 0.04f), new Vector2(0.97f, 0.17f), Vector2.zero, Vector2.zero);

            momentumText = UIFactory.CreateText(
                canvas.transform,
                "Momentum",
                "",
                17,
                TextAnchor.LowerRight,
                new Color(0.78f, 0.84f, 0.90f, 0f),
                new Vector2(0.72f, 0.225f),
                new Vector2(0.97f, 0.305f),
                Vector2.zero,
                Vector2.zero);
            momentumText.raycastTarget = false;

            Image momentumBackground = UIFactory.CreatePanel(
                canvas.transform,
                "MomentumTimer",
                new Color(0.045f, 0.035f, 0.025f, 0.86f),
                new Vector2(0.785f, 0.208f),
                new Vector2(0.968f, 0.220f),
                Vector2.zero,
                Vector2.zero);
            momentumBackground.raycastTarget = false;
            momentumTimerBar = momentumBackground.gameObject;

            momentumFillImage = UIFactory.CreatePanel(
                momentumBackground.transform,
                "MomentumFill",
                new Color(0.94f, 0.66f, 0.24f, 0.98f),
                Vector2.zero,
                Vector2.one,
                new Vector2(2f, 2f),
                new Vector2(-2f, -2f));
            momentumFillImage.raycastTarget = false;
            momentumFill = momentumFillImage.rectTransform;
            momentumTimerBar.SetActive(false);

            boostText = UIFactory.CreateText(canvas.transform, "BoostText", "BOOST  [SPACE]  READY", 15,
                TextAnchor.LowerLeft, new Color(0.57f, 0.63f, 0.48f),
                new Vector2(0.03f, 0.04f), new Vector2(0.34f, 0.11f), Vector2.zero, Vector2.zero);

            Image boostBarBackground = UIFactory.CreatePanel(
                canvas.transform,
                "BoostBarBackground",
                new Color(0.060f, 0.070f, 0.052f, 0.94f),
                new Vector2(0.03f, 0.029f),
                new Vector2(0.245f, 0.041f),
                Vector2.zero,
                Vector2.zero);
            boostBarBackground.raycastTarget = false;

            UIFactory.AddPixelFrame(
                boostBarBackground.transform,
                "BoostPixelFrame",
                new Color(0.38f, 0.40f, 0.29f, 0.86f),
                1f);

            boostFillImage = UIFactory.CreatePanel(
                boostBarBackground.transform,
                "BoostBarFill",
                new Color(0.37f, 0.53f, 0.34f, 0.98f),
                Vector2.zero,
                Vector2.one,
                new Vector2(2f, 2f),
                new Vector2(-2f, -2f));
            boostFillImage.raycastTarget = false;
            boostFill = boostFillImage.rectTransform;

            UIFactory.AddBarTicks(
                boostBarBackground.transform,
                "BoostTick",
                5,
                new Color(0.08f, 0.09f, 0.065f, 0.70f));

            BuildGuardHUD();

            crosshairText = UIFactory.CreateText(canvas.transform, "Crosshair", "+", 28, TextAnchor.MiddleCenter,
                new Color(0.92f, 0.95f, 0.98f, 0.78f),
                new Vector2(0.485f, 0.47f), new Vector2(0.515f, 0.53f), Vector2.zero, Vector2.zero);
            crosshairText.raycastTarget = false;

            hitmarkerText = UIFactory.CreateText(canvas.transform, "Hitmarker", "X", 22, TextAnchor.MiddleCenter,
                new Color(1f, 1f, 1f, 0f),
                new Vector2(0.485f, 0.47f), new Vector2(0.515f, 0.53f), Vector2.zero, Vector2.zero);
            hitmarkerText.raycastTarget = false;

            damageDirectionText = UIFactory.CreateText(
                canvas.transform,
                "DamageDirection",
                "^",
                24,
                TextAnchor.MiddleCenter,
                new Color(1f, 0.34f, 0.24f, 0f),
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                Vector2.zero);

            RectTransform damageDirectionRect = damageDirectionText.rectTransform;
            damageDirectionRect.sizeDelta = new Vector2(42f, 42f);
            damageDirectionRect.pivot = new Vector2(0.5f, 0.5f);
            damageDirectionText.raycastTarget = false;

            feedbackText = UIFactory.CreateText(canvas.transform, "Feedback", "", 19, TextAnchor.MiddleCenter,
                new Color(0.72f, 0.65f, 0.46f, 0f),
                new Vector2(0.38f, 0.39f), new Vector2(0.62f, 0.445f), Vector2.zero, Vector2.zero);
            feedbackText.raycastTarget = false;

            roomBannerText = UIFactory.CreateText(canvas.transform, "RoomBanner", "", 28, TextAnchor.MiddleCenter,
                new Color(0.82f, 0.80f, 0.70f, 0f),
                new Vector2(0.32f, 0.70f), new Vector2(0.68f, 0.80f), Vector2.zero, Vector2.zero);
            roomBannerText.raycastTarget = false;
            roomBannerBasePosition = roomBannerText.rectTransform.anchoredPosition;

            objectiveText = UIFactory.CreateText(
                canvas.transform,
                "Objective",
                "ENTER THE ANTECHAMBER",
                14,
                TextAnchor.UpperLeft,
                new Color(0.62f, 0.61f, 0.50f, 0.90f),
                new Vector2(0.03f, 0.825f),
                new Vector2(0.28f, 0.875f),
                Vector2.zero,
                Vector2.zero);
            objectiveText.raycastTarget = false;
            objectiveBasePosition = objectiveText.rectTransform.anchoredPosition;
            objectiveTargetColor = objectiveText.color;

            hintText = UIFactory.CreateText(canvas.transform, "Hint",
                "LMB  COMBO     Q  SWITCH     RMB  BLOCK / PARRY     SPACE  BOOST     B  BESTIARY     ESC  PAUSE",
                13, TextAnchor.LowerCenter, new Color(0.52f, 0.51f, 0.44f, 0.72f),
                new Vector2(0.18f, 0.018f), new Vector2(0.82f, 0.068f), Vector2.zero, Vector2.zero);

            BuildBossHUD();

            Button alwaysQuitButton = UIFactory.CreateButton(
                canvas.transform,
                "AlwaysQuitButton",
                "QUIT",
                new Vector2(1f, 1f),
                new Vector2(-68f, -31f),
                new Vector2(108f, 38f),
                QuitUtility.Quit,
                new Color(0.24f, 0.055f, 0.055f, 0.90f));

            BuildPausePanel();
            BuildUpgradePanel();
            BuildEndPanel();
            BuildBestiaryPanel();

            alwaysQuitButton.transform.SetAsLastSibling();
        }

        private void BuildGuardHUD()
        {
            Image panel = UIFactory.CreatePanel(
                canvas.transform,
                "GuardPanel",
                new Color(0.050f, 0.055f, 0.043f, 0.94f),
                new Vector2(0.755f, 0.165f),
                new Vector2(0.968f, 0.222f),
                Vector2.zero,
                Vector2.zero);

            guardPanel = panel.gameObject;

            UIFactory.AddPixelFrame(
                panel.transform,
                "GuardPixelFrame",
                new Color(0.40f, 0.40f, 0.29f, 0.88f),
                2f);

            Image background = UIFactory.CreatePanel(
                panel.transform,
                "GuardBarBackground",
                new Color(0.070f, 0.080f, 0.060f, 0.98f),
                new Vector2(0.04f, 0.14f),
                new Vector2(0.96f, 0.55f),
                Vector2.zero,
                Vector2.zero);

            Image fill = UIFactory.CreatePanel(
                background.transform,
                "GuardBarFill",
                new Color(0.38f, 0.50f, 0.38f, 0.98f),
                Vector2.zero,
                Vector2.one,
                new Vector2(2f, 2f),
                new Vector2(-2f, -2f));

            guardFill = fill.rectTransform;
            guardFillImage = fill;

            UIFactory.AddBarTicks(
                background.transform,
                "GuardTick",
                5,
                new Color(0.08f, 0.09f, 0.065f, 0.72f));

            guardText = UIFactory.CreateText(
                panel.transform,
                "GuardText",
                "GUARD  100",
                15,
                TextAnchor.UpperRight,
                new Color(0.66f, 0.69f, 0.56f),
                new Vector2(0.04f, 0.55f),
                new Vector2(0.96f, 1f),
                Vector2.zero,
                Vector2.zero);

            panel.raycastTarget = false;
            background.raycastTarget = false;
            fill.raycastTarget = false;
            guardText.raycastTarget = false;
            guardPanel.SetActive(false);
        }

        private void BuildBossHUD()
        {
            Image panel = UIFactory.CreatePanel(
                canvas.transform,
                "BossPanel",
                new Color(0.060f, 0.045f, 0.034f, 0.96f),
                new Vector2(0.335f, 0.840f),
                new Vector2(0.665f, 0.895f),
                Vector2.zero,
                Vector2.zero);

            bossPanel = panel.gameObject;
            bossPanelImage = panel;

            UIFactory.AddPixelFrame(
                panel.transform,
                "BossPixelFrame",
                new Color(0.48f, 0.31f, 0.20f, 0.92f),
                2f);

            bossText = UIFactory.CreateText(
                panel.transform,
                "BossName",
                "WARDEN",
                15,
                TextAnchor.UpperCenter,
                new Color(0.76f, 0.57f, 0.42f),
                new Vector2(0f, 0.52f),
                new Vector2(1f, 1f),
                Vector2.zero,
                Vector2.zero);

            Image barBackground = UIFactory.CreatePanel(
                panel.transform,
                "BossBarBackground",
                new Color(0.115f, 0.070f, 0.050f, 0.98f),
                new Vector2(0.06f, 0.15f),
                new Vector2(0.94f, 0.46f),
                Vector2.zero,
                Vector2.zero);

            Image bossTrail = UIFactory.CreatePanel(
                barBackground.transform,
                "BossBarTrail",
                new Color(0.66f, 0.40f, 0.20f, 0.92f),
                Vector2.zero,
                Vector2.one,
                new Vector2(2f, 2f),
                new Vector2(-2f, -2f));

            bossTrail.raycastTarget = false;
            bossTrailFill = bossTrail.rectTransform;

            Image fill = UIFactory.CreatePanel(
                barBackground.transform,
                "BossBarFill",
                new Color(0.54f, 0.14f, 0.09f, 0.98f),
                Vector2.zero,
                Vector2.one,
                new Vector2(2f, 2f),
                new Vector2(-2f, -2f));

            bossFill = fill.rectTransform;
            bossFillImage = fill;

            UIFactory.AddBarTicks(
                barBackground.transform,
                "BossTick",
                10,
                new Color(0.06f, 0.035f, 0.025f, 0.78f));

            Image phaseMarker = UIFactory.CreatePanel(
                barBackground.transform,
                "PhaseTwoMarker",
                new Color(0.74f, 0.49f, 0.24f, 0.90f),
                new Vector2(0.496f, 0.02f),
                new Vector2(0.504f, 0.98f),
                Vector2.zero,
                Vector2.zero);

            phaseMarker.raycastTarget = false;
            bossText.raycastTarget = false;
            barBackground.raycastTarget = false;
            fill.raycastTarget = false;
            bossPanel.SetActive(false);
        }

        private void BuildPausePanel()
        {
            Image backdrop = UIFactory.CreatePanel(
                canvas.transform,
                "PauseBackdrop",
                new Color(0.010f, 0.009f, 0.006f, 0.58f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            backdrop.raycastTarget = false;
            pauseBackdrop = backdrop.gameObject;

            Image panel = UIFactory.CreatePanel(canvas.transform, "PausePanel",
                new Color(0.052f, 0.050f, 0.038f, 0.985f),
                new Vector2(0.30f, 0.10f), new Vector2(0.70f, 0.90f), Vector2.zero, Vector2.zero);
            pausePanel = panel.gameObject;

            UIFactory.AddPixelFrame(
                panel.transform,
                "PausePixelFrame",
                new Color(0.46f, 0.42f, 0.30f, 0.94f),
                3f);

            UIFactory.CreateText(panel.transform, "PauseTitle", "PAUSED", 40, TextAnchor.MiddleCenter,
                new Color(0.84f, 0.81f, 0.68f), new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.97f), Vector2.zero, Vector2.zero);

            UIFactory.CreateText(panel.transform, "PauseSub", "THE DUNGEON WAITS", 18, TextAnchor.MiddleCenter,
                new Color(0.58f, 0.56f, 0.47f),
                new Vector2(0.08f, 0.79f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);

            settingsSensitivityText = UIFactory.CreateText(
                panel.transform,
                "SensitivityValue",
                "",
                18,
                TextAnchor.MiddleCenter,
                new Color(0.68f, 0.66f, 0.56f),
                new Vector2(0.30f, 0.67f),
                new Vector2(0.70f, 0.73f),
                Vector2.zero,
                Vector2.zero);

            UIFactory.CreateButton(panel.transform, "SensitivityMinus", "-",
                new Vector2(0.23f, 0.70f), Vector2.zero, new Vector2(52f, 40f),
                () => AdjustSensitivity(-0.10f),
                new Color(0.090f, 0.095f, 0.070f, 0.98f));

            UIFactory.CreateButton(panel.transform, "SensitivityPlus", "+",
                new Vector2(0.77f, 0.70f), Vector2.zero, new Vector2(52f, 40f),
                () => AdjustSensitivity(0.10f),
                new Color(0.090f, 0.095f, 0.070f, 0.98f));

            settingsShakeText = UIFactory.CreateText(
                panel.transform,
                "ShakeValue",
                "",
                18,
                TextAnchor.MiddleCenter,
                new Color(0.68f, 0.66f, 0.56f),
                new Vector2(0.30f, 0.37f),
                new Vector2(0.70f, 0.43f),
                Vector2.zero,
                Vector2.zero);

            UIFactory.CreateButton(
                panel.transform,
                "ShakeMinus",
                "-",
                new Vector2(0.23f, 0.40f),
                Vector2.zero,
                new Vector2(52f, 40f),
                () => AdjustCameraShake(-0.25f),
                new Color(0.090f, 0.095f, 0.070f, 0.98f));

            UIFactory.CreateButton(
                panel.transform,
                "ShakePlus",
                "+",
                new Vector2(0.77f, 0.40f),
                Vector2.zero,
                new Vector2(52f, 40f),
                () => AdjustCameraShake(0.25f),
                new Color(0.090f, 0.095f, 0.070f, 0.98f));

            settingsDisplayText = UIFactory.CreateText(
                panel.transform,
                "DisplayValue",
                "",
                18,
                TextAnchor.MiddleCenter,
                new Color(0.68f, 0.66f, 0.56f),
                new Vector2(0.26f, 0.47f),
                new Vector2(0.74f, 0.53f),
                Vector2.zero,
                Vector2.zero);

            Button fullButton = UIFactory.CreateButton(
                panel.transform,
                "DisplayToggle",
                "FULL",
                new Vector2(0.78f, 0.50f),
                Vector2.zero,
                new Vector2(70f, 36f),
                () => ToggleDisplay(),
                new Color(0.090f, 0.095f, 0.070f, 0.98f));

            // 单独缩小 FULL 字号，不影响其他按钮。
            fullButton.GetComponentInChildren<Text>().fontSize = 14;

            settingsVolumeText = UIFactory.CreateText(
                panel.transform,
                "VolumeValue",
                "",
                18,
                TextAnchor.MiddleCenter,
                new Color(0.68f, 0.66f, 0.56f),
                new Vector2(0.30f, 0.57f),
                new Vector2(0.70f, 0.63f),
                Vector2.zero,
                Vector2.zero);

            UIFactory.CreateButton(panel.transform, "VolumeMinus", "-",
                new Vector2(0.23f, 0.60f), Vector2.zero, new Vector2(52f, 40f),
                () => AdjustVolume(-0.10f),
                new Color(0.090f, 0.095f, 0.070f, 0.98f));

            UIFactory.CreateButton(panel.transform, "VolumePlus", "+",
                new Vector2(0.77f, 0.60f), Vector2.zero, new Vector2(52f, 40f),
                () => AdjustVolume(0.10f),
                new Color(0.090f, 0.095f, 0.070f, 0.98f));

            UIFactory.CreateButton(panel.transform, "Resume", "RESUME",
                new Vector2(0.5f, 0.315f), Vector2.zero, new Vector2(300f, 46f),
                () => SetPaused(false));

            UIFactory.CreateButton(panel.transform, "RestartRun", "RESTART RUN",
                new Vector2(0.5f, 0.235f), Vector2.zero, new Vector2(300f, 46f),
                QuitUtility.LoadGame,
                new Color(0.090f, 0.095f, 0.070f, 0.98f));

            UIFactory.CreateButton(panel.transform, "MainMenu", "MAIN MENU",
                new Vector2(0.5f, 0.155f), Vector2.zero, new Vector2(300f, 46f),
                QuitUtility.LoadMenu);

            UIFactory.CreateButton(panel.transform, "QuitDesktop", "QUIT TO DESKTOP",
                new Vector2(0.5f, 0.075f), Vector2.zero, new Vector2(300f, 46f),
                QuitUtility.Quit, new Color(0.30f, 0.090f, 0.060f, 0.98f));

            RefreshSettingsText();
            pauseBackdrop.SetActive(false);
            pausePanel.SetActive(false);
        }

        private void AdjustSensitivity(float delta)
        {
            RuntimeSettings.AdjustMouseScale(delta);
            RefreshSettingsText();
        }

        private void AdjustVolume(float delta)
        {
            RuntimeSettings.AdjustMasterVolume(delta);
            RefreshSettingsText();
        }

        private void AdjustCameraShake(float delta)
        {
            RuntimeSettings.AdjustCameraShake(delta);
            RefreshSettingsText();
        }

        private void ToggleDisplay()
        {
            RuntimeSettings.ToggleFullscreen();
            RefreshSettingsText();
        }

        private void RefreshSettingsText()
        {
            if (settingsSensitivityText != null)
            {
                settingsSensitivityText.text =
                    "MOUSE SENSITIVITY   " +
                    RuntimeSettings.MouseScale.ToString("0.0") +
                    "x";
            }

            if (settingsVolumeText != null)
            {
                settingsVolumeText.text =
                    "MASTER VOLUME   " +
                    Mathf.RoundToInt(RuntimeSettings.MasterVolume * 100f) +
                    "%";
            }

            if (settingsDisplayText != null)
            {
                settingsDisplayText.text =
                    RuntimeSettings.Fullscreen
                        ? "DISPLAY   FULLSCREEN"
                        : "DISPLAY   WINDOWED";
            }

            if (settingsShakeText != null)
            {
                settingsShakeText.text =
                    "CAMERA SHAKE   " +
                    Mathf.RoundToInt(RuntimeSettings.CameraShakeScale * 100f) +
                    "%";
            }
        }

        private void BuildUpgradePanel()
        {
            Image backdrop = UIFactory.CreatePanel(
                canvas.transform,
                "UpgradeBackdrop",
                new Color(0.010f, 0.009f, 0.006f, 0.64f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            backdrop.raycastTarget = false;
            upgradeBackdrop = backdrop.gameObject;

            Image panel = UIFactory.CreatePanel(canvas.transform, "UpgradePanel",
                new Color(0.050f, 0.048f, 0.036f, 0.992f),
                new Vector2(0.17f, 0.22f), new Vector2(0.83f, 0.78f), Vector2.zero, Vector2.zero);
            upgradePanel = panel.gameObject;

            UIFactory.AddPixelFrame(
                panel.transform,
                "UpgradePixelFrame",
                new Color(0.46f, 0.42f, 0.30f, 0.94f),
                3f);

            UIFactory.CreateText(panel.transform, "UpgradeTitle", "CHOOSE YOUR BOON", 34,
                TextAnchor.MiddleCenter, new Color(0.84f, 0.81f, 0.68f),
                new Vector2(0.05f, 0.79f), new Vector2(0.95f, 0.95f), Vector2.zero, Vector2.zero);

            UIFactory.CreateText(panel.transform, "UpgradeSub", "The next chamber will be less forgiving.", 19,
                TextAnchor.MiddleCenter, new Color(0.59f, 0.57f, 0.48f),
                new Vector2(0.05f, 0.70f), new Vector2(0.95f, 0.80f), Vector2.zero, Vector2.zero);

            Button vitalityButton = UIFactory.CreateButton(
                panel.transform,
                "Vitality",
                "[1]  VITALITY\n+25 MAX HP",
                new Vector2(0.25f, 0.5f),
                new Vector2(0f, -35f),
                new Vector2(290f, 155f),
                () => ChooseUpgrade(UpgradeChoice.Vitality),
                new Color(0.23f, 0.10f, 0.08f, 0.98f));

            Button brutalityButton = UIFactory.CreateButton(
                panel.transform,
                "Brutality",
                "[2]  BRUTALITY\n+25% DAMAGE",
                new Vector2(0.50f, 0.5f),
                new Vector2(0f, -35f),
                new Vector2(290f, 155f),
                () => ChooseUpgrade(UpgradeChoice.Brutality),
                new Color(0.22f, 0.15f, 0.07f, 0.98f));

            Button mobilityButton = UIFactory.CreateButton(
                panel.transform,
                "Mobility",
                "[3]  MOBILITY\n+12% SPEED  |  -12% BOOST CD",
                new Vector2(0.75f, 0.5f),
                new Vector2(0f, -35f),
                new Vector2(290f, 155f),
                () => ChooseUpgrade(UpgradeChoice.Mobility),
                new Color(0.10f, 0.18f, 0.14f, 0.98f));

            AddBoonAccent(
                vitalityButton.transform,
                new Color(0.64f, 0.27f, 0.20f, 0.98f));

            AddBoonAccent(
                brutalityButton.transform,
                new Color(0.67f, 0.46f, 0.20f, 0.98f));

            AddBoonAccent(
                mobilityButton.transform,
                new Color(0.34f, 0.55f, 0.39f, 0.98f));

            upgradeBackdrop.SetActive(false);
            upgradePanel.SetActive(false);
        }

        private void AddBoonAccent(Transform parent, Color color)
        {
            Image accent = UIFactory.CreatePanel(
                parent,
                "Accent",
                color,
                new Vector2(0.06f, 0.92f),
                new Vector2(0.94f, 0.965f),
                Vector2.zero,
                Vector2.zero);

            accent.raycastTarget = false;
        }

        private void BuildBestiaryPanel()
        {
            Image backdrop = UIFactory.CreatePanel(
                canvas.transform,
                "BestiaryBackdrop",
                new Color(0.010f, 0.014f, 0.012f, 0.82f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            bestiaryBackdrop = backdrop.gameObject;

            Image panel = UIFactory.CreatePanel(
                canvas.transform,
                "BestiaryPanel",
                new Color(0.045f, 0.052f, 0.040f, 0.995f),
                new Vector2(0.16f, 0.08f),
                new Vector2(0.84f, 0.92f),
                Vector2.zero,
                Vector2.zero);
            bestiaryPanel = panel.gameObject;

            UIFactory.AddPixelFrame(
                panel.transform,
                "BestiaryPixelFrame",
                new Color(0.48f, 0.46f, 0.31f, 0.96f),
                3f);

            UIFactory.CreateText(
                panel.transform,
                "BestiaryTitle",
                "DEPTHBOUND BESTIARY",
                34,
                TextAnchor.MiddleCenter,
                new Color(0.86f, 0.82f, 0.63f),
                new Vector2(0.06f, 0.88f),
                new Vector2(0.94f, 0.98f),
                Vector2.zero,
                Vector2.zero);

            bestiaryBody = UIFactory.CreateText(
                panel.transform,
                "BestiaryBody",
                "",
                16,
                TextAnchor.UpperLeft,
                new Color(0.72f, 0.74f, 0.65f),
                new Vector2(0.07f, 0.12f),
                new Vector2(0.93f, 0.87f),
                Vector2.zero,
                Vector2.zero);
            bestiaryBody.horizontalOverflow =
                HorizontalWrapMode.Wrap;
            bestiaryBody.verticalOverflow =
                VerticalWrapMode.Truncate;

            UIFactory.CreateButton(
                panel.transform,
                "BestiaryClose",
                "CLOSE  [B / ESC]",
                new Vector2(0.5f, 0.5f),
                new Vector2(0f, -330f),
                new Vector2(260f, 46f),
                () => SetBestiary(false),
                new Color(0.20f, 0.25f, 0.17f, 0.96f));

            bestiaryBackdrop.SetActive(false);
            bestiaryPanel.SetActive(false);
        }

        public void ShowBestiaryDiscovery(string entryName)
        {
            ShowCombatFeedback(
                "BESTIARY UPDATED - " + entryName,
                new Color(0.72f, 0.78f, 0.48f),
                1.65f);
        }

        private void BuildEndPanel()
        {
            Image backdrop = UIFactory.CreatePanel(
                canvas.transform,
                "EndBackdrop",
                new Color(0.010f, 0.009f, 0.006f, 0.72f),
                Vector2.zero,
                Vector2.one,
                Vector2.zero,
                Vector2.zero);
            backdrop.raycastTarget = false;
            endBackdrop = backdrop.gameObject;
            endBackdropImage = backdrop;

            Image panel = UIFactory.CreatePanel(canvas.transform, "EndPanel",
                new Color(0.048f, 0.046f, 0.034f, 0.995f),
                new Vector2(0.27f, 0.12f), new Vector2(0.73f, 0.88f), Vector2.zero, Vector2.zero);
            endPanel = panel.gameObject;

            UIFactory.AddPixelFrame(
                panel.transform,
                "EndPixelFrame",
                new Color(0.46f, 0.42f, 0.30f, 0.94f),
                3f);

            endAccent = UIFactory.CreatePanel(
                panel.transform,
                "EndAccent",
                new Color(0.40f, 0.58f, 0.38f, 0.95f),
                new Vector2(0.08f, 0.94f),
                new Vector2(0.92f, 0.965f),
                Vector2.zero,
                Vector2.zero);
            endAccent.raycastTarget = false;

            endTitle = UIFactory.CreateText(panel.transform, "EndTitle", "RUN COMPLETE", 38,
                TextAnchor.MiddleCenter, new Color(0.84f, 0.81f, 0.68f),
                new Vector2(0.05f, 0.79f), new Vector2(0.95f, 0.92f), Vector2.zero, Vector2.zero);

            endBody = UIFactory.CreateText(panel.transform, "EndBody", "", 21,
                TextAnchor.MiddleCenter, new Color(0.68f, 0.66f, 0.56f),
                new Vector2(0.07f, 0.34f), new Vector2(0.93f, 0.76f), Vector2.zero, Vector2.zero);

            UIFactory.CreateButton(panel.transform, "Restart", "DESCEND AGAIN",
                new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(300f, 52f),
                () => SceneFadeController.Load("Game"));

            UIFactory.CreateButton(panel.transform, "EndMenu", "MAIN MENU",
                new Vector2(0.5f, 0.11f), Vector2.zero, new Vector2(300f, 52f),
                QuitUtility.LoadMenu);

            endBackdrop.SetActive(false);
            endPanel.SetActive(false);
        }

        private static void UpdateTimedText(Text text, ref float timer, float dt, float fadeDuration)
        {
            if (text == null) return;

            if (timer > 0f)
            {
                timer = Mathf.Max(0f, timer - dt);
                float alpha = Mathf.Clamp01(timer / Mathf.Max(0.01f, fadeDuration));
                SetTextAlpha(text, alpha);
            }
            else
            {
                SetTextAlpha(text, 0f);
            }
        }

        private static void SetTextAlpha(Text text, float alpha)
        {
            Color c = text.color;
            c.a = alpha;
            text.color = c;
        }

        private static string ToRoman(int number)
        {
            switch (number)
            {
                case 1: return "I";
                case 2: return "II";
                case 3: return "III";
                case 4: return "IV";
                case 5: return "V";
                default: return number.ToString();
            }
        }

        private void EnsureEventSystem()
        {
            if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() == null)
            {
                new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
            }
        }
    }
}

