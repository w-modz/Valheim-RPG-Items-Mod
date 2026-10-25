using System.Collections.Generic;
using Jotunn.Entities;
using Jotunn.Managers;

namespace IzzemisMod
{
    internal static class ModCommands
    {
        public static void Register()
        {
            CommandManager.Instance.AddConsoleCommand(new SetRarityCommand());
            CommandManager.Instance.AddConsoleCommand(new ModifierCommand());
            CommandManager.Instance.AddConsoleCommand(new RemoveModifierCommand());
            CommandManager.Instance.AddConsoleCommand(new DumpItemDataCommand());
        }

        private static bool IsWeaponCategory(ModifierItemCategory category)
        {
            return category == ModifierItemCategory.Weapon ||
                category == ModifierItemCategory.Bow ||
                category == ModifierItemCategory.Crossbow ||
                category == ModifierItemCategory.MagicWeapon;
        }

        private static void AddIfSupported(
            List<ItemDrop.ItemData> items,
            ItemDrop.ItemData item)
        {
            if (!ModifierCatalog.TryGetItemCategory(item, out _))
            {
                return;
            }

            foreach (ItemDrop.ItemData existing in items)
            {
                if (ReferenceEquals(existing, item))
                {
                    return;
                }
            }

            items.Add(item);
        }

        private static List<ItemDrop.ItemData> GetEquippedArmourItems(Player player)
        {
            var items = new List<ItemDrop.ItemData>();
            AddIfSupported(items, player.m_helmetItem);
            AddIfSupported(items, player.m_chestItem);
            AddIfSupported(items, player.m_legItem);
            AddIfSupported(items, player.m_shoulderItem);
            return items;
        }

        private static List<ItemDrop.ItemData> GetSupportedEquippedItems(Player player)
        {
            var items = new List<ItemDrop.ItemData>();
            AddIfSupported(items, player.GetCurrentWeapon());
            if (player.m_leftItem != null &&
                player.m_leftItem.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
            {
                AddIfSupported(items, player.m_leftItem);
            }

            foreach (ItemDrop.ItemData armourItem in GetEquippedArmourItems(player))
            {
                AddIfSupported(items, armourItem);
            }

            return items;
        }

        private static bool TryGetOneItem(
            List<ItemDrop.ItemData> items,
            string slot,
            out ItemDrop.ItemData item)
        {
            item = null;
            if (items.Count == 1)
            {
                item = items[0];
                return true;
            }

            if (items.Count == 0)
            {
                global::Console.instance.Print($"No equipped {slot} was found.");
            }
            else
            {
                global::Console.instance.Print(
                    $"More than one {slot} is equipped. Specify a specific slot.");
            }

            return false;
        }

        private static bool TryGetEquippedItem(string slotArgument, out ItemDrop.ItemData item)
        {
            item = null;
            if (Player.m_localPlayer == null)
            {
                global::Console.instance.Print("No local player is available.");
                return false;
            }

            Player player = Player.m_localPlayer;
            if (string.IsNullOrWhiteSpace(slotArgument))
            {
                List<ItemDrop.ItemData> equippedItems = GetSupportedEquippedItems(player);
                if (equippedItems.Count == 1)
                {
                    item = equippedItems[0];
                    return true;
                }

                global::Console.instance.Print(
                    equippedItems.Count == 0
                        ? "No supported weapon, shield, or armour is equipped."
                        : "Multiple supported items are equipped. Specify weapon, shield, armour, helmet, chest, legs, or cape.");
                return false;
            }

            string slot = slotArgument.Trim().ToLowerInvariant();
            switch (slot)
            {
                case "weapon":
                    item = player.GetCurrentWeapon();
                    if (ModifierCatalog.TryGetItemCategory(item, out ModifierItemCategory weaponCategory) &&
                        IsWeaponCategory(weaponCategory))
                    {
                        return true;
                    }

                    global::Console.instance.Print("No supported weapon is equipped in the weapon slot.");
                    return false;
                case "bow":
                case "crossbow":
                case "magic":
                case "magic_weapon":
                    item = player.GetCurrentWeapon();
                    if (ModifierCatalog.TryGetItemCategory(item, out ModifierItemCategory category) &&
                        ((slot == "bow" && category == ModifierItemCategory.Bow) ||
                         (slot == "crossbow" && category == ModifierItemCategory.Crossbow) ||
                         ((slot == "magic" || slot == "magic_weapon") &&
                          category == ModifierItemCategory.MagicWeapon)))
                    {
                        return true;
                    }

                    global::Console.instance.Print($"No equipped {slot.Replace('_', ' ')} was found.");
                    return false;
                case "shield":
                    var shields = new List<ItemDrop.ItemData>();
                    AddIfShield(shields, player.m_leftItem);
                    AddIfShield(shields, player.m_rightItem);
                    return TryGetOneItem(shields, "shield", out item);
                case "armour":
                    return TryGetOneItem(GetEquippedArmourItems(player), "armour piece", out item);
                case "helmet":
                    item = player.m_helmetItem;
                    break;
                case "chest":
                    item = player.m_chestItem;
                    break;
                case "legs":
                    item = player.m_legItem;
                    break;
                case "cape":
                    item = player.m_shoulderItem;
                    break;
                default:
                    global::Console.instance.Print(
                        "Invalid slot. Use weapon, shield, armour, helmet, chest, legs, cape, bow, crossbow, or magic.");
                    return false;
            }

            if (ModifierCatalog.TryGetItemCategory(item, out ModifierItemCategory armourCategory) &&
                armourCategory == ModifierItemCategory.Armour)
            {
                return true;
            }

            global::Console.instance.Print($"No equipped {slot} was found.");
            return false;
        }

        private static void AddIfShield(List<ItemDrop.ItemData> shields, ItemDrop.ItemData item)
        {
            if (item != null &&
                item.m_shared.m_itemType == ItemDrop.ItemData.ItemType.Shield)
            {
                AddIfSupported(shields, item);
            }
        }

        private static bool TryGetEquippedItem(string[] args, int requiredArgumentCount, out ItemDrop.ItemData item)
        {
            item = null;
            if (args.Length < requiredArgumentCount || args.Length > requiredArgumentCount + 1)
            {
                return false;
            }

            return TryGetEquippedItem(
                args.Length == requiredArgumentCount ? null : args[requiredArgumentCount],
                out item);
        }

        private static bool TryGetModifierDefinition(
            string commandName,
            out ModifierDefinition definition)
        {
            if (ModifierCatalog.TryGetByCommandName(commandName.Trim(), out definition))
            {
                return true;
            }

            global::Console.instance.Print(
                $"Unknown modifier. Available modifiers: {string.Join(", ", ModifierCatalog.GetCommandOptions())}.");
            return false;
        }

        private static int GetModifierCount(ModifierRoll[] modifiers)
        {
            if (modifiers == null)
            {
                return 0;
            }

            int count = 0;
            foreach (ModifierRoll modifier in modifiers)
            {
                if (modifier != null && !string.IsNullOrWhiteSpace(modifier.Id))
                {
                    count++;
                }
            }

            return count;
        }

        private sealed class SetRarityCommand : ConsoleCommand
        {
            public override string Name => "set_rarity";

            public override string Help =>
                "Sets rarity on an equipped item. Usage: set_rarity <rarity> [slot] (use 0 to clear rarity and modifiers).";

            public override List<string> CommandOptionList()
            {
                return ItemRarityCatalog.GetCommandOptions();
            }

            public override void Run(string[] args)
            {
                if (args.Length < 1 || string.IsNullOrWhiteSpace(args[0]))
                {
                    global::Console.instance.Print(Help);
                    return;
                }

                if (args.Length > 2)
                {
                    global::Console.instance.Print(Help);
                    return;
                }

                string rarity = args[0].Trim().ToLowerInvariant();
                ItemRarityDefinition rarityDefinition = null;
                if (rarity != "0" &&
                    !ItemRarityCatalog.TryGet(rarity, out rarityDefinition))
                {
                    global::Console.instance.Print(
                        $"Invalid rarity '{args[0]}'. Valid rarities are magic, rare, and epic; use 0 to remove rarity and modifiers.");
                    return;
                }

                if (!TryGetEquippedItem(args, 1, out ItemDrop.ItemData item))
                {
                    return;
                }

                if (rarity == "0")
                {
                    ItemModifierDataStore.Clear(item);
                    ItemRarityVisuals.RefreshEquippedGlow(Player.m_localPlayer);
                    global::Console.instance.Print(
                        "Cleared rarity and modifiers from the equipped item.");
                    return;
                }

                if (!ItemModifierDataStore.TryLoad(item, out ItemModifierData data))
                {
                    data = new ItemModifierData();
                }

                int modifierCount = GetModifierCount(data.Modifiers);
                if (modifierCount > rarityDefinition.MaxModifiers)
                {
                    global::Console.instance.Print(
                        $"Cannot set rarity to '{rarity}': the item has {modifierCount} modifiers, but {rarityDefinition.DisplayName} allows at most {rarityDefinition.MaxModifiers}. Remove modifiers first.");
                    return;
                }

                data.RarityId = rarity;
                ItemModifierDataStore.Save(item, data);
                ItemRarityVisuals.RefreshEquippedGlow(Player.m_localPlayer);
                global::Console.instance.Print(
                    $"Set equipped item rarity to '{data.RarityId}'.");
            }
        }

        private sealed class ModifierCommand : ConsoleCommand
        {
            public override string Name => "modifier";

            public override string Help =>
                "Adds a modifier to an equipped item at its max value. Usage: modifier <modifier> [value] [slot]. Value uses tooltip units (25 = 25% for percentage modifiers) and may exceed the normal range.";

            public override List<string> CommandOptionList()
            {
                return ModifierCatalog.GetCommandOptions();
            }

            public override void Run(string[] args)
            {
                if (args.Length < 1 || string.IsNullOrWhiteSpace(args[0]) || args.Length > 3)
                {
                    global::Console.instance.Print(Help);
                    return;
                }

                if (!TryGetModifierDefinition(args[0], out ModifierDefinition definition))
                {
                    return;
                }

                float? customValue = null;
                string slotArgument = null;
                for (int i = 1; i < args.Length; i++)
                {
                    if (customValue == null &&
                        float.TryParse(args[i], System.Globalization.NumberStyles.Float,
                            System.Globalization.CultureInfo.InvariantCulture, out float parsed))
                    {
                        customValue = definition.FromDisplayValue(parsed);
                    }
                    else if (slotArgument == null)
                    {
                        slotArgument = args[i];
                    }
                    else
                    {
                        global::Console.instance.Print(Help);
                        return;
                    }
                }

                string modifierId = definition.Id;
                string modifierName = definition.CommandName;
                if (!TryGetEquippedItem(slotArgument, out ItemDrop.ItemData item))
                {
                    return;
                }

                if (!definition.IsAllowedOn(item) &&
                    ModifierCatalog.TryGetItemCategory(item, out ModifierItemCategory category))
                {
                    if (category == ModifierItemCategory.Shield &&
                        ModifierCatalog.IsTowerShield(item) &&
                        (definition.EffectType == ModifierEffectType.ParryBonus ||
                         definition.EffectType == ModifierEffectType.ParryAdrenaline))
                    {
                        global::Console.instance.Print(
                            "Parry modifiers are not available for tower shields.");
                    }
                    else
                    {
                        global::Console.instance.Print(
                            $"Modifier '{modifierName}' is not compatible with {ModifierCatalog.GetCategoryDisplayName(category)} items.");
                    }

                    return;
                }

                if (!ItemModifierDataStore.TryLoad(item, out ItemModifierData data))
                {
                    data = new ItemModifierData();
                }

                var modifiers = data.Modifiers ?? new ModifierRoll[0];
                foreach (ModifierRoll modifier in modifiers)
                {
                    if (modifier != null && modifier.Id == modifierId)
                    {
                        global::Console.instance.Print(
                            $"Equipped item already has modifier '{modifierName}'.");
                        return;
                    }
                }

                if (!ItemRarityCatalog.TryGet(data.RarityId, out ItemRarityDefinition rarityDefinition))
                {
                    global::Console.instance.Print(
                        "Set a rarity before adding modifiers. Magic allows 1, Rare allows 2, and Epic allows 4 modifiers.");
                    return;
                }

                int modifierCount = GetModifierCount(modifiers);
                if (modifierCount >= rarityDefinition.MaxModifiers)
                {
                    global::Console.instance.Print(
                        $"{rarityDefinition.DisplayName} items can have at most {rarityDefinition.MaxModifiers} modifier(s).");
                    return;
                }

                var updatedModifiers = new ModifierRoll[modifiers.Length + 1];
                modifiers.CopyTo(updatedModifiers, 0);
                updatedModifiers[updatedModifiers.Length - 1] = new ModifierRoll
                {
                    Id = modifierId,
                    Value = customValue ?? definition.StrongestValue
                };
                data.Modifiers = updatedModifiers;

                ItemModifierDataStore.Save(item, data);
                global::Console.instance.Print(
                    $"Added '{modifierName}' to the equipped item ({definition.FormatTooltipLine(updatedModifiers[updatedModifiers.Length - 1].Value)}).");
            }
        }

        private sealed class RemoveModifierCommand : ConsoleCommand
        {
            public override string Name => "remove_modifier";

            public override string Help =>
                "Removes a modifier from an equipped item. Usage: remove_modifier <modifier> [slot].";

            public override List<string> CommandOptionList()
            {
                return ModifierCatalog.GetCommandOptions();
            }

            public override void Run(string[] args)
            {
                if (args.Length < 1 || string.IsNullOrWhiteSpace(args[0]) || args.Length > 2)
                {
                    global::Console.instance.Print(Help);
                    return;
                }

                if (!TryGetModifierDefinition(args[0], out ModifierDefinition definition))
                {
                    return;
                }

                string modifierId = definition.Id;
                string modifierName = definition.CommandName;
                if (!TryGetEquippedItem(args, 1, out ItemDrop.ItemData item))
                {
                    return;
                }

                if (!ItemModifierDataStore.TryLoad(item, out ItemModifierData data) ||
                    data.Modifiers == null || data.Modifiers.Length == 0)
                {
                    global::Console.instance.Print(
                        $"Equipped item does not have modifier '{modifierName}'.");
                    return;
                }

                int remainingCount = 0;
                foreach (ModifierRoll modifier in data.Modifiers)
                {
                    if (modifier == null || modifier.Id != modifierId)
                    {
                        remainingCount++;
                    }
                }

                if (remainingCount == data.Modifiers.Length)
                {
                    global::Console.instance.Print(
                        $"Equipped item does not have modifier '{modifierName}'.");
                    return;
                }

                var remainingModifiers = new ModifierRoll[remainingCount];
                int nextIndex = 0;
                foreach (ModifierRoll modifier in data.Modifiers)
                {
                    if (modifier == null || modifier.Id != modifierId)
                    {
                        remainingModifiers[nextIndex++] = modifier;
                    }
                }

                data.Modifiers = remainingModifiers;
                ItemModifierDataStore.Save(item, data);
                global::Console.instance.Print(
                    $"Removed '{modifierName}' from the equipped item.");
            }
        }

        private sealed class DumpItemDataCommand : ConsoleCommand
        {
            public override string Name => "dump_item_data";

            public override string Help => "Prints the local player's inventory item data.";

            public override void Run(string[] args)
            {
                Player player = Player.m_localPlayer;
                if (player == null)
                {
                    global::Console.instance.Print("No local player is available.");
                    return;
                }

                var items = player.GetInventory().GetAllItems();
                global::Console.instance.Print($"Inventory contains {items.Count} item entries.");

                for (int index = 0; index < items.Count; index++)
                {
                    ItemDrop.ItemData item = items[index];
                    var shared = item.m_shared;

                    global::Console.instance.Print(
                        $"Item {index}: prefab={item.m_dropPrefab?.name ?? "<none>"}, " +
                        $"name={shared.m_name}, quality={item.m_quality}, stack={item.m_stack}, " +
                        $"durability={item.m_durability}, worldLevel={item.m_worldLevel}, " +
                        $"variant={item.m_variant}, equipped={item.m_equipped}, " +
                        $"pickedUp={item.m_pickedUp}, crafter={item.m_crafterName} ({item.m_crafterID})");

                    global::Console.instance.Print(
                        $"  Base damage: {shared.m_damages.GetTooltipString()}");

                    var calculatedDamage = item.GetDamage();
                    global::Console.instance.Print(
                        $"  Calculated damage: {calculatedDamage.GetTooltipString()}");

                    if (ItemModifierDataStore.TryLoad(item, out ItemModifierData itemData))
                    {
                        global::Console.instance.Print(
                            $"  Item data: rarity={itemData.RarityId}, version={itemData.Version}");

                        if (itemData.Modifiers == null || itemData.Modifiers.Length == 0)
                        {
                            global::Console.instance.Print("  Modifiers: <none>");
                        }
                        else
                        {
                            global::Console.instance.Print("  Modifiers:");
                            foreach (ModifierRoll modifier in itemData.Modifiers)
                            {
                                if (modifier != null)
                                {
                                    global::Console.instance.Print(
                                        $"    {modifier.Id} = {modifier.Value}");
                                }
                            }
                        }
                    }
                    else
                    {
                        global::Console.instance.Print("  Item data: <not found>");
                    }

                    if (item.m_customData == null || item.m_customData.Count == 0)
                    {
                        global::Console.instance.Print("  Custom data: <empty>");
                        continue;
                    }

                    foreach (var entry in item.m_customData)
                    {
                        global::Console.instance.Print(
                            $"  Custom data [{entry.Key}] = {entry.Value}");
                    }
                }
            }
        }
    }
}
