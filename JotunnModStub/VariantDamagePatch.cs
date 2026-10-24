using HarmonyLib;

namespace JotunnModStub
{
    [HarmonyPatch(
        typeof(ItemDrop.ItemData),
        nameof(ItemDrop.ItemData.GetDamage),
        new[] { typeof(int), typeof(float) })]
    internal static class VariantDamagePatch
    {
        private static void Postfix(ItemDrop.ItemData __instance, ref HitData.DamageTypes __result)
        {
            if (__instance == null || __instance.m_dropPrefab == null ||
                __instance.m_dropPrefab.name != VariantItemDataStore.RareSwordPrefabName)
            {
                return;
            }

            if (!VariantItemDataStore.TryGetModifierValue(
                __instance,
                VariantItemDataStore.SlashDamageModifierId,
                out float bonus))
            {
                return;
            }

            __result.m_slash *= 1f + bonus;
        }
    }
}
