using Jotunn.Entities;

namespace JotunnModStub
{
    internal sealed class DumpItemDataCommand : ConsoleCommand
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

                if (VariantItemDataStore.TryLoad(item, out VariantItemData variantData))
                {
                    global::Console.instance.Print(
                        $"  Variant data: rarity={variantData.RarityId}, version={variantData.Version}");

                    if (variantData.Modifiers == null || variantData.Modifiers.Length == 0)
                    {
                        global::Console.instance.Print("  Modifiers: <none>");
                    }
                    else
                    {
                        global::Console.instance.Print("  Modifiers:");
                        foreach (ModifierRoll modifier in variantData.Modifiers)
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
                    global::Console.instance.Print("  Variant data: <not found>");
                }

                if (item.m_customData == null || item.m_customData.Count == 0)
                {
                    global::Console.instance.Print("  Custom data: <empty>");
                    continue;
                }

                foreach (var entry in item.m_customData)
                {
                    global::Console.instance.Print($"  Custom data [{entry.Key}] = {entry.Value}");
                }
            }
        }
    }
}
