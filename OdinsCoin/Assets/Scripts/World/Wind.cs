using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The wind over the sea. Direction is where the wind blows TO (a flat, normalised vector).
    /// It veers slowly over time; blessings and storms can push it around.
    /// </summary>
    public static class Wind
    {
        static float angle = 40f;      // degrees, 0 = blowing towards +Z (north)
        static float strength = 0.7f;  // 0..1
        static float targetAngle = 40f, targetStrength = 0.7f, nextChange;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { angle = targetAngle = 40f; strength = targetStrength = 0.7f; nextChange = 0f; }

        public static Vector3 Direction { get { return Quaternion.Euler(0f, angle, 0f) * Vector3.forward; } }
        public static float Strength { get { return strength; } }
        /// <summary>The strongest wind a storm brings.</summary>
        public const float MaxStrength = 1.6f;
        public static float Angle { get { return angle; } }

        /// <summary>Wind speed in knots: a breeze at 0, a stiff wind at 1, a full gale (41 kn) at <see cref="MaxStrength"/> in storms.</summary>
        public static float Knots { get { return 6f + strength * 22f; } }

        /// <summary>Forces the wind (blessings, storms). Changes ease in over a few seconds.</summary>
        public static void Set(float newAngle, float newStrength, float holdSeconds)
        {
            targetAngle = newAngle;
            targetStrength = Mathf.Clamp(newStrength, 0f, MaxStrength);
            nextChange = Time.time + holdSeconds;
        }

        public static void Tick(float dt)
        {
            if (Time.time >= nextChange)
            {
                targetAngle = angle + Random.Range(-60f, 60f);
                targetStrength = Random.Range(0.35f, 0.95f);
                nextChange = Time.time + Random.Range(40f, 90f);
            }
            angle = Mathf.MoveTowards(angle, targetAngle, dt * 4f);
            strength = Mathf.MoveTowards(strength, targetStrength, dt * 0.05f);
        }
    }

    /// <summary>
    /// Numbers that shape how the longship handles. Kept as pure functions so they can be tested
    /// without a physics scene.
    /// </summary>
    public static class ShipTuning
    {
        public const float Mass = 8000f;
        public const int BuoyancyPoints = 8;
        /// <summary>How deep the hull sits at rest (m below the waterline at each float point).</summary>
        public const float RestDraft = 0.5f;
        /// <summary>Spring per float point, chosen so the ship floats at <see cref="RestDraft"/>.</summary>
        public static float Spring { get { return Mass * 9.81f / (BuoyancyPoints * RestDraft); } }
        /// <summary>Damping per float point: just under critical, so the ship settles after a wave.</summary>
        public static float Damping { get { return 0.7f * 2f * Mathf.Sqrt(Spring * Mass / BuoyancyPoints); } }

        public const float MaxSailThrust = 9000f;
        public const float RowThrust = 2600f;
        public const float ForwardDrag = 140f;   // N per (m/s)^2
        public const float SideDrag = 4500f;     // the keel resists sliding sideways
        public const float RudderTorque = 11000f;

        /// <summary>Upward force at one float point: spring on how deep it is, damped by how fast it moves up/down.</summary>
        public static float Buoyancy(float depth, float verticalSpeed)
        {
            if (depth <= 0f) return 0f;
            return Mathf.Max(0f, Spring * Mathf.Min(depth, 2.5f) - Damping * verticalSpeed);
        }

        /// <summary>
        /// A square sail loves wind from behind and is useless into the wind.
        /// 1 = running straight downwind, ~0.5 = wind from the side, 0.08 = headwind.
        /// </summary>
        public static float SailEfficiency(Vector3 shipForward, Vector3 windDirection)
        {
            shipForward.y = 0f;
            windDirection.y = 0f;
            float along = Vector3.Dot(shipForward.normalized, windDirection.normalized);
            return Mathf.Max(0.08f, (1f + along) / 2f);
        }

        public static float SailThrust(float sailAmount, Vector3 shipForward)
        {
            return MaxSailThrust * Mathf.Clamp01(sailAmount) * Wind.Strength * SailEfficiency(shipForward, Wind.Direction);
        }

        public static float MetresPerSecondToKnots(float mps) { return mps * 1.9438f; }
    }
}
