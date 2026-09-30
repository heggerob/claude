using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The people of the world, drawn as storybook heroes like the player: each is an outfit with its own colours
    /// and a body picked from a seed, so a band of Saxons or a raider crew aren't all the same person.
    /// </summary>
    public static class NpcHeroes
    {
        /// <summary>A monastery guard: the Spear Guard's kit in Saxon green and blue, with a sword and round shield.</summary>
        public static CharacterSpec Saxon(int seed)
        {
            var spec = CharacterSpec.Default(OutfitId.SpearGuard);
            spec.body = RandomBody(seed);
            var p = Outfits.Get(OutfitId.SpearGuard).palette();
            p.cloth = new Color(0.33f, 0.4f, 0.26f);
            p.clothDark = new Color(0.2f, 0.24f, 0.17f);
            p.accent = new Color(0.22f, 0.32f, 0.58f);
            p.emblem = new Color(0.86f, 0.82f, 0.7f);
            p.hair = Hair(seed);
            spec.palette = p;
            spec.weapon = WeaponId.Sword;
            spec.offHand = OffHandId.RoundShield;
            return spec;
        }

        /// <summary>A market trader in a real town: the Navigator's coat in a merchant's colours, with a map of the routes.</summary>
        public static CharacterSpec Merchant(int seed)
        {
            var spec = CharacterSpec.Default(OutfitId.Navigator);
            spec.body = RandomBody(seed);
            var p = Outfits.Get(OutfitId.Navigator).palette();
            var rng = new System.Random(seed);
            Color[] coats = { new Color(0.45f, 0.22f, 0.3f), new Color(0.25f, 0.35f, 0.5f), new Color(0.5f, 0.42f, 0.2f), new Color(0.3f, 0.42f, 0.3f) };
            p.cloth = coats[rng.Next(coats.Length)];
            p.clothDark = p.cloth * 0.65f;
            p.hair = Hair(seed);
            spec.palette = p;
            spec.weapon = WeaponId.None;
            spec.offHand = OffHandId.Map;
            return spec;
        }

        /// <summary>A Norse hall's housecarl: the Spear Guard's kit in red and brown, with an axe and a round shield.</summary>
        public static CharacterSpec NorseGuard(int seed)
        {
            var spec = CharacterSpec.Default(OutfitId.SpearGuard);
            spec.body = RandomBody(seed);
            var p = Outfits.Get(OutfitId.SpearGuard).palette();
            p.cloth = new Color(0.5f, 0.2f, 0.14f);
            p.clothDark = new Color(0.3f, 0.14f, 0.1f);
            p.accent = new Color(0.28f, 0.22f, 0.16f);
            p.hair = Hair(seed);
            spec.palette = p;
            spec.weapon = WeaponId.Sword;
            spec.offHand = OffHandId.RoundShield;
            return spec;
        }

        /// <summary>A Danish raider archer: the Raider's furs in soot and dark leather, with a bow.</summary>
        public static CharacterSpec DanishRaider(int seed)
        {
            var spec = CharacterSpec.Default(OutfitId.Raider);
            spec.body = RandomBody(seed);
            var p = Outfits.Get(OutfitId.Raider).palette();
            p.cloth = new Color(0.2f, 0.18f, 0.17f);
            p.clothDark = new Color(0.1f, 0.09f, 0.09f);
            p.accent = new Color(0.12f, 0.12f, 0.12f);
            p.emblem = new Color(0.75f, 0.2f, 0.15f);
            p.fur = new Color(0.45f, 0.38f, 0.3f);
            p.furShadow = new Color(0.3f, 0.25f, 0.2f);
            p.hair = Hair(seed + 7);
            spec.palette = p;
            spec.weapon = WeaponId.Bow;
            spec.offHand = OffHandId.None;
            return spec;
        }

        /// <summary>Bjorn the mead-keeper: a big, broad old raider in red-brown, empty-handed behind his dice table.</summary>
        public static CharacterSpec Bjorn()
        {
            var spec = CharacterSpec.Default(OutfitId.Raider);
            spec.body = new BodyShape { height = 1.78f, width = 1.25f, gender = Gender.Male };
            var p = Outfits.Get(OutfitId.Raider).palette();
            p.cloth = new Color(0.5f, 0.22f, 0.15f);
            p.clothDark = new Color(0.3f, 0.14f, 0.1f);
            p.hair = new Color(0.62f, 0.42f, 0.22f);
            spec.palette = p;
            spec.hair = HairStyle.LongBraids;
            spec.weapon = WeaponId.None;
            spec.offHand = OffHandId.None;
            return spec;
        }

        /// <summary>Gunnar the trader: a Navigator's coat in ochre and green, a sea chart in hand.</summary>
        public static CharacterSpec Gunnar()
        {
            var spec = CharacterSpec.Default(OutfitId.Navigator);
            spec.body = new BodyShape { height = 1.7f, width = 1.05f, gender = Gender.Male };
            var p = Outfits.Get(OutfitId.Navigator).palette();
            p.cloth = new Color(0.55f, 0.45f, 0.2f);
            p.clothDark = new Color(0.36f, 0.29f, 0.14f);
            p.accent = new Color(0.3f, 0.45f, 0.3f);
            p.hair = new Color(0.85f, 0.85f, 0.8f);
            spec.palette = p;
            spec.weapon = WeaponId.None;
            spec.offHand = OffHandId.Map;
            return spec;
        }

        /// <summary>A believable body from a seed: 1.52–1.86 m, slim to broad, either gender.</summary>
        public static BodyShape RandomBody(int seed)
        {
            var rng = new System.Random(seed * 7919 + 13);
            return new BodyShape
            {
                height = BodyShape.ClampHeight(1.52f + (float)rng.NextDouble() * 0.34f),
                width = BodyShape.ClampWidth(0.85f + (float)rng.NextDouble() * 0.35f),
                gender = rng.NextDouble() < 0.5 ? Gender.Male : Gender.Female,
            };
        }

        static readonly Color[] hairs = {
            new Color(0.72f, 0.26f, 0.12f), new Color(0.88f, 0.72f, 0.42f), new Color(0.35f, 0.22f, 0.12f),
            new Color(0.15f, 0.12f, 0.1f), new Color(0.6f, 0.42f, 0.25f), new Color(0.8f, 0.78f, 0.72f) };

        static Color Hair(int seed) { return hairs[((seed % hairs.Length) + hairs.Length) % hairs.Length]; }
    }

    /// <summary>Poses shared by everyone who fights, for both the storybook heroes and the old smooth Vikings.</summary>
    public static class HeroPose
    {
        /// <summary>
        /// How much the elbow bends (degrees, negative = forearm forward) for an upper arm swung by
        /// <paramref name="armPitch"/>: always a little, and more as the arm swings forward, like a relaxed walk.
        /// </summary>
        public static float WalkElbow(float armPitch) { return -10f - Mathf.Max(0f, -armPitch) * 0.6f; }

        /// <summary>
        /// The elbow through a chop, <paramref name="t"/> 0..1: folded tight during the wind-up, snapping straight
        /// as the blow lands, easing back after.
        /// </summary>
        public static float ChopElbow(float t)
        {
            t = Mathf.Clamp01(t);
            if (t < 0.35f) return Mathf.Lerp(-10f, -95f, t / 0.35f);
            if (t < 0.55f) return Mathf.Lerp(-95f, 0f, (t - 0.35f) / 0.2f);
            return Mathf.Lerp(0f, -10f, (t - 0.55f) / 0.45f);
        }

        /// <summary>A slow breath: the body rocks forward and back by a degree or two, about every four seconds.</summary>
        public static float Breath(float time) { return Mathf.Sin(time * 1.6f) * 1.4f; }

        /// <summary>
        /// Proud posture, as in the concept art: the body leans back a little from the ankles so the chest is carried
        /// high (the legs turn the other way to stay upright), and the head is lifted to look up and out.
        /// </summary>
        public const float Lean = -3.5f, HeadUp = -9f;

        /// <summary>The body's rotation for a forward tilt of <paramref name="pitch"/> degrees on top of the posture.</summary>
        public static Quaternion Torso(float pitch) { return Quaternion.Euler(Lean + pitch, 0f, 0f); }

        /// <summary>A leg swung by <paramref name="swing"/> degrees, standing straight under the leaning body.</summary>
        public static Quaternion Leg(float swing) { return Quaternion.Euler(swing - Lean, 0f, 0f); }

        /// <summary>Local rotations for carrying a big axe over the right shoulder: arm out, elbow folded, haft back.</summary>
        public static readonly Vector3 AxeCarryArm = new Vector3(-10f, 0f, 55f);
        public static readonly Vector3 AxeCarryForearm = new Vector3(-150f, 0f, 0f);
        /// <summary>
        /// The axe's local rotation in the fist for the carry: worked out from where the haft should lie in the body's
        /// space (up and back across the shoulders towards the left, the blade facing forward), whatever the arm does.
        /// </summary>
        public static Quaternion AxeCarryWeapon { get { return WeaponInFist(CarryFor(WeaponId.TwoHandAxe)); } }

        /// <summary>
        /// Carry a big axe over the shoulder (by <paramref name="amount"/> 0..1): the right arm swings out and folds up
        /// so the fist sits by the shoulder, and the haft lies back across it with the head behind.
        /// </summary>
        public static void AxeCarry(VikingBuilder.Parts parts, float amount) { Carry(parts, WeaponId.TwoHandAxe, amount); }

        /// <summary>How a weapon is carried while walking: upper arm, forearm (local Euler) and the haft's direction in the body.</summary>
        public struct CarryPose { public Vector3 arm, forearm, haft; public bool set; /// <summary>Turn about the haft, degrees (to show a blade's flat).</summary>
            public float roll;
            /// <summary>How far the grip slides down the haft (metres on the reference body), lifting a long weapon clear of the ground.</summary>
            public float slide; }

        public static CarryPose CarryFor(WeaponId w)
        {
            switch (w)
            {
                case WeaponId.TwoHandAxe: return new CarryPose { set = true, arm = AxeCarryArm, forearm = AxeCarryForearm, haft = new Vector3(-1f, 0.3f, -0.55f) };
                // Long hafts held upright in a fist at chest height, the butt clear of the ground.
                case WeaponId.Spear: return new CarryPose { set = true, arm = new Vector3(-6f, 0f, 30f), forearm = new Vector3(-50f, 0f, -18f), haft = new Vector3(0.03f, 1f, 0.05f), slide = 0.22f };
                case WeaponId.Staff: return new CarryPose { set = true, arm = new Vector3(-6f, 0f, 28f), forearm = new Vector3(-50f, 0f, -16f), haft = new Vector3(0.04f, 1f, 0.08f), slide = 0.44f };
                // A sword hangs point-down from a loose fist, angled out and forward like the Jarl's.
                case WeaponId.Sword: return new CarryPose { set = true, arm = new Vector3(-6f, 0f, 14f), forearm = new Vector3(-62f, 0f, -8f), haft = new Vector3(0.12f, -1f, 0.66f), roll = 90f };
                // A bow held low at the side, slanting down and back.
                case WeaponId.Bow: return new CarryPose { set = true, arm = new Vector3(0f, 0f, 10f), forearm = new Vector3(-20f, 0f, 0f), haft = new Vector3(0.35f, 0.75f, -0.4f) };
                default: return new CarryPose();
            }
        }

        /// <summary>The weapon's local rotation in the fist that makes its haft lie along <paramref name="p"/>.haft in body space.</summary>
        public static Quaternion WeaponInFist(CarryPose p)
        {
            Vector3 haft = p.haft.normalized;
            // Upright things turn their flat side (a pennant, a rune stone) out to the right; others face forward.
            Vector3 side = Mathf.Abs(haft.y) > 0.8f ? Vector3.back : Vector3.forward;
            Quaternion inBody = Quaternion.LookRotation(haft, Vector3.Cross(side, haft));
            return Quaternion.Inverse(Quaternion.Euler(p.arm) * Quaternion.Euler(p.forearm)) * inBody * Quaternion.AngleAxis(p.roll, Vector3.forward);
        }

        /// <summary>Carry the hero's weapon the way it's meant to be carried (if it has a carry pose).</summary>
        public static void Carry(VikingBuilder.Parts parts, WeaponId w, float amount)
        {
            var p = CarryFor(w);
            if (!p.set || parts == null || parts.rightForearm == null || amount <= 0.001f) return;
            parts.rightArm.localRotation = Quaternion.Slerp(parts.rightArm.localRotation, Quaternion.Euler(p.arm), amount);
            parts.rightForearm.localRotation = Quaternion.Slerp(parts.rightForearm.localRotation, Quaternion.Euler(p.forearm), amount);
            if (parts.axe == null) return;
            parts.axe.localRotation = Quaternion.Slerp(parts.axe.localRotation, WeaponInFist(p), amount);
            parts.axe.localPosition = Vector3.Lerp(parts.axe.localPosition, parts.axeRest + GripSlide(p, parts.scale), amount);
        }

        /// <summary>How far the weapon moves in the fist (forearm space) when held further down the haft.</summary>
        public static Vector3 GripSlide(CarryPose p, float scale)
        {
            return WeaponInFist(p) * Vector3.forward * (p.slide * scale);
        }

        /// <summary>Put the weapon back where the fist holds it at rest (after a carry slid it along the haft).</summary>
        public static void RestGrip(VikingBuilder.Parts parts)
        {
            if (parts != null && parts.axe != null && parts.rightForearm != null) parts.axe.localPosition = parts.axeRest;
        }

        /// <summary>Set both elbows (only storybook heroes have them).</summary>
        public static void Elbows(VikingBuilder.Parts parts, float left, float right)
        {
            if (parts == null || parts.leftForearm == null) return;
            parts.leftForearm.localRotation = Quaternion.Euler(left, 0f, 0f);
            parts.rightForearm.localRotation = Quaternion.Euler(right, 0f, 0f);
        }

        /// <summary>
        /// Bring the shield up (<paramref name="amount"/> 0 = down, 1 = raised). Heroes lift the shield arm at the
        /// shoulder and elbow; the old models slide the shield from their back to the front.
        /// </summary>
        public static void Block(VikingBuilder.Parts parts, float amount)
        {
            if (parts == null) return;
            if (parts.leftForearm != null)
            {
                if (amount > 0.01f)
                {
                    parts.leftArm.localRotation = Quaternion.Euler(-55f * amount, 0f, 25f * amount);
                    parts.leftForearm.localRotation = Quaternion.Euler(-60f * amount, 0f, -20f * amount);
                    if (parts.shield != null) parts.shield.localRotation = Quaternion.Euler(0f, -70f * amount, 0f);
                }
                else
                {
                    parts.leftForearm.localRotation = Quaternion.identity;
                    if (parts.shield != null) parts.shield.localRotation = Quaternion.identity;
                }
                return;
            }
            if (parts.shield == null) return;
            parts.shield.localPosition = Vector3.Lerp(new Vector3(0f, 1.25f, -0.24f), new Vector3(-0.3f, 1.3f, 0.5f), amount);
            parts.shield.localRotation = Quaternion.Euler(0f, 180f * amount, 0f);
            if (amount > 0.01f) parts.leftArm.localRotation = Quaternion.Euler(-80f * amount, 0f, 0f);
        }
    }
}
