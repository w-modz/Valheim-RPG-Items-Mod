using BepInEx;
using HarmonyLib;
using Jotunn.Entities;
using Jotunn.Managers;

namespace JotunnModStub
{
    [BepInPlugin(PluginGUID, PluginName, PluginVersion)]
    [BepInDependency(Jotunn.Main.ModGuid)]
    //[NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.Minor)]
    internal partial class JotunnModStub : BaseUnityPlugin
    {
        public const string PluginGUID = "com.izzemi.myfirstmod";
        public const string PluginName = "Izzemi's Mod";
        public const string PluginVersion = "0.1.0";
        
        // Use this class to add your own localization to the game
        // https://valheim-modding.github.io/Jotunn/tutorials/localization.html
        public static CustomLocalization ModLocalization = LocalizationManager.Instance.GetLocalization();

        private void Awake()
        {
            // Log to console and file that the mod has loaded
            Jotunn.Logger.LogInfo("Izzemi's Mod has landed");

            new Harmony(PluginGUID).PatchAll();

            CommandManager.Instance.AddConsoleCommand(new DumpItemDataCommand());
            CommandManager.Instance.AddConsoleCommand(new InitializeVariantDataCommand());

            // Add custom items
            PrefabManager.OnVanillaPrefabsAvailable += AddItemVariants;
        }
    }
}