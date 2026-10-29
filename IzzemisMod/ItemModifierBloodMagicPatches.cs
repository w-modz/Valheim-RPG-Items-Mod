using System;
using HarmonyLib;

namespace IzzemisMod
{
    internal static class BloodMagicModifierHelpers
    {
        public static float GetBaseHealthCost(
            float healthCost,
            float healthCostPercentage,
            Humanoid character,
            ItemDrop.ItemData weapon)
        {
            if (character == null || weapon?.m_shared == null)
            {
                return 0f;
            }

            float cost = healthCost + character.GetHealth() * healthCostPercentage / 100f;
            if (cost <= 0f)
            {
                return 0f;
            }

            float skillFactor = character.GetSkillFactor(weapon.m_shared.m_skillType);
            return Math.Max(0f, cost - cost * 0.33f * skillFactor);
        }

        public static float GetStaminaConversion(ItemDrop.ItemData weapon, float baseHealthCost)
        {
            return weapon != null &&
                ModifierCatalog.TryGetModifierValue(
                    weapon,
                    ModifierEffectType.BloodHealthToStamina,
                    out _,
                    out float conversion)
                ? baseHealthCost * conversion
                : 0f;
        }

        public static bool TryGetLowHealthBonus(
            ItemDrop.ItemData weapon,
            Character character,
            out float bonus)
        {
            bonus = 0f;
            return weapon != null &&
                character != null &&
                character.GetHealth() < character.GetMaxHealth() * 0.5f &&
                ModifierCatalog.TryGetModifierValue(
                    weapon,
                    ModifierEffectType.LowHealthEffect,
                    out _,
                    out bonus);
        }
    }

    [HarmonyPatch(typeof(Attack), "GetAttackHealth")]
    internal static class BloodMagicHealthCostModifierPatch
    {
        private static void Postfix(
            Attack __instance,
            ref float __result,
            float ___m_attackHealth,
            float ___m_attackHealthPercentage)
        {
            ItemDrop.ItemData weapon = __instance.m_weapon;
            if (weapon == null)
            {
                return;
            }

            float reduction = 0f;
            if (ModifierCatalog.TryGetModifierValue(
                    weapon,
                    ModifierEffectType.BloodHealthCost,
                    out _,
                    out float healthCostReduction))
            {
                reduction += healthCostReduction;
            }

            if (ModifierCatalog.TryGetModifierValue(
                    weapon,
                    ModifierEffectType.BloodHealthToStamina,
                    out _,
                    out float healthConversion))
            {
                reduction += healthConversion;
            }

            float baseHealthCost = BloodMagicModifierHelpers.GetBaseHealthCost(
                ___m_attackHealth,
                ___m_attackHealthPercentage,
                __instance.m_character,
                weapon);
            __result = Math.Max(0f, baseHealthCost * (1f - reduction));
        }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackStamina))]
    internal static class BloodMagicStaminaConversionPatch
    {
        private static void Postfix(
            Attack __instance,
            ref float __result,
            float ___m_attackHealth,
            float ___m_attackHealthPercentage)
        {
            ItemDrop.ItemData weapon = __instance.m_weapon;
            if (weapon == null)
            {
                return;
            }

            float baseHealthCost = BloodMagicModifierHelpers.GetBaseHealthCost(
                ___m_attackHealth,
                ___m_attackHealthPercentage,
                __instance.m_character,
                weapon);
            __result += BloodMagicModifierHelpers.GetStaminaConversion(weapon, baseHealthCost);
        }
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
    internal static class BloodMagicLowHealthDamagePatch
    {
        private static void Postfix(
            ItemDrop.ItemData item,
            Character owner,
            ref HitData.DamageTypes ___m_damage)
        {
            if (BloodMagicModifierHelpers.TryGetLowHealthBonus(item, owner, out float bonus))
            {
                ___m_damage.Modify(1f + bonus);
            }
        }
    }

    [HarmonyPatch(typeof(SE_Shield), nameof(SE_Shield.Setup))]
    internal static class BloodMagicLowHealthShieldPatch
    {
        private static readonly AccessTools.FieldRef<SE_Shield, float> TotalAbsorbDamage =
            AccessTools.FieldRefAccess<SE_Shield, float>("m_totalAbsorbDamage");

        private static void Postfix(SE_Shield __instance, Character character)
        {
            if (character is Humanoid humanoid &&
                BloodMagicModifierHelpers.TryGetLowHealthBonus(
                    humanoid.GetCurrentWeapon(),
                    character,
                    out float bonus))
            {
                TotalAbsorbDamage(__instance) *= 1f + bonus;
            }
        }
    }
}
