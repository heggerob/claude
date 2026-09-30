using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// A ship of one of the new classes, sailing by <see cref="ShipPhysics"/>: it floats on the waves by the
    /// buoyancy of each patch of its bottom (so it pitches, rolls and heels by itself), and the water, wind, sails,
    /// oars and rudder push it as the design's real numbers say. It runs aground where the real sea floor
    /// (<see cref="WorldMap"/>) is shallower than its keel. The crew sets <see cref="Controls"/>.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Sailship : MonoBehaviour
    {
        public ShipDesign Design { get; private set; }
        public Rigidbody Body { get; private set; }
        /// <summary>What the crew wants: the rudder and sails move towards it at a crew's pace.</summary>
        public ShipPhysics.Controls Controls;
        /// <summary>Where the rudder and sails actually are.</summary>
        public ShipPhysics.Controls Actual;
        public bool Aground { get; private set; }
        /// <summary>Speed through the water, forward (m/s).</summary>
        public float Speed { get; private set; }
        public float Knots { get { return ShipPhysics.Knots(Speed); } }

        List<ShipPhysics.FloatCell> cells;
        float strokePhase;

        /// <summary>How fast the helm and the sails answer: the rudder swings over in about 2 s, sails set or furl in about 8 s.</summary>
        public const float RudderRate = 0.5f, SailRate = 0.12f, OarRate = 0.8f;

        public static Sailship Create(Transform parent, ShipDesign design, ShipLook look, Vector3 position, float heading)
        {
            var go = new GameObject(design.title);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, heading, 0f);
            var rb = go.AddComponent<Rigidbody>();
            float m = design.Mass;
            rb.mass = m;
            rb.useGravity = true;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            // The weight sits low (ballast and the keel), and she is long and heavy to swing round.
            rb.centerOfMass = new Vector3(0f, design.cgHeight * (design.draught + design.freeboard) - design.draught, 0f);
            rb.inertiaTensor = new Vector3(m * Sq(0.25f * design.length), m * Sq(0.22f * design.length) * 1.3f, m * Sq(0.35f * design.beam));
            rb.inertiaTensorRotation = Quaternion.identity;
            rb.maxAngularVelocity = 3f;
            var ship = go.AddComponent<Sailship>();
            ship.Design = design;
            ship.Body = rb;
            ship.cells = ShipPhysics.FloatCells(design);
            Show(ShipModel.Build(design, look), go.transform);
            WorldOrigin.Roots.Add(go.transform);
            return ship;
        }

        static float Sq(float x) { return x * x; }

        /// <summary>Put a built model's pieces into the scene under <paramref name="root"/>.</summary>
        static void Show(VikingModel model, Transform root)
        {
            foreach (var piece in model.Pieces)
            {
                var go = new GameObject(piece.ink ? "Ink" : "Part");
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = piece.mesh.ToMesh(model.Joints.Count > 0 ? model.Joints[0].name : "Ship");
                go.AddComponent<MeshRenderer>().sharedMaterial = piece.ink ? Materials.Get(piece.color, 0f) : Materials.GetDrawn(piece.color, piece.surface);
            }
            if (root.GetComponent<OwnInk>() == null) root.gameObject.AddComponent<OwnInk>();
        }

        void FixedUpdate()
        {
            if (Design == null) return;
            float dt = Time.fixedDeltaTime;
            var t = transform;

            // The crew works the helm, sails and oars towards what's wanted.
            Actual.rudder = Mathf.MoveTowards(Actual.rudder, Mathf.Clamp(Controls.rudder, -1f, 1f), RudderRate * dt);
            Actual.sail = Mathf.MoveTowards(Actual.sail, Mathf.Clamp01(Controls.sail), SailRate * dt);
            Actual.oarsPort = Mathf.MoveTowards(Actual.oarsPort, Mathf.Clamp(Controls.oarsPort, -1f, 1f), OarRate * dt);
            Actual.oarsStarboard = Mathf.MoveTowards(Actual.oarsStarboard, Mathf.Clamp(Controls.oarsStarboard, -1f, 1f), OarRate * dt);
            strokePhase = Mathf.Repeat(strokePhase + ShipPhysics.StrokeRate / 60f * dt, 1f);

            // Buoyancy: each patch of the bottom pushes up by the water it displaces, damped as it bobs.
            int wet = 0;
            float dampingRatio = 0.35f;
            foreach (var c in cells)
            {
                Vector3 p = t.TransformPoint(c.at);
                float depth = Waves.Height(p.x, p.z) - p.y;
                if (depth <= 0f) continue;
                wet++;
                float k = ShipPhysics.RhoWater * ShipPhysics.G * c.area;
                float cellMass = Design.Mass * c.area / (Design.blockCoef * Design.length * Design.beam);
                float damping = 2f * dampingRatio * Mathf.Sqrt(k * cellMass);
                float vy = Body.GetPointVelocity(p).y;
                Body.AddForceAtPosition(Vector3.up * Mathf.Max(0f, ShipPhysics.CellBuoyancy(Design, c, depth) - damping * vy), p);
            }
            if (wet == 0) return; // off the top of a wave: nothing to push against

            // Water, wind, sails, oars and rudder, in the ship's own frame.
            Vector3 vel = t.InverseTransformDirection(Compat.Velocity(Body));
            Vector3 ang = t.InverseTransformDirection(Body.angularVelocity);
            Vector3 wind = t.InverseTransformDirection(Wind.Direction * Wind.Knots * 0.514f);
            Speed = vel.z;
            var f = ShipPhysics.Total(Design, vel.z, vel.x, ang.y, new Vector2(wind.x, wind.z), Actual, strokePhase);
            // The water the hull drags along makes her heavier to push sideways and to turn than ahead.
            Vector3 flatForward = t.forward; flatForward.y = 0f; flatForward.Normalize();
            Vector3 flatRight = new Vector3(flatForward.z, 0f, -flatForward.x);
            Body.AddForce(flatForward * (f.fz / 1.05f) + flatRight * (f.fx / 1.8f));
            Body.AddTorque(Vector3.up * (f.mz / 1.3f));
            // The sails' side force, high up, heels her over.
            Body.AddRelativeTorque(new Vector3(0f, 0f, -f.heel));
            // Rolling and pitching die away in the water.
            Body.AddRelativeTorque(new Vector3(-ang.x, 0f, -ang.z) * Design.Mass * Design.beam * 0.6f);

            // Aground: where the real sea floor comes up under her keel, she grinds to a stop.
            Aground = false;
            var map = WorldMap.Current;
            if (map != null)
            {
                Vector3 keel = t.TransformPoint(new Vector3(0f, -Design.draught, Design.length * 0.3f));
                float floor = TerrainDetail.Height(map, WorldOrigin.GlobalX(keel), WorldOrigin.GlobalZ(keel));
                if (floor > keel.y)
                {
                    Aground = true;
                    Vector3 v = Compat.Velocity(Body);
                    v.y = 0f;
                    Body.AddForce(-v * Design.Mass * 1.5f + Vector3.up * Design.Mass * ShipPhysics.G * Mathf.Clamp01((floor - keel.y) / Design.draught) * 0.5f);
                }
            }
        }
    }

    /// <summary>
    /// Sailing a ship from the keyboard: A/D the helm, W/S the sails up and down, and the oars: Up rows ahead,
    /// Down backs water, and holding Up with the helm over rows one side harder so she turns faster.
    /// </summary>
    [RequireComponent(typeof(Sailship))]
    public class SailshipHelm : MonoBehaviour
    {
        Sailship ship;

        void Awake() { ship = GetComponent<Sailship>(); }

        void Update()
        {
            var c = ship.Controls;
            float steer = GameInput.Move().x;
            c.rudder = steer;
            if (GameInput.Pressed(Key.SailUp)) c.sail = Mathf.Min(1f, c.sail + 0.25f);
            if (GameInput.Pressed(Key.SailDown)) c.sail = Mathf.Max(0f, c.sail - 0.25f);
            float row = GameInput.Held(Key.Up) ? 1f : GameInput.Held(Key.Down) ? -1f : 0f;
            // Turning while rowing: the outside of the turn pulls harder, the inside eases.
            c.oarsPort = Mathf.Clamp(row + steer * 0.6f * Mathf.Abs(row), -1f, 1f);
            c.oarsStarboard = Mathf.Clamp(row - steer * 0.6f * Mathf.Abs(row), -1f, 1f);
            if (Mathf.Abs(row) < 0.01f && Mathf.Abs(steer) > 0.5f && GameInput.Held(Key.Sprint))
            {
                // Spin her where she lies: one side pulls, the other backs.
                c.oarsPort = steer;
                c.oarsStarboard = -steer;
            }
            ship.Controls = c;
        }
    }
}
