using UnityEngine;

namespace Depthbound
{
    public class PlayerHealth : MonoBehaviour
    {
        public float MaxHealth { get; private set; } = 100f;
        public float CurrentHealth { get; private set; }

        private float lastDamageTime = -99f;
        private PlayerCombat combat;
        private FirstPersonController controller;

        private void Awake()
        {
            CurrentHealth = MaxHealth;
            combat = GetComponent<PlayerCombat>();
            controller = GetComponent<FirstPersonController>();
        }

        private void Start()
        {
            PushHUD();
        }

        public void TakeDamage(
            float damage,
            Vector3 sourcePosition,
            EnemyController sourceEnemy = null)
        {
            if (CurrentHealth <= 0f || Time.time < lastDamageTime + 0.18f) return;

            if (controller != null && controller.IsBoostInvulnerable)
            {
                lastDamageTime = Time.time;

                if (HUDController.Instance != null)
                {
                    HUDController.Instance.ShowEvade();
                }

                CombatEffects.SpawnEvade(transform.position);

                if (DungeonDirector.Instance != null)
                {
                    DungeonDirector.Instance.RecordEvade();
                }

                return;
            }

            lastDamageTime = Time.time;
            bool blocked = combat != null && combat.TryBlock(sourcePosition, damage);
            if (blocked)
            {
                bool perfect = combat != null && combat.LastBlockWasPerfect;
                damage *= perfect ? 0f : 0.20f;

                if (ProceduralAudio.Instance != null)
                {
                    if (perfect) ProceduralAudio.Instance.Parry();
                    else ProceduralAudio.Instance.Block();
                }

                if (HUDController.Instance != null)
                {
                    if (perfect) HUDController.Instance.ShowParry();
                    else HUDController.Instance.ShowBlock();
                }

                Vector3 blockEffectPosition =
                    transform.position +
                    Vector3.up * 1.25f +
                    transform.forward * 0.55f;

                if (perfect)
                {
                    CombatEffects.SpawnParry(blockEffectPosition, transform.forward);
                }
                else
                {
                    CombatEffects.SpawnBlock(blockEffectPosition, transform.forward);
                }

                if (perfect)
                {
                    if (DungeonDirector.Instance != null)
                    {
                        DungeonDirector.Instance.RecordPerfectBlock();
                    }

                    if (sourceEnemy != null)
                    {
                        sourceEnemy.ApplyParryStagger(transform.position);
                    }
                }

                if (controller != null)
                {
                    controller.AddCameraImpact(perfect ? 0.72f : 0.55f, -0.3f);
                }
            }
            else
            {
                if (ProceduralAudio.Instance != null) ProceduralAudio.Instance.Hurt();
                if (HUDController.Instance != null)
                {
                    HUDController.Instance.ShowDamage(Mathf.Clamp01(damage / 30f));
                    HUDController.Instance.ShowDamageDirection(sourcePosition, transform);
                }
                if (controller != null)
                {
                    float direction = Vector3.Dot(transform.right, sourcePosition - transform.position) >= 0f ? 1f : -1f;
                    controller.AddCameraImpact(Mathf.Clamp(damage / 18f, 0.7f, 1.6f), direction);
                }
            }

            if (RunMorality.Instance != null)
            {
                damage *= RunMorality.Instance.DefenseMultiplier;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);

            if (damage > 0.01f && combat != null)
            {
                combat.BreakMomentum();
            }

            PushHUD();

            if (CurrentHealth <= 0f && DungeonDirector.Instance != null)
            {
                DungeonDirector.Instance.OnPlayerDied();
            }
        }

        public void TakeEnvironmentalDamage(float damage, Vector3 sourcePosition)
        {
            if (CurrentHealth <= 0f || Time.time < lastDamageTime + 0.18f) return;

            if (controller != null && controller.IsBoostInvulnerable)
            {
                lastDamageTime = Time.time;

                if (HUDController.Instance != null)
                {
                    HUDController.Instance.ShowEvade();
                }

                CombatEffects.SpawnEvade(transform.position);

                if (DungeonDirector.Instance != null)
                {
                    DungeonDirector.Instance.RecordEvade();
                }

                return;
            }

            lastDamageTime = Time.time;

            if (ProceduralAudio.Instance != null)
            {
                ProceduralAudio.Instance.Hurt();
            }

            if (HUDController.Instance != null)
            {
                HUDController.Instance.ShowDamage(Mathf.Clamp01(damage / 30f));
                HUDController.Instance.ShowDamageDirection(sourcePosition, transform);
            }

            if (controller != null)
            {
                float direction = Vector3.Dot(
                    transform.right,
                    sourcePosition - transform.position) >= 0f ? 1f : -1f;

                controller.AddCameraImpact(
                    Mathf.Clamp(damage / 18f, 0.8f, 1.7f),
                    direction);
            }

            if (RunMorality.Instance != null)
            {
                damage *= RunMorality.Instance.DefenseMultiplier;
            }

            CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);

            if (damage > 0.01f && combat != null)
            {
                combat.BreakMomentum();
            }

            PushHUD();

            if (CurrentHealth <= 0f && DungeonDirector.Instance != null)
            {
                DungeonDirector.Instance.OnPlayerDied();
            }
        }

        public void IncreaseMaxHealth(float amount)
        {
            MaxHealth += amount;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            PushHUD();
        }

        public void Heal(float amount)
        {
            if (amount <= 0f || CurrentHealth <= 0f || CurrentHealth >= MaxHealth) return;

            float before = CurrentHealth;
            CurrentHealth = Mathf.Min(MaxHealth, CurrentHealth + amount);
            float gained = CurrentHealth - before;

            PushHUD();

            if (gained > 0.01f && HUDController.Instance != null)
            {
                HUDController.Instance.ShowHeal(gained);
            }
        }

        private void PushHUD()
        {
            if (HUDController.Instance != null)
            {
                HUDController.Instance.SetHealth(CurrentHealth, MaxHealth);
            }
        }
    }
}
