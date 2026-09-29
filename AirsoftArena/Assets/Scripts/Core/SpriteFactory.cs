using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    /// <summary>
    /// Generates all placeholder pixel art at runtime so the prototype needs no imported assets.
    /// Replace these with real sprites later; nothing else depends on how they are made.
    /// </summary>
    public static class SpriteFactory
    {
        public const int PixelsPerUnit = 32;
        /// <summary>Ground and wall tiles are still authored at 16 px per metre.</summary>
        public const int TilePixelsPerUnit = 16;

        static Sprite pixel, circle, smallCircle, bb, grass, crate, sandbag, concrete, forestFloor, shelf, log, canopy, bush, rock, ring, disc64;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { pixel = circle = smallCircle = bb = grass = crate = sandbag = concrete = forestFloor = shelf = log = canopy = bush = rock = ring = disc64 = null; }

        /// <summary>1x1 white square, one world unit big. Scale and tint it for rectangles.</summary>
        public static Sprite Pixel { get { return pixel ?? (pixel = Make(Solid(1, 1, Color.white), 1)); } }

        /// <summary>White disc with a darker rim, 12 px across (0.75 m).</summary>
        public static Sprite Circle { get { return circle ?? (circle = Make(Disc(24, true), PixelsPerUnit)); } }

        public static Sprite SmallCircle { get { return smallCircle ?? (smallCircle = Make(Disc(12, false), PixelsPerUnit)); } }

        /// <summary>A BB is 6 mm; drawn at 2 px so it is actually visible.</summary>
        public static Sprite BB { get { return bb ?? (bb = Make(RoundBB(), PixelsPerUnit)); } }

        public static Sprite Grass { get { return grass ?? (grass = Make(GrassTile(), TilePixelsPerUnit)); } }
        public static Sprite Crate { get { return crate ?? (crate = Make(CrateTile(), TilePixelsPerUnit)); } }
        public static Sprite Sandbag { get { return sandbag ?? (sandbag = Make(SandbagTile(), TilePixelsPerUnit)); } }
        /// <summary>White ring, 4 m across. Scale it for zones.</summary>
        public static Sprite Ring { get { return ring ?? (ring = Make(RingTexture(128, 5f), PixelsPerUnit)); } }
        /// <summary>Filled white disc, 4 m across.</summary>
        public static Sprite Disc64 { get { return disc64 ?? (disc64 = Make(RingTexture(128, 128f), PixelsPerUnit)); } }
        public static Sprite Concrete { get { return concrete ?? (concrete = Make(ConcreteTile(), TilePixelsPerUnit)); } }
        public static Sprite ForestFloor { get { return forestFloor ?? (forestFloor = Make(ForestTile(), TilePixelsPerUnit)); } }
        public static Sprite Shelf { get { return shelf ?? (shelf = Make(ShelfTile(), TilePixelsPerUnit)); } }
        public static Sprite Log { get { return log ?? (log = Make(LogTile(), TilePixelsPerUnit)); } }
        /// <summary>Tree crown, 3 m across, drawn above players.</summary>
        public static Sprite Canopy { get { return canopy ?? (canopy = Make(Blob(96, 11, new Color32(38, 72, 40, 255), new Color32(52, 94, 50, 255), new Color32(30, 58, 34, 255)), PixelsPerUnit)); } }
        public static Sprite Bush { get { return bush ?? (bush = Make(Blob(64, 21, new Color32(64, 110, 52, 255), new Color32(82, 132, 62, 255), new Color32(50, 90, 44, 255)), PixelsPerUnit)); } }
        public static Sprite Rock { get { return rock ?? (rock = Make(Blob(48, 5, new Color32(128, 128, 122, 255), new Color32(150, 150, 144, 255), new Color32(100, 100, 96, 255)), PixelsPerUnit)); } }

        static Sprite Make(Texture2D tex, int ppu)
        {
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            // FullRect so the sprite works with SpriteDrawMode.Tiled.
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
        }

        /// <summary>A 4 px round BB (about 12 cm on screen, so you can follow it).</summary>
        static Texture2D RoundBB()
        {
            var tex = Solid(4, 4, Color.white);
            tex.SetPixel(0, 0, Color.clear); tex.SetPixel(3, 0, Color.clear); tex.SetPixel(0, 3, Color.clear); tex.SetPixel(3, 3, Color.clear);
            tex.SetPixel(1, 2, new Color(1f, 1f, 1f, 1f));
            tex.SetPixel(2, 1, new Color(0.8f, 0.8f, 0.8f, 1f));
            return tex;
        }

        static Texture2D Solid(int w, int h, Color c)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, c);
            return tex;
        }

        static Texture2D Disc(int size, bool rim)
        {
            var tex = Solid(size, size, Color.clear);
            float r = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = new Vector2(x + 0.5f - r, y + 0.5f - r).magnitude;
                    if (d > r) continue;
                    bool edge = rim && d > r - 1.2f;
                    tex.SetPixel(x, y, edge ? new Color(0.35f, 0.35f, 0.35f, 1f) : Color.white);
                }
            }
            return tex;
        }

        static Texture2D GrassTile()
        {
            var rng = new System.Random(7);
            var tex = Solid(16, 16, Color.white);
            Color a = new Color32(74, 112, 58, 255), b = new Color32(82, 122, 62, 255), c = new Color32(64, 98, 52, 255);
            for (int y = 0; y < 16; y++)
            {
                for (int x = 0; x < 16; x++)
                {
                    int n = rng.Next(100);
                    tex.SetPixel(x, y, n < 55 ? a : n < 88 ? b : c);
                }
            }
            // A few lighter blades so the tiling is less obvious.
            for (int i = 0; i < 5; i++)
            {
                int x = rng.Next(16), y = rng.Next(15);
                tex.SetPixel(x, y, new Color32(104, 146, 76, 255));
                tex.SetPixel(x, y + 1, new Color32(96, 136, 70, 255));
            }
            return tex;
        }

        static Texture2D RingTexture(int size, float thickness)
        {
            var tex = Solid(size, size, Color.clear);
            float r = size / 2f;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float d = new Vector2(x + 0.5f - r, y + 0.5f - r).magnitude;
                    if (d <= r && d >= r - thickness) tex.SetPixel(x, y, Color.white);
                }
            return tex;
        }

        static Texture2D ConcreteTile()
        {
            var rng = new System.Random(11);
            var tex = Solid(16, 16, Color.white);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    int n = rng.Next(100);
                    byte v = (byte)(n < 70 ? 122 : n < 90 ? 128 : 114);
                    tex.SetPixel(x, y, new Color32(v, v, (byte)(v - 4), 255));
                }
            // Expansion joints along two edges so the tiles read as floor slabs.
            for (int i = 0; i < 16; i++) { tex.SetPixel(i, 0, new Color32(96, 96, 94, 255)); tex.SetPixel(0, i, new Color32(96, 96, 94, 255)); }
            tex.SetPixel(9, 5, new Color32(104, 104, 100, 255)); tex.SetPixel(10, 6, new Color32(104, 104, 100, 255));
            return tex;
        }

        static Texture2D ForestTile()
        {
            var rng = new System.Random(5);
            var tex = Solid(16, 16, Color.white);
            Color a = new Color32(52, 78, 44, 255), b = new Color32(60, 88, 48, 255), c = new Color32(46, 68, 40, 255);
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                {
                    int n = rng.Next(100);
                    tex.SetPixel(x, y, n < 50 ? a : n < 85 ? b : c);
                }
            // Pine needles and a few brown leaves.
            for (int i = 0; i < 6; i++) tex.SetPixel(rng.Next(16), rng.Next(16), new Color32(112, 86, 52, 255));
            for (int i = 0; i < 4; i++) tex.SetPixel(rng.Next(16), rng.Next(16), new Color32(84, 62, 40, 255));
            return tex;
        }

        static Texture2D ShelfTile()
        {
            var tex = Solid(16, 16, new Color(0.3f, 0.32f, 0.36f));
            var beam = new Color(0.85f, 0.5f, 0.15f);
            for (int x = 0; x < 16; x++) { tex.SetPixel(x, 0, beam); tex.SetPixel(x, 15, beam); }
            // Boxes on the shelf.
            Color box = new Color32(176, 140, 96, 255), tape = new Color32(206, 186, 140, 255);
            for (int bx = 1; bx < 15; bx += 7)
                for (int y = 2; y < 14; y++)
                    for (int x = bx; x < bx + 6 && x < 15; x++)
                        tex.SetPixel(x, y, y == 8 ? tape : box);
            return tex;
        }

        static Texture2D LogTile()
        {
            var tex = Solid(16, 16, new Color32(104, 74, 46, 255));
            for (int x = 0; x < 16; x++)
            {
                tex.SetPixel(x, 0, new Color32(70, 48, 30, 255));
                tex.SetPixel(x, 15, new Color32(70, 48, 30, 255));
                if (x % 5 == 1) for (int y = 4; y < 12; y += 3) tex.SetPixel(x, y, new Color32(84, 58, 36, 255));
                tex.SetPixel(x, 13, new Color32(128, 94, 60, 255));
            }
            return tex;
        }

        /// <summary>Round, lumpy blob with speckles: crowns, bushes and rocks.</summary>
        static Texture2D Blob(int size, int seed, Color mid, Color light, Color dark)
        {
            var rng = new System.Random(seed);
            var tex = Solid(size, size, Color.clear);
            float r = size / 2f;
            var bumps = new Vector2[7];
            for (int i = 0; i < bumps.Length; i++)
            {
                float a = i / (float)bumps.Length * Mathf.PI * 2f;
                bumps[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r * 0.55f;
            }
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    var p = new Vector2(x + 0.5f - r, y + 0.5f - r);
                    bool inside = p.magnitude < r * 0.62f;
                    foreach (var b in bumps) if ((p - b).magnitude < r * 0.42f) inside = true;
                    if (!inside) continue;
                    int n = rng.Next(100);
                    Color c = p.y > r * 0.15f ? (n < 60 ? light : mid) : (n < 60 ? mid : dark);
                    if (n > 94) c = dark;
                    tex.SetPixel(x, y, c);
                }
            // Dark outline.
            var outline = dark * 0.7f;
            outline.a = 1f;
            var edge = new List<Vector2Int>();
            for (int y = 1; y < size - 1; y++)
                for (int x = 1; x < size - 1; x++)
                {
                    if (tex.GetPixel(x, y).a > 0f) continue;
                    if (tex.GetPixel(x + 1, y).a > 0f || tex.GetPixel(x - 1, y).a > 0f || tex.GetPixel(x, y + 1).a > 0f || tex.GetPixel(x, y - 1).a > 0f)
                        edge.Add(new Vector2Int(x, y));
                }
            foreach (var e in edge) tex.SetPixel(e.x, e.y, outline);
            return tex;
        }

        static Texture2D CrateTile()
        {
            var tex = Solid(16, 16, new Color(0.85f, 0.85f, 0.85f));
            Color line = new Color(0.55f, 0.55f, 0.55f), edge = new Color(0.4f, 0.4f, 0.4f);
            for (int x = 0; x < 16; x++)
            {
                tex.SetPixel(x, 5, line);
                tex.SetPixel(x, 10, line);
                tex.SetPixel(x, 0, edge);
                tex.SetPixel(x, 15, new Color(1f, 1f, 1f));
            }
            for (int y = 0; y < 16; y++)
            {
                tex.SetPixel(0, y, edge);
                tex.SetPixel(15, y, edge);
            }
            tex.SetPixel(3, 2, line); tex.SetPixel(12, 7, line); tex.SetPixel(6, 13, line);
            return tex;
        }

        static Texture2D SandbagTile()
        {
            var tex = Solid(16, 16, new Color(0.45f, 0.45f, 0.45f));
            // Two rows of bags, offset like bricks.
            for (int row = 0; row < 2; row++)
            {
                int y0 = row * 8, shift = row == 0 ? 0 : 4;
                for (int bag = -1; bag < 2; bag++)
                {
                    int x0 = bag * 8 + shift;
                    for (int y = 1; y < 7; y++)
                    {
                        for (int x = 1; x < 8; x++)
                        {
                            int px = x0 + x;
                            if (px < 0 || px > 15) continue;
                            bool corner = (x == 1 || x == 7) && (y == 1 || y == 6);
                            if (corner) continue;
                            float shade = y > 4 ? 1f : y < 3 ? 0.78f : 0.9f;
                            tex.SetPixel(px, y0 + y, new Color(shade, shade, shade));
                        }
                    }
                }
            }
            return tex;
        }
    }
}
