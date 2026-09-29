using UnityEngine;

namespace OdinsCoin
{
    public enum HeadGear { None, NasalHelmet }

    /// <summary>
    /// How a storybook hero looks. The signature never changes: a big round face with two black dash eyes
    /// and no mouth, and thin black stick arms and legs. Everything else (clothes, fur, gear) is dressing.
    /// </summary>
    public class HeroLook
    {
        public string name = "Viking";
        public Color skin = new Color(0.96f, 0.86f, 0.74f);
        public Color ink = new Color(0.09f, 0.07f, 0.06f);
        public Color tunic = new Color(0.33f, 0.26f, 0.2f);
        public Color tunicDark = new Color(0.22f, 0.17f, 0.13f);
        public Color skirt = new Color(0.27f, 0.22f, 0.18f);
        public Color fur = new Color(0.86f, 0.82f, 0.74f);
        public Color furShadow = new Color(0.6f, 0.55f, 0.48f);
        public Color leather = new Color(0.4f, 0.26f, 0.15f);
        public Color leatherDark = new Color(0.22f, 0.15f, 0.1f);
        public Color brass = new Color(0.78f, 0.6f, 0.27f);
        public Color hair = new Color(0.72f, 0.3f, 0.14f);
        public Color iron = new Color(0.5f, 0.52f, 0.55f);
        public HeadGear headGear = HeadGear.NasalHelmet;
        /// <summary>Braids hanging from under the helmet, one each side.</summary>
        public bool braids = true;
        /// <summary>1 = an adult raider, about 1.75 m to the top of the head.</summary>
        public float height = 1f;
    }

    /// <summary>
    /// Builds a storybook hero as a <see cref="VikingModel"/> (same joints as the old Viking, so the game's
    /// animations drive it): stick limbs, gloves, fur-cuffed boots with wound straps, a tunic and ragged
    /// skirt, a pelt over the shoulders, a belt with brass fittings, and the face. Ink-outlined.
    /// </summary>
    public static class HeroModel
    {
        // Proportions from the concept art: the head is ~1/7 of the height, boots ~1.2 heads tall,
        // a short gap of bare stick leg between boot and hem, and a long torso and skirt.
        public const float SoleToBootTop = 0.3f, HemHeight = 0.5f, WaistHeight = 0.9f, ShoulderHeight = 1.34f;
        public const float NeckHeight = 1.42f, HeadRadius = 0.135f, HipWidth = 0.075f, ShoulderWidth = 0.2f;
        public const float ArmLength = 0.46f, LimbRadius = 0.012f, OutlineWidth = 0.007f;

        public static VikingModel Build(HeroLook look)
        {
            var m = new VikingModel();
            float hip = WaistHeight - 0.1f;
            m.AddJoint(VikingModel.Body, null, Vector3.zero);
            m.AddJoint(VikingModel.LeftLeg, VikingModel.Body, new Vector3(-HipWidth, hip, 0f));
            m.AddJoint(VikingModel.RightLeg, VikingModel.Body, new Vector3(HipWidth, hip, 0f));
            m.AddJoint(VikingModel.LeftArm, VikingModel.Body, new Vector3(-ShoulderWidth, ShoulderHeight, 0f));
            m.AddJoint(VikingModel.RightArm, VikingModel.Body, new Vector3(ShoulderWidth, ShoulderHeight, 0f));
            m.AddJoint(VikingModel.Head, VikingModel.Body, new Vector3(0f, NeckHeight, 0f));
            m.AddJoint(VikingModel.Weapon, VikingModel.RightArm, new Vector3(0f, -ArmLength - 0.05f, 0.02f));
            m.AddJoint(VikingModel.Shield, VikingModel.Body, new Vector3(0f, 1.05f, -0.2f));

            Legs(m, look, hip);
            Torso(m, look);
            Arms(m, look);
            Head(m, look);

            m.AddOutlines(OutlineWidth, look.ink);
            if (look.height != 1f) Scale(m, look.height);
            return m;
        }

        static void Scale(VikingModel m, float k)
        {
            foreach (var j in m.Joints) j.localPosition *= k;
            foreach (var p in m.Pieces)
                for (int i = 0; i < p.mesh.Vertices.Count; i++) p.mesh.Vertices[i] *= k;
        }

        // ---------------------------------------------------------------- legs and boots

        static void Legs(VikingModel m, HeroLook look, float hip)
        {
            foreach (var joint in new[] { VikingModel.LeftLeg, VikingModel.RightLeg })
            {
                float side = joint == VikingModel.LeftLeg ? -1f : 1f;
                float sole = -hip;
                // The stick leg, from inside the skirt down into the boot.
                m.Add(joint, look.ink, CharacterKit.Stick(new Vector3(0f, 0.02f, 0f), new Vector3(0f, sole + SoleToBootTop - 0.02f, 0f), LimbRadius), false);
                // A tall, slightly baggy boot with a pointed toe that turns up a little.
                m.Add(joint, look.leather, MeshData.Lathe(new[] {
                    new Vector2(0.055f, sole), new Vector2(0.068f, sole + 0.04f), new Vector2(0.062f, sole + 0.12f),
                    new Vector2(0.066f, sole + 0.22f), new Vector2(0.072f, sole + SoleToBootTop) }, 12));
                m.Add(joint, look.leather, MeshData.Tube(new[] {
                    new Vector3(0f, sole + 0.045f, 0.0f), new Vector3(0f, sole + 0.04f, 0.1f), new Vector3(0f, sole + 0.055f, 0.16f) },
                    new[] { 0.058f, 0.045f, 0.018f }, 10));
                m.Add(joint, look.leatherDark, MeshData.Lathe(new[] { new Vector2(0.06f, sole), new Vector2(0.06f, sole + 0.018f) }, 12)
                    .Transformed(new Vector3(0f, 0f, 0.05f), Quaternion.identity, new Vector3(1f, 1f, 2.2f)), false); // sole
                // Leather straps wound criss-cross up the boot.
                m.Add(joint, look.leatherDark, CharacterKit.Spiral(sole + 0.08f, sole + 0.26f, 0.068f, 1.5f, 0f, 0.007f), false);
                m.Add(joint, look.leatherDark, CharacterKit.Spiral(sole + 0.08f, sole + 0.26f, 0.068f, -1.5f, Mathf.PI, 0.007f), false);
                // Fur cuff at the top.
                m.Add(joint, look.fur, CharacterKit.FurRing(new Vector3(0f, sole + SoleToBootTop, 0f), 0.075f, 1f, 0.035f, 11, 0.05f, 11 + (int)side));
            }
        }

        // ---------------------------------------------------------------- torso

        static void Torso(VikingModel m, HeroLook look)
        {
            string b = VikingModel.Body;
            // Tunic: from the belt up to the shoulders, broad and a little flattened front to back.
            m.Add(b, look.tunic, MeshData.Lathe(new[] {
                new Vector2(0.15f, WaistHeight - 0.02f), new Vector2(0.155f, 1.02f), new Vector2(0.17f, 1.16f),
                new Vector2(0.175f, 1.26f), new Vector2(0.16f, ShoulderHeight), new Vector2(0.1f, 1.4f), new Vector2(0.05f, NeckHeight) }, 16)
                .Transformed(Vector3.zero, Quaternion.identity, new Vector3(1f, 1f, 0.72f)));
            // Ragged skirt: layered cloth from the belt down past the knees.
            m.Add(b, look.skirt, CharacterKit.RaggedSkirt(WaistHeight, 0.155f, HemHeight, 0.215f, 0.78f, 22, 0.06f, 7));
            // A darker under-layer peeking out below.
            m.Add(b, look.tunicDark, CharacterKit.RaggedSkirt(WaistHeight - 0.2f, 0.17f, HemHeight - 0.05f, 0.185f, 0.75f, 18, 0.04f, 8));
            // Belt, with a brass buckle and rings, and a pouch on each hip.
            m.Add(b, look.leatherDark, CharacterKit.Band(WaistHeight, 0.055f, 0.158f, 0.8f));
            m.Add(b, look.brass, MeshData.Box(new Vector3(0f, WaistHeight, 0.128f), new Vector3(0.07f, 0.06f, 0.015f)));
            m.Add(b, look.leatherDark, MeshData.Box(new Vector3(0f, WaistHeight, 0.134f), new Vector3(0.04f, 0.032f, 0.01f)), false);
            foreach (float x in new[] { -1f, 1f })
            {
                m.Add(b, look.leather, MeshData.Box(new Vector3(x * 0.13f, WaistHeight - 0.07f, 0.06f), new Vector3(0.07f, 0.085f, 0.045f)));
                m.Add(b, look.leatherDark, MeshData.Box(new Vector3(x * 0.13f, WaistHeight - 0.035f, 0.083f), new Vector3(0.072f, 0.03f, 0.008f)), false); // flap
                m.Add(b, look.brass, MeshData.Lathe(new[] { new Vector2(0.012f, -0.004f), new Vector2(0.012f, 0.004f) }, 8)
                    .Transformed(new Vector3(x * 0.13f, WaistHeight - 0.045f, 0.088f), Quaternion.Euler(90f, 0f, 0f), Vector3.one), false);
            }
            // A cross strap over the chest from the right shoulder to the left hip.
            m.Add(b, look.leatherDark, MeshData.Tube(new[] {
                new Vector3(0.14f, 1.3f, 0.1f), new Vector3(0.02f, 1.12f, 0.132f), new Vector3(-0.12f, 0.95f, 0.115f) },
                new[] { 0.012f, 0.012f, 0.012f }, 5), false);
            m.Add(b, look.brass, MeshData.Lathe(new[] { new Vector2(0.018f, -0.005f), new Vector2(0.018f, 0.005f) }, 10)
                .Transformed(new Vector3(0.02f, 1.12f, 0.142f), Quaternion.Euler(90f, 0f, 0f), Vector3.one), false);
            // The pelt over the shoulders: a thick shaggy collar that makes the silhouette wide.
            m.Add(b, look.fur, MeshData.Ellipsoid(new Vector3(0f, 1.33f, -0.005f), new Vector3(0.235f, 0.075f, 0.17f), 16, 8));
            m.Add(b, look.fur, CharacterKit.FurRing(new Vector3(0f, 1.3f, -0.005f), 0.225f, 0.74f, 0.07f, 26, 0.11f, 21, 0.8f));
            m.Add(b, look.furShadow, CharacterKit.FurRing(new Vector3(0f, 1.36f, -0.005f), 0.17f, 0.78f, 0.05f, 16, 0.07f, 22, 0.5f));
            // Neck: a short black stick.
            m.Add(b, look.ink, CharacterKit.Stick(new Vector3(0f, 1.36f, 0f), new Vector3(0f, NeckHeight + 0.06f, 0f), LimbRadius * 1.3f), false);
        }

        // ---------------------------------------------------------------- arms and gloves

        static void Arms(VikingModel m, HeroLook look)
        {
            foreach (var joint in new[] { VikingModel.LeftArm, VikingModel.RightArm })
            {
                m.Add(joint, look.ink, CharacterKit.Stick(new Vector3(0f, 0.02f, 0f), new Vector3(0f, -ArmLength + 0.02f, 0f), LimbRadius), false);
                // Chunky leather glove with a flared cuff.
                float w = -ArmLength;
                m.Add(joint, look.leatherDark, MeshData.Lathe(new[] { new Vector2(0.022f, w + 0.02f), new Vector2(0.04f, w + 0.085f), new Vector2(0.042f, w + 0.1f) }, 10));
                m.Add(joint, look.leatherDark, MeshData.Ellipsoid(new Vector3(0f, w - 0.02f, 0.008f), new Vector3(0.036f, 0.045f, 0.04f), 10, 6));
                m.Add(joint, look.leatherDark, MeshData.Ellipsoid(new Vector3(0f, w - 0.01f, 0.035f), new Vector3(0.016f, 0.028f, 0.016f), 6, 4)); // thumb
            }
        }

        // ---------------------------------------------------------------- the face

        static void Head(VikingModel m, HeroLook look)
        {
            string h = VikingModel.Head;
            float cy = 0.19f, r = HeadRadius;
            // The big round face: a sphere, a touch wider than tall.
            m.Add(h, look.skin, MeshData.Ellipsoid(new Vector3(0f, cy, 0f), new Vector3(r * 1.02f, r, r * 0.98f), 18, 12));
            // Two black dash eyes, tall and narrow, set a little below the middle. No nose, no mouth.
            foreach (float x in new[] { -0.04f, 0.04f })
            {
                float z = Mathf.Sqrt(Mathf.Max(0f, r * r * 0.96f - x * x)) - 0.006f;
                var eye = MeshData.Ellipsoid(Vector3.zero, new Vector3(0.0095f, 0.029f, 0.008f), 8, 6);
                m.Add(h, look.ink, eye.Transformed(new Vector3(x, cy - 0.02f, z), Quaternion.Euler(0f, Mathf.Atan2(x, z) * Mathf.Rad2Deg, 0f), Vector3.one), false);
            }

            if (look.braids)
                foreach (float x in new[] { -1f, 1f })
                    m.Add(h, look.hair, CharacterKit.Braid(new[] {
                        new Vector3(x * 0.11f, cy + 0.02f, -0.03f), new Vector3(x * 0.13f, cy - 0.12f, -0.01f), new Vector3(x * 0.12f, cy - 0.3f, 0.03f) }, 0.028f));

            if (look.headGear == HeadGear.NasalHelmet)
            {
                // A steel cap over the top half of the head with a brow band, a ridge and a nose guard.
                // Sits high, so the brow band is just above the eyes and most of the face shows.
                float brim = cy + 0.045f;
                m.Add(h, look.iron, MeshData.Dome(new Vector3(0f, brim, 0f), new Vector3(r * 1.06f, r * 1.02f, r * 1.04f), 18, 7));
                m.Add(h, Shade(look.iron, 0.78f), CharacterKit.Band(brim + 0.005f, 0.028f, r * 1.08f, 0.97f, 18));
                m.Add(h, Shade(look.iron, 0.78f), MeshData.Tube(new[] {
                    new Vector3(0f, brim + 0.02f, r * 1.05f), new Vector3(0f, brim + r * 0.75f, r * 0.62f), new Vector3(0f, brim + r * 1.04f, 0f),
                    new Vector3(0f, brim + r * 0.75f, -r * 0.62f), new Vector3(0f, brim + 0.02f, -r * 1.05f) },
                    new[] { 0.011f, 0.012f, 0.012f, 0.012f, 0.011f }, 6));
                m.Add(h, Shade(look.iron, 0.78f), MeshData.Box(new Vector3(0f, brim - 0.035f, r * 1.01f), new Vector3(0.02f, 0.075f, 0.016f)));
                // Rivets along the brow band.
                for (int i = -3; i <= 3; i++)
                {
                    float a = i * 0.32f;
                    m.Add(h, Shade(look.iron, 1.2f), MeshData.Ellipsoid(new Vector3(Mathf.Sin(a) * r * 1.1f, brim + 0.005f, Mathf.Cos(a) * r * 1.07f), Vector3.one * 0.006f, 5, 3), false);
                }
            }
        }

        public static Color Shade(Color c, float k) { return VikingModel.Shade(c, k); }
    }
}
