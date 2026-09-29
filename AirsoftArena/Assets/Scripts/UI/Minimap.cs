using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    /// <summary>Pre-renders a small top-down picture of each map for the HUD minimap.</summary>
    public static class Minimap
    {
        public const int PixelsPerMetre = 3;
        static readonly Dictionary<string, Texture2D> cache = new Dictionary<string, Texture2D>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cache.Clear(); }

        public static Texture2D For(MapDefinition map)
        {
            Texture2D tex;
            if (cache.TryGetValue(map.id, out tex) && tex != null) return tex;

            var b = map.bounds;
            int w = Mathf.CeilToInt(b.width * PixelsPerMetre), h = Mathf.CeilToInt(b.height * PixelsPerMetre);
            tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color ground = map.ground == GroundStyle.Concrete ? new Color(0.36f, 0.36f, 0.35f, 0.9f)
                : map.ground == GroundStyle.ForestFloor ? new Color(0.17f, 0.25f, 0.14f, 0.9f) : new Color(0.24f, 0.36f, 0.19f, 0.9f);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) tex.SetPixel(x, y, ground);

            FillRect(tex, map, map.spawnZones[0], new Color(0.3f, 0.55f, 1f, 0.9f), true);
            FillRect(tex, map, map.spawnZones[1], new Color(1f, 0.33f, 0.28f, 0.9f), true);

            foreach (var p in map.pieces)
            {
                var r = new Rect(p.center.x - p.size.x / 2f, p.center.y - p.size.y / 2f, p.size.x, p.size.y);
                switch (p.kind)
                {
                    case PieceKind.Tree: FillCircle(tex, map, p.center, 1.2f, new Color(0.08f, 0.16f, 0.08f, 1f)); break;
                    case PieceKind.Bush: FillCircle(tex, map, p.center, p.size.x * 0.45f, new Color(0.3f, 0.5f, 0.24f, 1f)); break;
                    case PieceKind.Rock: FillCircle(tex, map, p.center, p.size.x * 0.45f, new Color(0.55f, 0.55f, 0.52f, 1f)); break;
                    case PieceKind.Sandbags:
                    case PieceKind.Crates:
                    case PieceKind.Log:
                        FillRect(tex, map, r, new Color(0.72f, 0.64f, 0.45f, 1f), false); break;
                    default:
                        FillRect(tex, map, r, new Color(0.1f, 0.1f, 0.1f, 1f), false); break;
                }
            }
            tex.Apply();
            cache[map.id] = tex;
            return tex;
        }

        /// <summary>Converts a world position to a 0..1 position on the minimap (y up).</summary>
        public static Vector2 Normalized(MapDefinition map, Vector2 world)
        {
            var b = map.bounds;
            return new Vector2((world.x - b.xMin) / b.width, (world.y - b.yMin) / b.height);
        }

        static void FillRect(Texture2D tex, MapDefinition map, Rect r, Color c, bool blend)
        {
            var b = map.bounds;
            int x0 = Mathf.FloorToInt((r.xMin - b.xMin) * PixelsPerMetre), x1 = Mathf.CeilToInt((r.xMax - b.xMin) * PixelsPerMetre);
            int y0 = Mathf.FloorToInt((r.yMin - b.yMin) * PixelsPerMetre), y1 = Mathf.CeilToInt((r.yMax - b.yMin) * PixelsPerMetre);
            for (int y = Mathf.Max(0, y0); y < Mathf.Min(tex.height, y1); y++)
                for (int x = Mathf.Max(0, x0); x < Mathf.Min(tex.width, x1); x++)
                {
                    if (blend)
                    {
                        var d = tex.GetPixel(x, y);
                        tex.SetPixel(x, y, new Color((d.r + c.r) / 2f, (d.g + c.g) / 2f, (d.b + c.b) / 2f, 0.9f));
                    }
                    else tex.SetPixel(x, y, c);
                }
        }

        static void FillCircle(Texture2D tex, MapDefinition map, Vector2 center, float radius, Color c)
        {
            var b = map.bounds;
            float cx = (center.x - b.xMin) * PixelsPerMetre, cy = (center.y - b.yMin) * PixelsPerMetre, r = radius * PixelsPerMetre;
            for (int y = Mathf.FloorToInt(cy - r); y <= Mathf.CeilToInt(cy + r); y++)
                for (int x = Mathf.FloorToInt(cx - r); x <= Mathf.CeilToInt(cx + r); x++)
                    if (x >= 0 && y >= 0 && x < tex.width && y < tex.height && (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r)
                        tex.SetPixel(x, y, c);
        }
    }
}
