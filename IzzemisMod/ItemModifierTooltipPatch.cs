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
            string runeDescription = RuneEffects.DescribeValheimRune(item);
            if (runeDescription != null)
            {
                tooltip.Set(
                    Localization.instance.Localize(item.m_shared.m_name),
                    item.GetTooltip().TrimEnd() + "\n\n" + runeDescription,
                    __instance.m_tooltipAnchor);
                return false;
            }

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
                int maxModifiers = rarityDefinition != null ? rarityDefinition.MaxModifiers : int.MaxValue;
                int index = -1;
                foreach (ModifierRoll modifier in data.Modifiers)
                {
                    index++;
                    if (modifier == null || string.IsNullOrWhiteSpace(modifier.Id))
                    {
                        continue;
                    }

                    if (ModifierCatalog.TryGetById(modifier.Id, out ModifierDefinition definition))
                    {
                        string line = definition.FormatTooltipLine(modifier.Value, index >= maxModifiers);
                        if (definition.EffectType == ModifierEffectType.ParryBonus)
                        {
                            float total = item.m_shared.m_timedBlockBonus + modifier.Value;
                            line += $" (total: {FormatMultiplier(total)})";
                        }
                        else if (definition.EffectType == ModifierEffectType.BackstabBonus)
                        {
                            float total = item.m_shared.m_backstabBonus + modifier.Value;
                            line += $" (total: {FormatMultiplier(total)})";
                        }

                        modifierLines.Add(line);
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
            if (data.HelTouched)
            {
                modifierLines.Add("<color=#9B30FF>Touched by Hel</color>");
            }
            if (modifierLines.Count > 0)
            {
                description = description.TrimEnd() +
                    "\n\n" +
                    string.Join("\n", modifierLines);
            }

            tooltip.Set(title, description, __instance.m_tooltipAnchor);
            return false;
        }

        private static string FormatMultiplier(float value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture) + "x";
        }
    }
}
