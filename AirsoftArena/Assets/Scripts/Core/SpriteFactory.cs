using UnityEngine;

namespace AirsoftArena
{
    /// <summary>
    /// Generates all placeholder pixel art at runtime so the prototype needs no imported assets.
    /// Replace these with real sprites later; nothing else depends on how they are made.
    /// </summary>
    public static class SpriteFactory
    {
        public const int PixelsPerUnit = 16;

        static Sprite pixel, circle, smallCircle, bb, grass, crate, sandbag;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { pixel = circle = smallCircle = bb = grass = crate = sandbag = null; }

        /// <summary>1x1 white square, one world unit big. Scale and tint it for rectangles.</summary>
        public static Sprite Pixel { get { return pixel ?? (pixel = Make(Solid(1, 1, Color.white), 1)); } }

        /// <summary>White disc with a darker rim, 12 px across (0.75 m).</summary>
        public static Sprite Circle { get { return circle ?? (circle = Make(Disc(12, true), PixelsPerUnit)); } }

        public static Sprite SmallCircle { get { return smallCircle ?? (smallCircle = Make(Disc(6, false), PixelsPerUnit)); } }

        /// <summary>A BB is 6 mm; drawn at 2 px so it is actually visible.</summary>
        public static Sprite BB { get { return bb ?? (bb = Make(Solid(2, 2, Color.white), PixelsPerUnit)); } }

        public static Sprite Grass { get { return grass ?? (grass = Make(GrassTile(), PixelsPerUnit)); } }
        public static Sprite Crate { get { return crate ?? (crate = Make(CrateTile(), PixelsPerUnit)); } }
        public static Sprite Sandbag { get { return sandbag ?? (sandbag = Make(SandbagTile(), PixelsPerUnit)); } }

        static Sprite Make(Texture2D tex, int ppu)
        {
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            // FullRect so the sprite works with SpriteDrawMode.Tiled.
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
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
