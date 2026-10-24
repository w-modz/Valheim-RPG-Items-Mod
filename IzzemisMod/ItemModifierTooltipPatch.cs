using System.Collections.Generic;
using System.Globalization;
using HarmonyLib;

namespace IzzemisMod
{
    [HarmonyPatch(typeof(InventoryGrid), "CreateItemTooltip")]
    internal static class ItemModifierTooltipPatch
    {
        private static bool Prefix(
            InventoryGrid __instance,
            ItemDrop.ItemData item,
            UITooltip tooltip)
        {
            if (item == null ||
                !ItemModifierDataStore.TryLoad(item, out ItemModifierData data))
            {
                return true;
            }

            string title = Localization.instance.Localize(item.m_shared.m_name);
            if (ItemRarityCatalog.TryGet(data.RarityId, out ItemRarityDefinition rarityDefinition))
            {
                title +=
                    $" <color={rarityDefinition.TooltipColor}>({rarityDefinition.DisplayName})</color>";
            }

            var modifierLines = new List<string>();
            if (data.Modifiers != null)
            {
                foreach (ModifierRoll modifier in data.Modifiers)
                {
                    if (modifier == null || string.IsNullOrWhiteSpace(modifier.Id))
                    {
                        continue;
                    }

                    if (ModifierCatalog.TryGetById(modifier.Id, out ModifierDefinition definition))
                    {
                        modifierLines.Add(definition.FormatTooltipLine(modifier.Value));
                    }
                    else
                    {
                        string name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(
                            modifier.Id.Replace('_', ' ').ToLowerInvariant());
                        string value = modifier.Value.ToString(
                            "+0.##;-0.##;0", CultureInfo.InvariantCulture);
                        modifierLines.Add(
                            $"<color=#ADD8E6>{name}:</color> " +
                            $"<color=green>{value}</color>");
                    }
                }
            }

            string description = item.GetTooltip();
            if (modifierLines.Count > 0)
            {
                description = description.TrimEnd() +
                    "\n\n" +
                    string.Join("\n", modifierLines);
            }

            tooltip.Set(title, description, __instance.m_tooltipAnchor);
            return false;
        }
    }
}
