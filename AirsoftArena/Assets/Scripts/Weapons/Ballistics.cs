using UnityEngine;

namespace AirsoftArena
{
    /// <summary>
    /// The BB flight model, shared by the live simulation (BBSystem) and by predictions for the HUD
    /// (range readout, drop hint, effective range in the shop). Height z is metres above the ground.
    /// </summary>
    public static class Ballistics
    {
        public const float Gravity = 9.81f;
        // 0.5 * air density * drag coefficient of a sphere * cross-section area of a 6 mm BB.
        public const float DragFactor = 0.5f * 1.2f * 0.47f * Mathf.PI * 0.003f * 0.003f;
        public const float MinSpeed = 8f;
        public const float MaxAge = 4f;
        const float PredictStep = 1f / 240f;
        static readonly System.Collections.Generic.Dictionary<WeaponData, float> effectiveCache = new System.Collections.Generic.Dictionary<WeaponData, float>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { effectiveCache.Clear(); }

        /// <summary>Cached <see cref="EffectiveRange"/> for UI that asks every frame.</summary>
        public static float EffectiveRangeCached(WeaponData w)
        {
            float r;
            if (w == null) return 0f;
            if (!effectiveCache.TryGetValue(w, out r)) { r = EffectiveRange(w); effectiveCache[w] = r; }
            return r;
        }

        /// <summary>One integration step. Returns the new horizontal speed.</summary>
        public static float Step(ref Vector2 vel, ref float vz, float mass, float hop, float v0, Vector2 wind, float h)
        {
            // Quadratic air drag, relative to the moving air (that is what makes wind push BBs sideways).
            Vector2 rel = vel - wind;
            vel -= rel * (DragFactor / mass * rel.magnitude * h);
            float speed = vel.magnitude;
            // Hop-up backspin lift, strongest while the BB is fast.
            float lift = Gravity * hop * Mathf.Sqrt(Mathf.Clamp01(speed / v0));
            vz += (lift - Gravity) * h;
            return speed;
        }

        /// <summary>
        /// Height of a perfectly aimed BB when it has travelled <paramref name="distance"/> metres,
        /// or -1 if it hits the ground or runs out of speed before getting there.
        /// </summary>
        public static float HeightAtDistance(WeaponData w, float muzzleHeight, float distance)
        {
            if (w == null || w.IsMelee) return -1f;
            float v0 = w.MuzzleVelocity, mass = Mathf.Max(0.12f, w.bbWeightGrams) / 1000f;
            Vector2 vel = new Vector2(v0, 0f);
            float vz = 0f, z = muzzleHeight, x = 0f, t = 0f;
            while (x < distance)
            {
                float speed = Step(ref vel, ref vz, mass, w.hopUp, v0, Vector2.zero, PredictStep);
                x += vel.x * PredictStep;
                z += vz * PredictStep;
                t += PredictStep;
                if (z <= 0f || speed < MinSpeed || t > MaxAge) return -1f;
            }
            return z;
        }

        /// <summary>Where the BB lands when fired level from muzzle height.</summary>
        public static float MaxRange(WeaponData w, float muzzleHeight = 1.4f)
        {
            if (w == null || w.IsMelee) return w != null ? w.meleeRange : 0f;
            float v0 = w.MuzzleVelocity, mass = Mathf.Max(0.12f, w.bbWeightGrams) / 1000f;
            Vector2 vel = new Vector2(v0, 0f);
            float vz = 0f, z = muzzleHeight, x = 0f, t = 0f;
            while (true)
            {
                float speed = Step(ref vel, ref vz, mass, w.hopUp, v0, Vector2.zero, PredictStep);
                x += vel.x * PredictStep;
                z += vz * PredictStep;
                t += PredictStep;
                if (z <= 0f || speed < MinSpeed || t > MaxAge) return x;
            }
        }

        /// <summary>
        /// Distance up to which the BB stays at chest height or above (hits a standing player in the body):
        /// the range you can actually rely on.
        /// </summary>
        public static float EffectiveRange(WeaponData w, float muzzleHeight = 1.4f)
        {
            if (w == null || w.IsMelee) return w != null ? w.meleeRange : 0f;
            float v0 = w.MuzzleVelocity, mass = Mathf.Max(0.12f, w.bbWeightGrams) / 1000f;
            Vector2 vel = new Vector2(v0, 0f);
            float vz = 0f, z = muzzleHeight, x = 0f, t = 0f;
            while (true)
            {
                float speed = Step(ref vel, ref vz, mass, w.hopUp, v0, Vector2.zero, PredictStep);
                x += vel.x * PredictStep;
                z += vz * PredictStep;
                t += PredictStep;
                if (z < 1.0f || speed < MinSpeed || t > MaxAge) return x;
            }
        }
    }
}
