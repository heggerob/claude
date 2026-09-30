using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The seamanship side of sailing, as plain maths so it can be tested: water coming over a rail that's gone
    /// under (so in a blow you reef or swamp), the anchor and its rode (it holds, or drags when the pull is too
    /// much), and the mooring lines that make a ship fast to a jetty's bollards.
    /// </summary>
    public static class Seamanship
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Bollards.Clear(); }

        /// <summary>Every bollard in the world a ship can make fast to (the jetties register theirs).</summary>
        public static readonly List<Transform> Bollards = new List<Transform>();

        // ---- Shipping water ----

        /// <summary>
        /// Water pouring in over a stretch of rail that's under the surface (m³/s): the flow over a weir,
        /// 1.7 · length · depth^1.5.
        /// </summary>
        public static float ShippedWater(float immersion, float railLength)
        {
            if (immersion <= 0f || railLength <= 0f) return 0f;
            return 1.7f * railLength * Mathf.Pow(immersion, 1.5f);
        }

        /// <summary>How much water fills her open hull to the point she founders (m³).</summary>
        public static float OpenVolume(ShipDesign d) { return 0.6f * d.length * d.beam * d.freeboard; }

        /// <summary>The heel at which the rail amidships touches flat water (degrees).</summary>
        public static float RailUnderAngle(ShipDesign d) { return Mathf.Atan2(d.freeboard, d.beam / 2f) * Mathf.Rad2Deg; }

        // ---- Pointing and tacking ----

        static readonly Dictionary<long, float> closest = new Dictionary<long, float>();

        /// <summary>
        /// The closest she can sail to the wind (degrees off the true wind) and still be driven ahead: the smallest
        /// angle where the sails' drive beats the water's drag at a working speed, with a little leeway. A lateen
        /// points higher than a square sail. Upwind of this she's in irons: tack, or row.
        /// </summary>
        public static float ClosestToWind(ShipDesign d, float windKnots)
        {
            int kn = Mathf.Clamp(Mathf.RoundToInt(windKnots), 2, 60);
            long key = ((long)(uint)d.GetHashCode() << 8) | (uint)kn;
            float found;
            if (closest.TryGetValue(key, out found)) return found;
            float speed = kn * 0.514f, u = Mathf.Min(2f, 0.3f * d.HullSpeed), v = -u * Mathf.Tan(4f * Mathf.Deg2Rad);
            found = 180f;
            for (float a = 15f; a <= 180f; a += 1f)
            {
                // Wind coming from a° off the starboard bow.
                var wind = new Vector2(-Mathf.Sin(a * Mathf.Deg2Rad), -Mathf.Cos(a * Mathf.Deg2Rad)) * speed;
                if (ShipPhysics.Total(d, u, v, 0f, wind, new ShipPhysics.Controls { sail = 1f }, 0f).fz > 0f) { found = a; break; }
            }
            closest[key] = found;
            return found;
        }

        /// <summary>The two headings to tack on to work up towards where the wind comes from (port tack, starboard tack).</summary>
        public static void TackHeadings(float windFrom, float offWind, out float portTack, out float starboardTack)
        {
            // On port tack the wind comes over the port bow: she heads to the right of the wind.
            portTack = Mathf.Repeat(windFrom + offWind, 360f);
            starboardTack = Mathf.Repeat(windFrom - offWind, 360f);
        }

        // ---- The anchor ----

        /// <summary>The anchor rode's length (m): she can anchor in water up to about half that deep.</summary>
        public const float RodeLength = 80f;
        /// <summary>The deepest water the anchor holds in (m).</summary>
        public const float MaxAnchorDepth = RodeLength * 0.5f;
        /// <summary>How far the rode stretches (with its catenary sag straightening) at full holding pull (m).</summary>
        public const float RodeStretch = 3f;

        /// <summary>The anchor's weight for a ship (kg): about 0.4% of her displacement, at least 30 kg.</summary>
        public static float AnchorMass(ShipDesign d) { return Mathf.Max(30f, 0.004f * d.Mass); }

        /// <summary>How hard the anchor can pull before it drags (N): an iron anchor in mud holds about ten times its weight.</summary>
        public static float HoldingForce(ShipDesign d) { return 10f * AnchorMass(d) * ShipDesign.Gravity; }

        /// <summary>
        /// The rode's pull on the ship (horizontal, towards the anchor, N) from where the bow is relative to the
        /// anchor (<paramref name="offset"/> = anchor − bow, flat) in water <paramref name="depth"/> deep, with the
        /// bow moving at <paramref name="velocity"/>. The rode lies slack until the bow reaches the end of its scope,
        /// then pulls like a stiff, damped spring. If the pull would be more than the anchor's holding, the anchor
        /// drags: <paramref name="drag"/> says how far it slides towards the ship, and the pull is capped at the holding.
        /// </summary>
        public static Vector2 RodePull(ShipDesign d, Vector2 offset, Vector2 velocity, float depth, out float drag)
        {
            drag = 0f;
            float reach = Mathf.Sqrt(Mathf.Max(0f, RodeLength * RodeLength - depth * depth));
            float r = offset.magnitude;
            if (r <= reach || r < 1e-4f) return Vector2.zero;
            Vector2 dir = offset / r;
            float hold = HoldingForce(d);
            float k = hold / RodeStretch;
            float c = 2f * 0.6f * Mathf.Sqrt(k * d.Mass);
            float pull = k * (r - reach) - c * Vector2.Dot(velocity, dir);
            pull = Mathf.Max(0f, pull);
            if (pull > hold)
            {
                drag = (pull - hold) / k;
                pull = hold;
            }
            return dir * pull;
        }

        // ---- Mooring ----

        /// <summary>How far from a bollard she can be to throw a line to it (m).</summary>
        public const float LineReach = 12f;

        /// <summary>
        /// A mooring line's pull (N) on the ship at the cleat: slack up to its length, then stiff and well damped, so
        /// she lies quietly alongside. <paramref name="offset"/> = bollard − cleat.
        /// </summary>
        public static Vector3 LinePull(ShipDesign d, Vector3 offset, Vector3 velocity, float length)
        {
            float r = offset.magnitude;
            if (r <= length || r < 1e-4f) return Vector3.zero;
            Vector3 dir = offset / r;
            float k = d.Mass * 2f;
            float c = 2f * 0.9f * Mathf.Sqrt(k * d.Mass);
            return dir * Mathf.Max(0f, k * (r - length) - c * Vector3.Dot(velocity, dir));
        }

        /// <summary>The nearest bollard to a point, within <paramref name="reach"/>; null if none.</summary>
        public static Transform NearestBollard(Vector3 p, float reach)
        {
            Transform best = null;
            float bestD = reach;
            for (int i = Bollards.Count - 1; i >= 0; i--)
            {
                var b = Bollards[i];
                if (b == null) { Bollards.RemoveAt(i); continue; }
                float dist = Vector3.Distance(b.position, p);
                if (dist < bestD) { bestD = dist; best = b; }
            }
            return best;
        }

        /// <summary>Put a bollard on a jetty (local position), registered so ships can make fast to it.</summary>
        public static Transform AddBollard(Transform parent, Vector3 local)
        {
            var b = LongshipBuilder.Deco(PrimitiveType.Cylinder, parent, local + new Vector3(0f, 0.35f, 0f), new Vector3(0.35f, 0.35f, 0.35f), Materials.DarkWood);
            b.name = "Bollard";
            Bollards.Add(b);
            return b;
        }
    }
}
