using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace Runecarved
{
    internal sealed class RuneDropModifier
    {
        private const int DefaultIndex = 0;

        public static readonly RuneDropModifier Enemy = new RuneDropModifier(
            "runecarved_runedroprate", "RunecarvedRuneDropRate", "Rune Drop Rate",
            "Changes rune drop chances from normal enemies. Boss drops are unchanged.",
            new[] { "Few (1x)", "Normal (3x)", "High (5x)", "Very High (10x)", "Abundant (20x)" },
            new[] { "100", "300", "500", "1000", "2000" });

        public static readonly RuneDropModifier Chest = new RuneDropModifier(
            "runecarved_runechestrate", "RunecarvedRuneChestRate", "Chest Rune Rate",
            "Changes the number of runes in newly generated loot chests. The chance remains 25%.",
            new[] { "1x", "2x", "3x", "4x" },
            new[] { "100", "200", "300", "400" });

        private readonly string globalKey;
        private readonly string controlName;
        private readonly string label;
        private readonly string tooltip;
        private readonly string[] names;
        private readonly string[] percentages;

        private RuneDropModifier(string globalKey, string controlName, string label, string tooltip, string[] names, string[] percentages)
        {
            this.globalKey = globalKey;
            this.controlName = controlName;
            this.label = label;
            this.tooltip = tooltip;
            this.names = names;
            this.percentages = percentages;
        }

        private static readonly WorldModifierOption[] Options =
        {
            WorldModifierOption.Default,
            WorldModifierOption.MuchMore,
            WorldModifierOption.VeryHard,
            WorldModifierOption.Hardcore,
            WorldModifierOption.Most
        };

        public float GetMultiplier()
        {
            if (ZoneSystem.instance == null ||
                !ZoneSystem.instance.GetGlobalKey(globalKey, out string value) ||
                !float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out float percentage) ||
                float.IsNaN(percentage) ||
                float.IsInfinity(percentage))
            {
                return 1f;
            }

            return float.Parse(percentages[FindNearestSettingIndex(percentage)], CultureInfo.InvariantCulture) / 100f;
        }

        public void AddToWorldSettings(ServerOptionsGUI settings)
        {
            if (settings.m_modifiersRoot == null)
            {
                Jotunn.Logger.LogError($"Could not add the {label} world modifier: the world settings panel has no modifier root.");
                return;
            }

            foreach (KeySlider existing in settings.m_modifiersRoot.GetComponentsInChildren<KeySlider>(true))
            {
                if (existing.gameObject.name == controlName)
                {
                    return;
                }
            }

            KeySlider template = settings.m_modifiersRoot.GetComponentInChildren<KeySlider>(true);
            if (template == null)
            {
                Jotunn.Logger.LogError($"Could not add the {label} world modifier: no vanilla modifier slider was found.");
                return;
            }

            Transform templateRow = template.transform.parent;
            if (templateRow == null || templateRow.parent == null)
            {
                Jotunn.Logger.LogError($"Could not add the {label} world modifier: the vanilla slider row has no layout parent.");
                return;
            }

            GameObject holder = new GameObject(controlName + "Holder");
            holder.SetActive(false);
            GameObject control = UnityEngine.Object.Instantiate(templateRow.gameObject, holder.transform, false);
            control.name = controlName;

            KeySlider slider = control.GetComponentInChildren<KeySlider>(true);
            if (slider == null)
            {
                Jotunn.Logger.LogError($"Could not add the {label} world modifier: cloned slider row has no KeySlider.");
                UnityEngine.Object.Destroy(holder);
                return;
            }

            slider.gameObject.name = controlName;
            slider.m_modifier = WorldModifiers.Resources;
            slider.m_defaultIndex = DefaultIndex;
            slider.m_manualSet = false;
            slider.m_toolTip = tooltip;
            slider.m_settings = CreateSettings();

            foreach (TMP_Text label in control.GetComponentsInChildren<TMP_Text>(true))
            {
                if (label != slider.m_nameLabel && label != slider.m_toolTipLabel)
                {
                    label.text = this.label;
                }
            }

            control.transform.SetParent(templateRow.parent, false);
            RectTransform templateRect = templateRow as RectTransform;
            RectTransform controlRect = control.transform as RectTransform;
            RectTransform rowsRect = templateRow.parent as RectTransform;
            RectTransform panelRect = templateRow.parent.parent as RectTransform;
            if (templateRect == null || controlRect == null || rowsRect == null || panelRect == null || panelRect.rect.height <= 0f)
            {
                Jotunn.Logger.LogError($"Could not position the {label} world modifier: the slider rows and panel need valid RectTransforms.");
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
            float previousHeight = panelRect.rect.height;
            panelRect.sizeDelta += new Vector2(0f, rowSpacing);
            float rowAnchor = Mathf.Lerp(rowsRect.anchorMin.y, rowsRect.anchorMax.y, rowsRect.pivot.y);
            rowsRect.anchoredPosition += new Vector2(0f, rowSpacing * (1f - rowAnchor));
            panelRect.localScale *= previousHeight / (previousHeight + rowSpacing);
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

        public void ReadWorldSettings(ServerOptionsGUI settings, World world)
        {
            KeySlider slider = FindSlider(settings);
            if (slider == null)
            {
                return;
            }

            FieldInfo presetField = AccessTools.Field(typeof(ServerOptionsGUI), "m_preset");
            if (presetField == null)
            {
                Jotunn.Logger.LogError($"Could not read the {label} world modifier without the vanilla preset field.");
                return;
            }

            object preset = presetField.GetValue(settings);
            int index = FindWorldSettingIndex(world);
            slider.SetValue(Options[index]);
            slider.m_manualSet = false;
            presetField.SetValue(settings, preset);
        }

        public void SaveWorldSettings(ServerOptionsGUI settings, World world)
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

        private int FindWorldSettingIndex(World world)
        {
            if (world == null || world.m_startingGlobalKeys == null)
            {
                return DefaultIndex;
            }

            string keyPrefix = globalKey + " ";
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

        private int FindNearestSettingIndex(float percentage)
        {
            int nearestIndex = DefaultIndex;
            float nearestDistance = float.MaxValue;
            for (int i = 0; i < percentages.Length; i++)
            {
                float distance = Mathf.Abs(percentage - float.Parse(percentages[i], CultureInfo.InvariantCulture));
                if (distance < nearestDistance)
                {
                    nearestIndex = i;
                    nearestDistance = distance;
                }
            }

            return nearestIndex;
        }

        private KeySlider FindSlider(ServerOptionsGUI settings)
        {
            if (settings == null || settings.m_modifiersRoot == null)
            {
                return null;
            }

            foreach (KeySlider slider in settings.m_modifiersRoot.GetComponentsInChildren<KeySlider>(true))
            {
                if (slider.gameObject.name == controlName)
                {
                    return slider;
                }
            }

            return null;
        }

        private List<KeySlider.SliderSetting> CreateSettings()
        {
            List<KeySlider.SliderSetting> settings = new List<KeySlider.SliderSetting>();
            for (int i = 0; i < names.Length; i++)
            {
                settings.Add(new KeySlider.SliderSetting
                {
                    m_name = names[i],
                    m_toolTip = label + " multiplier: " + percentages[i] + "%.",
                    m_modifierValue = Options[i],
                    m_keys = new List<string> { globalKey + " " + percentages[i] }
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
            RuneDropModifier.Chest.AddToWorldSettings(__instance);
            RuneDropModifier.Enemy.AddToWorldSettings(__instance);
        }

        private static void Postfix(ServerOptionsGUI __instance)
        {
            FieldInfo modifiersField = AccessTools.Field(typeof(ServerOptionsGUI), "m_modifiers");
            if (modifiersField == null)
            {
                Jotunn.Logger.LogError("Could not isolate rune world modifiers from vanilla presets.");
                return;
            }

            KeyUI[] current = (KeyUI[])modifiersField.GetValue(null);
            List<KeyUI> vanillaModifiers = new List<KeyUI>(current.Length);
            foreach (KeyUI modifier in current)
            {
                if (modifier.gameObject.name != "RunecarvedRuneDropRate" &&
                    modifier.gameObject.name != "RunecarvedRuneChestRate")
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
            RuneDropModifier.Enemy.ReadWorldSettings(__instance, world);
            RuneDropModifier.Chest.ReadWorldSettings(__instance, world);
        }
    }

    [HarmonyPatch(typeof(ServerOptionsGUI), nameof(ServerOptionsGUI.SetKeys))]
    internal static class RuneDropModifierSaveWorldSettingsPatch
    {
        private static void Postfix(ServerOptionsGUI __instance, World world)
        {
            RuneDropModifier.Enemy.SaveWorldSettings(__instance, world);
            RuneDropModifier.Chest.SaveWorldSettings(__instance, world);
        }
    }
}
