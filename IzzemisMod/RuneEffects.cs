using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using UnityEngine;

namespace IzzemisMod
{
    internal static class RuneEffects
    {
        public const string ValheimRuneName = "Valheim";

        private const string ValheimModifierKey = "com.izzemi.izzemismod.rune.modifier";
        private const string ValheimValueKey = "com.izzemi.izzemismod.rune.value";
        private const string ValheimCategoryKey = "com.izzemi.izzemismod.rune.category";
        private const int NormalMaxModifiers = 4;
        private const int HelMaxModifiers = 5;
        private const float HelBoostMultiplier = 1.2f;

        // Rounds the magnitude up to the next multiple of the modifier's step, so boosts stay "whole".
        private static float RoundUpToStep(float value, string id)
        {
            ModifierCatalog.TryGetById(id, out ModifierDefinition definition);
            double step = definition.Step;
            double steps = Math.Ceiling(Math.Round(Math.Abs(value) / step, 4));
            return (float)Math.Round(Math.Sign(value) * steps * step, 4);
        }

        private static bool IsBoostable(ModifierRoll roll)
        {
            return roll != null &&
                ModifierCatalog.TryGetById(roll.Id, out ModifierDefinition definition) &&
                definition.Step > 0f;
        }

        public static string GetRuneName(ItemDrop.ItemData rune)
        {
            if (!RuneGui.IsRune(rune))
            {
                return null;
            }

            return rune.m_dropPrefab.name.Substring(RuneGui.RunePrefabPrefix.Length);
        }

        // Runes that stack together share a key; each Rune of Valheim is keyed by its stored modifier.
        public static string GetRuneKey(ItemDrop.ItemData rune)
        {
            string name = GetRuneName(rune);
            if (name == ValheimRuneName &&
                TryGetValheimData(rune, out string id, out float value, out ModifierItemCategory category))
            {
                return $"{name}|{id}|{value.ToString("R", CultureInfo.InvariantCulture)}|{(int)category}";
            }

            return name;
        }

        public static bool TryGetValheimData(
            ItemDrop.ItemData rune,
            out string modifierId,
            out float value,
            out ModifierItemCategory category)
        {
            modifierId = null;
            value = 0f;
            category = ModifierItemCategory.None;
            if (rune == null ||
                rune.m_customData == null ||
                !rune.m_customData.TryGetValue(ValheimModifierKey, out modifierId) ||
                !rune.m_customData.TryGetValue(ValheimValueKey, out string valueText) ||
                !rune.m_customData.TryGetValue(ValheimCategoryKey, out string categoryText))
            {
                return false;
            }

            if (!float.TryParse(valueText, NumberStyles.Float, CultureInfo.InvariantCulture, out value) ||
                !int.TryParse(categoryText, NumberStyles.Integer, CultureInfo.InvariantCulture, out int categoryValue))
            {
                return false;
            }

            category = (ModifierItemCategory)categoryValue;
            return ModifierCatalog.TryGetById(modifierId, out _);
        }

        public static string DescribeValheimRune(ItemDrop.ItemData rune)
        {
            if (!TryGetValheimData(rune, out string id, out float value, out ModifierItemCategory category) ||
                !ModifierCatalog.TryGetById(id, out ModifierDefinition definition))
            {
                return null;
            }

            return definition.FormatTooltipLine(value) +
                $"\n<color=#ADD8E6>Usable on:</color> {ModifierCatalog.GetCategoryDisplayName(category)}";
        }

        // Validates and (when commit is true) applies a rune to an item.
        public static bool Execute(
            Player player,
            ItemDrop.ItemData rune,
            ItemDrop.ItemData target,
            bool commit,
            out string message)
        {
            string name = GetRuneName(rune);
            if (name == null || target == null)
            {
                message = "Select a rune and an item.";
                return false;
            }

            if (!ModifierCatalog.TryGetItemCategory(target, out ModifierItemCategory category))
            {
                message = "This item cannot hold modifiers.";
                return false;
            }

            if (!ItemModifierDataStore.TryLoad(target, out ItemModifierData data))
            {
                data = new ItemModifierData();
            }

            var modifiers = new List<ModifierRoll>();
            if (data.Modifiers != null)
            {
                foreach (ModifierRoll roll in data.Modifiers)
                {
                    if (roll != null && !string.IsNullOrEmpty(roll.Id))
                    {
                        modifiers.Add(roll);
                    }
                }
            }

            ItemRarityCatalog.TryGet(data.RarityId, out ItemRarityDefinition rarity);
            bool destroysItem = name == "Muspelheim" || name == "Nidavellir";
            if (data.HelTouched && !destroysItem)
            {
                message = "This item was touched by Hel and can no longer be modified by this rune.";
                return false;
            }

            switch (name)
            {
                case "Midgard":
                    if (rarity != null)
                    {
                        message = "This rune only works on items without a rarity.";
                        return false;
                    }

                    return UpgradeRarity(target, data, modifiers, category, "magic", commit, out message);
                case "Vanaheim":
                    return RequireRarity(rarity, "magic", out message) &&
                        UpgradeRarity(target, data, modifiers, category, "rare", commit, out message);
                case "Alfheim":
                    return RequireRarity(rarity, "rare", out message) &&
                        UpgradeRarity(target, data, modifiers, category, "epic", commit, out message);
                case "Jotunheim":
                    return ApplyJotunheim(target, data, modifiers, rarity, commit, out message);
                case "Asgard":
                    return ApplyAsgard(target, data, modifiers, rarity, commit, out message);
                case "Niflheim":
                    return ApplyNiflheim(target, data, modifiers, rarity, commit, out message);
                case "Hel":
                    return ApplyHel(player, target, data, modifiers, rarity, commit, out message);
                case "Muspelheim":
                    return ApplyMuspelheim(player, target, rarity, commit, out message);
                case "Nidavellir":
                    return ApplyNidavellir(player, target, category, modifiers, commit, out message);
                case ValheimRuneName:
                    return ApplyValheim(target, data, modifiers, category, rarity, rune, commit, out message);
                default:
                    message = "This rune has no effect.";
                    return false;
            }
        }

        private static bool RequireRarity(ItemRarityDefinition rarity, string expectedId, out string message)
        {
            ItemRarityCatalog.TryGet(expectedId, out ItemRarityDefinition expected);
            if (rarity == null || rarity.Id != expectedId)
            {
                message = $"This rune only works on {expected.DisplayName} items.";
                return false;
            }

            message = null;
            return true;
        }

        private static bool UpgradeRarity(
            ItemDrop.ItemData target,
            ItemModifierData data,
            List<ModifierRoll> modifiers,
            ModifierItemCategory category,
            string newRarityId,
            bool commit,
            out string message)
        {
            ModifierDefinition added = PickNewModifier(target, modifiers, null);
            if (added == null)
            {
                message = "No modifiers are available for this item.";
                return false;
            }

            ItemRarityCatalog.TryGet(newRarityId, out ItemRarityDefinition newRarity);
            if (commit)
            {
                data.RarityId = newRarityId;
                modifiers.Add(new ModifierRoll { Id = added.Id, Value = added.Roll() });
                Save(target, data, modifiers);
            }

            message = $"The item is now {newRarity.DisplayName} and gained {added.DisplayName}.";
            return true;
        }

        private static bool ApplyJotunheim(
            ItemDrop.ItemData target,
            ItemModifierData data,
            List<ModifierRoll> modifiers,
            ItemRarityDefinition rarity,
            bool commit,
            out string message)
        {
            if (!RequireRarity(rarity, "epic", out message))
            {
                return false;
            }

            if (modifiers.Count >= NormalMaxModifiers)
            {
                message = "This item already has the maximum number of modifiers.";
                return false;
            }

            ModifierDefinition added = PickNewModifier(target, modifiers, null);
            if (added == null)
            {
                message = "No further modifiers are available for this item.";
                return false;
            }

            if (commit)
            {
                modifiers.Add(new ModifierRoll { Id = added.Id, Value = added.Roll() });
                Save(target, data, modifiers);
            }

            message = $"The item gained {added.DisplayName}.";
            return true;
        }

        private static bool ApplyAsgard(
            ItemDrop.ItemData target,
            ItemModifierData data,
            List<ModifierRoll> modifiers,
            ItemRarityDefinition rarity,
            bool commit,
            out string message)
        {
            if (rarity == null || modifiers.Count == 0)
            {
                message = "This item has no modifiers to remove.";
                return false;
            }

            ModifierRoll removed = modifiers[UnityEngine.Random.Range(0, modifiers.Count)];
            ModifierDefinition added = PickNewModifier(target, modifiers, removed.Id);
            if (added == null)
            {
                message = "No other modifiers are available for this item.";
                return false;
            }

            if (commit)
            {
                modifiers.Remove(removed);
                modifiers.Add(new ModifierRoll { Id = added.Id, Value = added.Roll() });
                Save(target, data, modifiers);
            }

            message = $"Replaced {NameOf(removed.Id)} with {added.DisplayName}.";
            return true;
        }

        private static bool ApplyNiflheim(
            ItemDrop.ItemData target,
            ItemModifierData data,
            List<ModifierRoll> modifiers,
            ItemRarityDefinition rarity,
            bool commit,
            out string message)
        {
            if (rarity == null || modifiers.Count == 0)
            {
                message = "This item has no modifiers to randomize.";
                return false;
            }

            if (commit)
            {
                foreach (ModifierRoll roll in modifiers)
                {
                    if (ModifierCatalog.TryGetById(roll.Id, out ModifierDefinition definition))
                    {
                        roll.Value = definition.Roll();
                    }
                }

                Save(target, data, modifiers);
            }

            message = "The values of all modifiers were rerolled.";
            return true;
        }

        private static bool ApplyHel(
            Player player,
            ItemDrop.ItemData target,
            ItemModifierData data,
            List<ModifierRoll> modifiers,
            ItemRarityDefinition rarity,
            bool commit,
            out string message)
        {
            if (!RequireRarity(rarity, "epic", out message))
            {
                return false;
            }

            ModifierDefinition bonus = modifiers.Count < HelMaxModifiers
                ? PickNewModifier(target, modifiers, null)
                : null;
            var outcomes = new List<string> { "destroy" };
            if (modifiers.Count > 0)
            {
                outcomes.Add("remove");
                outcomes.Add("boost");
            }

            if (bonus != null)
            {
                outcomes.Add("add");
            }

            if (!commit)
            {
                message = "Hel's outcome is unknown until the rune is applied.";
                return true;
            }

            string outcome = outcomes[UnityEngine.Random.Range(0, outcomes.Count)];
            switch (outcome)
            {
                case "destroy":
                    DestroyItem(player, target);
                    message = "Hel claims the item. It is destroyed.";
                    return true;
                case "remove":
                    ModifierRoll removed = modifiers[UnityEngine.Random.Range(0, modifiers.Count)];
                    modifiers.Remove(removed);
                    message = $"Hel strips {NameOf(removed.Id)} from the item.";
                    break;
                case "add":
                    modifiers.Add(new ModifierRoll { Id = bonus.Id, Value = bonus.Roll() });
                    message = $"Hel grants a bonus modifier: {bonus.DisplayName}.";
                    break;
                default:
                    foreach (ModifierRoll roll in modifiers)
                    {
                        if (IsBoostable(roll))
                        {
                            roll.Value = RoundUpToStep(roll.Value * HelBoostMultiplier, roll.Id);
                        }
                    }

                    message = modifiers.Exists(IsBoostable)
                        ? "Hel strengthens all non-fixed modifiers by 20%."
                        : "Hel tried to strengthen the modifiers, but all of them are fixed.";
                    break;
            }

            data.HelTouched = true;
            Save(target, data, modifiers);
            return true;
        }

        private static bool ApplyMuspelheim(
            Player player,
            ItemDrop.ItemData target,
            ItemRarityDefinition rarity,
            bool commit,
            out string message)
        {
            if (rarity == null)
            {
                message = "This rune only works on items of Magic rarity or higher.";
                return false;
            }

            string runeName = rarity.Id == "magic" ? "Midgard" : rarity.Id == "rare" ? "Vanaheim" : "Alfheim";
            if (commit)
            {
                DestroyItem(player, target);
                GiveRune(player, runeName, null);
            }

            message = $"The item is destroyed and you receive a Rune of {runeName}.";
            return true;
        }

        private static bool ApplyNidavellir(
            Player player,
            ItemDrop.ItemData target,
            ModifierItemCategory category,
            List<ModifierRoll> modifiers,
            bool commit,
            out string message)
        {
            if (modifiers.Count == 0)
            {
                message = "This item has no modifiers to extract.";
                return false;
            }

            if (commit)
            {
                ModifierRoll chosen = modifiers[UnityEngine.Random.Range(0, modifiers.Count)];
                DestroyItem(player, target);
                GiveRune(player, ValheimRuneName, chosen, category);
                message = $"The item is destroyed and you receive a Rune of Valheim holding {NameOf(chosen.Id)}.";
            }
            else
            {
                message = null;
            }

            return true;
        }

        private static bool ApplyValheim(
            ItemDrop.ItemData target,
            ItemModifierData data,
            List<ModifierRoll> modifiers,
            ModifierItemCategory category,
            ItemRarityDefinition rarity,
            ItemDrop.ItemData rune,
            bool commit,
            out string message)
        {
            if (!TryGetValheimData(rune, out string id, out float value, out ModifierItemCategory runeCategory) ||
                !ModifierCatalog.TryGetById(id, out ModifierDefinition definition))
            {
                message = "This Rune of Valheim holds no modifier.";
                return false;
            }

            if (category != runeCategory)
            {
                message = $"This rune can only be used on {ModifierCatalog.GetCategoryDisplayName(runeCategory)} items.";
                return false;
            }

            if (rarity == null)
            {
                message = "The item needs a rarity first.";
                return false;
            }

            if (modifiers.Count >= rarity.MaxModifiers)
            {
                message = $"{rarity.DisplayName} items can have at most {rarity.MaxModifiers} modifier(s).";
                return false;
            }

            if (modifiers.Exists(m => m.Id == id))
            {
                message = $"The item already has {definition.DisplayName}.";
                return false;
            }

            if (!definition.IsAllowedOn(target))
            {
                message = $"{definition.DisplayName} cannot be applied to this item.";
                return false;
            }

            if (commit)
            {
                modifiers.Add(new ModifierRoll { Id = id, Value = value });
                Save(target, data, modifiers);
            }

            message = $"The item gained {definition.DisplayName}.";
            return true;
        }

        private static ModifierDefinition PickNewModifier(
            ItemDrop.ItemData target,
            List<ModifierRoll> modifiers,
            string excludedId)
        {
            var pool = new List<ModifierDefinition>();
            foreach (ModifierDefinition definition in ModifierCatalog.All)
            {
                if (definition.Id != excludedId &&
                    definition.IsAllowedOn(target) &&
                    !modifiers.Exists(m => m.Id == definition.Id))
                {
                    pool.Add(definition);
                }
            }

            if (pool.Count == 0)
            {
                return null;
            }

            float total = 0f;
            foreach (ModifierDefinition definition in pool)
            {
                total += ModifierCatalog.GetWeight(definition.Id);
            }

            if (total <= 0f)
            {
                return pool[UnityEngine.Random.Range(0, pool.Count)];
            }

            float roll = UnityEngine.Random.Range(0f, total);
            foreach (ModifierDefinition definition in pool)
            {
                roll -= ModifierCatalog.GetWeight(definition.Id);
                if (roll < 0f)
                {
                    return definition;
                }
            }

            return pool[pool.Count - 1];
        }

        private static string NameOf(string modifierId)
        {
            return ModifierCatalog.TryGetById(modifierId, out ModifierDefinition definition)
                ? definition.DisplayName
                : modifierId;
        }

        private static void Save(ItemDrop.ItemData target, ItemModifierData data, List<ModifierRoll> modifiers)
        {
            data.Modifiers = modifiers.ToArray();
            ItemModifierDataStore.Save(target, data);
            ItemRarityVisuals.RefreshEquippedGlow(Player.m_localPlayer);
        }

        private static void DestroyItem(Player player, ItemDrop.ItemData item)
        {
            if (item.m_equipped)
            {
                player.UnequipItem(item, false);
            }

            player.GetInventory().RemoveItem(item);
            ItemRarityVisuals.RefreshEquippedGlow(player);
        }

        private static ItemDrop.ItemData CreateRuneItem(string runeName, ModifierRoll roll, ModifierItemCategory category)
        {
            GameObject prefab = ObjectDB.instance.GetItemPrefab(RuneGui.RunePrefabPrefix + runeName);
            ItemDrop drop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
            if (drop == null)
            {
                return null;
            }

            ItemDrop.ItemData item = drop.m_itemData.Clone();
            item.m_dropPrefab = prefab;
            item.m_stack = 1;
            item.m_customData = new Dictionary<string, string>();
            if (roll != null)
            {
                item.m_customData[ValheimModifierKey] = roll.Id;
                item.m_customData[ValheimValueKey] = roll.Value.ToString("R", CultureInfo.InvariantCulture);
                item.m_customData[ValheimCategoryKey] = ((int)category).ToString(CultureInfo.InvariantCulture);
            }

            return item;
        }

        // Drops a Rune of Valheim for a random item category holding a random modifier at its maximum value.
        public static void DropRandomValheimRune(Vector3 position)
        {
            var categories = new List<ModifierItemCategory>();
            foreach (ModifierItemCategory category in new[]
            {
                ModifierItemCategory.Weapon, ModifierItemCategory.Bow, ModifierItemCategory.Crossbow,
                ModifierItemCategory.MagicWeapon, ModifierItemCategory.Shield, ModifierItemCategory.Armour
            })
            {
                if (ModifierCatalog.All.Any(d => (d.AllowedItemCategories & category) != 0))
                {
                    categories.Add(category);
                }
            }

            if (categories.Count == 0)
            {
                return;
            }

            ModifierItemCategory chosenCategory = categories[UnityEngine.Random.Range(0, categories.Count)];
            var pool = ModifierCatalog.All.Where(d => (d.AllowedItemCategories & chosenCategory) != 0).ToList();
            ModifierDefinition chosen = pool[UnityEngine.Random.Range(0, pool.Count)];
            ItemDrop.ItemData item = CreateRuneItem(
                ValheimRuneName,
                new ModifierRoll { Id = chosen.Id, Value = chosen.StrongestValue },
                chosenCategory);
            if (item != null)
            {
                ItemDrop.DropItem(item, 1, position, Quaternion.identity);
            }
        }

        private static void GiveRune(Player player, string runeName, ModifierRoll roll, ModifierItemCategory category = ModifierItemCategory.None)
        {
            ItemDrop.ItemData item = CreateRuneItem(runeName, roll, category);
            if (item == null)
            {
                return;
            }

            if (!player.GetInventory().AddItem(item))
            {
                ItemDrop.DropItem(
                    item,
                    1,
                    player.transform.position + player.transform.forward + Vector3.up,
                    Quaternion.identity);
            }
        }
    }
}
