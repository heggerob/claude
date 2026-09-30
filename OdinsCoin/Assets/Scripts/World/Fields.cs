using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>A fenced field by a farm: its middle (global), which way its furrows run, half its size, and its crop.</summary>
    public struct Field
    {
        public Vector3 centre;
        public float yaw, halfX, halfZ;
        /// <summary>Ripe barley (gold), or a green crop not yet ripe.</summary>
        public bool ripe;
    }

    /// <summary>
    /// The fields round a place's houses: a fenced strip of ploughed land behind a longhouse, hall or storehouse,
    /// on dry, fairly level ground and clear of every building and each other, with a gate in the fence on the
    /// side facing the house. Nothing else grows in them (they're cleared of trees and rocks).
    /// </summary>
    public static class Fields
    {
        /// <summary>How far apart the fence posts stand (m), the fence's height, and the furrows' spacing.</summary>
        public const float PostSpacing = 2.4f, FenceHeight = 1.1f, Furrow = 0.9f;

        /// <summary>
        /// Where a place's fields lie. <paramref name="height"/> gives the ground height at a global spot;
        /// <paramref name="most"/> caps how many.
        /// </summary>
        public static List<Field> Layout(List<Plot> plots, System.Func<float, float, float> height, int most, int seed)
        {
            var fields = new List<Field>();
            var rng = new System.Random(seed);
            foreach (var plot in plots)
            {
                if (fields.Count >= most) break;
                if (plot.kind != BuildingKind.Longhouse && plot.kind != BuildingKind.GreatHall && plot.kind != BuildingKind.Storehouse) continue;
                var foot = Buildings.Footprint(plot.kind);
                var turn = Quaternion.Euler(0f, plot.yaw, 0f);
                // Behind the house, then off either side of it.
                foreach (var dir in new[] { new Vector3(0f, 0f, -1f), new Vector3(1f, 0f, 0f), new Vector3(-1f, 0f, 0f) })
                {
                    var f = new Field
                    {
                        halfX = 9f + (float)rng.NextDouble() * 6f,
                        halfZ = 6f + (float)rng.NextDouble() * 4f,
                        yaw = plot.yaw,
                        ripe = rng.NextDouble() < 0.6,
                    };
                    float reach = dir.z != 0f ? foot.z + f.halfZ + 5f : foot.x + f.halfX + 5f;
                    f.centre = plot.at + turn * (dir * reach);
                    f.centre.y = 0f;
                    if (Fits(f, plots, fields, height)) { fields.Add(f); break; }
                }
            }
            return fields;
        }

        /// <summary>A field fits where all of it is dry, fairly level land, away from every building and every other field.</summary>
        public static bool Fits(Field f, List<Plot> plots, List<Field> others, System.Func<float, float, float> height)
        {
            float lo = float.MaxValue, hi = float.MinValue;
            foreach (var p in Corners(f, 1.5f, 4))
            {
                float h = height(p.x, p.z);
                if (h < 0.8f * WorldMap.Scale) return false;
                lo = Mathf.Min(lo, h); hi = Mathf.Max(hi, h);
                foreach (var plot in plots) if (PlaceLife.InsideBuilding(plot, p, 2f)) return false;
            }
            if (hi - lo > 3f) return false;
            foreach (var o in others)
                if (Vector3.Distance(o.centre, f.centre) < Mathf.Max(o.halfX, o.halfZ) + Mathf.Max(f.halfX, f.halfZ) + 2f) return false;
            return true;
        }

        /// <summary>Points over a field (global, flat), a grid of <paramref name="n"/> by <paramref name="n"/> reaching <paramref name="margin"/> past its fence.</summary>
        public static IEnumerable<Vector3> Corners(Field f, float margin, int n)
        {
            var turn = Quaternion.Euler(0f, f.yaw, 0f);
            for (int j = 0; j <= n; j++)
                for (int i = 0; i <= n; i++)
                {
                    float u = i / (float)n * 2f - 1f, v = j / (float)n * 2f - 1f;
                    yield return f.centre + turn * new Vector3(u * (f.halfX + margin), 0f, v * (f.halfZ + margin));
                }
        }

        /// <summary>Round a field nothing else grows (global x, z and radius, for <see cref="Scenery.Clearings"/>).</summary>
        public static Vector3 Clearing(Field f)
        {
            return new Vector3(f.centre.x, f.centre.z, Mathf.Sqrt(f.halfX * f.halfX + f.halfZ * f.halfZ) + 1.5f);
        }

        /// <summary>The fence to walk into (field-local boxes: centre and size), with the gate left open.</summary>
        public static List<Bounds> FenceWalls(Field f)
        {
            float t = 0.3f, h = FenceHeight + 0.2f, gate = 1.4f;
            var walls = new List<Bounds> {
                new Bounds(new Vector3(0f, h / 2f, -f.halfZ), new Vector3(f.halfX * 2f, h, t)),
                new Bounds(new Vector3(-f.halfX, h / 2f, 0f), new Vector3(t, h, f.halfZ * 2f)),
                new Bounds(new Vector3(f.halfX, h / 2f, 0f), new Vector3(t, h, f.halfZ * 2f)) };
            float side = f.halfX - gate;
            walls.Add(new Bounds(new Vector3(-(gate + side / 2f), h / 2f, f.halfZ), new Vector3(side, h, t)));
            walls.Add(new Bounds(new Vector3(gate + side / 2f, h / 2f, f.halfZ), new Vector3(side, h, t)));
            return walls;
        }

        /// <summary>Is a point (global, flat) inside the field's fence?</summary>
        public static bool Inside(Field f, Vector3 p)
        {
            var local = Quaternion.Euler(0f, -f.yaw, 0f) * new Vector3(p.x - f.centre.x, 0f, p.z - f.centre.z);
            return Mathf.Abs(local.x) <= f.halfX && Mathf.Abs(local.z) <= f.halfZ;
        }

        /// <summary>The fence posts round a field (field-local, flat): along each side at about the post spacing, a gap for the gate in the +Z side.</summary>
        public static List<Vector3> Posts(Field f)
        {
            var posts = new List<Vector3>();
            var corners = new[] { new Vector3(-f.halfX, 0f, -f.halfZ), new Vector3(f.halfX, 0f, -f.halfZ), new Vector3(f.halfX, 0f, f.halfZ), new Vector3(-f.halfX, 0f, f.halfZ) };
            for (int s = 0; s < 4; s++)
            {
                var a = corners[s]; var b = corners[(s + 1) % 4];
                int n = Mathf.Max(1, Mathf.RoundToInt(Vector3.Distance(a, b) / PostSpacing));
                for (int i = 0; i < n; i++)
                {
                    var p = Vector3.Lerp(a, b, i / (float)n);
                    // The gate: the middle of the side facing the house (+Z) is left open.
                    if (s == 2 && Mathf.Abs(p.x) < 1.4f) continue;
                    posts.Add(p);
                }
            }
            return posts;
        }

        static readonly Color Soil = new Color(0.4f, 0.28f, 0.18f), SoilDark = new Color(0.3f, 0.2f, 0.13f),
            Barley = new Color(0.82f, 0.7f, 0.36f), Sprouts = new Color(0.46f, 0.6f, 0.28f), Wood = new Color(0.46f, 0.34f, 0.22f);

        /// <summary>
        /// A field drawn in the storybook style, relative to its middle: furrows of turned earth with the crop in
        /// rows along them, and a post-and-rail fence following the ground. <paramref name="height"/> as for <see cref="Layout"/>.
        /// </summary>
        public static VikingModel Model(Field f, System.Func<float, float, float> height)
        {
            var merged = new MergedModel();
            var turn = Quaternion.Euler(0f, f.yaw, 0f);
            float baseY = height(f.centre.x, f.centre.z);
            System.Func<Vector3, float> ground = local => { var g = f.centre + turn * local; return height(g.x, g.z) - baseY; };
            // Furrows running the length of the field, each a low ridge with its crop.
            int rows = Mathf.Max(2, Mathf.FloorToInt(f.halfX * 2f / Furrow));
            for (int r = 0; r < rows; r++)
            {
                float x = -f.halfX + (r + 0.5f) * (f.halfX * 2f / rows);
                const int segs = 4;
                for (int s = 0; s < segs; s++)
                {
                    float z0 = -f.halfZ + 0.6f + s * (f.halfZ * 2f - 1.2f) / segs, z1 = z0 + (f.halfZ * 2f - 1.2f) / segs;
                    var mid = new Vector3(x, 0f, (z0 + z1) / 2f);
                    mid.y = ground(mid);
                    merged.Put(r % 2 == 0 ? Soil : SoilDark, SurfaceKind.Plain, MeshData.Box(mid + new Vector3(0f, 0.08f, 0f), new Vector3(Furrow * 0.7f, 0.22f, z1 - z0)));
                    merged.Put(f.ripe ? Barley : Sprouts, SurfaceKind.Cloth, MeshData.Box(mid + new Vector3(0f, f.ripe ? 0.5f : 0.3f, 0f), new Vector3(Furrow * 0.45f, f.ripe ? 0.62f : 0.25f, z1 - z0 - 0.1f)));
                }
            }
            // The fence: posts, and two rails between neighbours (none across the gate).
            var posts = Posts(f);
            for (int i = 0; i < posts.Count; i++)
            {
                var p = posts[i];
                p.y = ground(p);
                merged.Put(Wood, SurfaceKind.Wood, MeshData.Box(p + new Vector3(0f, FenceHeight / 2f, 0f), new Vector3(0.14f, FenceHeight, 0.14f)));
                var q = posts[(i + 1) % posts.Count];
                q.y = ground(q);
                if (Vector3.Distance(new Vector3(p.x, 0f, p.z), new Vector3(q.x, 0f, q.z)) > PostSpacing * 1.6f) continue;
                foreach (float h in new[] { 0.45f, 0.9f })
                {
                    var a = p + Vector3.up * h; var b = q + Vector3.up * h;
                    merged.Put(Wood, SurfaceKind.Wood, MeshData.Tube(new[] { a, b }, new[] { 0.04f, 0.04f }, 5));
                }
            }
            return merged.ToModel("Field");
        }
    }

    /// <summary>Builds a model with every piece of the same colour merged into one mesh, so big scatters stay cheap to draw.</summary>
    public class MergedModel
    {
        readonly Dictionary<Color, MeshData> merged = new Dictionary<Color, MeshData>();
        readonly Dictionary<Color, SurfaceKind> surfaces = new Dictionary<Color, SurfaceKind>();
        readonly List<Color> order = new List<Color>();

        public void Put(Color c, SurfaceKind surface, MeshData mesh)
        {
            MeshData into;
            if (!merged.TryGetValue(c, out into)) { into = new MeshData(); merged[c] = into; surfaces[c] = surface; order.Add(c); }
            into.Append(mesh);
        }

        public VikingModel ToModel(string joint)
        {
            var model = new VikingModel();
            model.AddJoint(joint, null, Vector3.zero);
            foreach (var c in order) model.Add(joint, c, merged[c], true, surfaces[c]);
            return model;
        }
    }
}
