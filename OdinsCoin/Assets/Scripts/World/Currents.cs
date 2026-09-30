using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The water itself moves: the great tidal races of the North run hard and turn with the tide (Saltstraumen at
    /// up to ten knots, the Maelstrom off Lofoten, the Pentland Firth, the Corryvreckan), and the Norwegian Coastal
    /// Current sets steadily north along the coast. A ship moves through the water, the water over the ground; the
    /// physics works with both.
    /// </summary>
    public static class Currents
    {
        /// <summary>A tidal race: where (lat, lon), how far it reaches (m), how hard it runs at springs (m/s), and the bearing it floods towards.</summary>
        public struct Race { public string name; public float lat, lon, radius, peak, flood; }

        public static readonly Race[] Races =
        {
            new Race { name = "Saltstraumen", lat = 67.235f, lon = 14.62f, radius = 2500f, peak = 5f, flood = 70f },
            new Race { name = "the Maelstrom", lat = 67.80f, lon = 12.75f, radius = 5000f, peak = 3f, flood = 0f },
            new Race { name = "the Pentland Firth", lat = 58.72f, lon = -3.1f, radius = 7000f, peak = 4.5f, flood = 90f },
            new Race { name = "the Corryvreckan", lat = 56.15f, lon = -5.72f, radius = 2500f, peak = 4f, flood = 90f },
        };

        /// <summary>The tide's period (h): the moon's half day.</summary>
        public const float TidePeriod = 12.42f;
        /// <summary>The Norwegian Coastal Current's set (m/s) and bearing.</summary>
        public const float CoastalSpeed = 0.3f, CoastalBearing = 20f;

        /// <summary>How the tide stands: +1 full flood, -1 full ebb, 0 slack, at a time (hours since the voyage began).</summary>
        public static float Tide(double hours) { return Mathf.Sin((float)(2.0 * System.Math.PI * (hours % TidePeriod) / TidePeriod)); }

        /// <summary>The water's velocity over the ground (m/s, x east, z north) at a global position and time (h).</summary>
        public static Vector2 At(WorldMap map, Vector3 global, double hours)
        {
            if (map == null) return Vector2.zero;
            var flow = Vector2.zero;
            float tide = Tide(hours);
            foreach (var r in Races)
            {
                var c = map.ToWorld(r.lat, r.lon);
                float d = Vector2.Distance(new Vector2(c.x, c.z), new Vector2(global.x, global.z)) / WorldMap.Scale;
                if (d >= r.radius) continue;
                float k = 1f - d / r.radius;
                float a = r.flood * Mathf.Deg2Rad;
                flow += new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * r.peak * tide * k * k;
            }
            // The coastal current, off the Norwegian coast between Stavanger and the North Cape, in water that isn't open ocean.
            var ll = map.ToLatLon(global);
            if (ll.x > 58.5f && ll.x < 71f && ll.y > 4f && ll.y < 26f)
            {
                float depth = -TerrainDetail.Height(map, global.x, global.z) / WorldMap.Scale;
                if (depth > 5f && depth < 500f)
                {
                    float a = CoastalBearing * Mathf.Deg2Rad;
                    flow += new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * CoastalSpeed;
                }
            }
            return flow;
        }

        /// <summary>The time for the currents: hours since the voyage began, by the sky clock.</summary>
        public static double Now { get { return SkyClock.Day * 24.0 + SkyClock.Hours; } }
    }
}
