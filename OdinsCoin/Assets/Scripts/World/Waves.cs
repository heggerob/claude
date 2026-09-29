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
                float phase = k * (d.x * x + d.y * z) - w.speed * k * t;
                // Sharpen crests a little: sin shifted towards peaks.
                float s = Mathf.Sin(phase);
                h += w.amplitude * (s + 0.25f * s * s - 0.125f);
            }
            return h * Roughness;
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
