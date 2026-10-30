using System;
using System.Collections.Generic;
using System.Globalization;

namespace Runecarved
{
    [System.Flags]
    internal enum ModifierItemCategory
    {
        None = 0,
        Weapon = 1,
        Bow = 2,
        Crossbow = 4,
        MagicWeapon = 8,
        Shield = 16,
        Armour = 32
    }

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
        AdrenalineGain,
        BowDrawStaminaCost,
        ReloadTime,
        BlockStaminaCost,
        MovementSpeedBonus,
        ArmourFlat,
        ArmourPercentage,
        FireResistance,
        FrostResistance,
        LightningResistance,
        PoisonResistance,
        ParryAdrenaline,
        ProjectileSpeed,
        AmmoSaveChance,
        ExtraProjectileChance,
        ParryDamage,
        ParryHealth,
        ParryStamina,
        HealthRegen,
        StaminaRegen,
        MaxHealth,
        AdrenalineOnHit,
        DodgeStaminaCost,
        Thorns,
        SkillLevel,
        AttackEitrCost,
        ShieldOnUse,
        AoeRadius,
        EitrRegen,
        MagicExtraProjectile,
        SummonRoots,
        BloodHealthCost,
        BloodHealthToStamina,
        LowHealthEffect,
        ShieldAllyHeal,
        SummonMaxCount,
        ExtraSummonChance,
        ExtraSummonStars
    }

    internal sealed class ModifierDefinition
    {
        public string Id { get; }
        public string CommandName { get; }
        public string DisplayName { get; }
        // Centre of the roll range; rolls span 50% to 150% of this unless overridden.
        public float BaseValue { get; }
        // Weakest and strongest normal rolls. Commands and special runes may go beyond them.
        public float WeakestValue { get; }
        public float StrongestValue { get; }
        // Rolls are multiples of Step from WeakestValue; 0 means the value is fixed.
        public float Step { get; }
        public ModifierDamageChannel DamageChannel { get; }
        public ModifierValueType ValueType { get; }
        public ModifierEffectType EffectType { get; }
        public ModifierItemCategory AllowedItemCategories { get; }
        // Only used by SkillLevel modifiers.
        public Skills.SkillType Skill { get; }

        public ModifierDefinition(
            string id,
            string commandName,
            string displayName,
            float testValue,
            ModifierDamageChannel damageChannel,
            ModifierValueType valueType,
            ModifierEffectType effectType = ModifierEffectType.None,
            ModifierItemCategory allowedItemCategories = ModifierItemCategory.Weapon,
            Skills.SkillType skill = Skills.SkillType.None)
        {
            Id = id;
            CommandName = commandName;
            DisplayName = displayName;
            BaseValue = testValue;
            if (ModifierCatalog.RollRanges.TryGetValue(id, out float[] range))
            {
                WeakestValue = range[0];
                StrongestValue = range[1];
                Step = range[2];
            }
            else
            {
                WeakestValue = testValue * 0.5f;
                StrongestValue = testValue * 1.5f;
                Step = 0f;
            }

            DamageChannel = damageChannel;
            ValueType = valueType;
            EffectType = effectType;
            AllowedItemCategories = allowedItemCategories;
            Skill = skill;
        }

        // Converts a value typed as shown in the tooltip (e.g. 25 for 25%) to the stored value.
        public float FromDisplayValue(float displayValue)
        {
            return ValueType == ModifierValueType.Percentage ||
                ValueType == ModifierValueType.PercentagePoints
                ? displayValue / 100f
                : displayValue;
        }

        public float Roll()
        {
            if (Step <= 0f)
            {
                return StrongestValue;
            }

            int steps = (int)Math.Round(Math.Abs(StrongestValue - WeakestValue) / Step);
            int chosen = UnityEngine.Random.Range(0, steps + 1);
            float direction = StrongestValue >= WeakestValue ? 1f : -1f;
            return (float)Math.Round(WeakestValue + direction * chosen * Step, 4);
        }

        public bool IsAllowedOn(ItemDrop.ItemData item)
        {
            if (!ModifierCatalog.TryGetItemCategory(item, out ModifierItemCategory category) ||
                (AllowedItemCategories & category) == 0)
            {
                return false;
            }

            if (EffectType == ModifierEffectType.MovementPenalty &&
                item.m_shared.m_movementModifier >= 0f)
            {
                return false;
            }

            if (EffectType == ModifierEffectType.AoeRadius && !MagicWeaponInfo.HasAoe(item))
            {
                return false;
            }

            if ((EffectType == ModifierEffectType.MagicExtraProjectile ||
                 EffectType == ModifierEffectType.SummonRoots) &&
                item.m_shared.m_skillType != Skills.SkillType.ElementalMagic)
            {
                return false;
            }

            if ((EffectType == ModifierEffectType.BloodHealthCost ||
                 EffectType == ModifierEffectType.BloodHealthToStamina ||
                 EffectType == ModifierEffectType.LowHealthEffect) &&
                item.m_shared.m_skillType != Skills.SkillType.BloodMagic)
            {
                return false;
            }

            if (EffectType == ModifierEffectType.ShieldAllyHeal &&
                !StaffShieldSource.IsStaffOfProtection(item))
            {
                return false;
            }

            if ((EffectType == ModifierEffectType.SummonMaxCount ||
                 EffectType == ModifierEffectType.ExtraSummonChance ||
                 EffectType == ModifierEffectType.ExtraSummonStars) &&
                !SummonWeaponInfo.HasSummonAbility(item))
            {
                return false;
            }

            if (EffectType == ModifierEffectType.MagicExtraProjectile &&
                !MagicWeaponInfo.FiresProjectile(item))
            {
                return false;
            }

            return !IsParryOnlyEffect(EffectType) ||
                category != ModifierItemCategory.Shield ||
                !ModifierCatalog.IsTowerShield(item);
        }

        // Effects that need a timed (parry) block, which tower shields lack.
        public static bool IsParryOnlyEffect(ModifierEffectType effectType)
        {
            return effectType == ModifierEffectType.ParryBonus ||
                effectType == ModifierEffectType.ParryAdrenaline ||
                effectType == ModifierEffectType.ParryDamage ||
                effectType == ModifierEffectType.ParryHealth ||
                effectType == ModifierEffectType.ParryStamina;
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

        public bool IsOverLimit(float value)
        {
            const float epsilon = 0.0001f;
            return StrongestValue >= WeakestValue
                ? value > StrongestValue + epsilon
                : value < StrongestValue - epsilon;
        }

        public string FormatTooltipLine(float value, bool wholeLinePurple = false)
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
                if (EffectType == ModifierEffectType.ParryDamage)
                {
                    formattedValue += " block armor";
                }
            }
            else
            {
                formattedValue = value.ToString("+0.##;-0.##;0", CultureInfo.InvariantCulture);
            }

            const string purple = "#9B30FF";
            if (wholeLinePurple)
            {
                return $"<color={purple}>{DisplayName}: {formattedValue}</color>";
            }

            string valueColor = IsOverLimit(value) ? purple : "green";
            return $"<color=#ADD8E6>{DisplayName}:</color> " +
                $"<color={valueColor}>{formattedValue}</color>";
        }
    }

    internal static class ModifierCatalog
    {
        // id -> { weakest, strongest, step }. Step 0 means fixed at the strongest value.
        internal static readonly Dictionary<string, float[]> RollRanges = new Dictionary<string, float[]>
        {
            { "base_damage", new[] { 0.10f, 0.30f, 0.01f } },
            { "fire_damage", new[] { 5f, 15f, 1f } },
            { "frost_damage", new[] { 5f, 15f, 1f } },
            { "lightning_damage", new[] { 5f, 15f, 1f } },
            { "poison_damage", new[] { 5f, 15f, 1f } },
            { "spirit_damage", new[] { 5f, 15f, 1f } },
            { "stamina_cost", new[] { -0.05f, -0.15f, 0.01f } },
            { "eitr_cost", new[] { -0.10f, -0.20f, 0.01f } },
            { "shield_on_use", new[] { 0.10f, 0.20f, 0.01f } },
            { "aoe_radius", new[] { 0.25f, 0.50f, 0.01f } },
            { "eitr_regen", new[] { 0.15f, 0.30f, 0.01f } },
            { "magic_extra_projectile", new[] { 0.10f, 0.25f, 0.01f } },
            { "summon_roots", new[] { 0.10f, 0.20f, 0.01f } },
            { "blood_health_cost", new[] { 0.10f, 0.30f, 0.01f } },
            { "blood_health_to_stamina", new[] { 0.10f, 0.30f, 0.01f } },
            { "low_health_effect", new[] { 0.25f, 0.50f, 0.01f } },
            { "shield_ally_heal", new[] { 20f, 40f, 1f } },
            { "summon_max_count", new[] { 1f, 2f, 1f } },
            { "extra_summon_chance", new[] { 0.15f, 0.30f, 0.01f } },
            { "extra_summon_stars", new[] { 0.25f, 0.50f, 0.01f } },
            { "bow_draw_stamina", new[] { -1f, -3f, 1f } },
            { "crossbow_reload", new[] { -0.25f, -0.25f, 0f } },
            { "projectile_speed", new[] { 0.25f, 1.00f, 0.05f } },
            { "ammo_save_chance", new[] { 0.25f, 0.50f, 0.01f } },
            { "extra_projectile_chance", new[] { 0.25f, 0.50f, 0.01f } },
            { "movement_penalty", new[] { 0.5f, 1f, 0.05f } },
            { "block_armour", new[] { 0.10f, 0.30f, 0.01f } },
            { "parry_bonus", new[] { 0.25f, 0.75f, 0.05f } },
            { "backstab_bonus", new[] { 0.5f, 1.5f, 0.5f } },
            { "adrenaline_gain", new[] { 1f, 2f, 1f } },
            { "shield_block_stamina", new[] { -0.5f, -0.5f, 0f } },
            { "shield_movespeed", new[] { 0.03f, 0.07f, 0.01f } },
            { "armour_flat", new[] { 2f, 6f, 1f } },
            { "armour_percent", new[] { 0.10f, 0.30f, 0.01f } },
            { "armour_movespeed", new[] { 0.02f, 0.04f, 0.01f } },
            { "fire_resistance", new[] { 0.25f, 0.25f, 0f } },
            { "frost_resistance", new[] { 0.25f, 0.25f, 0f } },
            { "lightning_resistance", new[] { 0.25f, 0.25f, 0f } },
            { "poison_resistance", new[] { 0.25f, 0.25f, 0f } },
            { "parry_adrenaline", new[] { 3f, 7f, 1f } },
            { "parry_damage", new[] { 1f, 2f, 0.1f } },
            { "parry_health", new[] { 5f, 10f, 1f } },
            { "parry_stamina", new[] { 10f, 20f, 1f } },
            { "health_regen", new[] { 0.05f, 0.15f, 0.01f } },
            { "stamina_regen", new[] { 0.05f, 0.15f, 0.01f } },
            { "max_health", new[] { 2f, 5f, 1f } },
            { "adrenaline_on_hit", new[] { 1f, 4f, 1f } },
            { "dodge_stamina", new[] { -0.05f, -0.15f, 0.01f } },
            { "thorns", new[] { 0.20f, 0.40f, 0.01f } },
            { "skill_axes", new[] { 10f, 20f, 1f } },
            { "skill_blood_magic", new[] { 10f, 20f, 1f } },
            { "skill_bows", new[] { 10f, 20f, 1f } },
            { "skill_clubs", new[] { 10f, 20f, 1f } },
            { "skill_crossbows", new[] { 10f, 20f, 1f } },
            { "skill_elemental_magic", new[] { 10f, 20f, 1f } },
            { "skill_fists", new[] { 10f, 20f, 1f } },
            { "skill_knives", new[] { 10f, 20f, 1f } },
            { "skill_polearms", new[] { 10f, 20f, 1f } },
            { "skill_spears", new[] { 10f, 20f, 1f } },
            { "skill_swords", new[] { 10f, 20f, 1f } }
        };

        // Relative chance of a modifier being picked by any rune that adds a random modifier. Missing ids default to 1.
        internal static readonly Dictionary<string, float> Weights = new Dictionary<string, float>
        {
            // Weapons, bows and crossbows (damage)
            { "base_damage", 5f },
            { "fire_damage", 1f },
            { "frost_damage", 1f },
            { "lightning_damage", 1f },
            { "poison_damage", 1f },
            { "spirit_damage", 1f },

            // Weapons (other)
            { "stamina_cost", 1f },
            { "eitr_cost", 1f },
            { "shield_on_use", 1f },
            { "aoe_radius", 1f },
            { "eitr_regen", 1f },
            { "magic_extra_projectile", 1f },
            { "summon_roots", 1f },
            { "blood_health_cost", 1f },
            { "blood_health_to_stamina", 1f },
            { "low_health_effect", 1f },
            { "shield_ally_heal", 1f },
            { "summon_max_count", 1f },
            { "extra_summon_chance", 1f },
            { "extra_summon_stars", 1f },
            { "backstab_bonus", 2f },
            { "adrenaline_gain", 1f },

            // Bows and crossbows
            { "bow_draw_stamina", 2f },
            { "crossbow_reload", 1f },
            { "projectile_speed", 2f },
            { "ammo_save_chance", 2f },
            { "extra_projectile_chance", 1f },

            // Weapons, shields and armour (shared)
            { "movement_penalty", 2f },

            // Weapons and shields (shared)
            { "block_armour", 1f },
            { "parry_bonus", 2f },

            // Shields
            { "shield_block_stamina", 4f },
            { "shield_movespeed", 2f },
            { "parry_adrenaline", 1f },
            { "parry_damage", 1f },
            { "parry_health", 1f },
            { "parry_stamina", 1f },

            // Armour
            { "armour_flat", 1f },
            { "armour_percent", 3f },
            { "armour_movespeed", 2f },
            { "fire_resistance", 1f },
            { "frost_resistance", 1f },
            { "lightning_resistance", 1f },
            { "poison_resistance", 1f },
            { "health_regen", 1f },
            { "stamina_regen", 1f },
            { "max_health", 1f },
            { "adrenaline_on_hit", 1f },
            { "dodge_stamina", 1f },
            { "thorns", 2f },

            // Armour skill bonuses (one entry per skill, so keep each low)
            { "skill_axes", 0.2f },
            { "skill_blood_magic", 0.2f },
            { "skill_bows", 0.2f },
            { "skill_clubs", 0.2f },
            { "skill_crossbows", 0.2f },
            { "skill_elemental_magic", 0.2f },
            { "skill_fists", 0.2f },
            { "skill_knives", 0.2f },
            { "skill_polearms", 0.2f },
            { "skill_spears", 0.2f },
            { "skill_swords", 0.2f }
        };

        public static float GetWeight(string id)
        {
            return Weights.TryGetValue(id, out float weight) ? Math.Max(0f, weight) : 1f;
        }

        private static readonly ModifierDefinition[] Definitions =
        {
            new ModifierDefinition(
                "base_damage",
                "base_damage",
                "Base damage",
                0.2f,
                ModifierDamageChannel.All,
                ModifierValueType.Percentage,
                allowedItemCategories: ModifierItemCategory.Weapon |
                    ModifierItemCategory.MagicWeapon |
                    ModifierItemCategory.Bow |
                    ModifierItemCategory.Crossbow),
            new ModifierDefinition(
                "fire_damage",
                "fire_damage",
                "Fire damage",
                10f,
                ModifierDamageChannel.Fire,
                ModifierValueType.Flat,
                allowedItemCategories: ModifierItemCategory.Weapon |
                    ModifierItemCategory.Bow |
                    ModifierItemCategory.Crossbow),
            new ModifierDefinition(
                "frost_damage",
                "frost_damage",
                "Frost damage",
                10f,
                ModifierDamageChannel.Frost,
                ModifierValueType.Flat,
                allowedItemCategories: ModifierItemCategory.Weapon |
                    ModifierItemCategory.Bow |
                    ModifierItemCategory.Crossbow),
            new ModifierDefinition(
                "lightning_damage",
                "lightning_damage",
                "Lightning damage",
                10f,
                ModifierDamageChannel.Lightning,
                ModifierValueType.Flat,
                allowedItemCategories: ModifierItemCategory.Weapon |
                    ModifierItemCategory.Bow |
                    ModifierItemCategory.Crossbow),
            new ModifierDefinition(
                "poison_damage",
                "poison_damage",
                "Poison damage",
                10f,
                ModifierDamageChannel.Poison,
                ModifierValueType.Flat,
                allowedItemCategories: ModifierItemCategory.Weapon |
                    ModifierItemCategory.Bow |
                    ModifierItemCategory.Crossbow),
            new ModifierDefinition(
                "spirit_damage",
                "spirit_damage",
                "Spirit damage",
                10f,
                ModifierDamageChannel.Spirit,
                ModifierValueType.Flat,
                allowedItemCategories: ModifierItemCategory.Weapon |
                    ModifierItemCategory.Bow |
                    ModifierItemCategory.Crossbow),
            new ModifierDefinition(
                "stamina_cost",
                "stamina_cost",
                "Weapon stamina cost",
                -0.1f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.AttackStaminaCost),
            new ModifierDefinition(
                "eitr_cost",
                "eitr_cost",
                "Weapon eitr cost",
                -0.15f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.AttackEitrCost,
                ModifierItemCategory.MagicWeapon),
            new ModifierDefinition(
                "shield_on_use",
                "shield_on_use",
                "Chance to gain a shield on use",
                0.15f,
                ModifierDamageChannel.None,
                ModifierValueType.PercentagePoints,
                ModifierEffectType.ShieldOnUse,
                ModifierItemCategory.MagicWeapon),
            new ModifierDefinition(
                "aoe_radius",
                "aoe_radius",
                "Area of effect radius",
                0.35f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.AoeRadius,
                ModifierItemCategory.MagicWeapon),
            new ModifierDefinition(
                "eitr_regen",
                "eitr_regen",
                "Eitr regeneration while held",
                0.2f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.EitrRegen,
                ModifierItemCategory.MagicWeapon),
            new ModifierDefinition(
                "magic_extra_projectile",
                "magic_extra_projectile",
                "Chance for additional projectiles",
                0.15f,
                ModifierDamageChannel.None,
                ModifierValueType.PercentagePoints,
                ModifierEffectType.MagicExtraProjectile,
                ModifierItemCategory.MagicWeapon),
            new ModifierDefinition(
                "summon_roots",
                "summon_roots",
                "Chance to summon roots on use",
                0.15f,
                ModifierDamageChannel.None,
                ModifierValueType.PercentagePoints,
                ModifierEffectType.SummonRoots,
                ModifierItemCategory.MagicWeapon),
            new ModifierDefinition(
                "blood_health_cost",
                "blood_health_cost",
                "Blood magic health cost",
                0.2f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.BloodHealthCost,
                ModifierItemCategory.MagicWeapon),
            new ModifierDefinition(
                "blood_health_to_stamina",
                "blood_health_to_stamina",
                "Convert health cost to stamina cost",
                0.2f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.BloodHealthToStamina,
                ModifierItemCategory.MagicWeapon),
            new ModifierDefinition(
                "low_health_effect",
                "low_health_effect",
                "Effect while below 50% health",
                0.35f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.LowHealthEffect,
                ModifierItemCategory.MagicWeapon),
            new ModifierDefinition(
                "shield_ally_heal",
                "shield_ally_heal",
                "Heal nearby allies when shield is granted",
                30f,
                ModifierDamageChannel.None,
                ModifierValueType.Flat,
                ModifierEffectType.ShieldAllyHeal,
                ModifierItemCategory.MagicWeapon),
            new ModifierDefinition(
                "summon_max_count",
                "summon_max_count",
                "Maximum summons",
                1f,
                ModifierDamageChannel.None,
                ModifierValueType.Flat,
                ModifierEffectType.SummonMaxCount,
                ModifierItemCategory.Weapon | ModifierItemCategory.MagicWeapon),
            new ModifierDefinition(
                "extra_summon_chance",
                "extra_summon_chance",
                "Chance to spawn an extra summon",
                0.225f,
                ModifierDamageChannel.None,
                ModifierValueType.PercentagePoints,
                ModifierEffectType.ExtraSummonChance,
                ModifierItemCategory.Weapon | ModifierItemCategory.MagicWeapon),
            new ModifierDefinition(
                "extra_summon_stars",
                "extra_summon_stars",
                "Chance for extra summon stars (max 2)",
                0.375f,
                ModifierDamageChannel.None,
                ModifierValueType.PercentagePoints,
                ModifierEffectType.ExtraSummonStars,
                ModifierItemCategory.Weapon | ModifierItemCategory.MagicWeapon),
            new ModifierDefinition(
                "bow_draw_stamina",
                "bow_draw_stamina",
                "Bow draw stamina cost",
                -2f,
                ModifierDamageChannel.None,
                ModifierValueType.Flat,
                ModifierEffectType.BowDrawStaminaCost,
                ModifierItemCategory.Bow),
            new ModifierDefinition(
                "crossbow_reload",
                "crossbow_reload",
                "Crossbow reload time",
                -0.25f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.ReloadTime,
                ModifierItemCategory.Crossbow),
            new ModifierDefinition(
                "projectile_speed",
                "projectile_speed",
                "Projectile speed",
                0.6f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.ProjectileSpeed,
                ModifierItemCategory.Bow | ModifierItemCategory.Crossbow),
            new ModifierDefinition(
                "ammo_save_chance",
                "ammo_save_chance",
                "Chance to not consume ammo",
                0.35f,
                ModifierDamageChannel.None,
                ModifierValueType.PercentagePoints,
                ModifierEffectType.AmmoSaveChance,
                ModifierItemCategory.Bow | ModifierItemCategory.Crossbow),
            new ModifierDefinition(
                "extra_projectile_chance",
                "extra_projectile_chance",
                "Chance to fire 2 extra projectiles",
                0.4f,
                ModifierDamageChannel.None,
                ModifierValueType.PercentagePoints,
                ModifierEffectType.ExtraProjectileChance,
                ModifierItemCategory.Bow | ModifierItemCategory.Crossbow),
            new ModifierDefinition(
                "movement_penalty",
                "movement_penalty",
                "Movement penalty reduction",
                1f,
                ModifierDamageChannel.None,
                ModifierValueType.PercentagePoints,
                ModifierEffectType.MovementPenalty,
                ModifierItemCategory.Weapon |
                    ModifierItemCategory.MagicWeapon |
                    ModifierItemCategory.Shield |
                    ModifierItemCategory.Armour),
            new ModifierDefinition(
                "block_armour",
                "block_armour",
                "Block armour",
                0.2f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.BlockPower,
                ModifierItemCategory.Weapon |
                    ModifierItemCategory.MagicWeapon |
                    ModifierItemCategory.Shield),
            new ModifierDefinition(
                "parry_bonus",
                "parry_bonus",
                "Parry bonus",
                0.5f,
                ModifierDamageChannel.None,
                ModifierValueType.Multiplier,
                ModifierEffectType.ParryBonus,
                ModifierItemCategory.Weapon |
                    ModifierItemCategory.Shield),
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
                ModifierEffectType.AdrenalineGain),
            new ModifierDefinition(
                "shield_block_stamina",
                "shield_block_stamina",
                "Block and parry stamina cost",
                -0.5f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.BlockStaminaCost,
                ModifierItemCategory.Shield),
            new ModifierDefinition(
                "shield_movespeed",
                "shield_movespeed",
                "Movement speed",
                0.05f,
                ModifierDamageChannel.None,
                ModifierValueType.PercentagePoints,
                ModifierEffectType.MovementSpeedBonus,
                ModifierItemCategory.Shield),
            new ModifierDefinition(
                "armour_flat",
                "armour_flat",
                "Armour",
                4f,
                ModifierDamageChannel.None,
                ModifierValueType.Flat,
                ModifierEffectType.ArmourFlat,
                ModifierItemCategory.Armour),
            new ModifierDefinition(
                "armour_percent",
                "armour_percent",
                "Armour",
                0.2f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.ArmourPercentage,
                ModifierItemCategory.Armour),
            new ModifierDefinition(
                "armour_movespeed",
                "armour_movespeed",
                "Movement speed",
                0.03f,
                ModifierDamageChannel.None,
                ModifierValueType.PercentagePoints,
                ModifierEffectType.MovementSpeedBonus,
                ModifierItemCategory.Armour),
            new ModifierDefinition(
                "fire_resistance",
                "fire_resistance",
                "Fire resistance",
                0.25f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.FireResistance,
                ModifierItemCategory.Armour),
            new ModifierDefinition(
                "frost_resistance",
                "frost_resistance",
                "Frost resistance",
                0.25f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.FrostResistance,
                ModifierItemCategory.Armour),
            new ModifierDefinition(
                "lightning_resistance",
                "lightning_resistance",
                "Lightning resistance",
                0.25f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.LightningResistance,
                ModifierItemCategory.Armour),
            new ModifierDefinition(
                "poison_resistance",
                "poison_resistance",
                "Poison resistance",
                0.25f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.PoisonResistance,
                ModifierItemCategory.Armour),
            new ModifierDefinition(
                "parry_adrenaline",
                "parry_adrenaline",
                "Adrenaline on parry",
                5f,
                ModifierDamageChannel.None,
                ModifierValueType.Flat,
                ModifierEffectType.ParryAdrenaline,
                ModifierItemCategory.Shield),
            new ModifierDefinition(
                "parry_damage",
                "parry_damage",
                "Blunt damage on parry",
                1.5f,
                ModifierDamageChannel.None,
                ModifierValueType.Multiplier,
                ModifierEffectType.ParryDamage,
                ModifierItemCategory.Shield),
            new ModifierDefinition(
                "parry_health",
                "parry_health",
                "Health on parry",
                7.5f,
                ModifierDamageChannel.None,
                ModifierValueType.Flat,
                ModifierEffectType.ParryHealth,
                ModifierItemCategory.Shield),
            new ModifierDefinition(
                "parry_stamina",
                "parry_stamina",
                "Stamina on parry",
                15f,
                ModifierDamageChannel.None,
                ModifierValueType.Flat,
                ModifierEffectType.ParryStamina,
                ModifierItemCategory.Shield),
            new ModifierDefinition(
                "health_regen",
                "health_regen",
                "Health regeneration",
                0.1f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.HealthRegen,
                ModifierItemCategory.Armour),
            new ModifierDefinition(
                "stamina_regen",
                "stamina_regen",
                "Stamina regeneration",
                0.1f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.StaminaRegen,
                ModifierItemCategory.Armour),
            new ModifierDefinition(
                "max_health",
                "max_health",
                "Max health",
                3.5f,
                ModifierDamageChannel.None,
                ModifierValueType.Flat,
                ModifierEffectType.MaxHealth,
                ModifierItemCategory.Armour),
            new ModifierDefinition(
                "adrenaline_on_hit",
                "adrenaline_on_hit",
                "Adrenaline when damaged",
                2.5f,
                ModifierDamageChannel.None,
                ModifierValueType.Flat,
                ModifierEffectType.AdrenalineOnHit,
                ModifierItemCategory.Armour),
            new ModifierDefinition(
                "dodge_stamina",
                "dodge_stamina",
                "Dodge roll stamina cost",
                -0.1f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.DodgeStaminaCost,
                ModifierItemCategory.Armour),
            new ModifierDefinition(
                "thorns",
                "thorns",
                "Thorns damage (pierce, % of total armour)",
                0.3f,
                ModifierDamageChannel.None,
                ModifierValueType.Percentage,
                ModifierEffectType.Thorns,
                ModifierItemCategory.Armour),
            SkillDefinition("skill_axes", "Axes", Skills.SkillType.Axes),
            SkillDefinition("skill_blood_magic", "Blood magic", Skills.SkillType.BloodMagic),
            SkillDefinition("skill_bows", "Bows", Skills.SkillType.Bows),
            SkillDefinition("skill_clubs", "Clubs", Skills.SkillType.Clubs),
            SkillDefinition("skill_crossbows", "Crossbows", Skills.SkillType.Crossbows),
            SkillDefinition("skill_elemental_magic", "Elemental magic", Skills.SkillType.ElementalMagic),
            SkillDefinition("skill_fists", "Fists", Skills.SkillType.Unarmed),
            SkillDefinition("skill_knives", "Knives", Skills.SkillType.Knives),
            SkillDefinition("skill_polearms", "Polearms", Skills.SkillType.Polearms),
            SkillDefinition("skill_spears", "Spears", Skills.SkillType.Spears),
            SkillDefinition("skill_swords", "Swords", Skills.SkillType.Swords)
        };

        private static ModifierDefinition SkillDefinition(
            string id,
            string name,
            Skills.SkillType skill)
        {
            return new ModifierDefinition(
                id,
                id,
                name + " skill",
                15f,
                ModifierDamageChannel.None,
                ModifierValueType.Flat,
                ModifierEffectType.SkillLevel,
                ModifierItemCategory.Armour,
                skill);
        }

        public static IReadOnlyList<ModifierDefinition> All => Definitions;

        public static bool TryGetItemCategory(
            ItemDrop.ItemData item,
            out ModifierItemCategory category)
        {
            category = ModifierItemCategory.None;
            if (item == null || item.m_shared == null)
            {
                return false;
            }

            ItemDrop.ItemData.ItemType itemType = item.m_shared.m_itemType;
            if (itemType == ItemDrop.ItemData.ItemType.Ammo ||
                itemType == ItemDrop.ItemData.ItemType.AmmoNonEquipable)
            {
                return false;
            }

            if (itemType == ItemDrop.ItemData.ItemType.Shield)
            {
                category = ModifierItemCategory.Shield;
                return true;
            }

            if (itemType == ItemDrop.ItemData.ItemType.Helmet ||
                itemType == ItemDrop.ItemData.ItemType.Chest ||
                itemType == ItemDrop.ItemData.ItemType.Legs ||
                itemType == ItemDrop.ItemData.ItemType.Shoulder)
            {
                category = ModifierItemCategory.Armour;
                return true;
            }

            Skills.SkillType skillType = item.m_shared.m_skillType;
            if (skillType == Skills.SkillType.Crossbows)
            {
                category = ModifierItemCategory.Crossbow;
                return true;
            }

            if (itemType == ItemDrop.ItemData.ItemType.Bow)
            {
                category = ModifierItemCategory.Bow;
                return true;
            }

            if (skillType == Skills.SkillType.ElementalMagic ||
                skillType == Skills.SkillType.BloodMagic)
            {
                category = ModifierItemCategory.MagicWeapon;
                return true;
            }

            switch (itemType)
            {
                case ItemDrop.ItemData.ItemType.OneHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeapon:
                case ItemDrop.ItemData.ItemType.TwoHandedWeaponLeft:
                case ItemDrop.ItemData.ItemType.Attach_Atgeir:
                    category = ModifierItemCategory.Weapon;
                    return true;
                default:
                    return false;
            }
        }

        public static bool IsTowerShield(ItemDrop.ItemData item)
        {
            if (item == null ||
                item.m_shared == null ||
                item.m_shared.m_itemType != ItemDrop.ItemData.ItemType.Shield)
            {
                return false;
            }

            string prefabName = item.m_dropPrefab == null ? null : item.m_dropPrefab.name;
            string sharedName = item.m_shared.m_name;
            return item.m_shared.m_timedBlockBonus <= 1f ||
                (prefabName != null &&
                    prefabName.IndexOf("tower", StringComparison.OrdinalIgnoreCase) >= 0) ||
                (sharedName != null &&
                 sharedName.IndexOf("tower", StringComparison.OrdinalIgnoreCase) >= 0);
        }

        public static string GetCategoryDisplayName(ModifierItemCategory category)
        {
            switch (category)
            {
                case ModifierItemCategory.Weapon:
                    return "weapon";
                case ModifierItemCategory.Bow:
                    return "bow";
                case ModifierItemCategory.Crossbow:
                    return "crossbow";
                case ModifierItemCategory.MagicWeapon:
                    return "magic weapon";
                case ModifierItemCategory.Shield:
                    return "shield";
                case ModifierItemCategory.Armour:
                    return "armour";
                default:
                    return "item";
            }
        }

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
                if (candidate.EffectType != effectType ||
                    !candidate.IsAllowedOn(item))
                {
                    continue;
                }

                if (ItemModifierDataStore.TryGetModifierValue(item, candidate.Id, out value))
                {
                    definition = candidate;
                    return true;
                }

                if (IsResistanceEffect(effectType) &&
                    ItemModifierDataStore.TryGetModifierValue(
                        item,
                        "elemental_resistance",
                        out value))
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            value = 0f;
            return false;
        }

        private static bool IsResistanceEffect(ModifierEffectType effectType)
        {
            return effectType == ModifierEffectType.FireResistance ||
                effectType == ModifierEffectType.FrostResistance ||
                effectType == ModifierEffectType.LightningResistance ||
                effectType == ModifierEffectType.PoisonResistance;
        }
    }
}
