using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;

namespace JotunnModStub
{
    internal partial class JotunnModStub
    {
        private void AddItemVariants()
        {
            // Create and add a custom item based on SwordBlackmetal
            ItemConfig variantSwordBlackmetalConfig = new ItemConfig
            {
                // Add custom variant properties here, for example:
                // Name = "SwordBlackmetalRare",
            };

            CustomItem variantSwordBlackmetalRare = new CustomItem("SwordBlackmetalRare", "SwordBlackmetal", variantSwordBlackmetalConfig);

            var shared = variantSwordBlackmetalRare.ItemDrop.m_itemData.m_shared;
            shared.m_name =
                $"{Localization.instance.Localize(shared.m_name)} (Rare)";

            ItemManager.Instance.AddItem(variantSwordBlackmetalRare);

            // You want that to run only once, Jotunn has the item cached for the game session
            PrefabManager.OnVanillaPrefabsAvailable -= AddItemVariants;
        }
    }
}