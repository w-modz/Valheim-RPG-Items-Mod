using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace Runecarved
{
    internal static class RuneDropModifier
    {
        private const string GlobalKey = "runecarved_runedroprate";
        private const string ControlName = "RunecarvedRuneDropRate";
        private const int DefaultIndex = 0;

        private static readonly string[] Names =
        {
            "Few (1x)",
            "Normal (3x)",
            "High (5x)",
            "Very High (10x)",
            "Abundant (20x)"
        };

        private static readonly string[] Percentages = { "100", "300", "500", "1000", "2000" };
        private static readonly WorldModifierOption[] Options =
        {
            WorldModifierOption.Default,
            WorldModifierOption.MuchMore,
            WorldModifierOption.VeryHard,
            WorldModifierOption.Hardcore,
            WorldModifierOption.Most
        };

        public static float GetMultiplier()
        {
            if (ZoneSystem.instance == null ||
                !ZoneSystem.instance.GetGlobalKey(GlobalKey, out string value) ||
                !float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float percentage) ||
                float.IsNaN(percentage) ||
                float.IsInfinity(percentage))
            {
                return 1f;
            }

            return float.Parse(Percentages[FindNearestSettingIndex(percentage)], CultureInfo.InvariantCulture) / 100f;
        }

        public static void AddToWorldSettings(ServerOptionsGUI settings)
        {
            if (settings.m_modifiersRoot == null)
            {
                Jotunn.Logger.LogError("Could not add the Rune Drop Rate world modifier: the world settings panel has no modifier root.");
                return;
            }

            foreach (KeySlider existing in settings.m_modifiersRoot.GetComponentsInChildren<KeySlider>(true))
            {
                if (existing.gameObject.name == ControlName)
                {
                    return;
                }
            }

            KeySlider template = settings.m_modifiersRoot.GetComponentInChildren<KeySlider>(true);
            if (template == null)
            {
                Jotunn.Logger.LogError("Could not add the Rune Drop Rate world modifier: no vanilla modifier slider was found.");
                return;
            }

            Transform templateRow = template.transform.parent;
            if (templateRow == null || templateRow.parent == null)
            {
                Jotunn.Logger.LogError("Could not add the Rune Drop Rate world modifier: the vanilla slider row has no layout parent.");
                return;
            }

            GameObject holder = new GameObject(ControlName + "Holder");
            holder.SetActive(false);
            GameObject control = UnityEngine.Object.Instantiate(templateRow.gameObject, holder.transform, false);
            control.name = ControlName;

            KeySlider slider = control.GetComponentInChildren<KeySlider>(true);
            if (slider == null)
            {
                Jotunn.Logger.LogError("Could not add the Rune Drop Rate world modifier: cloned slider row has no KeySlider.");
                UnityEngine.Object.Destroy(holder);
                return;
            }

            slider.gameObject.name = ControlName;
            slider.m_modifier = WorldModifiers.Resources;
            slider.m_defaultIndex = DefaultIndex;
            slider.m_manualSet = false;
            slider.m_toolTip = "Changes rune drop chances from normal enemies. Boss drops are unchanged.";
            slider.m_settings = CreateSettings();

            foreach (TMP_Text label in control.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label != slider.m_nameLabel && label != slider.m_toolTipLabel)
                {
                    label.text = "Rune Drop Rate";
                }
            }

            control.transform.SetParent(templateRow.parent, false);
            RectTransform templateRect = templateRow as RectTransform;
            RectTransform controlRect = control.transform as RectTransform;
            RectTransform rowsRect = templateRow.parent as RectTransform;
            if (templateRect == null || controlRect == null || rowsRect == null)
            {
                Jotunn.Logger.LogError("Could not position the Rune Drop Rate world modifier: the slider rows must use RectTransforms.");
                UnityEngine.Object.Destroy(control);
                UnityEngine.Object.Destroy(holder);
                return;
            }

            int templateIndex = templateRow.GetSiblingIndex();
            float rowSpacing = GetRowSpacing(settings, templateRow, templateIndex);
            foreach (Transform sibling in GetFollowingRows(templateRow.parent, templateIndex))
            {
                RectTransform siblingRect = sibling as RectTransform;
                if (siblingRect != null)
                {
                    siblingRect.anchoredPosition -= new Vector2(0f, rowSpacing);
                }
            }

            controlRect.anchoredPosition = templateRect.anchoredPosition - new Vector2(0f, rowSpacing);
            control.transform.SetSiblingIndex(templateIndex + 1);
            rowsRect.sizeDelta += new Vector2(0f, rowSpacing);
            control.SetActive(true);
            UnityEngine.Object.Destroy(holder);
        }

        private static List<Transform> GetFollowingRows(Transform parent, int siblingIndex)
        {
            List<Transform> followingRows = new List<Transform>();
            for (int i = siblingIndex + 1; i < parent.childCount; i++)
            {
                followingRows.Add(parent.GetChild(i));
            }

            return followingRows;
        }

        private static float GetRowSpacing(ServerOptionsGUI settings, Transform templateRow, int templateIndex)
        {
            float templateY = ((RectTransform)templateRow).anchoredPosition.y;
            foreach (KeySlider candidate in settings.m_modifiersRoot.GetComponentsInChildren<KeySlider>(true))
            {
                Transform candidateRow = candidate.transform.parent;
                if (candidateRow != null &&
                    candidateRow.parent == templateRow.parent &&
                    candidateRow.GetSiblingIndex() > templateIndex)
                {
                    float spacing = Mathf.Abs(((RectTransform)candidateRow).anchoredPosition.y - templateY);
                    if (spacing > 0f)
                    {
                        return spacing;
                    }
                }
            }

            return ((RectTransform)templateRow).rect.height + 10f;
        }

        public static void ReadWorldSettings(ServerOptionsGUI settings, World world)
        {
            KeySlider slider = FindSlider(settings);
            if (slider == null)
            {
                return;
            }

            FieldInfo presetField = AccessTools.Field(typeof(ServerOptionsGUI), "m_preset");
            if (presetField == null)
            {
                Jotunn.Logger.LogError("Could not read the Rune Drop Rate world modifier without the vanilla preset field.");
                return;
            }

            object preset = presetField.GetValue(settings);
            int index = FindWorldSettingIndex(world);
            slider.SetValue(Options[index]);
            slider.m_manualSet = false;
            presetField.SetValue(settings, preset);
        }

        public static void SaveWorldSettings(ServerOptionsGUI settings, World world)
        {
            if (world == null)
            {
                return;
            }

            KeySlider slider = FindSlider(settings);
            if (slider != null)
            {
                slider.SetKeys(world);
            }
        }

        private static int FindWorldSettingIndex(World world)
        {
            if (world == null || world.m_startingGlobalKeys == null)
            {
                return DefaultIndex;
            }

            string keyPrefix = GlobalKey + " ";
            foreach (string key in world.m_startingGlobalKeys)
            {
                if (key.StartsWith(keyPrefix, StringComparison.OrdinalIgnoreCase) &&
                    float.TryParse(key.Substring(keyPrefix.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out float percentage))
                {
                    return FindNearestSettingIndex(percentage);
                }
            }

            return DefaultIndex;
        }

        private static int FindNearestSettingIndex(float percentage)
        {
            int nearestIndex = DefaultIndex;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < Percentages.Length; i++)
            {
                float distance = Mathf.Abs(percentage - float.Parse(Percentages[i], CultureInfo.InvariantCulture));
                if (distance < nearestDistance)
                {
                    nearestIndex = i;
                    nearestDistance = distance;
                }
            }

            return nearestIndex;
        }

        private static KeySlider FindSlider(ServerOptionsGUI settings)
        {
            if (settings == null || settings.m_modifiersRoot == null)
            {
                return null;
            }

            foreach (KeySlider slider in settings.m_modifiersRoot.GetComponentsInChildren<KeySlider>(true))
            {
                if (slider.gameObject.name == ControlName)
                {
                    return slider;
                }
            }

            return null;
        }

        private static List<KeySlider.SliderSetting> CreateSettings()
        {
            List<KeySlider.SliderSetting> settings = new List<KeySlider.SliderSetting>();
            for (int i = 0; i < Names.Length; i++)
            {
                settings.Add(new KeySlider.SliderSetting
                {
                    m_name = Names[i],
                    m_toolTip = "Rune drop chance multiplier: " + Percentages[i] + "%.",
                    m_modifierValue = Options[i],
                    m_keys = new List<string> { GlobalKey + " " + Percentages[i] }
                });
            }

            return settings;
        }
    }

    [HarmonyPatch(typeof(ServerOptionsGUI), nameof(ServerOptionsGUI.Awake))]
    internal static class RuneDropModifierWorldSettingsPatch
    {
        private static void Prefix(ServerOptionsGUI __instance)
        {
            RuneDropModifier.AddToWorldSettings(__instance);
        }

        private static void Postfix(ServerOptionsGUI __instance)
        {
            FieldInfo modifiersField = AccessTools.Field(typeof(ServerOptionsGUI), "m_modifiers");
            if (modifiersField == null)
            {
                Jotunn.Logger.LogError("Could not isolate the Rune Drop Rate world modifier from vanilla presets.");
                return;
            }

            KeyUI[] current = (KeyUI[])modifiersField.GetValue(null);
            List<KeyUI> vanillaModifiers = new List<KeyUI>(current.Length);
            foreach (KeyUI modifier in current)
            {
                if (modifier.gameObject.name != "RunecarvedRuneDropRate")
                {
                    vanillaModifiers.Add(modifier);
                }
            }

            modifiersField.SetValue(null, vanillaModifiers.ToArray());
        }
    }

    [HarmonyPatch(typeof(ServerOptionsGUI), nameof(ServerOptionsGUI.ReadKeys))]
    internal static class RuneDropModifierReadWorldSettingsPatch
    {
        private static void Postfix(ServerOptionsGUI __instance, World world)
        {
            RuneDropModifier.ReadWorldSettings(__instance, world);
        }
    }

    [HarmonyPatch(typeof(ServerOptionsGUI), nameof(ServerOptionsGUI.SetKeys))]
    internal static class RuneDropModifierSaveWorldSettingsPatch
    {
        private static void Postfix(ServerOptionsGUI __instance, World world)
        {
            RuneDropModifier.SaveWorldSettings(__instance, world);
        }
    }
}
