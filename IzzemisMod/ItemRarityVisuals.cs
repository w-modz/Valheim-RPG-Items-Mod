using UnityEngine;
using UnityEngine.UI;

namespace Runecarved
{
    internal static class ItemRarityVisuals
    {
        private const string GlowObjectName = "RunecarvedRarityGlow";
        private const float DropGlowRange = 4.5f;
        private const float DropGlowIntensity = 1.5f;
        private const float EquippedGlowRange = 4f;
        private const float EquippedGlowIntensity = 0.9f;

        public static bool TryGetRarity(ItemDrop.ItemData item, out ItemRarityDefinition rarity)
        {
            rarity = null;
            return ItemModifierDataStore.TryGetRarityId(item, out string rarityId) &&
                ItemRarityCatalog.TryGet(rarityId, out rarity);
        }

        public static void SetWorldDropGlow(ItemDrop itemDrop)
        {
            if (itemDrop == null)
            {
                return;
            }

            if (TryGetRarity(itemDrop.m_itemData, out ItemRarityDefinition rarity))
            {
                SetGlow(
                    itemDrop.transform,
                    rarity.GlowColor,
                    new Vector3(0f, 0.25f, 0f),
                    DropGlowRange,
                    DropGlowIntensity);
            }
            else
            {
                SetGlow(itemDrop.transform, null, Vector3.zero, 0f, 0f);
            }
        }

        public static void RefreshEquippedGlow(Humanoid humanoid)
        {
            if (humanoid == null)
            {
                return;
            }

            ItemRarityDefinition highestRarity = null;
            var inventory = humanoid.GetInventory();
            if (inventory != null)
            {
                foreach (ItemDrop.ItemData item in inventory.GetAllItems())
                {
                    if (!item.m_equipped ||
                        !TryGetRarity(item, out ItemRarityDefinition rarity))
                    {
                        continue;
                    }

                    if (highestRarity == null || rarity.Rank > highestRarity.Rank)
                    {
                        highestRarity = rarity;
                    }
                }
            }

            SetGlow(
                humanoid.transform,
                highestRarity?.GlowColor,
                new Vector3(0f, 1f, 0f),
                EquippedGlowRange,
                EquippedGlowIntensity);
        }

        public static void ApplyInventoryIconRarity(
            InventoryGrid grid,
            Inventory inventory,
            System.Collections.Generic.List<InventoryElement> elements)
        {
            if (grid == null || inventory == null || elements == null)
            {
                return;
            }

            foreach (InventoryElement element in elements)
            {
                Outline existingOutline = element.m_icon.GetComponent<Outline>();
                if (existingOutline != null)
                {
                    existingOutline.enabled = false;
                }
            }

            int width = inventory.GetWidth();
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (!TryGetRarity(item, out ItemRarityDefinition rarity))
                {
                    continue;
                }

                int index = item.m_gridPos.y * width + item.m_gridPos.x;
                if (index < 0 || index >= elements.Count)
                {
                    continue;
                }

                Image icon = elements[index].m_icon;
                Outline outline = icon.GetComponent<Outline>();
                if (outline == null)
                {
                    outline = icon.gameObject.AddComponent<Outline>();
                }

                Color outlineColor = rarity.GlowColor;
                outlineColor.a = 1f;
                outline.effectColor = outlineColor;
                outline.effectDistance = new Vector2(3f, 3f);
                outline.useGraphicAlpha = true;
                outline.enabled = true;
            }
        }

        private static void SetGlow(
            Transform owner,
            Color? color,
            Vector3 localPosition,
            float range,
            float intensity)
        {
            Transform glowTransform = owner.Find(GlowObjectName);
            if (!color.HasValue)
            {
                if (glowTransform != null)
                {
                    glowTransform.gameObject.SetActive(false);
                }

                return;
            }

            if (glowTransform == null)
            {
                var glowObject = new GameObject(GlowObjectName);
                glowTransform = glowObject.transform;
                glowTransform.SetParent(owner, false);
                Light light = glowObject.AddComponent<Light>();
                light.type = LightType.Point;
                light.shadows = LightShadows.None;
            }

            glowTransform.localPosition = localPosition;
            glowTransform.gameObject.SetActive(true);

            Light glow = glowTransform.GetComponent<Light>();
            glow.color = color.Value;
            glow.range = range;
            glow.intensity = intensity;
        }
    }
}
