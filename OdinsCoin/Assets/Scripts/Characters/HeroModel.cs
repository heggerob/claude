using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Builds a character from a <see cref="CharacterSpec"/>, in layers:
    /// 1. the body (the signature: big round face, two black dash eyes, no mouth, stick-thin black limbs),
    ///    sized by height, width and gender;
    /// 2. hair;
    /// 3. the outfit's clothes, fitted to the body and painted in the palette;
    /// 4. an ink outline around everything.
    /// Weapons are separate (<see cref="BuildWeapon"/>) and attach to the weapon hand.
    /// </summary>
    public static class HeroModel
    {
        public const float OutlineWidth = 0.0065f;

        public static VikingModel Build(CharacterSpec spec)
        {
            var fit = Fit.Of(spec.body);
            var outfit = Outfits.Get(spec.outfit);
            var d = new Dresser { model = new VikingModel(), fit = fit, pal = spec.palette ?? outfit.palette(), seed = 17 + (int)spec.outfit * 31 };
            Joints.Build(d.model, fit);
            BaseBody(d);
            Hair(d, spec.hair);
            outfit.dress(d);
            d.model.AddOutlines(OutlineWidth * fit.s, d.pal.ink);
            return d.model;
        }

        /// <summary>The weapon on its own: pieces on the <see cref="Joints.Weapon"/> joint, to attach to the hand.</summary>
        public static VikingModel BuildWeapon(CharacterSpec spec)
        {
            var fit = Fit.Of(spec.body);
            var d = new Dresser { model = new VikingModel(), fit = fit, pal = spec.palette ?? Outfits.Get(spec.outfit).palette(), seed = 5 };
            d.model.AddJoint(Joints.Weapon, null, Vector3.zero);
            Weapons.Build(d, spec.weapon);
            d.model.AddOutlines(OutlineWidth * fit.s, d.pal.ink);
            return d.model;
        }

        /// <summary>Height of the head's centre above the neck joint.</summary>
        public static float HeadCentre(Fit f) { return f.headY - f.neckY; }

        static void BaseBody(Dresser d)
        {
            var f = d.fit;
            float s = f.s;
            var ink = d.pal.ink;
            // Stick legs from the hip to the sole, stick arms in two parts (so the elbows bend), a stick neck.
            foreach (var leg in new[] { Joints.LeftLeg, Joints.RightLeg })
                d.Add(leg, ink, CharacterKit.Stick(new Vector3(0f, 0.02f * s, 0f), new Vector3(0f, -f.hip + 0.02f * s, 0f), f.limbR * 1.15f), false);
            foreach (var arm in new[] { Joints.LeftArm, Joints.RightArm })
                d.Add(arm, ink, CharacterKit.Stick(new Vector3(0f, 0.01f * s, 0f), new Vector3(0f, -f.upperArm, 0f), f.limbR), false);
            foreach (var fore in new[] { Joints.LeftForearm, Joints.RightForearm })
            {
                d.Add(fore, ink, MeshData.Ellipsoid(Vector3.zero, Vector3.one * f.limbR * 1.2f, 6, 4), false); // elbow
                d.Add(fore, ink, CharacterKit.Stick(Vector3.zero, new Vector3(0f, -f.foreArm, 0f), f.limbR), false);
                d.Add(fore, ink, MeshData.Ellipsoid(new Vector3(0f, -f.foreArm - 0.02f * s, 0.005f), new Vector3(0.024f, 0.03f, 0.026f) * s, 8, 5), false); // bare hand
            }
            d.Add(Joints.Body, ink, CharacterKit.Stick(new Vector3(0f, f.hip, 0f), new Vector3(0f, f.neckY + 0.03f * s, 0f), f.limbR * 1.3f), false);

            // The face: a big round head with two tall dash eyes a little below the middle. No nose, no mouth.
            float cy = HeadCentre(f), r = f.headR;
            d.Add(Joints.Head, d.pal.skin, MeshData.Ellipsoid(new Vector3(0f, cy, 0f), new Vector3(r * 1.02f, r, r * 0.98f), 20, 14));
            float eyeX = 0.33f * r;
            foreach (float x in new[] { -eyeX, eyeX })
            {
                float z = Mathf.Sqrt(Mathf.Max(0f, r * r * 0.96f - x * x)) - 0.005f * s;
                var eye = MeshData.Ellipsoid(Vector3.zero, new Vector3(0.078f * r, 0.23f * r, 0.06f * r), 8, 6);
                d.Add(Joints.Head, ink, eye.Transformed(new Vector3(x, cy - 0.34f * r, z), Quaternion.Euler(0f, Mathf.Atan2(x, z) * Mathf.Rad2Deg, 0f), Vector3.one), false);
            }
        }

        static void Hair(Dresser d, HairStyle hair)
        {
            switch (hair)
            {
                case HairStyle.LongBraids: Garments.LongBraids(d, 0.42f); break;
            }
        }
    }

    /// <summary>Weapons are add-ons, not part of the character: built on their own and held in the weapon hand.</summary>
    public static class Weapons
    {
        public static void Build(Dresser d, WeaponId id)
        {
            float s = d.S;
            switch (id)
            {
                case WeaponId.TwoHandAxe:
                    // A long ash haft with iron bands and a pommel cap, and a broad bearded head.
                    // Held at the lower end, haft pointing forward (+Z) from the fist.
                    d.Add(Joints.Weapon, d.pal.leather, MeshData.Tube(new[] { new Vector3(0f, 0f, -0.1f * s), new Vector3(0f, 0f, 0.95f * s) }, new[] { 0.026f * s, 0.024f * s }, 10));
                    d.Add(Joints.Weapon, d.pal.leatherDark, MeshData.Lathe(new[] { new Vector2(0.032f * s, 0f), new Vector2(0.032f * s, 0.04f * s) }, 10).Transformed(new Vector3(0f, 0f, -0.12f * s), Quaternion.Euler(90f, 0f, 0f), Vector3.one));
                    foreach (float z in new[] { 0.3f, 0.7f })
                        d.Add(Joints.Weapon, d.pal.metal, MeshData.Lathe(new[] { new Vector2(0.03f * s, 0f), new Vector2(0.03f * s, 0.03f * s) }, 10).Transformed(new Vector3(0f, 0f, z * s), Quaternion.Euler(90f, 0f, 0f), Vector3.one), false);
                    d.Add(Joints.Weapon, d.pal.metal, MeshData.Extrude(new[] {
                        new Vector2(0.78f, 0.05f), new Vector2(0.95f, 0.05f), new Vector2(1.0f, 0.12f), new Vector2(1.09f, 0.2f), new Vector2(1.08f, -0.2f),
                        new Vector2(1.0f, -0.12f), new Vector2(0.95f, -0.05f), new Vector2(0.78f, -0.05f) }, 0.03f)
                        .Transformed(Vector3.zero, Quaternion.identity, Vector3.one * s));
                    d.Add(Joints.Weapon, VikingModel.Shade(d.pal.metal, 1.35f), MeshData.Extrude(new[] {
                        new Vector2(1.06f, 0.2f), new Vector2(1.1f, 0.21f), new Vector2(1.1f, -0.21f), new Vector2(1.06f, -0.2f) }, 0.022f)
                        .Transformed(Vector3.zero, Quaternion.identity, Vector3.one * s), false);
                    break;
            }
        }
    }
}
