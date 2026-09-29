using UnityEngine;

namespace AirsoftArena
{
    public enum MarkKind
    {
        /// <summary>Worn dirt path with gravel.</summary>
        Path,
        /// <summary>Muddy puddle (points[0] = centre, width = radius).</summary>
        Puddle,
        /// <summary>Painted floor line (warehouse).</summary>
        PaintLine,
        /// <summary>Oil stain on concrete (points[0] = centre, width = radius).</summary>
        Stain,
    }

    /// <summary>Something painted onto the ground: paths, puddles, floor markings.</summary>
    public class GroundMark
    {
        public MarkKind kind;
        public Vector2[] points;
        public float width;
        public Color color;

        Rect? area;

        /// <summary>World rectangle this mark can touch (cached), so painting can skip it quickly.</summary>
        public Rect Area
        {
            get
            {
                if (area.HasValue) return area.Value;
                float xMin = float.MaxValue, yMin = float.MaxValue, xMax = float.MinValue, yMax = float.MinValue;
                foreach (var p in points)
                {
                    xMin = Mathf.Min(xMin, p.x); yMin = Mathf.Min(yMin, p.y);
                    xMax = Mathf.Max(xMax, p.x); yMax = Mathf.Max(yMax, p.y);
                }
                float pad = width * 1.5f + 1f;
                area = new Rect(xMin - pad, yMin - pad, xMax - xMin + pad * 2f, yMax - yMin + pad * 2f);
                return area.Value;
            }
        }
    }

    /// <summary>
    /// Paints a unique ground texture for a map: patchy grass or floor, paths, puddles, pebbles, flowers,
    /// slab joints and floor markings. Pure pixel math (no Unity objects) so tools can render it too.
    /// </summary>
    public static class GroundPainter
    {
        public const int PixelsPerMetre = SpriteFactory.PixelsPerUnit;

        /// <summary>Paints a w x h pixel block whose bottom-left corner is at world (x0, y0).</summary>
        public static Color32[] Paint(MapDefinition map, float x0, float y0, int w, int h)
        {
            return Paint(map, x0, y0, w, h, PixelsPerMetre);
        }

        /// <summary>Same, at a custom resolution (the preview tools paint smaller overview images).</summary>
        public static Color32[] Paint(MapDefinition map, float x0, float y0, int w, int h, int pixelsPerMetre)
        {
            var pixels = new Color32[w * h];
            float step = 1f / pixelsPerMetre;
            int seed = map.id.GetHashCode() & 0xFFFF;
            for (int py = 0; py < h; py++)
            {
                for (int px = 0; px < w; px++)
                {
                    float x = x0 + (px + 0.5f) * step, y = y0 + (py + 0.5f) * step;
                    int gx = Mathf.FloorToInt(x * PixelsPerMetre), gy = Mathf.FloorToInt(y * PixelsPerMetre);
                    Color c = Base(map, x, y, gx, gy, seed);
                    c = ApplyMarks(map, c, x, y, gx, gy, seed);
                    // Outside the field: a bit darker so the play area reads clearly.
                    if (!map.bounds.Contains(new Vector2(x, y))) c *= map.indoor ? 0.35f : 0.8f;
                    c.a = 1f;
                    pixels[py * w + px] = c;
                }
            }
            return pixels;
        }

        static Color Base(MapDefinition map, float x, float y, int gx, int gy, int seed)
        {
            float patch = Noise.Fbm(x * 0.18f, y * 0.18f, 3, seed);
            float fine = Noise.Hash(gx, gy, seed);
            switch (map.ground)
            {
                case GroundStyle.Concrete:
                {
                    Color c = Color.Lerp(new Color32(112, 112, 108, 255), new Color32(132, 131, 126, 255), patch);
                    c *= 0.94f + fine * 0.1f;
                    // Slab joints every 4 m.
                    float fx = Mathf.Repeat(x, 4f), fy = Mathf.Repeat(y, 4f);
                    if (fx < 0.04f || fy < 0.04f) c *= 0.72f;
                    // Hairline cracks.
                    float crack = Noise.Value(x * 1.3f, y * 1.3f, seed + 7);
                    if (crack > 0.495f && crack < 0.505f && Noise.Value(x * 0.3f, y * 0.3f, seed + 9) > 0.6f) c *= 0.75f;
                    return c;
                }
                case GroundStyle.ForestFloor:
                {
                    Color moss = new Color32(52, 80, 42, 255), needles = new Color32(92, 74, 48, 255), fern = new Color32(66, 104, 50, 255);
                    Color c = Color.Lerp(moss, needles, Mathf.Clamp01((patch - 0.35f) * 2.2f));
                    c = Color.Lerp(c, fern, Mathf.Clamp01((Noise.Value(x * 0.5f, y * 0.5f, seed + 3) - 0.6f) * 3f));
                    c *= 0.9f + fine * 0.18f;
                    if (fine > 0.985f) c = new Color32(128, 98, 60, 255);           // pine cone / twig
                    else if (fine < 0.006f) c = new Color32(150, 150, 140, 255);    // pebble
                    return c;
                }
                default:
                {
                    Color dry = new Color32(96, 118, 60, 255), lush = new Color32(70, 108, 52, 255), deep = new Color32(58, 92, 46, 255);
                    Color c = Color.Lerp(lush, dry, Mathf.Clamp01((patch - 0.4f) * 2.5f));
                    c = Color.Lerp(c, deep, Mathf.Clamp01((Noise.Value(x * 0.7f, y * 0.7f, seed + 5) - 0.65f) * 3f));
                    // Blades: every pixel a slightly different green, some lighter tips.
                    c *= 0.88f + fine * 0.22f;
                    // Flowers and pebbles in small clusters.
                    float cluster = Noise.Value(x * 0.4f, y * 0.4f, seed + 11);
                    if (cluster > 0.7f && fine > 0.992f) c = fine > 0.996f ? new Color32(240, 220, 90, 255) : new Color32(235, 235, 240, 255);
                    else if (fine < 0.004f) c = new Color32(140, 138, 128, 255);
                    return c;
                }
            }
        }

        static Color ApplyMarks(MapDefinition map, Color c, float x, float y, int gx, int gy, int seed)
        {
            if (map.marks.Count == 0) return c;
            var p = new Vector2(x, y);
            float fine = Noise.Hash(gx, gy, seed + 21);
            foreach (var m in map.marks)
            {
                if (!m.Area.Contains(p)) continue;
                switch (m.kind)
                {
                    case MarkKind.Path:
                    {
                        // Wobbly edges so paths look worn in, not ruled.
                        float wobble = (Noise.Value(x * 0.8f, y * 0.8f, seed + 13) - 0.5f) * 0.6f;
                        float d = DistanceToPolyline(p, m.points) - (m.width / 2f + wobble);
                        if (d > 0.35f) break;
                        Color dirt = Color.Lerp(new Color32(128, 104, 72, 255), new Color32(148, 124, 88, 255), Noise.Value(x * 2f, y * 2f, seed + 17));
                        dirt *= 0.9f + fine * 0.2f;
                        if (fine > 0.94f) dirt = new Color32(170, 164, 150, 255); // gravel
                        c = d <= 0f ? dirt : Color.Lerp(dirt, c, d / 0.35f);
                        break;
                    }
                    case MarkKind.Puddle:
                    {
                        float r = m.width * (0.8f + 0.4f * Noise.Value(x * 1.1f, y * 1.1f, seed + 19));
                        float d = Vector2.Distance(p, m.points[0]);
                        if (d > r + 0.4f) break;
                        if (d <= r)
                        {
                            Color water = Color.Lerp(new Color32(64, 78, 82, 255), new Color32(96, 116, 124, 255), Noise.Value(x * 3f, y * 3f, seed + 23));
                            if (fine > 0.97f) water = new Color32(170, 190, 200, 255); // glint
                            c = water;
                        }
                        else c = Color.Lerp(new Color32(84, 66, 46, 255), c, (d - r) / 0.4f); // mud rim
                        break;
                    }
                    case MarkKind.PaintLine:
                    {
                        float d = DistanceToPolyline(p, m.points);
                        if (d > m.width / 2f) break;
                        Color paint = m.color;
                        // Worn paint: some pixels show the concrete through.
                        c = fine > 0.2f ? paint * (0.9f + fine * 0.1f) : Color.Lerp(paint, c, 0.6f);
                        break;
                    }
                    case MarkKind.Stain:
                    {
                        float r = m.width * (0.7f + 0.6f * Noise.Value(x * 1.5f, y * 1.5f, seed + 29));
                        if (Vector2.Distance(p, m.points[0]) < r) c *= 0.62f;
                        break;
                    }
                }
            }
            return c;
        }

        public static float DistanceToPolyline(Vector2 p, Vector2[] points)
        {
            float best = float.MaxValue;
            for (int i = 0; i + 1 < points.Length; i++)
            {
                Vector2 a = points[i], b = points[i + 1], ab = b - a;
                float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
                best = Mathf.Min(best, Vector2.Distance(p, a + ab * t));
            }
            return best;
        }
    }
}
