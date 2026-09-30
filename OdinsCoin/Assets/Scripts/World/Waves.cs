using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The sea surface as a pure function of position and time, so the ocean mesh, ship buoyancy
    /// and anything else floating all agree on where the water is. Sum of a few directional waves.
    /// </summary>
    public static class Waves
    {
        struct Wave
        {
            public Vector2 direction;
            public float amplitude, length, speed;
        }

        static readonly Wave[] Set =
        {
            new Wave { direction = new Vector2(1f, 0.2f), amplitude = 0.55f, length = 28f, speed = 5.5f },
            new Wave { direction = new Vector2(0.6f, 1f), amplitude = 0.32f, length = 15f, speed = 4f },
            new Wave { direction = new Vector2(-0.8f, 0.5f), amplitude = 0.16f, length = 8f, speed = 3f },
            new Wave { direction = new Vector2(0.2f, -1f), amplitude = 0.08f, length = 4.5f, speed = 2.2f },
        };

        /// <summary>1 = normal sea, higher in storms.</summary>
        public static float Roughness = 1f;

        /// <summary>
        /// The sea the wind raises, before any storm: a light breeze barely ruffles it, a stiff wind builds a proper
        /// sea, and in the lee of the land (a fjord, behind an island) it lies much calmer.
        /// </summary>
        public static float SeaState(float windKnots, float lee)
        {
            float wind = Mathf.Clamp01((windKnots - 4f) / 24f);
            return Mathf.Lerp(0.35f, 1.25f, wind * wind * (3f - 2f * wind)) * Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(lee));
        }

        public static float Time { get { return UnityEngine.Time.time; } }

        public static float MaxHeight
        {
            get
            {
                float h = 0f;
                foreach (var w in Set) h += w.amplitude;
                return h * Roughness;
            }
        }

        /// <summary>Water height (world y) at a point.</summary>
        public static float Height(float x, float z) { return Height(x, z, Time); }

        public static float Height(float x, float z, float t)
        {
            float h = 0f;
            for (int i = 0; i < Set.Length; i++)
            {
                var w = Set[i];
                Vector2 d = w.direction.normalized;
                float k = 2f * Mathf.PI / w.length;
                float phase = k * (d.x * x + d.y * z) + Origin(i, k, d) - w.speed * k * t;
                // Sharpen crests a little: sin shifted towards peaks.
                float s = Mathf.Sin(phase);
                h += w.amplitude * (s + 0.25f * s * s - 0.125f);
            }
            return h * Roughness;
        }

        // The waves are fixed to the whole world, not the scene: when the floating origin shifts, each wave's
        // phase takes up the offset (worked out in double, wrapped to one wavelength) so the sea doesn't jump.
        static double phaseX = double.NaN, phaseZ = double.NaN;
        static readonly float[] originPhase = new float[8];

        static float Origin(int i, float k, Vector2 d)
        {
            if (WorldOrigin.OffsetX != phaseX || WorldOrigin.OffsetZ != phaseZ)
            {
                phaseX = WorldOrigin.OffsetX;
                phaseZ = WorldOrigin.OffsetZ;
                for (int j = 0; j < Set.Length; j++)
                {
                    var dj = Set[j].direction.normalized;
                    double along = dj.x * phaseX + dj.y * phaseZ, length = Set[j].length;
                    double wrapped = along - System.Math.Floor(along / length) * length;
                    originPhase[j] = (float)(wrapped * 2.0 * System.Math.PI / length);
                }
            }
            return originPhase[i];
        }

        /// <summary>Surface normal from the height field (finite differences).</summary>
        public static Vector3 Normal(float x, float z)
        {
            const float e = 0.3f;
            float t = Time;
            float hx = Height(x + e, z, t) - Height(x - e, z, t);
            float hz = Height(x, z + e, t) - Height(x, z - e, t);
            return new Vector3(-hx, 2f * e, -hz).normalized;
        }
    }
}
