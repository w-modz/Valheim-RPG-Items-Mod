using HarmonyLib;

namespace IzzemisMod
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

            foreach (ModifierDefinition definition in ModifierCatalog.All)
            {
                if (definition.DamageChannel == ModifierDamageChannel.None ||
                    !definition.IsAllowedOn(__instance) ||
                    !ItemModifierDataStore.TryGetModifierValue(
                        __instance,
                        definition.Id,
                        out float value))
                {
                    continue;
                }

                definition.ApplyDamage(ref __result, value);
            }
        }
    }
}
