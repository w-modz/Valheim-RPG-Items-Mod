using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;

namespace JotunnModStub
{
    internal partial class JotunnModStub
    {
        private void AddClonedItems()
        {
            // Create and add a custom item based on SwordBlackmetal
            ItemConfig evilSwordConfig = new ItemConfig();
            evilSwordConfig.Name = "$Evil Sword";
            evilSwordConfig.Description = "$Test description for Evil Sword";
            evilSwordConfig.CraftingStation = CraftingStations.Workbench;
            evilSwordConfig.AddRequirement("Stone", 1, 1);
            evilSwordConfig.AddRequirement("Wood", 1, 2);

            CustomItem evilSword = new CustomItem("EvilSword", "SwordBlackmetal", evilSwordConfig);

            var shared = evilSword.ItemDrop.m_itemData.m_shared;

            shared.m_damages.m_slash = 100;
            shared.m_damages.m_pierce = 50;
            shared.m_blockPower = 77;

            ItemManager.Instance.AddItem(evilSword);

            // You want that to run only once, Jotunn has the item cached for the game session
            PrefabManager.OnVanillaPrefabsAvailable -= AddClonedItems;
        }
    }
}