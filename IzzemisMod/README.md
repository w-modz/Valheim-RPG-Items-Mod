# Izzemi's Mod

Author: Izzemi

Adds rarity and damage modifiers to existing Valheim weapons.

## Installation (manual)

Install the package with a compatible Thunderstore mod manager or place the plugin files in `BepInEx/plugins`.

## Features

- Set rarity on equipped weapons, bows, crossbows, magic weapons, shields, and armour.
- Add all damage modifiers to bows and crossbows, with category-specific stamina and reload modifiers.
- Add shield modifiers for block armour, movement, stamina cost, and non-tower-shield parries.
- Add armour modifiers for armour rating, movement, and separate `fire_resistance`, `frost_resistance`, `lightning_resistance`, and `poison_resistance` effects.
- Magic weapons remain unsupported for modifiers for now. Incompatible modifiers are rejected by commands.
- Rarity limits remain Magic: 1, Rare: 2, Epic: 4 modifiers.
- Current test values: bow draw stamina −2, crossbow reload time −25%, shield block/parry stamina −50%, held shield movement +5%, non-tower shield parry adrenaline +5, armour +4 flat or +20% rounded up to a whole number, armour movement +3%, and 25% resistance per damage type (the strongest equipped resistance applies separately to each type).
- Existing effects remain: −10% weapon stamina cost, up to +100 percentage points movement penalty reduction (capped at 0 penalty), +20% block armour, +0.5x parry, +1x backstab, and +1 adrenaline per hit.
- View rarity and modifier details in item tooltips.
- See rarity colors on item tooltips and visuals.
- Preserve rarity and modifiers when upgrading an item through the crafting UI.

Commands accept an optional equipped-item slot, such as `set_rarity rare shield` or `modifier fire_damage weapon`. If no slot is given, a command runs only when exactly one supported item is equipped. Use `armour`, `helmet`, `chest`, `legs`, or `cape` to select armour.

## Changelog

- 0.1.0: Initial release.

## Known issues
