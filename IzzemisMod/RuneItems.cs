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

        private static readonly RuneDefinition[] Runes =
        {
            new RuneDefinition("IzzemisMod_RuneAsgard", "Rune of Asgard",
                "Removes one random modifier from an item and adds one new random modifier.",
                new Color(1f, 0.85f, 0.3f)),
            new RuneDefinition("IzzemisMod_RuneMidgard", "Rune of Midgard",
                "Upgrades a normal item to Magic and adds one random modifier.",
                new Color(0.6f, 0.45f, 0.25f)),
            new RuneDefinition("IzzemisMod_RuneVanaheim", "Rune of Vanaheim",
                "Upgrades a Magic item to Rare and adds one random modifier.",
                new Color(0.3f, 0.8f, 0.35f)),
            new RuneDefinition("IzzemisMod_RuneAlfheim", "Rune of Alfheim",
                "Upgrades a Rare item to Epic and adds one random modifier.",
                new Color(0.95f, 0.95f, 0.8f)),
            new RuneDefinition("IzzemisMod_RuneJotunheim", "Rune of Jotunheim",
                "Adds an additional modifier to an Epic item, up to 4.",
                new Color(0.4f, 0.7f, 1f)),
            new RuneDefinition("IzzemisMod_RuneMuspelheim", "Rune of Muspelheim",
                "Destroys an item of Magic rarity or higher and grants the corresponding rune.",
                new Color(1f, 0.35f, 0.1f)),
            new RuneDefinition("IzzemisMod_RuneNiflheim", "Rune of Niflheim",
                "Randomizes the values of all modifiers on an item.",
                new Color(0.6f, 0.9f, 1f)),
            new RuneDefinition("IzzemisMod_RuneHel", "Rune of Hel",
                "Randomly modifies or destroys an Epic item. Items modified cannot be further altered by other runes.",
                new Color(0.3f, 0.1f, 0.4f)),
            new RuneDefinition("IzzemisMod_RuneNidavellir", "Rune of Nidavellir",
                "Destroys an item and grants a rune holding one random modifier of the sacrificed item.",
                new Color(0.55f, 0.55f, 0.6f)),
            new RuneDefinition("IzzemisMod_RuneValheim", "Rune of Valheim",
                "Applies a modifier to an item of specific type.",
                new Color(1f, 1f, 1f)),
        };

        public static void Register()
        {
            PrefabManager.OnVanillaPrefabsAvailable += AddRunes;
        }

        private static void AddRunes()
        {
            PrefabManager.OnVanillaPrefabsAvailable -= AddRunes;

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
