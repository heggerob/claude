using UnityEngine;

namespace OdinsCoin
{
    /// <summary>Pure rules for the dangers at sea, kept apart from the scene so they can be tested.</summary>
    public static class SeaMath
    {
        /// <summary>Storm strength 0..1 at a point: full inside the eye wall, fading out over the storm's edge.</summary>
        public static float StormIntensity(Vector2 centre, float radius, Vector2 p)
        {
            float d = Vector2.Distance(centre, p);
            float t = Mathf.Clamp01((d - radius * 0.55f) / (radius * 0.45f));
            return 1f - t * t * (3f - 2f * t);
        }

        /// <summary>How fast water comes over the side in a storm (hull fraction per second).</summary>
        public static float StormLeakRate(float intensity) { return intensity < 0.35f ? 0f : 0.012f * (intensity - 0.35f) / 0.65f; }

        /// <summary>Is a world point inside a longship's hull footprint (given in the ship's local space)?</summary>
        public static bool InsideHull(Vector3 local)
        {
            if (Mathf.Abs(local.z) > LongshipBuilder.Length / 2f) return false;
            float hw, k, g;
            LongshipBuilder.Station(local.z / (LongshipBuilder.Length / 2f), out hw, out k, out g);
            return Mathf.Abs(local.x) < hw + 0.3f && local.y > k - 0.5f && local.y < g + 1.5f;
        }

        /// <summary>Ram damage from closing speed (m/s): nothing below a walking pace, then it grows fast.</summary>
        public static float RamDamage(float closingSpeed, float multiplier)
        {
            if (closingSpeed < 2f) return 0f;
            return (closingSpeed - 1f) * 9f * multiplier;
        }

        /// <summary>
        /// Where a raider aims: alongside its target, on the side it's already on, a little ahead
        /// so it closes in while matching course.
        /// </summary>
        public static Vector3 InterceptPoint(Vector3 target, Vector3 targetForward, Vector3 targetVelocity, Vector3 raider, float standOff)
        {
            Vector3 fwd = new Vector3(targetForward.x, 0f, targetForward.z).normalized;
            Vector3 right = new Vector3(fwd.z, 0f, -fwd.x);
            float side = Vector3.Dot(raider - target, right) >= 0f ? 1f : -1f;
            Vector3 lead = new Vector3(targetVelocity.x, 0f, targetVelocity.z) * 2f;
            return target + right * side * standOff + lead + fwd * 4f;
        }

        /// <summary>Signed turn (-1..1) to steer from <paramref name="heading"/> (degrees) towards a point.</summary>
        public static float SteerTowards(Vector3 from, float heading, Vector3 to)
        {
            Vector3 d = to - from;
            float want = Mathf.Atan2(d.x, d.z) * Mathf.Rad2Deg;
            return Mathf.Clamp(Mathf.DeltaAngle(heading, want) / 35f, -1f, 1f);
        }
    }

    /// <summary>Water sloshing in the hull: rises from storms and arrow holes, bailed out by the crew.</summary>
    public class HullWater
    {
        public const float BailAmount = 0.07f;
        public float Level;          // 0..1, the ship founders at 1
        public int Holes;            // each hole lets water in until it's plugged

        public const float HoleRate = 0.004f;

        public bool Sunk { get { return Level >= 1f; } }

        /// <summary>Thrust multiplier: a waterlogged ship is sluggish.</summary>
        public float SpeedMultiplier { get { return 1f - 0.6f * Mathf.Clamp01(Level); } }

        public void Tick(float dt, float stormIntensity)
        {
            Level = Mathf.Clamp01(Level + (SeaMath.StormLeakRate(stormIntensity) + Holes * HoleRate) * dt);
        }

        public void Flood(float amount) { Level = Mathf.Clamp01(Level + amount); }

        /// <summary>One bucket over the side. Plugs a hole first if there is one.</summary>
        public void Bail()
        {
            if (Holes > 0) { Holes--; return; }
            Level = Mathf.Max(0f, Level - BailAmount);
        }

        public void Reset() { Level = 0f; Holes = 0; }
    }

    /// <summary>
    /// Jörmungandr's moods, as a small state machine: surfaces, circles the ship, rears up (the warning),
    /// strikes, then lies stunned with its head at the gunwale, where axes can reach it.
    /// </summary>
    public class SerpentBrain
    {
        public enum Phase { Rising, Circling, Rearing, Striking, Stunned, Diving, Gone }

        public const float RiseTime = 3f, CircleTime = 9f, RearTime = 2.5f, StrikeTime = 0.6f, StunTime = 4f, DiveTime = 3f;
        public const float MaxHealth = 240f;
        public const int MaxStrikes = 4;

        public Phase State = Phase.Rising;
        public float Timer;
        public float Health = MaxHealth;
        public int Strikes;

        /// <summary>Advance time. Returns true on the frame the strike lands.</summary>
        public bool Tick(float dt)
        {
            Timer += dt;
            switch (State)
            {
                case Phase.Rising: if (Timer >= RiseTime) Next(Phase.Circling); break;
                case Phase.Circling: if (Timer >= CircleTime) Next(Phase.Rearing); break;
                case Phase.Rearing: if (Timer >= RearTime) Next(Phase.Striking); break;
                case Phase.Striking:
                    if (Timer >= StrikeTime)
                    {
                        Strikes++;
                        Next(Phase.Stunned);
                        return true;
                    }
                    break;
                case Phase.Stunned:
                    if (Timer >= StunTime) Next(Strikes >= MaxStrikes ? Phase.Diving : Phase.Circling);
                    break;
                case Phase.Diving: if (Timer >= DiveTime) Next(Phase.Gone); break;
            }
            return false;
        }

        /// <summary>Only a stunned serpent's head can be hit. Returns true if the blow landed.</summary>
        public bool Hit(float damage)
        {
            if (State != Phase.Stunned || Health <= 0f) return false;
            Health -= damage;
            if (Health <= 0f) { Health = 0f; Next(Phase.Diving); }
            return true;
        }

        public bool Defeated { get { return Health <= 0f; } }

        void Next(Phase p) { State = p; Timer = 0f; }
    }
}
