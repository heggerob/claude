using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Buried hoards: every place has one, dug in somewhere out beyond its houses, marked only by a cairn and a
    /// cross cut in the turf. Dig at the cairn (E) and up comes a silver chest. You'll rarely stumble on one by
    /// luck: plundered chests sometimes hold a map to another place's hoard, which puts a cross on your chart and
    /// sends a raven to circle over the cairn when you're near.
    /// </summary>
    public static class Hoards
    {
        /// <summary>How far out from the place's main building a hoard lies (m).</summary>
        public const float MinDistance = 350f, MaxDistance = 800f;
        /// <summary>The share of plundered chests with a map in them.</summary>
        public const float MapChance = 1f / 3f;
        /// <summary>How close you must be to dig (m), and how near before the raven comes (m).</summary>
        public const float DigRange = 2.5f, RavenRange = 2000f;
        public const int MinGold = 150, MaxGold = 300;

        /// <summary>The cairns standing now, and the place each belongs to.</summary>
        public static readonly List<KeyValuePair<Transform, Place>> Cairns = new List<KeyValuePair<Transform, Place>>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Cairns.Clear(); }

        /// <summary>
        /// Where a place's hoard is buried (global, with its ground height): out on dry, open ground between
        /// <see cref="MinDistance"/> and <see cref="MaxDistance"/> from its main building, clear of its buildings,
        /// the same spot every time.
        /// </summary>
        public static bool Spot(Place place, List<Plot> plots, System.Func<float, float, float> height, out Vector3 spot)
        {
            var main = PlaceLife.MainBuilding(plots);
            var rng = new System.Random(place.name.GetHashCode() ^ 0x5eed);
            for (int tries = 0; tries < 60; tries++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = Mathf.Lerp(MinDistance, MaxDistance, (float)rng.NextDouble());
                var p = new Vector3(main.at.x + Mathf.Sin(a) * r, 0f, main.at.z + Mathf.Cos(a) * r);
                float h = height(p.x, p.z);
                if (h < 2f * WorldMap.Scale || h > 600f) continue;
                bool clear = true;
                foreach (var plot in plots) if (PlaceLife.InsideBuilding(plot, p, 10f)) { clear = false; break; }
                if (!clear) continue;
                spot = new Vector3(p.x, h, p.z);
                return true;
            }
            spot = Vector3.zero;
            return false;
        }

        /// <summary>A plundered chest has been opened: does it hold a map? (<paramref name="roll"/> 0..1.)</summary>
        public static bool HasMap(float roll) { return roll < MapChance; }

        /// <summary>
        /// The place whose hoard a new map leads to: the nearest one to <paramref name="from"/> (global) that's
        /// neither dug up nor already mapped, or null.
        /// </summary>
        public static Place MapTo(WorldMap map, Vector3 from, ICollection<string> dug, ICollection<string> mapped)
        {
            Place best = null;
            float bestD = float.MaxValue;
            foreach (var p in Places.All)
            {
                if (dug.Contains(p.name) || mapped.Contains(p.name)) continue;
                float d = (Places.Position(map, p) - from).sqrMagnitude;
                if (d < bestD) { bestD = d; best = p; }
            }
            return best;
        }

        static readonly Dictionary<string, Vector3> spots = new Dictionary<string, Vector3>();

        /// <summary>Where a place's hoard lies (global), worked out from its settlement's layout and remembered.</summary>
        public static bool SpotOf(WorldMap map, Place place, out Vector3 spot)
        {
            if (spots.TryGetValue(place.name, out spot)) return true;
            var plots = Settlements.Layout(map, place);
            if (plots.Count == 0 || !Spot(place, plots, (x, z) => TerrainDetail.Height(map, x, z), out spot)) return false;
            spots[place.name] = spot;
            return true;
        }

        /// <summary>The cairn you're standing at (to dig), if any.</summary>
        public static bool Near(Vector3 scenePos, out Transform cairn, out Place place)
        {
            Cairns.RemoveAll(kv => kv.Key == null);
            foreach (var kv in Cairns)
            {
                var d = kv.Key.position - scenePos;
                d.y = 0f;
                if (d.magnitude < DigRange) { cairn = kv.Key; place = kv.Value; return true; }
            }
            cairn = null; place = null;
            return false;
        }

        static readonly Color StoneA = new Color(0.58f, 0.56f, 0.52f), StoneB = new Color(0.46f, 0.45f, 0.43f), Turf = new Color(0.36f, 0.27f, 0.18f);

        /// <summary>A cairn of stacked stones with a cross cut in the turf beside it.</summary>
        public static VikingModel Model()
        {
            var m = new MergedModel();
            m.Put(StoneB, SurfaceKind.Stone, MeshData.Ellipsoid(new Vector3(0f, 0.2f, 0f), new Vector3(0.6f, 0.3f, 0.5f), 8, 5));
            m.Put(StoneA, SurfaceKind.Stone, MeshData.Ellipsoid(new Vector3(0.05f, 0.55f, 0f), new Vector3(0.42f, 0.24f, 0.36f), 8, 5));
            m.Put(StoneB, SurfaceKind.Stone, MeshData.Ellipsoid(new Vector3(-0.03f, 0.84f, 0.02f), new Vector3(0.28f, 0.18f, 0.24f), 7, 4));
            m.Put(StoneA, SurfaceKind.Stone, MeshData.Ellipsoid(new Vector3(0f, 1.05f, 0f), new Vector3(0.15f, 0.12f, 0.14f), 6, 4));
            foreach (float a in new[] { 45f, -45f })
                m.Put(Turf, SurfaceKind.Plain, MeshData.Box(Vector3.zero, new Vector3(0.18f, 0.04f, 1.4f)).Transformed(new Vector3(1.4f, 0.02f, 0f), Quaternion.Euler(0f, a, 0f), Vector3.one));
            return m.ToModel("Cairn");
        }

        /// <summary>Put a place's cairn up in the scene (unless its hoard has been dug up).</summary>
        public static Transform Build(Transform parent, Place place, Vector3 global)
        {
            if (Upgrades.Current.Dug.Contains(place.name)) return null;
            var t = new GameObject("Hoard Cairn").transform;
            t.SetParent(parent, false);
            t.position = WorldOrigin.ToScene(global.x, global.z, global.y - 0.1f);
            t.rotation = Quaternion.Euler(0f, (place.name.GetHashCode() & 0xff) * 1.4f, 0f);
            ModelView.Show(Model(), t);
            Cairns.Add(new KeyValuePair<Transform, Place>(t, place));
            return t;
        }

        /// <summary>Dig up a place's hoard: a silver chest by the cairn, and the cairn is gone.</summary>
        public static TreasureChest Dig(Transform cairn, Place place, Transform world)
        {
            var u = Upgrades.Current;
            if (!u.Dug.Contains(place.name)) { u.Dug.Add(place.name); Secrets.Found(place.name); }
            u.Maps.Remove(place.name);
            var rng = new System.Random(place.name.GetHashCode());
            var chest = TreasureChest.Create(world, cairn.position + cairn.right * 1.4f + Vector3.up * 0.05f, cairn.eulerAngles.y, MinGold + rng.Next(MaxGold - MinGold + 1));
            chest.SetTier(1);
            Cairns.RemoveAll(kv => kv.Key == cairn);
            Object.Destroy(cairn.gameObject);
            return chest;
        }
    }
}
