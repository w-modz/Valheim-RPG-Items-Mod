using HarmonyLib;

namespace Runecarved
{
    [HarmonyPatch(typeof(ItemDrop), nameof(ItemDrop.Start))]
    internal static class WorldDropRarityGlowPatch
    {
        private static void Postfix(ItemDrop __instance)
        {
            ItemRarityVisuals.SetWorldDropGlow(__instance);
        }
    }

    [HarmonyPatch(
        typeof(InventoryGrid),
        "UpdateInventory",
        new[] { typeof(Inventory), typeof(Player), typeof(ItemDrop.ItemData) })]
    internal static class InventoryIconRarityGlowPatch
    {
        private static void Postfix(
            InventoryGrid __instance,
            Inventory ___m_inventory,
            System.Collections.Generic.List<InventoryElement> ___m_elements)
        {
            ItemRarityVisuals.ApplyInventoryIconRarity(
                __instance,
                ___m_inventory,
                ___m_elements);
        }
    }

    [HarmonyPatch(typeof(Humanoid), "EquipItem")]
    internal static class EquipRarityGlowPatch
    {
        private static void Postfix(Humanoid __instance)
        {
            ItemRarityVisuals.RefreshEquippedGlow(__instance);
        }
    }

    [HarmonyPatch(typeof(Humanoid), "UnequipItem")]
    internal static class UnequipRarityGlowPatch
    {
        private static void Postfix(Humanoid __instance)
        {
            ItemRarityVisuals.RefreshEquippedGlow(__instance);
        }
    }
}
