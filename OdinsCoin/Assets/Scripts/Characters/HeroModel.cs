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

        /// <summary>The off-hand item (a shield...) on its own, on the <see cref="Joints.OffHand"/> joint.</summary>
        public static VikingModel BuildOffHand(CharacterSpec spec)
        {
            var fit = Fit.Of(spec.body);
            var d = new Dresser { model = new VikingModel(), fit = fit, pal = spec.palette ?? Outfits.Get(spec.outfit).palette(), seed = 9 };
            d.model.AddJoint(Joints.OffHand, null, Vector3.zero);
            Weapons.BuildOffHand(d, spec.offHand);
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
                case HairStyle.WrappedBraids: Garments.LongBraids(d, 0.4f, true); break;
                case HairStyle.SideBraid: Garments.SideBraid(d, 0.36f); break;
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
                case WeaponId.Sword:
                    // A long broad sword pointing forward from the fist: leather grip, gold curved guard and pommel,
                    // a fuller and runes down the blade.
                    d.Add(Joints.Weapon, d.pal.leatherDark, MeshData.Tube(new[] { new Vector3(0f, 0f, -0.07f * s), new Vector3(0f, 0f, 0.07f * s) }, new[] { 0.018f * s, 0.018f * s }, 8));
                    d.Add(Joints.Weapon, d.pal.brass, MeshData.Ellipsoid(new Vector3(0f, 0f, -0.095f * s), new Vector3(0.03f, 0.03f, 0.025f) * s, 8, 5));
                    d.Add(Joints.Weapon, d.pal.brass, MeshData.Tube(new[] { new Vector3(-0.11f * s, 0f, 0.1f * s), new Vector3(0f, 0f, 0.08f * s), new Vector3(0.11f * s, 0f, 0.1f * s) }, new[] { 0.012f * s, 0.016f * s, 0.012f * s }, 6));
                    d.Add(Joints.Weapon, new Color(0.78f, 0.8f, 0.82f), MeshData.Extrude(new[] {
                        new Vector2(0.09f, 0.045f), new Vector2(0.8f, 0.04f), new Vector2(0.9f, 0f), new Vector2(0.8f, -0.04f), new Vector2(0.09f, -0.045f) }, 0.014f)
                        .Transformed(Vector3.zero, Quaternion.Euler(0f, 0f, 90f), Vector3.one * s));
                    for (int i = 0; i < 6; i++)
                        d.Add(Joints.Weapon, d.pal.ink, MeshData.Box(new Vector3(0.009f * s, 0f, (0.2f + i * 0.09f) * s), new Vector3(0.003f, i % 2 == 0 ? 0.02f : 0.012f, 0.03f) * s), false);
                    break;
            }
        }

        public static void BuildOffHand(Dresser d, OffHandId id)
        {
            float s = d.S;
            switch (id)
            {
                case OffHandId.RoundShield:
                    // Held by the grip behind the boss, the face turned outwards (+X, away from the body on the left side).
                    var face = Quaternion.Euler(0f, -90f, 0f);
                    var centre = new Vector3(-0.05f * s, 0f, 0f);
                    for (int q = 0; q < 8; q++)
                        d.Add(Joints.OffHand, q % 2 == 0 ? VikingModel.Shade(d.pal.accent, 0.72f) : VikingModel.Shade(d.pal.leather, 0.8f),
                            MeshData.Wedge(0.29f * s, 0.03f * s, q * 45f, q * 45f + 45f, 4).Transformed(centre, face, Vector3.one));
                    d.Add(Joints.OffHand, d.pal.metal, MeshData.Dome(Vector3.zero, new Vector3(0.07f, 0.06f, 0.07f) * s, 12, 4).Transformed(centre + new Vector3(-0.015f * s, 0f, 0f), Quaternion.Euler(0f, 0f, 90f), Vector3.one));
                    var rim = new Vector3[33];
                    var radii = new float[33];
                    for (int i = 0; i < rim.Length; i++)
                    {
                        float a = i / 32f * Mathf.PI * 2f;
                        rim[i] = centre + face * new Vector3(Mathf.Cos(a) * 0.29f * s, Mathf.Sin(a) * 0.29f * s, 0f);
                        radii[i] = 0.022f * s;
                    }
                    d.Add(Joints.OffHand, d.pal.brass, MeshData.Tube(rim, radii, 6));
                    break;
                case OffHandId.Map:
                    // A sheet of parchment with torn edges, gently curled, held up facing forward (+Z) above the fist,
                    // with a compass rose and a coastline drawn in ink.
                    const int rows = 6, cols = 5;
                    var grid = new Vector3[rows, cols];
                    var rng = new System.Random(3);
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < cols; c++)
                        {
                            float u = c / (float)(cols - 1) - 0.5f, v = r / (float)(rows - 1);
                            float torn = (r == 0 || r == rows - 1 || c == 0 || c == cols - 1) ? ((float)rng.NextDouble() - 0.5f) * 0.02f : 0f;
                            grid[r, c] = new Vector3((u * 0.26f + 0.06f + torn) * s, (0.2f - v * 0.3f + torn) * s, (0.03f + 0.02f * Mathf.Cos(u * 3f)) * s);
                        }
                    d.Add(Joints.OffHand, d.pal.parchment, CharacterKit.Sheet(grid, Vector3.forward, 0.006f * s));
                    var rose = new Vector3(0.09f * s, 0.07f * s, 0.052f * s);
                    var ring = new Vector3[17];
                    var rr = new float[17];
                    for (int i = 0; i < ring.Length; i++) { float a = i / 16f * Mathf.PI * 2f; ring[i] = rose + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.045f * s; rr[i] = 0.002f * s; }
                    d.Add(Joints.OffHand, d.pal.ink, MeshData.Tube(ring, rr, 4), false);
                    for (int k = 0; k < 8; k++)
                    {
                        float a = k * Mathf.PI / 4f, len = (k % 2 == 0 ? 0.075f : 0.04f) * s;
                        d.Add(Joints.OffHand, d.pal.ink, MeshData.Tube(new[] { rose, rose + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * len }, new[] { 0.0035f * s, 0.001f * s }, 4), false);
                    }
                    var coast = new[] { new Vector3(-0.04f, 0.16f, 0.05f), new Vector3(-0.01f, 0.12f, 0.052f), new Vector3(-0.03f, 0.06f, 0.052f), new Vector3(0.01f, 0.0f, 0.052f), new Vector3(-0.02f, -0.05f, 0.05f) };
                    for (int i = 0; i < coast.Length; i++) coast[i] *= s;
                    d.Add(Joints.OffHand, d.pal.ink, CharacterKit.ZigZag(coast, Vector3.right, 0.004f * s, 0.002f * s), false);
                    break;
            }
        }
    }
}
