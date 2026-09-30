using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// A long passage at full speed: the ship sails on the same physics (<see cref="ShipPhysics.Step"/>), flat and
    /// in big steps, holding her course on the helm by herself, so hours of open sea go by in minutes. She stops at
    /// shoal water, and whatever stops fast time (raiders, the serpent, a storm) stops the passage too.
    /// </summary>
    public static class Passage
    {
        /// <summary>How much faster than real time the ship sails on a passage.</summary>
        public const float Factor = 240f;
        /// <summary>The flat simulation's time step (s): small enough to stay steady, big enough to be quick.</summary>
        public const float SubStep = 0.25f;
        /// <summary>Stop the passage when the water ahead is shallower than her draught plus this (m).</summary>
        public const float ShoalMargin = 2f;

        /// <summary>The helm that holds a course: rudder over towards it, eased as she swings.</summary>
        public static float Autopilot(float heading, float course, float turnRate)
        {
            return Mathf.Clamp(Mathf.DeltaAngle(heading, course) / 15f - turnRate * Mathf.Rad2Deg / 6f, -1f, 1f);
        }

        /// <summary>
        /// Sail <paramref name="seconds"/> of ship time on the flat simulation, holding <paramref name="course"/>,
        /// stopping early if <paramref name="shoal"/> says the water ahead of the bow is too shallow. Returns the new
        /// state; <paramref name="stopped"/> is true if she had to stop.
        /// </summary>
        public static ShipPhysics.State Advance(ShipDesign d, ShipPhysics.State s, ShipPhysics.Controls c, Vector2 wind, float course, float seconds, System.Func<Vector2, bool> shoal, out bool stopped)
        {
            stopped = false;
            float left = seconds;
            while (left > 1e-4f)
            {
                float dt = Mathf.Min(SubStep, left);
                left -= dt;
                c.rudder = Autopilot(s.heading, course, s.r);
                s = ShipPhysics.Step(d, s, c, wind, dt);
                if (shoal != null)
                {
                    float h = s.heading * Mathf.Deg2Rad;
                    var bow = s.position + new Vector2(Mathf.Sin(h), Mathf.Cos(h)) * (d.length * 0.5f + Mathf.Max(20f, s.u * 10f));
                    if (shoal(bow)) { stopped = true; s.u = s.v = s.r = 0f; break; }
                }
            }
            return s;
        }
    }
}
