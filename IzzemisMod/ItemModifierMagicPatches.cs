using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Runecarved
{
    internal static class MagicWeaponInfo
    {
        private static readonly Dictionary<string, bool> AoeCache = new Dictionary<string, bool>();

        public static bool FiresProjectile(ItemDrop.ItemData item)
        {
            Attack attack = item?.m_shared?.m_attack;
            return attack != null &&
                attack.m_attackType == Attack.AttackType.Projectile &&
                attack.m_attackProjectile != null;
        }

        // True when one of the weapon's attacks spawns an area effect, directly or on hit.
        public static bool HasAoe(ItemDrop.ItemData item)
        {
            if (item?.m_shared == null)
            {
                return false;
            }

            string key = item.m_shared.m_name;
            if (AoeCache.TryGetValue(key, out bool cached))
            {
                return cached;
            }

            bool result = AttackHasAoe(item.m_shared.m_attack) ||
                AttackHasAoe(item.m_shared.m_secondaryAttack);
            AoeCache[key] = result;
            return result;
        }

        private static bool AttackHasAoe(Attack attack)
        {
            GameObject prefab = attack?.m_attackProjectile;
            if (prefab == null)
            {
                return false;
            }

            if (prefab.GetComponentInChildren<Aoe>(true) != null)
            {
                return true;
            }

            foreach (Projectile projectile in prefab.GetComponentsInChildren<Projectile>(true))
            {
                if (projectile.m_aoe > 0f ||
                    (projectile.m_spawnOnHit != null &&
                     projectile.m_spawnOnHit.GetComponentInChildren<Aoe>(true) != null))
                {
                    return true;
                }
            }

            return false;
        }
    }

    // Finds the Staff of Protection's own shield so the modifier grants an identical one.
    internal static class StaffShieldSource
    {
        private const string StaffPrefabName = "StaffShield";
        private static SE_Shield _shield;
        private static Skills.SkillType _skill = Skills.SkillType.BloodMagic;
        private static bool _resolved;

        public static bool TryGet(out SE_Shield shield, out Skills.SkillType skill)
        {
            if (!_resolved && ObjectDB.instance != null)
            {
                _resolved = true;
                Resolve();
            }

            shield = _shield;
            skill = _skill;
            return _shield != null;
        }

        public static bool IsStaffOfProtection(ItemDrop.ItemData item)
        {
            return item?.m_dropPrefab != null &&
                string.Equals(item.m_dropPrefab.name, StaffPrefabName, System.StringComparison.Ordinal);
        }

        private static void Resolve()
        {
            GameObject prefab = ObjectDB.instance.GetItemPrefab(StaffPrefabName);
            ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (drop != null)
            {
                ItemDrop.ItemData.SharedData shared = drop.m_itemData.m_shared;
                if (shared.m_skillType != Skills.SkillType.None)
                {
                    _skill = shared.m_skillType;
                }

                _shield = shared.m_attackStatusEffect as SE_Shield ??
                    shared.m_equipStatusEffect as SE_Shield;
                if (_shield == null)
                {
                    GameObject projectile = shared.m_attack?.m_attackProjectile;
                    _shield = projectile != null ? FindInProjectile(projectile) : null;
                }
            }

            if (_shield == null)
            {
                foreach (StatusEffect effect in ObjectDB.instance.m_StatusEffects)
                {
                    if (effect is SE_Shield candidate &&
                        candidate.name.IndexOf("shield", System.StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        _shield = candidate;
                        Jotunn.Logger.LogWarning(
                            $"Staff of Protection shield not found; using '{candidate.name}'.");
                        break;
                    }
                }
            }

            if (_shield == null)
            {
                Jotunn.Logger.LogError("No shield status effect found for the shield modifier.");
            }
        }

        private static SE_Shield FindInProjectile(GameObject projectile)
        {
            foreach (Aoe aoe in projectile.GetComponentsInChildren<Aoe>(true))
            {
                SE_Shield found = Lookup(aoe.m_statusEffect) ?? Lookup(aoe.m_statusEffectIfPlayer);
                if (found != null)
                {
                    return found;
                }
            }

            foreach (Projectile p in projectile.GetComponentsInChildren<Projectile>(true))
            {
                SE_Shield found = Lookup(p.m_statusEffect);
                if (found == null && p.m_spawnOnHit != null)
                {
                    found = FindInProjectile(p.m_spawnOnHit);
                }

                if (found != null)
                {
                    return found;
                }
            }

            return null;
        }

        private static SE_Shield Lookup(string name)
        {
            return string.IsNullOrEmpty(name)
                ? null
                : ObjectDB.instance.GetStatusEffect(name.GetStableHashCode()) as SE_Shield;
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    internal static class ShieldOnUseModifierPatch
    {
        private static void Postfix(Humanoid __instance, bool __result)
        {
            if (!__result || __instance != Player.m_localPlayer)
            {
                return;
            }

            ItemDrop.ItemData weapon = __instance.GetCurrentWeapon();
            if (weapon == null)
            {
                return;
            }

            bool shouldGrantShield =
                ModifierCatalog.TryGetModifierValue(
                    weapon,
                    ModifierEffectType.ShieldOnUse,
                    out _,
                    out float chance) &&
                Random.value < chance;

            if (!shouldGrantShield ||
                !StaffShieldSource.TryGet(out SE_Shield shield, out Skills.SkillType skill))
            {
                return;
            }

            __instance.GetSEMan().AddStatusEffect(
                shield, true, weapon.m_quality, __instance.GetSkillLevel(skill));
        }
    }

    // Runs when the cast animation triggers, i.e. when the staff's own shield is granted.
    [HarmonyPatch(typeof(Attack), nameof(Attack.OnAttackTrigger))]
    internal static class ShieldAllyHealModifierPatch
    {
        private static void Postfix(Attack __instance, Humanoid ___m_character)
        {
            if (___m_character == null || ___m_character != Player.m_localPlayer)
            {
                return;
            }

            ItemDrop.ItemData weapon = __instance.GetWeapon();
            if (weapon == null ||
                !StaffShieldSource.IsStaffOfProtection(weapon) ||
                !ModifierCatalog.TryGetModifierValue(
                    weapon,
                    ModifierEffectType.ShieldAllyHeal,
                    out _,
                    out float heal))
            {
                return;
            }

            Vector3 origin = ___m_character.transform.position;
            var nearbyPlayers = new List<Player>();
            Player.GetPlayersInRange(origin, 10f, nearbyPlayers);
            foreach (Player ally in nearbyPlayers)
            {
                if (ally != ___m_character && !ally.IsDead())
                {
                    ally.Heal(heal);
                }
            }

            // Tamed creatures and summons (e.g. skeletons) count as allies too.
            foreach (Character creature in Character.GetAllCharacters())
            {
                if (!(creature is Player) &&
                    creature.IsTamed() &&
                    !creature.IsDead() &&
                    Vector3.Distance(creature.transform.position, origin) <= 10f)
                {
                    creature.Heal(heal);
                }
            }
        }
    }

    [HarmonyPatch(typeof(Humanoid), nameof(Humanoid.StartAttack))]
    internal static class SummonRootsModifierPatch
    {
        private static ItemDrop.ItemData _staff;
        private static bool _resolved;

        private static void Postfix(Humanoid __instance, bool __result)
        {
            if (!__result || __instance != Player.m_localPlayer)
            {
                return;
            }

            ItemDrop.ItemData weapon = __instance.GetCurrentWeapon();
            if (weapon == null ||
                !ModifierCatalog.TryGetModifierValue(
                    weapon,
                    ModifierEffectType.SummonRoots,
                    out _,
                    out float chance) ||
                Random.value >= chance)
            {
                return;
            }

            ItemDrop.ItemData staff = GetStaff();
            Attack attack = staff?.m_shared.m_attack;
            if (attack == null || attack.m_attackProjectile == null)
            {
                return;
            }

            // Fires the Staff of the Wild's own projectile, which spawns the roots on hit.
            Vector3 direction = __instance.GetAimDir(__instance.GetEyePoint());
            Vector3 position = __instance.GetEyePoint() + direction * 0.5f;
            GameObject projectile = Object.Instantiate(
                attack.m_attackProjectile,
                position,
                Quaternion.LookRotation(direction));
            IProjectile component = projectile.GetComponent<IProjectile>();
            component?.Setup(__instance, direction * attack.m_projectileVel, -1f, null, staff, null);
        }

        private static ItemDrop.ItemData GetStaff()
        {
            if (!_resolved && ObjectDB.instance != null)
            {
                _resolved = true;
                GameObject prefab = ObjectDB.instance.GetItemPrefab("StaffGreenRoots");
                ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                _staff = drop?.m_itemData;
                if (_staff == null)
                {
                    Jotunn.Logger.LogError("StaffGreenRoots not found; the summon roots modifier does nothing.");
                }
            }

            return _staff;
        }
    }

    [HarmonyPatch(typeof(Aoe), nameof(Aoe.Setup))]
    internal static class AoeRadiusModifierPatch
    {
        private static void Postfix(Aoe __instance, ItemDrop.ItemData item)
        {
            if (item == null ||
                !ModifierCatalog.TryGetModifierValue(
                    item,
                    ModifierEffectType.AoeRadius,
                    out ModifierDefinition definition,
                    out float value))
            {
                return;
            }

            float factor = definition.ApplyScalar(1f, value);
            if (__instance.m_useCollider != null)
            {
                __instance.m_useCollider.size *= factor;
                __instance.m_useCollider.center *= factor;
            }
            else
            {
                __instance.m_radius *= factor;
            }

            __instance.transform.localScale *= factor;
        }
    }

    [HarmonyPatch(typeof(Projectile), nameof(Projectile.Setup))]
    internal static class ProjectileAoeModifierPatch
    {
        private static void Postfix(Projectile __instance, ItemDrop.ItemData item)
        {
            if (item != null &&
                __instance.m_aoe > 0f &&
                ModifierCatalog.TryGetModifierValue(
                    item,
                    ModifierEffectType.AoeRadius,
                    out ModifierDefinition definition,
                    out float value))
            {
                __instance.m_aoe *= definition.ApplyScalar(1f, value);
            }
        }
    }

    [HarmonyPatch(typeof(Player), nameof(Player.GetEquipmentEitrRegenModifier))]
    internal static class HeldEitrRegenModifierPatch
    {
        private static void Postfix(Player __instance, ref float __result)
        {
            AddHeld(__instance.m_rightItem, ref __result);
            if (!ReferenceEquals(__instance.m_leftItem, __instance.m_rightItem))
            {
                AddHeld(__instance.m_leftItem, ref __result);
            }
        }

        private static void AddHeld(ItemDrop.ItemData item, ref float result)
        {
            if (item != null &&
                ModifierCatalog.TryGetModifierValue(
                    item,
                    ModifierEffectType.EitrRegen,
                    out _,
                    out float value))
            {
                result += value;
            }
        }
    }
}
