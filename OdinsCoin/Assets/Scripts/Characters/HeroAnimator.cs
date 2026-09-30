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
    /// legs and arms +X swing back, -X forward; body +X leans forward, +Z leans left.</para>
    /// </summary>
    public class HeroAnimator
    {
        public struct Frame
        {
            public Vector3 body, head, leftLeg, rightLeg, leftArm, rightArm;
            public float leftElbow, rightElbow;
            /// <summary>How far the whole body is lifted (m): the bob of each step, the dip of a landing.</summary>
            public float lift;
        }

        public Frame pose;

        readonly Damped bodyPitch = new Damped(), bodyRoll = new Damped(), bodyYaw = new Damped(), lift = new Damped();
        readonly Damped headPitch = new Damped(), headYaw = new Damped();
        readonly Damped legL = new Damped(), legR = new Damped(), legSplay = new Damped();
        readonly Damped armL = new Damped(), armR = new Damped(), armOut = new Damped();
        readonly Damped elbowL = new Damped(), elbowR = new Damped();
        readonly Damped air = new Damped(), stride = new Damped(), bobSize = new Damped(), fall = new Damped();
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

            // Legs: step with the footsteps; bigger strides the faster we go, small ones when pivoting.
            // (The size of the stride eases in and out too, so starting, stopping and leaving the ground never snap.)
            float reachTarget = stepping ? Mathf.Lerp(14f, 42f, g) : 0f;
            if (loco.Pivoting && grounded) reachTarget = Mathf.Max(reachTarget, 16f);
            float reach = stride.Step(reachTarget, dt, 0.1f);
            float swing = Mathf.Sin(a) * reach;
            // Tucked up in the air: knees forward, one more than the other; reaching down again as it falls.
            float falling = fall.Step(Mathf.Clamp01(-verticalSpeed / 6f), dt, 0.1f);
            float tuckL = Mathf.Lerp(-38f, -12f, falling), tuckR = Mathf.Lerp(-14f, 6f, falling);
            float wl = swimming ? Mathf.Sin(time * 4f) * 30f : Mathf.Lerp(swing, tuckL, air.value) - squash * 18f;
            float wr = swimming ? -Mathf.Sin(time * 4f) * 30f : Mathf.Lerp(-swing, tuckR, air.value) - squash * 18f;
            pose.leftLeg = new Vector3(legL.Step(wl, dt, 0.05f), 0f, 0f);
            pose.rightLeg = new Vector3(legR.Step(wr, dt, 0.05f), 0f, 0f);
            float splay = legSplay.Step(air.value * 7f + squash * 6f, dt, 0.08f);
            pose.leftLeg.z = -splay; pose.rightLeg.z = splay;

            // Arms: against the legs, swinging wider when running; pumping with bent elbows at a sprint; thrown up
            // and out for balance in the air.
            float armSwing = swing * Mathf.Lerp(0.7f, 1.25f, g);
            float airArm = Mathf.Lerp(-55f, -95f, falling);
            float al = Mathf.Lerp(-armSwing, airArm, air.value) + squash * 10f;
            float ar = Mathf.Lerp(armSwing, airArm * 0.8f, air.value) + squash * 10f;
            pose.leftArm = new Vector3(armL.Step(al, dt, 0.06f), 0f, 0f);
            pose.rightArm = new Vector3(armR.Step(ar, dt, 0.06f), 0f, 0f);
            float outward = armOut.Step(Mathf.Lerp(4f + 4f * g, 28f, air.value) + squash * 12f, dt, 0.08f);
            pose.leftArm.z = outward; pose.rightArm.z = -outward;
            float el = Mathf.Lerp(HeroPose.WalkElbow(pose.leftArm.x), -80f - 10f * Mathf.Sin(a), sprint);
            float er = Mathf.Lerp(HeroPose.WalkElbow(pose.rightArm.x), -80f + 10f * Mathf.Sin(a), sprint);
            pose.leftElbow = elbowL.Step(Mathf.Lerp(el, -45f, air.value), dt, 0.06f);
            pose.rightElbow = elbowR.Step(Mathf.Lerp(er, -45f, air.value), dt, 0.06f);

            // Body: leans into its speed (and harder while speeding up), banks into turns, twists a little
            // against the stride, bobs twice a stride, breathes when still, folds over a hard landing.
            float lean = swimming ? 60f : Mathf.Lerp(0f, 10f, g) + Mathf.Clamp(loco.acceleration * 1.1f, -7f, 9f)
                         + (g < 0.05f ? HeroPose.Breath(time) : 0f) + squash * 16f - air.value * 4f;
            float bank = -Mathf.Clamp(loco.turnRate * 0.05f * (0.3f + g), -12f, 12f);
            float twist = Mathf.Sin(a) * Mathf.Lerp(2f, 7f, g) * Mathf.Clamp01(reach / 14f);
            pose.body = new Vector3(bodyPitch.Step(lean, dt, 0.12f), bodyYaw.Step(twist, dt, 0.06f), bodyRoll.Step(bank, dt, 0.15f));
            float bob = (0.5f - 0.5f * Mathf.Cos(2f * a)) * bobSize.Step(stepping ? Mathf.Lerp(0.008f, 0.04f, g) : 0f, dt, 0.1f);
            pose.lift = lift.Step(bob - squash * 0.07f, dt, 0.04f);

            // Head: keeps looking ahead (undoing most of the lean) and turns towards where we're going before the
            // body does.
            float look = Mathf.Clamp(Mathf.DeltaAngle(loco.heading, loco.Desired) * 0.6f, -40f, 40f);
            pose.head = new Vector3(headPitch.Step(-pose.body.x * 0.6f, dt, 0.1f), headYaw.Step(look, dt, 0.12f), -pose.body.z * 0.5f);
        }

        /// <summary>Put the pose on a hero's joints.</summary>
        public void Apply(VikingBuilder.Parts parts)
        {
            if (parts == null) return;
            parts.body.localRotation = Quaternion.Euler(HeroPose.Lean + pose.body.x, pose.body.y, pose.body.z);
            parts.body.localPosition = new Vector3(0f, pose.lift, 0f);
            parts.leftLeg.localRotation = Quaternion.Euler(pose.leftLeg.x - HeroPose.Lean, 0f, pose.leftLeg.z);
            parts.rightLeg.localRotation = Quaternion.Euler(pose.rightLeg.x - HeroPose.Lean, 0f, pose.rightLeg.z);
            parts.leftArm.localRotation = Quaternion.Euler(pose.leftArm);
            parts.rightArm.localRotation = Quaternion.Euler(pose.rightArm);
            HeroPose.Elbows(parts, pose.leftElbow, pose.rightElbow);
            if (parts.head != null) parts.head.localRotation = Quaternion.Euler(HeroPose.HeadUp + pose.head.x, pose.head.y, pose.head.z);
        }
    }
}
