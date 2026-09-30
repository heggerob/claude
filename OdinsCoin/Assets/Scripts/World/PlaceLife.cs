using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>What a real place holds for a raider: chests and the guards over them.</summary>
    public struct Plunder
    {
        public int chests, minGold, maxGold, guards;
    }

    /// <summary>
    /// What happens at the real places: monasteries, halls and fortresses hold plunder behind their guards, and
    /// the market towns have traders who buy it (the great markets pay best). A place stays plundered once
    /// every chest has been carried off, for the rest of the voyage.
    /// </summary>
    public static class PlaceLife
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Raided.Clear(); }

        /// <summary>The places already stripped this voyage.</summary>
        public static readonly HashSet<string> Raided = new HashSet<string>();

        /// <summary>How close the ship must come before the guards turn out and the chests are there to take (m).</summary>
        public const float PopulateWithin = 3000f;

        public static Plunder PlunderOf(PlaceKind kind)
        {
            switch (kind)
            {
                case PlaceKind.Monastery: return new Plunder { chests = 4, minGold = 150, maxGold = 300, guards = 3 };
                case PlaceKind.Hall: return new Plunder { chests = 2, minGold = 200, maxGold = 350, guards = 4 };
                case PlaceKind.Fortress: return new Plunder { chests = 3, minGold = 250, maxGold = 400, guards = 7 };
                case PlaceKind.Landing: return new Plunder { chests = 1, minGold = 60, maxGold = 120, guards = 1 };
                default: return new Plunder(); // market towns trade; they aren't raided
            }
        }

        /// <summary>Does this place have a market to sell plunder at?</summary>
        public static bool HasMarket(Place p) { return p.kind == PlaceKind.Town; }

        /// <summary>What a place's traders pay, times a chest's worth: the great markets of the North pay best.</summary>
        public static float PriceFactor(Place p)
        {
            switch (p.name)
            {
                case "Hedeby": return 1.3f;
                case "Birka": return 1.25f;
                case "Jorvik": case "Dyflin": case "Visby": return 1.2f;
                default: return 1.1f;
            }
        }

        /// <summary>The spots (global) around a place's main building where its chests and guards go, from a seed.</summary>
        public static List<Vector3> Spots(Vector3 centre, int count, float radius, int seed)
        {
            var list = new List<Vector3>();
            var rng = new System.Random(seed);
            for (int i = 0; i < count; i++)
            {
                float a = (i + (float)rng.NextDouble() * 0.5f) / Mathf.Max(1, count) * Mathf.PI * 2f;
                float r = radius * (0.6f + 0.4f * (float)rng.NextDouble());
                list.Add(centre + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r));
            }
            return list;
        }
    }

    /// <summary>A trader at a real town's jetty who buys chests, carried to him or straight off a ship moored alongside.</summary>
    public class Market : MonoBehaviour
    {
        public static readonly List<Market> All = new List<Market>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }

        public const float TradeRange = 3.2f, CargoRange = 70f;
        public Place Place { get; private set; }
        public float Price { get; private set; }

        public static Market Create(Transform parent, Place place, Vector3 localOnJetty)
        {
            var t = new GameObject(place.name + " Trader").transform;
            t.SetParent(parent, false);
            t.localPosition = localOnJetty;
            // Faces across the jetty to where ships moor.
            t.localRotation = Quaternion.Euler(0f, 90f, 0f);
            var spec = NpcHeroes.Merchant(place.name.GetHashCode() & 0x7fff);
            HeroIdle.Add(t, HeroBuilder.Build(t, spec), spec.weapon);
            LongshipBuilder.Deco(PrimitiveType.Cube, t, new Vector3(0f, 0.45f, 0.9f), new Vector3(1.4f, 0.9f, 0.7f), Materials.Wood);
            LongshipBuilder.Deco(PrimitiveType.Cylinder, t, new Vector3(0.3f, 1f, 0.9f), new Vector3(0.3f, 0.02f, 0.3f), Materials.Gold);
            var m = t.gameObject.AddComponent<Market>();
            m.Place = place;
            m.Price = PlaceLife.PriceFactor(place);
            return m;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        /// <summary>The trader close enough to talk to, or null.</summary>
        public static Market Near(Vector3 p)
        {
            foreach (var m in All) if (m != null && Vector3.Distance(p, m.transform.position) < TradeRange) return m;
            return null;
        }

        public bool ShipInRange(Longship ship) { return ship != null && Vector3.Distance(ship.transform.position, transform.position) <= CargoRange; }

        public int Sell(TreasureChest chest) { return HomeHarbour.SellChest(chest, transform, Price); }

        /// <summary>Buy every chest stowed on the ship, if she's alongside. Returns the gold.</summary>
        public int SellCargo(Longship ship, out int count)
        {
            count = 0;
            int gold = 0;
            if (!ShipInRange(ship)) return 0;
            foreach (var chest in TreasureChest.All.ToArray())
            {
                if (chest == null || !chest.Stowed(ship)) continue;
                gold += Sell(chest);
                count++;
            }
            return gold;
        }

        /// <summary>What he'd pay for the ship's cargo now.</summary>
        public int CargoValue(Longship ship, out int count)
        {
            int gold = HomeHarbour.CargoValue(ship, out count);
            return Mathf.RoundToInt(gold * Price);
        }
    }
}
