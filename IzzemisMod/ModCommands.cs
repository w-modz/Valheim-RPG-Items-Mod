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

        private static ItemDrop.ItemData GetHeldItem()
        {
            Player player = Player.m_localPlayer;
            return player == null ? null : player.GetCurrentWeapon();
        }

        private static bool IsWeapon(ItemDrop.ItemData item)
        {
            if (item == null)
            {
                return false;
            }

            switch (item.m_shared.m_itemType)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                case ItemDrop.ItemData.ItemType.Bow:
                case ItemDrop.ItemData.ItemType.Attach_Atgeir:
                    return true;
                default:
                    return false;
            }
        }

        private static bool TryGetHeldWeapon(out ItemDrop.ItemData item)
        {
            item = GetHeldItem();
            if (Player.m_localPlayer == null)
            {
                global::Console.instance.Print("No local player is available.");
                return false;
            }

            if (!IsWeapon(item))
            {
                global::Console.instance.Print(
                    "Hold a weapon to use this command. Tools and other items are not supported.");
                return false;
            }

            return true;
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
                "Sets the held weapon's rarity. Usage: set_rarity <rarity> (use 0 to clear rarity and modifiers).";

            public override List<string> CommandOptionList()
            {
                return ItemRarityCatalog.GetCommandOptions();
            }

            public override void Run(string[] args)
            {
                if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
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

                if (!TryGetHeldWeapon(out ItemDrop.ItemData item))
                {
                    return;
                }

                if (rarity == "0")
                {
                    ItemModifierDataStore.Clear(item);
                    ItemRarityVisuals.RefreshEquippedGlow(Player.m_localPlayer);
                    global::Console.instance.Print(
                        "Cleared rarity and modifiers from the held item.");
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
                    $"Set held item rarity to '{data.RarityId}'.");
            }
        }

        private sealed class ModifierCommand : ConsoleCommand
        {
            public override string Name => "modifier";

            public override string Help =>
                "Adds a modifier to the held weapon. Use autocomplete to see available modifiers.";

            public override List<string> CommandOptionList()
            {
                return ModifierCatalog.GetCommandOptions();
            }

            public override void Run(string[] args)
            {
                if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
                {
                    global::Console.instance.Print(Help);
                    return;
                }

                if (!TryGetModifierDefinition(args[0], out ModifierDefinition definition))
                {
                    return;
                }

                string modifierId = definition.Id;
                if (!TryGetHeldWeapon(out ItemDrop.ItemData item))
                {
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
                            $"Held item already has modifier '{modifierId}'.");
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
                    Value = definition.TestValue
                };
                data.Modifiers = updatedModifiers;

                ItemModifierDataStore.Save(item, data);
                global::Console.instance.Print(
                    $"Added '{modifierId}' to the held item.");
            }
        }

        private sealed class RemoveModifierCommand : ConsoleCommand
        {
            public override string Name => "remove_modifier";

            public override string Help =>
                "Removes a modifier from the held weapon. Use autocomplete to see available modifiers.";

            public override List<string> CommandOptionList()
            {
                return ModifierCatalog.GetCommandOptions();
            }

            public override void Run(string[] args)
            {
                if (args.Length != 1 || string.IsNullOrWhiteSpace(args[0]))
                {
                    global::Console.instance.Print(Help);
                    return;
                }

                if (!TryGetModifierDefinition(args[0], out ModifierDefinition definition))
                {
                    return;
                }

                string modifierId = definition.Id;
                if (!TryGetHeldWeapon(out ItemDrop.ItemData item))
                {
                    return;
                }

                if (!ItemModifierDataStore.TryLoad(item, out ItemModifierData data) ||
                    data.Modifiers == null || data.Modifiers.Length == 0)
                {
                    global::Console.instance.Print(
                        $"Held item does not have modifier '{modifierId}'.");
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
                        $"Held item does not have modifier '{modifierId}'.");
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
                    $"Removed '{modifierId}' from the held item.");
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
