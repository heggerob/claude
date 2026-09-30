using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Breath for effort, as in Zelda: climbing drains it fastest, sprinting and hard swimming more slowly, and it
    /// comes back when you stand or walk on solid footing. Each Rune of Endurance gives more of it.
    /// </summary>
    public static class StaminaRules
    {
        /// <summary>Seconds of climbing you start with, and what each Rune of Endurance adds.</summary>
        public const float BaseStamina = 8f;
        /// <summary>How fast you climb (m/s).</summary>
        public const float ClimbSpeed = 1.6f;
        /// <summary>Drain per second climbing, sprinting and swimming; refill per second at rest on your feet.</summary>
        public const float ClimbDrain = 1f, SprintDrain = 0.35f, SwimDrain = 0.25f, Refill = 2.5f;
        /// <summary>Having run dry, you're tired until it's back to this share of the full.</summary>
        public const float RecoverAt = 0.3f;

        public static float Max(int endurance) { return BaseStamina + RuneShrines.EnduranceSeconds * Mathf.Max(0, endurance); }

        /// <summary>Steep enough to climb rather than walk up (a slope past about 55°), and not an overhang.</summary>
        public static bool Climbable(Vector3 normal) { return normal.y < 0.57f && normal.y > -0.3f; }

        public static float Step(float stamina, float max, float dt, bool climbing, bool sprinting, bool swimming, bool footing)
        {
            float change;
            if (climbing) change = -ClimbDrain;
            else if (sprinting) change = -SprintDrain;
            else if (swimming) change = -SwimDrain;
            else change = footing ? Refill : 0f;
            return Mathf.Clamp(stamina + change * dt, 0f, max);
        }
    }
}
