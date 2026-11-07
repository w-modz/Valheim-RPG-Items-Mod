using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Runecarved
{
    [HarmonyPatch(typeof(Container), nameof(Container.AddDefaultItems))]
    internal static class RuneChestLoot
    {
        private const double RuneChance = 0.25d;
        private static readonly System.Random Random = new System.Random();
        private static readonly HashSet<string> ReportedProblems = new HashSet<string>();

        private static void Postfix(Container __instance)
        {
            string prefabName = Utils.GetPrefabName(__instance.gameObject);
            if (!IsLootChest(prefabName))
            {
                return;
            }

            if (WorldGenerator.instance == null)
            {
                WarnOnce("world", "Cannot determine biome for rune chest loot: WorldGenerator is unavailable.");
                return;
            }

            if (!TryGetDropGroup(WorldGenerator.instance.GetBiome(__instance.transform.position), out DropGroup group) ||
                Random.NextDouble() >= RuneChance)
            {
                return;
            }

            int runeCount = (int)RuneDropModifier.Chest.GetMultiplier();
            for (int i = 0; i < runeCount; i++)
            {
                if (!TryAddRune(__instance, prefabName, group))
                {
                    break;
                }
            }
        }

        private static bool TryAddRune(Container container, string prefabName, DropGroup group)
        {
            string rune = RuneDrops.SelectChestRune(group, Random.NextDouble());
            if (rune == null)
            {
                return false;
            }

            string runePrefabName = "IzzemisMod_Rune" + rune;
            GameObject prefab = ObjectDB.instance != null ? ObjectDB.instance.GetItemPrefab(runePrefabName) : null;
            ItemDrop itemDrop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (itemDrop == null)
            {
                WarnOnce(runePrefabName, $"Cannot add chest loot: rune prefab {runePrefabName} is unavailable.");
                return false;
            }

            Inventory inventory = container.m_inventory;
            ItemDrop.ItemData item = itemDrop.m_itemData.Clone();
            item.m_dropPrefab = prefab;
            item.m_stack = 1;
            item.m_worldLevel = (byte)Game.m_worldLevel;
            if (container.m_nview.GetZDO().IsCheated() && !PlayerProfile.s_bypassCheatChecks)
            {
                item.m_cheated = true;
            }

            if (!inventory.AddItem(item))
            {
                Jotunn.Logger.LogWarning($"Could not add {runePrefabName} to chest {prefabName} at {container.transform.position}; no existing loot was replaced.");
                return false;
            }

            return true;
        }

        private static bool IsLootChest(string prefabName)
        {
            return prefabName.StartsWith("TreasureChest", StringComparison.OrdinalIgnoreCase) ||
                prefabName.StartsWith("loot_chest_", StringComparison.OrdinalIgnoreCase) ||
                prefabName.StartsWith("stonechest", StringComparison.OrdinalIgnoreCase);
        }

        private static bool TryGetDropGroup(Heightmap.Biome biome, out DropGroup group)
        {
            switch (biome)
            {
                case Heightmap.Biome.BlackForest: group = DropGroup.BlackForest; return true;
                case Heightmap.Biome.Swamp: group = DropGroup.Swamp; return true;
                case Heightmap.Biome.Mountain: group = DropGroup.Mountain; return true;
                case Heightmap.Biome.Plains: group = DropGroup.Plains; return true;
                case Heightmap.Biome.Mistlands: group = DropGroup.Mistlands; return true;
                case Heightmap.Biome.AshLands: group = DropGroup.Ashlands; return true;
                default: group = default(DropGroup); return false;
            }
        }

        private static void WarnOnce(string id, string message)
        {
            if (ReportedProblems.Add(id))
            {
                Jotunn.Logger.LogWarning(message);
            }
        }
    }
}
