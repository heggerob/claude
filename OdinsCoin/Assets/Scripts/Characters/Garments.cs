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
        public static void FurBoots(Dresser d)
        {
            var f = d.fit;
            foreach (var joint in new[] { Joints.LeftLeg, Joints.RightLeg })
            {
                float sole = -f.hip, top = f.bootTop - f.hip, s = d.S;
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

        // ---------------------------------------------------------------- hair

        /// <summary>Two long braids from behind the ears, forward over the shoulders and down the chest, tied with leather.</summary>
        public static void LongBraids(Dresser d, float length)
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
