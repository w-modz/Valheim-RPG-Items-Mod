# Runecarved

Author: Izzemi

Built on the [Jotunn mod stub](https://github.com/Valheim-Modding/JotunnModStub) by the Valheim Modding Community.

Runecarved adds rarity tiers and per-item modifiers to existing Valheim equipment, creating a more varied and unpredictable gear progression.

## Installation
Either:
> [Install from thunderstore.](https://thunderstore.io/c/valheim/p/Izzemi_dev/Runecarved/)
Requirements: BepInExPack Valheim and Jötunn (both are installed automatically if you use a mod manager). 
- Install r2modman or the Thunderstore Mod Manager.
- Select Valheim, create a profile, and search for this mod.
- Click Install with dependencies, then Start modded.
> [Download one of the releases from the repo page.](https://github.com/w-modz/Valheim-RPG-Items-Mod/releases)
- Install BepInEx for Valheim by extracting BepInExPack Valheim into your Valheim folder (usually C:\Program Files (x86)\Steam\steamapps\common\Valheim). Run the game once, then close it.
- Install Jötunn by placing Jotunn.dll in Valheim\BepInEx\plugins\.
- Put the .dll file in Valheim\BepInEx\plugins\. A subfolder such as plugins\ModName\ also works.
- Launch Valheim through Steam.

## Building

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

3. Build `Runecarved.sln` (for example `dotnet build Runecarved.sln`, or Build in Visual Studio). The build copies the plugin DLL into `MOD_DEPLOYPATH`.
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
