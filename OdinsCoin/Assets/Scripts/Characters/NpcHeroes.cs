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
