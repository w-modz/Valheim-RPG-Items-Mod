using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;

namespace IzzemisMod
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

    [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentMovementModifier))]
    internal static class MovementPenaltyModifierPatch
    {
        private static void Postfix(Player __instance, ref float __result)
        {
            ItemDrop.ItemData weapon = __instance.GetCurrentWeapon();
            if (weapon == null ||
                weapon.m_shared.m_movementModifier >= 0f ||
                !ModifierCatalog.TryGetModifierValue(
                    weapon,
                    ModifierEffectType.MovementPenalty,
                    out _,
                    out float value))
            {
                return;
            }

            __result += System.Math.Min(value, -weapon.m_shared.m_movementModifier);
        }
    }

    [HarmonyPatch(
        typeof(ItemDrop.ItemData),
        nameof(ItemDrop.ItemData.GetBaseBlockPower),
        new[] { typeof(int) })]
    internal static class BlockArmorModifierPatch
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

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.BlockAttack))]
    internal static class ParryBonusModifierPatch
    {
        private struct State
        {
            public ItemDrop.ItemData Item;
            public float OriginalTimedBlockBonus;
        }

        private static void Prefix(Humanoid __instance, out State __state)
        {
            __state = default(State);
            ItemDrop.ItemData blocker = __instance.GetCurrentBlocker();
            if (blocker == null ||
                !ModifierCatalog.TryGetModifierValue(
                    blocker,
                    ModifierEffectType.ParryBonus,
                    out _,
                    out float value))
            {
                return;
            }

            __state.Item = blocker;
            __state.OriginalTimedBlockBonus = blocker.m_shared.m_timedBlockBonus;
            blocker.m_shared.m_timedBlockBonus += value;
        }

        private static System.Exception Finalizer(State __state, System.Exception __exception)
        {
            if (__state.Item != null)
            {
                __state.Item.m_shared.m_timedBlockBonus = __state.OriginalTimedBlockBonus;
            }

            return __exception;
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
