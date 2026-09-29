using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The longship's physics: float points push it up out of the waves, the keel resists sliding sideways,
    /// the square sail pushes it with the wind and the steering oar turns it.
    /// Controls come from whoever stands at the helm (or directly from the keyboard for now).
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    public class Longship : MonoBehaviour
    {
        /// <summary>-1 (port) .. 1 (starboard).</summary>
        public float RudderInput;
        /// <summary>0 = furled, 1 = full sail. The sail eases towards this.</summary>
        public float SailTarget = 0.6f;
        public float SailAmount { get; private set; }
        public bool Rowing;

        public Rigidbody Body { get; private set; }
        public LongshipBuilder.Parts Parts { get; private set; }
        public float SpeedKnots { get { return ShipTuning.MetresPerSecondToKnots(Vector3.Dot(Compat.Velocity(Body), transform.forward)); } }
        public float Heading { get { return Mathf.Repeat(Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg, 360f); } }

        float rudderAngle;

        public static Longship Create(Transform parent, Vector3 position, float heading)
        {
            var go = new GameObject("Longship");
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, heading, 0f);
            var rb = go.AddComponent<Rigidbody>();
            rb.mass = ShipTuning.Mass;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.centerOfMass = new Vector3(0f, -0.6f, 0f); // ballast keeps her upright
            rb.maxAngularVelocity = 2f;
            var ship = go.AddComponent<Longship>();
            ship.Body = rb;
            ship.Parts = LongshipBuilder.Build(go.transform);
            ship.SailAmount = ship.SailTarget;
            return ship;
        }

        void Awake() { if (Body == null) Body = GetComponent<Rigidbody>(); }

        void FixedUpdate()
        {
            var t = transform;
            float dt = Time.fixedDeltaTime;

            // Buoyancy at each float point.
            int submerged = 0;
            foreach (var local in Parts.floatPoints)
            {
                Vector3 p = t.TransformPoint(local);
                float depth = Waves.Height(p.x, p.z) - p.y;
                if (depth <= 0f) continue;
                submerged++;
                float vy = Body.GetPointVelocity(p).y;
                Body.AddForceAtPosition(Vector3.up * ShipTuning.Buoyancy(depth, vy), p);
            }
            if (submerged == 0) return; // airborne off a big wave: nothing to push against

            // Water drag, split along the hull: slippery forwards, stubborn sideways.
            Vector3 v = t.InverseTransformDirection(Compat.Velocity(Body));
            Vector3 drag = new Vector3(
                -v.x * Mathf.Abs(v.x) * ShipTuning.SideDrag - v.x * 1500f,
                0f,
                -v.z * Mathf.Abs(v.z) * ShipTuning.ForwardDrag);
            Body.AddForce(t.TransformDirection(drag));
            Body.AddTorque(-Body.angularVelocity * ShipTuning.Mass * 1.2f);

            // Sail and oars.
            SailAmount = Mathf.MoveTowards(SailAmount, Mathf.Clamp01(SailTarget), dt * 0.35f);
            float thrust = ShipTuning.SailThrust(SailAmount, t.forward);
            if (Rowing && SailAmount < 0.15f) thrust += ShipTuning.RowThrust;
            Vector3 forwardFlat = t.forward;
            forwardFlat.y = 0f;
            Body.AddForce(forwardFlat.normalized * thrust);

            // Steering oar: needs water flowing past it, so it bites harder at speed.
            rudderAngle = Mathf.MoveTowards(rudderAngle, Mathf.Clamp(RudderInput, -1f, 1f), dt * 1.5f);
            float flow = Mathf.Clamp(v.z, -2f, 9f) + (Rowing ? 1.5f : 0f);
            Body.AddTorque(Vector3.up * rudderAngle * flow * ShipTuning.RudderTorque);
        }

        void Update()
        {
            // Visuals: sail rolls up, yard braces round to the wind, steering oar swings.
            if (Parts.sail != null) Parts.sail.localScale = new Vector3(1f, Mathf.Max(0.08f, SailAmount), 1f);
            if (Parts.yard != null)
            {
                Vector3 localWind = transform.InverseTransformDirection(Wind.Direction);
                float brace = Mathf.Clamp(Mathf.Atan2(localWind.x, localWind.z) * Mathf.Rad2Deg * 0.5f, -35f, 35f);
                Parts.yard.localRotation = Quaternion.Slerp(Parts.yard.localRotation, Quaternion.Euler(0f, brace, 0f), Time.deltaTime * 2f);
            }
            if (Parts.rudder != null) Parts.rudder.localRotation = Quaternion.Euler(0f, -rudderAngle * 30f, 0f);
        }
    }

    /// <summary>Temporary direct helm control until the Viking can walk to the steering oar (roadmap item 3).</summary>
    [RequireComponent(typeof(Longship))]
    public class ShipKeyboardHelm : MonoBehaviour
    {
        Longship ship;

        void Awake() { ship = GetComponent<Longship>(); }

        void Update()
        {
            ship.RudderInput = GameInput.Move().x;
            if (GameInput.Pressed(Key.SailUp)) ship.SailTarget = Mathf.Min(1f, ship.SailTarget + 0.25f);
            if (GameInput.Pressed(Key.SailDown)) ship.SailTarget = Mathf.Max(0f, ship.SailTarget - 0.25f);
            ship.Rowing = GameInput.Held(Key.Up) && ship.SailTarget < 0.15f;
        }
    }
}
