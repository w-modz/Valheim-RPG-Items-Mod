using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace IzzemisMod
{
    // Armour modifiers stack across every equipped armour piece.
    internal static class ArmourModifiers
    {
        private static IEnumerable<ItemDrop.ItemData> Pieces(Player player)
        {
            yield return player.m_helmetItem;
            yield return player.m_chestItem;
            yield return player.m_legItem;
            yield return player.m_shoulderItem;
        }

        public static float Sum(Player player, ModifierEffectType effectType)
        {
            float total = 0f;
            if (player == null)
            {
                return total;
            }

            foreach (ItemDrop.ItemData piece in Pieces(player))
            {
                if (piece != null &&
                    ModifierCatalog.TryGetModifierValue(piece, effectType, out _, out float value))
                {
                    total += value;
                }
            }

            return total;
        }

        public static float SumSkill(Player player, Skills.SkillType skill)
        {
            float total = 0f;
            if (player == null)
            {
                return total;
            }

            foreach (ItemDrop.ItemData piece in Pieces(player))
            {
                if (piece == null)
                {
                    continue;
                }

                foreach (ModifierDefinition definition in ModifierCatalog.All)
                {
                    if (definition.EffectType == ModifierEffectType.SkillLevel &&
                        definition.Skill == skill &&
                        definition.IsAllowedOn(piece) &&
                        ItemModifierDataStore.TryGetModifierValue(
                            piece, definition.Id, out float value))
                    {
                        total += value;
                    }
                }
            }

            return total;
        }
    }

    [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyHealthRegen))]
    internal static class HealthRegenModifierPatch
    {
        private static readonly AccessTools.FieldRef<SEMan, Character> Owner =
            AccessTools.FieldRefAccess<SEMan, Character>("m_character");

        private static void Postfix(SEMan __instance, ref float regenMultiplier)
        {
            regenMultiplier += ArmourModifiers.Sum(
                Owner(__instance) as Player, ModifierEffectType.HealthRegen);
        }
    }

    [HarmonyPatch(typeof(SEMan), nameof(SEMan.ModifyStaminaRegen))]
    internal static class StaminaRegenModifierPatch
    {
        private static readonly AccessTools.FieldRef<SEMan, Character> Owner =
            AccessTools.FieldRefAccess<SEMan, Character>("m_character");

        private static void Postfix(SEMan __instance, ref float staminaMultiplier)
        {
            staminaMultiplier += ArmourModifiers.Sum(
                Owner(__instance) as Player, ModifierEffectType.StaminaRegen);
        }
    }

    [HarmonyPatch(typeof(Player), "GetTotalFoodValue")]
    internal static class MaxHealthModifierPatch
    {
        private static void Postfix(Player __instance, ref float hp)
        {
            hp += ArmourModifiers.Sum(__instance, ModifierEffectType.MaxHealth);
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentDodgeStaminaModifier))]
    internal static class DodgeStaminaModifierPatch
    {
        private static void Postfix(Player __instance, ref float __result)
        {
            __result += ArmourModifiers.Sum(__instance, ModifierEffectType.DodgeStaminaCost);
        }
    }

    [HarmonyPatch(typeof(Skills), nameof(Skills.GetSkillLevel))]
    internal static class SkillLevelModifierPatch
    {
        private static readonly AccessTools.FieldRef<Skills, Player> Owner =
            AccessTools.FieldRefAccess<Skills, Player>("m_player");

        private static void Postfix(Skills __instance, Skills.SkillType skillType, ref float __result)
        {
            if (skillType != Skills.SkillType.None)
            {
                __result += ArmourModifiers.SumSkill(Owner(__instance), skillType);
            }
        }
    }

    [HarmonyPatch(typeof(Player), "OnDamaged")]
    internal static class DamagedArmourModifierPatch
    {
        private static bool _thornsActive;

        private static void Postfix(Player __instance, HitData hit)
        {
            if (__instance != Player.m_localPlayer || hit.GetTotalDamage() <= 0f)
            {
                return;
            }

            float adrenaline = ArmourModifiers.Sum(__instance, ModifierEffectType.AdrenalineOnHit);
            if (adrenaline > 0f)
            {
                __instance.AddAdrenaline(adrenaline);
            }

            Character attacker = hit.GetAttacker();
            float thorns = ArmourModifiers.Sum(__instance, ModifierEffectType.Thorns);
            if (_thornsActive || thorns <= 0f || attacker == null || attacker == __instance)
            {
                return;
            }

            float damage = thorns * __instance.GetBodyArmor();
            if (damage <= 0f)
            {
                return;
            }

            _thornsActive = true;
            try
            {
                var counter = new HitData
                {
                    m_point = attacker.GetCenterPoint(),
                    m_dir = (attacker.transform.position - __instance.transform.position).normalized,
                    m_hitType = HitData.HitType.PlayerHit
                };
                counter.m_damage.m_pierce = damage;
                counter.SetAttacker(__instance);
                attacker.Damage(counter);
            }
            finally
            {
                _thornsActive = false;
            }
        }
    }
}
