using System;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>What the ground is at a spot: it picks the terrain's colour.</summary>
    public enum Ground { Seabed, Sand, Grass, Rock, Snow }

    /// <summary>
    /// The land at close range: the real 1 km map (<see cref="WorldMap"/>) with detail made up on top in code, so
    /// the coast gets skerries and coves and the mountains get crags, without changing the real shape of the land.
    /// Pure maths on global game coordinates (see <see cref="WorldOrigin"/>), so it can be tested and every
    /// chunk agrees with its neighbours.
    /// </summary>
    public static class TerrainDetail
    {
        /// <summary>The height of the ground at a global game position (with detail).</summary>
        public static float Height(WorldMap map, double x, double z)
        {
            float s = WorldMap.Scale;
            float baseH = map.GroundHeight((float)x, (float)z);
            double rx = x / s, rz = z / s;
            float real = baseH / s;
            // Rolling detail on the land, strongest in the mountains: crags a few tens of metres high. It fades out
            // at the waterline, so the real coast (and the shallows off it) stays where it is and navigable.
            float onLand = Mathf.Clamp01(real / 15f + 0.2f);
            float hills = Fbm(rx / 700.0, rz / 700.0, 4) * Mathf.Clamp(real * 0.08f, 3f, 70f) * onLand;
            // Right at the waterline, small bumps that fray the coast into a few holms and coves.
            float coast = Mathf.Clamp01(1f - Mathf.Abs(real - 1f) / 6f);
            float skerries = Fbm(rx / 160.0 + 31.7, rz / 160.0 - 12.3, 2) * 2.2f * coast;
            // Out at sea, the detail fades so the sea floor stays smooth.
            float sea = real < -25f ? Mathf.Clamp01(1f + (real + 25f) / 60f) : 1f;
            // The rivers and sounds too narrow for the map's grid, carved in.
            return Caves.Hollow(x, z, Channels.Carve(map, x, z, baseH + (hills * sea + skerries) * s));
        }

        /// <summary>What the ground is: sand on the shore, grass on the lower slopes, rock on steep and high ground, snow on the peaks.</summary>
        public static Ground Kind(float realHeight, float slope)
        {
            if (realHeight < -0.5f) return Ground.Seabed;
            if (realHeight > 1500f || (realHeight > 1100f && slope < 0.5f)) return Ground.Snow;
            // Only a strip of beach at the waterline: the low farmland of the south is green, not a desert.
            if (realHeight < 0.22f && slope < 0.35f) return Ground.Sand;
            if (slope > 0.7f || realHeight > 850f) return Ground.Rock;
            return Ground.Grass;
        }

        // ---------------------------------------------------------------- noise

        /// <summary>Fractal value noise, about -1..1.</summary>
        public static float Fbm(double x, double z, int octaves)
        {
            double sum = 0.0, amp = 0.5, norm = 0.0;
            for (int i = 0; i < octaves; i++)
            {
                sum += Value(x, z) * amp;
                norm += amp;
                x = x * 2.03 + 17.1;
                z = z * 2.03 - 9.7;
                amp *= 0.5;
            }
            return (float)(sum / norm);
        }

        /// <summary>Smooth value noise on the integer lattice, -1..1.</summary>
        static double Value(double x, double z)
        {
            double fx = Math.Floor(x), fz = Math.Floor(z);
            long ix = (long)fx, iz = (long)fz;
            double tx = x - fx, tz = z - fz;
            tx = tx * tx * (3.0 - 2.0 * tx);
            tz = tz * tz * (3.0 - 2.0 * tz);
            double a = Hash(ix, iz), b = Hash(ix + 1, iz), c = Hash(ix, iz + 1), d = Hash(ix + 1, iz + 1);
            return (a + (b - a) * tx) + ((c + (d - c) * tx) - (a + (b - a) * tx)) * tz;
        }

        static double Hash(long x, long z)
        {
            unchecked
            {
                ulong h = (ulong)x * 0x9E3779B97F4A7C15UL ^ (ulong)z * 0xC2B2AE3D27D4EB4FUL;
                h ^= h >> 31;
                h *= 0xBF58476D1CE4E5B9UL;
                h ^= h >> 29;
                return (h >> 11) * (1.0 / (1UL << 53)) * 2.0 - 1.0;
            }
        }
    }
}
