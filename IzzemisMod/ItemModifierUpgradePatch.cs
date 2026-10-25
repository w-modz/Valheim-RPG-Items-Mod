using System;
using System.Collections.Generic;
using HarmonyLib;

namespace IzzemisMod
{
    [HarmonyPatch(
        typeof(InventoryGui),
        nameof(InventoryGui.DoCrafting),
        new[] { typeof(Player) })]
    internal static class ItemModifierUpgradePatch
    {
        private struct State
        {
            public bool Active;
            public Player Player;
            public ItemDrop.ItemData SourceItem;
            public ItemModifierData ModifierData;
            public List<ItemDrop.ItemData> ExistingItems;
            public string SharedName;
            public int Quality;
            public int Variant;
        }

        private static void Prefix(InventoryGui __instance, Player player, out State __state)
        {
            __state = default(State);
            ItemDrop.ItemData sourceItem = __instance.m_craftUpgradeItem;
            if (sourceItem == null ||
                player == null ||
                !ItemModifierDataStore.TryLoad(sourceItem, out ItemModifierData data))
            {
                return;
            }

            __state.Active = true;
            __state.Player = player;
            __state.SourceItem = sourceItem;
            __state.ModifierData = Clone(data);
            __state.ExistingItems = new List<ItemDrop.ItemData>(
                player.GetInventory().GetAllItems());
            __state.SharedName = sourceItem.m_shared.m_name;
            __state.Quality = sourceItem.m_quality;
            __state.Variant = sourceItem.m_variant;
        }

        private static void Postfix(State __state)
        {
            if (!__state.Active ||
                ContainsReference(
                    __state.Player.GetInventory().GetAllItems(),
                    __state.SourceItem))
            {
                return;
            }

            foreach (ItemDrop.ItemData item in __state.Player.GetInventory().GetAllItems())
            {
                if (!ContainsReference(__state.ExistingItems, item) &&
                    item.m_shared.m_name == __state.SharedName &&
                    item.m_quality != __state.Quality &&
                    item.m_variant == __state.Variant)
                {
                    ItemModifierDataStore.Save(item, __state.ModifierData);
                    return;
                }
            }
        }

        private static bool ContainsReference(
            List<ItemDrop.ItemData> items,
            ItemDrop.ItemData target)
        {
            foreach (ItemDrop.ItemData item in items)
            {
                if (ReferenceEquals(item, target))
                {
                    return true;
                }
            }

            return false;
        }

        private static ItemModifierData Clone(ItemModifierData data)
        {
            var modifiers = new List<ModifierRoll>();
            if (data.Modifiers != null)
            {
                foreach (ModifierRoll modifier in data.Modifiers)
                {
                    if (modifier != null)
                    {
                        modifiers.Add(new ModifierRoll
                        {
                            Id = modifier.Id,
                            Value = modifier.Value
                        });
                    }
                }
            }

            return new ItemModifierData
            {
                Version = data.Version,
                RarityId = data.RarityId,
                HelTouched = data.HelTouched,
                Modifiers = modifiers.ToArray()
            };
        }
    }
}
