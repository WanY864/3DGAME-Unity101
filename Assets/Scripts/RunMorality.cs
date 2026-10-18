using System.Collections.Generic;
using UnityEngine;

namespace Depthbound
{
    public enum EnemyArchetype
    {
        Mole = 0,
        Armored = 1,
        Mech = 2,
        Skeleton = 3,
        Bat = 4,
        Slime = 5
    }

    public class RunMorality : MonoBehaviour
    {
        public static RunMorality Instance { get; private set; }

        private const float ChoiceStep = 0.14f;
        private const float MercyCarryThreshold = 0.80f;
        private const float DoctrineTierOne = 0.35f;
        private const float DoctrineTierTwo = 0.70f;

        public float Mercy01 { get; private set; }
        public float Kill01 { get; private set; }
        public int SparedCount { get; private set; }
        public int KilledCount { get; private set; }
        public int CurrentAct { get; private set; } = 1;

        public float DefenseMultiplier =>
            Mathf.Lerp(1f, 0.74f, Mercy01);

        public float AttackIntervalMultiplier =>
            1f / Mathf.Lerp(1f, 1.30f, Kill01);

        public bool BloodRushActive =>
            Time.time < bloodRushUntil;

        public float OutgoingDamageMultiplier =>
            !BloodRushActive
                ? 1f
                : Kill01 >= DoctrineTierTwo
                    ? 1.28f
                    : 1.16f;

        public float ImpactMultiplier =>
            !BloodRushActive
                ? 1f
                : Kill01 >= DoctrineTierTwo
                    ? 1.22f
                    : 1.10f;

        public float PerfectBlockGuardBonus =>
            Mercy01 >= DoctrineTierTwo
                ? 16f
                : Mercy01 >= DoctrineTierOne
                    ? 8f
                    : 0f;

        private float SpareHealFraction =>
            Mercy01 >= DoctrineTierTwo
                ? 0.08f
                : Mercy01 >= DoctrineTierOne
                    ? 0.04f
                    : 0f;

        private float FinishBoostRefund =>
            Kill01 >= DoctrineTierTwo
                ? 0.78f
                : Kill01 >= DoctrineTierOne
                    ? 0.42f
                    : 0f;

        private float BloodRushDuration =>
            Kill01 >= DoctrineTierTwo
                ? 4.5f
                : Kill01 >= DoctrineTierOne
                    ? 3.0f
                    : 0f;

        public IReadOnlyList<EnemyArchetype> SparedArchetypes =>
            sparedArchetypes;

        private readonly List<EnemyArchetype> sparedArchetypes =
            new List<EnemyArchetype>();

        private readonly List<EnemyController> mercyQueue =
            new List<EnemyController>();

        private EnemyController activeMercyTarget;
        private PlayerHealth playerHealth;
        private FirstPersonController playerController;
        private float bloodRushUntil = -99f;

        private void Awake()
        {
            Instance = this;
            Mercy01 = 0f;
            Kill01 = 0f;
        }

        private void Start()
        {
            PushHUD();
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
            CleanupQueue();

            if (HUDController.Instance != null &&
                HUDController.Instance.IsModalOpen)
            {
                return;
            }

            if (activeMercyTarget == null && mercyQueue.Count > 0)
            {
                activeMercyTarget = mercyQueue[0];
                mercyQueue.RemoveAt(0);

                if (HUDController.Instance != null &&
                    activeMercyTarget != null)
                {
                    HUDController.Instance.ShowMercyPrompt(
                        activeMercyTarget.Archetype);
                }
            }

            if (activeMercyTarget == null)
            {
                return;
            }

            if (Input.GetKeyDown(KeyCode.Y))
            {
                ResolveActive(true);
            }
            else if (Input.GetKeyDown(KeyCode.X))
            {
                ResolveActive(false);
            }
        }

        public void RegisterMercyCandidate(EnemyController enemy)
        {
            if (enemy == null ||
                activeMercyTarget == enemy ||
                mercyQueue.Contains(enemy))
            {
                return;
            }

            mercyQueue.Add(enemy);
        }

        public void UnregisterMercyCandidate(EnemyController enemy)
        {
            if (enemy == null)
            {
                return;
            }

            mercyQueue.Remove(enemy);

            if (activeMercyTarget == enemy)
            {
                activeMercyTarget = null;

                if (HUDController.Instance != null)
                {
                    HUDController.Instance.HideMercyPrompt();
                }
            }
        }

        public void RecordSpare(EnemyArchetype archetype)
        {
            SparedCount++;

            Mercy01 = Mathf.Clamp01(Mercy01 + ChoiceStep);
            Kill01 = Mathf.Clamp01(Kill01 - ChoiceStep);

            sparedArchetypes.Add(archetype);

            EnsurePlayerReferences();

            float healed = 0f;

            if (playerHealth != null &&
                SpareHealFraction > 0f)
            {
                float before =
                    playerHealth.CurrentHealth;

                playerHealth.Heal(
                    playerHealth.MaxHealth *
                    SpareHealFraction);

                healed =
                    playerHealth.CurrentHealth -
                    before;
            }

            PushHUD();

            if (HUDController.Instance != null)
            {
                string message =
                    healed > 0.1f
                        ? "SPARED - RENEW +" +
                          Mathf.CeilToInt(healed) +
                          " HP"
                        : "SPARED - MERCY RISES";

                HUDController.Instance.ShowMoralityFeedback(
                    message,
                    true);
            }
        }

        public void RecordKill(EnemyArchetype archetype)
        {
            KilledCount++;

            Kill01 = Mathf.Clamp01(Kill01 + ChoiceStep);
            Mercy01 = Mathf.Clamp01(Mercy01 - ChoiceStep);

            EnsurePlayerReferences();

            float rushDuration =
                BloodRushDuration;

            if (rushDuration > 0f)
            {
                bloodRushUntil =
                    Mathf.Max(
                        bloodRushUntil,
                        Time.time +
                        rushDuration);

                if (playerController != null)
                {
                    playerController.RefundBoostCooldown(
                        FinishBoostRefund);
                }
            }

            PushHUD();

            if (HUDController.Instance != null)
            {
                string message =
                    rushDuration > 0f
                        ? "FINISHED - BLOOD RUSH " +
                          rushDuration.ToString("0.0") +
                          "S"
                        : "FINISHED - SIN RISES";

                HUDController.Instance.ShowMoralityFeedback(
                    message,
                    false);
            }
        }

        public bool CommitActBoundary(int completedAct)
        {
            if (completedAct < 1 || completedAct >= 3)
            {
                return false;
            }

            bool mercyCarries =
                Mercy01 >= MercyCarryThreshold;

            Mercy01 = mercyCarries ? 1f : 0f;
            Kill01 = 0f;
            bloodRushUntil = -99f;
            CurrentAct = completedAct + 1;

            sparedArchetypes.Clear();
            mercyQueue.Clear();
            activeMercyTarget = null;

            if (HUDController.Instance != null)
            {
                HUDController.Instance.HideMercyPrompt();
                HUDController.Instance.ShowActMoralityTransition(
                    mercyCarries,
                    CurrentAct);
            }

            PushHUD();
            return mercyCarries;
        }

        public string BuildSummary()
        {
            int defensePercent =
                Mathf.RoundToInt(
                    (1f - DefenseMultiplier) * 100f);

            int speedPercent =
                Mathf.RoundToInt(
                    (1f /
                     AttackIntervalMultiplier -
                     1f) *
                    100f);

            return
                "Mercy " +
                Mathf.RoundToInt(Mercy01 * 100f) +
                "%" +
                "   Sin " +
                Mathf.RoundToInt(Kill01 * 100f) +
                "%" +
                "\nSpared " +
                SparedCount +
                "   Finished " +
                KilledCount +
                "\nDefense +" +
                defensePercent +
                "%" +
                "   Attack speed +" +
                speedPercent +
                "%" +
                "\nMercy doctrine  " +
                MercyDoctrineLabel() +
                "   Sin doctrine  " +
                SinDoctrineLabel() +
                "\nPath  " +
                BuildPathLabel();
        }

        private string BuildPathLabel()
        {
            int difference =
                SparedCount - KilledCount;

            if (difference >= 4)
            {
                return "MERCIFUL";
            }

            if (difference <= -4)
            {
                return "RUTHLESS";
            }

            return "UNCERTAIN";
        }

        private string MercyDoctrineLabel()
        {
            if (Mercy01 >= DoctrineTierTwo)
            {
                return "RENEW II";
            }

            if (Mercy01 >= DoctrineTierOne)
            {
                return "RENEW I";
            }

            return "DORMANT";
        }

        private string SinDoctrineLabel()
        {
            if (Kill01 >= DoctrineTierTwo)
            {
                return "BLOOD RUSH II";
            }

            if (Kill01 >= DoctrineTierOne)
            {
                return "BLOOD RUSH I";
            }

            return "DORMANT";
        }

        private void EnsurePlayerReferences()
        {
            if (playerHealth == null)
            {
                playerHealth =
                    Object.FindFirstObjectByType<
                        PlayerHealth>();
            }

            if (playerController == null)
            {
                playerController =
                    Object.FindFirstObjectByType<
                        FirstPersonController>();
            }
        }

        private void ResolveActive(bool spare)
        {
            EnemyController target = activeMercyTarget;
            activeMercyTarget = null;

            if (HUDController.Instance != null)
            {
                HUDController.Instance.HideMercyPrompt();
            }

            if (target != null)
            {
                target.ResolveMercy(spare);
            }
        }

        private void CleanupQueue()
        {
            for (int i = mercyQueue.Count - 1; i >= 0; i--)
            {
                if (mercyQueue[i] == null)
                {
                    mercyQueue.RemoveAt(i);
                }
            }

            if (activeMercyTarget != null &&
                !activeMercyTarget.IsAwaitingMercy)
            {
                activeMercyTarget = null;

                if (HUDController.Instance != null)
                {
                    HUDController.Instance.HideMercyPrompt();
                }
            }
        }

        private void PushHUD()
        {
            if (HUDController.Instance == null)
            {
                return;
            }

            float defenseBonus =
                (1f - DefenseMultiplier);

            float speedBonus =
                (1f / AttackIntervalMultiplier - 1f);

            HUDController.Instance.SetMorality(
                Mercy01,
                Kill01,
                defenseBonus,
                speedBonus);
        }
    }
}
