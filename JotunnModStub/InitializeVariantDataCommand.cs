using Jotunn.Entities;

namespace JotunnModStub
{
    internal sealed class InitializeVariantDataCommand : ConsoleCommand
    {
        private const string VariantPrefabName = "SwordBlackmetalRare";
        public override string Name => "initialize_variant_data";

        public override string Help => "Adds the test modifier to Rare Blackmetal Swords in your inventory.";

        public override void Run(string[] args)
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                global::Console.instance.Print("No local player is available.");
                return;
            }

            int updatedCount = 0;
            foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
            {
                if (item.m_dropPrefab == null || item.m_dropPrefab.name != VariantPrefabName)
                {
                    continue;
                }

                if (VariantItemDataStore.EnsureTestVariantData(item))
                {
                    updatedCount++;
                }
            }

            global::Console.instance.Print(
                $"Initialized variant data on {updatedCount} Rare Blackmetal Sword item(s).");
        }
    }
}
