using HarmonyLib;
using UnityEngine;

namespace IzzemisMod
{
    [HarmonyPatch(typeof(Humanoid), "BlockAttack")]
    internal static class ParryRewardModifierPatch
    {
        private static void Postfix(
            Humanoid __instance,
            HitData hit,
            Character attacker,
            bool __result)
        {
            if (!__result || attacker == null)
            {
                return;
            }

            ItemDrop.ItemData blocker = __instance.GetCurrentBlocker();
            // Same parry window as Humanoid.BlockAttack.
            if (blocker == null ||
                blocker.m_shared.m_timedBlockBonus <= 1f ||
                __instance.m_blockTimer == -1f ||
                __instance.m_blockTimer >= 0.25f)
            {
                return;
            }

            if (ModifierCatalog.TryGetModifierValue(
                    blocker, ModifierEffectType.ParryHealth, out _, out float health) &&
                health > 0f)
            {
                __instance.Heal(health);
            }

            if (ModifierCatalog.TryGetModifierValue(
                    blocker, ModifierEffectType.ParryStamina, out _, out float stamina) &&
                stamina > 0f)
            {
                __instance.AddStamina(stamina);
            }

            if (!hit.m_ranged &&
                ModifierCatalog.TryGetModifierValue(
                    blocker, ModifierEffectType.ParryDamage, out _, out float multiplier) &&
                multiplier > 0f)
            {
                float damage = blocker.GetBlockPower(
                    __instance.GetSkillFactor(Skills.SkillType.Blocking)) * multiplier;
                var counter = new HitData
                {
                    m_point = attacker.GetCenterPoint(),
                    m_dir = (attacker.transform.position - __instance.transform.position).normalized,
                    m_hitType = __instance.IsPlayer()
                        ? HitData.HitType.PlayerHit
                        : HitData.HitType.EnemyHit
                };
                counter.m_damage.m_blunt = damage;
                counter.SetAttacker(__instance);
                attacker.Damage(counter);
            }
        }
    }
}
