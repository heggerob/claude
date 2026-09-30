using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The rivers and sounds the Viking ships really sailed that are too narrow for the map's 200 m grid: the Thames
    /// up to Lundenwic, the Humber and the Ouse to Jorvik, the Schlei to Hedeby, Roskilde Fjord, and the water from Sigtuna into
    /// Lake Mälaren. Each is a line of points (lat, lon); within <see cref="HalfWidth"/> of it the ground is carved
    /// down to a navigable channel, deepest in the middle.
    /// </summary>
    public static class Channels
    {
        /// <summary>Half the width of a carved channel (m), and its depth in the middle (m).</summary>
        public const float HalfWidth = 140f, Depth = 5f;

        public static readonly Vector2[][] Routes =
        {
            // The Thames, from Lundenwic down to the estuary.
            new[] { new Vector2(51.508f, -0.10f), new Vector2(51.505f, -0.02f), new Vector2(51.495f, 0.03f), new Vector2(51.50f, 0.07f), new Vector2(51.485f, 0.13f),
                    new Vector2(51.47f, 0.22f), new Vector2(51.455f, 0.33f), new Vector2(51.46f, 0.45f), new Vector2(51.49f, 0.60f), new Vector2(51.50f, 0.80f) },
            // The Ouse from Jorvik down into the Humber and out to sea.
            new[] { new Vector2(53.955f, -1.08f), new Vector2(53.90f, -1.10f), new Vector2(53.83f, -1.08f), new Vector2(53.77f, -1.03f), new Vector2(53.72f, -0.95f),
                    new Vector2(53.70f, -0.85f), new Vector2(53.70f, -0.72f), new Vector2(53.72f, -0.55f), new Vector2(53.74f, -0.40f), new Vector2(53.70f, -0.20f),
                    new Vector2(53.62f, -0.05f), new Vector2(53.58f, 0.10f) },
            // The Schlei, from Hedeby out to the Baltic at Schleimünde.
            new[] { new Vector2(54.495f, 9.575f), new Vector2(54.505f, 9.62f), new Vector2(54.53f, 9.68f), new Vector2(54.55f, 9.74f), new Vector2(54.59f, 9.80f),
                    new Vector2(54.63f, 9.87f), new Vector2(54.66f, 9.93f), new Vector2(54.672f, 10.00f), new Vector2(54.675f, 10.06f) },
            // Tønsberg's harbour, east through Byfjorden to the Oslofjord.
            new[] { new Vector2(59.2665f, 10.4135f), new Vector2(59.262f, 10.44f), new Vector2(59.258f, 10.4835f), new Vector2(59.252f, 10.52f),
                    new Vector2(59.245f, 10.56f), new Vector2(59.24f, 10.60f) },
            // Roskilde Fjord, from the kings' seat north to the Kattegat.
            new[] { new Vector2(55.645f, 12.08f), new Vector2(55.67f, 12.07f), new Vector2(55.71f, 12.05f), new Vector2(55.76f, 12.03f), new Vector2(55.81f, 12.01f),
                    new Vector2(55.86f, 11.98f), new Vector2(55.91f, 11.95f), new Vector2(55.95f, 11.91f), new Vector2(55.975f, 11.86f), new Vector2(55.99f, 11.80f) },
            // From Sigtuna south through the bays into Lake Mälaren by Birka.
            new[] { new Vector2(59.614f, 17.72f), new Vector2(59.59f, 17.70f), new Vector2(59.56f, 17.66f), new Vector2(59.50f, 17.60f), new Vector2(59.45f, 17.58f),
                    new Vector2(59.38f, 17.55f), new Vector2(59.34f, 17.545f) },
        };

        struct Segment { public Vector2 a, b; public Rect box; }
        static WorldMap forMap;
        static float forScale;
        static List<Segment> segments;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { forMap = null; segments = null; }

        static void Build(WorldMap map)
        {
            forMap = map;
            forScale = WorldMap.Scale;
            segments = new List<Segment>();
            float pad = HalfWidth * WorldMap.Scale;
            foreach (var route in Routes)
                for (int i = 0; i + 1 < route.Length; i++)
                {
                    var a3 = map.ToWorld(route[i].x, route[i].y);
                    var b3 = map.ToWorld(route[i + 1].x, route[i + 1].y);
                    var a = new Vector2(a3.x, a3.z);
                    var b = new Vector2(b3.x, b3.z);
                    var box = Rect.MinMaxRect(Mathf.Min(a.x, b.x) - pad, Mathf.Min(a.y, b.y) - pad, Mathf.Max(a.x, b.x) + pad, Mathf.Max(a.y, b.y) + pad);
                    segments.Add(new Segment { a = a, b = b, box = box });
                }
        }

        /// <summary>The ground at a global point, carved down where a channel runs: -<see cref="Depth"/> in its middle, easing up to its banks.</summary>
        public static float Carve(WorldMap map, double x, double z, float height)
        {
            if (segments == null || forMap != map || forScale != WorldMap.Scale) Build(map);
            var p = new Vector2((float)x, (float)z);
            float s = WorldMap.Scale, half = HalfWidth * s;
            float best = float.MaxValue;
            foreach (var seg in segments)
            {
                if (!seg.box.Contains(p)) continue;
                var ab = seg.b - seg.a;
                float t = Mathf.Clamp01(Vector2.Dot(p - seg.a, ab) / Mathf.Max(1e-3f, ab.sqrMagnitude));
                best = Mathf.Min(best, Vector2.Distance(p, seg.a + ab * t));
            }
            if (best >= half) return height;
            float k = best / half;
            float bed = -Depth * s * (1f - k * k * k * k);
            // Only ever deepens: a real channel that's already deep stays as it is.
            return Mathf.Min(height, bed + Mathf.Max(0f, height) * k * k * k * k);
        }
    }
}
