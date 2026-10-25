using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace IzzemisMod
{
    internal enum DropGroup
    {
        Meadows,
        BlackForest,
        Swamp,
        Mountain,
        Plains,
        Mistlands,
        Ashlands
    }

    internal static class RuneDrops
    {
        // Column order of every row in BaseChances.
        private static readonly string[] RuneOrder =
        {
            "Asgard", "Midgard", "Vanaheim", "Alfheim", "Jotunheim",
            "Muspelheim", "Niflheim", "Hel", "Nidavellir", "Valheim"
        };

        // Chance in percent for one kill of an enemy with multiplier 1.
        // Rune of Valheim is 0 because it only exists as the product of Nidavellir.
        private static readonly Dictionary<DropGroup, float[]> BaseChances = new Dictionary<DropGroup, float[]>
        {
            //                       Asgard Midgard Vanaheim Alfheim Jotunheim Muspelheim Niflheim Hel  Nidavellir Valheim
            { DropGroup.Meadows,     new[] { 0f,  0f,  0f,  0f,  0f,  0f,  0f,  0f,  0f,  0f } },
            { DropGroup.BlackForest, new[] { 0.2f, 0.1f, 0f,  0f,  0f,  0f,  0f,  0f,  0f,  0f } },
            { DropGroup.Swamp,       new[] { 0.4f, 0.2f, 0.2f, 0f,  0f,  0.1f, 0f,  0f,  0f,  0f } },
            { DropGroup.Mountain,    new[] { 0.6f, 0.3f, 0.4f, 0.1f, 0f,  0.2f, 0.1f, 0f,  0f,  0f } },
            { DropGroup.Plains,      new[] { 1.0f, 0.4f, 1.0f, 0.2f, 0.1f, 0.3f, 0.1f, 0f,  0.5f, 0f } },
            { DropGroup.Mistlands,   new[] { 2.0f, 0.5f, 1.5f, 0.3f, 0.2f, 0.4f, 0.2f, 0.2f, 0.2f, 0f } },
            { DropGroup.Ashlands,    new[] { 3.0f, 1.0f, 2.0f, 0.5f, 0.3f, 0.5f, 0.3f, 0.3f, 0.3f, 0f } }
                    };

        // Enemy prefab name -> group and multiplier applied to every chance of that group.
        private static readonly Dictionary<string, (DropGroup Group, float Multiplier)> Enemies =
            new Dictionary<string, (DropGroup, float)>
        {
            // Meadows
            { "Boar", (DropGroup.Meadows, 1f) },
            { "Neck", (DropGroup.Meadows, 1f) },
            { "Greyling", (DropGroup.Meadows, 1f) },

            // Black Forest
            { "Greydwarf", (DropGroup.BlackForest, 1f) },
            { "Greydwarf_Shaman", (DropGroup.BlackForest, 3f) },
            { "Greydwarf_Elite", (DropGroup.BlackForest, 3f) },
            { "Skeleton", (DropGroup.BlackForest, 1.5f) },
            { "Skeleton_Poison", (DropGroup.BlackForest, 3f) },
            { "Ghost", (DropGroup.BlackForest, 2f) },
            { "Troll", (DropGroup.BlackForest, 4f) },

            // Swamp
            { "Blob", (DropGroup.Swamp, 1f) },
            { "BlobElite", (DropGroup.Swamp, 2f) },
            { "Leech", (DropGroup.Swamp, 1f) },
            { "Draugr", (DropGroup.Swamp, 1f) },
            { "Draugr_Ranged", (DropGroup.Swamp, 1f) },
            { "Draugr_Elite", (DropGroup.Swamp, 2f) },
            { "Surtling", (DropGroup.Swamp, 0.5f) },
            { "Wraith", (DropGroup.Swamp, 2f) },
            { "Abomination", (DropGroup.Swamp, 5f) },

            // Mountain
            { "Wolf", (DropGroup.Mountain, 1f) },
            { "Fenring", (DropGroup.Mountain, 3f) },
            { "Fenring_Cultist", (DropGroup.Mountain, 3f) },
            { "Hatchling", (DropGroup.Mountain, 1f) },
            { "Bat", (DropGroup.Mountain, 0.1f) },
            { "Ulv", (DropGroup.Mountain, 1f) },
            { "StoneGolem", (DropGroup.Mountain, 3f) },

            // Plains
            { "Goblin", (DropGroup.Plains, 1f) },
            { "GoblinArcher", (DropGroup.Plains, 1f) },
            { "GoblinShaman", (DropGroup.Plains, 2f) },
            { "GoblinBrute", (DropGroup.Plains, 2f) },
            { "Deathsquito", (DropGroup.Plains, 0.5f) },
            { "Lox", (DropGroup.Plains, 3f) },

            // Mistlands
            { "Tick", (DropGroup.Mistlands, 0.1f) },
            { "Seeker", (DropGroup.Mistlands, 1f) },
            { "SeekerBrood", (DropGroup.Mistlands, 0.5f) },
            { "SeekerBrute", (DropGroup.Mistlands, 2f) },
            { "Gjall", (DropGroup.Mistlands, 3f) },
            { "Dverger", (DropGroup.Mistlands, 5f) },
            { "DvergerMage", (DropGroup.Mistlands, 5f) },
            { "DvergerMageFire", (DropGroup.Mistlands, 5f) },
            { "DvergerMageIce", (DropGroup.Mistlands, 5f) },
            { "DvergerMageSupport", (DropGroup.Mistlands, 5f) },

            // Ashlands
            { "Charred_Melee", (DropGroup.Ashlands, 1f) },
            { "Charred_Archer", (DropGroup.Ashlands, 1f) },
            { "Charred_Mage", (DropGroup.Ashlands, 2f) },
            { "Charred_Twitcher", (DropGroup.Ashlands, 1f) },
            { "Asksvin", (DropGroup.Ashlands, 2f) },
            { "Morgen", (DropGroup.Ashlands, 3f) },
            { "FallenValkyrie", (DropGroup.Ashlands, 3f) },
            { "Volture", (DropGroup.Ashlands, 0.5f) },
            { "BlobLava", (DropGroup.Ashlands, 0.5f) },

            // Bosses are not listed here; see BossDrops.
        };

        // Boss prefab name -> rune name; one is dropped per player present. Null drops a random Rune of Valheim instead.
        private static readonly Dictionary<string, string> BossDrops = new Dictionary<string, string>
        {
            { "Eikthyr", "Midgard" },
            { "gd_king", "Vanaheim" },
            { "Bonemass", "Alfheim" },
            { "Dragon", "Jotunheim" },
            { "GoblinKing", null },
            { "SeekerQueen", null },
            { "Fader", null }
        };

        private static ConfigEntry<float> dropRateMultiplier;
        private static ConfigEntry<float> starMultiplier;

        public static void Bind(ConfigFile config)
        {
            dropRateMultiplier = config.Bind(
                "Rune Drops",
                "DropRateMultiplier",
                1f,
                new ConfigDescription(
                    "Global multiplier for all rune drop chances. 1 = default, 0 = no rune drops, 2 = double.",
                    new AcceptableValueRange<float>(0f, 100f)));
            starMultiplier = config.Bind(
                "Rune Drops",
                "StarMultiplier",
                3f,
                new ConfigDescription(
                    "Drop chance is multiplied by this value once per enemy star (1 star = x3, 2 stars = x9 by default). 1 disables the bonus.",
                    new AcceptableValueRange<float>(1f, 20f)));
        }

        public static void AddDrops(CharacterDrop source, string enemyName, int stars, List<KeyValuePair<GameObject, int>> drops)
        {
            if (ObjectDB.instance == null)
            {
                return;
            }

            if (BossDrops.TryGetValue(enemyName, out string bossRune))
            {
                AddBossDrops(source, bossRune, drops);
                return;
            }

            float globalMultiplier = dropRateMultiplier != null ? dropRateMultiplier.Value : 1f;
            float starFactor = Mathf.Pow(starMultiplier != null ? starMultiplier.Value : 3f, Mathf.Max(0, stars));
            globalMultiplier *= starFactor;
            if (globalMultiplier <= 0f ||
                !Enemies.TryGetValue(enemyName, out var enemy) ||
                !BaseChances.TryGetValue(enemy.Group, out float[] chances))
            {
                return;
            }

            for (int i = 0; i < RuneOrder.Length && i < chances.Length; i++)
            {
                float percent = chances[i] * enemy.Multiplier * globalMultiplier;
                if (percent <= 0f || Random.value * 100f >= percent)
                {
                    continue;
                }

                GameObject prefab = ObjectDB.instance.GetItemPrefab("IzzemisMod_Rune" + RuneOrder[i]);
                if (prefab != null)
                {
                    drops.Add(new KeyValuePair<GameObject, int>(prefab, 1));
                }
            }
        }
        private static void AddBossDrops(CharacterDrop source, string runeName, List<KeyValuePair<GameObject, int>> drops)
        {
            if (dropRateMultiplier != null && dropRateMultiplier.Value <= 0f)
            {
                return;
            }

            Vector3 position = source.transform.position;
            int players = Mathf.Max(1, Player.GetPlayersInRangeXZ(position, 200f));
            if (runeName == null)
            {
                for (int i = 0; i < players; i++)
                {
                    RuneEffects.DropRandomValheimRune(position + Vector3.up);
                }

                return;
            }

            GameObject prefab = ObjectDB.instance.GetItemPrefab("IzzemisMod_Rune" + runeName);
            if (prefab != null)
            {
                drops.Add(new KeyValuePair<GameObject, int>(prefab, players));
            }
        }
    }

    [HarmonyPatch(typeof(CharacterDrop), nameof(CharacterDrop.GenerateDropList))]
    internal static class RuneDropsPatch
    {
        private static void Postfix(CharacterDrop __instance, List<KeyValuePair<GameObject, int>> __result)
        {
            if (__result != null)
            {
                var character = __instance.GetComponent<Character>();
                int stars = character != null ? character.GetLevel() - 1 : 0;
                RuneDrops.AddDrops(__instance, Utils.GetPrefabName(__instance.gameObject), stars, __result);
            }
        }
    }
}
