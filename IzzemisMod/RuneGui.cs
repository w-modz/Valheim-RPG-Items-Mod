using System.Collections.Generic;
using BepInEx.Configuration;
using HarmonyLib;
using Jotunn.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace IzzemisMod
{
    internal static class RuneGui
    {
        public const string RunePrefabPrefix = "IzzemisMod_Rune";
        public const float ApplyDurationSeconds = 3f;

        private static ConfigEntry<KeyCode> openKey;
        private static RuneGuiPanel panel;

        public static bool IsOpen => panel != null;

        public static void Bind(ConfigFile config)
        {
            openKey = config.Bind(
                "Rune GUI",
                "OpenKey",
                KeyCode.H,
                "Key that opens the rune crafting window.");
        }

        public static void HandleHotkey()
        {
            if (openKey == null || !Input.GetKeyDown(openKey.Value))
            {
                return;
            }

            if (IsOpen)
            {
                Close();
            }
            else if (CanOpen())
            {
                Open();
            }
        }

        public static void Open()
        {
            if (IsOpen || GUIManager.Instance == null || GUIManager.CustomGUIFront == null)
            {
                return;
            }

            if (InventoryGui.IsVisible())
            {
                InventoryGui.instance.Hide();
            }

            GameObject root = new GameObject("IzzemisModRuneGui", typeof(RectTransform));
            root.transform.SetParent(GUIManager.CustomGUIFront.transform, false);
            RectTransform rect = (RectTransform)root.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            panel = root.AddComponent<RuneGuiPanel>();
            panel.Build();
            GUIManager.BlockInput(true);
        }

        public static void Close()
        {
            if (panel == null)
            {
                return;
            }

            Object.Destroy(panel.gameObject);
            panel = null;
            GUIManager.BlockInput(false);
        }

        private static bool CanOpen()
        {
            if (Player.m_localPlayer == null || Player.m_localPlayer.IsDead())
            {
                return false;
            }

            return !(Chat.instance != null && Chat.instance.HasFocus()) &&
                !Console.IsVisible() &&
                !Menu.IsVisible() &&
                !TextInput.IsVisible() &&
                !StoreGui.IsVisible() &&
                !Minimap.IsOpen();
        }

        public static bool IsRune(ItemDrop.ItemData item)
        {
            return item != null &&
                item.m_dropPrefab != null &&
                item.m_dropPrefab.name.StartsWith(RunePrefabPrefix);
        }
    }

    [HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Awake))]
    internal static class RuneGuiInventoryButtonPatch
    {
        private static void Postfix(InventoryGui __instance)
        {
            if (__instance.m_player == null || GUIManager.Instance == null)
            {
                return;
            }

            Transform parent = __instance.m_player;
            if (parent.Find("IzzemisModRuneButton") != null)
            {
                return;
            }

            GameObject button = GUIManager.Instance.CreateButton(
                "Runes",
                parent,
                new Vector2(0f, 0f),
                new Vector2(0f, 0f),
                new Vector2(100f, -30f),
                180f,
                45f);
            button.name = "IzzemisModRuneButton";
            button.GetComponent<Button>().onClick.AddListener(RuneGui.Open);
        }
    }

    internal sealed class RuneGuiPanel : MonoBehaviour
    {
        private const float RowHeight = 60f;
        private const float ListWidth = 540f;
        private const float ListHeight = 465f;

        private Text itemsHeader;
        private Text infoText;
        private Text applyLabel;
        private RectTransform progressFill;
        private RectTransform itemContent;
        private RectTransform runeContent;

        private ItemDrop.ItemData selectedItem;
        private string selectedRunePrefab;
        private float applyElapsed;
        private bool applying;

        public void Build()
        {
            GUIManager gui = GUIManager.Instance;
            GameObject panelObject = gui.CreateWoodpanel(
                transform,
                new Vector2(0.5f, 0.5f),
                new Vector2(0.5f, 0.5f),
                Vector2.zero,
                1300f,
                860f,
                false);
            RectTransform panelRect = (RectTransform)panelObject.transform;

            gui.CreateText("Rune Crafting", panelRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -40f), gui.AveriaSerifBold, 48, gui.ValheimOrange, true, Color.black, 700f, 70f, false)
                .GetComponent<Text>().alignment = TextAnchor.MiddleCenter;
            itemsHeader = gui.CreateText("Item", panelRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(-290f, -95f), gui.AveriaSerifBold, 36, gui.ValheimOrange, true, Color.black, ListWidth, 46f, false)
                .GetComponent<Text>();
            gui.CreateText("Rune", panelRect, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(290f, -95f), gui.AveriaSerifBold, 36, gui.ValheimOrange, true, Color.black, ListWidth, 46f, false);

            itemContent = CreateScrollList(panelRect, new Vector2(-290f, -125f - ListHeight / 2f));
            runeContent = CreateScrollList(panelRect, new Vector2(290f, -125f - ListHeight / 2f));

            infoText = gui.CreateText(string.Empty, panelRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 215f), gui.AveriaSerif, 30, Color.white, true, Color.black, 1200f, 110f, false)
                .GetComponent<Text>();

            infoText.horizontalOverflow = HorizontalWrapMode.Wrap;
            infoText.verticalOverflow = VerticalWrapMode.Overflow;
            infoText.alignment = TextAnchor.MiddleCenter;

            CreateProgressBar(panelRect);

            GameObject apply = gui.CreateButton("Apply Rune", panelRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(-170f, 55f), 320f, 75f);
            applyLabel = apply.GetComponentInChildren<Text>();
            applyLabel.fontSize = 32;
            apply.GetComponent<Button>().onClick.AddListener(OnApplyClicked);

            GameObject close = gui.CreateButton("Close", panelRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(170f, 55f), 320f, 75f);
            close.GetComponentInChildren<Text>().fontSize = 32;
            close.GetComponent<Button>().onClick.AddListener(RuneGui.Close);

            RefreshLists();
        }

        private RectTransform CreateScrollList(RectTransform parent, Vector2 position)
        {
            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            viewport.transform.SetParent(parent, false);
            viewport.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
            RectTransform viewportRect = (RectTransform)viewport.transform;
            viewportRect.anchorMin = viewportRect.anchorMax = new Vector2(0.5f, 1f);
            viewportRect.sizeDelta = new Vector2(ListWidth, ListHeight);
            viewportRect.anchoredPosition = position;

            GameObject content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            RectTransform contentRect = (RectTransform)content.transform;
            contentRect.anchorMin = new Vector2(0f, 1f);
            contentRect.anchorMax = new Vector2(1f, 1f);
            contentRect.pivot = new Vector2(0.5f, 1f);
            contentRect.offsetMin = Vector2.zero;
            contentRect.offsetMax = Vector2.zero;

            ScrollRect scroll = viewport.AddComponent<ScrollRect>();
            scroll.viewport = viewportRect;
            scroll.content = contentRect;
            scroll.horizontal = false;
            scroll.scrollSensitivity = 30f;
            return contentRect;
        }

        private void CreateProgressBar(RectTransform parent)
        {
            GameObject background = new GameObject("ProgressBackground", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(parent, false);
            background.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);
            RectTransform backgroundRect = (RectTransform)background.transform;
            backgroundRect.anchorMin = backgroundRect.anchorMax = new Vector2(0.5f, 0f);
            backgroundRect.sizeDelta = new Vector2(640f, 22f);
            backgroundRect.anchoredPosition = new Vector2(0f, 135f);

            GameObject fill = new GameObject("ProgressFill", typeof(RectTransform), typeof(Image));
            fill.transform.SetParent(background.transform, false);
            fill.GetComponent<Image>().color = new Color(1f, 0.72f, 0.2f, 1f);
            progressFill = (RectTransform)fill.transform;
            progressFill.anchorMin = Vector2.zero;
            progressFill.anchorMax = new Vector2(0f, 1f);
            progressFill.offsetMin = Vector2.zero;
            progressFill.offsetMax = Vector2.zero;
        }

        private void RefreshLists()
        {
            Player player = Player.m_localPlayer;
            if (player == null)
            {
                RuneGui.Close();
                return;
            }

            List<ItemDrop.ItemData> items = new List<ItemDrop.ItemData>();
            Dictionary<string, List<ItemDrop.ItemData>> runes = new Dictionary<string, List<ItemDrop.ItemData>>();
            foreach (ItemDrop.ItemData item in player.GetInventory().GetAllItems())
            {
                if (RuneGui.IsRune(item))
                {
                    string key = item.m_dropPrefab.name;
                    if (!runes.TryGetValue(key, out List<ItemDrop.ItemData> stacks))
                    {
                        stacks = new List<ItemDrop.ItemData>();
                        runes[key] = stacks;
                    }

                    stacks.Add(item);
                }
                else if (ModifierCatalog.TryGetItemCategory(item, out _))
                {
                    items.Add(item);
                }
            }

            if (selectedItem != null && !items.Contains(selectedItem))
            {
                selectedItem = null;
            }

            if (selectedRunePrefab != null && !runes.ContainsKey(selectedRunePrefab))
            {
                selectedRunePrefab = null;
            }

            ClearChildren(itemContent);
            ClearChildren(runeContent);

            int row = 0;
            foreach (ItemDrop.ItemData item in items)
            {
                ItemDrop.ItemData captured = item;
                AddRow(itemContent, row++, DescribeItem(item), item == selectedItem, () => SelectItem(captured));
            }

            row = 0;
            foreach (KeyValuePair<string, List<ItemDrop.ItemData>> entry in runes)
            {
                int count = 0;
                foreach (ItemDrop.ItemData stack in entry.Value)
                {
                    count += stack.m_stack;
                }

                string prefab = entry.Key;
                string label = $"{Localization.instance.Localize(entry.Value[0].m_shared.m_name)} x{count}";
                AddRow(runeContent, row++, label, prefab == selectedRunePrefab, () => SelectRune(prefab));
            }

            itemContent.sizeDelta = new Vector2(0f, items.Count * RowHeight);
            runeContent.sizeDelta = new Vector2(0f, runes.Count * RowHeight);
            UpdateInfo();
        }

        private static string DescribeItem(ItemDrop.ItemData item)
        {
            string name = Localization.instance.Localize(item.m_shared.m_name);
            if (item.m_equipped)
            {
                name += " (equipped)";
            }

            if (ItemRarityVisuals.TryGetRarity(item, out ItemRarityDefinition rarity))
            {
                return $"<color={rarity.TooltipColor}>{name}</color> [{rarity.DisplayName}]";
            }

            return name;
        }

        private void AddRow(RectTransform content, int index, string label, bool selected, UnityEngine.Events.UnityAction onClick)
        {
            GameObject button = GUIManager.Instance.CreateButton(
                label,
                content,
                new Vector2(0.5f, 1f),
                new Vector2(0.5f, 1f),
                new Vector2(0f, -(index * RowHeight) - RowHeight / 2f),
                ListWidth - 10f,
                RowHeight - 4f);
            Text text = button.GetComponentInChildren<Text>();
            text.fontSize = 30;
            text.supportRichText = true;
            if (selected)
            {
                text.text = "> " + label;
                button.GetComponent<Image>().color = new Color(1f, 0.8f, 0.4f, 1f);
            }

            button.GetComponent<Button>().onClick.AddListener(onClick);
        }

        private static void ClearChildren(RectTransform content)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Destroy(content.GetChild(i).gameObject);
            }
        }

        private void SelectItem(ItemDrop.ItemData item)
        {
            CancelApply();
            selectedItem = item;
            RefreshLists();
        }

        private void SelectRune(string prefab)
        {
            CancelApply();
            selectedRunePrefab = prefab;
            RefreshLists();
        }

        private void UpdateInfo()
        {
            if (applying)
            {
                return;
            }

            if (selectedItem == null || selectedRunePrefab == null)
            {
                infoText.text = "Select an item and a rune.";
                return;
            }

            infoText.text = RuneDescription(selectedRunePrefab);
        }

        private static string RuneDescription(string prefab)
        {
            GameObject runePrefab = PrefabManager.Instance.GetPrefab(prefab);
            ItemDrop drop = runePrefab != null ? runePrefab.GetComponent<ItemDrop>() : null;
            return drop != null ? Localization.instance.Localize(drop.m_itemData.m_shared.m_description) : string.Empty;
        }

        private void OnApplyClicked()
        {
            if (applying)
            {
                CancelApply();
                return;
            }

            if (selectedItem == null || selectedRunePrefab == null)
            {
                infoText.text = "Select an item and a rune first.";
                return;
            }

            applying = true;
            applyElapsed = 0f;
            applyLabel.text = "Cancel";
            infoText.text = "Applying rune...";
        }

        private void CancelApply()
        {
            if (!applying)
            {
                return;
            }

            applying = false;
            applyElapsed = 0f;
            applyLabel.text = "Apply Rune";
            SetProgress(0f);
            UpdateInfo();
        }

        private void SetProgress(float fraction)
        {
            progressFill.anchorMax = new Vector2(Mathf.Clamp01(fraction), 1f);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                RuneGui.Close();
                return;
            }

            if (!applying)
            {
                return;
            }

            Player player = Player.m_localPlayer;
            if (player == null || !player.GetInventory().ContainsItem(selectedItem))
            {
                CancelApply();
                RefreshLists();
                return;
            }

            applyElapsed += Time.deltaTime;
            SetProgress(applyElapsed / RuneGui.ApplyDurationSeconds);
            if (applyElapsed >= RuneGui.ApplyDurationSeconds)
            {
                FinishApply();
            }
        }

        // Rune effects are not implemented yet, so the rune is not consumed and the item is unchanged.
        private void FinishApply()
        {
            applying = false;
            applyLabel.text = "Apply Rune";
            SetProgress(0f);
            infoText.text = "Rune effect not implemented yet.";
            Player localPlayer = Player.m_localPlayer;
            localPlayer.m_skillLevelupEffects.Create(localPlayer.transform.position, Quaternion.identity, localPlayer.transform);
            localPlayer.Message(MessageHud.MessageType.TopLeft, "Rune effect not implemented yet.");
        }

        private void OnDestroy()
        {
            if (RuneGui.IsOpen)
            {
                GUIManager.BlockInput(false);
            }
        }
    }
}
