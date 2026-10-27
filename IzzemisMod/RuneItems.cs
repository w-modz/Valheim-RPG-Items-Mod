using System;
using System.IO;
using System.Reflection;
using Jotunn.Configs;
using Jotunn.Entities;
using Jotunn.Managers;
using UnityEngine;

namespace IzzemisMod
{
    internal static class RuneItems
    {
        private sealed class RuneDefinition
        {
            public RuneDefinition(string prefabName, string displayName, string description, Color color)
            {
                PrefabName = prefabName;
                DisplayName = displayName;
                Description = description;
                Color = color;
            }

            public string PrefabName { get; }
            public string DisplayName { get; }
            public string Description { get; }
            public Color Color { get; }
        }

        private const string PlaceholderBasePrefab = "Ruby";
        private const string DestructiveColor = "#E03030";
        private const string UpgradeColor = "#2E8B57";

        private static string Red(string text) => $"<color={DestructiveColor}>{text}</color>";

        private static string Up(string text) => $"<color={UpgradeColor}>{text}</color>";

        private static string Rarity(string id)
        {
            ItemRarityCatalog.TryGet(id, out ItemRarityDefinition rarity);
            return $"<color={rarity.TooltipColor}>{rarity.DisplayName}</color>";
        }

        private static readonly RuneDefinition[] Runes =
        {
            new RuneDefinition("IzzemisMod_RuneAsgard", "Rune of Asgard",
                Red("Removes") + " one random modifier from an item and adds one new random modifier.",
                new Color(1f, 0.85f, 0.3f)),
            new RuneDefinition("IzzemisMod_RuneMidgard", "Rune of Midgard",
                Up("Upgrades") + " a normal item to " + Rarity("magic") + " and adds one random modifier.",
                new Color(0.6f, 0.45f, 0.25f)),
            new RuneDefinition("IzzemisMod_RuneVanaheim", "Rune of Vanaheim",
                Up("Upgrades") + " a " + Rarity("magic") + " item to " + Rarity("rare") + " and adds one random modifier.",
                new Color(0.3f, 0.8f, 0.35f)),
            new RuneDefinition("IzzemisMod_RuneAlfheim", "Rune of Alfheim",
                Up("Upgrades") + " a " + Rarity("rare") + " item to " + Rarity("epic") + " and adds one random modifier.",
                new Color(0.95f, 0.95f, 0.8f)),
            new RuneDefinition("IzzemisMod_RuneJotunheim", "Rune of Jotunheim",
                "Adds an additional modifier to an " + Rarity("epic") + " item, up to 4.",
                new Color(0.4f, 0.7f, 1f)),
            new RuneDefinition("IzzemisMod_RuneMuspelheim", "Rune of Muspelheim",
                Red("Destroys") + " an item of " + Rarity("magic") + " rarity or higher and grants the corresponding rune.",
                new Color(1f, 0.35f, 0.1f)),
            new RuneDefinition("IzzemisMod_RuneNiflheim", "Rune of Niflheim",
                "Randomizes the values of all modifiers on an item.",
                new Color(0.6f, 0.9f, 1f)),
            new RuneDefinition("IzzemisMod_RuneHel", "Rune of Hel",
                "Randomly modifies or " + Red("destroys") + " an " + Rarity("epic") + " item. Items modified cannot be further altered by other runes.",
                new Color(0.3f, 0.1f, 0.4f)),
            new RuneDefinition("IzzemisMod_RuneNidavellir", "Rune of Nidavellir",
                Red("Destroys") + " an item and grants a Rune of Valheim holding one random modifier of the sacrificed item.",
                new Color(0.55f, 0.55f, 0.6f)),
            new RuneDefinition("IzzemisMod_RuneValheim", "Rune of Valheim",
                "Applies its stored modifier to an item of the same type as the sacrificed item. Only obtainable via the Rune of Nidavellir.",
                new Color(1f, 1f, 1f)),
        };

        private const string BundleResource = "Bundles.runebundle";
        private const string BundlePrefabPath = "assets/rune/rune.prefab";

        public static void Register()
        {
            PrefabManager.OnVanillaPrefabsAvailable += AddRunes;
        }

        private static GameObject LoadModelPrefab()
        {
            try
            {
                using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(BundleResource))
                {
                    if (stream == null)
                    {
                        Jotunn.Logger.LogWarning("Rune bundle resource not found; using the placeholder model.");
                        return null;
                    }

                    AssetBundle bundle = AssetBundle.LoadFromStream(stream);
                    if (bundle == null)
                    {
                        Jotunn.Logger.LogWarning("Rune bundle could not be loaded; using the placeholder model.");
                        return null;
                    }

                    GameObject model = bundle.LoadAsset<GameObject>(BundlePrefabPath);
                    if (model == null)
                    {
                        Jotunn.Logger.LogWarning($"'{BundlePrefabPath}' not found in the rune bundle. Assets: {string.Join(", ", bundle.GetAllAssetNames())}");
                    }

                    return model;
                }
            }
            catch (Exception ex)
            {
                Jotunn.Logger.LogWarning($"Failed to load the rune model: {ex.Message}");
                return null;
            }
        }

        // Replaces the placeholder's mesh, material and collider with the bundled model.
        private static void ApplyModel(GameObject prefab, GameObject model)
        {
            MeshFilter modelFilter = model.GetComponentInChildren<MeshFilter>(true);
            MeshRenderer modelRenderer = model.GetComponentInChildren<MeshRenderer>(true);
            if (modelFilter == null || modelRenderer == null)
            {
                return;
            }

            MeshRenderer[] renderers = prefab.GetComponentsInChildren<MeshRenderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                MeshRenderer renderer = renderers[i];
                if (i > 0)
                {
                    renderer.enabled = false;
                    continue;
                }

                MeshFilter filter = renderer.GetComponent<MeshFilter>();
                if (filter == null)
                {
                    filter = renderer.gameObject.AddComponent<MeshFilter>();
                }

                filter.sharedMesh = modelFilter.sharedMesh;
                renderer.sharedMaterials = modelRenderer.sharedMaterials;
                renderer.transform.localPosition = modelFilter.transform.localPosition;
                renderer.transform.localRotation = modelFilter.transform.localRotation;
                renderer.transform.localScale = modelFilter.transform.localScale;
            }

            BoxCollider modelCollider = model.GetComponentInChildren<BoxCollider>(true);
            if (modelCollider == null)
            {
                return;
            }

            foreach (Collider existing in prefab.GetComponentsInChildren<Collider>(true))
            {
                UnityEngine.Object.Destroy(existing);
            }

            BoxCollider collider = prefab.AddComponent<BoxCollider>();
            collider.center = modelCollider.center;
            collider.size = modelCollider.size;
        }

        private static void AddRunes()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= AddRunes;

            GameObject model = LoadModelPrefab();
            foreach (RuneDefinition rune in Runes)
            {
                ItemConfig config = new ItemConfig
                {
                    Name = rune.DisplayName,
                    Description = rune.Description,
                    Enabled = false, // no recipe yet
                    Icons = new[] { LoadIcon(rune.PrefabName) ?? CreatePlaceholderIcon(rune.Color) }
                };

                CustomItem customItem = new CustomItem(rune.PrefabName, PlaceholderBasePrefab, config);
                if (model != null)
                {
                    ApplyModel(customItem.ItemPrefab, model);
                }
                if (rune.PrefabName == "IzzemisMod_RuneValheim")
                {
                    customItem.ItemDrop.m_itemData.m_shared.m_maxStackSize = 1;
                }

                ItemManager.Instance.AddItem(customItem);
            }
        }

        // Loads Assets/Icons/<Name>.png (embedded) where the prefab is IzzemisMod_Rune<Name>.
        private static Sprite LoadIcon(string prefabName)
        {
            string resource = "Icons." + prefabName.Substring("IzzemisMod_Rune".Length) + ".png";
            using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resource))
            {
                if (stream == null)
                {
                    return null;
                }

                using (MemoryStream buffer = new MemoryStream())
                {
                    stream.CopyTo(buffer);
                    Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                    // Called via reflection: the ImageConversion module can't be referenced from net48.
                    Type imageConversion = Type.GetType("UnityEngine.ImageConversion, UnityEngine.ImageConversionModule");
                    MethodInfo loadImage = imageConversion?.GetMethod("LoadImage", new[] { typeof(Texture2D), typeof(byte[]) });
                    if (loadImage == null || !(bool)loadImage.Invoke(null, new object[] { texture, buffer.ToArray() }))
                    {
                        return null;
                    }

                    return Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
                }
            }
        }

        // Draws a filled, soft-edged diamond with a darker border on a transparent background.
        private static Sprite CreatePlaceholderIcon(Color color)
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color[] pixels = new Color[size * size];
            float center = (size - 1) / 2f;
            float radius = size * 0.42f;
            Color border = new Color(color.r * 0.4f, color.g * 0.4f, color.b * 0.4f, 1f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float distance = Mathf.Abs(x - center) + Mathf.Abs(y - center);
                    Color pixel = Color.clear;
                    if (distance <= radius - 3f)
                    {
                        pixel = color;
                    }
                    else if (distance <= radius)
                    {
                        pixel = border;
                    }

                    pixels[y * size + x] = pixel;
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f));
        }
    }
}
