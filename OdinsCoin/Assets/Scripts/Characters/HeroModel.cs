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
            var d = new Dresser { model = new VikingModel(), fit = fit, pal = spec.Paint(), seed = 17 + (int)spec.outfit * 31 };
            Joints.Build(d.model, fit);
            // How the hand holds its weapon at rest (weapons are built pointing forward from the fist).
            d.model.Find(Joints.Weapon).restEuler = Weapons.RestEuler(spec.weapon);
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
            var d = new Dresser { model = new VikingModel(), fit = fit, pal = spec.Paint(), seed = 5 };
            d.model.AddJoint(Joints.Weapon, null, Vector3.zero);
            Weapons.Build(d, spec.weapon);
            d.model.AddOutlines(OutlineWidth * fit.s, d.pal.ink);
            return d.model;
        }

        /// <summary>The off-hand item (a shield...) on its own, on the <see cref="Joints.OffHand"/> joint.</summary>
        public static VikingModel BuildOffHand(CharacterSpec spec)
        {
            var fit = Fit.Of(spec.body);
            var d = new Dresser { model = new VikingModel(), fit = fit, pal = spec.Paint(), seed = 9 };
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
            // The eyes hang on joints of their own at eye height, so they can blink (squash) and swap for other
            // expressions: ^ ^ when happy, > < when hurt.
            float eyeX = 0.33f * r, eyeY = cy - 0.34f * r;
            var pivot = new Vector3(0f, eyeY, 0f);
            d.model.AddJoint(Joints.Eyes, Joints.Head, pivot);
            d.model.AddJoint(Joints.EyesHappy, Joints.Head, pivot);
            d.model.AddJoint(Joints.EyesHurt, Joints.Head, pivot);
            d.model.Find(Joints.EyesHappy).hidden = true;
            d.model.Find(Joints.EyesHurt).hidden = true;
            foreach (float x in new[] { -eyeX, eyeX })
            {
                float z = Mathf.Sqrt(Mathf.Max(0f, r * r * 0.96f - x * x)) - 0.005f * s;
                var face = Quaternion.Euler(0f, Mathf.Atan2(x, z) * Mathf.Rad2Deg, 0f);
                var at = new Vector3(x, eyeY, z) - pivot;
                d.Add(Joints.Eyes, ink, Eye(r).Transformed(at, face, Vector3.one), false);
                // Happy: a little arch, like a smile turned into an eye.
                var arch = new Vector3[7];
                var ar = new float[7];
                for (int i = 0; i < arch.Length; i++)
                {
                    float a = Mathf.Lerp(Mathf.PI * 0.95f, Mathf.PI * 0.05f, i / 6f);
                    arch[i] = at + face * new Vector3(Mathf.Cos(a) * 0.11f * r, Mathf.Sin(a) * 0.12f * r - 0.04f * r, 0.012f * r);
                    ar[i] = 0.036f * r;
                }
                d.Add(Joints.EyesHappy, ink, MeshData.Tube(arch, ar, 6), false);
                // Hurt: chevrons squeezed shut, pointing in towards the nose.
                float inward = x < 0f ? 1f : -1f;
                var chev = new[] {
                    at + face * new Vector3(-inward * 0.08f * r, 0.11f * r, 0.012f * r),
                    at + face * new Vector3(inward * 0.07f * r, 0f, 0.012f * r),
                    at + face * new Vector3(-inward * 0.08f * r, -0.11f * r, 0.012f * r) };
                d.Add(Joints.EyesHurt, ink, MeshData.Tube(chev, new[] { 0.034f * r, 0.036f * r, 0.034f * r }, 6), false);
            }
        }

        /// <summary>
        /// One eye as in the concept art: two round dots stacked on top of each other and joined, a tall pill with
        /// round ends (not an oval), flattened onto the face.
        /// </summary>
        public static MeshData Eye(float headR)
        {
            float w = 0.08f * headR, half = 0.15f * headR;
            var profile = new System.Collections.Generic.List<Vector2>();
            // The outline of a stadium: the lower circle, straight sides, the upper circle.
            for (int i = 0; i <= 6; i++) { float a = -Mathf.PI * 0.5f + i / 6f * Mathf.PI * 0.5f; profile.Add(new Vector2(Mathf.Max(0.0005f, Mathf.Cos(a) * w), -half + Mathf.Sin(a) * w)); }
            for (int i = 0; i <= 6; i++) { float a = i / 6f * Mathf.PI * 0.5f; profile.Add(new Vector2(Mathf.Max(0.0005f, Mathf.Cos(a) * w), half + Mathf.Sin(a) * w)); }
            return MeshData.Lathe(profile.ToArray(), 12).Transformed(Vector3.zero, Quaternion.identity, new Vector3(1f, 1f, 0.75f));
        }

        static void Hair(Dresser d, HairStyle hair)
        {
            switch (hair)
            {
                case HairStyle.LongBraids: Garments.LongBraids(d, 0.34f); break;
                case HairStyle.WrappedBraids: Garments.LongBraids(d, 0.4f, true); break;
                case HairStyle.SideBraid: Garments.SideBraid(d, 0.36f); break;
                case HairStyle.ShortLocks: Garments.ShortLocks(d); break;
                case HairStyle.VeryLongBraids: Garments.LongBraids(d, 0.85f); break;
                case HairStyle.LowBraid: Garments.LowBraid(d); break;
                case HairStyle.SwungBraids: Garments.SwungBraids(d, 0.4f); break;
            }
        }
    }

    /// <summary>Weapons are add-ons, not part of the character: built on their own and held in the weapon hand.</summary>
    public static class Weapons
    {
        /// <summary>
        /// How each weapon is carried when the arm hangs at rest: long hafts and staves upright, the sword point
        /// down, the axe tipped back so it leans against the shoulder. (Every weapon is modelled pointing +Z.)
        /// </summary>
        public static Vector3 RestEuler(WeaponId id)
        {
            switch (id)
            {
                case WeaponId.Spear:
                case WeaponId.Staff:
                case WeaponId.Bow: return new Vector3(-90f, 0f, 0f);
                case WeaponId.TwoHandAxe: return new Vector3(-115f, 0f, 0f);
                case WeaponId.Sword: return new Vector3(80f, 0f, 0f);
                default: return Vector3.zero;
            }
        }

        /// <summary>The first <paramref name="count"/> outline points as a 3D polyline in the blade's plane (x offset for the bevel).</summary>
        /// <summary>The axe's cutting edge, t = 0 at the top horn to 1 at the tip of the beard, pushed out by <paramref name="off"/>.</summary>
        static Vector2 Edge(float t, float off)
        {
            return new Vector2(Mathf.Lerp(0.965f, 0.945f, t) + 0.065f * Mathf.Sin(t * Mathf.PI) + off, Mathf.Lerp(0.16f, -0.23f, t));
        }

        /// <summary>
        /// The back of the axe head for the same t as <see cref="Edge"/>: from just inside the top horn, dipping down to
        /// the neck on the haft, then the beard hooking out and down to its tip.
        /// </summary>
        static Vector2 AxeBack(float t)
        {
            if (t < 0.35f)
            {
                float u = t / 0.35f;
                return new Vector2(Mathf.Lerp(0.95f, 0.8f, u), 0.035f + 0.115f * (1f - u) * (1f - u));
            }
            if (t < 0.45f) return new Vector2(0.8f, Mathf.Lerp(0.035f, -0.035f, (t - 0.35f) / 0.1f));
            float v = (t - 0.45f) / 0.55f;
            return new Vector2(0.8f + 0.13f * v * v, -0.035f - 0.185f * v);
        }

        /// <summary>A darker, wider copy of a painted band, pushed back behind it so it shows as an outline.</summary>
        static MeshData KnotEdge(Vector3[] path, float radius, float back)
        {
            var p = new Vector3[path.Length];
            var r = new float[path.Length];
            for (int i = 0; i < path.Length; i++) { p[i] = path[i] - new Vector3(0f, 0f, back); r[i] = radius; }
            return MeshData.Tube(p, r, 6);
        }

        static Vector3[] ToPath(System.Collections.Generic.List<Vector2> pts, float x, float s, int count)
        {
            var p = new Vector3[count];
            for (int i = 0; i < count; i++) p[i] = new Vector3(x, pts[i].y, pts[i].x) * s;
            return p;
        }

        static float[] Radii(int count, float r)
        {
            var a = new float[count];
            for (int i = 0; i < count; i++) a[i] = r;
            return a;
        }

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
                    // The head: a socket round the haft, a narrow neck, then a broad crescent blade flaring up and down.
                    d.Add(Joints.Weapon, VikingModel.Shade(d.pal.metal, 0.8f), MeshData.Lathe(new[] { new Vector2(0.036f * s, 0f), new Vector2(0.036f * s, 0.1f * s) }, 10)
                        .Transformed(new Vector3(0f, 0f, 0.78f * s), Quaternion.Euler(90f, 0f, 0f), Vector3.one));
                    // A bearded head about the size of the face: a short top horn, a long beard sweeping down.
                    // Built as a strip from the back of the head (horn, neck, hooked beard) out to the curved edge, so the
                    // top and the beard can sweep inwards the way they do in the concept art.
                    const int bladeRows = 17;
                    var bladeGrid = new Vector3[bladeRows, 3];
                    for (int i = 0; i < bladeRows; i++)
                    {
                        float t = i / (float)(bladeRows - 1);
                        Vector2 inner = AxeBack(t), outer = Edge(t, 0f);
                        for (int c = 0; c < 3; c++)
                        {
                            var p = Vector2.Lerp(inner, outer, c / 2f);
                            bladeGrid[i, c] = new Vector3(0f, p.y, p.x) * s;
                        }
                    }
                    // Worn grey steel, lighter than the helm iron, as in the concept art.
                    d.Add(Joints.Weapon, VikingModel.Shade(d.pal.metal, 1.45f), CharacterKit.Sheet(bladeGrid, Vector3.right, 0.022f * s), true, SurfaceKind.Metal);
                    var edge = new System.Collections.Generic.List<Vector2>();
                    for (int i = 0; i <= 8; i++) edge.Add(Edge(i / 8f, 0.008f));
                    for (int i = 8; i >= 0; i--) edge.Add(Edge(i / 8f, -0.025f));
                    d.Add(Joints.Weapon, VikingModel.Shade(d.pal.metal, 2.1f), MeshData.Tube(ToPath(edge, 0.016f, s, 9), Radii(9, 0.012f * s), 4), false);
                    break;
                case WeaponId.Spear:
                    // A tall spear held about a third of the way up: an ash shaft with leather bindings, a leaf-shaped
                    // iron head on a socket, and a red pennant with a knot rune tied below the head.
                    d.Add(Joints.Weapon, d.pal.leather, MeshData.Tube(new[] { new Vector3(0f, 0f, -1.02f * s), new Vector3(0f, 0f, 1.0f * s) }, new[] { 0.02f * s, 0.018f * s }, 8));
                    foreach (float z in new[] { 0.76f, 0.88f })
                        d.Add(Joints.Weapon, d.pal.leatherDark, MeshData.Lathe(new[] { new Vector2(0.024f * s, 0f), new Vector2(0.024f * s, 0.05f * s) }, 8).Transformed(new Vector3(0f, 0f, z * s), Quaternion.Euler(90f, 0f, 0f), Vector3.one), false);
                    d.Add(Joints.Weapon, d.pal.metal, MeshData.Lathe(new[] { new Vector2(0.024f * s, 0f), new Vector2(0.018f * s, 0.08f * s) }, 8).Transformed(new Vector3(0f, 0f, 0.98f * s), Quaternion.Euler(90f, 0f, 0f), Vector3.one));
                    d.Add(Joints.Weapon, d.pal.metal, MeshData.Extrude(new[] {
                        new Vector2(1.05f, 0.0f), new Vector2(1.12f, 0.055f), new Vector2(1.26f, 0.042f), new Vector2(1.38f, 0.0f), new Vector2(1.26f, -0.042f), new Vector2(1.12f, -0.055f) }, 0.018f)
                        .Transformed(Vector3.zero, Quaternion.identity, Vector3.one * s));
                    d.Add(Joints.Weapon, VikingModel.Shade(d.pal.metal, 1.35f), MeshData.Box(new Vector3(0f, 0f, 1.21f * s), new Vector3(0.024f, 0.008f, 0.26f) * s), false);
                    {
                        // The pennant: tied at the shaft, flying sideways with two torn tails.
                        const int prow = 4, pcol = 6;
                        var grid = new Vector3[prow, pcol];
                        for (int r = 0; r < prow; r++)
                            for (int c = 0; c < pcol; c++)
                            {
                                float u = c / (float)(pcol - 1), v = r / (float)(prow - 1);
                                float tail = u > 0.7f ? (r == 1 || r == 2 ? -0.05f : 0.03f) * (u - 0.7f) / 0.3f : 0f;
                                // Hanging in still air: it droops away from the shaft in a slow curve.
                                float droop = 0.2f * u * u + 0.06f * u;
                                grid[r, c] = new Vector3(0.012f * s * Mathf.Sin(u * 5f), (0.02f + u * 0.3f + tail) * s, (1.02f - v * (0.22f - 0.08f * u) - droop) * s);
                            }
                        d.Add(Joints.Weapon, d.pal.accent, CharacterKit.Sheet(grid, Vector3.right, 0.008f * s));
                        // The knot sign, painted on both faces of the cloth.
                        foreach (float cx in new[] { 0.015f, 0.004f })
                        {
                            var c0 = new Vector3(cx * s, 0.13f * s, 0.88f * s);
                            var ring = new Vector3[13];
                            var rr = new float[13];
                            for (int i = 0; i < ring.Length; i++) { float a = i / 12f * Mathf.PI * 2f; ring[i] = c0 + new Vector3(0f, Mathf.Cos(a), Mathf.Sin(a)) * 0.036f * s; rr[i] = 0.003f * s; }
                            d.Add(Joints.Weapon, d.pal.emblem, MeshData.Tube(ring, rr, 4), false);
                            var tri = new[] { c0 + new Vector3(0f, 0f, 0.05f) * s, c0 + new Vector3(0f, 0.045f, -0.028f) * s, c0 + new Vector3(0f, -0.045f, -0.028f) * s, c0 + new Vector3(0f, 0f, 0.05f) * s };
                            d.Add(Joints.Weapon, d.pal.emblem, MeshData.Tube(tri, new[] { 0.003f * s, 0.003f * s, 0.003f * s, 0.003f * s }, 4), false);
                        }
                    }
                    break;
                case WeaponId.Staff:
                    {
                        // A gnarled branch, forking at the top round a stone slab with a glowing rune, hung with charms.
                        var rng = new System.Random(7);
                        var path = new Vector3[17];
                        var radii = new float[17];
                        for (int i = 0; i < path.Length; i++)
                        {
                            float t = i / (float)(path.Length - 1);
                            // A crooked branch: wandering, with knots where it thickens.
                            path[i] = new Vector3(Mathf.Sin(t * 13f) * 0.018f * s + ((float)rng.NextDouble() - 0.5f) * 0.02f * s,
                                Mathf.Cos(t * 9f) * 0.014f * s + ((float)rng.NextDouble() - 0.5f) * 0.02f * s, Mathf.Lerp(-1.22f, 0.24f, t) * s);
                            radii[i] = (Mathf.Lerp(0.02f, 0.025f, t) + (i % 4 == 0 ? 0.008f : 0f)) * s;
                        }
                        d.Add(Joints.Weapon, d.pal.leather, MeshData.Tube(path, radii, 7));
                        var fork = path[path.Length - 1];
                        // The branches wrap round the stone and carry on well above it, splitting like antler tines.
                        foreach (float side in new[] { -1f, 1f })
                        {
                            var branch = new[] { fork, fork + new Vector3(0f, side * 0.1f, 0.08f) * s, fork + new Vector3(0f, side * 0.11f, 0.24f) * s,
                                fork + new Vector3(0f, side * 0.06f, 0.4f) * s, fork + new Vector3(0f, side * 0.1f, 0.52f) * s };
                            d.Add(Joints.Weapon, d.pal.leather, MeshData.Tube(branch, new[] { 0.022f * s, 0.017f * s, 0.014f * s, 0.01f * s, 0.004f * s }, 6));
                            foreach (var tw in new[] { new[] { 2f, 0.07f, 0.1f }, new[] { 3f, -0.05f, 0.12f }, new[] { 3f, 0.08f, 0.06f } })
                            {
                                var root = branch[(int)tw[0]];
                                var twig = new[] { root, root + new Vector3(0.01f, side * tw[1], tw[2]) * s };
                                d.Add(Joints.Weapon, d.pal.leather, MeshData.Tube(twig, new[] { 0.008f * s, 0.003f * s }, 5));
                            }
                        }
                        // The rune stone, lashed between the forks: grey slab, glowing blue rune on its face (+X).
                        var stone = fork + new Vector3(0f, 0f, 0.16f * s);
                        d.Add(Joints.Weapon, new Color(0.5f, 0.52f, 0.55f), MeshData.Ellipsoid(stone, new Vector3(0.03f, 0.1f, 0.11f) * s, 12, 8));
                        // A wooden ring bound round the stone's edge.
                        var rim = new Vector3[17];
                        var rimR = new float[17];
                        for (int i = 0; i < rim.Length; i++) { float a = i / 16f * Mathf.PI * 2f; rim[i] = stone + new Vector3(0f, Mathf.Cos(a) * 0.105f, Mathf.Sin(a) * 0.115f) * s; rimR[i] = 0.011f * s; }
                        d.Add(Joints.Weapon, d.pal.leatherDark, MeshData.Tube(rim, rimR, 5));
                        // The rune (like Raidho): a stem, a bowl and a leg, glowing on the face.
                        var glyph = new[] {
                            new[] { new Vector2(-0.035f, -0.08f), new Vector2(-0.035f, 0.08f) },
                            new[] { new Vector2(-0.035f, 0.08f), new Vector2(0.03f, 0.05f) },
                            new[] { new Vector2(0.03f, 0.05f), new Vector2(-0.035f, 0.0f) },
                            new[] { new Vector2(-0.035f, 0.0f), new Vector2(0.035f, -0.08f) } };
                        foreach (var seg in glyph)
                            d.Add(Joints.Weapon, d.pal.emblem, MeshData.Tube(new[] { stone + new Vector3(0.031f, seg[0].x, seg[0].y) * s, stone + new Vector3(0.031f, seg[1].x, seg[1].y) * s }, new[] { 0.007f * s, 0.007f * s }, 4), false);
                        foreach (float z in new[] { 0.08f, 0.25f })
                            d.Add(Joints.Weapon, d.pal.leatherDark, MeshData.Lathe(new[] { new Vector2(0.03f * s, 0f), new Vector2(0.03f * s, 0.012f * s) }, 8).Transformed(fork + new Vector3(0f, 0f, z * s), Quaternion.Euler(90f, 0f, 0f), Vector3.one), false);
                        // Charms hanging from the forks.
                        foreach (float side in new[] { -1f, 1f })
                        {
                            foreach (var h in new[] { new Vector2(0.4f, 0.16f), new Vector2(0.28f, 0.2f) })
                            {
                                var hang = fork + new Vector3(0f, side * 0.1f, h.x) * s;
                                var end = hang + new Vector3(0.0f, side * 0.015f, -h.y) * s;
                                d.Add(Joints.Weapon, d.pal.leatherDark, MeshData.Tube(new[] { hang, end }, new[] { 0.0025f * s, 0.0025f * s }, 4), false);
                                Garments.RuneDisc(d, Joints.Weapon, end + new Vector3(0f, 0f, -0.03f * s), 0.032f * s, Quaternion.Euler(0f, 0f, -90f));
                            }
                        }
                    }
                    break;
                case WeaponId.Bow:
                    {
                        // A recurve longbow held at the grip, limbs along Z, bending away from the string (+Y) with
                        // the tips curling back; leather grip wrap, horn nocks and a taut string.
                        var limb = new Vector3[13];
                        var radii = new float[13];
                        for (int i = 0; i < limb.Length; i++)
                        {
                            float t = i / (float)(limb.Length - 1) * 2f - 1f;
                            float bend = 0.1f * (1f - t * t) - 0.05f * Mathf.Pow(Mathf.Abs(t), 6f);
                            limb[i] = new Vector3(0f, bend * s, t * 0.62f * s);
                            radii[i] = Mathf.Lerp(0.02f, 0.008f, Mathf.Abs(t)) * s;
                        }
                        d.Add(Joints.Weapon, d.pal.leather, MeshData.Tube(limb, radii, 6));
                        d.Add(Joints.Weapon, d.pal.leatherDark, MeshData.Tube(new[] { limb[5], limb[7] }, new[] { 0.024f * s, 0.024f * s }, 6));
                        d.Add(Joints.Weapon, d.pal.parchment, MeshData.Tube(new[] { limb[0], limb[0] + new Vector3(0f, -0.01f, -0.02f) * s }, new[] { 0.01f * s, 0.004f * s }, 5), false);
                        d.Add(Joints.Weapon, d.pal.parchment, MeshData.Tube(new[] { limb[12], limb[12] + new Vector3(0f, -0.01f, 0.02f) * s }, new[] { 0.01f * s, 0.004f * s }, 5), false);
                        d.Add(Joints.Weapon, d.pal.parchment, MeshData.Tube(new[] { limb[0], limb[12] }, new[] { 0.0025f * s, 0.0025f * s }, 4), false);
                    }
                    break;
                case WeaponId.Sword:
                    // A long broad sword pointing forward from the fist: leather grip, gold curved guard and pommel,
                    // a fuller and runes down the blade.
                    d.Add(Joints.Weapon, d.pal.leatherDark, MeshData.Tube(new[] { new Vector3(0f, 0f, -0.07f * s), new Vector3(0f, 0f, 0.07f * s) }, new[] { 0.018f * s, 0.018f * s }, 8));
                    d.Add(Joints.Weapon, d.pal.brass, MeshData.Ellipsoid(new Vector3(0f, 0f, -0.095f * s), new Vector3(0.03f, 0.03f, 0.025f) * s, 8, 5));
                    d.Add(Joints.Weapon, d.pal.brass, MeshData.Tube(new[] { new Vector3(-0.11f * s, 0f, 0.1f * s), new Vector3(0f, 0f, 0.08f * s), new Vector3(0.11f * s, 0f, 0.1f * s) }, new[] { 0.012f * s, 0.016f * s, 0.012f * s }, 6));
                    // The blade tapers gently to a point.
                    var steel = new Color(0.78f, 0.8f, 0.82f);
                    d.Add(Joints.Weapon, steel, MeshData.Extrude(new[] {
                        new Vector2(0.09f, 0.045f), new Vector2(0.5f, 0.042f), new Vector2(0.8f, 0.032f), new Vector2(0.93f, 0f),
                        new Vector2(0.8f, -0.032f), new Vector2(0.5f, -0.042f), new Vector2(0.09f, -0.045f) }, 0.014f)
                        .Transformed(Vector3.zero, Quaternion.Euler(0f, 0f, 90f), Vector3.one * s));
                    // A darker fuller down the middle, with runes cut into it (on both faces).
                    d.Add(Joints.Weapon, VikingModel.Shade(steel, 0.78f), MeshData.Box(new Vector3(0f, 0f, 0.43f * s), new Vector3(0.018f, 0.0155f, 0.62f) * s), false);
                    var glyphs = new[] {
                        new[] { 0f, -1f, 0f, 1f, 0f, 0.2f, 0.7f, 0.8f },            // ᚠ-ish
                        new[] { 0f, -1f, 0f, 1f, -0.7f, 0.3f, 0.7f, -0.3f },        // a slash through a stave
                        new[] { -0.6f, -1f, 0f, 1f, 0f, 1f, 0.6f, -1f },            // ᛏ-ish chevron
                        new[] { 0f, -1f, 0f, 1f, 0f, 0.6f, 0.7f, 0f },
                        new[] { -0.6f, 1f, 0.6f, -1f, -0.6f, -1f, 0.6f, 1f } };    // ᚷ cross
                    for (int i = 0; i < glyphs.Length; i++)
                    {
                        float z0 = (0.2f + i * 0.1f) * s, gs = 0.018f * s;
                        var gl = glyphs[i];
                        for (int k = 0; k < gl.Length; k += 4)
                        {
                            var a = new Vector3(gl[k] * gs * 0.5f, 0f, z0 + gl[k + 1] * gs);
                            var b = new Vector3(gl[k + 2] * gs * 0.5f, 0f, z0 + gl[k + 3] * gs);
                            var mid = (a + b) * 0.5f;
                            var dir = b - a;
                            d.Add(Joints.Weapon, d.pal.ink, MeshData.Box(Vector3.zero, new Vector3(0.0035f * s, 0.017f * s, dir.magnitude + 0.003f * s))
                                .Transformed(mid, Quaternion.LookRotation(dir.normalized, Vector3.up), Vector3.one), false);
                        }
                    }
                    break;
            }
        }

        public static void BuildOffHand(Dresser d, OffHandId id)
        {
            float s = d.S;
            switch (id)
            {
                case OffHandId.RoundShield:
                    // Held by the grip behind the boss; the face with the boss is on -X.
                    var face = Quaternion.Euler(0f, -90f, 0f);
                    var centre = new Vector3(-0.05f * s, 0f, 0f);
                    {
                        // Dark red-stained planks, as in the concept art, with thin dark seams between them.
                        float rs = 0.29f * s;
                        for (int q = 0; q < 6; q++)
                        {
                            float z0 = -rs + q * rs / 3f, z1 = z0 + rs / 3f - 0.004f * s;
                            var plank = new System.Collections.Generic.List<Vector2>();
                            for (int k = 0; k <= 6; k++) { float z = Mathf.Lerp(z0, z1, k / 6f); plank.Add(new Vector2(z, Mathf.Sqrt(Mathf.Max(0f, rs * rs - z * z)))); }
                            for (int k = 6; k >= 0; k--) { float z = Mathf.Lerp(z0, z1, k / 6f); plank.Add(new Vector2(z, -Mathf.Sqrt(Mathf.Max(0f, rs * rs - z * z)))); }
                            var wood = Color.Lerp(d.pal.accent, d.pal.leather, q % 2 == 0 ? 0.35f : 0.55f);
                            d.Add(Joints.OffHand, wood, MeshData.Extrude(plank.ToArray(), 0.03f * s).Transformed(centre, Quaternion.identity, Vector3.one), true, SurfaceKind.Wood);
                        }
                        // A dark backing disc on the inner side (the boss is on -X, the outer face) that shows through the seams.
                        d.Add(Joints.OffHand, VikingModel.Shade(d.pal.leatherDark, 0.6f), MeshData.Wedge(rs * 0.99f, 0.024f * s, 0f, 360f, 24).Transformed(centre + new Vector3(0.006f * s, 0f, 0f), face, Vector3.one), false);
                    }
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
                case OffHandId.KnotShield:
                    {
                        // A big shield of red planks with an iron rim and boss and a pale triple-knot painted on it,
                        // held in front of the body, face forward (+Z).
                        float rad = 0.37f * s;
                        var at = new Vector3(0f, 0.02f * s, 0.06f * s);
                        for (int q = 0; q < 6; q++)
                        {
                            // Vertical planks: slices of the disc, alternating shades.
                            float x0 = -rad + q * rad / 3f, x1 = x0 + rad / 3f;
                            var plank = new System.Collections.Generic.List<Vector2>();
                            for (int k = 0; k <= 6; k++) { float x = Mathf.Lerp(x0, x1, k / 6f); plank.Add(new Vector2(x, Mathf.Sqrt(Mathf.Max(0f, rad * rad - x * x)))); }
                            for (int k = 6; k >= 0; k--) { float x = Mathf.Lerp(x0, x1, k / 6f); plank.Add(new Vector2(x, -Mathf.Sqrt(Mathf.Max(0f, rad * rad - x * x)))); }
                            d.Add(Joints.OffHand, q % 2 == 0 ? d.pal.accent : VikingModel.Shade(d.pal.accent, 0.85f),
                                MeshData.Extrude(plank.ToArray(), 0.028f * s).Transformed(at, Quaternion.Euler(0f, 90f, 0f), Vector3.one));
                        }
                        var kRim = new Vector3[33];
                        var kRr = new float[33];
                        for (int i = 0; i < kRim.Length; i++) { float a = i / 32f * Mathf.PI * 2f; kRim[i] = at + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * rad; kRr[i] = 0.02f * s; }
                        // Old iron, browned with rust.
                        d.Add(Joints.OffHand, Color.Lerp(VikingModel.Shade(d.pal.metal, 0.8f), d.pal.leatherDark, 0.45f), MeshData.Tube(kRim, kRr, 6), true, SurfaceKind.Metal);
                        // Rivets holding the rim on.
                        for (int i = 0; i < 14; i++)
                        {
                            float a = (i + 0.5f) / 14f * Mathf.PI * 2f;
                            d.Add(Joints.OffHand, d.pal.metal, MeshData.Ellipsoid(at + new Vector3(Mathf.Cos(a) * rad * 0.9f, Mathf.Sin(a) * rad * 0.9f, 0.016f * s), Vector3.one * 0.009f * s, 5, 3), false);
                        }
                        d.Add(Joints.OffHand, d.pal.metal, MeshData.Dome(Vector3.zero, new Vector3(0.08f, 0.065f, 0.08f) * s, 14, 5).Transformed(at + new Vector3(0f, 0f, 0.012f * s), Quaternion.Euler(90f, 0f, 0f), Vector3.one));
                        // The triquetra: three pointed lobes (vesicas) meeting at the boss, woven with a circle.
                        for (int k = 0; k < 3; k++)
                        {
                            float a0 = k * Mathf.PI * 2f / 3f + Mathf.PI / 2f;
                            var dir = new Vector3(Mathf.Cos(a0), Mathf.Sin(a0), 0f);
                            var perp = new Vector3(-dir.y, dir.x, 0f);
                            var lobe = new Vector3[25];
                            var lr = new float[25];
                            for (int i = 0; i < lobe.Length; i++)
                            {
                                // Out along one arc and back along the other: a pointed lens from the centre to the tip.
                                float t = i / (float)(lobe.Length - 1) * 2f;
                                float u = t <= 1f ? t : 2f - t, side = t <= 1f ? 1f : -1f;
                                lobe[i] = at + new Vector3(0f, 0f, 0.017f * s) + dir * (0.08f + u * 0.78f) * rad + perp * side * Mathf.Sin(u * Mathf.PI) * 0.3f * rad;
                                lr[i] = 0.015f * s;
                            }
                            // Broad painted bands with a dark edge, like the concept art's knot.
                            d.Add(Joints.OffHand, d.pal.emblem, MeshData.Tube(lobe, lr, 6), false);
                            d.Add(Joints.OffHand, VikingModel.Shade(d.pal.accent, 0.45f), KnotEdge(lobe, 0.021f * s, 0.006f * s), false);
                        }
                        var circle = new Vector3[25];
                        var cr = new float[25];
                        for (int i = 0; i < circle.Length; i++) { float a = i / 24f * Mathf.PI * 2f; circle[i] = at + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * rad * 0.52f + new Vector3(0f, 0f, 0.018f * s); cr[i] = 0.013f * s; }
                        d.Add(Joints.OffHand, d.pal.emblem, MeshData.Tube(circle, cr, 6), false);
                        d.Add(Joints.OffHand, VikingModel.Shade(d.pal.accent, 0.45f), KnotEdge(circle, 0.019f * s, 0.006f * s), false);
                    }
                    break;
                case OffHandId.Map:
                    // A sheet of parchment with torn edges, gently curled, held up facing forward (+Z) above the fist,
                    // with a compass rose and a coastline drawn in ink.
                    const int rows = 9, cols = 8;
                    var grid = new Vector3[rows, cols];
                    var rng = new System.Random(3);
                    for (int r = 0; r < rows; r++)
                        for (int c = 0; c < cols; c++)
                        {
                            float u = c / (float)(cols - 1) - 0.5f, v = r / (float)(rows - 1);
                            // Torn edges: each border point pulled in or out by a different amount.
                            bool edgeX = c == 0 || c == cols - 1, edgeY = r == 0 || r == rows - 1;
                            float tx = edgeX ? ((float)rng.NextDouble() - 0.5f) * 0.035f * Mathf.Sign(u) : 0f;
                            float ty = edgeY ? ((float)rng.NextDouble() - 0.5f) * 0.035f * (r == 0 ? 1f : -1f) : 0f;
                            // The corners are curled a little towards the reader.
                            float curl = (edgeX && edgeY) ? 0.02f : 0f;
                            grid[r, c] = new Vector3((u * 0.27f + 0.07f + tx) * s, (0.14f - v * 0.3f + ty) * s, (0.03f + 0.02f * Mathf.Cos(u * 3f) + curl) * s);
                        }
                    d.Add(Joints.OffHand, d.pal.parchment, CharacterKit.Sheet(grid, Vector3.forward, 0.006f * s));
                    var rose = new Vector3(0.09f * s, -0.01f * s, 0.052f * s);
                    var ring = new Vector3[17];
                    var rr = new float[17];
                    for (int i = 0; i < ring.Length; i++) { float a = i / 16f * Mathf.PI * 2f; ring[i] = rose + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * 0.045f * s; rr[i] = 0.002f * s; }
                    d.Add(Joints.OffHand, d.pal.ink, MeshData.Tube(ring, rr, 4), false);
                    for (int k = 0; k < 8; k++)
                    {
                        float a = k * Mathf.PI / 4f, len = (k % 2 == 0 ? 0.075f : 0.04f) * s;
                        d.Add(Joints.OffHand, d.pal.ink, MeshData.Tube(new[] { rose, rose + new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f) * len }, new[] { 0.0035f * s, 0.001f * s }, 4), false);
                    }
                    // A coastline down the left side, an island, and a dotted sailing route to the rose.
                    var coast = new[] { new Vector3(-0.03f, 0.1f, 0f), new Vector3(0.0f, 0.05f, 0f), new Vector3(-0.02f, 0.0f, 0f), new Vector3(0.015f, -0.06f, 0f), new Vector3(-0.01f, -0.13f, 0f) };
                    for (int i = 0; i < coast.Length; i++) { coast[i] *= s; coast[i].z = (0.03f + 0.02f * Mathf.Cos(((coast[i].x / s - 0.07f) / 0.3f) * 3f) + 0.006f) * s + 0.001f; }
                    d.Add(Joints.OffHand, d.pal.ink, CharacterKit.ZigZag(coast, Vector3.right, 0.005f * s, 0.0025f * s), false);
                    var isle = new Vector3(0.17f * s, 0.08f * s, 0.05f * s);
                    var shore = new Vector3[9];
                    var sr = new float[9];
                    for (int i = 0; i < shore.Length; i++) { float a = i / 8f * Mathf.PI * 2f; shore[i] = isle + new Vector3(Mathf.Cos(a) * 0.028f, Mathf.Sin(a) * 0.018f * (1f + 0.3f * Mathf.Sin(a * 3f)), 0f) * s; sr[i] = 0.0022f * s; }
                    d.Add(Joints.OffHand, d.pal.ink, MeshData.Tube(shore, sr, 4), false);
                    for (int i = 0; i < 6; i++)
                    {
                        var p = Vector3.Lerp(new Vector3(0.02f, 0.09f, 0.054f), new Vector3(0.14f, 0.06f, 0.054f), i / 5f) * s + new Vector3(0f, Mathf.Sin(i * 1.3f) * 0.008f * s, 0f);
                        d.Add(Joints.OffHand, d.pal.ink, MeshData.Ellipsoid(p, Vector3.one * 0.0035f * s, 4, 3), false);
                    }
                    break;
            }
        }
    }
}
