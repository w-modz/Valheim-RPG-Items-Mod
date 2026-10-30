using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Runecarved
{
    internal static class SummonWeaponInfo
    {
        public static bool HasSummonAbility(ItemDrop.ItemData item)
        {
            return HasSummonAbility(item?.m_shared?.m_attack) ||
                HasSummonAbility(item?.m_shared?.m_secondaryAttack) ||
                IsKnownSummonWeapon(item);
        }

        private static bool IsKnownSummonWeapon(ItemDrop.ItemData item)
        {
            string prefabName = item?.m_dropPrefab != null ? item.m_dropPrefab.name : string.Empty;
            string sharedName = item?.m_shared?.m_name ?? string.Empty;
            return prefabName.IndexOf("StaffSkeleton", StringComparison.OrdinalIgnoreCase) >= 0 ||
                sharedName.IndexOf("staffskeleton", StringComparison.OrdinalIgnoreCase) >= 0 ||
                sharedName.IndexOf("dead raiser", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool HasSummonAbility(Attack attack)
        {
            return GetSpawnAbilities(attack).Length > 0;
        }

        internal static SpawnAbility[] GetSpawnAbilities(Attack attack)
        {
            GameObject projectilePrefab = attack?.m_attackProjectile;
            if (projectilePrefab == null)
            {
                return Array.Empty<SpawnAbility>();
            }

            var abilities = new List<SpawnAbility>(
                projectilePrefab.GetComponentsInChildren<SpawnAbility>(true));
            foreach (Projectile projectile in projectilePrefab.GetComponentsInChildren<Projectile>(true))
            {
                if (projectile.m_spawnOnHit != null)
                {
                    abilities.AddRange(
                        projectile.m_spawnOnHit.GetComponentsInChildren<SpawnAbility>(true));
                }
            }

            return abilities.ToArray();
        }

    }

    [HarmonyPatch(typeof(SpawnAbility), nameof(SpawnAbility.Setup))]
    internal static class SummonModifierSetupPatch
    {
        private static void Prefix(SpawnAbility __instance, ItemDrop.ItemData item)
        {
            if (item == null)
            {
                return;
            }

            if (ModifierCatalog.TryGetModifierValue(
                    item,
                    ModifierEffectType.SummonMaxCount,
                    out _,
                    out float additionalCount))
            {
                int count = Mathf.RoundToInt(additionalCount);
                if (__instance.m_maxSpawned > 0)
                {
                    __instance.m_maxSpawned += count;
                }

                // The real cap is the MaxInstances value written to each spawned creature, which comes from
                // the weapon quality when this flag is set, so convert it into explicit per-level limits.
                bool fromQuality = __instance.m_setMaxInstancesFromWeaponLevel;
                __instance.m_setMaxInstancesFromWeaponLevel = false;
                if (__instance.m_levelUpSettings != null)
                {
                    foreach (SpawnAbility.LevelUpSettings setting in __instance.m_levelUpSettings)
                    {
                        int baseLimit = fromQuality ? item.m_quality : setting.m_maxSpawns;
                        setting.m_maxSpawns = baseLimit + count;
                    }
                }
            }

            if (ModifierCatalog.TryGetModifierValue(
                    item,
                    ModifierEffectType.ExtraSummonChance,
                    out _,
                    out float extraSummonChance) &&
                UnityEngine.Random.value < extraSummonChance)
            {
                __instance.m_minToSpawn++;
                __instance.m_maxToSpawn++;
            }

            if (__instance.m_levelUpSettings != null &&
                ModifierCatalog.TryGetModifierValue(
                    item,
                    ModifierEffectType.ExtraSummonStars,
                    out _,
                    out float extraStarChance) &&
                UnityEngine.Random.value < extraStarChance)
            {
                int extraStars = UnityEngine.Random.Range(1, 3);
                foreach (SpawnAbility.LevelUpSettings setting in __instance.m_levelUpSettings)
                {
                    setting.m_setLevel = Mathf.Min(3, setting.m_setLevel + extraStars);
                }
            }

        }
    }
}
