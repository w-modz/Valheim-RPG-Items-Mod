using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

namespace JotunnModStub
{
    // Stores one modifier's identifier and rolled value on an item.
    [Serializable]
    public sealed class ModifierRoll
    {
        // Stable modifier identifier used to look up its definition.
        public string Id;

        // Rolled value interpreted by the modifier definition.
        public float Value;
    }

    // Serializable per-instance metadata, including rarity and modifier rolls.
    [Serializable]
    public sealed class ItemModifierData
    {
        // Version of this saved data format, not the item's rarity.
        public int Version = 1;

        // Identifier of the item's rarity, such as "rare".
        public string RarityId;

        // The modifiers rolled for this specific item instance.
        public ModifierRoll[] Modifiers = new ModifierRoll[0];
    }

    // Reads and writes item metadata in an item's custom data dictionary.
    internal static class ItemModifierDataStore
    {
        private const string CustomDataPrefix = "com.izzemi.myfirstmod.variant";
        private const string LegacyCustomDataKey = CustomDataPrefix;
        private const string VersionKey = CustomDataPrefix + ".version";
        private const string RarityKey = CustomDataPrefix + ".rarity";
        private const string ModifierKeyPrefix = CustomDataPrefix + ".modifier.";
        internal const string SlashDamageModifierId = "bonus_slash_damage";
        internal const float TestSlashDamageBonus = 2.0f;

        public static void Clear(ItemDrop.ItemData item)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (item.m_customData == null)
            {
                return;
            }

            var keysToRemove = new List<string>();
            foreach (string key in item.m_customData.Keys)
            {
                if (key == LegacyCustomDataKey ||
                    key.StartsWith(CustomDataPrefix + ".", StringComparison.Ordinal))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (string key in keysToRemove)
            {
                item.m_customData.Remove(key);
            }
        }

        // Reads a modifier's stored numeric value without deserializing all variant data.
        public static bool TryGetModifierValue(ItemDrop.ItemData item, string modifierId, out float value)
        {
            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            value = 0f;
            if (item.m_customData == null ||
                !item.m_customData.TryGetValue(ModifierKeyPrefix + modifierId, out string storedValue))
            {
                return false;
            }

            if (float.TryParse(storedValue, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return true;
            }

            Jotunn.Logger.LogError(
                $"Invalid saved value '{storedValue}' for variant modifier '{modifierId}'.");
            return false;
        }

        // Stores variant metadata as separate persistent item custom-data entries.
        public static void Save(ItemDrop.ItemData item, ItemModifierData data)
        {
            Jotunn.Logger.LogInfo(
                $"ItemModifierDataStore.Save called for '{item?.m_dropPrefab?.name ?? "<null>"}'.");

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            if (item.m_customData == null)
            {
                item.m_customData = new Dictionary<string, string>();
            }

            var keysToRemove = new List<string>();
            foreach (string key in item.m_customData.Keys)
            {
                if (key == LegacyCustomDataKey || key.StartsWith(CustomDataPrefix + ".", StringComparison.Ordinal))
                {
                    keysToRemove.Add(key);
                }
            }

            foreach (string key in keysToRemove)
            {
                item.m_customData.Remove(key);
            }

            item.m_customData[VersionKey] = data.Version.ToString(CultureInfo.InvariantCulture);
            item.m_customData[RarityKey] = data.RarityId ?? string.Empty;

            if (data.Modifiers == null)
            {
                return;
            }

            foreach (ModifierRoll modifier in data.Modifiers)
            {
                if (modifier == null || string.IsNullOrEmpty(modifier.Id))
                {
                    continue;
                }

                item.m_customData[ModifierKeyPrefix + modifier.Id] =
                    modifier.Value.ToString("R", CultureInfo.InvariantCulture);
            }
        }

        // Loads variant metadata, supporting the previous JSON format for existing items.
        public static bool TryLoad(ItemDrop.ItemData item, out ItemModifierData data)
        {
            Jotunn.Logger.LogInfo(
                $"ItemModifierDataStore.TryLoad called for '{item?.m_dropPrefab?.name ?? "<null>"}'.");

            if (item == null)
            {
                throw new ArgumentNullException(nameof(item));
            }

            data = null;
            if (item.m_customData == null)
            {
                return false;
            }

            if (item.m_customData.TryGetValue(LegacyCustomDataKey, out string json))
            {
                data = JsonUtility.FromJson<ItemModifierData>(json);
            }

            if (data == null &&
                !item.m_customData.ContainsKey(VersionKey) &&
                !item.m_customData.ContainsKey(RarityKey))
            {
                return false;
            }

            data = data ?? new ItemModifierData();
            if (item.m_customData.TryGetValue(VersionKey, out string version) &&
                int.TryParse(version, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsedVersion))
            {
                data.Version = parsedVersion;
            }

            if (item.m_customData.TryGetValue(RarityKey, out string rarity))
            {
                data.RarityId = rarity;
            }

            var modifiers = new List<ModifierRoll>();
            foreach (KeyValuePair<string, string> entry in item.m_customData)
            {
                if (!entry.Key.StartsWith(ModifierKeyPrefix, StringComparison.Ordinal))
                {
                    continue;
                }

                string modifierId = entry.Key.Substring(ModifierKeyPrefix.Length);
                if (float.TryParse(entry.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float value))
                {
                    modifiers.Add(new ModifierRoll { Id = modifierId, Value = value });
                }
                else
                {
                    Jotunn.Logger.LogError(
                        $"Invalid saved value '{entry.Value}' for variant modifier '{modifierId}'.");
                }
            }

            data.Modifiers = modifiers.ToArray();
            return true;
        }
    }
}
