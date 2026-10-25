using System;
using System.Collections.Generic;
using UnityEngine;

namespace IzzemisMod
{
    internal sealed class ItemRarityDefinition
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string TooltipColor { get; }
        public Color GlowColor { get; }
        public int Rank { get; }
        public int MaxModifiers { get; }

        public ItemRarityDefinition(
            string id,
            string displayName,
            string tooltipColor,
            Color glowColor,
            int rank,
            int maxModifiers)
        {
            Id = id;
            DisplayName = displayName;
            TooltipColor = tooltipColor;
            GlowColor = glowColor;
            Rank = rank;
            MaxModifiers = maxModifiers;
        }
    }

    internal static class ItemRarityCatalog
    {
        private static readonly ItemRarityDefinition[] Definitions =
        {
            new ItemRarityDefinition("magic", "Magic", "green", new Color(0.2f, 1f, 0.35f), 1, 1),
            new ItemRarityDefinition("rare", "Rare", "blue", new Color(0.25f, 0.55f, 1f), 2, 2),
            new ItemRarityDefinition("epic", "Epic", "purple", new Color(0.75f, 0.3f, 1f), 3, 4)
        };

        public static IReadOnlyList<ItemRarityDefinition> All => Definitions;

        public static List<string> GetCommandOptions()
        {
            var options = new List<string>(Definitions.Length + 1) { "0" };
            foreach (ItemRarityDefinition definition in Definitions)
            {
                options.Add(definition.Id);
            }

            return options;
        }

        public static bool TryGet(string id, out ItemRarityDefinition definition)
        {
            foreach (ItemRarityDefinition candidate in Definitions)
            {
                if (string.Equals(candidate.Id, id, StringComparison.OrdinalIgnoreCase))
                {
                    definition = candidate;
                    return true;
                }
            }

            definition = null;
            return false;
        }
    }
}
