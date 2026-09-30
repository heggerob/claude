using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// A cave in the hillside near every place: a rock chamber dug into the steepest slope nearby, its mouth
    /// facing downhill. Inside lies a gold chest, and a draugr (a dead Viking who won't lie down) guards the way
    /// in. Once the chest is taken the cave stays empty.
    /// </summary>
    public static class Caves
    {
        /// <summary>How far out from the place's main building a cave may be (m).</summary>
        public const float MinDistance = 150f, MaxDistance = 700f;
        /// <summary>The chamber's inside: half its width, its height and its depth (m).</summary>
        public const float HalfWidth = 3f, Height = 3.6f, Depth = 8f;
        public const int MinGold = 250, MaxGold = 450;

        /// <summary>The chambers dug into the hills: global centre of the mouth, yaw, and floor height.</summary>
        public static readonly List<Vector4> Hollows = new List<Vector4>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Hollows.Clear(); }

        /// <summary>The ground height with any cave chamber dug out of it (the hill inside a cave is gone).</summary>
        public static float Hollow(double x, double z, float h)
        {
            foreach (var c in Hollows)
            {
                var local = Quaternion.Euler(0f, -c.y, 0f) * new Vector3((float)(x - c.x), 0f, (float)(z - c.z));
                if (Mathf.Abs(local.x) < HalfWidth + 0.5f && local.z < 0.5f && local.z > -Depth - 0.5f) h = Mathf.Min(h, c.w - 0.25f);
            }
            return h;
        }

        /// <summary>How far round a chamber the land is drawn finely enough to show it (m).</summary>
        public const float Margin = 2f;

        /// <summary>Does any chamber (with <see cref="Margin"/>) reach into the global rectangle (x0, z0)–(x1, z1)?</summary>
        public static bool Touches(double x0, double z0, double x1, double z1)
        {
            float reach = Mathf.Sqrt((HalfWidth + 0.5f) * (HalfWidth + 0.5f) + (Depth + 0.5f) * (Depth + 0.5f)) + Margin;
            foreach (var c in Hollows)
                if (c.x + reach > x0 && c.x - reach < x1 && c.z + reach > z0 && c.z - reach < z1) return true;
            return false;
        }

        /// <summary>
        /// Where a place's cave is (global, ground height) and which way its mouth faces (yaw, downhill): the steepest
        /// dry slope found between <see cref="MinDistance"/> and <see cref="MaxDistance"/>, clear of the buildings.
        /// </summary>
        public static bool Spot(Place place, List<Plot> plots, System.Func<float, float, float> height, out Vector3 spot, out float yaw)
        {
            var main = PlaceLife.MainBuilding(plots);
            var rng = new System.Random(place.name.GetHashCode() ^ 0xca7e);
            spot = Vector3.zero; yaw = 0f;
            float best = 2.5f; // at least a 2.5 m rise over 8 m to count as a hillside
            for (int tries = 0; tries < 80; tries++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f, r = Mathf.Lerp(MinDistance, MaxDistance, (float)rng.NextDouble());
                var p = new Vector3(main.at.x + Mathf.Sin(a) * r, 0f, main.at.z + Mathf.Cos(a) * r);
                float h = height(p.x, p.z);
                if (h < 2f * WorldMap.Scale) continue;
                // The slope: which way is downhill, and how steep.
                float gx = height(p.x + 4f, p.z) - height(p.x - 4f, p.z), gz = height(p.x, p.z + 4f) - height(p.x, p.z - 4f);
                float rise = Mathf.Sqrt(gx * gx + gz * gz);
                if (rise <= best) continue;
                bool clear = true;
                foreach (var plot in plots) if (PlaceLife.InsideBuilding(plot, p, 12f)) { clear = false; break; }
                if (!clear) continue;
                best = rise;
                spot = new Vector3(p.x, h, p.z);
                yaw = Mathf.Atan2(-gx, -gz) * Mathf.Rad2Deg; // facing downhill
            }
            return best > 2.5f;
        }

        static readonly Color RockA = new Color(0.44f, 0.42f, 0.4f), RockB = new Color(0.34f, 0.33f, 0.32f), Floor = new Color(0.3f, 0.27f, 0.23f);

        /// <summary>The chamber: boulder walls either side and at the back, a rock roof over, a floor, open at the front (+Z).</summary>
        public static VikingModel Model()
        {
            var m = new MergedModel();
            m.Put(Floor, SurfaceKind.Plain, MeshData.Box(new Vector3(0f, -0.1f, -Depth / 2f), new Vector3(HalfWidth * 2f + 1f, 0.3f, Depth + 1f)));
            for (int i = 0; i < 4; i++)
            {
                float z = -1f - i * 2.2f;
                foreach (float side in new[] { -1f, 1f })
                    m.Put(i % 2 == 0 ? RockA : RockB, SurfaceKind.Plain, MeshData.Ellipsoid(new Vector3(side * (HalfWidth + 1.3f), Height * 0.5f, z), new Vector3(1.6f, Height * 0.75f, 1.5f), 8, 5));
            }
            m.Put(RockB, SurfaceKind.Plain, MeshData.Ellipsoid(new Vector3(0f, Height * 0.5f, -Depth - 1f), new Vector3(HalfWidth + 1.5f, Height * 0.8f, 1.6f), 9, 5));
            m.Put(RockA, SurfaceKind.Plain, MeshData.Ellipsoid(new Vector3(0f, Height + 1.2f, -Depth / 2f), new Vector3(HalfWidth + 2.6f, 1.6f, Depth / 2f + 1.6f), 10, 6));
            // A lintel of stone over the mouth.
            m.Put(RockB, SurfaceKind.Plain, MeshData.Ellipsoid(new Vector3(0f, Height + 0.3f, 0.3f), new Vector3(HalfWidth + 1.4f, 0.8f, 0.9f), 8, 5));
            return m.ToModel("Cave");
        }

        /// <summary>The chamber's walls and roof to bump into (cave-local boxes: centre and size).</summary>
        public static List<Bounds> Walls()
        {
            return new List<Bounds> {
                new Bounds(new Vector3(-(HalfWidth + 0.8f), Height / 2f, -Depth / 2f), new Vector3(1.6f, Height, Depth + 1f)),
                new Bounds(new Vector3(HalfWidth + 0.8f, Height / 2f, -Depth / 2f), new Vector3(1.6f, Height, Depth + 1f)),
                new Bounds(new Vector3(0f, Height / 2f, -Depth - 0.8f), new Vector3(HalfWidth * 2f + 3f, Height, 1.6f)),
                new Bounds(new Vector3(0f, Height + 0.6f, -Depth / 2f), new Vector3(HalfWidth * 2f + 3f, 1.2f, Depth + 1.6f)) };
        }

        /// <summary>Is a point (cave-local) inside the chamber?</summary>
        public static bool Inside(Vector3 local)
        {
            return Mathf.Abs(local.x) < HalfWidth && local.y > -0.5f && local.y < Height && local.z < 0f && local.z > -Depth;
        }

        /// <summary>Put a place's cave up: the chamber, its walls, and (unless it's been emptied) the chest and its draugr.</summary>
        public static Transform Build(Transform parent, Place place, Vector3 global, float yaw)
        {
            // Dig the chamber out of the hill, and have the land round it rebuilt without it.
            var hollow = new Vector4(global.x, yaw, global.z, global.y);
            if (!Hollows.Contains(hollow)) Hollows.Add(hollow);
            if (WorldTerrain.Instance != null) WorldTerrain.Instance.Rebuild(global.x, global.z, Depth + 10f);
            var t = new GameObject("Cave").transform;
            t.SetParent(parent, false);
            t.position = WorldOrigin.ToScene(global.x, global.z, global.y - 0.2f);
            t.rotation = Quaternion.Euler(0f, yaw, 0f);
            ModelView.Show(Model(), t);
            foreach (var w in Walls())
            {
                var col = t.gameObject.AddComponent<BoxCollider>();
                col.center = w.center;
                col.size = w.size;
            }
            if (Upgrades.Current.Caves.Contains(place.name)) return t;
            var rng = new System.Random(place.name.GetHashCode() ^ 0xca7e);
            var chest = TreasureChest.Create(t, t.TransformPoint(new Vector3(0f, 0.1f, -Depth + 1.5f)), yaw + 180f, MinGold + rng.Next(MaxGold - MinGold + 1));
            chest.SetTier(2);
            chest.Cave = place.name;
            Saxon.Create(t, t.TransformPoint(new Vector3(0.8f, 0.3f, -2f)), NpcHeroes.Draugr(place.name.GetHashCode()));
            return t;
        }
    }
}
