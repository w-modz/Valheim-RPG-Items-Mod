using BepInEx;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;

namespace IzzemisMod
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal partial class IzzemisMod : BaseUnityPlugin
    {
        public const string PluginGUID = "com.izzemi.izzemismod";
        public const string PluginName = "Izzemi's Mod";
        public const string PluginVersion = "0.1.0";
        
        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization ModLocalization = LocalizationManager.Instance.GetLocalization();

        private void Awake()
        {
            new Harmony(PluginGUID).PatchAll();

            ModCommands.Register();
        }
    }
}