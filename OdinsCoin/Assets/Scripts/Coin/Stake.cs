using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Staking your treasure at Odin's altar on deck. Lay a chest on the altar and throw: Odin's eye and the chest is
    /// raised a step (plain, silver, gold, Odin's hoard), each step doubling what it's worth; the serpent and Odin
    /// takes it. Or stake everything on deck at once: win and every chest becomes Odin's hoard, lose and the deck is
    /// bare. The odds are always shown, and there's only game gold at stake.
    /// </summary>
    public static class Stake
    {
        /// <summary>The chance Odin smiles on a stake.</summary>
        public const float Odds = 0.5f;
        /// <summary>How long you have to press again to stake everything (s), so it's never done by accident.</summary>
        public const float ConfirmWindow = 3f;

        public static bool CanRaise(int tier) { return tier < TreasureChest.MaxTier; }

        /// <summary>The tier a chest reaches when Odin smiles on it.</summary>
        public static int Raised(int tier) { return Mathf.Min(TreasureChest.MaxTier, tier + 1); }

        /// <summary>What a chest would be worth if the stake is won.</summary>
        public static int WinValue(int baseGold, int tier, Fortune fortune) { return TreasureChest.Worth(baseGold, Raised(tier), fortune); }

        /// <summary>Everything on deck together: what it's worth now, and what it would be worth as Odin's hoard.</summary>
        public static void AllOrNothing(IList<TreasureChest> chests, Fortune fortune, out int now, out int ifWon)
        {
            now = 0; ifWon = 0;
            foreach (var c in chests)
            {
                if (c == null) continue;
                now += TreasureChest.Worth(c.BaseGold, c.Tier, fortune);
                ifWon += TreasureChest.Worth(c.BaseGold, TreasureChest.MaxTier, fortune);
            }
        }

        /// <summary>The chests stowed on the ship's deck (not held, not sold).</summary>
        public static List<TreasureChest> OnDeck(Longship ship)
        {
            var list = new List<TreasureChest>();
            foreach (var c in TreasureChest.All)
                if (c != null && c.Stowed(ship)) list.Add(c);
            return list;
        }

        /// <summary>The line shown for a stake: what you have, what you could have, and the odds.</summary>
        public static string Offer(int now, int ifWon)
        {
            return now + " gold: " + ifWon + " or nothing (" + Mathf.RoundToInt(Odds * 100f) + "%)";
        }
    }
}
