using UnityEngine;

namespace OdinsCoin
{
    /// <summary>What a garment is built with: the model to add to, the body's measurements and the colours.</summary>
    public class Dresser
    {
        public VikingModel model;
        public Fit fit;
        public Palette pal;
        public int seed;

        public void Add(string joint, Color color, MeshData mesh, bool outline = true) { model.Add(joint, color, mesh, outline); }
        public float S { get { return fit.s; } }

        // The skirt, once made, so layers on top (tabards, fur, pouches) can sit just outside it.
        public float skirtTop, skirtTopR, skirtBottom, skirtBottomR, skirtDepth;
        public bool hasSkirt;

        /// <summary>The skirt's radius (x; times <see cref="skirtDepth"/> for z) at a height, matching RaggedSkirt's shape.</summary>
        public float SkirtRadius(float y)
        {
            if (!hasSkirt) return fit.waistR;
            float k = Mathf.Clamp01((y - skirtBottom) / (skirtTop - skirtBottom));
            return Mathf.Lerp(skirtBottomR, skirtTopR, k * k) * (1f + 0.04f * Mathf.Sin(k * Mathf.PI));
        }
    }

    /// <summary>
    /// Clothes and gear that go on over any body. Each takes the body's <see cref="Fit"/> so it fits every height,
    /// width and gender, and only uses palette slots, so every colour can be changed.
    /// </summary>
    public static class Garments
    {
        // ---------------------------------------------------------------- feet and hands

        /// <summary>Tall boots with pointed toes, criss-cross straps and fur cuffs.</summary>
        public static void FurBoots(Dresser d) { FurBoots(d, 0f); }

        /// <summary>Fur-cuffed boots, <paramref name="extra"/> metres taller than the body's usual boot top.</summary>
        public static void FurBoots(Dresser d, float extra)
        {
            var f = d.fit;
            foreach (var joint in new[] { Joints.LeftLeg, Joints.RightLeg })
            {
                float sole = -f.hip, top = f.bootTop + extra * d.S - f.hip, s = d.S;
                float r = 0.065f * s * Mathf.Sqrt(f.width);
                d.Add(joint, d.pal.leatherDark, MeshData.Lathe(new[] {
                    new Vector2(r * 0.85f, sole), new Vector2(r * 1.02f, sole + 0.04f * s), new Vector2(r * 0.92f, sole + 0.13f * s),
                    new Vector2(r * 0.98f, sole + 0.25f * s), new Vector2(r * 1.08f, top) }, 12));
                d.Add(joint, d.pal.leatherDark, MeshData.Tube(new[] {
                    new Vector3(0f, sole + 0.045f * s, 0f), new Vector3(0f, sole + 0.035f * s, 0.11f * s), new Vector3(0f, sole + 0.05f * s, 0.19f * s) },
                    new[] { r * 0.88f, r * 0.62f, 0.004f }, 10));
                // Straps wound criss-cross, lighter leather.
                d.Add(joint, d.pal.leather, CharacterKit.Spiral(sole + 0.07f * s, top - 0.05f * s, r * 1.02f, 1.6f, 0f, 0.0075f * s), false);
                d.Add(joint, d.pal.leather, CharacterKit.Spiral(sole + 0.07f * s, top - 0.05f * s, r * 1.02f, -1.6f, Mathf.PI, 0.0075f * s), false);
                d.Add(joint, d.pal.fur, CharacterKit.FurRing(new Vector3(0f, top, 0f), r * 1.12f, 1f, 0.04f * s, 12, 0.055f * s, d.seed + joint.Length));
            }
        }

        /// <summary>Chunky leather gloves with flared cuffs.</summary>
        public static void Gloves(Dresser d)
        {
            var f = d.fit;
            float s = d.S;
            foreach (var joint in new[] { Joints.LeftForearm, Joints.RightForearm })
            {
                float wrist = -f.foreArm;
                d.Add(joint, d.pal.leatherDark, MeshData.Lathe(new[] { new Vector2(0.02f * s, wrist + 0.03f * s), new Vector2(0.038f * s, wrist + 0.09f * s), new Vector2(0.041f * s, wrist + 0.1f * s) }, 10));
                d.Add(joint, d.pal.leatherDark, MeshData.Ellipsoid(new Vector3(0f, wrist - 0.022f * s, 0.006f * s), new Vector3(0.036f, 0.046f, 0.04f) * s, 10, 6));
                d.Add(joint, d.pal.leatherDark, MeshData.Ellipsoid(new Vector3(0f, wrist - 0.008f * s, 0.034f * s), new Vector3(0.016f, 0.027f, 0.016f) * s, 6, 4));
            }
        }

        // ---------------------------------------------------------------- body

        /// <summary>A tunic from the waist to the shoulders, with short sleeves ending in a ragged fur edge.</summary>
        public static void Tunic(Dresser d, bool furSleeves)
        {
            var f = d.fit;
            float s = d.S;
            var profile = new[] {
                new Vector2(f.waistR * 1.02f, f.waist - 0.03f * s), new Vector2(f.TorsoRadius(f.waist + 0.08f * s), f.waist + 0.08f * s),
                new Vector2(f.chestR * 1.02f, f.chest), new Vector2(f.chestR * 0.98f, f.shoulderY - 0.02f * s),
                new Vector2(f.chestR * 0.6f, f.neckY - 0.01f * s), new Vector2(0.035f * s, f.neckY + 0.02f * s) };
            d.Add(Joints.Body, d.pal.cloth, MeshData.Lathe(profile, 16).Transformed(Vector3.zero, Quaternion.identity, new Vector3(1f, 1f, f.depth)));
            foreach (var joint in new[] { Joints.LeftArm, Joints.RightArm })
            {
                float r = 0.052f * s * Mathf.Sqrt(f.width);
                d.Add(joint, d.pal.cloth, MeshData.Lathe(new[] { new Vector2(r * 0.95f, -0.13f * s), new Vector2(r, -0.04f * s), new Vector2(r * 0.8f, 0.03f * s) }, 10));
                if (furSleeves) d.Add(joint, d.pal.fur, CharacterKit.FurRing(new Vector3(0f, -0.13f * s, 0f), r * 1.05f, 1f, 0.028f * s, 9, 0.045f * s, d.seed + 5 + joint.Length));
            }
        }

        /// <summary>A long skirt from the belt to just above the boots, ragged at the hem, with a darker layer under it.</summary>
        public static void LongSkirt(Dresser d, float hemAboveBoots, float flare)
        {
            var f = d.fit;
            float s = d.S;
            float hem = f.bootTop + hemAboveBoots * s;
            d.hasSkirt = true;
            d.skirtTop = f.waist; d.skirtTopR = f.waistR * 0.98f; d.skirtBottom = hem + 0.02f * s; d.skirtBottomR = f.hipR * flare; d.skirtDepth = f.depth + 0.05f;
            d.Add(Joints.Body, d.pal.cloth, CharacterKit.RaggedSkirt(d.skirtTop, d.skirtTopR, d.skirtBottom, d.skirtBottomR, d.skirtDepth, 22, 0.06f * s, d.seed + 1));
            d.Add(Joints.Body, d.pal.clothDark, CharacterKit.RaggedSkirt(hem + 0.2f * s, d.SkirtRadius(hem + 0.2f * s) * 0.97f, hem - 0.015f * s, d.skirtBottomR * 0.96f, d.skirtDepth - 0.01f, 18, 0.04f * s, d.seed + 2));
        }

        /// <summary>Pelt tatters hanging around the skirt (the raider's knee fur), with the dark skirt showing between them.</summary>
        public static void FurSkirt(Dresser d, float top, float bottom)
        {
            var f = d.fit;
            float s = d.S;
            float t = f.bootTop + top * s, b = f.bootTop + bottom * s;
            // Separate pelt pieces in two tones, plus a few torn cloth flaps lower down.
            System.Func<float, float> skirt = y => d.SkirtRadius(y);
            d.Add(Joints.Body, d.pal.fur, CharacterKit.Flaps(t, t - b, 7, 0.55f, d.skirtDepth, skirt, 0.012f, d.seed + 3, 0.012f * s));
            d.Add(Joints.Body, d.pal.furShadow, CharacterKit.Flaps(t - 0.05f * s, t - b, 5, 0.4f, d.skirtDepth, skirt, 0.02f, d.seed + 13, 0.012f * s));
            d.Add(Joints.Body, d.pal.clothDark, CharacterKit.Flaps(b + 0.08f * s, 0.14f * s, 6, 0.5f, d.skirtDepth, skirt, 0.006f, d.seed + 23, 0.01f * s));
        }

        /// <summary>A thick pelt over the shoulders: the wide shaggy silhouette of the concept art.</summary>
        public static void ShoulderPelt(Dresser d, float size)
        {
            var f = d.fit;
            float s = d.S;
            float y = f.shoulderY - 0.02f * s;
            float r = (f.shoulderX + 0.04f * s) * size;
            d.Add(Joints.Body, d.pal.fur, MeshData.Ellipsoid(new Vector3(0f, y + 0.01f * s, -0.005f), new Vector3(r, 0.07f * s, r * 0.72f), 16, 8));
            d.Add(Joints.Body, d.pal.fur, CharacterKit.FurRing(new Vector3(0f, y - 0.02f * s, -0.005f), r * 0.97f, 0.74f, 0.075f * s, 18, 0.12f * s * size, d.seed + 6, 0.95f));
            d.Add(Joints.Body, d.pal.furShadow, CharacterKit.FurRing(new Vector3(0f, y + 0.035f * s, -0.005f), r * 0.72f, 0.78f, 0.05f * s, 16, 0.07f * s, d.seed + 7, 0.5f));
            // The pelt rises behind the neck like a mane, framing the head.
            d.Add(Joints.Body, d.pal.fur, MeshData.Ellipsoid(new Vector3(0f, y + 0.06f * s * size, -f.chestR * f.depth * 0.7f), new Vector3(r * 0.75f, 0.075f * s * size, 0.06f * s), 14, 7));
            // Pelt pieces draping down over the chest and back.
            System.Func<float, float> torso = yy => f.TorsoRadius(yy) * 1.02f;
            d.Add(Joints.Body, d.pal.fur, CharacterKit.Flaps(y - 0.03f * s, 0.13f * s * size, 6, 0.6f, f.depth, torso, 0.015f, d.seed + 8, 0.012f * s));
        }

        /// <summary>A wide belt with a big brass buckle, a ring, pouches and a strap across the chest.</summary>
        public static void RaiderBelt(Dresser d, int pouches)
        {
            var f = d.fit;
            float s = d.S;
            float y = f.waist, r = f.waistR * 1.06f;
            d.Add(Joints.Body, d.pal.leatherDark, CharacterKit.Band(y, 0.055f * s, r, f.depth + 0.05f));
            float front = r * (f.depth + 0.05f);
            d.Add(Joints.Body, d.pal.brass, MeshData.Box(new Vector3(0f, y, front + 0.004f), new Vector3(0.075f, 0.065f, 0.014f) * s));
            d.Add(Joints.Body, d.pal.leatherDark, MeshData.Box(new Vector3(0f, y, front + 0.011f), new Vector3(0.042f, 0.034f, 0.008f) * s), false);
            // A second, thinner hip belt slung lower and askew.
            d.Add(Joints.Body, d.pal.leather, CharacterKit.Band(0f, 0.03f * s, d.SkirtRadius(y - 0.075f * s) * 1.03f, (d.hasSkirt ? d.skirtDepth : f.depth) + 0.02f)
                .Transformed(new Vector3(0f, y - 0.075f * s, 0f), Quaternion.Euler(0f, 0f, -7f), Vector3.one));
            float[] xs = { -0.11f, 0.12f, -0.035f, 0.07f };
            for (int i = 0; i < Mathf.Min(pouches, xs.Length); i++)
            {
                float x = xs[i] * s * f.width, py = y - (0.065f + (i & 1) * 0.02f) * s;
                float pr = d.SkirtRadius(py) * 1.02f + 0.01f;
                float z = Mathf.Sqrt(Mathf.Max(0f, pr * pr - x * x)) * (d.hasSkirt ? d.skirtDepth : f.depth + 0.06f) + 0.012f;
                var size = new Vector3(0.055f + 0.01f * (i % 2), 0.07f, 0.04f) * s;
                d.Add(Joints.Body, d.pal.leather, MeshData.Ellipsoid(new Vector3(x, py - 0.01f * s, z), size * 0.62f, 9, 6));
                d.Add(Joints.Body, d.pal.leather, MeshData.Box(new Vector3(x, py + 0.012f * s, z), new Vector3(size.x * 1.05f, size.y * 0.45f, size.z * 1.05f)));
                d.Add(Joints.Body, d.pal.leatherDark, MeshData.Box(new Vector3(x, py + 0.028f * s, z + size.z * 0.5f), new Vector3(size.x * 1.08f, 0.022f * s, 0.008f * s)), false);
                d.Add(Joints.Body, d.pal.brass, MeshData.Ellipsoid(new Vector3(x, py + 0.014f * s, z + size.z * 0.56f), Vector3.one * 0.007f * s, 5, 3), false);
            }
            // A strap from the right shoulder to the left hip, with a brass ring on the chest.
            float cz = f.chestR * f.depth;
            d.Add(Joints.Body, d.pal.leatherDark, MeshData.Tube(new[] {
                new Vector3(f.shoulderX * 0.7f, f.shoulderY, cz * 0.6f), new Vector3(0.02f * s, f.chest - 0.02f * s, cz + 0.012f), new Vector3(-f.waistR * 0.9f, y + 0.02f * s, f.waistR * f.depth + 0.01f) },
                new[] { 0.011f * s, 0.012f * s, 0.011f * s }, 5), false);
            d.Add(Joints.Body, d.pal.brass, MeshData.Lathe(new[] { new Vector2(0.018f * s, -0.005f * s), new Vector2(0.018f * s, 0.005f * s) }, 10)
                .Transformed(new Vector3(0.02f * s, f.chest - 0.02f * s, cz + 0.022f), Quaternion.Euler(90f, 0f, 0f), Vector3.one), false);
        }

        /// <summary>A cloth banner hanging from the belt down the front, with a pointed hem and a triangle symbol.</summary>
        public static void Tabard(Dresser d, float length, float width)
        {
            var f = d.fit;
            float s = d.S;
            float top = f.waist - 0.02f * s;
            const int rows = 9, cols = 3;
            var grid = new Vector3[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    float v = r / (float)(rows - 1), u = c / (float)(cols - 1) * 2f - 1f;
                    float drop = v * length * s + (r == rows - 1 && c == 1 ? 0.045f * s : 0f);
                    float y = top - drop;
                    // Lies just outside the skirt (which flares), so it's never swallowed by it.
                    float z = d.SkirtRadius(y) * (d.hasSkirt ? d.skirtDepth : f.depth + 0.05f) + 0.014f + 0.02f * v;
                    grid[r, c] = new Vector3(u * width * 0.5f * s, y, z);
                }
            d.Add(Joints.Body, d.pal.accent, CharacterKit.Sheet(grid, Vector3.forward, 0.012f * s));
            // Embroidered border lines down both edges, and a triangle rune, riding on the cloth's surface.
            for (int r = 0; r < rows - 2; r++)
                foreach (float x in new[] { -1f, 1f })
                {
                    Vector3 a = grid[r, 1] + new Vector3(x * width * 0.36f * s, 0f, 0.008f * s), b = grid[r + 1, 1] + new Vector3(x * width * 0.36f * s, 0f, 0.008f * s);
                    d.Add(Joints.Body, d.pal.emblem, MeshData.Tube(new[] { a, b }, new[] { 0.0035f * s, 0.0035f * s }, 4), false);
                }
            float cy = top - length * 0.4f * s, tri = 0.03f * s;
            float ez = grid[Mathf.RoundToInt((rows - 1) * 0.4f), 1].z + 0.009f * s;
            Vector3 c0 = new Vector3(0f, cy + tri, ez), c1 = new Vector3(-tri * 0.9f, cy - tri * 0.6f, ez), c2 = new Vector3(tri * 0.9f, cy - tri * 0.6f, ez);
            foreach (var edge in new[] { new[] { c0, c1 }, new[] { c1, c2 }, new[] { c2, c0 } })
                d.Add(Joints.Body, d.pal.emblem, MeshData.Tube(edge, new[] { 0.004f * s, 0.004f * s }, 4), false);
        }

        // ---------------------------------------------------------------- headgear

        /// <summary>A deep round steel helmet sitting low (the brim right above the eyes), with a riveted ridge and a nose guard.</summary>
        public static void NasalHelmet(Dresser d)
        {
            var f = d.fit;
            float r = f.headR, cy = HeroModel.HeadCentre(f);
            float brim = cy + 0.12f * r;
            var steel = d.pal.metal;
            // A deep bowl: wider than the face and coming down at the back and sides.
            d.Add(Joints.Head, steel, MeshData.Lathe(new[] {
                new Vector2(r * 1.2f, brim - 0.12f * r), new Vector2(r * 1.24f, brim + 0.15f * r), new Vector2(r * 1.2f, brim + 0.5f * r),
                new Vector2(r * 0.98f, brim + 0.88f * r), new Vector2(r * 0.55f, brim + 1.12f * r), new Vector2(0.01f, brim + 1.2f * r) }, 20)
                .Transformed(new Vector3(0f, 0f, -0.02f * r), Quaternion.identity, Vector3.one));
            d.Add(Joints.Head, VikingModel.Shade(steel, 0.72f), CharacterKit.Band(brim - 0.08f * r, 0.035f * d.S, r * 1.26f, 1f, 20)
                .Transformed(new Vector3(0f, 0f, -0.02f * r), Quaternion.identity, Vector3.one));
            // A broad riveted ridge from brow to nape, a paler strip down the front (the concept's shine).
            var ridge = new Vector3[7];
            for (int i = 0; i < ridge.Length; i++)
            {
                float a = Mathf.Lerp(-0.1f, Mathf.PI + 0.1f, i / (float)(ridge.Length - 1));
                ridge[i] = new Vector3(0f, brim + Mathf.Sin(a) * r * 1.18f, Mathf.Cos(a) * r * 1.22f - 0.02f * r);
            }
            var rr = new float[ridge.Length];
            for (int i = 0; i < rr.Length; i++) rr[i] = 0.02f * d.S;
            d.Add(Joints.Head, VikingModel.Shade(steel, 1.12f), MeshData.Tube(ridge, rr, 6).Transformed(Vector3.zero, Quaternion.identity, new Vector3(1.6f, 1f, 1f)));
            // Nose guard coming down between the eyes.
            d.Add(Joints.Head, VikingModel.Shade(steel, 0.9f), MeshData.Box(new Vector3(0f, brim - 0.2f * r, r * 1.02f), new Vector3(0.024f, 0.4f * r, 0.018f)));
            for (int i = 1; i < ridge.Length - 1; i++)
                d.Add(Joints.Head, VikingModel.Shade(steel, 1.3f), MeshData.Ellipsoid(ridge[i] + (ridge[i] - new Vector3(0f, brim, 0f)).normalized * 0.02f * d.S, Vector3.one * 0.007f * d.S, 5, 3), false);
            for (int i = -5; i <= 5; i++)
            {
                float a = i * 0.28f;
                d.Add(Joints.Head, VikingModel.Shade(steel, 1.3f), MeshData.Ellipsoid(new Vector3(Mathf.Sin(a) * r * 1.28f, brim - 0.08f * r, Mathf.Cos(a) * r * 1.26f - 0.02f * r), Vector3.one * 0.0065f * d.S, 5, 3), false);
            }
        }

        // ---------------------------------------------------------------- the jarl

        /// <summary>A long coat skirt to the knees with a straight hem, a gold border and a bordered front panel.</summary>
        public static void JarlCoat(Dresser d, float hemAboveBoots)
        {
            var f = d.fit;
            float s = d.S;
            float hem = f.bootTop + hemAboveBoots * s;
            d.hasSkirt = true;
            d.skirtTop = f.waist; d.skirtTopR = f.waistR * 0.98f; d.skirtBottom = hem; d.skirtBottomR = f.hipR * 1.35f; d.skirtDepth = f.depth + 0.05f;
            d.Add(Joints.Body, d.pal.cloth, CharacterKit.RaggedSkirt(d.skirtTop, d.skirtTopR, d.skirtBottom, d.skirtBottomR, d.skirtDepth, 22, 0.012f * s, d.seed + 31));
            // Gold border along the hem.
            d.Add(Joints.Body, d.pal.brass, CharacterKit.Band(hem + 0.022f * s, 0.022f * s, d.SkirtRadius(hem + 0.022f * s) + 0.004f, d.skirtDepth, 22), false);
            // Front panel: darker cloth edged in gold, over the coat.
            const int rows = 7;
            var grid = new Vector3[rows, 3];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < 3; c++)
                {
                    float v = r / (float)(rows - 1), u = c - 1f;
                    float y = Mathf.Lerp(f.waist - 0.03f * s, hem - 0.01f * s, v);
                    float z = d.SkirtRadius(y) * d.skirtDepth + 0.01f;
                    grid[r, c] = new Vector3(u * Mathf.Lerp(0.06f, 0.1f, v) * s * f.width, y, z);
                }
            d.Add(Joints.Body, d.pal.clothDark, CharacterKit.Sheet(grid, Vector3.forward, 0.01f * s));
            foreach (int c in new[] { 0, 2 })
            {
                var edge = new Vector3[rows];
                for (int r = 0; r < rows; r++) edge[r] = grid[r, c] + new Vector3(0f, 0f, 0.008f * s);
                var radii = new float[rows];
                for (int r = 0; r < rows; r++) radii[r] = 0.007f * s;
                d.Add(Joints.Body, d.pal.brass, MeshData.Tube(edge, radii, 5), false);
            }
            // Studs down the panel.
            for (int r = 1; r < rows - 1; r++)
                d.Add(Joints.Body, d.pal.brass, MeshData.Ellipsoid(grid[r, 1] + new Vector3(0f, 0f, 0.01f * s), Vector3.one * 0.008f * s, 5, 3), false);
        }

        /// <summary>Long sleeves down to the wrists, ending in fur cuffs, with gold-strapped bracers.</summary>
        public static void LongSleeves(Dresser d)
        {
            var f = d.fit;
            float s = d.S;
            float r = 0.05f * s * Mathf.Sqrt(f.width);
            foreach (var arm in new[] { Joints.LeftArm, Joints.RightArm })
                d.Add(arm, d.pal.cloth, MeshData.Lathe(new[] { new Vector2(r * 0.9f, -f.upperArm - 0.01f * s), new Vector2(r, -0.1f * s), new Vector2(r * 0.85f, 0.03f * s) }, 10));
            foreach (var fore in new[] { Joints.LeftForearm, Joints.RightForearm })
            {
                d.Add(fore, d.pal.cloth, MeshData.Lathe(new[] { new Vector2(r * 0.75f, -f.foreArm * 0.55f), new Vector2(r * 0.9f, 0.01f * s) }, 10));
                d.Add(fore, d.pal.fur, CharacterKit.FurRing(new Vector3(0f, -f.foreArm * 0.55f, 0f), r * 0.85f, 1f, 0.03f * s, 9, 0.045f * s, d.seed + fore.Length));
                // Bracer between the cuff and the glove, laced with gold.
                d.Add(fore, d.pal.leatherDark, MeshData.Lathe(new[] { new Vector2(r * 0.62f, -f.foreArm + 0.02f * s), new Vector2(r * 0.7f, -f.foreArm * 0.6f) }, 10));
                d.Add(fore, d.pal.brass, CharacterKit.Spiral(-f.foreArm + 0.03f * s, -f.foreArm * 0.62f, r * 0.72f, 1f, 0f, 0.004f * s), false);
                d.Add(fore, d.pal.brass, CharacterKit.Spiral(-f.foreArm + 0.03f * s, -f.foreArm * 0.62f, r * 0.72f, -1f, Mathf.PI, 0.004f * s), false);
            }
        }

        /// <summary>
        /// A big red cape from the shoulders to the ankles, ragged at the hem, wrapping forward over the shoulders.
        /// </summary>
        public static void BigCape(Dresser d, float length, float flare)
        {
            var f = d.fit;
            float s = d.S;
            var top = new Vector3(0f, f.shoulderY + 0.02f * s, -f.chestR * f.depth * 0.3f);
            d.Add(Joints.Body, d.pal.accent, CharacterKit.Cape(top, f.shoulderX * 2.6f, f.shoulderX * 2f * flare, length * s, f.chestR * 1.3f, 0.17f * s, d.seed + 41, 0.02f * s));
            // Front drapes falling over each shoulder, open at the chest.
            foreach (float x in new[] { -1f, 1f })
            {
                const int rows = 6, cols = 3;
                var grid = new Vector3[rows, cols];
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                    {
                        float v = r / (float)(rows - 1), u = c / (float)(cols - 1);
                        float drop = v * (0.55f + 0.08f * c) * s;
                        float y = f.shoulderY + 0.03f * s - drop;
                        float xx = x * (f.shoulderX * (0.85f + 0.5f * u) + v * 0.06f * s);
                        float z = Mathf.Lerp(f.chestR * f.depth * 0.9f, -f.chestR * 0.3f, u) + 0.02f + v * 0.02f;
                        grid[r, c] = new Vector3(xx, y, z);
                    }
                d.Add(Joints.Body, d.pal.accent, CharacterKit.Sheet(grid, new Vector3(x, 0f, 0.6f), 0.018f * s));
            }
        }

        /// <summary>Two big round gold brooches on the chest, joined by straps crossing down to the belt.</summary>
        public static void Brooches(Dresser d)
        {
            var f = d.fit;
            float s = d.S;
            float y = f.shoulderY - 0.16f * s, z = f.chestR * f.depth + 0.03f * s;
            foreach (float x in new[] { -1f, 1f })
            {
                var at = new Vector3(x * f.chestR * 0.72f, y, z);
                d.Add(Joints.Body, d.pal.brass, MeshData.Lathe(new[] { new Vector2(0.058f * s, -0.008f * s), new Vector2(0.06f * s, 0f), new Vector2(0.052f * s, 0.012f * s), new Vector2(0.001f, 0.015f * s) }, 18)
                    .Transformed(at, Quaternion.Euler(90f, 0f, 0f), Vector3.one));
                // Knotwork: an inner ring and a cross.
                d.Add(Joints.Body, VikingModel.Shade(d.pal.brass, 0.6f), MeshData.Lathe(new[] { new Vector2(0.032f * s, 0f), new Vector2(0.032f * s, 0.004f * s) }, 16).Transformed(at + new Vector3(0f, 0f, 0.012f * s), Quaternion.Euler(90f, 0f, 0f), Vector3.one), false);
                d.Add(Joints.Body, VikingModel.Shade(d.pal.brass, 0.6f), MeshData.Box(at + new Vector3(0f, 0f, 0.014f * s), new Vector3(0.06f, 0.008f, 0.004f) * s), false);
                d.Add(Joints.Body, VikingModel.Shade(d.pal.brass, 0.6f), MeshData.Box(at + new Vector3(0f, 0f, 0.014f * s), new Vector3(0.008f, 0.06f, 0.004f) * s), false);
                // Strap from the brooch across the chest to the opposite hip, with a ring halfway.
                var end = new Vector3(-x * f.waistR * 0.8f, f.waist + 0.03f * s, f.waistR * f.depth + 0.015f);
                var mid = Vector3.Lerp(at, end, 0.5f) + new Vector3(0f, 0f, 0.012f * s);
                d.Add(Joints.Body, d.pal.leatherDark, MeshData.Tube(new[] { at + new Vector3(0f, -0.03f * s, -0.01f * s), mid, end }, new[] { 0.011f * s, 0.012f * s, 0.011f * s }, 5), false);
                d.Add(Joints.Body, d.pal.brass, MeshData.Lathe(new[] { new Vector2(0.02f * s, -0.005f * s), new Vector2(0.02f * s, 0.005f * s) }, 10).Transformed(mid + new Vector3(0f, 0f, 0.012f * s), Quaternion.Euler(90f, 0f, 0f), Vector3.one), false);
            }
        }

        /// <summary>A belt hung with gold rings and dangling straps.</summary>
        public static void RingBelt(Dresser d)
        {
            var f = d.fit;
            float s = d.S;
            float y = f.waist, r = f.waistR * 1.06f, depth = f.depth + 0.05f;
            d.Add(Joints.Body, d.pal.leatherDark, CharacterKit.Band(y, 0.06f * s, r, depth));
            d.Add(Joints.Body, d.pal.leather, CharacterKit.Band(y - 0.07f * s, 0.03f * s, d.SkirtRadius(y - 0.07f * s) * 1.03f, depth + 0.01f));
            for (int i = -2; i <= 2; i++)
            {
                float a = i * 0.42f;
                var at = new Vector3(Mathf.Sin(a) * r, y - (i == 0 ? 0f : 0.01f) * s, Mathf.Cos(a) * r * depth + 0.012f);
                float size = i == 0 ? 0.034f : 0.022f;
                d.Add(Joints.Body, d.pal.brass, MeshData.Lathe(new[] { new Vector2(size * s, -0.006f * s), new Vector2(size * s, 0.006f * s) }, 12)
                    .Transformed(at, Quaternion.LookRotation(new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a))) * Quaternion.Euler(90f, 0f, 0f), Vector3.one), false);
                if (i != 0)
                {
                    var hang = at + new Vector3(0f, -0.13f * s, 0.02f * s);
                    d.Add(Joints.Body, d.pal.leatherDark, MeshData.Tube(new[] { at + new Vector3(0f, -size * s, 0f), hang }, new[] { 0.008f * s, 0.007f * s }, 5), false);
                    d.Add(Joints.Body, d.pal.brass, MeshData.Ellipsoid(hang, Vector3.one * 0.01f * s, 5, 3), false);
                }
            }
        }

        /// <summary>The jarl's tall black crown-helm: flaring up from a low brim, gold-trimmed, with curved horns and a rune plate.</summary>
        public static void JarlCrown(Dresser d)
        {
            var f = d.fit;
            float r = f.headR, cy = HeroModel.HeadCentre(f), s = d.S;
            float brim = cy + 0.12f * r;
            var black = d.pal.clothDark;
            // A low crown that flares out wide towards the top, like an upturned bell.
            var scale = new Vector3(1.1f, 1f, 0.92f);
            var offset = new Vector3(0f, 0f, -0.02f * r);
            d.Add(Joints.Head, black, MeshData.Lathe(new[] {
                new Vector2(r * 1.13f, brim - 0.1f * r), new Vector2(r * 1.15f, brim + 0.2f * r), new Vector2(r * 1.28f, brim + 0.5f * r),
                new Vector2(r * 1.46f, brim + 0.72f * r), new Vector2(r * 1.3f, brim + 0.78f * r), new Vector2(0.01f, brim + 0.7f * r) }, 24)
                .Transformed(offset, Quaternion.identity, scale));
            // Gold bands at the brim and around the flared rim, with studs.
            d.Add(Joints.Head, d.pal.brass, CharacterKit.Band(brim - 0.03f * r, 0.03f * s, r * 1.19f, 0.97f, 24).Transformed(offset, Quaternion.identity, scale));
            d.Add(Joints.Head, d.pal.brass, CharacterKit.Band(brim + 0.7f * r, 0.024f * s, r * 1.48f, 0.97f, 24).Transformed(offset, Quaternion.identity, scale));
            for (int i = -4; i <= 4; i++)
            {
                float a = i * 0.36f;
                d.Add(Joints.Head, d.pal.brass, MeshData.Ellipsoid(offset + Vector3.Scale(new Vector3(Mathf.Sin(a) * r * 1.34f, brim + 0.45f * r, Mathf.Cos(a) * r * 1.34f), scale), Vector3.one * 0.009f * s, 5, 3), false);
            }
            // Big curved horns sweeping out and up from the rim: two at the sides, two smaller at the back,
            // black with gold edges.
            foreach (var a in new[] { -0.95f, 0.95f, -2.35f, 2.35f })
            {
                bool side = Mathf.Abs(a) < 2f;
                var dir = new Vector3(Mathf.Sin(a) * scale.x, 0f, Mathf.Cos(a) * scale.z);
                var root = offset + new Vector3(0f, brim + 0.62f * r, 0f) + dir * r * 1.3f;
                float len = side ? 1.45f : 0.8f;
                var path = new[] {
                    root, root + dir * r * 0.35f * len + Vector3.up * r * 0.25f * len,
                    root + dir * r * 0.45f * len + Vector3.up * r * 0.65f * len, root + dir * r * 0.3f * len + Vector3.up * r * 0.95f * len + Vector3.forward * 0.1f * r };
                d.Add(Joints.Head, black, MeshData.Tube(path, new[] { 0.04f * s, 0.032f * s, 0.018f * s, 0.003f * s }, 7));
                var edge = new Vector3[path.Length - 1];
                for (int k = 1; k < path.Length; k++) edge[k - 1] = path[k] + dir * 0.014f * s * (1f - k * 0.25f) + Vector3.up * 0.004f * s;
                d.Add(Joints.Head, d.pal.brass, MeshData.Tube(edge, new[] { 0.009f * s, 0.006f * s, 0.002f * s }, 5), false);
            }
            // A tall crenellated gold crest on the front, rising above the rim, with a rune.
            float pz = r * 1.2f * scale.z + 0.012f * s;
            var plate = new[] {
                new Vector2(-0.065f, 0.0f), new Vector2(0.065f, 0.0f), new Vector2(0.07f, 0.13f), new Vector2(0.045f, 0.16f),
                new Vector2(0.025f, 0.13f), new Vector2(0f, 0.19f), new Vector2(-0.025f, 0.13f), new Vector2(-0.045f, 0.16f), new Vector2(-0.07f, 0.13f) };
            d.Add(Joints.Head, d.pal.brass, MeshData.Extrude(plate, 0.018f).Transformed(new Vector3(0f, brim + 0.05f * r, pz), Quaternion.Euler(-8f, 90f, 0f), Vector3.one * s));
            d.Add(Joints.Head, black, MeshData.Box(new Vector3(0f, brim + 0.05f * r + 0.075f * s, pz + 0.012f * s), new Vector3(0.009f, 0.08f, 0.004f) * s), false);
            d.Add(Joints.Head, black, MeshData.Box(new Vector3(0.017f * s, brim + 0.05f * r + 0.095f * s, pz + 0.012f * s), new Vector3(0.034f, 0.008f, 0.004f) * s), false);
            d.Add(Joints.Head, black, MeshData.Box(new Vector3(-0.012f * s, brim + 0.05f * r + 0.055f * s, pz + 0.012f * s), new Vector3(0.026f, 0.008f, 0.004f) * s), false);
        }

        // ---------------------------------------------------------------- the navigator

        /// <summary>Plain leather boots to mid-shin with a turned-down top and criss-cross straps (no fur).</summary>
        public static void ShinBoots(Dresser d, float height)
        {
            var f = d.fit;
            float s = d.S;
            foreach (var joint in new[] { Joints.LeftLeg, Joints.RightLeg })
            {
                float sole = -f.hip, top = sole + height * s;
                float r = 0.058f * s * Mathf.Sqrt(f.width);
                d.Add(joint, d.pal.leatherDark, MeshData.Lathe(new[] {
                    new Vector2(r * 0.85f, sole), new Vector2(r * 1.02f, sole + 0.04f * s), new Vector2(r * 0.9f, sole + 0.1f * s),
                    new Vector2(r * 1.0f, top - 0.03f * s), new Vector2(r * 1.2f, top) }, 12));
                d.Add(joint, d.pal.leatherDark, MeshData.Tube(new[] {
                    new Vector3(0f, sole + 0.045f * s, 0f), new Vector3(0f, sole + 0.035f * s, 0.11f * s), new Vector3(0f, sole + 0.05f * s, 0.18f * s) },
                    new[] { r * 0.88f, r * 0.6f, 0.004f }, 10));
                d.Add(joint, VikingModel.Shade(d.pal.leatherDark, 1.3f), MeshData.Lathe(new[] { new Vector2(r * 1.2f, top - 0.035f * s), new Vector2(r * 1.24f, top + 0.005f * s) }, 12));
                d.Add(joint, d.pal.leather, CharacterKit.Spiral(sole + 0.06f * s, top - 0.05f * s, r * 1.02f, 1.2f, 0f, 0.0065f * s), false);
                d.Add(joint, d.pal.leather, CharacterKit.Spiral(sole + 0.06f * s, top - 0.05f * s, r * 1.02f, -1.2f, Mathf.PI, 0.0065f * s), false);
            }
        }

        /// <summary>An ochre apron of two long panels hanging from the belt at the front, with fringed ends.</summary>
        public static void Apron(Dresser d, float length)
        {
            var f = d.fit;
            float s = d.S;
            foreach (float x in new[] { -1f, 1f })
            {
                const int rows = 6;
                var grid = new Vector3[rows, 2];
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < 2; c++)
                    {
                        float v = r / (float)(rows - 1);
                        float y = f.waist - 0.03f * s - v * length * s - (r == rows - 1 && c == 1 ? 0.02f * s : 0f);
                        float xx = x * (0.012f + c * 0.055f) * s;
                        float z = d.SkirtRadius(y) * (d.hasSkirt ? d.skirtDepth : f.depth) + 0.01f + 0.012f * v;
                        grid[r, c] = new Vector3(xx, y, z);
                    }
                d.Add(Joints.Body, d.pal.cloth2, CharacterKit.Sheet(grid, Vector3.forward, 0.01f * s));
                // A stitched line down the middle of each panel.
                var line = new Vector3[rows - 1];
                var radii = new float[rows - 1];
                for (int r = 0; r < rows - 1; r++) { line[r] = (grid[r, 0] + grid[r, 1]) * 0.5f + new Vector3(0f, 0f, 0.007f * s); radii[r] = 0.0028f * s; }
                d.Add(Joints.Body, VikingModel.Shade(d.pal.cloth2, 0.6f), MeshData.Tube(line, radii, 4), false);
            }
        }

        /// <summary>
        /// A travelling cloak: short at the back, and one long drape thrown over the right shoulder that falls to the
        /// knee, fur along the shoulder and a pale embroidered zig-zag border near the hem.
        /// </summary>
        public static void SideCloak(Dresser d, float backLength, float drapeLength)
        {
            var f = d.fit;
            float s = d.S;
            var top = new Vector3(0f, f.shoulderY + 0.02f * s, -f.chestR * f.depth * 0.3f);
            float tw = f.shoulderX * 2.4f, bw = f.shoulderX * 3.2f, len = backLength * s, wrap = f.chestR * 1.2f;
            d.Add(Joints.Body, d.pal.accent, CharacterKit.Cape(top, tw, bw, len, wrap, 0.08f * s, d.seed + 51, 0.016f * s));
            // Embroidered border across the back, above the torn hem.
            var row = new Vector3[15];
            for (int i = 0; i < row.Length; i++)
                row[i] = CharacterKit.CapePoint(top, tw, bw, len, wrap, Mathf.Lerp(-0.9f, 0.9f, i / (float)(row.Length - 1)), 0.8f) + new Vector3(0f, 0f, -0.016f * s);
            d.Add(Joints.Body, d.pal.emblem, CharacterKit.ZigZag(row, Vector3.up, 0.018f * s, 0.004f * s), false);
            for (int i = 0; i < row.Length; i++) row[i] += Vector3.down * 0.05f * s;
            d.Add(Joints.Body, d.pal.emblem, CharacterKit.ZigZag(row, Vector3.up, 0.012f * s, 0.0035f * s), false);

            // The long drape over the right shoulder, falling down the front and side.
            const int rows = 9, cols = 4;
            var grid = new Vector3[rows, cols];
            var rng = new System.Random(d.seed + 52);
            var hang = new float[cols];
            for (int c = 0; c < cols; c++) hang[c] = drapeLength * s * (0.85f + 0.2f * c) + ((c & 1) == 0 ? 0.05f : 0f) * s * (float)rng.NextDouble();
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    float v = r / (float)(rows - 1), u = c / (float)(cols - 1);
                    float y = f.shoulderY + 0.04f * s - v * hang[c];
                    // From the front of the shoulder (u = 0) round to the side (u = 1), widening as it falls.
                    float ang = Mathf.Lerp(0.35f, 1.55f, u) + v * 0.15f;
                    float rad = Mathf.Max(f.TorsoRadius(Mathf.Max(y, f.waist)), d.SkirtRadius(y)) + 0.035f * s + v * 0.05f * s;
                    grid[r, c] = new Vector3(Mathf.Sin(ang) * rad, y, Mathf.Cos(ang) * rad * (f.depth + 0.1f));
                }
            d.Add(Joints.Body, d.pal.accent, CharacterKit.Sheet(grid, new Vector3(1f, 0f, 0.6f), 0.016f * s));
            var trim = new Vector3[cols * 2 + 1];
            for (int i = 0; i < trim.Length; i++)
            {
                float t = i / (float)(trim.Length - 1) * (cols - 1);
                int c0 = Mathf.Min(cols - 2, Mathf.FloorToInt(t));
                trim[i] = Vector3.Lerp(grid[rows - 2, c0], grid[rows - 2, c0 + 1], t - c0);
                trim[i] += new Vector3(trim[i].x, 0f, trim[i].z).normalized * 0.014f * s;
            }
            d.Add(Joints.Body, d.pal.emblem, CharacterKit.ZigZag(trim, Vector3.up, 0.02f * s, 0.004f * s), false);
            // Fur along the shoulder where the drape is thrown over.
            d.Add(Joints.Body, d.pal.fur, CharacterKit.FurRing(new Vector3(f.shoulderX * 0.55f, f.shoulderY + 0.02f * s, 0f), f.shoulderX * 0.55f, 0.9f, 0.05f * s, 12, 0.08f * s, d.seed + 53, 0.9f));
        }

        /// <summary>A fur collar round the neck and shoulders, smaller than a full pelt.</summary>
        public static void FurCollar(Dresser d)
        {
            var f = d.fit;
            float s = d.S;
            d.Add(Joints.Body, d.pal.fur, CharacterKit.FurRing(new Vector3(0f, f.shoulderY - 0.005f * s, 0f), f.chestR * 0.95f, f.depth + 0.05f, 0.055f * s, 16, 0.07f * s, d.seed + 54, 0.8f));
        }

        /// <summary>A soft cloth bandana tied over the head, a studded leather band across the brow, a rune on the front,
        /// the knot's two tails hanging at the back, and a fringe of hair showing at the temples.</summary>
        public static void Bandana(Dresser d)
        {
            var f = d.fit;
            float r = f.headR, cy = HeroModel.HeadCentre(f), s = d.S;
            float brim = cy + 0.1f * r;
            d.Add(Joints.Head, d.pal.accent, MeshData.Lathe(new[] {
                new Vector2(r * 1.07f, brim - 0.05f * r), new Vector2(r * 1.09f, brim + 0.3f * r), new Vector2(r * 1.0f, brim + 0.65f * r),
                new Vector2(r * 0.75f, brim + 0.92f * r), new Vector2(0.01f, brim + 1.05f * r) }, 18)
                .Transformed(new Vector3(0f, 0f, -0.03f * r), Quaternion.Euler(-8f, 0f, 0f), new Vector3(1f, 1f, 1.05f)));
            // Studded leather band.
            d.Add(Joints.Head, d.pal.leather, CharacterKit.Band(brim + 0.02f * r, 0.03f * s, r * 1.1f, 1.02f, 18));
            for (int i = -3; i <= 3; i++)
            {
                float a = i * 0.32f;
                d.Add(Joints.Head, d.pal.brass, MeshData.Ellipsoid(new Vector3(Mathf.Sin(a) * r * 1.13f, brim + 0.02f * r, Mathf.Cos(a) * r * 1.15f), Vector3.one * 0.006f * s, 5, 3), false);
            }
            // A pale rune (like a K with a crossbar) on the front.
            float rz = r * 1.02f, ry = brim + 0.45f * r;
            var rune = new[] {
                new[] { new Vector3(-0.02f, -0.025f, 0f), new Vector3(-0.02f, 0.03f, 0f) },
                new[] { new Vector3(-0.02f, 0.003f, 0f), new Vector3(0.018f, 0.03f, 0f) },
                new[] { new Vector3(-0.02f, 0.003f, 0f), new Vector3(0.018f, -0.025f, 0f) } };
            foreach (var seg in rune)
                d.Add(Joints.Head, d.pal.emblem, MeshData.Tube(new[] { new Vector3(0f, ry, rz) + seg[0] * s, new Vector3(0f, ry, rz) + seg[1] * s }, new[] { 0.004f * s, 0.004f * s }, 4)
                    .Transformed(Vector3.zero, Quaternion.Euler(-12f, 0f, 0f), Vector3.one), false);
            // The knot at the back left, and two tails hanging down.
            var knot = new Vector3(r * 0.55f, brim + 0.35f * r, -r * 0.95f);
            d.Add(Joints.Head, d.pal.accent, MeshData.Ellipsoid(knot, new Vector3(0.035f, 0.03f, 0.03f) * s, 8, 5));
            for (int t = 0; t < 2; t++)
            {
                var grid = new Vector3[4, 2];
                for (int r2 = 0; r2 < 4; r2++)
                    for (int c = 0; c < 2; c++)
                    {
                        float v = r2 / 3f;
                        grid[r2, c] = knot + new Vector3((0.005f + t * 0.03f + v * 0.015f) * s + c * 0.03f * s, -0.01f * s - v * (0.13f + t * 0.04f) * s, -0.015f * s - v * 0.03f * s);
                    }
                d.Add(Joints.Head, d.pal.accent, CharacterKit.Sheet(grid, new Vector3(0.3f, 0f, -1f), 0.01f * s));
            }
            // Fringe of hair at the temples.
            foreach (float x in new[] { -1f, 1f })
                d.Add(Joints.Head, d.pal.hair, CharacterKit.Tuft(new Vector3(x * r * 0.85f, brim, r * 0.45f), new Vector3(x * r * 0.98f, cy - r * 0.35f, r * 0.45f), 0.028f * s));
        }

        /// <summary>Rolled charts and a leather chart case hanging on the belt at the right hip.</summary>
        public static void ScrollCase(Dresser d)
        {
            var f = d.fit;
            float s = d.S;
            float y = f.waist - 0.1f * s;
            float x = f.waistR * 1.1f + 0.02f * s;
            var at = new Vector3(x, y, d.SkirtRadius(y) * 0.2f);
            // The case: a leather tube hanging at a slant.
            d.Add(Joints.Body, d.pal.leather, MeshData.Lathe(new[] { new Vector2(0.03f * s, -0.12f * s), new Vector2(0.032f * s, 0.1f * s) }, 10)
                .Transformed(at + new Vector3(0.02f * s, -0.05f * s, 0.03f * s), Quaternion.Euler(0f, 0f, 25f), Vector3.one));
            d.Add(Joints.Body, d.pal.brass, MeshData.Lathe(new[] { new Vector2(0.035f * s, -0.01f * s), new Vector2(0.035f * s, 0.01f * s) }, 10)
                .Transformed(at + new Vector3(0.02f * s, -0.05f * s, 0.03f * s), Quaternion.Euler(0f, 0f, 25f), Vector3.one), false);
            // Two loose rolls of parchment tucked in the belt, lying crosswise.
            for (int i = 0; i < 2; i++)
            {
                var c = at + new Vector3(-0.02f * s, (0.06f - i * 0.05f) * s, (0.07f + i * 0.02f) * s);
                d.Add(Joints.Body, d.pal.parchment, MeshData.Lathe(new[] { new Vector2(0.024f * s, -0.07f * s), new Vector2(0.024f * s, 0.07f * s) }, 10)
                    .Transformed(c, Quaternion.Euler(0f, 30f + i * 20f, 80f - i * 15f), Vector3.one));
                d.Add(Joints.Body, VikingModel.Shade(d.pal.parchment, 0.7f), MeshData.Lathe(new[] { new Vector2(0.026f * s, -0.008f * s), new Vector2(0.026f * s, 0.008f * s) }, 10)
                    .Transformed(c, Quaternion.Euler(0f, 30f + i * 20f, 80f - i * 15f), Vector3.one), false);
            }
        }

        // ---------------------------------------------------------------- the spear guard

        /// <summary>A red scarf wound round the neck, its two ends hanging down across the chest.</summary>
        public static void Scarf(Dresser d)
        {
            var f = d.fit;
            float s = d.S;
            float y = f.shoulderY + 0.035f * s;
            d.Add(Joints.Body, d.pal.accent, MeshData.Lathe(new[] {
                new Vector2(f.chestR * 0.55f, y - 0.035f * s), new Vector2(f.chestR * 0.72f, y - 0.01f * s), new Vector2(f.chestR * 0.68f, y + 0.03f * s), new Vector2(f.chestR * 0.45f, y + 0.05f * s) }, 16)
                .Transformed(Vector3.zero, Quaternion.identity, new Vector3(1f, 1f, 0.95f)));
            float cz = f.chestR * f.depth + 0.02f;
            foreach (float side in new[] { -1f, 1f })
            {
                const int rows = 5;
                var grid = new Vector3[rows, 2];
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < 2; c++)
                    {
                        float v = r / (float)(rows - 1);
                        float len = side < 0f ? 0.26f : 0.2f;
                        float x = side * 0.02f * s + Mathf.Lerp(0f, 0.09f, v) * s * (side < 0f ? 1f : 0.4f) + (c - 0.5f) * 0.065f * s;
                        float yy = y - 0.02f * s - v * len * s - (r == rows - 1 && c == 0 ? 0.025f * s : 0f);
                        grid[r, c] = new Vector3(x, yy, cz + v * 0.02f + (side < 0f ? 0.012f : 0f));
                    }
                d.Add(Joints.Body, side < 0f ? d.pal.accent : VikingModel.Shade(d.pal.accent, 0.82f), CharacterKit.Sheet(grid, Vector3.forward, 0.012f * s));
            }
        }

        /// <summary>A leather jerkin: brass studs down both front edges and around the hem of its short skirt.</summary>
        public static void StuddedTrim(Dresser d)
        {
            var f = d.fit;
            float s = d.S;
            foreach (float x in new[] { -0.045f, 0.045f })
                for (int i = 0; i < 9; i++)
                {
                    float y = Mathf.Lerp(f.chest - 0.04f * s, d.hasSkirt ? d.skirtBottom + 0.03f * s : f.waist, i / 8f);
                    float r = y > f.waist ? f.TorsoRadius(y) * f.depth : d.SkirtRadius(y) * d.skirtDepth;
                    d.Add(Joints.Body, d.pal.brass, MeshData.Ellipsoid(new Vector3(x * s, y, r + 0.008f * s), Vector3.one * 0.008f * s, 5, 3), false);
                }
            if (!d.hasSkirt) return;
            for (int i = 0; i < 18; i++)
            {
                float a = i / 18f * Mathf.PI * 2f;
                float y = d.skirtBottom + 0.035f * s, r = d.SkirtRadius(y) + 0.006f;
                d.Add(Joints.Body, d.pal.brass, MeshData.Ellipsoid(new Vector3(Mathf.Sin(a) * r, y, Mathf.Cos(a) * r * d.skirtDepth), Vector3.one * 0.0075f * s, 5, 3), false);
            }
        }

        /// <summary>A belt with a big round ring buckle and one pouch hanging at the right hip.</summary>
        public static void RingBuckleBelt(Dresser d)
        {
            var f = d.fit;
            float s = d.S;
            float y = f.waist, r = f.waistR * 1.06f, depth = f.depth + 0.05f;
            d.Add(Joints.Body, d.pal.leatherDark, CharacterKit.Band(y, 0.05f * s, r, depth));
            var at = new Vector3(0f, y, r * depth + 0.006f);
            d.Add(Joints.Body, d.pal.metal, MeshData.Lathe(new[] { new Vector2(0.034f * s, -0.007f * s), new Vector2(0.036f * s, 0f), new Vector2(0.034f * s, 0.007f * s), new Vector2(0.02f * s, 0.007f * s), new Vector2(0.02f * s, -0.007f * s) }, 16)
                .Transformed(at, Quaternion.Euler(90f, 0f, 0f), Vector3.one));
            float px = f.waistR * 0.95f, py = y - 0.1f * s;
            var pouch = new Vector3(px, py, d.SkirtRadius(py) * d.skirtDepth * 0.6f + 0.03f * s);
            d.Add(Joints.Body, d.pal.leather, MeshData.Box(pouch, new Vector3(0.06f, 0.085f, 0.045f) * s));
            d.Add(Joints.Body, d.pal.leatherDark, MeshData.Box(pouch + new Vector3(0f, 0.03f * s, 0.024f * s), new Vector3(0.064f, 0.03f, 0.008f) * s), false);
            d.Add(Joints.Body, d.pal.leatherDark, MeshData.Tube(new[] { new Vector3(px, y, f.waistR * depth * 0.7f), pouch + new Vector3(0f, 0.04f * s, 0f) }, new[] { 0.007f * s, 0.007f * s }, 4), false);
        }

        // ---------------------------------------------------------------- the old seer

        /// <summary>A long robe to the ground, torn into strips at the hem, flaring wide.</summary>
        public static void Robe(Dresser d)
        {
            var f = d.fit;
            float s = d.S;
            d.hasSkirt = true;
            d.skirtTop = f.waist; d.skirtTopR = f.waistR * 0.98f; d.skirtBottom = 0.06f * s; d.skirtBottomR = f.hipR * 1.9f; d.skirtDepth = f.depth + 0.08f;
            d.Add(Joints.Body, d.pal.cloth, CharacterKit.RaggedSkirt(d.skirtTop, d.skirtTopR, d.skirtBottom, d.skirtBottomR, d.skirtDepth, 26, 0.09f * s, d.seed + 61));
        }

        /// <summary>
        /// A shaggy cloak of torn strips and feathers: tier upon tier of dark flaps from the shoulders to the ground,
        /// in two shades, so the whole figure looks ragged like the concept's seer.
        /// </summary>
        public static void FeatherCloak(Dresser d)
        {
            var f = d.fit;
            float s = d.S;
            System.Func<float, float> surface = y => y > f.waist ? f.TorsoRadius(y) * 1.15f + 0.03f * s : d.SkirtRadius(y) * 1.02f;
            float depth = d.hasSkirt ? d.skirtDepth : f.depth;
            float[] tops = { f.shoulderY + 0.01f * s, f.chest - 0.02f * s, f.waist - 0.02f * s, f.waist - 0.3f * s, 0.45f * s };
            for (int t = 0; t < tops.Length; t++)
            {
                var colour = (t & 1) == 0 ? d.pal.cloth : d.pal.clothDark;
                float len = t == 0 ? 0.22f * s : 0.34f * s;
                d.Add(Joints.Body, colour, CharacterKit.Flaps(tops[t], len, 14 + t * 2, 0.95f, depth, surface, 0.01f + t * 0.004f, d.seed + 62 + t, 0.01f * s));
            }
            // Feathers bristling round the shoulders.
            d.Add(Joints.Body, d.pal.clothDark, CharacterKit.FurRing(new Vector3(0f, f.shoulderY - 0.01f * s, 0f), f.shoulderX + 0.05f * s, 0.8f, 0.06f * s, 24, 0.14f * s, d.seed + 67, 1.1f));
            // Ragged sleeves hanging from the arms.
            foreach (var arm in new[] { Joints.LeftArm, Joints.RightArm })
            {
                float r = 0.06f * s;
                d.Add(arm, d.pal.cloth, MeshData.Lathe(new[] { new Vector2(r * 1.3f, -f.upperArm - 0.02f * s), new Vector2(r, -0.1f * s), new Vector2(r * 0.9f, 0.03f * s) }, 10));
            }
            foreach (var fore in new[] { Joints.LeftForearm, Joints.RightForearm })
            {
                float r = 0.07f * s;
                d.Add(fore, d.pal.cloth, CharacterKit.RaggedSkirt(0.01f * s, r, -f.foreArm * 0.7f, r * 1.4f, 1f, 12, 0.05f * s, d.seed + fore.Length));
            }
        }

        /// <summary>A deep hood round the face, dark, its edge ragged; the face shows in its opening.</summary>
        public static void Hood(Dresser d)
        {
            var f = d.fit;
            float r = f.headR, cy = HeroModel.HeadCentre(f), s = d.S;
            // The hood's shell sits behind and over the head, a little bigger, with the front cut open.
            d.Add(Joints.Head, d.pal.clothDark, MeshData.Ellipsoid(new Vector3(0f, cy + 0.06f * r, -0.28f * r), new Vector3(r * 1.25f, r * 1.28f, r * 1.1f), 16, 10));
            // The hood's rim framing the face.
            var rim = new Vector3[15];
            var rr = new float[15];
            for (int i = 0; i < rim.Length; i++)
            {
                float a = Mathf.Lerp(-0.35f, Mathf.PI + 0.35f, i / (float)(rim.Length - 1));
                rim[i] = new Vector3(Mathf.Cos(a) * r * 1.12f, cy + Mathf.Sin(a) * r * 1.15f + 0.02f * r, r * 0.45f);
                rr[i] = 0.03f * s;
            }
            d.Add(Joints.Head, d.pal.cloth, MeshData.Tube(rim, rr, 6));
            // Ragged feathers bristling all round the hood's edge.
            var rng = new System.Random(d.seed + 70);
            for (int i = 0; i < 16; i++)
            {
                float a = Mathf.Lerp(-0.6f, Mathf.PI + 0.6f, i / 15f);
                var root = new Vector3(Mathf.Cos(a) * r * 1.2f, cy + Mathf.Sin(a) * r * 1.22f, r * 0.2f);
                var outDir = new Vector3(Mathf.Cos(a), Mathf.Sin(a) - 0.6f, -0.3f).normalized;
                d.Add(Joints.Head, i % 2 == 0 ? d.pal.clothDark : d.pal.cloth, CharacterKit.Tuft(root, root + outDir * (0.06f + (float)rng.NextDouble() * 0.05f) * s, 0.02f * s));
            }
            // The hood falls onto the shoulders behind the neck.
            d.Add(Joints.Head, d.pal.clothDark, MeshData.Ellipsoid(new Vector3(0f, cy - r * 1.0f, -r * 0.55f), new Vector3(r * 1.35f, r * 0.7f, r * 0.9f), 14, 8));
        }

        /// <summary>A long pale stole hanging down the front to the ground, with dark zig-zags and runes.</summary>
        public static void Stole(Dresser d)
        {
            var f = d.fit;
            float s = d.S;
            const int rows = 12;
            var grid = new Vector3[rows, 3];
            float top = f.chest + 0.02f * s, bottom = 0.1f * s;
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < 3; c++)
                {
                    float v = r / (float)(rows - 1);
                    float y = Mathf.Lerp(top, bottom, v) - (r == rows - 1 && c != 1 ? 0.03f * s : 0f);
                    float rad = y > f.waist ? f.TorsoRadius(y) * f.depth + 0.03f * s : d.SkirtRadius(y) * d.skirtDepth + 0.03f * s;
                    grid[r, c] = new Vector3((c - 1) * Mathf.Lerp(0.05f, 0.075f, v) * s * f.width, y, rad + 0.02f * v);
                }
            d.Add(Joints.Body, d.pal.cloth2, CharacterKit.Sheet(grid, Vector3.forward, 0.01f * s));
            // Rows of zig-zags down its length.
            for (int band = 0; band < 5; band++)
            {
                float v = 0.2f + band * 0.16f;
                int r = Mathf.RoundToInt(v * (rows - 1));
                var line = new Vector3[7];
                for (int i = 0; i < line.Length; i++)
                    line[i] = Vector3.Lerp(grid[r, 0], grid[r, 2], i / 6f) * 1f + new Vector3(0f, 0f, 0.009f * s);
                d.Add(Joints.Body, d.pal.clothDark, CharacterKit.ZigZag(line, Vector3.up, 0.018f * s, 0.004f * s), false);
            }
        }

        /// <summary>Necklaces of beads with bone charms and wooden rune discs, and a rune-disc belt.</summary>
        public static void Charms(Dresser d)
        {
            var f = d.fit;
            float s = d.S;
            float cz = f.chestR * f.depth + 0.035f * s;
            // Two strings of beads hanging in loops across the chest.
            for (int k = 0; k < 2; k++)
            {
                int n = 13;
                for (int i = 0; i < n; i++)
                {
                    float u = i / (float)(n - 1) * 2f - 1f;
                    var p = new Vector3(u * f.chestR * (0.7f + 0.1f * k), f.shoulderY - 0.02f * s - (1f - u * u) * (0.1f + 0.08f * k) * s, cz + (1f - u * u) * 0.01f);
                    d.Add(Joints.Body, i % 3 == 0 ? d.pal.parchment : d.pal.leather, MeshData.Ellipsoid(p, Vector3.one * 0.011f * s, 6, 4), false);
                }
            }
            // Pendants: a bone, a rune disc, a claw.
            var pend = new[] { new Vector3(-0.05f, 0f, 0f), new Vector3(0.0f, -0.03f, 0.005f), new Vector3(0.055f, 0.01f, 0f) };
            for (int i = 0; i < pend.Length; i++)
            {
                var p = new Vector3(pend[i].x * s, f.shoulderY - 0.2f * s + pend[i].y * s, cz + 0.01f);
                if (i == 1) RuneDisc(d, Joints.Body, p, 0.03f * s, Quaternion.Euler(90f, 0f, 0f));
                else d.Add(Joints.Body, d.pal.parchment, MeshData.Ellipsoid(p, new Vector3(0.012f, 0.03f, 0.012f) * s, 6, 5));
            }
            // Belt: a cord with a big wooden rune disc at the front.
            d.Add(Joints.Body, d.pal.leather, CharacterKit.Band(f.waist, 0.03f * s, d.SkirtRadius(f.waist) * 1.06f, d.skirtDepth + 0.02f));
            RuneDisc(d, Joints.Body, new Vector3(0f, f.waist, d.SkirtRadius(f.waist) * (d.skirtDepth + 0.02f) + 0.06f * s), 0.045f * s, Quaternion.Euler(90f, 0f, 0f));
            // Charms hanging from the belt on cords.
            foreach (float x in new[] { -0.09f, -0.05f, 0.07f })
            {
                var top = new Vector3(x * s, f.waist - 0.02f * s, d.SkirtRadius(f.waist) * d.skirtDepth + 0.02f);
                var end = top + new Vector3(0f, -0.2f * s, 0.02f * s);
                d.Add(Joints.Body, d.pal.leatherDark, MeshData.Tube(new[] { top, end }, new[] { 0.003f * s, 0.003f * s }, 4), false);
                if (x > 0f) RuneDisc(d, Joints.Body, end, 0.028f * s, Quaternion.Euler(90f, 0f, 0f));
                else d.Add(Joints.Body, d.pal.parchment, MeshData.Ellipsoid(end, new Vector3(0.018f, 0.028f, 0.016f) * s, 7, 5));
            }
        }

        /// <summary>A round wooden disc with a rune burnt into it (Algiz-like), facing along the rotation's up axis.</summary>
        public static void RuneDisc(Dresser d, string joint, Vector3 at, float radius, Quaternion facing)
        {
            float s = d.S;
            d.Add(joint, d.pal.leather, MeshData.Lathe(new[] { new Vector2(radius, -0.006f * s), new Vector2(radius, 0.006f * s) }, 14).Transformed(at, facing, Vector3.one));
            var up = facing * Vector3.up;
            var a = facing * Vector3.forward;
            var b = facing * Vector3.right;
            var c = at + up * 0.0075f * s;
            foreach (var seg in new[] { new[] { -0.7f, 0f, 0.7f, 0f }, new[] { 0f, 0f, 0.6f, 0.45f }, new[] { 0f, 0f, 0.6f, -0.45f } })
                d.Add(joint, d.pal.ink, MeshData.Tube(new[] { c + (a * seg[0] + b * seg[1]) * radius, c + (a * seg[2] + b * seg[3]) * radius }, new[] { 0.0035f * s, 0.0035f * s }, 4), false);
        }

        /// <summary>Great antlers rising from the hood, hung with rune discs and bone charms on cords.</summary>
        public static void Antlers(Dresser d)
        {
            var f = d.fit;
            float r = f.headR, cy = HeroModel.HeadCentre(f), s = d.S;
            var horn = d.pal.leather;
            foreach (float x in new[] { -1f, 1f })
            {
                var root = new Vector3(x * r * 0.55f, cy + r * 1.1f, -r * 0.3f);
                var beam = new[] { root, root + new Vector3(x * 0.1f, 0.06f, 0f) * s, root + new Vector3(x * 0.22f, 0.13f, -0.02f) * s, root + new Vector3(x * 0.33f, 0.24f, -0.03f) * s, root + new Vector3(x * 0.38f, 0.36f, -0.02f) * s };
                d.Add(Joints.Head, horn, MeshData.Tube(beam, new[] { 0.034f * s, 0.03f * s, 0.025f * s, 0.018f * s, 0.008f * s }, 7));
                // Tines branching upwards off the beam.
                foreach (var t in new[] { new[] { 1f, 0.12f, 0.02f }, new[] { 2f, 0.16f, -0.03f }, new[] { 3f, 0.12f, 0.05f } })
                {
                    var at = beam[(int)t[0]];
                    var tip = at + new Vector3(x * t[2], t[1], 0.02f) * s;
                    d.Add(Joints.Head, horn, MeshData.Tube(new[] { at, (at + tip) * 0.5f + new Vector3(0f, 0f, 0.01f * s), tip }, new[] { 0.018f * s, 0.013f * s, 0.005f * s }, 6));
                }
                // Charms hanging from the beam.
                for (int k = 0; k < 3; k++)
                {
                    var hang = Vector3.Lerp(beam[1], beam[3], k / 2f);
                    float drop = (0.1f + 0.05f * k) * s;
                    var end = hang + Vector3.down * drop;
                    d.Add(Joints.Head, d.pal.leatherDark, MeshData.Tube(new[] { hang, end }, new[] { 0.0025f * s, 0.0025f * s }, 4), false);
                    if (k == 1) d.Add(Joints.Head, d.pal.parchment, MeshData.Ellipsoid(end + Vector3.down * 0.02f * s, new Vector3(0.01f, 0.03f, 0.01f) * s, 6, 4));
                    else RuneDisc(d, Joints.Head, end + Vector3.down * 0.035f * s, 0.036f * s, Quaternion.Euler(90f, 0f, 0f));
                }
            }
        }

        // ---------------------------------------------------------------- the scout

        /// <summary>A dark cloth cap pulled low, its crown and brim covered in shaggy fur, worn a little askew.</summary>
        public static void FurCap(Dresser d)
        {
            var f = d.fit;
            float r = f.headR, cy = HeroModel.HeadCentre(f), s = d.S;
            float brim = cy + 0.2f * r;
            var tilt = Quaternion.Euler(-6f, 0f, 8f);
            d.Add(Joints.Head, d.pal.clothDark, MeshData.Dome(Vector3.zero, new Vector3(r * 1.1f, r * 1.05f, r * 1.1f), 16, 6).Transformed(new Vector3(0f, brim, -0.02f * r), tilt, Vector3.one));
            d.Add(Joints.Head, d.pal.fur, CharacterKit.FurRing(Vector3.zero, r * 1.12f, 1f, 0.045f * s, 16, 0.03f * s, d.seed + 81, 0.3f).Transformed(new Vector3(0f, brim + 0.025f * s, -0.02f * r), tilt, Vector3.one));
            // Shaggy fur over the crown, leaving dark cloth showing on one side.
            var rng = new System.Random(d.seed + 82);
            for (int i = 0; i < 10; i++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 1.3f - 0.2f, e = 0.3f + (float)rng.NextDouble() * 0.9f;
                var p = new Vector3(Mathf.Cos(a) * Mathf.Cos(e), Mathf.Sin(e), Mathf.Sin(a) * Mathf.Cos(e) * 0.6f) * r * 1.08f;
                d.Add(Joints.Head, i % 3 == 0 ? d.pal.furShadow : d.pal.fur, MeshData.Ellipsoid(p, new Vector3(0.045f, 0.03f, 0.045f) * s, 7, 4).Transformed(new Vector3(0f, brim, -0.02f * r), tilt, Vector3.one));
            }
        }

        /// <summary>A green hooded capelet round the neck and shoulders, its hood thrown back, the edge ragged.</summary>
        public static void Cowl(Dresser d)
        {
            var f = d.fit;
            float s = d.S;
            d.Add(Joints.Body, d.pal.accent, MeshData.Lathe(new[] {
                new Vector2(f.chestR * 0.5f, f.neckY - 0.01f * s), new Vector2(f.chestR * 0.7f, f.neckY + 0.04f * s), new Vector2(f.chestR * 0.55f, f.neckY + 0.07f * s) }, 16)
                .Transformed(Vector3.zero, Quaternion.identity, new Vector3(1f, 1f, 0.95f)));
            d.Add(Joints.Body, d.pal.accent, CharacterKit.RaggedSkirt(f.neckY + 0.01f * s, f.chestR * 0.62f, f.chest - 0.05f * s, f.shoulderX + 0.06f * s, 0.8f, 18, 0.05f * s, d.seed + 83));
            // The hood, down, bunched behind the neck.
            d.Add(Joints.Body, VikingModel.Shade(d.pal.accent, 0.85f), MeshData.Ellipsoid(new Vector3(0f, f.neckY + 0.02f * s, -f.chestR * 0.7f), new Vector3(0.11f, 0.08f, 0.07f) * s, 12, 7));
            // A shaggy pelt thrown over the right shoulder.
            d.Add(Joints.Body, d.pal.fur, CharacterKit.Flaps(f.shoulderY + 0.03f * s, 0.14f * s, 3, 0.3f, f.depth, y => y > f.waist ? f.TorsoRadius(y) + 0.08f * s : f.waistR, 0.01f, d.seed + 84, 0.014f * s)
                .Transformed(Vector3.zero, Quaternion.Euler(0f, 70f, 0f), Vector3.one));
        }

        /// <summary>A knife in a sheath worn crosswise at the front of the belt.</summary>
        public static void BeltKnife(Dresser d)
        {
            var f = d.fit;
            float s = d.S;
            float y = f.waist - 0.06f * s, z = d.SkirtRadius(y) * (d.hasSkirt ? d.skirtDepth : f.depth) + 0.035f * s;
            var rot = Quaternion.Euler(0f, 0f, -12f);
            d.Add(Joints.Body, d.pal.leather, MeshData.Box(Vector3.zero, new Vector3(0.2f, 0.028f, 0.02f) * s).Transformed(new Vector3(0.05f * s, y, z), rot, Vector3.one));
            d.Add(Joints.Body, d.pal.metal, MeshData.Box(Vector3.zero, new Vector3(0.012f, 0.04f, 0.024f) * s).Transformed(new Vector3(-0.06f * s, y + 0.023f * s, z), rot, Vector3.one));
            d.Add(Joints.Body, d.pal.leatherDark, MeshData.Tube(new[] { new Vector3(-0.07f * s, y + 0.025f * s, z), new Vector3(-0.15f * s, y + 0.04f * s, z) }, new[] { 0.011f * s, 0.01f * s }, 6));
        }

        /// <summary>A leather quiver slung across the back, full of red-fletched arrows, with its strap across the chest.</summary>
        public static void Quiver(Dresser d)
        {
            var f = d.fit;
            float s = d.S;
            var at = new Vector3(0.06f * s, f.chest - 0.02f * s, -f.chestR * f.depth - 0.05f * s);
            var tilt = Quaternion.Euler(0f, 0f, -28f);
            d.Add(Joints.Body, d.pal.leather, MeshData.Lathe(new[] { new Vector2(0.035f * s, -0.22f * s), new Vector2(0.045f * s, 0.14f * s) }, 10).Transformed(at, tilt, Vector3.one));
            d.Add(Joints.Body, d.pal.leatherDark, MeshData.Lathe(new[] { new Vector2(0.048f * s, 0.1f * s), new Vector2(0.048f * s, 0.14f * s) }, 10).Transformed(at, tilt, Vector3.one), false);
            for (int i = 0; i < 4; i++)
            {
                var off = new Vector3((i - 1.5f) * 0.018f, 0f, (i % 2) * 0.015f) * s;
                var shaftTop = at + tilt * (off + new Vector3(0f, 0.3f * s, 0f));
                d.Add(Joints.Body, d.pal.leatherDark, MeshData.Tube(new[] { at + tilt * (off + new Vector3(0f, 0.1f * s, 0f)), shaftTop }, new[] { 0.005f * s, 0.005f * s }, 4), false);
                // Fletching: two red vanes.
                foreach (float side in new[] { -1f, 1f })
                    d.Add(Joints.Body, new Color(0.7f, 0.2f, 0.14f), MeshData.Extrude(new[] { new Vector2(0f, 0f), new Vector2(0.09f, 0f), new Vector2(0.1f, 0.03f), new Vector2(0.03f, 0.03f) }, 0.004f)
                        .Transformed(shaftTop - tilt * new Vector3(0f, 0.1f * s, 0f), tilt * Quaternion.Euler(-90f, side * 90f, 0f), Vector3.one * s));
            }
            // Strap from the right shoulder across the chest to the left hip.
            float cz = f.chestR * f.depth;
            d.Add(Joints.Body, d.pal.leatherDark, MeshData.Tube(new[] {
                new Vector3(f.shoulderX * 0.6f, f.shoulderY + 0.01f * s, cz * 0.3f), new Vector3(0f, f.chest, cz + 0.015f), new Vector3(-f.waistR * 0.9f, f.waist + 0.02f * s, f.waistR * f.depth + 0.012f) },
                new[] { 0.013f * s, 0.014f * s, 0.013f * s }, 5), false);
        }

        // ---------------------------------------------------------------- hair

        /// <summary>Two long braids from behind the ears, forward over the shoulders and down the chest, tied with leather.</summary>
        public static void LongBraids(Dresser d, float length) { LongBraids(d, length, false); }

        /// <summary>Short locks showing under the helmet at the temples and nape, and one thin braid by the left cheek.</summary>
        public static void ShortLocks(Dresser d)
        {
            var f = d.fit;
            float r = f.headR, cy = HeroModel.HeadCentre(f), s = d.S;
            d.Add(Joints.Head, d.pal.hair, MeshData.Ellipsoid(new Vector3(0f, cy - r * 0.05f, -r * 0.3f), new Vector3(r * 0.98f, r * 0.85f, r * 0.8f), 12, 8));
            foreach (float x in new[] { -1f, 1f })
            {
                d.Add(Joints.Head, d.pal.hair, CharacterKit.Tuft(new Vector3(x * r * 0.9f, cy + r * 0.1f, r * 0.25f), new Vector3(x * r * 1.0f, cy - r * 0.5f, r * 0.4f), 0.026f * s));
                d.Add(Joints.Head, d.pal.hair, CharacterKit.Tuft(new Vector3(x * r * 0.7f, cy - r * 0.3f, -r * 0.7f), new Vector3(x * r * 0.75f, cy - r * 1.05f, -r * 0.6f), 0.03f * s));
            }
            var braid = new[] { new Vector3(-r * 0.95f, cy - r * 0.2f, r * 0.2f), new Vector3(-r * 1.02f, cy - r * 0.8f, r * 0.35f), new Vector3(-r * 0.98f, cy - r * 1.35f, r * 0.45f) };
            d.Add(Joints.Head, d.pal.hair, CharacterKit.Braid(braid, 0.018f * s));
        }

        /// <summary>Messy hair tied in a low loose braid hanging behind the left shoulder, strands by the cheeks.</summary>
        public static void LowBraid(Dresser d)
        {
            var f = d.fit;
            float r = f.headR, cy = HeroModel.HeadCentre(f), s = d.S;
            d.Add(Joints.Head, d.pal.hair, MeshData.Ellipsoid(new Vector3(0f, cy - r * 0.02f, -r * 0.3f), new Vector3(r * 1.02f, r * 0.9f, r * 0.82f), 12, 8));
            foreach (float x in new[] { -1f, 1f })
                d.Add(Joints.Head, d.pal.hair, CharacterKit.Tuft(new Vector3(x * r * 0.9f, cy + r * 0.2f, r * 0.3f), new Vector3(x * r * 1.02f, cy - r * 0.55f, r * 0.42f), 0.026f * s));
            var path = new[] { new Vector3(-r * 0.6f, cy - r * 0.55f, -r * 0.75f), new Vector3(-r * 1.05f, cy - r * 1.05f, -r * 0.6f), new Vector3(-r * 1.35f, cy - r * 1.7f, -r * 0.4f), new Vector3(-r * 1.45f, cy - r * 2.3f, -r * 0.3f) };
            d.Add(Joints.Head, d.pal.hair, CharacterKit.Braid(path, 0.03f * s));
            var end = path[path.Length - 1];
            d.Add(Joints.Head, d.pal.hair, CharacterKit.Tuft(end, end + new Vector3(-0.02f, -0.08f, 0.01f) * s, 0.028f * s));
            d.Add(Joints.Head, d.pal.hair, CharacterKit.Tuft(path[1], path[1] + new Vector3(-0.07f, -0.05f, 0f) * s, 0.015f * s));
        }

        /// <summary>One thick braid over the right shoulder, down the front to the waist.</summary>
        public static void SideBraid(Dresser d, float length)
        {
            var f = d.fit;
            float r = f.headR, cy = HeroModel.HeadCentre(f), s = d.S;
            float sy = f.shoulderY - f.neckY;
            var path = new[] {
                new Vector3(r * 0.5f, cy - r * 0.2f, -r * 0.7f), new Vector3(r * 1.0f, cy - r * 0.75f, -r * 0.1f),
                new Vector3(f.shoulderX * 0.6f, sy + 0.06f * s, r * 0.6f), new Vector3(f.shoulderX * 0.55f, sy - 0.08f * s, r * 1.0f),
                new Vector3(f.shoulderX * 0.45f, sy - length * s, r * 1.0f) };
            d.Add(Joints.Head, d.pal.hair, CharacterKit.Braid(path, 0.038f * s));
            Vector3 end = path[path.Length - 1];
            d.Add(Joints.Head, d.pal.leatherDark, MeshData.Ellipsoid(CharacterKit.Along(path, 0.9f), new Vector3(0.03f, 0.013f, 0.03f) * s, 8, 4), false);
            d.Add(Joints.Head, d.pal.hair, CharacterKit.Tuft(end, end + new Vector3(0f, -0.07f * s, 0.01f), 0.026f * s));
            d.Add(Joints.Head, d.pal.hair, MeshData.Ellipsoid(new Vector3(0f, cy - r * 0.1f, -r * 0.25f), new Vector3(r * 0.98f, r * 0.8f, r * 0.8f), 12, 8));
        }

        /// <summary>Long braids; <paramref name="wrapped"/> binds the lower part in pale cloth, like the jarl's.</summary>
        public static void LongBraids(Dresser d, float length, bool wrapped)
        {
            var f = d.fit;
            float r = f.headR, cy = HeroModel.HeadCentre(f), s = d.S;
            float sy = f.shoulderY - f.neckY; // the shoulders, in head space
            foreach (float x in new[] { -1f, 1f })
            {
                var path = new[] {
                    new Vector3(x * r * 0.9f, cy - r * 0.15f, -r * 0.35f), new Vector3(x * r * 1.02f, cy - r * 0.75f, -r * 0.05f),
                    new Vector3(x * f.shoulderX * 0.75f, sy + 0.07f * s, r * 0.55f), new Vector3(x * f.shoulderX * 0.7f, sy - 0.06f * s, r * 0.95f),
                    new Vector3(x * f.shoulderX * 0.62f, sy - length * s, r * 0.95f) };
                d.Add(Joints.Head, d.pal.hair, CharacterKit.Braid(path, 0.03f * s));
                if (wrapped)
                    for (int k = 0; k < 4; k++)
                        d.Add(Joints.Head, d.pal.fur, MeshData.Ellipsoid(CharacterKit.Along(path, 0.62f + k * 0.08f), new Vector3(0.034f, 0.024f, 0.034f) * s, 8, 5));
                // Leather ties and a loose tassel at the end.
                Vector3 tie = CharacterKit.Along(path, 0.88f);
                d.Add(Joints.Head, d.pal.leatherDark, MeshData.Ellipsoid(tie, new Vector3(0.026f, 0.012f, 0.026f) * s, 8, 4), false);
                Vector3 end = path[path.Length - 1];
                d.Add(Joints.Head, d.pal.hair, CharacterKit.Tuft(end, end + new Vector3(0f, -0.06f * s, 0.01f), 0.02f * s));
            }
            // Hair showing at the back of the head under the helmet, and loose locks by the cheeks.
            d.Add(Joints.Head, d.pal.hair, MeshData.Ellipsoid(new Vector3(0f, cy - r * 0.1f, -r * 0.25f), new Vector3(r * 0.98f, r * 0.8f, r * 0.8f), 12, 8));
            foreach (float x in new[] { -1f, 1f })
                d.Add(Joints.Head, d.pal.hair, CharacterKit.Tuft(new Vector3(x * r * 0.92f, cy + r * 0.05f, r * 0.2f), new Vector3(x * r * 1.02f, cy - r * 0.55f, r * 0.35f), 0.022f * s));
        }
    }
}
