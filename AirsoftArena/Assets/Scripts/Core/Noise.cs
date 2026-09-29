using UnityEngine;

namespace AirsoftArena
{
    /// <summary>Deterministic value noise for procedural textures (same result on every machine).</summary>
    public static class Noise
    {
        /// <summary>Hash of an integer cell to 0..1.</summary>
        public static float Hash(int x, int y, int seed = 0)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393 + y * 668265263 + seed * 144665);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return (h & 0xFFFFFF) / (float)0x1000000;
            }
        }

        /// <summary>Smooth value noise, 0..1, one feature per unit.</summary>
        public static float Value(float x, float y, int seed = 0)
        {
            int x0 = Mathf.FloorToInt(x), y0 = Mathf.FloorToInt(y);
            float fx = x - x0, fy = y - y0;
            float sx = fx * fx * (3f - 2f * fx), sy = fy * fy * (3f - 2f * fy);
            float a = Hash(x0, y0, seed), b = Hash(x0 + 1, y0, seed), c = Hash(x0, y0 + 1, seed), d = Hash(x0 + 1, y0 + 1, seed);
            return Mathf.Lerp(Mathf.Lerp(a, b, sx), Mathf.Lerp(c, d, sx), sy);
        }

        /// <summary>Fractal noise: several octaves of value noise, 0..1.</summary>
        public static float Fbm(float x, float y, int octaves, int seed = 0)
        {
            float sum = 0f, amp = 0.5f, norm = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Value(x, y, seed + i * 31) * amp;
                norm += amp;
                x *= 2.03f; y *= 2.03f;
                amp *= 0.5f;
            }
            return sum / norm;
        }
    }
}
