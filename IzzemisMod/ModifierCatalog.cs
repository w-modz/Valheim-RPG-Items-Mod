using System;
using System.Collections.Generic;
using System.Globalization;

namespace IzzemisMod
{
    internal enum ModifierDamageChannel
    {
        None,
        All,
        Slash,
        Pierce,
        Blunt,
        Chop,
        Pickaxe,
        Fire,
        Frost,
        Lightning,
        Poison,
        Spirit
    }

    internal enum ModifierValueType
    {
        Percentage,
        Flat
    }

    internal sealed class ModifierDefinition
    {
        public string Id { get; }
        public string CommandName { get; }
        public string DisplayName { get; }
        public float TestValue { get; }
        public ModifierDamageChannel DamageChannel { get; }
        public ModifierValueType ValueType { get; }

        public ModifierDefinition(
            string id,
            string commandName,
            string displayName,
            float testValue,
            ModifierDamageChannel damageChannel,
            ModifierValueType valueType)
        {
            Id = id;
            CommandName = commandName;
            DisplayName = displayName;
            TestValue = testValue;
            DamageChannel = damageChannel;
            ValueType = valueType;
        }

        public void ApplyDamage(ref HitData.DamageTypes damage, float value)
        {
            float appliedValue = ValueType == ModifierValueType.Percentage
                ? 1f + value
                : value;
            switch (DamageChannel)
            {
                case ModifierDamageChannel.All:
                    ApplyValue(ref damage.m_blunt, appliedValue);
                    ApplyValue(ref damage.m_slash, appliedValue);
                    ApplyValue(ref damage.m_pierce, appliedValue);
                    ApplyValue(ref damage.m_chop, appliedValue);
                    ApplyValue(ref damage.m_pickaxe, appliedValue);
                    ApplyValue(ref damage.m_fire, appliedValue);
                    ApplyValue(ref damage.m_frost, appliedValue);
                    ApplyValue(ref damage.m_lightning, appliedValue);
                    ApplyValue(ref damage.m_poison, appliedValue);
                    ApplyValue(ref damage.m_spirit, appliedValue);
                    break;
                case ModifierDamageChannel.Slash:
                    ApplyValue(ref damage.m_slash, appliedValue);
                    break;
                case ModifierDamageChannel.Pierce:
                    ApplyValue(ref damage.m_pierce, appliedValue);
                    break;
                case ModifierDamageChannel.Blunt:
                    ApplyValue(ref damage.m_blunt, appliedValue);
                    break;
                case ModifierDamageChannel.Chop:
                    ApplyValue(ref damage.m_chop, appliedValue);
                    break;
                case ModifierDamageChannel.Pickaxe:
                    ApplyValue(ref damage.m_pickaxe, appliedValue);
                    break;
                case ModifierDamageChannel.Fire:
                    ApplyValue(ref damage.m_fire, appliedValue);
                    break;
                case ModifierDamageChannel.Frost:
                    ApplyValue(ref damage.m_frost, appliedValue);
                    break;
                case ModifierDamageChannel.Lightning:
                    ApplyValue(ref damage.m_lightning, appliedValue);
                    break;
                case ModifierDamageChannel.Poison:
                    ApplyValue(ref damage.m_poison, appliedValue);
                    break;
                case ModifierDamageChannel.Spirit:
                    ApplyValue(ref damage.m_spirit, appliedValue);
                    break;
            }
        }

        private void ApplyValue(ref float damage, float value)
        {
            if (ValueType == ModifierValueType.Percentage)
            {
                damage *= value;
            }
            else
            {
                damage += value;
            }
        }

        public string FormatTooltipLine(float value)
        {
            string formattedValue;
            if (ValueType == ModifierValueType.Percentage)
            {
                formattedValue =
                    $"+{(value * 100f).ToString("0.##", CultureInfo.InvariantCulture)}%";
            }
            else
            {
                formattedValue = value.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture);
            }

            return $"<color=#ADD8E6>{DisplayName}:</color> " +
                $"<color=green>{formattedValue}</color>";
        }
    }

    internal static class ModifierCatalog
    {
        private static readonly ModifierDefinition[] Definitions =
        {
            new ModifierDefinition(
                "base_damage",
                "base_damage",
                "Base damage",
                0.2f,
                ModifierDamageChannel.All,
                ModifierValueType.Percentage),
            new ModifierDefinition(
                "fire_damage",
                "fire_damage",
                "Fire damage",
                10f,
                ModifierDamageChannel.Fire,
                ModifierValueType.Flat),
            new ModifierDefinition(
                "frost_damage",
                "frost_damage",
                "Frost damage",
                10f,
                ModifierDamageChannel.Frost,
                ModifierValueType.Flat),
            new ModifierDefinition(
                "lightning_damage",
                "lightning_damage",
                "Lightning damage",
                10f,
                ModifierDamageChannel.Lightning,
                ModifierValueType.Flat),
            new ModifierDefinition(
                "poison_damage",
                "poison_damage",
                "Poison damage",
                10f,
                ModifierDamageChannel.Poison,
                ModifierValueType.Flat),
            new ModifierDefinition(
                "spirit_damage",
                "spirit_damage",
                "Spirit damage",
                10f,
                ModifierDamageChannel.Spirit,
                ModifierValueType.Flat)
        };

        public static IReadOnlyList<ModifierDefinition> All => Definitions;

        public static List<string> GetCommandOptions()
        {
            var options = new List<string>(Definitions.Length);
            foreach (ModifierDefinition definition in Definitions)
            {
                options.Add(definition.CommandName);
            }

            return options;
        }

        public static bool TryGetByCommandName(string commandName, out ModifierDefinition definition)
        {
            foreach (ModifierDefinition candidate in Definitions)
            {
                if (string.Equals(
                    candidate.CommandName,
                    commandName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }

        public static bool TryGetById(string id, out ModifierDefinition definition)
        {
            foreach (ModifierDefinition candidate in Definitions)
            {
                if (string.Equals(candidate.Id, id, StringComparison.Ordinal))
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }
    }
}
