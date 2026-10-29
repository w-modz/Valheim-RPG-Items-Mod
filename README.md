# Izzemi's Mod

Author: Izzemi

Built on the [Jotunn mod stub](https://github.com/Valheim-Modding/JotunnModStub) by the Valheim Modding Community.

Izzemi’s Mod (working title) adds rarity tiers and per-item modifiers to existing Valheim equipment, creating a more varied and unpredictable gear progression.

## Installation

> **Note:** there is no release and no mod manager (Thunderstore) page for this mod yet, so it has to be built from source.

Requirements: Windows, a Valheim install with BepInEx and [Jötunn](https://valheim-modding.github.io/Jotunn/) installed (for example through a mod manager profile), and the .NET SDK (or Visual Studio).

1. Clone the repository.
2. Create `Environment.props` in the repository root (it is git-ignored) with the paths for your machine:

   ```xml
   <Project>
     <PropertyGroup>
       <VALHEIM_INSTALL>C:\Program Files (x86)\Steam\steamapps\common\Valheim</VALHEIM_INSTALL>
       <BEPINEX_PATH>C:\path\to\BepInEx</BEPINEX_PATH>
       <MOD_DEPLOYPATH>C:\path\to\BepInEx\plugins</MOD_DEPLOYPATH>
     </PropertyGroup>
   </Project>
   ```

3. Build `IzzemisMod.sln` (for example `dotnet build IzzemisMod.sln`, or Build in Visual Studio). The build copies the plugin DLL into `MOD_DEPLOYPATH`.
4. Start Valheim.

## Current features

- Magic, Rare, and Epic rarity tiers with per-item modifiers for supported weapons, bows, crossbows, shields, armour, and magic weapons.
- Custom rune items with unique sprites and models, plus an in-game rune interface for upgrading, rerolling, transferring, and sacrificing equipment; runes can drop from enemies and bosses.
- Modifier effects cover combat, defense, movement, resource use, ranged attacks, magic, summons, and skills.
- Persistent item data, modifier and rarity tooltips, rarity visuals, and preservation of item upgrades through crafting.

For modifier pools, console commands, rune effects, and drop details, see the [mod description](MOD_DESCRIPTION.md).

## Not implemented yet

- Additional random effects for enemies with stars, that increase their strength and dropped rewards.
- Legendary item variants rarely dropped from enemies, each with unique effects.
