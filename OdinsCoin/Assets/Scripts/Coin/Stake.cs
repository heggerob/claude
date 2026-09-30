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

        /// <summary>The odds now: even, tilted a little your way by each Rune of Luck you've woken.</summary>
        public static float CurrentOdds { get { return RuneShrines.StakeOdds(Upgrades.Current.Luck); } }

        /// <summary>How many ravens carry off a lost stake.</summary>
        public const int Swarm = 7;

        /// <summary>Odin takes it: the chest bursts into a swarm of ravens that scatter into the sky.</summary>
        public static void RavenSwarm(Transform world, Vector3 at)
        {
            for (int i = 0; i < Swarm; i++)
            {
                var start = at + new Vector3(Random.Range(-0.5f, 0.5f), 0.4f + Random.Range(0f, 0.6f), Random.Range(-0.5f, 0.5f));
                RavenBird.Create(world, start, null, false).FlyAway();
            }
            Sfx.At(SfxId.Curse, at, 1f, 0.1f);
        }

        /// <summary>Odin smiles: a flash of gold light where the chest sits, fading over a second.</summary>
        public static void GoldFlash(Transform chest)
        {
            var light = new GameObject("Odin's Favour").AddComponent<Light>();
            light.transform.SetParent(chest, false);
            light.transform.localPosition = new Vector3(0f, 1.2f, 0f);
            light.type = LightType.Point;
            light.color = new Color(1f, 0.82f, 0.35f);
            light.range = 8f;
            light.intensity = 4f;
            light.gameObject.AddComponent<FadeLight>();
        }

        /// <summary>What the Seer's Foresight adds to the altar's prompt, knowing how the next throw will land.</summary>
        public static string Foresight(bool odinsEye)
        {
            return odinsEye ? "\n<color=#ffd060>Foresight: the coin will show Odin's eye.</color>" : "\n<color=#88cc88>Foresight: the coin will show the serpent.</color>";
        }

        /// <summary>The line shown for a stake: what you have, what you could have, and the odds.</summary>
        public static string Offer(int now, int ifWon)
        {
            return now + " gold: " + ifWon + " or nothing (" + Mathf.RoundToInt(CurrentOdds * 100f) + "%)";
        }
    }
}
