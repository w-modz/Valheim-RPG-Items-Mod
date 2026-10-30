# Mod description

Runecarved adds rarity tiers and per-item modifiers to existing Valheim equipment. This document describes the gameplay systems in detail. For installation and project information, see the [README](README.md).

## Rarity tiers

Rarity limits are **Magic: 1 modifier**, **Rare: 2 modifiers**, and **Epic: 4 modifiers**. Rarity can be changed without losing an item's modifiers as long as the new tier's limit is not exceeded.

## Modifier pools and value ranges

Modifier values are not fixed: each one rolls within a range, in steps. Ranges are shown as weakest to strongest. Modifiers marked *fixed* do not roll. Commands default to the strongest value and can exceed the range.

| Modifier | Range / effect | Equipment |
| --- | --- | --- |
| `base_damage` | +10% to +30% all damage types. | Weapons, bows, crossbows |
| `fire_damage`, `frost_damage`, `lightning_damage`, `poison_damage`, `spirit_damage` | +5 to +15 flat damage of that type. | Weapons, bows, crossbows |
| `stamina_cost` | −5% to −15% attack stamina. | Weapons |
| `movement_penalty` | Cancels 50% to 100% of the item's negative movement penalty, in 5% steps. | Weapons |
| `block_armour` | +10% to +30% block power. | Weapons, shields, magic weapons |
| `parry_bonus` | +0.25x to +0.75x, in 0.05 steps. | Weapons, non-tower shields |
| `backstab_bonus` | +0.5x to +1.5x, in 0.5 steps. | Weapons |
| `adrenaline_gain` | +1 to +2 on hit. | Weapons |
| `projectile_speed` | +25% to +100%, in 5% steps. | Bows, crossbows |
| `ammo_save_chance` | 25% to 50% chance not to consume ammo. | Bows, crossbows |
| `extra_projectile_chance` | 25% to 50% chance to fire 2 extra projectiles, angled 10° left and right. | Bows, crossbows |
| `bow_draw_stamina` | −1 to −3 draw stamina. | Bows |
| `crossbow_reload` | −25% reload time (fixed). | Crossbows |
| `shield_movespeed` | +3% to +7% movement speed. | Shields |
| `shield_block_stamina` | −50% block/parry stamina (fixed). | Shields |
| `parry_adrenaline` | +3 to +7 on a successful parry. | Non-tower shields |
| `parry_damage` | Deals blunt damage to the attacker equal to 1x to 2x your block armour, in 0.1 steps. | Non-tower shields |
| `parry_health` | +5 to +10 health on parry. | Non-tower shields |
| `parry_stamina` | +10 to +20 stamina on parry. | Non-tower shields |
| `base_damage` | +10% to +30%. | Magic weapons |
| `block_armour` | +10% to +30%. | Magic weapons |
| `movement_penalty` | As for weapons. | Magic weapons |
| `eitr_cost` | −10% to −20% eitr per attack. | Magic weapons |
| `eitr_regen` | +15% to +30% eitr regeneration while held. | Magic weapons |
| `shield_on_use` | 10% to 20% chance to gain the Staff of Protection shield when used; only the user is shielded. | Magic weapons |
| `aoe_radius` | +25% to +50% area radius. | Magic weapons with an area effect |
| `magic_extra_projectile` | 10% to 25% chance of an extra projectile. | Elemental magic weapons that fire projectiles |
| `summon_roots` | 10% to 20% chance to also summon friendly roots from the Staff of the Wild. | Elemental magic weapons |
| `blood_health_cost` | −10% to −30% health cost. | Blood magic weapons |
| `blood_health_to_stamina` | 10% to 30% of the health cost is paid with stamina instead. | Blood magic weapons |
| `low_health_effect` | +25% to +50% damage and shield strength while below 50% health. | Blood magic weapons |
| `shield_ally_heal` | +20 to +40 health to nearby players and tamed creatures or summons when the shield is cast; never the user. | Staff of Protection |
| `summon_max_count` | +1 to +2 maximum summons. | Summoning weapons (such as Dead Raiser) |
| `extra_summon_chance` | 15% to 30% chance to summon one extra minion. | Summoning weapons (such as Dead Raiser) |
| `extra_summon_stars` | 25% to 50% chance to add 1–2 stars to summons, up to 3 stars. | Summoning weapons (such as Dead Raiser) |
| `armour_flat` | +2 to +6 armour. | Armour |
| `armour_percent` | +10% to +30% armour; result rounded up to a whole number. | Armour |
| `movement_penalty` | Cancels 50% to 100% of the negative movement penalty. | Armour with a negative movement penalty |
| `armour_movespeed` | +2% to +4% movement speed. | Armour |
| `fire_resistance`, `frost_resistance`, `lightning_resistance`, `poison_resistance` | +25% resistance to the respective damage type (fixed). | Armour |
| `health_regen` | +5% to +15%. | Armour |
| `stamina_regen` | +5% to +15%. | Armour |
| `max_health` | +2 to +5. | Armour |
| `adrenaline_on_hit` | +1 to +4 when damaged. | Armour |
| `dodge_stamina` | −5% to −15% dodge roll stamina. | Armour |
| `thorns` | Deals 20% to 40% of your total body armour as pierce damage to melee attackers. | Armour |
| `skill_<name>` | +10 to +20 to one skill: axes, blood_magic, bows, clubs, crossbows, elemental_magic, fists, knives, polearms, spears, or swords. | Armour |

Resistance modifiers are separate by damage type. For each type, the strongest resistance on equipped armour applies; resistance does not stack across pieces. Movement-penalty reduction cannot turn an item's penalty into a movement bonus. Stored values are never clamped to the ranges above, so values beyond them (from commands or special runes) are kept when loading.

## Console commands

Open the in-game console and use the commands below. `set_rarity`, `modifier`, and `remove_modifier` only work when cheats are enabled (the vanilla `devcommands`).

| Command | Usage | Effect |
| --- | --- | --- |
| `set_rarity` | `set_rarity <magic\|rare\|epic\|0> [slot]` | Set rarity. Use `0` to clear rarity and all modifiers from the item. |
| `modifier` | `modifier <modifier_name> [value] [slot]` | Add the named modifier at its strongest value, or at `value` if given. `value` uses tooltip units (`25` means 25% for percentage modifiers) and may exceed the normal range. The item must already have a rarity. |
| `remove_modifier` | `remove_modifier <modifier_name> [slot]` | Remove that modifier from the item. |
| `dump_item_data` | `dump_item_data` | Print local-player inventory item data for troubleshooting. |

Examples:

```text
set_rarity rare bow
modifier bow_draw_stamina bow
modifier fire_damage 20 weapon
modifier crossbow_reload crossbow
modifier fire_resistance chest
remove_modifier fire_resistance chest
set_rarity 0 chest
```

The optional slot targets the equipped item. Supported slots are `weapon`, `bow`, `crossbow`, `magic`, `shield`, `armour`, `helmet`, `chest`, `legs`, and `cape`. Use `armour` only when exactly one armour piece is equipped. If the slot is omitted, the command runs only when exactly one supported item is equipped; otherwise, specify a slot. Modifier compatibility is checked for the selected item, and parry modifiers are unavailable on tower shields.

## Runes

Runes are used from the rune window, opened with the **Runes** button under the inventory (the hotkey is temporarily disabled). Pick an item and a rune, then apply: a 3-second progress bar runs and can be cancelled by clicking again. The rune is consumed unless the vanilla `nocost` cheat is on. Runes have no crafting recipe; they drop from enemies (see [Rune drops](#rune-drops)) or can be spawned with the console, e.g. `spawn IzzemisMod_RuneAsgard`. Ammo (arrows and bolts) cannot receive rarity, modifiers, or runes.

| Rune | Effect |
| --- | --- |
| Asgard | Removes one random modifier and adds one new random modifier. |
| Midgard | Upgrades a normal item to Magic and adds one random modifier. |
| Vanaheim | Upgrades a Magic item to Rare and adds one random modifier. |
| Alfheim | Upgrades a Rare item to Epic and adds one random modifier. |
| Jotunheim | Adds a modifier to an Epic item, up to 4. |
| Muspelheim | Destroys a Magic or higher item and grants the matching rune (Magic: Midgard, Rare: Vanaheim, Epic: Alfheim). |
| Niflheim | Rerolls the values of all modifiers on an item. |
| Hel | Epic items only. One of four equal outcomes: destroys the item, removes a modifier, adds a bonus 5th modifier, or raises all non-fixed modifier values by 20% (may exceed normal maximums; fixed values such as resistances are unchanged). The item is then "Touched by Hel": only Nidavellir and Muspelheim can be used on it. It can still be upgraded at crafting stations, keeping its rarity and modifiers. |
| Nidavellir | Destroys an item with modifiers and grants a Rune of Valheim holding one of its modifiers at its exact value. |
| Valheim | Only obtained from Nidavellir or the three late-game bosses (stack size 1). Applies its modifier to an item of the same type as the sacrificed item that has a rarity, a free modifier slot, and does not already have that modifier. |

## Rune drops

Runes drop from a fixed list of enemies (any enemy not listed drops no runes). Enemies are grouped by biome. Each group has a base chance per rune, in percent per kill, and every enemy has a multiplier applied to its group's chances. Rune of Valheim never drops from normal enemies. The tables are in `RuneDrops.cs` (`BaseChances` and `Enemies`).

- **Unlock biomes:** Asgard and Midgard drop from the Black Forest, Vanaheim and Muspelheim from the Swamp, Alfheim and Niflheim from the Mountain, Jotunheim and Nidavellir from the Plains, and Hel from the Mistlands. Each keeps dropping in later biomes. Nothing drops in the Meadows.
- **Stars:** the chance is multiplied once per star (1 star = x3, 2 stars = x9 by default).
- **Bosses** ignore the tables and drop guaranteed runes, one per player within 200 m: Eikthyr drops Midgard, The Elder Vanaheim, Bonemass Alfheim, and Moder Jotunheim. Yagluth, The Queen, and Fader each drop a Rune of Valheim per player, rolled separately, with a random item type and a random modifier at its maximum value.

Configuration (`BepInEx/config/com.izzemi.izzemismod.cfg`, section `Rune Drops`):

| Option | Default | Effect |
| --- | --- | --- |
| `DropRateMultiplier` | 1 | Global multiplier for all rune drops, including bosses. 0 disables rune drops. |
| `StarMultiplier` | 3 | Chance multiplier per enemy star. 1 disables the star bonus. |
