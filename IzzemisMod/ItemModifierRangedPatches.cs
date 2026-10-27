using HarmonyLib;
using UnityEngine;

namespace IzzemisMod
{
    // All three effects are read from the bow or crossbow, since ammo never carries modifiers.
    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
    internal static class ProjectileSpeedModifierPatch
    {
        private static void Prefix(ref Vector3 velocity, ItemDrop.ItemData item)
        {
            if (item != null &&
                ModifierCatalog.TryGetModifierValue(
                    item,
                    ModifierEffectType.ProjectileSpeed,
                    out ModifierDefinition definition,
                    out float value))
            {
                velocity = definition.ApplyScalar(1f, value) * velocity;
            }
        }
    }

    [HarmonyPatch(typeof(Attack), "UseAmmo")]
    internal static class AmmoSaveModifierPatch
    {
        private static bool Prefix(
            Attack __instance,
            ref ItemDrop.ItemData ammoItem,
            ref ItemDrop.ItemData ___m_ammoItem,
            Humanoid ___m_character,
            ref bool __result)
        {
            ItemDrop.ItemData weapon = __instance.m_weapon;
            if (weapon == null ||
                string.IsNullOrWhiteSpace(weapon.m_shared.m_ammoType) ||
                !ModifierCatalog.TryGetModifierValue(
                    weapon,
                    ModifierEffectType.AmmoSaveChance,
                    out _,
                    out float chance) ||
                Random.value >= chance)
            {
                return true;
            }

            // Mirrors the game's ammo lookup, minus the removal.
            Inventory inventory = ___m_character.GetInventory();
            ItemDrop.ItemData ammo = ___m_character.GetAmmoItem();
            if (ammo != null &&
                (!inventory.ContainsItem(ammo) ||
                 ammo.m_shared.m_ammoType != weapon.m_shared.m_ammoType))
            {
                ammo = null;
            }

            ammo ??= inventory.GetAmmoItem(weapon.m_shared.m_ammoType);

            // Missing ammo and consumables fall through to the original method.
            if (ammo == null || ammo.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Consumable)
            {
                return true;
            }

            ammoItem = ammo;
            ___m_ammoItem = ammo;
            __result = true;
            return false;
        }
    }

    [HarmonyPatch(typeof(Attack), "FireProjectileBurst")]
    internal static class ExtraProjectileModifierPatch
    {
        private const float SideAngle = 10f;
        private static readonly float[] ExtraAngles = { -SideAngle, SideAngle };
        private static readonly AccessTools.FieldRef<Attack, bool> PerBurstUsage =
            AccessTools.FieldRefAccess<Attack, bool>("m_perBurstResourceUsage");
        private static readonly System.Action<Attack> FireBurst =
            (System.Action<Attack>)System.Delegate.CreateDelegate(
                typeof(System.Action<Attack>),
                AccessTools.Method(typeof(Attack), "FireProjectileBurst"));

        internal static bool Firing;
        internal static float AngleOffset;

        private static void Postfix(Attack __instance)
        {
            ItemDrop.ItemData weapon = __instance.m_weapon;
            if (Firing ||
                weapon == null ||
                PerBurstUsage(__instance) ||
                !ModifierCatalog.TryGetModifierValue(
                    weapon,
                    ModifierEffectType.ExtraProjectileChance,
                    out _,
                    out float chance) ||
                Random.value >= chance)
            {
                return;
            }

            Firing = true;
            try
            {
                foreach (float angle in ExtraAngles)
                {
                    AngleOffset = angle;
                    FireBurst(__instance);
                }
            }
            finally
            {
                AngleOffset = 0f;
                Firing = false;
            }
        }
    }

    [HarmonyPatch(typeof(Attack), "GetProjectileSpawnPoint")]
    internal static class ExtraProjectileAimPatch
    {
        private static void Postfix(ref Vector3 aimDir)
        {
            if (ExtraProjectileModifierPatch.AngleOffset != 0f)
            {
                aimDir = Quaternion.AngleAxis(ExtraProjectileModifierPatch.AngleOffset, Vector3.up) * aimDir;
            }
        }
    }
}
