using System.Text;
using UnityEngine;

namespace Depthbound
{
    /// Persistent discovery log shared by every run. Entries unlock when an
    /// enemy is actually configured in a chamber, never from static defaults.

    public static class BestiaryCatalog
    {
        private const string Prefix = "Depthbound.Bestiary.";

        private static readonly string[] CreatureNames =
        {
            "MOLE", "ARMORED SOLDIER", "MECH SOLDIER",
            "SKELETON", "BAT", "SLIME"
        };

        private static readonly string[] CreatureNotes =
        {
            "Fast burrow-rusher. Light body; easy to displace.",
            "Slow frontal guard. Every third attack can crush.",
            "Heavy machine infantry. Resists knockback.",
            "Diagonal lunges, measured retreat and bone reach.",
            "Fast airborne weave. Long warning, fragile body.",
            "Direct pressure, sturdy body and a slow round rhythm."
        };

        private static readonly string[] BossNames =
        {
            "BURROW WARDEN", "IRON WARDEN", "ASHEN WARDEN"
        };

        private static readonly string[] BossNotes =
        {
            "Tunnels out of reach, then erupts near the player.",
            "Locks both arena axes and owns the radial heavy slam.",
            "Chases while chained marks track, lock and detonate."
        };

        public static void Discover(
            EnemyArchetype archetype,
            bool boss,
            int bossAct)
        {
            string key = boss
                ? BossKey(bossAct)
                : CreatureKey((int)archetype);

            if (PlayerPrefs.GetInt(key, 0) == 1)
            {
                return;
            }

            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();

            if (HUDController.Instance != null)
            {
                HUDController.Instance.ShowBestiaryDiscovery(
                    boss
                        ? BossNames[Mathf.Clamp(bossAct, 0, 2)]
                        : CreatureNames[Mathf.Clamp((int)archetype, 0, 5)]);
            }
        }

        public static void RecordDefeat(
            EnemyArchetype archetype,
            bool boss,
            int bossAct)
        {
            string key = boss
                ? BossKey(bossAct)
                : CreatureKey((int)archetype);

            Increment(key + ".Defeated");
        }

        public static void RecordSpare(EnemyArchetype archetype)
        {
            Increment(CreatureKey((int)archetype) + ".Spared");
        }

        public static string BuildDisplayText()
        {
            StringBuilder text = new StringBuilder(900);
            text.AppendLine("CREATURES");

            for (int i = 0; i < CreatureNames.Length; i++)
            {
                string key = CreatureKey(i);
                bool known = PlayerPrefs.GetInt(key, 0) == 1;

                if (!known)
                {
                    text.AppendLine("  [LOCKED]  Unknown creature");
                    continue;
                }

                int defeated = PlayerPrefs.GetInt(key + ".Defeated", 0);
                int spared = PlayerPrefs.GetInt(key + ".Spared", 0);

                text.Append("  ")
                    .Append(CreatureNames[i])
                    .Append("  D:")
                    .Append(defeated)
                    .Append("  S:")
                    .Append(spared)
                    .AppendLine();
                text.Append("     ")
                    .AppendLine(CreatureNotes[i]);
            }

            text.AppendLine();
            text.AppendLine("WARDENS");

            for (int i = 0; i < BossNames.Length; i++)
            {
                string key = BossKey(i);
                bool known = PlayerPrefs.GetInt(key, 0) == 1;

                if (!known)
                {
                    text.AppendLine("  [LOCKED]  Unknown Warden");
                    continue;
                }

                int defeated = PlayerPrefs.GetInt(key + ".Defeated", 0);
                text.Append("  ")
                    .Append(BossNames[i])
                    .Append("  CLEARS:")
                    .Append(defeated)
                    .AppendLine();
                text.Append("     ")
                    .AppendLine(BossNotes[i]);
            }

            return text.ToString();
        }

        private static void Increment(string key)
        {
            PlayerPrefs.SetInt(
                key,
                PlayerPrefs.GetInt(key, 0) + 1);
            PlayerPrefs.Save();
        }

        private static string CreatureKey(int index)
        {
            return Prefix + "Creature." + Mathf.Clamp(index, 0, 5);
        }

        private static string BossKey(int act)
        {
            return Prefix + "Boss." + Mathf.Clamp(act, 0, 2);
        }
    }
}
