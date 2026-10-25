# Izzemi's Mod

Author: Izzemi

Izzemi's Mod adds rarity tiers and per-item modifiers to existing Valheim equipment. It does not add replacement equipment models or other custom item assets.

## Installation

Install through a compatible Valheim mod manager, or place the plugin DLL in `BepInEx/plugins`. The mod requires BepInEx and Jötunn; mod managers normally install these dependencies automatically.

## Current features

- Assign Magic, Rare, or Epic rarity to equipped supported equipment.
- Add and remove modifiers on individual item instances. Rarity and modifier data are saved in the item's custom data.
- Show rarity and modifier details on item tooltips. Rarity also affects inventory icon outlines and glow visuals for equipped items and world drops.
- Preserve rarity and modifiers when an item is upgraded through the crafting UI.
- Apply modifiers to supported weapons, bows, crossbows, shields, and armour. Magic weapons can be assigned a rarity, but currently have no compatible modifiers.

Rarity limits are **Magic: 1 modifier**, **Rare: 2 modifiers**, and **Epic: 4 modifiers**. Rarity can be changed without losing the item's modifiers as long as the new tier's limit is not exceeded.

## Modifier pools and current test values

The values below are fixed test values in the current implementation.

| Equipment | Available modifiers |
| --- | --- |
| Weapons | `base_damage` (+20% to all damage types), `fire_damage`, `frost_damage`, `lightning_damage`, `poison_damage`, or `spirit_damage` (+10 flat damage of that type); `stamina_cost` (−10% attack stamina); `movement_penalty` (up to +100 percentage points, capped at cancelling the item's negative penalty); `block_armour` (+20% block power); `parry_bonus` (+0.5x); `backstab_bonus` (+1x); `adrenaline_gain` (+1 on hit). |
| Bows | All damage modifiers above, plus `bow_draw_stamina` (−2 draw stamina). |
| Crossbows | All damage modifiers above, plus `crossbow_reload` (−25% reload time). |
| Shields | `block_armour` (+20% block power); `movement_penalty` (cancels up to the shield's negative movement penalty); `shield_movespeed` (+5% movement speed); `shield_block_stamina` (−50% block/parry stamina). Non-tower shields also allow `parry_bonus` (+0.5x) and `parry_adrenaline` (+5 on a successful parry). |
| Armour | `armour_flat` (+4 armour); `armour_percent` (+20% armour, with the result rounded up to a whole number); `movement_penalty` (only when the piece has a negative movement penalty, capped at cancelling it); `armour_movespeed` (+3% movement speed); and one of `fire_resistance`, `frost_resistance`, `lightning_resistance`, or `poison_resistance` (+25% resistance to that type). |

Resistance modifiers are separate by damage type. For each type, the strongest resistance on equipped armour applies; resistance does not stack across pieces. Movement-penalty reduction cannot turn an item's penalty into a movement bonus.

## Console commands

Open the in-game console and use:

| Command | Usage | Effect |
| --- | --- | --- |
| `set_rarity` | `set_rarity <magic\|rare\|epic\|0> [slot]` | Set rarity. Use `0` to clear rarity and all modifiers from the item. |
| `modifier` | `modifier <modifier_name> [slot]` | Add the named modifier at its current test value. The item must already have a rarity. |
| `remove_modifier` | `remove_modifier <modifier_name> [slot]` | Remove that modifier from the item. |
| `dump_item_data` | `dump_item_data` | Print local-player inventory item data for troubleshooting. |

Examples:

```text
set_rarity rare bow
modifier bow_draw_stamina bow
modifier crossbow_reload crossbow
modifier fire_resistance chest
remove_modifier fire_resistance chest
set_rarity 0 chest
```

The optional slot targets the equipped item. Supported slots are `weapon`, `bow`, `crossbow`, `magic`, `shield`, `armour`, `helmet`, `chest`, `legs`, and `cape`. Use `armour` only when exactly one armour piece is equipped. If the slot is omitted, the command runs only when exactly one supported item is equipped; otherwise, specify a slot. Modifier compatibility is checked for the selected item, and parry modifiers are unavailable on tower shields.

## Not implemented yet

- Runes to upgrade rarity and add modifiers to weapons.
- Custom GUI for weapon crafting with runes.
- Automatic rune drops from enemies.
- A modifier pool for magic weapons.
- Legendary item variants rarely dropped from enemies, each with unique effects.
