using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// How much of the North you've seen, for the sea chart: it starts as blank parchment and fills in as you go.
    /// Sailing charts a band of coast round the ship; standing at a landmark and surveying the land (a beacon
    /// tower, a bell tower, a great ash, a runestone) charts a wide circle round it at once, like Zelda's towers.
    /// Kept as circles (for the save) and a coarse grid (for painting the chart quickly).
    /// </summary>
    public static class ChartReveal
    {
        /// <summary>How far round the ship you chart as you sail, and how far a landmark shows you (m).</summary>
        public const float ShipSight = 6000f, SurveyRadius = 25000f;
        /// <summary>A new band is charted each time the ship has sailed this far from the last (m).</summary>
        public const float TrailStep = 3000f;
        /// <summary>The grid's cell size (m).</summary>
        public const float Cell = 2000f;

        /// <summary>The charted circles: global x, z and radius.</summary>
        public static readonly List<Vector3> Circles = new List<Vector3>();
        /// <summary>Goes up whenever more is charted, so the chart knows to repaint.</summary>
        public static int Version { get; private set; }
        static Vector3 lastTrail = new Vector3(float.NaN, 0f, 0f);
        static bool[] grid;
        static Rect bounds;
        static int gw, gh;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Clear(); }

        public static void Clear()
        {
            Circles.Clear();
            grid = null;
            lastTrail = new Vector3(float.NaN, 0f, 0f);
            Version++;
        }

        /// <summary>Use a grid over the map's bounds from now on (filled from the circles so far).</summary>
        public static void UseGrid(Rect mapBounds)
        {
            bounds = mapBounds;
            gw = Mathf.Max(1, Mathf.CeilToInt(bounds.width / Cell));
            gh = Mathf.Max(1, Mathf.CeilToInt(bounds.height / Cell));
            grid = new bool[gw * gh];
            foreach (var c in Circles) Mark(c);
            Version++;
        }

        /// <summary>Chart a circle round a global position.</summary>
        public static void Reveal(float x, float z, float radius)
        {
            var c = new Vector3(Mathf.Round(x / 100f) * 100f, Mathf.Round(z / 100f) * 100f, Mathf.Round(radius / 100f) * 100f);
            // Already charted: nothing to add.
            foreach (var o in Circles)
                if (new Vector2(o.x - c.x, o.y - c.y).magnitude + c.z <= o.z) return;
            Circles.Add(c);
            if (grid != null) Mark(c);
            Version++;
        }

        /// <summary>The ship is at a global position: chart round it now and then as she sails on.</summary>
        public static void Sail(float x, float z)
        {
            if (!float.IsNaN(lastTrail.x) && new Vector2(x - lastTrail.x, z - lastTrail.z).magnitude < TrailStep) return;
            lastTrail = new Vector3(x, 0f, z);
            Reveal(x, z, ShipSight);
        }

        /// <summary>Has this global spot been charted?</summary>
        public static bool Seen(double x, double z)
        {
            if (grid != null)
            {
                int i = (int)System.Math.Floor((x - bounds.xMin) / Cell), j = (int)System.Math.Floor((z - bounds.yMin) / Cell);
                return i >= 0 && j >= 0 && i < gw && j < gh && grid[j * gw + i];
            }
            foreach (var c in Circles)
            {
                double dx = x - c.x, dz = z - c.y;
                if (dx * dx + dz * dz <= (double)c.z * c.z) return true;
            }
            return false;
        }

        static void Mark(Vector3 c)
        {
            int i0 = Mathf.FloorToInt((c.x - c.z - bounds.xMin) / Cell), i1 = Mathf.FloorToInt((c.x + c.z - bounds.xMin) / Cell);
            int j0 = Mathf.FloorToInt((c.y - c.z - bounds.yMin) / Cell), j1 = Mathf.FloorToInt((c.y + c.z - bounds.yMin) / Cell);
            for (int j = Mathf.Max(0, j0); j <= Mathf.Min(gh - 1, j1); j++)
                for (int i = Mathf.Max(0, i0); i <= Mathf.Min(gw - 1, i1); i++)
                {
                    float cx = bounds.xMin + (i + 0.5f) * Cell, cz = bounds.yMin + (j + 0.5f) * Cell;
                    if (new Vector2(cx - c.x, cz - c.y).magnitude <= c.z + Cell * 0.5f) grid[j * gw + i] = true;
                }
        }

        /// <summary>The charted circles for the save: "x:z:r;x:z:r;…" in whole metres.</summary>
        public static string Serialize()
        {
            var sb = new StringBuilder();
            foreach (var c in Circles)
            {
                if (sb.Length > 0) sb.Append(';');
                sb.Append(((int)c.x).ToString(CultureInfo.InvariantCulture)).Append(':').Append(((int)c.y).ToString(CultureInfo.InvariantCulture)).Append(':').Append(((int)c.z).ToString(CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }

        /// <summary>Restore the charted circles from a save (bad entries are skipped).</summary>
        public static void Deserialize(string text)
        {
            Circles.Clear();
            if (!string.IsNullOrEmpty(text))
                foreach (var part in text.Split(';'))
                {
                    var n = part.Split(':');
                    int x, z, r;
                    if (n.Length == 3 && int.TryParse(n[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out x) && int.TryParse(n[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out z)
                        && int.TryParse(n[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out r) && r > 0 && r <= 100000)
                        Circles.Add(new Vector3(x, z, r));
                }
            if (grid != null) UseGrid(bounds);
            Version++;
        }
    }
}
