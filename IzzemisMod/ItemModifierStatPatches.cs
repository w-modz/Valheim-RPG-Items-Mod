using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace Runecarved
{
    [HarmonyPatch(typeof(Attack), nameof(Attack.GetAttackStamina))]
    internal static class WeaponStaminaModifierPatch
    {
        private static void Postfix(Attack __instance, ref float __result)
        {
            ItemDrop.ItemData weapon = __instance.m_weapon;
            if (weapon != null &&
                ModifierCatalog.TryGetModifierValue(
                    weapon,
                    ModifierEffectType.AttackStaminaCost,
                    out ModifierDefinition definition,
                    out float value))
            {
                __result = definition.ApplyScalar(__result, value);
            }
        }
    }

    [HarmonyPatch(
        typeof(Attack),
        nameof(Attack.GetAttackEitr),
        new[] { typeof(Character), typeof(ItemDrop.ItemData) })]
    internal static class WeaponEitrModifierPatch
    {
        private static void Postfix(ItemDrop.ItemData weapon, ref float __result)
        {
            if (weapon != null &&
                ModifierCatalog.TryGetModifierValue(
                    weapon,
                    ModifierEffectType.AttackEitrCost,
                    out ModifierDefinition definition,
                    out float value))
            {
                __result = Math.Max(0f, definition.ApplyScalar(__result, value));
            }
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetDrawStaminaDrain))]
    internal static class BowDrawStaminaModifierPatch
    {
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (ModifierCatalog.TryGetModifierValue(
                    __instance,
                    ModifierEffectType.BowDrawStaminaCost,
                    out ModifierDefinition definition,
                    out float value))
            {
                __result = Math.Max(0f, definition.ApplyScalar(__result, value));
            }
        }
    }

    [HarmonyPatch(typeof(ItemDrop.ItemData), nameof(ItemDrop.ItemData.GetWeaponLoadingTime))]
    internal static class CrossbowReloadModifierPatch
    {
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (ModifierCatalog.TryGetModifierValue(
                    __instance,
                    ModifierEffectType.ReloadTime,
                    out ModifierDefinition definition,
                    out float value))
            {
                __result = Math.Max(0f, definition.ApplyScalar(__result, value));
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentMovementModifier))]
    internal static class MovementPenaltyModifierPatch
    {
        private static void Postfix(Player __instance, ref float __result)
        {
            var items = new List<ItemDrop.ItemData>();
            AddEquippedItem(items, __instance.GetCurrentWeapon());
            AddEquippedItem(items, __instance.m_leftItem);
            AddEquippedItem(items, __instance.m_rightItem);
            AddEquippedItem(items, __instance.m_helmetItem);
            AddEquippedItem(items, __instance.m_chestItem);
            AddEquippedItem(items, __instance.m_legItem);
            AddEquippedItem(items, __instance.m_shoulderItem);

            foreach (ItemDrop.ItemData item in items)
            {
                if (item.m_shared.m_movementModifier < 0f &&
                    ModifierCatalog.TryGetModifierValue(
                        item,
                        ModifierEffectType.MovementPenalty,
                        out _,
                        out float reduction))
                {
                    __result += Math.Min(reduction, -item.m_shared.m_movementModifier);
                }

                if (ModifierCatalog.TryGetModifierValue(
                        item,
                        ModifierEffectType.MovementSpeedBonus,
                        out _,
                        out float bonus))
                {
                    __result += bonus;
                }
            }
        }

        private static void AddEquippedItem(
            List<ItemDrop.ItemData> items,
            ItemDrop.ItemData item)
        {
            if (item == null)
            {
                return;
            }

            foreach (ItemDrop.ItemData existing in items)
            {
                if (ReferenceEquals(existing, item))
                {
                    return;
                }
            }

            items.Add(item);
        }
    }

    [HarmonyPatch(
        typeof(ItemDrop.ItemData),
        nameof(ItemDrop.ItemData.GetBaseBlockPower),
        new[] { typeof(int) })]
    internal static class BlockArmourModifierPatch
    {
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (ModifierCatalog.TryGetModifierValue(
                    __instance,
                    ModifierEffectType.BlockPower,
                    out ModifierDefinition definition,
                    out float value))
            {
                __result = definition.ApplyScalar(__result, value);
            }
        }
    }

    [HarmonyPatch(
        typeof(ItemDrop.ItemData),
        nameof(ItemDrop.ItemData.GetArmor),
        new[] { typeof(int), typeof(float) })]
    internal static class ArmourModifierPatch
    {
        private static void Postfix(ItemDrop.ItemData __instance, ref float __result)
        {
            if (ModifierCatalog.TryGetModifierValue(
                    __instance,
                    ModifierEffectType.ArmourFlat,
                    out ModifierDefinition flatDefinition,
                    out float flatValue))
            {
                __result = flatDefinition.ApplyScalar(__result, flatValue);
            }

            if (ModifierCatalog.TryGetModifierValue(
                    __instance,
                    ModifierEffectType.ArmourPercentage,
                    out ModifierDefinition percentageDefinition,
                    out float percentageValue))
            {
                __result = (float)Math.Ceiling(
                    percentageDefinition.ApplyScalar(__result, percentageValue));
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
    internal static class ParryBonusModifierPatch
    {
        private struct State
        {
            public ItemDrop.ItemData Item;
            public float OriginalTimedBlockBonus;
            public float OriginalBlockAdrenaline;
            public float OriginalPerfectBlockAdrenaline;
            public float OriginalBlockStaminaDrain;
            public float OriginalPerfectBlockStaminaDrain;
            public bool ModifiedTimedBlockBonus;
            public bool ModifiedAdrenaline;
            public bool ModifiedStamina;
        }

        private static void Prefix(Humanoid __instance, out State __state)
        {
            __state = default(State);
            ItemDrop.ItemData blocker = __instance.GetCurrentBlocker();
            if (blocker == null)
            {
                return;
            }

            if (ModifierCatalog.TryGetModifierValue(
                    blocker,
                    ModifierEffectType.ParryBonus,
                    out _,
                    out float parryBonus))
            {
                __state.Item = blocker;
                __state.OriginalTimedBlockBonus = blocker.m_shared.m_timedBlockBonus;
                blocker.m_shared.m_timedBlockBonus += parryBonus;
                __state.ModifiedTimedBlockBonus = true;
            }

            if (__instance.m_blockTimer != -1f &&
                __instance.m_blockTimer < 0.25f &&
                blocker.m_shared.m_timedBlockBonus > 1f &&
                ModifierCatalog.TryGetModifierValue(
                    blocker,
                    ModifierEffectType.ParryAdrenaline,
                    out _,
                    out float adrenaline))
            {
                __state.Item = blocker;
                __state.OriginalBlockAdrenaline = blocker.m_shared.m_blockAdrenaline;
                __state.OriginalPerfectBlockAdrenaline = blocker.m_shared.m_perfectBlockAdrenaline;
                blocker.m_shared.m_blockAdrenaline += adrenaline;
                blocker.m_shared.m_perfectBlockAdrenaline += adrenaline;
                __state.ModifiedAdrenaline = true;
            }

            if (ModifierCatalog.TryGetModifierValue(
                    blocker,
                    ModifierEffectType.BlockStaminaCost,
                    out ModifierDefinition definition,
                    out float staminaCost))
            {
                __state.OriginalBlockStaminaDrain = __instance.m_blockStaminaDrain;
                __state.OriginalPerfectBlockStaminaDrain = __instance.m_perfectBlockStaminaDrain;
                __instance.m_blockStaminaDrain =
                    definition.ApplyScalar(__instance.m_blockStaminaDrain, staminaCost);
                __instance.m_perfectBlockStaminaDrain =
                    definition.ApplyScalar(__instance.m_perfectBlockStaminaDrain, staminaCost);
                __state.ModifiedStamina = true;
            }
        }

        private static System.Exception Finalizer(
            Humanoid __instance,
            State __state,
            System.Exception __exception)
        {
            if (__state.ModifiedTimedBlockBonus)
            {
                __state.Item.m_shared.m_timedBlockBonus = __state.OriginalTimedBlockBonus;
            }

            if (__state.ModifiedAdrenaline)
            {
                __state.Item.m_shared.m_blockAdrenaline = __state.OriginalBlockAdrenaline;
                __state.Item.m_shared.m_perfectBlockAdrenaline =
                    __state.OriginalPerfectBlockAdrenaline;
            }

            if (__state.ModifiedStamina)
            {
                __instance.m_blockStaminaDrain = __state.OriginalBlockStaminaDrain;
                __instance.m_perfectBlockStaminaDrain =
                    __state.OriginalPerfectBlockStaminaDrain;
            }

            return __exception;
        }
    }

    [HarmonyPatch(
        typeof(Character),
        nameof(Character.ApplyDamage),
        new[] { typeof(HitData), typeof(bool), typeof(bool), typeof(HitData.DamageModifier) })]
    internal static class ArmourElementalResistancePatch
    {
        private static void Prefix(Character __instance, HitData hit)
        {
            if (!(__instance is Player player) || hit == null)
            {
                return;
            }

            ItemDrop.ItemData[] armour =
            {
                player.m_helmetItem,
                player.m_chestItem,
                player.m_legItem,
                player.m_shoulderItem
            };

            hit.m_damage.m_fire *= 1f - GetResistance(armour, ModifierEffectType.FireResistance);
            hit.m_damage.m_frost *= 1f - GetResistance(armour, ModifierEffectType.FrostResistance);
            hit.m_damage.m_lightning *= 1f - GetResistance(armour, ModifierEffectType.LightningResistance);
            hit.m_damage.m_poison *= 1f - GetResistance(armour, ModifierEffectType.PoisonResistance);
        }

        private static float GetResistance(
            ItemDrop.ItemData[] armour,
            ModifierEffectType effectType)
        {
            float resistance = 0f;
            foreach (ItemDrop.ItemData item in armour)
            {
                if (ModifierCatalog.TryGetModifierValue(
                        item,
                        effectType,
                        out _,
                        out float value))
                {
                    resistance = Math.Max(resistance, value);
                }
            }

            return Math.Max(0f, Math.Min(1f, resistance));
        }
    }

    [HarmonyPatch(typeof(Attack), nameof(Attack.DoMeleeAttack))]
    internal static class MeleeWeaponModifierPatch
    {
        private struct State
        {
            public ItemDrop.ItemData Weapon;
            public float OriginalBackstabBonus;
        }

        private static void Prefix(Attack __instance, out State __state)
        {
            __state = default(State);
            ItemDrop.ItemData weapon = __instance.m_weapon;
            if (weapon == null)
            {
                return;
            }

            if (ModifierCatalog.TryGetModifierValue(
                    weapon,
                    ModifierEffectType.BackstabBonus,
                    out _,
                    out float backstabBonus))
            {
                __state.Weapon = weapon;
                __state.OriginalBackstabBonus = weapon.m_shared.m_backstabBonus;
                weapon.m_shared.m_backstabBonus += backstabBonus;
            }

        }

        private static System.Exception Finalizer(State __state, System.Exception __exception)
        {
            if (__state.Weapon != null)
            {
                __state.Weapon.m_shared.m_backstabBonus = __state.OriginalBackstabBonus;
            }

            return __exception;
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            MethodInfo addAdrenaline = AccessTools.Method(
                typeof(Character),
                nameof(Character.AddAdrenaline),
                new[] { typeof(float) });
            MethodInfo addHitAdrenaline = AccessTools.Method(
                typeof(MeleeWeaponModifierPatch),
                nameof(AddHitAdrenaline));
            bool patched = false;

            foreach (CodeInstruction instruction in instructions)
            {
                if (!patched &&
                    (instruction.opcode == OpCodes.Call || instruction.opcode == OpCodes.Callvirt) &&
                    Equals(instruction.operand, addAdrenaline))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Call, addHitAdrenaline);
                    patched = true;
                }
                else
                {
                    yield return instruction;
                }
            }

            if (!patched)
            {
                throw new InvalidOperationException(
                    "Could not find the melee hit adrenaline call to apply weapon modifiers.");
            }
        }

        private static void AddHitAdrenaline(Character character, float amount, Attack attack)
        {
            ItemDrop.ItemData weapon = attack.m_weapon;
            if (weapon != null &&
                ModifierCatalog.TryGetModifierValue(
                    weapon,
                    ModifierEffectType.AdrenalineGain,
                    out _,
                    out float value))
            {
                amount += value;
            }

            character.AddAdrenaline(amount);
        }
    }

    [HarmonyPatch(
        typeof(Projectile),
        nameof(Projectile.OnHit),
        new[] { typeof(UnityEngine.Collider), typeof(UnityEngine.Vector3), typeof(bool), typeof(UnityEngine.Vector3) })]
    internal static class ProjectileWeaponModifierPatch
    {
        private struct State
        {
            public float OriginalBackstabBonus;
            public float OriginalAdrenaline;
            public bool ModifiedBackstabBonus;
            public bool ModifiedAdrenaline;
        }

        private static void Prefix(Projectile __instance, out State __state)
        {
            __state = default(State);
            ItemDrop.ItemData weapon = __instance.m_weapon;
            if (weapon == null)
            {
                return;
            }

            if (ModifierCatalog.TryGetModifierValue(
                    weapon,
                    ModifierEffectType.BackstabBonus,
                    out _,
                    out float backstabBonus))
            {
                __state.OriginalBackstabBonus = __instance.m_backstabBonus;
                __instance.m_backstabBonus += backstabBonus;
                __state.ModifiedBackstabBonus = true;
            }

            if (ModifierCatalog.TryGetModifierValue(
                    weapon,
                    ModifierEffectType.AdrenalineGain,
                    out _,
                    out float adrenalineGain))
            {
                __state.OriginalAdrenaline = __instance.m_adrenaline;
                __instance.m_adrenaline += adrenalineGain;
                __state.ModifiedAdrenaline = true;
            }
        }

        private static System.Exception Finalizer(
            Projectile __instance,
            State __state,
            System.Exception __exception)
        {
            if (__state.ModifiedBackstabBonus)
            {
                __instance.m_backstabBonus = __state.OriginalBackstabBonus;
            }

            if (__state.ModifiedAdrenaline)
            {
                __instance.m_adrenaline = __state.OriginalAdrenaline;
            }

            return __exception;
        }
    }
}
