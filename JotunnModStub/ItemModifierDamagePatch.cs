using HarmonyLib;

namespace JotunnModStub
{
    [HarmonyPatch(
        typeof(ItemDrop.ItemData),
        nameof(ItemDrop.ItemData.GetDamage),
        new[] { typeof(int), typeof(float) })]
    internal static class ItemModifierDamagePatch
    {
        private static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
        {
            if (__instance == null)
            {
                return;
            }

            if (!ItemModifierDataStore.TryGetModifierValue(
                __instance,
                ItemModifierDataStore.SlashDamageModifierId,
                out float bonus))
            {
                return;
            }

            __result.m_slash *= 1f + bonus;
        }
    }
}
