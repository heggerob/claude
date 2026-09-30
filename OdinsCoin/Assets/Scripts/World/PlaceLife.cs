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

        /// <summary>Who guards a place: Saxons (and the Irish and Scots) at the monasteries and in England, housecarls in the North.</summary>
        public static bool SaxonGuards(Place p) { return p.kind == PlaceKind.Monastery || p.name == "Lundenwic"; }

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
            return Spots(centre, 0f, 0f, 0f, count, radius, seed);
        }

        /// <summary>A place's main building: the church, the great hall, or failing those the first house after the jetty.</summary>
        public static Plot MainBuilding(List<Plot> plots)
        {
            var main = plots.Count > 1 ? plots[1] : plots[0];
            foreach (var p in plots)
                if (p.kind == BuildingKind.Church || p.kind == BuildingKind.GreatHall) return p;
            return main;
        }

        /// <summary>Is a point (global, flat) inside a building's walls, give or take <paramref name="margin"/>?</summary>
        public static bool InsideBuilding(Plot plot, Vector3 p, float margin)
        {
            var foot = Buildings.Footprint(plot.kind);
            var local = Quaternion.Euler(0f, -plot.yaw, 0f) * new Vector3(p.x - plot.at.x, 0f, p.z - plot.at.z);
            if (plot.kind == BuildingKind.Jetty) return Mathf.Abs(local.x) < 2f + margin && local.z > -margin && local.z < 40f + margin;
            return Mathf.Abs(local.x) < foot.x + margin && Mathf.Abs(local.z) < foot.z + margin;
        }

        /// <summary>
        /// Where a place's chests (or guards) stand: round its main building, on dry land, never inside any of its
        /// buildings. Tries further out when the near ring is taken up by sea or houses.
        /// </summary>
        public static List<Vector3> Stations(WorldMap map, List<Plot> plots, Plot main, int count, float radius, int seed)
        {
            var found = new List<Vector3>();
            var foot = Buildings.Footprint(main.kind);
            for (int round = 0; round < 8 && found.Count < count; round++)
                foreach (var p in Spots(main.at, main.yaw, foot.x, foot.z, count * 3, radius + round * 4f, seed + round * 101))
                {
                    if (found.Count >= count) break;
                    if (TerrainDetail.Height(map, p.x, p.z) < 0.5f * WorldMap.Scale) continue;
                    bool clear = true;
                    foreach (var plot in plots) if (InsideBuilding(plot, p, 0.8f)) { clear = false; break; }
                    foreach (var q in found) if (Vector3.Distance(p, q) < 1.5f) clear = false;
                    if (clear) found.Add(p);
                }
            return found;
        }

        /// <summary>
        /// Where townsfolk stop and stand about: before the door of each house (and at its gable ends), on dry
        /// land and clear of every building. <paramref name="height"/> gives the ground height at a global spot.
        /// </summary>
        public static List<Vector3> Doorsteps(List<Plot> plots, System.Func<float, float, float> height)
        {
            var list = new List<Vector3>();
            foreach (var plot in plots)
            {
                if (plot.kind == BuildingKind.Jetty || plot.kind == BuildingKind.Palisade) continue;
                var foot = Buildings.Footprint(plot.kind);
                var turn = Quaternion.Euler(0f, plot.yaw, 0f);
                foreach (var off in new[] { new Vector3(foot.x + 2.5f, 0f, 0f), new Vector3(-foot.x - 2.5f, 0f, 0f), new Vector3(0f, 0f, foot.z + 2.5f), new Vector3(0f, 0f, -foot.z - 2.5f) })
                {
                    var p = plot.at + turn * off;
                    if (height(p.x, p.z) < 0.5f * WorldMap.Scale) continue;
                    bool clear = true;
                    foreach (var other in plots) if (InsideBuilding(other, p, 0.8f)) { clear = false; break; }
                    if (clear) list.Add(new Vector3(p.x, 0f, p.z));
                }
            }
            return list;
        }

        /// <summary>Can someone walk straight from a to b (global, flat) without going through a building?</summary>
        public static bool ClearPath(List<Plot> plots, Vector3 a, Vector3 b)
        {
            int steps = Mathf.Max(2, Mathf.CeilToInt(Vector3.Distance(a, b) / 1.5f));
            for (int i = 1; i < steps; i++)
            {
                var p = Vector3.Lerp(a, b, i / (float)steps);
                foreach (var plot in plots) if (InsideBuilding(plot, p, 0.5f)) return false;
            }
            return true;
        }

        /// <summary>How many people live about a place: a handful at a farm or monastery, more in a town.</summary>
        public static int FolkOf(Place place, int plots)
        {
            return place.kind == PlaceKind.Town ? Mathf.Clamp(3 + plots / 2, 4, 12) : Mathf.Clamp(1 + plots / 3, 2, 5);
        }

        /// <summary>
        /// Spots round a building of half-size (<paramref name="halfX"/>, <paramref name="halfZ"/>) turned to
        /// <paramref name="yaw"/>: on a ring <paramref name="radius"/> out from its walls, never inside them.
        /// </summary>
        public static List<Vector3> Spots(Vector3 centre, float yaw, float halfX, float halfZ, int count, float radius, int seed)
        {
            var list = new List<Vector3>();
            var rng = new System.Random(seed);
            var turn = Quaternion.Euler(0f, yaw, 0f);
            for (int i = 0; i < count; i++)
            {
                float a = (i + (float)rng.NextDouble() * 0.5f) / Mathf.Max(1, count) * Mathf.PI * 2f;
                float r = radius * (0.6f + 0.4f * (float)rng.NextDouble());
                list.Add(centre + turn * new Vector3(Mathf.Cos(a) * (halfX + r), 0f, Mathf.Sin(a) * (halfZ + r)));
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
