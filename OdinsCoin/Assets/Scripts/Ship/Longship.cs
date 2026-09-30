using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// A ship you sail and walk on. Either the classic longship (float points push it out of the waves, the keel
    /// resists sliding sideways, the square sail pushes it with the wind and the steering oar turns it), or one of
    /// the new classes (<see cref="Design"/>), which floats on each patch of its bottom and is pushed by the
    /// water, wind, sails, oars and rudder as <see cref="ShipPhysics"/> works out from the design's real numbers.
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
        /// <summary>The player's own ship: upgrades, blessings and curses apply to it, and it can flood.</summary>
        public bool PlayerShip;
        public readonly HullWater Hull = new HullWater();

        public Rigidbody Body { get; private set; }
        public LongshipBuilder.Parts Parts { get; private set; }
        /// <summary>The new ship class she is, or null for the classic longship.</summary>
        public ShipDesign Design { get; private set; }
        /// <summary>The new classes: where the rudder, sails and oars actually are (the crew works them towards the controls).</summary>
        public ShipPhysics.Controls Actual;
        /// <summary>The new classes: run aground on the real sea floor.</summary>
        public bool Aground { get; private set; }
        List<ShipPhysics.FloatCell> cells;
        float strokePhase;

        /// <summary>The new classes: lying to her anchor.</summary>
        public bool Anchored { get; private set; }
        /// <summary>How fast the anchor is dragging over the bottom (m/s; 0 when it holds).</summary>
        public float AnchorDragging { get; private set; }
        /// <summary>The depth she anchored in (m).</summary>
        public float AnchorDepth { get; private set; }
        double anchorX, anchorZ;
        /// <summary>Water coming in over a rail that's gone under (m³/s).</summary>
        public float Shipping { get; private set; }
        Transform bowBollard, sternBollard;
        float bowLine, sternLine;
        /// <summary>Made fast to a jetty's bollards.</summary>
        public bool Moored { get { return bowBollard != null || sternBollard != null; } }

        /// <summary>Half her length (m).</summary>
        public float HalfLength { get { return Design != null ? Design.length / 2f : LongshipBuilder.Length / 2f; } }
        /// <summary>The deck's height in her own space (m above the waterline).</summary>
        public float DeckY { get { return Design != null ? DesignedShipBuilder.DeckY(Design) : LongshipBuilder.DeckHeight; } }
        /// <summary>The rail's height amidships (m above the waterline).</summary>
        public float Freeboard { get { return Design != null ? Design.freeboard : LongshipBuilder.Freeboard; } }
        public float Beam { get { return Design != null ? Design.beam : LongshipBuilder.Beam; } }

        /// <summary>Half-width, keel depth and gunwale height at s in -1 (stern) .. 1 (bow).</summary>
        public void Station(float s, out float halfWidth, out float keel, out float gunwale)
        {
            if (Design != null) ShipModel.Section(Design, s, out keel, out gunwale, out halfWidth);
            else LongshipBuilder.Station(s, out halfWidth, out keel, out gunwale);
        }

        /// <summary>Is a point (in her own space) inside her hull?</summary>
        public bool InsideHull(Vector3 local)
        {
            if (Mathf.Abs(local.z) > HalfLength) return false;
            float hw, k, g;
            Station(local.z / HalfLength, out hw, out k, out g);
            return Mathf.Abs(local.x) < hw + 0.3f && local.y > k - 0.5f && local.y < g + 1.5f;
        }
        public float SpeedKnots { get { return ShipTuning.MetresPerSecondToKnots(Vector3.Dot(Compat.Velocity(Body), transform.forward)); } }
        public float Heading { get { return Mathf.Repeat(Mathf.Atan2(transform.forward.x, transform.forward.z) * Mathf.Rad2Deg, 360f); } }

        float rudderAngle;

        /// <summary>Sail down at once (moored in harbour).</summary>
        public void Furl()
        {
            SailTarget = 0f;
            SailAmount = 0f;
        }

        public static Longship Create(Transform parent, Vector3 position, float heading)
        {
            return Create(parent, position, heading, Materials.Sail, Materials.SailStripe);
        }

        public static Longship Create(Transform parent, Vector3 position, float heading, Color sail, Color stripe)
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
            ship.Parts = LongshipBuilder.Build(go.transform, sail, stripe);
            ship.SailAmount = ship.SailTarget;
            return ship;
        }

        /// <summary>One of the new ship classes, sailing by the real physics.</summary>
        public static Longship Create(Transform parent, Vector3 position, float heading, ShipDesign design, ShipLook look)
        {
            var go = new GameObject(design.title);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, heading, 0f);
            var rb = go.AddComponent<Rigidbody>();
            float m = design.Mass;
            rb.mass = m;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            // The weight sits low (ballast and the keel), and she is long and heavy to swing round.
            rb.centerOfMass = new Vector3(0f, design.cgHeight * (design.draught + design.freeboard) - design.draught, 0f);
            rb.inertiaTensor = new Vector3(m * Sq(0.25f * design.length), m * Sq(0.22f * design.length) * 1.3f, m * Sq(0.35f * design.beam));
            rb.inertiaTensorRotation = Quaternion.identity;
            rb.maxAngularVelocity = 3f;
            var ship = go.AddComponent<Longship>();
            ship.Body = rb;
            ship.Design = design;
            ship.cells = ShipPhysics.FloatCells(design);
            ship.Parts = DesignedShipBuilder.Build(go.transform, design, look);
            ship.SailAmount = ship.SailTarget;
            ship.Actual.sail = ship.SailTarget;
            return ship;
        }

        static float Sq(float x) { return x * x; }

        void Awake() { if (Body == null) Body = GetComponent<Rigidbody>(); }

        void FixedUpdate()
        {
            if (Design != null) { SailByPhysics(); return; }
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
            if (PlayerShip)
            {
                var up = Upgrades.Current;
                thrust = ShipTuning.SailThrust(SailAmount, t.forward) * up.SailMultiplier;
                if (Rowing && SailAmount < 0.15f) thrust += ShipTuning.RowThrust * up.OarMultiplier;
                thrust *= up.LeakMultiplier(Fortune.Current); // Rán's Net drags at the hull; a tarred hull resists
                thrust *= Hull.SpeedMultiplier;               // and so does water sloshing in the bilge
            }
            Vector3 forwardFlat = t.forward;
            forwardFlat.y = 0f;
            Body.AddForce(forwardFlat.normalized * thrust);

            // Steering oar: needs water flowing past it, so it bites harder at speed.
            rudderAngle = Mathf.MoveTowards(rudderAngle, Mathf.Clamp(RudderInput, -1f, 1f), dt * 1.5f);
            float flow = Mathf.Clamp(v.z, -2f, 9f) + (Rowing ? 1.5f : 0f);
            Body.AddTorque(Vector3.up * rudderAngle * flow * ShipTuning.RudderTorque);
        }

        /// <summary>What the crew is asked for, from the helm's rudder, sail and oars (with the ship's upgrades and woes).</summary>
        public ShipPhysics.Controls Wanted()
        {
            var c = new ShipPhysics.Controls();
            c.rudder = Mathf.Clamp(RudderInput, -1f, 1f);
            c.sail = Mathf.Clamp01(SailTarget);
            float row = Rowing ? 1f : 0f;
            // Turning while rowing: the outside of the turn pulls harder, the inside eases.
            c.oarsPort = Mathf.Clamp(row + c.rudder * 0.6f * row, -1f, 1f);
            c.oarsStarboard = Mathf.Clamp(row - c.rudder * 0.6f * row, -1f, 1f);
            return c;
        }

        /// <summary>How hard the sails and oars drive her: upgrades help, Rán's Net and a flooded bilge hold her back.</summary>
        public void Drive(out float sails, out float oars)
        {
            sails = oars = 1f;
            if (!PlayerShip) return;
            var up = Upgrades.Current;
            float woe = up.LeakMultiplier(Fortune.Current) * Hull.SpeedMultiplier;
            sails = up.SailMultiplier * woe;
            oars = up.OarMultiplier * woe;
        }

        /// <summary>The bow and stern cleats the mooring lines are made fast to, and the bow where the anchor rode runs out (her own space).</summary>
        Vector3 BowCleat { get { return new Vector3(0f, Freeboard, HalfLength * 0.8f); } }
        Vector3 SternCleat { get { return new Vector3(0f, Freeboard, -HalfLength * 0.8f); } }

        /// <summary>
        /// The one key for staying put: cast off if moored, weigh anchor if anchored, else make fast to a jetty if
        /// one's in reach, else let go the anchor. Returns what happened, for the HUD.
        /// </summary>
        public string AnchorOrMoor()
        {
            if (Design == null) return "This old hull carries no anchor.";
            if (Moored) { CastOff(); return "Lines cast off."; }
            if (Anchored) { WeighAnchor(); return "Anchor aweigh."; }
            string moor = MakeFast();
            return moor ?? DropAnchor();
        }

        /// <summary>Throw lines to the nearest bollards from the bow and stern; null if none is in reach.</summary>
        public string MakeFast()
        {
            var bow = transform.TransformPoint(BowCleat);
            var stern = transform.TransformPoint(SternCleat);
            var b = Seamanship.NearestBollard(bow, Seamanship.LineReach);
            var s = Seamanship.NearestBollard(stern, Seamanship.LineReach);
            if (b == null && s == null) return null;
            bowBollard = b;
            sternBollard = s;
            if (b != null) bowLine = Mathf.Max(1.5f, Vector3.Distance(b.position, bow) + 0.3f);
            if (s != null) sternLine = Mathf.Max(1.5f, Vector3.Distance(s.position, stern) + 0.3f);
            SailTarget = 0f;
            return b != null && s != null ? "Made fast, bow and stern." : "Made fast with one line.";
        }

        public void CastOff() { bowBollard = sternBollard = null; }

        /// <summary>Let the anchor go where the bow is. It only reaches the bottom in water up to <see cref="Seamanship.MaxAnchorDepth"/>.</summary>
        public string DropAnchor()
        {
            var bow = transform.TransformPoint(new Vector3(0f, 0f, HalfLength * 0.9f));
            float depth = DepthAt(bow);
            if (depth > Seamanship.MaxAnchorDepth) return string.Format("Too deep to anchor: {0:0} m. Find water under {1:0} m.", depth, Seamanship.MaxAnchorDepth);
            anchorX = WorldOrigin.GlobalX(bow);
            anchorZ = WorldOrigin.GlobalZ(bow);
            AnchorDepth = Mathf.Max(1f, depth);
            Anchored = true;
            return string.Format("Anchor down in {0:0} m.", depth);
        }

        public void WeighAnchor() { Anchored = false; AnchorDragging = 0f; }

        /// <summary>How deep the water is at a scene point (m): the real sea floor in the real North, else the storybook isles' shores.</summary>
        public static float DepthAt(Vector3 p)
        {
            // Close to home, the home island's own shore.
            var home = HomeHarbour.Spec;
            Vector2 local = new Vector2(p.x, p.z) - new Vector2(HomeHarbour.Drift.x, HomeHarbour.Drift.z);
            if (HomeHarbour.Instance != null && Vector2.Distance(local, home.centre) < home.radius * 1.6f)
                return Mathf.Max(0f, -Island.Height(home, local.x, local.y));
            if (RealWorld.Active && WorldMap.Current != null)
                return Mathf.Max(0f, -TerrainDetail.Height(WorldMap.Current, WorldOrigin.GlobalX(p), WorldOrigin.GlobalZ(p)));
            float depth = 25f;
            foreach (var island in WorldGen.Islands)
                if (island != null) depth = Mathf.Min(depth, Mathf.Max(0f, -Island.Height(island.Spec, p.x, p.z)));
            return depth;
        }

        /// <summary>The anchor rode and the mooring lines pull at her.</summary>
        void HoldFast(float dt)
        {
            var d = Design;
            AnchorDragging = 0f;
            if (Anchored)
            {
                var bow = transform.TransformPoint(new Vector3(0f, 0f, HalfLength * 0.9f));
                var anchor = WorldOrigin.ToScene(anchorX, anchorZ, 0f);
                var vel = Body.GetPointVelocity(bow);
                float drag;
                var pull = Seamanship.RodePull(d, new Vector2(anchor.x - bow.x, anchor.z - bow.z), new Vector2(vel.x, vel.z), AnchorDepth, out drag);
                Body.AddForceAtPosition(new Vector3(pull.x, 0f, pull.y), bow);
                if (drag > 0f)
                {
                    // The anchor ploughs through the mud towards her.
                    Vector2 toShip = new Vector2(bow.x - anchor.x, bow.z - anchor.z).normalized;
                    float slide = Mathf.Min(drag, 0.8f * dt); // mud lets it plough at most a slow walk
                    anchorX += toShip.x * slide;
                    anchorZ += toShip.y * slide;
                    AnchorDragging = slide / dt;
                }
            }
            if (bowBollard != null) Line(bowBollard, BowCleat, bowLine);
            if (sternBollard != null) Line(sternBollard, SternCleat, sternLine);
        }

        void Line(Transform bollard, Vector3 cleatLocal, float length)
        {
            var cleat = transform.TransformPoint(cleatLocal);
            var offset = bollard.position - cleat;
            offset.y = 0f;
            var v = Body.GetPointVelocity(cleat);
            v.y = 0f;
            Body.AddForceAtPosition(Seamanship.LinePull(Design, offset, v, length), cleat);
        }

        /// <summary>Water pours in wherever the rail is under the surface: heeled over in a blow, or burying her bow in a sea.</summary>
        void ShipWater(float dt)
        {
            var d = Design;
            const int stations = 10;
            float shipped = 0f, piece = d.length / stations;
            for (int i = 0; i < stations; i++)
            {
                float s = -0.9f + 1.8f * (i + 0.5f) / stations;
                float hw, k, g;
                Station(s, out hw, out k, out g);
                foreach (float side in new[] { -1f, 1f })
                {
                    var p = transform.TransformPoint(new Vector3(side * hw, g, s * HalfLength));
                    shipped += Seamanship.ShippedWater(Waves.Height(p.x, p.z) - p.y, piece);
                }
            }
            Shipping = shipped;
            if (PlayerShip && shipped > 0f) Hull.Flood(shipped * dt / Seamanship.OpenVolume(d));
        }

        void SailByPhysics()
        {
            var d = Design;
            float dt = Time.fixedDeltaTime;
            var t = transform;

            // The crew works the helm, sails and oars towards what's wanted.
            var want = Wanted();
            Actual.rudder = Mathf.MoveTowards(Actual.rudder, want.rudder, Sailship.RudderRate * dt);
            Actual.sail = Mathf.MoveTowards(Actual.sail, want.sail, Sailship.SailRate * dt);
            Actual.oarsPort = Mathf.MoveTowards(Actual.oarsPort, want.oarsPort, Sailship.OarRate * dt);
            Actual.oarsStarboard = Mathf.MoveTowards(Actual.oarsStarboard, want.oarsStarboard, Sailship.OarRate * dt);
            SailAmount = Actual.sail;
            rudderAngle = Actual.rudder;
            strokePhase = Mathf.Repeat(strokePhase + ShipPhysics.StrokeRate / 60f * dt, 1f);

            // Buoyancy: each patch of the bottom pushes up by the water it displaces, damped as it bobs.
            int wet = 0;
            foreach (var c in cells)
            {
                Vector3 p = t.TransformPoint(c.at);
                float depth = Waves.Height(p.x, p.z) - p.y;
                if (depth <= 0f) continue;
                wet++;
                float k = ShipPhysics.RhoWater * ShipPhysics.G * c.area;
                float cellMass = d.Mass * c.area / (d.blockCoef * d.length * d.beam);
                float damping = 2f * 0.35f * Mathf.Sqrt(k * cellMass);
                float vy = Body.GetPointVelocity(p).y;
                Body.AddForceAtPosition(Vector3.up * Mathf.Max(0f, ShipPhysics.CellBuoyancy(d, c, depth) - damping * vy), p);
            }
            if (wet == 0) return; // off the top of a wave: nothing to push against
            HoldFast(dt);
            ShipWater(dt);

            // Water, wind, sails, oars and rudder, in the ship's own frame.
            Vector3 vel = t.InverseTransformDirection(Compat.Velocity(Body));
            Vector3 ang = t.InverseTransformDirection(Body.angularVelocity);
            Vector3 wind = t.InverseTransformDirection(Wind.Direction * Wind.Knots * 0.514f);
            float sails, oars;
            Drive(out sails, out oars);
            var driven = Actual;
            driven.sail *= sails;
            driven.oarsPort *= oars;
            driven.oarsStarboard *= oars;
            var f = ShipPhysics.Total(d, vel.z, vel.x, ang.y, new Vector2(wind.x, wind.z), driven, strokePhase);
            // The water the hull drags along makes her heavier to push sideways and to turn than ahead.
            Vector3 flatForward = t.forward; flatForward.y = 0f; flatForward.Normalize();
            Vector3 flatRight = new Vector3(flatForward.z, 0f, -flatForward.x);
            Body.AddForce(flatForward * (f.fz / 1.05f) + flatRight * (f.fx / 1.8f));
            Body.AddTorque(Vector3.up * (f.mz / 1.3f));
            // The sails' side force, high up, heels her over.
            Body.AddRelativeTorque(new Vector3(0f, 0f, -f.heel));
            // Rolling and pitching die away in the water.
            Body.AddRelativeTorque(new Vector3(-ang.x, 0f, -ang.z) * d.Mass * d.beam * 0.6f);

            // Aground: where the real sea floor comes up under her keel, she grinds to a stop.
            Aground = false;
            var map = RealWorld.Active ? WorldMap.Current : null;
            if (map != null)
            {
                Vector3 keel = t.TransformPoint(new Vector3(0f, -d.draught, d.length * 0.3f));
                float floor = TerrainDetail.Height(map, WorldOrigin.GlobalX(keel), WorldOrigin.GlobalZ(keel));
                if (floor > keel.y)
                {
                    Aground = true;
                    Vector3 v = Compat.Velocity(Body);
                    v.y = 0f;
                    Body.AddForce(-v * d.Mass * 1.5f + Vector3.up * d.Mass * ShipPhysics.G * Mathf.Clamp01((floor - keel.y) / d.draught) * 0.5f);
                }
            }
        }

        /// <summary>Put the ship somewhere else at rest (after foundering, for example).</summary>
        public void Relocate(Vector3 position, float heading)
        {
            transform.position = position;
            transform.rotation = Quaternion.Euler(0f, heading, 0f);
            Body.position = position;
            Body.rotation = transform.rotation;
            Compat.SetVelocity(Body, Vector3.zero);
            Body.angularVelocity = Vector3.zero;
            rudderAngle = 0f;
            RudderInput = 0f;
            Actual = new ShipPhysics.Controls();
            WeighAnchor();
            CastOff();
            Rowing = false;
            Furl();
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
            if (Parts.rigs != null)
            {
                // The new classes: each yard braces round to the wind as far as its rig allows, the cloth gathers up when furled.
                Vector3 localWind = transform.InverseTransformDirection(Wind.Direction);
                float toWind = Mathf.Atan2(localWind.x, localWind.z) * Mathf.Rad2Deg;
                for (int i = 0; i < Parts.rigs.Length; i++)
                {
                    var plan = Design.sails[i];
                    float brace = Mathf.Clamp(toWind * 0.5f, -plan.maxBrace, plan.maxBrace);
                    Parts.rigs[i].localRotation = Quaternion.Slerp(Parts.rigs[i].localRotation, Quaternion.Euler(0f, brace, 0f), Time.deltaTime * 1.5f);
                    float set = Mathf.Max(0.06f, SailAmount);
                    Parts.cloths[i].localScale = plan.rig == Rig.Square ? new Vector3(1f, set, 1f) : Vector3.one * Mathf.Max(0.15f, SailAmount);
                    Parts.cloths[i].gameObject.SetActive(SailAmount > 0.02f);
                }
            }
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
            if (GameInput.Pressed(Key.Anchor)) CombatHud.Banner(ship.Moored || ship.Anchored ? "UNDER WAY" : "HOLDING FAST", ship.AnchorOrMoor());
        }
    }
}
