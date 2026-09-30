using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// How a hero moves over the ground, step by step. Pure maths, so it can be tested and previewed.
    /// <para>
    /// A body can't turn on the spot at full speed: it turns by planting its feet. Each footstep can only swing the
    /// heading so far (a lot when standing, little at a sprint), so every step plans where the next one lands and
    /// how far it turns, and the body turns smoothly between the two plants. A sharp turn first brakes (the feet
    /// dig in), then pivots in small steps, then pushes off again. Speed builds up and dies down gradually.
    /// </para>
    /// Headings are in degrees about Y, 0 = +Z; positions are on the ground plane (x, z).
    /// </summary>
    public class Locomotion
    {
        public struct Footstep
        {
            public Vector2 at;
            public float heading;
            public bool left;
        }

        /// <summary>Top sprint speed, which the gait and stride are measured against (m/s).</summary>
        public float sprintSpeed = 6.5f;
        /// <summary>Speeding up, and slowing down (m/s²).</summary>
        public float accel = 9f, decel = 13f;
        /// <summary>How far one footstep may turn the body: standing, and at a full sprint (degrees).</summary>
        public float turnPerStepStanding = 75f, turnPerStepSprinting = 20f;
        /// <summary>Steps per second when turning on the spot.</summary>
        public float pivotCadence = 3.8f;
        /// <summary>Stride of one step at a slow walk and at a sprint (m).</summary>
        public float shortStep = 0.6f, longStep = 1.35f;
        /// <summary>Half the gap between the feet (m).</summary>
        public float footSpacing = 0.1f;

        public Vector2 position;
        public float heading;
        /// <summary>Speed along the heading (m/s).</summary>
        public float speed;
        /// <summary>How far through the current step (0 = just planted, 1 = the swinging foot lands).</summary>
        public float phase;
        /// <summary>Which foot is swinging forward now.</summary>
        public bool leftSwinging = true;
        public Footstep planted, next;
        /// <summary>Smoothed turn rate (deg/s, + = turning right) and acceleration (m/s²), for leaning and banking.</summary>
        public float turnRate, acceleration;
        public int stepsTaken;

        float stepFrom, stepTo, desired;
        bool moving;

        /// <summary>Where the player wants to face (degrees): the head looks there before the body turns.</summary>
        public float Desired { get { return desired; } }

        /// <summary>0 standing still .. 1 full sprint.</summary>
        public float Gait { get { return Mathf.Clamp01(speed / sprintSpeed); } }

        /// <summary>Where the legs are in the stride: 0..1 over two steps (left then right), for animating.</summary>
        public float Stride { get { return ((leftSwinging ? 0f : 0.5f) + Mathf.Clamp01(phase) * 0.5f) % 1f; } }

        /// <summary>Turning on the spot: stepping round with little or no forward speed.</summary>
        public bool Pivoting { get { return moving && speed < 0.6f && Mathf.Abs(Mathf.DeltaAngle(heading, desired)) > 8f; } }

        /// <summary>Whether the feet are stepping at all (moving or pivoting).</summary>
        public bool Stepping { get { return moving; } }

        public Vector3 Velocity
        {
            get { float a = heading * Mathf.Deg2Rad; return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * speed; }
        }

        public void Reset(Vector2 at, float facing)
        {
            position = at;
            heading = stepFrom = stepTo = desired = facing;
            speed = phase = turnRate = acceleration = 0f;
            moving = false;
            planted = Foot(at, facing, false);
            next = Foot(at, facing, true);
        }

        /// <summary>Turn everything by <paramref name="degrees"/> (the ship under us turned).</summary>
        public void Rotate(float degrees)
        {
            heading += degrees; stepFrom += degrees; stepTo += degrees; desired += degrees;
        }

        /// <summary>How much one step may turn at the current speed (degrees).</summary>
        public float TurnPerStep { get { return Mathf.Lerp(turnPerStepStanding, turnPerStepSprinting, Gait); } }

        /// <summary>Steps per second at the current speed.</summary>
        public float Cadence
        {
            get
            {
                if (speed < 0.3f) return pivotCadence;
                return Mathf.Max(pivotCadence * 0.8f, speed / Mathf.Lerp(shortStep, longStep, Gait));
            }
        }

        /// <summary>
        /// Advance by <paramref name="dt"/>. <paramref name="wish"/> is where the player wants to go on the ground
        /// (x, z), its length 0..1 how hard the stick is pushed; <paramref name="maxSpeed"/> is the speed for a full push.
        /// </summary>
        public void Step(Vector2 wish, float maxSpeed, float dt)
        {
            if (dt <= 0f) return;
            float push = Mathf.Clamp01(wish.magnitude);
            if (push > 0.1f) desired = Mathf.Atan2(wish.x, wish.y) * Mathf.Rad2Deg;
            else desired = heading;
            float off = Mathf.DeltaAngle(heading, desired);

            // Speed: go for the pushed speed, less the further the heading is from where we want to go, so a sharp
            // turn brakes first (all the way to a stop for a U-turn) and only then pushes off in the new direction.
            // From a standstill the body pivots round before it sets off; on the move it carries on through turns.
            float give = Mathf.Lerp(-0.6f, 0.25f, Gait);
            float align = Mathf.Clamp01((Mathf.Cos(off * Mathf.Deg2Rad) + give) / (1f + give));
            float target = push * maxSpeed * align;
            float before = speed;
            speed = Mathf.MoveTowards(speed, target, (target > speed ? accel : decel) * dt);
            acceleration = Mathf.Lerp(acceleration, (speed - before) / dt, 1f - Mathf.Exp(-dt * 10f));

            // Stepping: while moving, or while there's turning left to do on the spot.
            bool wantsToStep = speed > 0.05f || (push > 0.1f && Mathf.Abs(off) > 3f);
            if (wantsToStep && !moving)
            {
                // First step: plan from where we stand.
                moving = true;
                phase = 0f;
                stepFrom = heading;
            }

            float prevHeading = heading;
            if (moving)
            {
                // The step now in the air may turn the body at most this far from where the last one landed; if the
                // wish changes mid-step the plan follows it, within that limit.
                float allow = TurnPerStep;
                stepTo = stepFrom + Mathf.Clamp(Mathf.DeltaAngle(stepFrom, desired), -allow, allow);
                float cadence = Cadence;
                phase += cadence * dt;
                // Turn with the step: slow off the planted foot, quickest mid-swing, easing into the landing. The body
                // follows that curve at a limited rate, so a plan changed mid-step never snaps it round.
                float along = stepFrom + Mathf.DeltaAngle(stepFrom, stepTo) * Smooth(Mathf.Clamp01(phase));
                heading = Mathf.MoveTowardsAngle(heading, along, allow * cadence * 1.6f * dt);
                if (phase >= 1f)
                {
                    // The swinging foot lands; the other one starts its swing, planned from where the body now faces.
                    phase -= 1f;
                    stepFrom = heading;
                    leftSwinging = !leftSwinging;
                    stepsTaken++;
                    planted = Foot(position, heading, !leftSwinging);
                    // Stop stepping once we've come to rest facing where we want.
                    if (speed <= 0.05f && Mathf.Abs(Mathf.DeltaAngle(heading, desired)) <= 3f) { moving = false; phase = 0f; }
                }
            }
            turnRate = Mathf.Lerp(turnRate, Mathf.DeltaAngle(prevHeading, heading) / dt, 1f - Mathf.Exp(-dt * 8f));

            position += new Vector2(Mathf.Sin(heading * Mathf.Deg2Rad), Mathf.Cos(heading * Mathf.Deg2Rad)) * speed * dt;

            // Where the swinging foot will land: half a stride ahead along the planned heading.
            float ahead = speed / Mathf.Max(0.1f, Cadence) * (1f - Mathf.Clamp01(phase));
            float a = stepTo * Mathf.Deg2Rad;
            next = Foot(position + new Vector2(Mathf.Sin(a), Mathf.Cos(a)) * ahead, stepTo, leftSwinging);
        }

        /// <summary>
        /// In the air there's no footing: no steps, only a little steering, and the run carries on (a jump keeps
        /// its momentum). <paramref name="wish"/> as for <see cref="Step"/>.
        /// </summary>
        public void Air(Vector2 wish, float dt)
        {
            if (dt <= 0f) return;
            float prev = heading;
            if (wish.sqrMagnitude > 0.01f) heading = Mathf.MoveTowardsAngle(heading, Mathf.Atan2(wish.x, wish.y) * Mathf.Rad2Deg, 90f * dt);
            stepFrom = stepTo = desired = heading;
            speed = Mathf.MoveTowards(speed, 0f, 0.6f * dt);
            turnRate = Mathf.Lerp(turnRate, Mathf.DeltaAngle(prev, heading) / dt, 1f - Mathf.Exp(-dt * 8f));
            acceleration = Mathf.Lerp(acceleration, 0f, 1f - Mathf.Exp(-dt * 10f));
            position += new Vector2(Mathf.Sin(heading * Mathf.Deg2Rad), Mathf.Cos(heading * Mathf.Deg2Rad)) * speed * dt;
        }

        Footstep Foot(Vector2 body, float facing, bool left)
        {
            float a = facing * Mathf.Deg2Rad;
            var right = new Vector2(Mathf.Cos(a), -Mathf.Sin(a));
            return new Footstep { at = body + right * (left ? -footSpacing : footSpacing), heading = facing, left = left };
        }

        static float Smooth(float t) { return t * t * (3f - 2f * t); }
    }
}
