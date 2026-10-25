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
        PercentagePoints,
        Multiplier,
        Flat
    }

    internal enum ModifierEffectType
    {
        None,
        AttackStaminaCost,
        MovementPenalty,
        BlockPower,
        ParryBonus,
        BackstabBonus,
        AdrenalineGain
    }

    internal sealed class ModifierDefinition
    {
        public string Id { get; }
        public string CommandName { get; }
        public string DisplayName { get; }
        public float TestValue { get; }
        public ModifierDamageChannel DamageChannel { get; }
        public ModifierValueType ValueType { get; }
        public ModifierEffectType EffectType { get; }

        public ModifierDefinition(
            string id,
            string commandName,
            string displayName,
            float testValue,
            ModifierDamageChannel damageChannel,
            ModifierValueType valueType,
            ModifierEffectType effectType = ModifierEffectType.None)
        {
            Id = id;
            CommandName = commandName;
            DisplayName = displayName;
            TestValue = testValue;
            DamageChannel = damageChannel;
            ValueType = valueType;
            EffectType = effectType;
        }

        public void ApplyDamage(ref HitData.DamageTypes damage, float value)
        {
            switch (DamageChannel)
            {
                case ModifierDamageChannel.All:
                    ApplyValue(ref damage.m_blunt, value);
                    ApplyValue(ref damage.m_slash, value);
                    ApplyValue(ref damage.m_pierce, value);
                    ApplyValue(ref damage.m_chop, value);
                    ApplyValue(ref damage.m_pickaxe, value);
                    ApplyValue(ref damage.m_fire, value);
                    ApplyValue(ref damage.m_frost, value);
                    ApplyValue(ref damage.m_lightning, value);
                    ApplyValue(ref damage.m_poison, value);
                    ApplyValue(ref damage.m_spirit, value);
                    break;
                case ModifierDamageChannel.Slash:
                    ApplyValue(ref damage.m_slash, value);
                    break;
                case ModifierDamageChannel.Pierce:
                    ApplyValue(ref damage.m_pierce, value);
                    break;
                case ModifierDamageChannel.Blunt:
                    ApplyValue(ref damage.m_blunt, value);
                    break;
                case ModifierDamageChannel.Chop:
                    ApplyValue(ref damage.m_chop, value);
                    break;
                case ModifierDamageChannel.Pickaxe:
                    ApplyValue(ref damage.m_pickaxe, value);
                    break;
                case ModifierDamageChannel.Fire:
                    ApplyValue(ref damage.m_fire, value);
                    break;
                case ModifierDamageChannel.Frost:
                    ApplyValue(ref damage.m_frost, value);
                    break;
                case ModifierDamageChannel.Lightning:
                    ApplyValue(ref damage.m_lightning, value);
                    break;
                case ModifierDamageChannel.Poison:
                    ApplyValue(ref damage.m_poison, value);
                    break;
                case ModifierDamageChannel.Spirit:
                    ApplyValue(ref damage.m_spirit, value);
                    break;
            }
        }

        private void ApplyValue(ref float damage, float value)
        {
            damage = ApplyScalar(damage, value);
        }

        public float ApplyScalar(float currentValue, float modifierValue)
        {
            return ValueType == ModifierValueType.Percentage
                ? currentValue * (1f + modifierValue)
                : currentValue + modifierValue;
        }

        public string FormatTooltipLine(float value)
        {
            string formattedValue;
            if (ValueType == ModifierValueType.Percentage ||
                ValueType == ModifierValueType.PercentagePoints)
            {
                formattedValue = (value * 100f).ToString(
                    "+0.##;-0.##;0", CultureInfo.InvariantCulture) + "%";
            }
            else if (ValueType == ModifierValueType.Multiplier)
            {
                formattedValue = value.ToString(
                    "+0.##;-0.##;0", CultureInfo.InvariantCulture) + "x";
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
                ModifierValueType.Flat),
            new ModifierDefinition(
                "stamina_cost",
                "stamina_cost",
                "Weapon stamina cost",
                -0.1f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.AttackStaminaCost),
            new ModifierDefinition(
                "movement_penalty",
                "movement_penalty",
                "Movement penalty reduction",
                1f,
                ModifierDamageChannel.None,
                ModifierValueType.PercentagePoints,
                ModifierEffectType.MovementPenalty),
            new ModifierDefinition(
                "block_armor",
                "block_armor",
                "Block armor",
                0.2f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.BlockPower),
            new ModifierDefinition(
                "parry_bonus",
                "parry_bonus",
                "Parry bonus",
                0.5f,
                ModifierDamageChannel.None,
                ModifierValueType.Multiplier,
                ModifierEffectType.ParryBonus),
            new ModifierDefinition(
                "backstab_bonus",
                "backstab_bonus",
                "Backstab bonus",
                1f,
                ModifierDamageChannel.None,
                ModifierValueType.Multiplier,
                ModifierEffectType.BackstabBonus),
            new ModifierDefinition(
                "adrenaline_gain",
                "adrenaline_gain",
                "Adrenaline on hit",
                1f,
                ModifierDamageChannel.None,
                ModifierValueType.Flat,
                ModifierEffectType.AdrenalineGain)
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

        public static bool TryGetModifierValue(
            ItemDrop.ItemData item,
            ModifierEffectType effectType,
            out ModifierDefinition definition,
            out float value)
        {
            foreach (ModifierDefinition candidate in Definitions)
            {
                if (candidate.EffectType == effectType &&
                    ItemModifierDataStore.TryGetModifierValue(item, candidate.Id, out value))
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            value = 0f;
            return false;
        }
    }
}
