using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// A value that follows its target like a critically damped spring: it never snaps, never overshoots, and
    /// settles in about <c>time</c> seconds whatever the frame rate.
    /// </summary>
    public class Damped
    {
        public float value, velocity;

        public float Step(float target, float dt, float time)
        {
            if (dt <= 0f) return value;
            float omega = 2f / Mathf.Max(0.0001f, time);
            float x = omega * dt;
            float decay = 1f / (1f + x + 0.48f * x * x + 0.235f * x * x * x);
            float change = value - target;
            float temp = (velocity + omega * change) * dt;
            velocity = (velocity - omega * temp) * decay;
            value = target + (change + temp) * decay;
            return value;
        }
    }

    /// <summary>
    /// Turns a hero's movement into a pose, every frame: legs stepping with the footsteps, arms swinging against
    /// them (and pumping with bent elbows at a sprint), a body that bobs, leans into its speed and banks into turns,
    /// a head that looks where it's going before the body gets there, a tuck in the air and a squash on landing.
    /// Every joint goes through a damped spring, so moving between walking, sprinting, turning and jumping is
    /// always smooth. Pure maths: the game (and the preview) put the angles on the joints.
    /// <para>Angles are local Euler degrees on top of the standing posture (see <see cref="HeroPose.Lean"/>):
    /// legs and arms +X swing back, -X forward, +Z swings towards +X (the right arm out, the left arm in);
    /// body +X leans forward, +Z leans left.</para>
    /// </summary>
    public class HeroAnimator
    {
        public struct Frame
        {
            public Vector3 body, head, leftLeg, rightLeg, leftArm, rightArm;
            public float leftElbow, rightElbow;
            /// <summary>Knee bend, degrees (the shin swings back).</summary>
            public float leftKnee, rightKnee;
            /// <summary>Ankle, degrees: + points the toes down, - pulls them up.</summary>
            public float leftAnkle, rightAnkle;
            /// <summary>How far the whole body is lifted (m): the bob of each step, the dip of a landing.</summary>
            public float lift;
        }

        public Frame pose;

        readonly Damped bodyPitch = new Damped(), bodyRoll = new Damped(), bodyYaw = new Damped(), lift = new Damped();
        readonly Damped headPitch = new Damped(), headYaw = new Damped();
        readonly Damped legL = new Damped(), legR = new Damped(), legSplay = new Damped();
        readonly Damped armL = new Damped(), armR = new Damped(), armOut = new Damped();
        readonly Damped elbowL = new Damped(), elbowR = new Damped();
        readonly Damped air = new Damped(), stride = new Damped(), fall = new Damped(), rise = new Damped();
        readonly Damped kneeLd = new Damped(), kneeRd = new Damped(), ankleLd = new Damped(), ankleRd = new Damped();
        float landing, landingHard, airTime;
        bool wasGrounded = true;

        /// <summary>
        /// Advance by <paramref name="dt"/>. <paramref name="verticalSpeed"/> is m/s (+ up); <paramref name="time"/>
        /// drives breathing.
        /// </summary>
        public void Step(float dt, Locomotion loco, bool grounded, float verticalSpeed, bool swimming, float time)
        {
            if (dt <= 0f) return;
            float g = loco.Gait;
            float sprint = Mathf.Clamp01((g - 0.72f) / 0.28f);
            float a = loco.Stride * Mathf.PI * 2f;
            bool stepping = loco.Stepping && grounded;

            // In the air: a weight that fades in over the jump and out on landing.
            if (!grounded) airTime += dt; else airTime = 0f;
            float airTarget = grounded || swimming ? 0f : Mathf.Clamp01(airTime / 0.12f);
            // Quick to leave the ground, a little slower to settle back.
            air.Step(airTarget, dt, airTarget > air.value ? 0.08f : 0.14f);
            if (grounded && !wasGrounded && !swimming)
            {
                // Touching down: a squash that's deeper the harder the fall.
                landing = 1f;
                landingHard = Mathf.Clamp01(-verticalSpeed / 9f + 0.35f);
            }
            wasGrounded = grounded;
            landing = Mathf.Max(0f, landing - dt / 0.3f);
            // Eases in and out (no sudden start or stop), deepest a little after touching down.
            float squash = (0.5f - 0.5f * Mathf.Cos(Mathf.Clamp01(1f - landing) * Mathf.PI * 2f)) * landingHard * (landing > 0f ? 1f : 0f);

            // Legs: hip and knee follow measured human gait curves (see Gait), walking blending into running with
            // speed, the left leg half a stride from the right. The stride's size eases in and out, so starting,
            // stopping and leaving the ground never snap; pivoting takes small steps.
            float strideTarget = stepping ? Mathf.Clamp01(0.35f + g) : 0f;
            if (loco.Pivoting && grounded) strideTarget = Mathf.Max(strideTarget, 0.45f);
            float reach = stride.Step(strideTarget, dt, 0.1f);
            // The game's walk (4.2 m/s) is really a brisk jog, so the running gait comes in early.
            float run = Mathf.Clamp01((g - 0.35f) / 0.45f);
            float cycleL = Frac(loco.Stride + 0.5f), cycleR = Frac(loco.Stride);
            float hipL, kneeL, hipR, kneeR, ankL, ankR;
            Gait.Sample(cycleL, run, out hipL, out kneeL, out ankL);
            Gait.Sample(cycleR, run, out hipR, out kneeR, out ankR);
            // Feet stay planted: the thigh sweeps as far as the body travels over the foot while it's down, so the
            // stance foot doesn't skate (within what a leg can do).
            float sweep = Gait.SweepFor(loco.speed, loco.Cadence, run);
            float mean = Mathf.Lerp(Gait.WalkMeanHip, Gait.RunMeanHip, run);
            hipL = mean + (hipL - mean) * sweep; hipR = mean + (hipR - mean) * sweep;
            hipL *= reach; kneeL *= reach; hipR *= reach; kneeR *= reach; ankL *= reach; ankR *= reach;
            // In the air: legs push off straight, tuck up at the top, and reach down for the ground as it falls.
            float falling = fall.Step(Mathf.Clamp01(-verticalSpeed / 6f), dt, 0.1f);
            float rising = rise.Step(Mathf.Clamp01(verticalSpeed / 4.5f), dt, 0.06f);
            float tuck = Mathf.Clamp01(1f - rising - falling);
            float aHipL = -6f * rising + 58f * tuck + 32f * falling, aKneeL = 6f * rising + 100f * tuck + 34f * falling;
            float aHipR = 12f * rising + 30f * tuck + 14f * falling, aKneeR = 30f * rising + 72f * tuck + 22f * falling;
            // Landing: both knees give and the hips fold, deeper the harder the fall.
            float sHip = squash * 42f, sKnee = squash * 80f;
            if (swimming) { hipL = Mathf.Sin(time * 4f) * 25f; hipR = -hipL; kneeL = 20f + 15f * Mathf.Sin(time * 4f + 1f); kneeR = 20f - 15f * Mathf.Sin(time * 4f + 1f); }
            float wl = Mathf.Lerp(hipL, aHipL, air.value) + sHip, wr = Mathf.Lerp(hipR, aHipR, air.value) + sHip;
            float kl = Mathf.Lerp(kneeL, aKneeL, air.value) + sKnee, kr = Mathf.Lerp(kneeR, aKneeR, air.value) + sKnee;
            // (Hip flexion swings the leg forward: -X.)
            pose.leftLeg = new Vector3(-legL.Step(wl, dt, 0.04f), 0f, 0f);
            pose.rightLeg = new Vector3(-legR.Step(wr, dt, 0.04f), 0f, 0f);
            pose.leftKnee = kneeLd.Step(Mathf.Max(0f, kl), dt, 0.04f);
            pose.rightKnee = kneeRd.Step(Mathf.Max(0f, kr), dt, 0.04f);
            float splay = legSplay.Step(air.value * 7f + squash * 8f, dt, 0.08f);
            pose.leftLeg.z = -splay; pose.rightLeg.z = splay;
            // Ankles: rolling heel to toe on the ground; toes pointed in the push-off and the air, flexed up to land.
            float airAnkle = 22f * rising + 12f * tuck + 6f * falling;
            float landAnkle = -squash * 18f;
            pose.leftAnkle = ankleLd.Step(Mathf.Lerp(ankL, airAnkle, air.value) + landAnkle, dt, 0.04f);
            pose.rightAnkle = ankleRd.Step(Mathf.Lerp(ankR, airAnkle * 0.8f, air.value) + landAnkle, dt, 0.04f);
            float swing = -(pose.leftLeg.x - pose.rightLeg.x) * 0.5f;

            // Arms: against the legs, swinging wider when running; pumping with bent elbows at a sprint; thrown up
            // and out for balance in the air.
            float armSwing = -swing * Mathf.Lerp(0.8f, 1.3f, g);
            float airArm = Mathf.Lerp(-55f, -95f, falling);
            float al = Mathf.Lerp(-armSwing, airArm, air.value) + squash * 10f;
            float ar = Mathf.Lerp(armSwing, airArm * 0.8f, air.value) + squash * 10f;
            pose.leftArm = new Vector3(armL.Step(al, dt, 0.06f), 0f, 0f);
            pose.rightArm = new Vector3(armR.Step(ar, dt, 0.06f), 0f, 0f);
            float outward = armOut.Step(Mathf.Lerp(4f + 4f * g, 28f, air.value) + squash * 12f, dt, 0.08f);
            // (+Z swings a hand towards +X: out for the right arm, in for the left.)
            pose.leftArm.z = -outward; pose.rightArm.z = outward;
            float el = Mathf.Lerp(HeroPose.WalkElbow(pose.leftArm.x), -80f - 10f * Mathf.Sin(a), sprint);
            float er = Mathf.Lerp(HeroPose.WalkElbow(pose.rightArm.x), -80f + 10f * Mathf.Sin(a), sprint);
            pose.leftElbow = elbowL.Step(Mathf.Lerp(el, -45f, air.value), dt, 0.06f);
            pose.rightElbow = elbowR.Step(Mathf.Lerp(er, -45f, air.value), dt, 0.06f);

            // Body: leans into its speed (and harder while speeding up), banks into turns, twists a little
            // against the stride, bobs twice a stride, breathes when still, folds over a hard landing.
            float lean = swimming ? 60f : Mathf.Lerp(0f, 10f, g) + Mathf.Clamp(loco.acceleration * 1.1f, -7f, 9f)
                         + (g < 0.05f ? HeroPose.Breath(time) : 0f) + squash * 16f - air.value * 4f;
            float bank = -Mathf.Clamp(loco.turnRate * 0.05f * (0.3f + g), -12f, 12f)
                         // ...and the weight rolls over onto each foot in turn (most at a walk).
                         - Mathf.Sin(a) * Mathf.Lerp(3.5f, 1.5f, run) * reach;
            float twist = Mathf.Sin(a) * Mathf.Lerp(2f, 7f, g) * reach;
            pose.body = new Vector3(bodyPitch.Step(lean, dt, 0.12f), bodyYaw.Step(twist, dt, 0.06f), bodyRoll.Step(bank, dt, 0.15f));
            // The body rides on its legs: it sits as low as the longer of them lets it (so it bobs twice a stride
            // and sinks into a landing by itself), and a runner springs up off each step.
            float drop = (1f - air.value) * (Gait.Thigh + Gait.Shin - Mathf.Max(Gait.Reach(pose.leftLeg.x, pose.leftKnee), Gait.Reach(pose.rightLeg.x, pose.rightKnee)));
            float spring = run * reach * 0.035f * Mathf.Abs(Mathf.Sin(a));
            pose.lift = lift.Step(-drop + spring, dt, 0.03f);

            // Head: keeps looking ahead (undoing most of the lean) and turns towards where we're going before the
            // body does.
            float look = Mathf.Clamp(Mathf.DeltaAngle(loco.heading, loco.Desired) * 0.6f, -40f, 40f);
            pose.head = new Vector3(headPitch.Step(-pose.body.x * 0.6f, dt, 0.1f), headYaw.Step(look, dt, 0.12f), -pose.body.z * 0.5f);
        }

        static float Frac(float x) { return x - Mathf.Floor(x); }

        /// <summary>Put the pose on a hero's joints.</summary>
        public void Apply(VikingBuilder.Parts parts)
        {
            if (parts == null) return;
            parts.body.localRotation = Quaternion.Euler(HeroPose.Lean + pose.body.x, pose.body.y, pose.body.z);
            parts.body.localPosition = new Vector3(0f, pose.lift * parts.scale, 0f);
            parts.leftLeg.localRotation = Quaternion.Euler(pose.leftLeg.x - HeroPose.Lean, 0f, pose.leftLeg.z);
            if (parts.leftShin != null) parts.leftShin.localRotation = Quaternion.Euler(pose.leftKnee, 0f, 0f);
            if (parts.rightShin != null) parts.rightShin.localRotation = Quaternion.Euler(pose.rightKnee, 0f, 0f);
            if (parts.leftFoot != null) parts.leftFoot.localRotation = Quaternion.Euler(pose.leftAnkle, 0f, 0f);
            if (parts.rightFoot != null) parts.rightFoot.localRotation = Quaternion.Euler(pose.rightAnkle, 0f, 0f);
            parts.rightLeg.localRotation = Quaternion.Euler(pose.rightLeg.x - HeroPose.Lean, 0f, pose.rightLeg.z);
            parts.leftArm.localRotation = Quaternion.Euler(pose.leftArm);
            parts.rightArm.localRotation = Quaternion.Euler(pose.rightArm);
            HeroPose.Elbows(parts, pose.leftElbow, pose.rightElbow);
            if (parts.head != null) parts.head.localRotation = Quaternion.Euler(HeroPose.HeadUp + pose.head.x, pose.head.y, pose.head.z);
        }
    }

    /// <summary>
    /// Human hip and knee angles through one stride, from published gait measurements (sagittal plane, degrees
    /// of flexion): walking after Winter's normal gait data, running and sprinting after treadmill studies.
    /// The cycle starts at the foot's strike; stance takes ~60% of a walking stride and ~38% of a running one.
    /// </summary>
    public static class Gait
    {
        /// <summary>Thigh and shin length on the reference body (m), for how high the hips ride.</summary>
        public const float Thigh = 0.454f, Shin = 0.47f;
        /// <summary>The middle of each gait's hip swing, and how far the thigh sweeps while the foot is down (degrees).</summary>
        public const float WalkMeanHip = 7.5f, RunMeanHip = 13f, WalkSweep = 33f, RunSweep = 48f;

        /// <summary>
        /// How much to scale the hip swing so the planted foot doesn't slide: the body moves speed/cadence metres
        /// per step, and the leg must sweep that far under it.
        /// </summary>
        public static float SweepFor(float speed, float cadence, float run)
        {
            if (speed < 0.05f || cadence <= 0f) return 1f;
            float travel = speed / cadence;
            float needed = 2f * Mathf.Asin(Mathf.Clamp(travel * 0.5f / (Thigh + Shin), 0f, 0.9f)) * Mathf.Rad2Deg;
            return Mathf.Clamp(needed / Mathf.Lerp(WalkSweep, RunSweep, run), 0.6f, 1.35f);
        }

        // Percent of the gait cycle, and the angle there.
        static readonly float[] walkT = { 0f, 12f, 30f, 50f, 60f, 73f, 87f, 100f };
        static readonly float[] walkHip = { 25f, 22f, 8f, -10f, -2f, 18f, 28f, 25f };
        static readonly float[] walkKnee = { 4f, 18f, 8f, 6f, 36f, 62f, 28f, 4f };
        // Ankle: + plantarflexion (toes down), - dorsiflexion (toes up).
        static readonly float[] walkAnkle = { -2f, 6f, -10f, -4f, 16f, 6f, 0f, -2f };
        static readonly float[] runT = { 0f, 14f, 38f, 55f, 72f, 88f, 100f };
        static readonly float[] runAnkle = { -4f, -18f, 24f, 10f, -2f, -6f, -4f };
        static readonly float[] runHip = { 32f, 14f, -16f, 0f, 42f, 50f, 32f };
        static readonly float[] runKnee = { 22f, 42f, 18f, 88f, 110f, 55f, 22f };

        /// <summary>
        /// Hip and knee at <paramref name="cycle"/> (0..1, our own stride where stance is the first half), blending
        /// walking into running by <paramref name="run"/>.
        /// </summary>
        public static void Sample(float cycle, float run, out float hip, out float knee)
        {
            float ankle;
            Sample(cycle, run, out hip, out knee, out ankle);
        }

        /// <summary>...and the ankle.</summary>
        public static void Sample(float cycle, float run, out float hip, out float knee, out float ankle)
        {
            // Our stride spends half its time on each foot; map that onto each gait's real stance/swing split.
            float wPct = Map(cycle, 60f), rPct = Map(cycle, 38f);
            hip = Mathf.Lerp(Curve(walkT, walkHip, wPct), Curve(runT, runHip, rPct), run);
            knee = Mathf.Lerp(Curve(walkT, walkKnee, wPct), Curve(runT, runKnee, rPct), run);
            ankle = Mathf.Lerp(Curve(walkT, walkAnkle, wPct), Curve(runT, runAnkle, rPct), run);
        }

        static float Map(float cycle, float stance)
        {
            return cycle < 0.5f ? cycle / 0.5f * stance : stance + (cycle - 0.5f) / 0.5f * (100f - stance);
        }

        /// <summary>A smooth periodic curve through the keys (Catmull-Rom, wrapping round).</summary>
        static float Curve(float[] t, float[] v, float pct)
        {
            int n = t.Length - 1; // the last key repeats the first
            pct = Mathf.Repeat(pct, 100f);
            int i = 0;
            while (i < n - 1 && pct > t[i + 1]) i++;
            float u = (pct - t[i]) / Mathf.Max(0.001f, t[i + 1] - t[i]);
            float p0 = v[(i - 1 + n) % n], p1 = v[i], p2 = v[i + 1], p3 = v[(i + 2) % n];
            float u2 = u * u, u3 = u2 * u;
            return 0.5f * (2f * p1 + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u2 + (-p0 + 3f * p1 - 3f * p2 + p3) * u3);
        }

        /// <summary>How far below the hip the foot is, for a leg swung by <paramref name="legX"/> (Euler X, -forward) and a knee bend.</summary>
        public static float Reach(float legX, float knee)
        {
            float thigh = legX * Mathf.Deg2Rad, shin = (legX + knee) * Mathf.Deg2Rad;
            return Thigh * Mathf.Cos(thigh) + Shin * Mathf.Cos(shin);
        }
    }
}
