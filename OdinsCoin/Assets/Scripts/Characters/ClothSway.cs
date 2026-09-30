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
        public void Step(float dt, Vector3 localVelocity, Vector3 localAccel) { Step(dt, localVelocity, localAccel, 0f, 0f); }

        /// <summary>
        /// ...with <paramref name="localVelocity"/> taken relative to the air (so wind counts), plus a flutter:
        /// extra degrees of pitch and roll the gusts add this instant.
        /// </summary>
        public void Step(float dt, Vector3 localVelocity, Vector3 localAccel, float flutterPitch, float flutterRoll)
        {
            if (dt <= 0f) return;
            // The gravity it feels: less when the pivot drops away under it (a jump's fall, ~zero or even "up"
            // since heroes fall faster than cloth), more when the pivot is stopped hard (landing).
            float felt = Mathf.Clamp(9.81f + localAccel.y, -4f, 40f);
            // Falling through the air pushes it up behind; rising presses it down.
            float lift = drag * -localVelocity.y * Mathf.Abs(localVelocity.y);
            // Where it would settle: trailing behind against the direction of travel, floating up in a fall.
            float targetPitch = Mathf.Atan2(drag * localVelocity.z * Mathf.Abs(localVelocity.z) + lift, felt) * Mathf.Rad2Deg + flutterPitch;
            float targetRoll = -Mathf.Atan2(drag * localVelocity.x * Mathf.Abs(localVelocity.x), Mathf.Max(2f, felt)) * Mathf.Rad2Deg + flutterRoll;
            // With little gravity to pull it straight it drifts lazily; slammed down by a landing it snaps.
            float pull = stiffness * Mathf.Clamp(Mathf.Abs(felt) / 9.81f, 0.3f, 2.5f);
            // A few small steps keep the spring stable at low frame rates.
            int steps = Mathf.Clamp(Mathf.CeilToInt(dt / 0.01f), 1, 8);
            float h = dt / steps;
            for (int i = 0; i < steps; i++)
            {
                float pa = -pull * (pitch - targetPitch) - damping * pitchVel + inertia * localAccel.z;
                float ra = -pull * (roll - targetRoll) - damping * rollVel - inertia * localAccel.x;
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
    /// Bends a hanging cloth instead of swinging it as one stiff board: the part by the pivot follows the body
    /// and each point further down takes more of the swing, so a cape drapes from the shoulders and flares out
    /// towards its hem. Pure maths, shared by the game and the preview.
    /// </summary>
    public static class ClothBend
    {
        /// <summary>How much of the swing a point takes at depth fraction <paramref name="t"/> (0 at the pivot, 1 at the hem).</summary>
        public static float Share(float t) { return Mathf.Pow(Mathf.Clamp01(t), 0.8f); }

        /// <summary>How far below its pivot (at the origin) a cloth hangs: the depth of its lowest point.</summary>
        public static float Length(IEnumerable<Vector3> points)
        {
            float d = 0f;
            foreach (var p in points) d = Mathf.Max(d, -p.y);
            return d;
        }

        /// <summary>The turn a point at rest position <paramref name="rest"/> takes (angles in degrees, as <see cref="SwingSpring"/>).</summary>
        public static Quaternion Turn(Vector3 rest, float length, float pitch, float roll)
        {
            float k = length > 1e-4f ? Share(-rest.y / length) : 1f;
            return Quaternion.Euler(pitch * k, 0f, roll * k);
        }

        public static Vector3 Apply(Vector3 rest, float length, float pitch, float roll) { return Turn(rest, length, pitch, roll) * rest; }
    }

    /// <summary>The air around the characters: how the sea wind reaches capes and braids.</summary>
    public static class ClothWind
    {
        /// <summary>How much of the open-sea wind a person feels (sheltered by the hull, the hall, the land).</summary>
        public const float Shelter = 0.5f;

        /// <summary>The air's velocity in m/s, world space. Tests and the preview can swap it.</summary>
        public static System.Func<Vector3> Air = () => Wind.Direction * Wind.Knots * 0.514f * Shelter;

        /// <summary>
        /// Gusts: a flutter angle in degrees for wind speed <paramref name="speed"/> (m/s) at time
        /// <paramref name="t"/>, different for each swinging thing (<paramref name="phase"/>). Grows with the wind.
        /// </summary>
        public static float Flutter(float speed, float t, float phase)
        {
            float k = Mathf.Clamp(speed, 0f, 12f) * 0.9f;
            return k * (0.6f * Mathf.Sin(t * 7.3f + phase) + 0.4f * Mathf.Sin(t * 12.1f + phase * 2.3f));
        }
    }

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
            public float phase;
            // A cape's or braid's meshes, bent rather than turned (see ClothBend); null for things that swing stiffly.
            public List<Bendable> bends;
            public float length;
        }

        class Bendable
        {
            public Mesh mesh;
            public Vector3[] rest, restNormals, verts, normals;
        }

        readonly List<Entry> entries = new List<Entry>();
        // Counts every swinging thing made, so no two flutter in step.
        static int made;

        public void Add(Transform joint, SwingKind kind)
        {
            var e = new Entry { joint = joint, spring = SwingSpring.For(kind), phase = (made++) * 2.1f + joint.name.Length * 0.37f };
            // Capes and braids bend: keep each of their meshes' rest shape (in the joint's space: the pieces sit on it
            // untransformed). Banners stand out sideways from their pole, so they swing stiffly.
            if (kind == SwingKind.Cape || kind == SwingKind.Braid)
            {
                e.bends = new List<Bendable>();
                foreach (var mf in joint.GetComponentsInChildren<MeshFilter>(true))
                {
                    var mesh = mf.sharedMesh;
                    if (mesh == null || mesh.vertices == null) continue;
                    var b = new Bendable { mesh = mesh, rest = mesh.vertices, restNormals = mesh.normals };
                    b.verts = new Vector3[b.rest.Length];
                    if (b.restNormals != null && b.restNormals.Length == b.rest.Length) b.normals = new Vector3[b.rest.Length];
                    e.bends.Add(b);
                    e.length = Mathf.Max(e.length, ClothBend.Length(b.rest));
                }
            }
            entries.Add(e);
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
                // Moving through still air and standing in the wind look the same to a cape.
                Vector3 air = ClothWind.Air();
                float speed = air.magnitude, phase = e.phase;
                e.spring.Step(dt, toLocal * (vel - air), toLocal * Vector3.ClampMagnitude(acc, 60f),
                    ClothWind.Flutter(speed, Time.time, phase), ClothWind.Flutter(speed, Time.time, phase + 1.7f) * 0.5f);
                if (e.bends == null) { e.joint.localRotation = e.spring.Rotation; continue; }
                foreach (var b in e.bends)
                {
                    for (int i = 0; i < b.rest.Length; i++)
                    {
                        var turn = ClothBend.Turn(b.rest[i], e.length, e.spring.pitch, e.spring.roll);
                        b.verts[i] = turn * b.rest[i];
                        if (b.normals != null) b.normals[i] = turn * b.restNormals[i];
                    }
                    b.mesh.vertices = b.verts;
                    if (b.normals != null) b.mesh.normals = b.normals;
                    b.mesh.RecalculateBounds();
                }
            }
        }
    }
}
