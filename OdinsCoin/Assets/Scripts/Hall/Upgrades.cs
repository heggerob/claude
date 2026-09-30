using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    public enum UpgradeKind { Sail, Oars, Hull, Mail, Axe }

    public class UpgradeDef
    {
        public UpgradeKind kind;
        public string name;
        public string[] levels;   // what you get at level 1, 2, ...
        public int[] costs;       // gold for level 1, 2, ...
        public string effect;     // per level
    }

    /// <summary>What you can buy in the mead hall: a better ship and better gear. Pure logic.</summary>
    public class Upgrades
    {
        public static readonly UpgradeDef[] All =
        {
            new UpgradeDef { kind = UpgradeKind.Sail, name = "Sail",  effect = "+10% sail speed per level",
                levels = new[] { "Striped wool sail", "Double-woven sail", "Jarl's red sail" }, costs = new[] { 150, 350, 700 } },
            new UpgradeDef { kind = UpgradeKind.Oars, name = "Oars",  effect = "+25% rowing speed per level",
                levels = new[] { "Ash oars", "Two more rowers", "A full crew of rowers" }, costs = new[] { 100, 250, 500 } },
            new UpgradeDef { kind = UpgradeKind.Hull, name = "Hull",  effect = "Rán's Net slows you 35% less per level",
                levels = new[] { "Pine-tarred hull", "Oak strakes" }, costs = new[] { 120, 300 } },
            new UpgradeDef { kind = UpgradeKind.Mail, name = "Mail",  effect = "+25 health per level",
                levels = new[] { "Padded gambeson", "Ring mail", "Jarl's byrnie" }, costs = new[] { 150, 400, 800 } },
            new UpgradeDef { kind = UpgradeKind.Axe,  name = "Axe",   effect = "+15% axe damage per level",
                levels = new[] { "Sharpened edge", "Dane axe", "Pattern-welded axe" }, costs = new[] { 150, 400, 800 } },
        };

        static Upgrades current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { current = null; }

        public static Upgrades Current { get { return current ?? (current = new Upgrades()); } }

        public static void SetCurrent(Upgrades u) { current = u; }

        public readonly int[] Levels = new int[All.Length];

        /// <summary>Where the voyage was left (global position and heading) when it wasn't lying at home; else she starts at the jetty.</summary>
        public bool AtSea;
        public double SeaX, SeaZ;
        public float SeaHeading;

        /// <summary>The ships you own (design ids), and the one moored at the jetty for you to sail.</summary>
        public readonly List<string> Fleet = new List<string> { Shipwright.Starter.id };
        public string Sailing = Shipwright.Starter.id;

        public bool Owns(ShipDesign d) { return Fleet.Contains(d.id); }

        public bool CanBuyShip(ShipDesign d, Fortune fortune) { return !Owns(d) && fortune.Gold >= Shipwright.Price(d); }

        /// <summary>Pay the shipwright; the new hull is yours and the one you'll sail.</summary>
        public bool BuyShip(ShipDesign d, Fortune fortune)
        {
            if (!CanBuyShip(d, fortune)) return false;
            fortune.Gold -= Shipwright.Price(d);
            Fleet.Add(d.id);
            Sailing = d.id;
            return true;
        }

        /// <summary>The ship you sail: the one chosen, if it's yours, else the starter.</summary>
        public ShipDesign SailingDesign
        {
            get
            {
                var d = Shipwright.Find(Sailing);
                return d != null && Owns(d) ? d : Shipwright.Starter;
            }
        }

        public static UpgradeDef Def(UpgradeKind kind) { return All[(int)kind]; }

        public int Level(UpgradeKind kind) { return Levels[(int)kind]; }

        public bool Maxed(UpgradeKind kind) { return Level(kind) >= Def(kind).costs.Length; }

        /// <summary>Gold for the next level, or -1 if it's maxed.</summary>
        public int NextCost(UpgradeKind kind) { return Maxed(kind) ? -1 : Def(kind).costs[Level(kind)]; }

        public bool CanBuy(UpgradeKind kind, Fortune fortune) { return !Maxed(kind) && fortune.Gold >= NextCost(kind); }

        public bool Buy(UpgradeKind kind, Fortune fortune)
        {
            if (!CanBuy(kind, fortune)) return false;
            fortune.Gold -= NextCost(kind);
            Levels[(int)kind]++;
            return true;
        }

        public float SailMultiplier { get { return 1f + 0.1f * Level(UpgradeKind.Sail); } }
        public float OarMultiplier { get { return 1f + 0.25f * Level(UpgradeKind.Oars); } }
        /// <summary>0..1: how much of Rán's Net the hull shrugs off.</summary>
        public float LeakResist { get { return Mathf.Clamp01(0.35f * Level(UpgradeKind.Hull)); } }
        public float HealthBonus { get { return 25f * Level(UpgradeKind.Mail); } }
        public float AxeMultiplier { get { return 1f + 0.15f * Level(UpgradeKind.Axe); } }

        /// <summary>Thrust multiplier from Rán's Net after the hull upgrades.</summary>
        public float LeakMultiplier(Fortune fortune)
        {
            float loss = 1f - fortune.ShipThrustMultiplier;
            return 1f - loss * (1f - LeakResist);
        }
    }
}
