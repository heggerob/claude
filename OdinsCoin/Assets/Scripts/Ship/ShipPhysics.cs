using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The forces on a wooden ship, in real units, from a <see cref="ShipDesign"/>. The look is storybook but the
    /// water and wind behave:
    /// <list type="bullet">
    /// <item>it floats by Archimedes, spread over the hull, so it rides the waves and heels;</item>
    /// <item>it is held back by skin friction (ITTC-57) and by wave-making, which climbs steeply past hull speed;</item>
    /// <item>the hull and keel grip the water sideways like a wing, so it makes little leeway and resists turning;</item>
    /// <item>the rudder is a foil in the flow past the stern: no way on, no steering, and it stalls if forced over;</item>
    /// <item>each sail is trimmed to the apparent wind and gives lift and drag; square sails can't be braced round
    /// as far as lateen sails, so they can't point as high, and nothing sails straight into the wind;</item>
    /// <item>oars push in strokes, each side on its own, so rowing one side and backing the other spins her.</item>
    /// </list>
    /// Everything is in the ship's frame: x to starboard, z forward, y up; u = forward speed, v = speed to
    /// starboard, r = turning rate (rad/s, positive turns the bow to starboard).
    /// </summary>
    public static class ShipPhysics
    {
        public const float RhoWater = ShipDesign.RhoWater, RhoAir = 1.225f, Nu = 1.19e-6f, G = ShipDesign.Gravity;
        /// <summary>Lift slope of the hull and keel against leeway (per radian), and their cross-flow drag.</summary>
        public const float HullLiftSlope = 0.35f, HullCrossDrag = 1.0f;
        /// <summary>Slices along the hull for the side forces.</summary>
        public const int Slices = 10;
        /// <summary>A rower's power (W), how much of it drives the ship, and the most one oar can pull (N).</summary>
        public const float RowerPower = 170f, OarEfficiency = 0.55f, MaxOarForce = 260f;
        /// <summary>Strokes per minute.</summary>
        public const float StrokeRate = 26f;

        // ---------------------------------------------------------------- resistance

        /// <summary>The water's resistance to going ahead at speed <paramref name="u"/> (N, always ≥ 0).</summary>
        public static float Resistance(ShipDesign d, float u)
        {
            float speed = Mathf.Abs(u);
            if (speed < 1e-4f) return 0f;
            float re = Mathf.Max(1e5f, speed * d.length / Nu);
            float lg = (float)System.Math.Log10(re) - 2f;
            float cf = 0.075f / (lg * lg) * 1.2f;            // skin friction, with form factor
            float fn = speed / Mathf.Sqrt(G * d.length);
            float cw = 0.004f * Mathf.Pow(fn / 0.4f, 5f);   // wave-making: the bow wave builds towards hull speed
            float q = 0.5f * RhoWater * speed * speed;
            return q * d.WettedArea * (cf + cw);
        }

        // ---------------------------------------------------------------- hull and keel sideways

        /// <summary>The hull's grip on the water sideways, along its length: side force (N) and turning moment (N·m).</summary>
        public static void HullSide(ShipDesign d, float u, float v, float r, out float fx, out float mz)
        {
            fx = mz = 0f;
            float dz = d.length / Slices;
            for (int i = 0; i < Slices; i++)
            {
                float z = -d.length / 2f + (i + 0.5f) * dz;
                // The hull is deepest amidships and fines away at the ends.
                float t = 2f * z / d.length;
                float share = Mathf.Sqrt(Mathf.Max(0f, 1f - t * t)) * (Mathf.PI / 4f);
                float area = d.LateralArea / Slices * share / (Mathf.PI / 4f) * 0.785f;
                float vl = v + r * z;
                float f = -0.5f * RhoWater * area * (HullLiftSlope * Mathf.Abs(u) * vl + HullCrossDrag * vl * Mathf.Abs(vl));
                fx += f;
                mz += z * f;
            }
        }

        // ---------------------------------------------------------------- rudder

        /// <summary>Lift coefficient of a foil at angle of attack <paramref name="alpha"/> (rad), aspect ratio <paramref name="ar"/>, stalling past ~22°.</summary>
        public static float FoilLift(float alpha, float ar)
        {
            float slope = 2f * Mathf.PI * ar / (ar + 2f);
            float stall = 22f * Mathf.Deg2Rad;
            float a = Mathf.Abs(alpha);
            float cl = a < stall ? slope * a : slope * stall * (1f - 0.45f * Mathf.Clamp01((a - stall) / 0.5f));
            return Mathf.Sign(alpha) * cl;
        }

        /// <summary>
        /// The rudder, hung at the stern, turned <paramref name="angle"/> radians (positive: to turn to starboard):
        /// side force, drag and turning moment. It only works with water flowing past it.
        /// </summary>
        public static void Rudder(ShipDesign d, float u, float v, float r, float angle, out float fx, out float fz, out float mz)
        {
            float z = -d.length / 2f - 0.3f;
            float vl = v + r * z;
            float speed2 = u * u + vl * vl;
            // The flow meets the blade at its own angle plus the stern's sideways drift.
            float inflow = Mathf.Atan2(vl, Mathf.Max(Mathf.Abs(u), 0.05f));
            float alpha = angle + inflow;
            if (u < 0f) alpha = -angle + inflow; // going astern the blade works backwards, and weakly
            float cl = FoilLift(alpha, d.rudderAspect) * (u < 0f ? 0.5f : 1f);
            float cd = 0.03f + 0.9f * Mathf.Sin(alpha) * Mathf.Sin(alpha);
            float q = 0.5f * RhoWater * speed2 * d.rudderArea;
            fx = -q * cl;
            fz = -q * cd * Mathf.Sign(u == 0f ? 1f : u);
            mz = z * fx;
        }

        // ---------------------------------------------------------------- sails

        /// <summary>A sail's lift and drag at angle of attack <paramref name="alpha"/> (rad, 0..π/2): cambered cloth, stalling past ~35°.</summary>
        public static void SailCoefficients(Rig rig, float alpha, out float cl, out float cd)
        {
            float a = Mathf.Clamp(alpha, 0f, Mathf.PI / 2f);
            float peak = rig == Rig.Lateen ? 1.35f : 1.15f;
            float stall = rig == Rig.Lateen ? 28f * Mathf.Deg2Rad : 32f * Mathf.Deg2Rad;
            cl = a < stall ? peak * Mathf.Sin(a / stall * Mathf.PI / 2f) : peak * Mathf.Lerp(1f, 0f, Mathf.Clamp01((a - stall) / (Mathf.PI / 2f - stall))) * 0.85f;
            cd = 0.08f + 1.25f * Mathf.Sin(a) * Mathf.Sin(a);
        }

        /// <summary>
        /// The force of one sail (N, ship frame x/z) in the apparent wind (<paramref name="apparent"/>: the air's
        /// velocity relative to the ship, m/s, ship frame x/z), trimmed for the most drive the rig allows;
        /// <paramref name="set"/> is how much sail is set (0..1). <paramref name="chord"/> gets the trim: the angle
        /// of the sail's chord from the centreline (degrees).
        /// </summary>
        public static Vector2 Sail(SailPlan s, Vector2 apparent, float set, out float chord)
        {
            chord = s.rig == Rig.Square ? 90f : 0f;
            float speed = apparent.magnitude;
            if (speed < 0.05f || set <= 0f) return Vector2.zero;
            var flow = apparent / speed;
            float q = 0.5f * RhoAir * speed * speed * s.area * Mathf.Clamp01(set);
            var best = Vector2.zero;
            float bestDrive = float.NegativeInfinity;
            float centre = s.rig == Rig.Square ? 90f : 0f;
            for (int k = -12; k <= 12; k++)
            {
                float phi = centre + s.maxBrace * k / 12f;
                var f = SailForce(s.rig, phi, flow, q);
                // Trim for drive, and when nothing drives, for the least backwards push.
                if (f.y > bestDrive) { bestDrive = f.y; best = f; chord = phi; }
            }
            return best;
        }

        /// <summary>The force of a sail whose chord lies at <paramref name="phi"/> degrees from the centreline, in air flowing along <paramref name="flow"/>.</summary>
        public static Vector2 SailForce(Rig rig, float phi, Vector2 flow, float q)
        {
            float rad = phi * Mathf.Deg2Rad;
            var chordDir = new Vector2(Mathf.Sin(rad), Mathf.Cos(rad));
            var normal = new Vector2(chordDir.y, -chordDir.x);
            if (Vector2.Dot(normal, flow) < 0f) normal = -normal;
            float alpha = Mathf.Acos(Mathf.Clamp01(Mathf.Abs(Vector2.Dot(chordDir, flow))));
            float cl, cd;
            SailCoefficients(rig, alpha, out cl, out cd);
            var liftDir = normal - Vector2.Dot(normal, flow) * flow;
            liftDir = liftDir.sqrMagnitude > 1e-8f ? liftDir.normalized : Vector2.zero;
            return q * (cl * liftDir + cd * flow);
        }

        /// <summary>The wind on the hull, masts and rigging, pushing her along with it even with the sails furled (N, ship frame).</summary>
        public static Vector2 Windage(ShipDesign d, Vector2 apparent)
        {
            float area = d.length * d.freeboard * 0.35f + d.sails.Length * 6f;
            return 0.5f * RhoAir * apparent.magnitude * area * 0.9f * apparent;
        }

        // ---------------------------------------------------------------- oars

        /// <summary>
        /// The oars of one side (<paramref name="effort"/> -1 backing water .. 1 full pull) at forward speed
        /// <paramref name="u"/> and stroke <paramref name="phase"/> (0..1): forward thrust (N). Each oar gives what a
        /// rower's power can at that speed, pulsing with the stroke.
        /// </summary>
        public static float Oars(int rowers, float effort, float u, float phase)
        {
            if (rowers <= 0 || Mathf.Abs(effort) < 1e-3f) return 0f;
            float along = effort > 0f ? Mathf.Max(u, 0.3f) : Mathf.Max(-u, 0.3f);
            float perOar = Mathf.Min(MaxOarForce, OarEfficiency * RowerPower / along);
            float stroke = 1f + 0.6f * Mathf.Sin(phase * 2f * Mathf.PI);
            // Backing water is weaker than pulling.
            float k = effort > 0f ? effort : effort * 0.6f;
            return rowers * perOar * k * stroke;
        }

        // ---------------------------------------------------------------- all together

        /// <summary>What the crew is doing.</summary>
        public struct Controls
        {
            /// <summary>Rudder, -1 hard to port .. 1 hard to starboard.</summary>
            public float rudder;
            /// <summary>How much sail is set, 0..1.</summary>
            public float sail;
            /// <summary>Each side's oars, -1 backing .. 1 pulling.</summary>
            public float oarsPort, oarsStarboard;
        }

        /// <summary>The ship's own frame forces and moments at a moment.</summary>
        public struct Forces
        {
            public float fx, fz, mz;
            /// <summary>Heeling moment from the sails (N·m, positive heels to starboard... the leeward side).</summary>
            public float heel;
        }

        /// <summary>The hardest the rudder is put over (radians).</summary>
        public const float MaxRudder = 35f * Mathf.Deg2Rad;

        /// <summary>Everything but buoyancy: resistance, hull grip, rudder, sails, windage and oars.</summary>
        public static Forces Total(ShipDesign d, float u, float v, float r, Vector2 trueWindLocal, Controls c, float strokePhase)
        {
            var f = new Forces();
            f.fz -= Resistance(d, u) * Mathf.Sign(u);
            float hx, hm;
            HullSide(d, u, v, r, out hx, out hm);
            f.fx += hx; f.mz += hm;
            float rx, rz, rm;
            Rudder(d, u, v, r, Mathf.Clamp(c.rudder, -1f, 1f) * MaxRudder, out rx, out rz, out rm);
            f.fx += rx; f.fz += rz; f.mz += rm;
            var apparent = trueWindLocal - new Vector2(v, u);
            foreach (var s in d.sails)
            {
                float chord;
                var sf = Sail(s, apparent, c.sail, out chord);
                f.fx += sf.x; f.fz += sf.y;
                f.mz += s.x * sf.x;
                f.heel += sf.x * s.height;
            }
            var w = Windage(d, apparent);
            f.fx += w.x; f.fz += w.y;
            float half = d.beam / 2f;
            float port = Oars(d.oarsPerSide, c.oarsPort, u, strokePhase);
            float star = Oars(d.oarsPerSide, c.oarsStarboard, u, strokePhase);
            f.fz += port + star;
            // Pulling harder on the starboard side swings the bow to port, and the other way round.
            f.mz += half * port - half * star;
            return f;
        }

        /// <summary>How far she leans at a heeling moment (degrees), from her stiffness.</summary>
        public static float HeelAngle(ShipDesign d, float heelMoment)
        {
            float righting = d.Mass * G * Mathf.Max(0.1f, d.GM);
            return Mathf.Asin(Mathf.Clamp(heelMoment / righting, -1f, 1f)) * Mathf.Rad2Deg;
        }

        // ---------------------------------------------------------------- buoyancy

        /// <summary>A patch of the bottom that floats the ship: where it is (ship frame, at the keel line) and its share of the waterplane (m²).</summary>
        public struct FloatCell { public Vector3 at; public float area; }

        /// <summary>
        /// The hull's floating cells: a grid over the waterplane, shaped like the hull (full amidships, fine at the
        /// ends), with areas scaled so she floats at exactly her design draught.
        /// </summary>
        public static List<FloatCell> FloatCells(ShipDesign d, int along = 9, int across = 3)
        {
            var cells = new List<FloatCell>();
            float total = 0f;
            for (int i = 0; i < along; i++)
                for (int j = 0; j < across; j++)
                {
                    float z = -d.length / 2f + (i + 0.5f) * d.length / along;
                    float t = 2f * z / d.length;
                    float halfBeam = d.beam / 2f * Mathf.Pow(Mathf.Max(0f, 1f - t * t), 0.6f);
                    float x = across == 1 ? 0f : Mathf.Lerp(-halfBeam, halfBeam, (j + 0.5f) / across) * 1f;
                    float a = halfBeam * 2f / across * d.length / along;
                    cells.Add(new FloatCell { at = new Vector3(x, -d.draught, z), area = a });
                    total += a;
                }
            float want = d.blockCoef * d.length * d.beam;
            for (int k = 0; k < cells.Count; k++) { var c = cells[k]; c.area *= want / total; cells[k] = c; }
            return cells;
        }

        /// <summary>The buoyancy of one cell (N) whose bottom is <paramref name="depth"/> metres under the water surface.</summary>
        public static float CellBuoyancy(ShipDesign d, FloatCell c, float depth)
        {
            return RhoWater * G * c.area * Mathf.Clamp(depth, 0f, d.draught + d.freeboard);
        }

        // ---------------------------------------------------------------- a simple flat simulation (tests, AI, previews)

        /// <summary>Where a ship is and how it moves on a flat sea: position (x east, z north), heading (degrees), u, v, r.</summary>
        public struct State { public Vector2 position; public float heading, u, v, r, strokePhase; }

        /// <summary>Advance a flat-sea simulation by <paramref name="dt"/> with the wind blowing towards <paramref name="windWorld"/> (m/s, x east, z north).</summary>
        public static State Step(ShipDesign d, State s, Controls c, Vector2 windWorld, float dt)
        {
            float h = s.heading * Mathf.Deg2Rad;
            var fwd = new Vector2(Mathf.Sin(h), Mathf.Cos(h));
            var right = new Vector2(fwd.y, -fwd.x);
            var windLocal = new Vector2(Vector2.Dot(windWorld, right), Vector2.Dot(windWorld, fwd));
            var f = Total(d, s.u, s.v, s.r, windLocal, c, s.strokePhase);
            float m = d.Mass;
            // Added mass of the water the hull drags with it: a little ahead, a lot sideways.
            float mu = m * 1.05f, mv = m * 1.8f, iz = m * (0.22f * d.length) * (0.22f * d.length) * 1.3f;
            s.u += (f.fz + mv * s.v * s.r) / mu * dt;
            s.v += (f.fx - mu * s.u * s.r) / mv * dt;
            s.r += f.mz / iz * dt;
            s.heading += s.r * Mathf.Rad2Deg * dt;
            s.position += (fwd * s.u + right * s.v) * dt;
            s.strokePhase = Mathf.Repeat(s.strokePhase + StrokeRate / 60f * dt, 1f);
            return s;
        }

        public static float Knots(float metresPerSecond) { return metresPerSecond * 1.943844f; }
    }
}
