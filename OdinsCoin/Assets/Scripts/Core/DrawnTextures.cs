using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>What a surface is made of, which decides the hand-drawn texture laid over its colour.</summary>
    public enum SurfaceKind { Plain, Cloth, Fur, Leather, Metal, Wood, Skin, Paper, Grass, Sand, Stone, Leaves }

    /// <summary>
    /// Hand-drawn looking textures, made in code: pencil strokes, weave, fur strands, wood grain. Each is a tileable
    /// greyscale multiplier (1 = the plain colour, lower = pencil/ink on top) so any colour can be drawn on. Pure
    /// maths and deterministic, so the preview renderer and the game draw exactly the same marks.
    /// </summary>
    public static class DrawnTextures
    {
        public const int Size = 128;

        static readonly Dictionary<SurfaceKind, float[]> cache = new Dictionary<SurfaceKind, float[]>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cache.Clear(); textures.Clear(); }

        /// <summary>The texture for a surface kind: <see cref="Size"/>² values in [0, 1], row-major, v = rows.</summary>
        public static float[] Get(SurfaceKind kind)
        {
            float[] t;
            if (cache.TryGetValue(kind, out t)) return t;
            t = Make(kind);
            cache[kind] = t;
            return t;
        }

        /// <summary>Bilinear, wrapping sample; u and v in texture repeats (1 = one tile).</summary>
        public static float Sample(float[] tex, float u, float v)
        {
            float x = (u - Mathf.Floor(u)) * Size - 0.5f, y = (v - Mathf.Floor(v)) * Size - 0.5f;
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            float a = At(tex, x0, y0), b = At(tex, x0 + 1, y0), c = At(tex, x0, y0 + 1), d = At(tex, x0 + 1, y0 + 1);
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        static float At(float[] tex, int x, int y) { return tex[Wrap(y) * Size + Wrap(x)]; }
        static int Wrap(int i) { i %= Size; return i < 0 ? i + Size : i; }

        static float[] Make(SurfaceKind kind)
        {
            var t = new float[Size * Size];
            for (int i = 0; i < t.Length; i++) t[i] = 1f;
            var rng = new System.Random(1000 + (int)kind * 7919);
            switch (kind)
            {
                case SurfaceKind.Cloth:
                    // Coarse wool: a faint weave, blotchy dye and loose pencil hatching.
                    for (int y = 0; y < Size; y++)
                        for (int x = 0; x < Size; x++)
                        {
                            float weave = 0.5f + 0.5f * Mathf.Sin(x * Mathf.PI * 2f * 32f / Size) * Mathf.Sin(y * Mathf.PI * 2f * 32f / Size);
                            t[y * Size + x] *= 1f - 0.05f * weave;
                        }
                    Mottle(t, rng, 8, 0.07f);
                    Hatch(t, rng, 55, 35f, 10f, 14f, 30f, 1.3f, 0.22f);
                    break;
                case SurfaceKind.Fur:
                    // Strands: lots of short strokes running down (v), a little wavy, darker at the roots.
                    Mottle(t, rng, 4, 0.1f);
                    Hatch(t, rng, 260, 90f, 16f, 8f, 18f, 1.3f, 0.34f);
                    // Darker tips: a second, sparser layer of short, strong strokes.
                    Hatch(t, rng, 90, 90f, 20f, 4f, 9f, 1.1f, 0.42f);
                    break;
                case SurfaceKind.Leather:
                    Mottle(t, rng, 8, 0.12f);
                    Mottle(t, rng, 16, 0.06f);
                    Hatch(t, rng, 26, 20f, 70f, 8f, 24f, 1.1f, 0.26f);
                    break;
                case SurfaceKind.Metal:
                    // Brushed streaks across and a few hard scratches.
                    for (int y = 0; y < Size; y++)
                    {
                        float s = 0.06f * (float)rng.NextDouble();
                        for (int x = 0; x < Size; x++) t[y * Size + x] *= 1f - s;
                    }
                    Hatch(t, rng, 16, 0f, 25f, 20f, 50f, 1.0f, 0.28f);
                    break;
                case SurfaceKind.Wood:
                    // Grain running along v, wavy, with a knot or two.
                    for (int y = 0; y < Size; y++)
                        for (int x = 0; x < Size; x++)
                        {
                            float wave = 3f * Mathf.Sin(y * Mathf.PI * 2f * 2f / Size + x * 0.05f);
                            float g = Mathf.Sin((x + wave) * Mathf.PI * 2f * 12f / Size);
                            t[y * Size + x] *= 1f - 0.16f * Mathf.Clamp01(g * 3f - 2f);
                        }
                    Mottle(t, rng, 4, 0.08f);
                    Hatch(t, rng, 30, 90f, 4f, 16f, 40f, 1.0f, 0.2f);
                    break;
                case SurfaceKind.Skin:
                    Mottle(t, rng, 8, 0.035f);
                    break;
                case SurfaceKind.Grass:
                    // Meadow: patchy tone and scattered pencil tufts, little upright ticks in twos and threes.
                    Mottle(t, rng, 4, 0.1f);
                    Mottle(t, rng, 16, 0.06f);
                    for (int k = 0; k < 150; k++)
                    {
                        float cx = (float)rng.NextDouble() * Size, cy = (float)rng.NextDouble() * Size;
                        int blades = 2 + rng.Next(2);
                        for (int b = 0; b < blades; b++)
                        {
                            float lean = (b - (blades - 1) * 0.5f) * 0.45f + ((float)rng.NextDouble() - 0.5f) * 0.3f;
                            float len = 4f + (float)rng.NextDouble() * 4f;
                            for (int i = 0; i <= 8; i++)
                            {
                                float f = i / 8f;
                                Dab(t, cx + lean * len * f, cy + len * f, 0.9f, 0.4f * (1f - f * 0.6f));
                            }
                        }
                    }
                    break;
                case SurfaceKind.Sand:
                    // Beach: soft blotches and a stipple of pencil dots, a few wind ripples.
                    Mottle(t, rng, 8, 0.08f);
                    for (int k = 0; k < 420; k++) Dab(t, (float)rng.NextDouble() * Size, (float)rng.NextDouble() * Size, 0.7f + (float)rng.NextDouble() * 0.5f, 0.12f + (float)rng.NextDouble() * 0.12f);
                    Hatch(t, rng, 10, 0f, 8f, 25f, 60f, 0.9f, 0.1f);
                    break;
                case SurfaceKind.Stone:
                    // Bare rock: heavy blotches, crossing cracks and short chisel strokes.
                    Mottle(t, rng, 4, 0.14f);
                    Mottle(t, rng, 16, 0.08f);
                    Hatch(t, rng, 12, 45f, 35f, 25f, 55f, 0.9f, 0.3f);
                    Hatch(t, rng, 40, 35f, 15f, 6f, 14f, 1.0f, 0.2f);
                    break;
                case SurfaceKind.Leaves:
                    // Foliage: patchy light and shade, and scalloped pencil arcs, the storybook way of drawing leaves.
                    Mottle(t, rng, 4, 0.12f);
                    Mottle(t, rng, 16, 0.06f);
                    for (int k = 0; k < 170; k++)
                    {
                        float cx = (float)rng.NextDouble() * Size, cy = (float)rng.NextDouble() * Size;
                        float r = 2.5f + (float)rng.NextDouble() * 3f, start = Mathf.PI * (1.1f + (float)rng.NextDouble() * 0.3f);
                        float dark = 0.22f + (float)rng.NextDouble() * 0.14f;
                        for (int i = 0; i <= 10; i++)
                        {
                            float a = start + i / 10f * Mathf.PI * 0.8f;
                            Dab(t, cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r, 0.75f, dark * (0.5f + 0.5f * Mathf.Sin(i / 10f * Mathf.PI)));
                        }
                    }
                    break;
                case SurfaceKind.Paper:
                    Mottle(t, rng, 4, 0.12f);
                    Mottle(t, rng, 16, 0.05f);
                    Hatch(t, rng, 22, 0f, 180f, 8f, 25f, 1.0f, 0.15f);
                    break;
                default:
                    Mottle(t, rng, 8, 0.03f);
                    break;
            }
            for (int i = 0; i < t.Length; i++) t[i] = Mathf.Clamp01(t[i]);
            return t;
        }

        /// <summary>Soft blotches: tileable value noise with <paramref name="cells"/> cells per side.</summary>
        static void Mottle(float[] t, System.Random rng, int cells, float strength)
        {
            var grid = new float[cells * cells];
            for (int i = 0; i < grid.Length; i++) grid[i] = (float)rng.NextDouble();
            float step = Size / (float)cells;
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float gx = x / step, gy = y / step;
                    int ix = (int)gx, iy = (int)gy;
                    float fx = Smooth(gx - ix), fy = Smooth(gy - iy);
                    float a = grid[(iy % cells) * cells + ix % cells], b = grid[(iy % cells) * cells + (ix + 1) % cells];
                    float c = grid[((iy + 1) % cells) * cells + ix % cells], d = grid[((iy + 1) % cells) * cells + (ix + 1) % cells];
                    float n = Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
                    t[y * Size + x] *= 1f - strength * n;
                }
        }

        static float Smooth(float f) { return f * f * (3f - 2f * f); }

        /// <summary>
        /// Pencil strokes: <paramref name="count"/> lines at <paramref name="angle"/>° (0 = along u, 90 = along v)
        /// ± <paramref name="spread"/>, <paramref name="minLen"/>–<paramref name="maxLen"/> pixels long, about
        /// <paramref name="width"/> wide, each darkening by up to <paramref name="dark"/> and tapering at both ends.
        /// Everything wraps, so the tile has no seams.
        /// </summary>
        static void Hatch(float[] t, System.Random rng, int count, float angle, float spread, float minLen, float maxLen, float width, float dark)
        {
            for (int s = 0; s < count; s++)
            {
                float ax = (float)rng.NextDouble() * Size, ay = (float)rng.NextDouble() * Size;
                float ang = (angle + ((float)rng.NextDouble() * 2f - 1f) * spread) * Mathf.Deg2Rad;
                float len = minLen + (float)rng.NextDouble() * (maxLen - minLen);
                float strength = dark * (0.5f + 0.5f * (float)rng.NextDouble());
                float bend = ((float)rng.NextDouble() * 2f - 1f) * 0.15f;
                float dx = Mathf.Cos(ang), dy = Mathf.Sin(ang);
                int steps = Mathf.CeilToInt(len * 2f);
                for (int i = 0; i <= steps; i++)
                {
                    float f = i / (float)steps;
                    float along = f * len, side = bend * len * f * (1f - f) * 4f;
                    float px = ax + dx * along - dy * side, py = ay + dy * along + dx * side;
                    // Pressure: light at the ends, firm in the middle.
                    float pressure = Mathf.Sin(f * Mathf.PI) * 0.7f + 0.3f;
                    Dab(t, px, py, width, strength * pressure);
                }
            }
        }

        static void Dab(float[] t, float px, float py, float radius, float strength)
        {
            int r = Mathf.CeilToInt(radius);
            int cx = Mathf.FloorToInt(px), cy = Mathf.FloorToInt(py);
            for (int y = cy - r; y <= cy + r; y++)
                for (int x = cx - r; x <= cx + r; x++)
                {
                    float d = Mathf.Sqrt((x + 0.5f - px) * (x + 0.5f - px) + (y + 0.5f - py) * (y + 0.5f - py));
                    if (d > radius) continue;
                    int i = Wrap(y) * Size + Wrap(x);
                    // Several dabs on one pixel build up like graphite, but never beyond the stroke's own darkness.
                    float target = 1f - strength * (1f - d / radius * 0.5f);
                    if (t[i] > target) t[i] = Mathf.Lerp(t[i], target, 0.8f);
                }
        }

        /// <summary>A Unity texture of a surface kind, made once and shared; repeats so uvs can tile.</summary>
        public static Texture2D Texture(SurfaceKind kind)
        {
            Texture2D tex;
            if (textures.TryGetValue(kind, out tex) && tex != null) return tex;
            var t = Get(kind);
            tex = new Texture2D(Size, Size, TextureFormat.RGBA32, true);
            tex.wrapMode = TextureWrapMode.Repeat;
            tex.filterMode = FilterMode.Bilinear;
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float v = t[y * Size + x];
                    tex.SetPixel(x, y, new Color(v, v, v, 1f));
                }
            tex.Apply();
            textures[kind] = tex;
            return tex;
        }

        static readonly Dictionary<SurfaceKind, Texture2D> textures = new Dictionary<SurfaceKind, Texture2D>();
    }
}
