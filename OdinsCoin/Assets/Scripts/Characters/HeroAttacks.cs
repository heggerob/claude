using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// One attack, as a handful of key poses that a smooth curve runs through: the ready pose, the wind-up, the
    /// blow, the follow-through and back to ready. Between keys every channel follows a Catmull-Rom curve (smooth,
    /// no corners), and the move eases in from whatever the hero was doing and back out to it.
    /// </summary>
    public class AttackMove
    {
        /// <summary>The channels of a key, in order.</summary>
        public enum Ch { ArmRX, ArmRZ, ElbowR, ArmLX, ArmLZ, ElbowL, HaftX, HaftY, HaftZ, Roll, Yaw, Pitch, Lunge, Count }

        public struct Key
        {
            public float t;
            public float[] v;
        }

        public string name;
        public WeaponId weapon;
        /// <summary>How long the whole move takes (s), and when in it (0..1) the blow lands.</summary>
        public float duration, hitAt;
        /// <summary>Whether the left arm takes part (two-handed weapons, a bow's string); otherwise it keeps guard.</summary>
        public bool twoHanded;
        /// <summary>How far down the haft the fist slides for this move (m on the reference body): a staff swung from the butt, a spear thrust from low on the shaft.</summary>
        public float slide;
        /// <summary>How hard it hits, against the weapon's basic blow (a combo's last swing hits hardest).</summary>
        public float power = 1f;
        public Key[] keys;

        /// <summary>The move's pose at <paramref name="t"/> (0..1).</summary>
        public float[] Sample(float t)
        {
            t = Mathf.Clamp01(t);
            int n = keys.Length;
            int i = 0;
            while (i < n - 2 && t > keys[i + 1].t) i++;
            var k0 = keys[Mathf.Max(0, i - 1)]; var k1 = keys[i]; var k2 = keys[i + 1]; var k3 = keys[Mathf.Min(n - 1, i + 2)];
            float u = Mathf.Clamp01((t - k1.t) / Mathf.Max(0.0001f, k2.t - k1.t));
            var result = new float[(int)Ch.Count];
            for (int c = 0; c < result.Length; c++)
                result[c] = CatmullRom(k0.v[c], k1.v[c], k2.v[c], k3.v[c], u);
            return result;
        }

        /// <summary>How much the move owns the body at <paramref name="t"/>: easing in at the start and out at the end.</summary>
        public float Weight(float t)
        {
            return Smooth(Mathf.Clamp01(t / 0.12f)) * (1f - Smooth(Mathf.Clamp01((t - 0.86f) / 0.14f)));
        }

        static float CatmullRom(float p0, float p1, float p2, float p3, float u)
        {
            float u2 = u * u, u3 = u2 * u;
            return 0.5f * (2f * p1 + (-p0 + p2) * u + (2f * p0 - 5f * p1 + 4f * p2 - p3) * u2 + (-p0 + 3f * p1 - 3f * p2 + p3) * u3);
        }

        static float Smooth(float x) { return x * x * (3f - 2f * x); }
    }

    /// <summary>
    /// The attacks for each weapon, and how to put them on a hero. The weapon's angle in the fist is worked out
    /// from where its haft should point in the body's space (like the carry poses), so the blade, head or point
    /// always goes where the move means it to, whatever the arm is doing.
    /// </summary>
    public static class HeroAttacks
    {
        static AttackMove.Key K(float t, float armRX, float armRZ, float elbowR, float armLX, float armLZ, float elbowL,
                                Vector3 haft, float roll, float yaw, float pitch, float lunge)
        {
            return new AttackMove.Key { t = t, v = new[] { armRX, armRZ, elbowR, armLX, armLZ, elbowL, haft.x, haft.y, haft.z, roll, yaw, pitch, lunge } };
        }

        static readonly System.Collections.Generic.Dictionary<WeaponId, AttackMove[]> combos = new System.Collections.Generic.Dictionary<WeaponId, AttackMove[]>();

        /// <summary>The weapon's opening attack.</summary>
        public static AttackMove For(WeaponId w) { return Combo(w)[0]; }

        /// <summary>
        /// The weapon's combo: the swings it chains when the attack is pressed again before one finishes (a chop,
        /// a cleave and a sweep; a slash, a backhand and a thrust...). Each starts and ends in the same ready pose,
        /// so they flow into each other.
        /// </summary>
        public static AttackMove[] Combo(WeaponId w)
        {
            AttackMove[] c;
            if (combos.TryGetValue(w, out c)) return c;
            switch (w)
            {
                case WeaponId.TwoHandAxe: c = new[] { Axe(), AxeCleave(), AxeSweep() }; break;
                case WeaponId.Sword: c = new[] { Sword(), SwordBackhand(), SwordThrust() }; break;
                case WeaponId.Spear: c = new[] { Spear(), SpearHigh(), SpearSweep() }; break;
                case WeaponId.Bow: c = new[] { Bow(), BowQuick() }; break;
                case WeaponId.Staff: c = new[] { Staff(), StaffSweep(), StaffJab() }; break;
                default: c = new[] { Fist(), FistCross(), FistUppercut() }; break;
            }
            combos[w] = c;
            return c;
        }

        /// <summary>A copy of the ready key at <paramref name="t"/> (to close a move where it began).</summary>
        static AttackMove.Key Back(AttackMove.Key ready, float t)
        {
            return new AttackMove.Key { t = t, v = (float[])ready.v.Clone() };
        }

        /// <summary>A diagonal cleave: axe swung up over the right shoulder and down across to the left hip.</summary>
        static AttackMove AxeCleave()
        {
            var ready = Axe().keys[0];
            return new AttackMove {
                name = "Cleave", weapon = WeaponId.TwoHandAxe, duration = 0.6f, hitAt = 0.52f, twoHanded = true,
                keys = new[] {
                    ready,
                    K(0.3f, -150f, 40f, -70f, -140f, 20f, -70f, new Vector3(0.7f, 0.5f, -0.5f), 0f, 30f, -6f, -0.02f),
                    K(0.52f, -70f, -10f, -10f, -65f, 5f, -10f, new Vector3(-0.6f, -0.05f, 1f), 0f, -25f, 14f, 0.22f),
                    K(0.72f, -30f, -30f, -25f, -25f, -10f, -25f, new Vector3(-0.9f, -0.4f, 0.2f), 0f, -35f, 12f, 0.18f),
                    Back(ready, 1f) } };
        }

        /// <summary>A great flat sweep: the whole body winds back to the right and swings round to the left.</summary>
        static AttackMove AxeSweep()
        {
            var ready = Axe().keys[0];
            return new AttackMove {
                name = "Sweep", weapon = WeaponId.TwoHandAxe, duration = 0.72f, hitAt = 0.55f, twoHanded = true, power = 1.4f,
                keys = new[] {
                    ready,
                    K(0.32f, -90f, 70f, -40f, -85f, 45f, -40f, new Vector3(1f, 0.1f, -0.4f), 0f, 55f, 4f, -0.04f),
                    K(0.55f, -90f, 0f, -5f, -90f, 0f, -5f, new Vector3(0f, 0.05f, 1f), 0f, -10f, 8f, 0.2f),
                    K(0.74f, -85f, -60f, -20f, -85f, -50f, -20f, new Vector3(-1f, 0f, 0.1f), 0f, -60f, 6f, 0.12f),
                    Back(ready, 1f) } };
        }

        /// <summary>A rising backhand: from low on the left, up and across to the right.</summary>
        static AttackMove SwordBackhand()
        {
            var ready = Sword().keys[0];
            return new AttackMove {
                name = "Backhand", weapon = WeaponId.Sword, duration = 0.48f, hitAt = 0.48f,
                keys = new[] {
                    ready,
                    K(0.28f, -40f, -40f, -90f, -55f, -12f, -75f, new Vector3(-0.8f, -0.3f, 0.4f), 90f, -25f, 4f, 0f),
                    K(0.48f, -95f, 10f, -15f, -55f, -12f, -75f, new Vector3(0.45f, 0.25f, 1f), 90f, 18f, 6f, 0.16f),
                    K(0.68f, -130f, 35f, -30f, -50f, -12f, -70f, new Vector3(0.8f, 0.7f, 0.2f), 90f, 28f, 0f, 0.1f),
                    Back(ready, 1f) } };
        }

        /// <summary>A lunging thrust with the point, a long step in.</summary>
        static AttackMove SwordThrust()
        {
            var ready = Sword().keys[0];
            return new AttackMove {
                name = "Lunge", weapon = WeaponId.Sword, duration = 0.55f, hitAt = 0.5f, power = 1.4f,
                keys = new[] {
                    ready,
                    K(0.3f, -20f, 15f, -110f, -60f, -12f, -80f, new Vector3(0.05f, 0.05f, 1f), 0f, 20f, -4f, -0.06f),
                    K(0.5f, -88f, 0f, 0f, -55f, -12f, -70f, new Vector3(0f, 0f, 1f), 0f, -15f, 14f, 0.42f),
                    K(0.7f, -82f, 2f, -10f, -55f, -12f, -70f, new Vector3(0f, 0.02f, 1f), 0f, -12f, 12f, 0.34f),
                    Back(ready, 1f) } };
        }

        /// <summary>An overhand thrust, spear raised by the ear and driven down and forward.</summary>
        static AttackMove SpearHigh()
        {
            var ready = Spear().keys[0];
            return new AttackMove {
                name = "Overhand thrust", weapon = WeaponId.Spear, duration = 0.58f, hitAt = 0.52f, slide = 0.45f,
                keys = new[] {
                    ready,
                    K(0.32f, -160f, 10f, -60f, -60f, -12f, -65f, new Vector3(0f, 0.3f, 1f), 0f, 15f, -6f, -0.04f),
                    K(0.52f, -120f, 0f, -5f, -55f, -12f, -60f, new Vector3(0f, -0.3f, 1f), 0f, -12f, 12f, 0.32f),
                    K(0.72f, -110f, 0f, -15f, -55f, -12f, -60f, new Vector3(0f, -0.25f, 1f), 0f, -10f, 10f, 0.26f),
                    Back(ready, 1f) } };
        }

        /// <summary>A low sweep with the shaft, to knock the legs from under them.</summary>
        static AttackMove SpearSweep()
        {
            var ready = Spear().keys[0];
            return new AttackMove {
                name = "Shaft sweep", weapon = WeaponId.Spear, duration = 0.65f, hitAt = 0.55f, slide = 0.2f, power = 1.3f,
                keys = new[] {
                    ready,
                    K(0.32f, -60f, 55f, -40f, -50f, 20f, -60f, new Vector3(1f, -0.1f, -0.2f), 0f, 45f, 6f, -0.02f),
                    K(0.55f, -75f, 0f, -10f, -60f, 0f, -40f, new Vector3(0f, -0.35f, 1f), 0f, -10f, 16f, 0.16f),
                    K(0.75f, -65f, -50f, -25f, -55f, -30f, -50f, new Vector3(-1f, -0.3f, 0.2f), 0f, -45f, 12f, 0.1f),
                    Back(ready, 1f) } };
        }

        /// <summary>A quick snap shot: half a draw, loosed from the hip.</summary>
        static AttackMove BowQuick()
        {
            var ready = Bow().keys[0];
            return new AttackMove {
                name = "Snap shot", weapon = WeaponId.Bow, duration = 0.55f, hitAt = 0.5f, twoHanded = true, power = 0.8f,
                keys = new[] {
                    ready,
                    K(0.25f, -70f, 0f, -10f, -65f, 10f, -60f, new Vector3(0f, 1f, 0.2f), 0f, -15f, 2f, 0f),
                    K(0.45f, -72f, 0f, -5f, -72f, 22f, -115f, new Vector3(0f, 1f, 0.18f), 0f, -22f, 2f, 0f),
                    K(0.52f, -72f, 0f, -5f, -62f, 5f, -55f, new Vector3(0f, 1f, 0.2f), 0f, -20f, 0f, -0.01f),
                    Back(ready, 1f) } };
        }

        /// <summary>A sweep of the staff from right to left at chest height.</summary>
        static AttackMove StaffSweep()
        {
            var ready = Staff().keys[0];
            return new AttackMove {
                name = "Staff sweep", weapon = WeaponId.Staff, duration = 0.65f, hitAt = 0.55f, slide = 0.95f,
                keys = new[] {
                    ready,
                    K(0.32f, -80f, 65f, -30f, -30f, -8f, -40f, new Vector3(1f, 0.2f, -0.3f), 0f, 40f, 0f, -0.02f),
                    K(0.55f, -85f, 0f, -5f, -30f, -8f, -40f, new Vector3(0f, 0.1f, 1f), 0f, -10f, 8f, 0.16f),
                    K(0.75f, -80f, -55f, -20f, -30f, -8f, -40f, new Vector3(-1f, 0.1f, 0.2f), 0f, -40f, 6f, 0.1f),
                    Back(ready, 1f) } };
        }

        /// <summary>A two-handed jab with the staff's head, driving it straight out.</summary>
        static AttackMove StaffJab()
        {
            var ready = Staff().keys[0];
            return new AttackMove {
                name = "Staff jab", weapon = WeaponId.Staff, duration = 0.55f, hitAt = 0.5f, slide = 0.6f, twoHanded = true, power = 1.3f,
                keys = new[] {
                    ready,
                    K(0.3f, -30f, 10f, -100f, -40f, -10f, -90f, new Vector3(0f, 0.1f, 1f), 0f, 15f, -4f, -0.05f),
                    K(0.5f, -85f, 0f, 0f, -80f, -5f, -10f, new Vector3(0f, 0f, 1f), 0f, -10f, 12f, 0.3f),
                    K(0.7f, -80f, 0f, -10f, -75f, -5f, -15f, new Vector3(0f, 0.02f, 1f), 0f, -8f, 10f, 0.24f),
                    Back(ready, 1f) } };
        }

        /// <summary>A straight punch with the other hand.</summary>
        static AttackMove FistCross()
        {
            var ready = Fist().keys[0];
            return new AttackMove {
                name = "Cross", weapon = WeaponId.None, duration = 0.42f, hitAt = 0.5f, twoHanded = true,
                keys = new[] {
                    ready,
                    K(0.3f, -35f, 10f, -95f, -15f, -12f, -120f, new Vector3(0f, 0f, 1f), 0f, -15f, -3f, -0.03f),
                    K(0.5f, -35f, 10f, -95f, -88f, 0f, 0f, new Vector3(0f, 0f, 1f), 0f, 18f, 8f, 0.2f),
                    Back(ready, 1f) } };
        }

        /// <summary>A rising uppercut to finish.</summary>
        static AttackMove FistUppercut()
        {
            var ready = Fist().keys[0];
            return new AttackMove {
                name = "Uppercut", weapon = WeaponId.None, duration = 0.5f, hitAt = 0.52f, power = 1.5f,
                keys = new[] {
                    ready,
                    K(0.32f, 10f, 10f, -110f, -35f, -10f, -95f, new Vector3(0f, 0f, 1f), 0f, 25f, 8f, -0.05f),
                    K(0.52f, -110f, 5f, -60f, -35f, -10f, -95f, new Vector3(0f, 0f, 1f), 0f, -15f, -6f, 0.14f),
                    Back(ready, 1f) } };
        }

        /// <summary>A great two-handed overhead chop: axe raised high behind the head, brought down with the whole body.</summary>
        static AttackMove Axe()
        {
            var ready = K(0f, -50f, -12f, -60f, -50f, 12f, -60f, new Vector3(0f, 0.8f, -0.3f), 0f, 0f, 0f, 0f);
            return new AttackMove {
                name = "Overhead chop", weapon = WeaponId.TwoHandAxe, duration = 0.62f, hitAt = 0.56f, twoHanded = true,
                keys = new[] {
                    ready,
                    K(0.3f, -170f, -8f, -55f, -165f, 8f, -55f, new Vector3(0f, 0.2f, -1f), 0f, 8f, -10f, -0.02f),
                    K(0.46f, -125f, -6f, -25f, -120f, 6f, -25f, new Vector3(0f, 1f, 0.25f), 0f, 0f, -2f, 0.08f),
                    K(0.56f, -55f, -10f, -5f, -52f, 10f, -5f, new Vector3(0f, -0.08f, 1f), 0f, -4f, 18f, 0.24f),
                    K(0.74f, -30f, -12f, -15f, -28f, 12f, -15f, new Vector3(0f, -0.45f, 0.75f), 0f, -4f, 15f, 0.2f),
                    K(1f, ready.v[0], ready.v[1], ready.v[2], ready.v[3], ready.v[4], ready.v[5], new Vector3(ready.v[6], ready.v[7], ready.v[8]), 0f, 0f, 0f, 0f) } };
        }

        /// <summary>A diagonal slash: sword drawn back over the right shoulder, cut down and across to the left.</summary>
        static AttackMove Sword()
        {
            var ready = K(0f, -40f, 10f, -55f, -55f, -12f, -75f, new Vector3(0.15f, 0.75f, 0.65f), 90f, 0f, 0f, 0f);
            return new AttackMove {
                name = "Slash", weapon = WeaponId.Sword, duration = 0.5f, hitAt = 0.5f,
                keys = new[] {
                    ready,
                    K(0.3f, -150f, 45f, -80f, -60f, -12f, -80f, new Vector3(0.55f, 0.55f, -0.6f), 90f, 25f, -5f, -0.02f),
                    K(0.5f, -75f, -15f, -10f, -55f, -12f, -75f, new Vector3(-0.45f, 0.05f, 1f), 90f, -22f, 8f, 0.18f),
                    K(0.68f, -25f, -30f, -30f, -50f, -12f, -70f, new Vector3(-0.8f, -0.6f, 0.3f), 90f, -28f, 10f, 0.14f),
                    K(1f, ready.v[0], ready.v[1], ready.v[2], ready.v[3], ready.v[4], ready.v[5], new Vector3(ready.v[6], ready.v[7], ready.v[8]), 90f, 0f, 0f, 0f) } };
        }

        /// <summary>A thrust: the spear drawn back by the hip, driven straight out with a step, then drawn in.</summary>
        static AttackMove Spear()
        {
            var ready = K(0f, -40f, 8f, -75f, -50f, -12f, -65f, new Vector3(0f, 0.25f, 1f), 0f, 0f, 0f, 0f);
            return new AttackMove {
                name = "Thrust", weapon = WeaponId.Spear, duration = 0.55f, hitAt = 0.52f, slide = 0.45f,
                keys = new[] {
                    ready,
                    K(0.34f, -10f, 12f, -105f, -50f, -12f, -65f, new Vector3(0f, 0.12f, 1f), 0f, 18f, -4f, -0.06f),
                    K(0.52f, -85f, 0f, 0f, -55f, -12f, -60f, new Vector3(0f, 0f, 1f), 0f, -15f, 12f, 0.36f),
                    K(0.7f, -80f, 2f, -12f, -55f, -12f, -60f, new Vector3(0f, 0.02f, 1f), 0f, -12f, 10f, 0.3f),
                    K(1f, ready.v[0], ready.v[1], ready.v[2], ready.v[3], ready.v[4], ready.v[5], new Vector3(ready.v[6], ready.v[7], ready.v[8]), 0f, 0f, 0f, 0f) } };
        }

        /// <summary>Raise the bow, draw the string to the cheek side-on, loose, and hold the follow-through.</summary>
        static AttackMove Bow()
        {
            var ready = K(0f, -30f, 8f, -40f, -20f, -8f, -40f, new Vector3(0.3f, 0.8f, -0.3f), 0f, 0f, 0f, 0f);
            return new AttackMove {
                name = "Draw and loose", weapon = WeaponId.Bow, duration = 0.9f, hitAt = 0.66f, twoHanded = true,
                keys = new[] {
                    ready,
                    K(0.3f, -88f, 0f, -5f, -80f, 15f, -60f, new Vector3(0f, 1f, 0.12f), 0f, -20f, 0f, 0f),
                    K(0.62f, -88f, 0f, 0f, -88f, 30f, -140f, new Vector3(0f, 1f, 0.1f), 0f, -30f, 2f, 0f),
                    K(0.68f, -88f, 0f, 0f, -75f, 5f, -55f, new Vector3(0f, 1f, 0.12f), 0f, -28f, 0f, -0.02f),
                    K(0.85f, -80f, 0f, -8f, -60f, 0f, -50f, new Vector3(0f, 1f, 0.15f), 0f, -22f, 0f, 0f),
                    K(1f, ready.v[0], ready.v[1], ready.v[2], ready.v[3], ready.v[4], ready.v[5], new Vector3(ready.v[6], ready.v[7], ready.v[8]), 0f, 0f, 0f, 0f) } };
        }

        /// <summary>Raise the staff high, and bring it down in a sweeping blow.</summary>
        static AttackMove Staff()
        {
            var ready = K(0f, -30f, 12f, -50f, -20f, -8f, -40f, new Vector3(0.04f, 1f, 0.1f), 0f, 0f, 0f, 0f);
            return new AttackMove {
                name = "Staff blow", weapon = WeaponId.Staff, duration = 0.7f, hitAt = 0.56f, slide = 0.95f,
                keys = new[] {
                    ready,
                    K(0.36f, -150f, 20f, -30f, -30f, -8f, -40f, new Vector3(0f, 1f, -0.3f), 0f, 10f, -8f, -0.02f),
                    K(0.56f, -70f, 0f, -10f, -30f, -8f, -40f, new Vector3(0f, 0.05f, 1f), 0f, -8f, 12f, 0.2f),
                    K(0.76f, -55f, 0f, -25f, -25f, -8f, -40f, new Vector3(0f, -0.2f, 1f), 0f, -6f, 10f, 0.16f),
                    K(1f, ready.v[0], ready.v[1], ready.v[2], ready.v[3], ready.v[4], ready.v[5], new Vector3(ready.v[6], ready.v[7], ready.v[8]), 0f, 0f, 0f, 0f) } };
        }

        /// <summary>No weapon: a straight punch.</summary>
        static AttackMove Fist()
        {
            var ready = K(0f, -30f, 10f, -95f, -30f, -10f, -95f, new Vector3(0f, 0f, 1f), 0f, 0f, 0f, 0f);
            return new AttackMove {
                name = "Punch", weapon = WeaponId.None, duration = 0.4f, hitAt = 0.5f,
                keys = new[] {
                    ready,
                    K(0.3f, -15f, 12f, -120f, -35f, -10f, -95f, new Vector3(0f, 0f, 1f), 0f, 15f, -3f, -0.03f),
                    K(0.5f, -88f, 0f, 0f, -35f, -10f, -95f, new Vector3(0f, 0f, 1f), 0f, -15f, 8f, 0.2f),
                    K(1f, ready.v[0], ready.v[1], ready.v[2], ready.v[3], ready.v[4], ready.v[5], new Vector3(ready.v[6], ready.v[7], ready.v[8]), 0f, 0f, 0f, 0f) } };
        }

        /// <summary>The weapon's rotation in the fist that points its haft along <paramref name="haft"/> (body space).</summary>
        public static Quaternion WeaponRotation(Quaternion arm, Quaternion forearm, Vector3 haft, float roll)
        {
            haft = haft.sqrMagnitude > 0.0001f ? haft.normalized : Vector3.forward;
            Vector3 side = Mathf.Abs(haft.y) > 0.8f ? Vector3.back : Vector3.up;
            Quaternion inBody = Quaternion.LookRotation(haft, Vector3.Cross(Vector3.Cross(haft, side), haft).normalized);
            return Quaternion.Inverse(arm * forearm) * inBody * Quaternion.AngleAxis(roll, Vector3.forward);
        }

        /// <summary>
        /// Put the move at <paramref name="t"/> on the hero, blended over whatever pose it already has by the move's
        /// weight (so it eases in and out). The body turn, lean and lunge add to the current pose.
        /// </summary>
        public static void Apply(VikingBuilder.Parts parts, AttackMove move, float t)
        {
            if (parts == null || move == null) return;
            float w = move.Weight(t);
            if (w <= 0f) return;
            var v = move.Sample(t);
            var armR = Quaternion.Euler(v[(int)AttackMove.Ch.ArmRX], 0f, v[(int)AttackMove.Ch.ArmRZ]);
            var foreR = Quaternion.Euler(v[(int)AttackMove.Ch.ElbowR], 0f, 0f);
            parts.rightArm.localRotation = Quaternion.Slerp(parts.rightArm.localRotation, armR, w);
            if (parts.rightForearm != null) parts.rightForearm.localRotation = Quaternion.Slerp(parts.rightForearm.localRotation, foreR, w);
            var armL = Quaternion.Euler(v[(int)AttackMove.Ch.ArmLX], 0f, v[(int)AttackMove.Ch.ArmLZ]);
            var foreL = Quaternion.Euler(v[(int)AttackMove.Ch.ElbowL], 0f, 0f);
            // One-handed moves leave the shield arm to the guard (the block pose may be on it).
            float lw = move.twoHanded ? w : w * 0.5f;
            parts.leftArm.localRotation = Quaternion.Slerp(parts.leftArm.localRotation, armL, lw);
            if (parts.leftForearm != null) parts.leftForearm.localRotation = Quaternion.Slerp(parts.leftForearm.localRotation, foreL, lw);
            if (parts.axe != null)
            {
                var haft = new Vector3(v[(int)AttackMove.Ch.HaftX], v[(int)AttackMove.Ch.HaftY], v[(int)AttackMove.Ch.HaftZ]);
                var inFist = WeaponRotation(parts.rightArm.localRotation, parts.rightForearm != null ? parts.rightForearm.localRotation : Quaternion.identity, haft, v[(int)AttackMove.Ch.Roll]);
                parts.axe.localRotation = Quaternion.Slerp(parts.axe.localRotation, inFist, w);
                parts.axe.localPosition = Vector3.Lerp(parts.axe.localPosition, parts.axeRest + inFist * Vector3.forward * (move.slide * parts.scale), w);
            }
            parts.body.localRotation = parts.body.localRotation * Quaternion.Euler(v[(int)AttackMove.Ch.Pitch] * w, v[(int)AttackMove.Ch.Yaw] * w, 0f);
            parts.body.localPosition += new Vector3(0f, 0f, v[(int)AttackMove.Ch.Lunge] * w);
        }
    }
}
