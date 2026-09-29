using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    /// <summary>
    /// 32 px-per-metre textures for everything placed on a map. Tiles repeat every metre (SpriteDrawMode.Tiled);
    /// round props (barrels, tyres) are single sprites. All generated from code.
    /// </summary>
    public static class MapArt
    {
        const int T = SpriteFactory.PixelsPerUnit; // 32 px per metre, one tile per metre
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cache.Clear(); }

        public static Sprite Plywood { get { return Get("plywood", PlywoodTile); } }
        public static Sprite IndoorWall { get { return Get("indoorwall", IndoorWallTile); } }
        public static Sprite Container { get { return Get("container", ContainerTile); } }
        public static Sprite Sandbags { get { return Get("sandbags", SandbagTile); } }
        public static Sprite Shelf { get { return Get("shelf", ShelfTile); } }
        public static Sprite Crates { get { return Get("crates", CrateTile); } }
        public static Sprite Log { get { return Get("log", LogTile); } }
        public static Sprite Hay { get { return Get("hay", HayTile); } }
        public static Sprite Fence { get { return Get("fence", FenceTile); } }
        public static Sprite Netting { get { return Get("netting", NettingTile); } }
        public static Sprite Pallet { get { return Get("pallet", PalletTile); } }
        public static Sprite Barrel { get { return Get("barrel", BarrelTexture); } }
        public static Sprite Tires { get { return Get("tires", TiresTexture); } }

        static Sprite Get(string key, System.Func<Texture2D> build)
        {
            Sprite s;
            if (cache.TryGetValue(key, out s) && s != null) return s;
            var tex = build();
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), T, 0, SpriteMeshType.FullRect);
            cache[key] = s;
            return s;
        }

        // ---------------------------------------------------------------- tiles (all 32x32 = 1 m)

        static Texture2D PlywoodTile()
        {
            var tex = New(T, T);
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    // Wood grain: stretched noise along x.
                    float grain = Noise.Value(x * 0.08f, y * 0.9f, 3) * 0.6f + Noise.Value(x * 0.3f, y * 2.5f, 4) * 0.4f;
                    Color c = Color.Lerp(new Color32(150, 112, 74, 255), new Color32(182, 140, 94, 255), grain);
                    if (y == 0 || y == T - 1) c = new Color32(98, 70, 44, 255);           // board edges
                    else if (y == 1) c *= 1.15f;                                        // top edge catches light
                    else if (x == 0) c *= 0.8f;                                          // sheet seam
                    tex.SetPixel(x, y, c);
                }
            Nail(tex, 3, 4); Nail(tex, 3, 27); Nail(tex, 28, 4); Nail(tex, 28, 27);
            return tex;
        }

        static Texture2D IndoorWallTile()
        {
            var tex = New(T, T);
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    // Painted concrete blocks.
                    bool mortar = y % 16 == 0 || (x + (y / 16) * 16) % 32 == 0;
                    float n = Noise.Hash(x, y, 8) * 0.08f;
                    Color c = mortar ? new Color32(128, 126, 120, 255) : new Color32(176, 174, 166, 255);
                    tex.SetPixel(x, y, c * (0.96f + n));
                }
            return tex;
        }

        static Texture2D ContainerTile()
        {
            var tex = New(T, T);
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    // Corrugated steel: ribs every 4 px, light on one flank, dark on the other.
                    int r = x % 4;
                    float shade = r == 0 ? 0.72f : r == 1 ? 1.05f : r == 2 ? 0.95f : 0.85f;
                    Color c = Gray(shade);
                    // Rust and dents.
                    float rust = Noise.Fbm(x * 0.15f, y * 0.15f, 3, 12);
                    if (rust > 0.68f) c = Color.Lerp(c, new Color(0.85f, 0.55f, 0.38f), (rust - 0.68f) * 3f);
                    if (y < 2 || y > T - 3) c = Gray(0.6f);                             // top and bottom rails
                    tex.SetPixel(x, y, c);
                }
            return tex;
        }

        static Texture2D SandbagTile()
        {
            var tex = New(T, T, new Color32(90, 82, 60, 255));
            // Two rows of bags, offset like bricks, each with a seam and stitching.
            for (int row = 0; row < 2; row++)
            {
                int y0 = row * 16, shift = row == 0 ? 0 : 8;
                for (int bag = -1; bag < 3; bag++)
                {
                    int x0 = bag * 16 + shift;
                    for (int y = 1; y < 15; y++)
                        for (int x = 1; x < 16; x++)
                        {
                            int px = x0 + x;
                            if (px < 0 || px >= T) continue;
                            float ex = (x - 8f) / 7.5f, ey = (y - 7.5f) / 7f;
                            if (ex * ex + ey * ey > 1.05f) continue;
                            float light = y > 9 ? 1f : y < 5 ? 0.8f : 0.9f;
                            float weave = Noise.Hash(px, y0 + y, 5) * 0.08f;
                            Color c = new Color(0.84f, 0.76f, 0.56f) * (light + weave);
                            if (x == 3 || x == 13) c *= 0.85f;                          // tied ends
                            if (y == 7 && x % 2 == 0 && x > 3 && x < 13) c *= 0.75f;    // stitching
                            tex.SetPixel(px, y0 + y, c);
                        }
                }
            }
            return tex;
        }

        static Texture2D ShelfTile()
        {
            var tex = New(T, T, new Color32(62, 66, 74, 255));
            Color beam = new Color32(224, 128, 36, 255), beamDark = new Color32(170, 92, 24, 255);
            for (int x = 0; x < T; x++) { tex.SetPixel(x, 0, beamDark); tex.SetPixel(x, 1, beam); tex.SetPixel(x, T - 2, beam); tex.SetPixel(x, T - 1, beamDark); }
            // Boxes of different sizes with tape and a white label.
            int[] starts = { 1, 13, 23 };
            int[] widths = { 11, 9, 8 };
            for (int b = 0; b < starts.Length; b++)
            {
                Color box = b == 1 ? new Color32(160, 124, 82, 255) : new Color32(184, 146, 100, 255);
                for (int y = 3; y < T - 3; y++)
                    for (int x = starts[b]; x < starts[b] + widths[b]; x++)
                    {
                        Color c = box;
                        if (x == starts[b] || y == 3) c *= 0.75f;
                        if (y == T / 2) c = new Color32(210, 190, 146, 255);           // tape
                        tex.SetPixel(x, y, c);
                    }
                for (int y = 8; y < 11; y++) for (int x = starts[b] + 2; x < starts[b] + 6; x++) tex.SetPixel(x, y, new Color32(232, 232, 226, 255));
            }
            return tex;
        }

        static Texture2D CrateTile()
        {
            var tex = New(T, T);
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    float grain = Noise.Value(x * 0.1f, y * 1.2f, 21);
                    Color c = Color.Lerp(new Color32(150, 108, 62, 255), new Color32(176, 132, 80, 255), grain);
                    if (y % 8 == 0) c *= 0.7f;                                          // plank gaps
                    tex.SetPixel(x, y, c);
                }
            // Frame and diagonal brace.
            Color frame = new Color32(112, 78, 44, 255);
            for (int i = 0; i < T; i++)
            {
                for (int k = 0; k < 3; k++)
                {
                    tex.SetPixel(i, k, frame); tex.SetPixel(i, T - 1 - k, frame);
                    tex.SetPixel(k, i, frame); tex.SetPixel(T - 1 - k, i, frame);
                    int d = Mathf.Clamp(i + k - 1, 0, T - 1);
                    tex.SetPixel(i, d, frame * 1.1f);
                }
            }
            Nail(tex, 2, 2); Nail(tex, 29, 2); Nail(tex, 2, 29); Nail(tex, 29, 29);
            return tex;
        }

        static Texture2D LogTile()
        {
            var tex = New(T, T);
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    // Round trunk: shaded across its width, bark ridges along it.
                    float across = Mathf.Abs(y - 15.5f) / 16f;
                    float ridge = Noise.Value(x * 0.25f, y * 1.4f, 31);
                    Color c = Color.Lerp(new Color32(118, 84, 52, 255), new Color32(80, 56, 34, 255), across * 0.9f + ridge * 0.3f);
                    if (across > 0.9f) c *= 0.7f;
                    if (Noise.Hash(x, y, 33) > 0.97f) c = new Color32(88, 110, 58, 255);  // moss
                    tex.SetPixel(x, y, c);
                }
            return tex;
        }

        static Texture2D HayTile()
        {
            var tex = New(T, T);
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    float straw = Noise.Value(x * 0.2f, y * 1.6f, 41);
                    Color c = Color.Lerp(new Color32(196, 164, 84, 255), new Color32(226, 196, 110, 255), straw);
                    c *= 0.92f + Noise.Hash(x, y, 43) * 0.16f;
                    if (x == 10 || x == 22) c = new Color32(120, 96, 60, 255);           // twine
                    if (y == 0 || y == T - 1) c *= 0.8f;
                    tex.SetPixel(x, y, c);
                }
            return tex;
        }

        static Texture2D FenceTile()
        {
            var tex = New(T, T, Color.clear);
            Color rail = new Color32(122, 90, 58, 255), railDark = new Color32(88, 64, 40, 255), post = new Color32(70, 50, 32, 255);
            for (int x = 0; x < T; x++)
            {
                for (int y = 6; y <= 10; y++) tex.SetPixel(x, y, y == 6 ? railDark : rail);
                for (int y = 20; y <= 24; y++) tex.SetPixel(x, y, y == 20 ? railDark : rail);
            }
            for (int y = 2; y < T - 2; y++) for (int x = 0; x < 5; x++) tex.SetPixel(x, y, x == 4 ? railDark : post);
            return tex;
        }

        static Texture2D NettingTile()
        {
            var tex = New(T, T, Color.clear);
            Color net = new Color(0.1f, 0.12f, 0.1f, 0.55f), post = new Color32(60, 60, 58, 255);
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                    if ((x + y) % 6 == 0 || (x - y + 64) % 6 == 0) tex.SetPixel(x, y, net);
            for (int y = 0; y < T; y++) for (int x = 14; x < 18; x++) tex.SetPixel(x, y, Color.clear);
            for (int y = 10; y < 22; y++) for (int x = 13; x < 19; x++) tex.SetPixel(x, y, post);
            return tex;
        }

        static Texture2D PalletTile()
        {
            var tex = New(T, T, Color.clear);
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    bool slat = (x % 8) < 6;
                    bool stringer = y < 4 || y > T - 5 || (y > 13 && y < 18);
                    if (!slat && !stringer) continue;
                    Color c = Color.Lerp(new Color32(170, 136, 90, 255), new Color32(196, 160, 110, 255), Noise.Value(x * 0.1f, y * 0.8f, 51));
                    if (stringer && !slat) c *= 0.75f;
                    tex.SetPixel(x, y, c);
                }
            return tex;
        }

        // ---------------------------------------------------------------- round props

        static Texture2D BarrelTexture()
        {
            var tex = New(T, T, Color.clear);
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    float d = new Vector2(x - 15.5f, y - 15.5f).magnitude;
                    if (d > 15f) continue;
                    // Lid seen from above: rim, two rolling rings, bung hole. Grayscale, tinted per barrel.
                    float light = Mathf.Clamp01(0.5f + ((y - 15.5f) - (x - 15.5f)) / 40f);
                    float shade = d > 13.5f ? 0.55f : (d > 11f && d < 12f) ? 0.7f : Mathf.Lerp(0.8f, 1f, light);
                    tex.SetPixel(x, y, Gray(shade));
                }
            for (int y = 19; y < 22; y++) for (int x = 19; x < 22; x++) tex.SetPixel(x, y, Gray(0.35f));
            return tex;
        }

        static Texture2D TiresTexture()
        {
            var tex = New(T, T, Color.clear);
            for (int y = 0; y < T; y++)
                for (int x = 0; x < T; x++)
                {
                    float d = new Vector2(x - 15.5f, y - 15.5f).magnitude;
                    if (d > 15.5f || d < 6f) continue;
                    // Tread blocks around the outside, sidewall inside.
                    float ang = Mathf.Atan2(y - 15.5f, x - 15.5f);
                    bool tread = d > 12.5f && Mathf.Repeat(ang * 8f, 1f) < 0.5f;
                    Color c = tread ? new Color32(46, 46, 48, 255) : new Color32(30, 30, 32, 255);
                    if (d < 7.5f) c = new Color32(20, 20, 22, 255);
                    if (d > 9f && d < 10f) c = new Color32(56, 56, 58, 255);
                    tex.SetPixel(x, y, c);
                }
            return tex;
        }

        // ---------------------------------------------------------------- helpers

        static void Nail(Texture2D tex, int x, int y)
        {
            tex.SetPixel(x, y, new Color32(70, 70, 72, 255));
            tex.SetPixel(x + 1, y + 1, new Color32(150, 150, 150, 255));
        }

        static Color Gray(float v) { return new Color(v, v, v, 1f); }

        static Texture2D New(int w, int h) { return New(w, h, Color.white); }

        static Texture2D New(int w, int h, Color fill)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) tex.SetPixel(x, y, fill);
            return tex;
        }
    }
}
