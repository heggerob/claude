using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// A hanging thing (cape, braid, banner) swinging on its pivot like a damped pendulum. Pure maths so it can be
    /// tested. Angles in degrees: <c>pitch</c> about the pivot's X (positive swings the free end backwards, -Z),
    /// <c>roll</c> about Z (positive swings it to +X).
    /// </summary>
    public class SwingSpring
    {
        /// <summary>How hard it's pulled back to hanging straight (1/s²).</summary>
        public float stiffness = 40f;
        /// <summary>How quickly swinging dies down (1/s).</summary>
        public float damping = 7f;
        /// <summary>How strongly the pivot's acceleration throws it (degrees per m/s²).</summary>
        public float inertia = 6f;
        /// <summary>Air drag: how far it trails behind at speed.</summary>
        public float drag = 0.06f;
        public float minPitch = -8f, maxPitch = 70f, maxRoll = 45f;

        public float pitch, roll;
        float pitchVel, rollVel;

        /// <summary>
        /// Advance by <paramref name="dt"/>, given the pivot's velocity and acceleration in the pivot's parent space
        /// (+Z forward, +X right).
        /// </summary>
        public void Step(float dt, Vector3 localVelocity, Vector3 localAccel)
        {
            if (dt <= 0f) return;
            // Where it would settle at this speed: trailing behind, against the direction of travel.
            float targetPitch = Mathf.Atan2(drag * localVelocity.z * Mathf.Abs(localVelocity.z), 9.81f) * Mathf.Rad2Deg;
            float targetRoll = -Mathf.Atan2(drag * localVelocity.x * Mathf.Abs(localVelocity.x), 9.81f) * Mathf.Rad2Deg;
            // A few small steps keep the spring stable at low frame rates.
            int steps = Mathf.Clamp(Mathf.CeilToInt(dt / 0.01f), 1, 8);
            float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                float pa = -stiffness * (pitch - targetPitch) - damping * pitchVel + inertia * localAccel.z;
                float ra = -stiffness * (roll - targetRoll) - damping * rollVel - inertia * localAccel.x;
                pitchVel += pa * h;
                rollVel += ra * h;
                pitch += pitchVel * h;
                roll += rollVel * h;
                if (pitch < minPitch) { pitch = minPitch; if (pitchVel < 0f) pitchVel = 0f; }
                if (pitch > maxPitch) { pitch = maxPitch; if (pitchVel > 0f) pitchVel = 0f; }
                if (Mathf.Abs(roll) > maxRoll) { roll = Mathf.Sign(roll) * maxRoll; rollVel = 0f; }
            }
        }

        public Quaternion Rotation { get { return Quaternion.Euler(pitch, 0f, roll); } }

        /// <summary>Tuned presets for the kinds of things that swing.</summary>
        public static SwingSpring For(SwingKind kind)
        {
            switch (kind)
            {
                case SwingKind.Cape: return new SwingSpring { stiffness = 28f, damping = 5f, inertia = 7f, drag = 0.09f, minPitch = -4f, maxPitch = 65f, maxRoll = 30f };
                case SwingKind.Banner: return new SwingSpring { stiffness = 45f, damping = 6f, inertia = 5f, drag = 0.05f, minPitch = -25f, maxPitch = 40f, maxRoll = 35f };
                case SwingKind.Braid: return new SwingSpring { stiffness = 60f, damping = 5f, inertia = 9f, drag = 0.07f, minPitch = -35f, maxPitch = 50f, maxRoll = 45f };
                default: return new SwingSpring();
            }
        }
    }

    public enum SwingKind { Cape, Banner, Braid }

    /// <summary>
    /// Drives every swinging joint of a character: works out how each pivot moves from frame to frame and lets
    /// its spring swing the joint. Added by <see cref="HeroBuilder"/>.
    /// </summary>
    public class ClothSway : MonoBehaviour
    {
        class Entry
        {
            public Transform joint;
            public SwingSpring spring;
            public Vector3 lastPos, lastVel;
            public bool primed;
        }

        readonly List<Entry> entries = new List<Entry>();

        public void Add(Transform joint, SwingKind kind)
        {
            entries.Add(new Entry { joint = joint, spring = SwingSpring.For(kind) });
        }

        public int Count { get { return entries.Count; } }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            if (dt <= 0f) return;
            foreach (var e in entries)
            {
                if (e.joint == null) continue;
                var parent = e.joint.parent;
                Vector3 pos = e.joint.position;
                if (!e.primed) { e.lastPos = pos; e.lastVel = Vector3.zero; e.primed = true; continue; }
                Vector3 vel = (pos - e.lastPos) / dt;
                Vector3 acc = (vel - e.lastVel) / dt;
                e.lastPos = pos;
                e.lastVel = vel;
                // Teleports (respawning, climbing aboard) would fling everything: ignore absurd jumps.
                if (vel.sqrMagnitude > 400f) { e.lastVel = Vector3.zero; continue; }
                Quaternion toLocal = parent != null ? Quaternion.Inverse(parent.rotation) : Quaternion.identity;
                e.spring.Step(dt, toLocal * vel, toLocal * Vector3.ClampMagnitude(acc, 60f));
                e.joint.localRotation = e.spring.Rotation;
            }
        }
    }
}
