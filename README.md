# Izzemi's Mod

Author: Izzemi

A Valheim mod that adds rarity and damage modifiers to existing weapons. Mod identity and package metadata are in `IzzemisMod.sln` and the `IzzemisMod` project directory.

## Building

Open `IzzemisMod.sln` in Visual Studio and build Debug or Release. The build requires `BEPINEX_PATH` and `VALHEIM_INSTALL` to point to the installed BepInEx profile and Valheim directory.

- Debug builds are copied into the BepInEx plugins directory (or `MOD_DEPLOYPATH`).
- Release builds package the plugin, manifest, and package README into a zip under the build output directory.

The included Unity project is only needed if custom assets are added later. Current rarity visuals are created in code and do not require new assets.

For Jötunn setup and game-update guidance, see the [Jötunn documentation](https://valheim-modding.github.io/Jotunn/guides/overview.html).
