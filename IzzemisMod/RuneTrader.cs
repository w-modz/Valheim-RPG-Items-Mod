using System;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;

namespace Runecarved
{
    [HarmonyPatch(typeof(Trader), nameof(Trader.GetAvailableItems))]
    internal static class RuneTrader
    {
        private sealed class RuneOffer
        {
            public RuneOffer(string rune, int price, string chest = null)
            {
                PrefabName = "IzzemisMod_Rune" + rune;
                Price = price;
                Chest = chest;
            }

            public string PrefabName { get; }
            public int Price { get; }
            public string Chest { get; }
        }

        private static readonly RuneOffer[] Offers =
        {
            new RuneOffer("Asgard", 500),
            new RuneOffer("Midgard", 500),
            new RuneOffer("Vanaheim", 1000, "chest_hildir1"),
            new RuneOffer("Muspelheim", 1000, "chest_hildir1"),
            new RuneOffer("Alfheim", 1500, "chest_hildir2"),
            new RuneOffer("Niflheim", 1500, "chest_hildir2"),
            new RuneOffer("Jotunheim", 2000, "chest_hildir3"),
            new RuneOffer("Nidavellir", 2000, "chest_hildir3")
        };

        private static readonly HashSet<string> ReportedProblems = new HashSet<string>();

        private static void Prefix(Trader __instance)
        {
            if (__instance.gameObject.name != "Hildir(Clone)" &&
                __instance.gameObject.name != "Hildir")
            {
                return;
            }

            foreach (RuneOffer offer in Offers)
            {
                if (__instance.m_items.Exists(item =>
                    item != null && item.m_prefab != null && item.m_prefab.name == offer.PrefabName))
                {
                    continue;
                }

                string key = "";
                if (offer.Chest != null && !TryGetChestKey(__instance, offer.Chest, out key))
                {
                    WarnOnce(offer.Chest, $"Could not find Hildir's unlock key for {offer.Chest}; its rune offers are unavailable.");
                    continue;
                }

                GameObject prefab = ZNetScene.instance != null
                    ? ZNetScene.instance.GetPrefab(offer.PrefabName)
                    : null;
                ItemDrop itemDrop = prefab != null ? prefab.GetComponent<ItemDrop>() : null;
                if (itemDrop == null)
                {
                    WarnOnce(offer.PrefabName, $"Could not find rune prefab {offer.PrefabName} for Hildir's shop.");
                    continue;
                }

                __instance.m_items.Add(new Trader.TradeItem
                {
                    m_prefab = itemDrop,
                    m_stack = 1,
                    m_price = offer.Price,
                    m_requiredGlobalKey = key,
                    m_buyPlayerEffects = new EffectList(),
                    m_tooltip = "",
                    m_buyKey = "",
                    m_incrementKey = ""
                });
            }
        }

        private static bool TryGetChestKey(Trader trader, string chest, out string key)
        {
            foreach (Trader.TraderUseItem useItem in trader.m_useItems)
            {
                if (useItem != null && useItem.m_prefab != null &&
                    string.Equals(useItem.m_prefab.name, chest, StringComparison.OrdinalIgnoreCase) &&
                    !string.IsNullOrEmpty(useItem.m_setsGlobalKey))
                {
                    key = useItem.m_setsGlobalKey;
                    return true;
                }
            }

            key = null;
            return false;
        }

        private static void WarnOnce(string id, string message)
        {
            if (ReportedProblems.Add(id))
            {
                Jotunn.Logger.LogWarning(message);
            }
        }
    }
}
