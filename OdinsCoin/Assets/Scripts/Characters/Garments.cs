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

        public void Add(string joint, Color color, MeshData mesh, bool outline = true) { model.Add(joint, color, mesh, outline, SurfaceOf(color)); }
        public void Add(string joint, Color color, MeshData mesh, bool outline, SurfaceKind surface) { model.Add(joint, color, mesh, outline, surface); }

        /// <summary>
        /// What a colour is drawn as, worked out from the palette slot it came from: fur and hair get strands, leather
        /// gets scuffs, cloth gets weave and pencil hatching. Shaded colours (<see cref="VikingModel.Shade"/>) and
        /// one-off colours are plain.
        /// </summary>
        public SurfaceKind SurfaceOf(Color c)
        {
            if (Same(c, pal.ink)) return SurfaceKind.Plain;
            if (Same(c, pal.skin)) return SurfaceKind.Skin;
            if (Same(c, pal.fur) || Same(c, pal.furShadow) || Same(c, pal.hair)) return SurfaceKind.Fur;
            if (Same(c, pal.leather) || Same(c, pal.leatherDark)) return SurfaceKind.Leather;
            if (Same(c, pal.metal) || Same(c, pal.brass)) return SurfaceKind.Metal;
            if (Same(c, pal.parchment)) return SurfaceKind.Paper;
            if (Same(c, pal.cloth) || Same(c, pal.clothDark) || Same(c, pal.cloth2) || Same(c, pal.accent) || Same(c, pal.emblem)) return SurfaceKind.Cloth;
            return SurfaceKind.Plain;
        }

        static bool Same(Color a, Color b)
        {
            return Mathf.Abs(a.r - b.r) < 0.002f && Mathf.Abs(a.g - b.g) < 0.002f && Mathf.Abs(a.b - b.b) < 0.002f;
        }

        /// <summary>
        /// A joint that swings on a spring, pivoting at <paramref name="pivot"/> (in the parent joint's space).
        /// Meshes added to it must be moved by -pivot first.
        /// </summary>
        public string Swing(string name, string parent, Vector3 pivot, SwingKind kind)
        {
            if (model.Find(name) == null)
            {
                model.AddJoint(name, parent, pivot);
                model.Swings.Add(new VikingModel.Swing { joint = name, kind = kind });
            }
            return name;
        }
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
                float r = 0.046f * s * Mathf.Sqrt(f.width);
                // A slim shaft, narrowest at the ankle, flaring a little at the top.
                d.Add(joint, d.pal.leatherDark, MeshData.Lathe(new[] {
                    new Vector2(r * 0.95f, sole), new Vector2(r * 1.08f, sole + 0.035f * s), new Vector2(r * 0.86f, sole + 0.1f * s),
                    new Vector2(r * 0.98f, sole + 0.2f * s), new Vector2(r * 1.12f, top) }, 12));
                // A long pointed foot that turns up a touch at the toe, and a heel.
                d.Add(joint, d.pal.leatherDark, MeshData.Tube(new[] {
                    new Vector3(0f, sole + 0.04f * s, -0.01f * s), new Vector3(0f, sole + 0.032f * s, 0.1f * s), new Vector3(0f, sole + 0.035f * s, 0.18f * s), new Vector3(0f, sole + 0.05f * s, 0.23f * s) },
                    new[] { r * 0.95f, r * 0.7f, r * 0.35f, 0.003f }, 10));
                d.Add(joint, VikingModel.Shade(d.pal.leatherDark, 0.7f), MeshData.Box(new Vector3(0f, sole + 0.012f * s, -0.025f * s), new Vector3(r * 1.5f, 0.024f * s, r * 1.2f)), false);
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
                // A flared gauntlet cuff with a pale stitched rim.
                d.Add(joint, d.pal.leatherDark, MeshData.Lathe(new[] { new Vector2(0.022f * s, wrist + 0.02f * s), new Vector2(0.042f * s, wrist + 0.1f * s), new Vector2(0.047f * s, wrist + 0.11f * s) }, 12));
                d.Add(joint, d.pal.leather, CharacterKit.Band(wrist + 0.1f * s, 0.008f * s, 0.048f * s, 1f, 12), false);
                // A big mitten-like fist, knuckles and a thumb: chunky, as the concept art draws the hands.
                d.Add(joint, d.pal.leatherDark, MeshData.Ellipsoid(new Vector3(0f, wrist - 0.028f * s, 0.008f * s), new Vector3(0.043f, 0.054f, 0.047f) * s, 12, 7));
                for (int k = -1; k <= 1; k++)
                    d.Add(joint, d.pal.leatherDark, MeshData.Ellipsoid(new Vector3(k * 0.022f * s, wrist - 0.06f * s, 0.03f * s), Vector3.one * 0.018f * s, 6, 4), false);
                d.Add(joint, d.pal.leatherDark, MeshData.Ellipsoid(new Vector3(0f, wrist - 0.01f * s, 0.042f * s), new Vector3(0.018f, 0.03f, 0.018f) * s, 6, 4));
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
        public static void ShoulderPelt(Dresser d, float size) { ShoulderPelt(d, size, 0f); }

        /// <summary>The pelt; <paramref name="side"/> +1 keeps it over the right shoulder only (-1 left, 0 both).</summary>
        public static void ShoulderPelt(Dresser d, float size, float side) { ShoulderPelt(d, size, side, 0.7f, 1f); }

        /// <summary>
        /// ...and <paramref name="frontOpen"/> (0–1) how far the front is lifted to show the chest,
        /// <paramref name="frontWidth"/> how wide that opening is (1 = narrow, 2+ = a wide V from shoulder to shoulder).
        /// </summary>
        public static void ShoulderPelt(Dresser d, float size, float side, float frontOpen, float frontWidth)
        {
            var f = d.fit;
            float s = d.S;
            float r = (f.shoulderX + 0.05f * s) * size;
            float bottom = f.shoulderY - 0.17f * s * size;
            // The pelt itself: a thick shaggy cape-let draped from the neck over the shoulders, its edge torn into
            // big uneven points, with a darker under-layer whose tips show below it.
            // It sits on the shoulders below the chin, rounding over them and hanging down.
            float top = f.shoulderY + 0.035f * s;
            // Short over the chest (so brooches, straps and the tunic show), long over the shoulders and back.
            float drop = f.shoulderY - bottom;
            System.Func<float, float> shortFront = a =>
            {
                float c = Mathf.Max(0f, Mathf.Cos(a));
                float raise = drop * frontOpen * Mathf.Pow(c, 2f / Mathf.Max(0.1f, frontWidth));
                // One-sided: pull the hem right up on the other shoulder and across the chest, so it hangs off one
                // shoulder only.
                if (side != 0f) raise = Mathf.Max(raise, (drop + 0.03f * s) * Mathf.Clamp01(-Mathf.Sin(a) * side * 1.5f + 0.2f + c * 2.5f));
                return raise;
            };
            float hug = Mathf.Clamp01(f.chestR * f.depth * 1.25f / (r * 0.8f));
            System.Func<float, float> hugFront = a => { float c = Mathf.Max(0f, Mathf.Cos(a)); return Mathf.Lerp(1f, hug, c * c); };
            d.Add(Joints.Body, d.pal.furShadow, CharacterKit.RaggedSkirt(top - 0.02f * s, f.chestR * 0.78f, bottom - 0.035f * s, r * 1.02f, 0.78f, 26, 0.07f * s * size, d.seed + 7, shortFront, hugFront));
            d.Add(Joints.Body, d.pal.fur, CharacterKit.RaggedSkirt(top, f.chestR * 0.62f, bottom, r, 0.8f, 30, 0.085f * s * size, d.seed + 6, shortFront, hugFront));
            // Lumpy clumps over the top so it doesn't read as a smooth shell, in both shades.
            var rng = new System.Random(d.seed + 8);
            for (int i = 0; i < 16; i++)
            {
                float a2 = side != 0f
                    ? Mathf.PI * (0.5f + 0.15f) * side + ((float)rng.NextDouble() - 0.5f) * 1.0f * side // on the one shoulder, towards the back
                    : Mathf.PI * 0.35f + (float)rng.NextDouble() * Mathf.PI * 1.3f;   // mostly on the shoulders and back
                float t = 0.35f + (float)rng.NextDouble() * 0.6f;
                float rad = Mathf.Lerp(f.chestR * 0.62f, r, t * t) + 0.01f * s;
                float y = Mathf.Lerp(top, bottom, t) + 0.012f * s;
                var p = new Vector3(Mathf.Sin(a2) * rad, y, Mathf.Cos(a2) * rad * 0.8f);
                float lump = (0.035f + (float)rng.NextDouble() * 0.03f) * s * size;
                d.Add(Joints.Body, i % 4 == 0 ? d.pal.furShadow : d.pal.fur, MeshData.Ellipsoid(p, new Vector3(lump * 1.3f, lump * 0.7f, lump), 7, 4));
            }
            // The pelt rises behind the neck like a mane, framing the head.
            d.Add(Joints.Body, d.pal.fur, MeshData.Ellipsoid(new Vector3(0f, f.shoulderY + 0.05f * s * size, -f.chestR * f.depth * 0.7f), new Vector3(r * 0.7f, 0.07f * s * size, 0.06f * s), 14, 7));
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
            // Where round the hips each pouch hangs, as a fraction of the way from the front to the side.
            float[] xs = { -0.72f, 0.78f, -0.95f, 0.42f };
            for (int i = 0; i < Mathf.Min(pouches, xs.Length); i++)
            {
                // Big, well-used pouches slung low off the belt on short straps, as in the concept art.
                float py = y - (0.1f + (i & 1) * 0.03f) * s;
                float pr = d.SkirtRadius(py) * 1.02f + 0.01f;
                float x = xs[i] * pr;
                float z = Mathf.Sqrt(Mathf.Max(0f, pr * pr - x * x)) * (d.hasSkirt ? d.skirtDepth : f.depth + 0.06f) + 0.022f;
                var size = new Vector3(0.078f + 0.012f * (i % 2), 0.1f, 0.052f) * s;
                d.Add(Joints.Body, d.pal.leatherDark, MeshData.Tube(new[] { new Vector3(x, y, z - 0.012f * s), new Vector3(x, py + 0.03f * s, z) }, new[] { 0.007f * s, 0.007f * s }, 4), false);
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
            // It swings from the belt.
            var pivot = grid[0, 1];
            string tab = d.Swing(Joints.Tabard, Joints.Body, pivot, SwingKind.Banner);
            d.Add(tab, d.pal.accent, CharacterKit.Sheet(grid, Vector3.forward, 0.012f * s).Moved(-pivot));
            // Embroidered border lines down both edges, and a triangle rune, riding on the cloth's surface.
            for (int r = 0; r < rows - 2; r++)
                foreach (float x in new[] { -1f, 1f })
                {
                    Vector3 a = grid[r, 1] + new Vector3(x * width * 0.36f * s, 0f, 0.008f * s) - pivot, b = grid[r + 1, 1] + new Vector3(x * width * 0.36f * s, 0f, 0.008f * s) - pivot;
                    d.Add(tab, d.pal.emblem, MeshData.Tube(new[] { a, b }, new[] { 0.0035f * s, 0.0035f * s }, 4), false);
                }
            float cy = top - length * 0.4f * s, tri = 0.03f * s;
            float ez = grid[Mathf.RoundToInt((rows - 1) * 0.4f), 1].z + 0.009f * s;
            Vector3 c0 = new Vector3(0f, cy + tri, ez) - pivot, c1 = new Vector3(-tri * 0.9f, cy - tri * 0.6f, ez) - pivot, c2 = new Vector3(tri * 0.9f, cy - tri * 0.6f, ez) - pivot;
            foreach (var edge in new[] { new[] { c0, c1 }, new[] { c1, c2 }, new[] { c2, c0 } })
                d.Add(tab, d.pal.emblem, MeshData.Tube(edge, new[] { 0.004f * s, 0.004f * s }, 4), false);
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
                new Vector2(r * 1.12f, brim - 0.12f * r), new Vector2(r * 1.15f, brim + 0.15f * r), new Vector2(r * 1.12f, brim + 0.5f * r),
                new Vector2(r * 0.92f, brim + 0.86f * r), new Vector2(r * 0.52f, brim + 1.08f * r), new Vector2(0.01f, brim + 1.15f * r) }, 20)
                .Transformed(new Vector3(0f, 0f, -0.02f * r), Quaternion.identity, Vector3.one));
            d.Add(Joints.Head, VikingModel.Shade(steel, 0.72f), CharacterKit.Band(brim - 0.08f * r, 0.035f * d.S, r * 1.17f, 1f, 20)
                .Transformed(new Vector3(0f, 0f, -0.02f * r), Quaternion.identity, Vector3.one));
            // A broad riveted ridge from brow to nape, a paler strip down the front (the concept's shine).
            var ridge = new Vector3[7];
            for (int i = 0; i < ridge.Length; i++)
            {
                float a = Mathf.Lerp(-0.1f, Mathf.PI + 0.1f, i / (float)(ridge.Length - 1));
                ridge[i] = new Vector3(0f, brim + Mathf.Sin(a) * r * 1.13f, Mathf.Cos(a) * r * 1.14f - 0.02f * r);
            }
            var rr = new float[ridge.Length];
            for (int i = 0; i < rr.Length; i++) rr[i] = 0.022f * d.S;
            // Pale polished steel, so the ridge and nose guard read as in the concept art.
            var shine = Color.Lerp(steel, new Color(0.82f, 0.82f, 0.8f), 0.55f);
            d.Add(Joints.Head, shine, MeshData.Tube(ridge, rr, 6).Transformed(Vector3.zero, Quaternion.identity, new Vector3(1.8f, 1f, 1f)));
            // Nose guard coming down between the eyes, a little wider at the top.
            d.Add(Joints.Head, shine, MeshData.Extrude(new[] { new Vector2(-0.022f, 0.02f), new Vector2(0.022f, 0.02f), new Vector2(0.014f, -0.46f), new Vector2(-0.014f, -0.46f) }, 0.016f)
                .Transformed(new Vector3(0f, brim - 0.02f * r, r * 1.02f), Quaternion.identity, new Vector3(d.S, r, 1f)));
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
            d.Add(Joints.Body, d.pal.brass, CharacterKit.Band(hem + 0.04f * s, 0.05f * s, d.SkirtRadius(hem + 0.04f * s) + 0.004f, d.skirtDepth, 22), false);
            // Runes worked into the gold border, round the front and sides.
            var runeRow = new Vector3[11];
            for (int i = 0; i < runeRow.Length; i++)
            {
                float a = Mathf.Lerp(-1.9f, 1.9f, i / (float)(runeRow.Length - 1));
                float ry = hem + 0.04f * s, rr = d.SkirtRadius(ry) + 0.012f;
                runeRow[i] = new Vector3(Mathf.Sin(a) * rr, ry, Mathf.Cos(a) * rr * d.skirtDepth);
            }
            d.Add(Joints.Body, d.pal.clothDark, CharacterKit.RuneBand(runeRow, p => new Vector3(p.x, 0f, p.z).normalized, 0.04f * s, 0.0045f * s, d.seed + 33), false);
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
            d.Add(Joints.Body, VikingModel.Shade(d.pal.cloth, 0.8f), CharacterKit.Sheet(grid, Vector3.forward, 0.01f * s), true, SurfaceKind.Cloth);
            foreach (int c in new[] { 0, 2 })
            {
                var edge = new Vector3[rows];
                for (int r = 0; r < rows; r++) edge[r] = grid[r, c] + new Vector3(0f, 0f, 0.008f * s);
                var radii = new float[rows];
                for (int r = 0; r < rows; r++) radii[r] = 0.007f * s;
                d.Add(Joints.Body, d.pal.brass, MeshData.Tube(edge, radii, 5), false);
            }
            // A broad gold border across the bottom of the panel, with a row of small studs above it.
            var foot = new Vector3[3];
            for (int c = 0; c < 3; c++) foot[c] = grid[rows - 1, c] + new Vector3(0f, 0.03f * s, 0.009f * s);
            d.Add(Joints.Body, d.pal.brass, MeshData.Tube(foot, new[] { 0.016f * s, 0.016f * s, 0.016f * s }, 5), false);
            // A row of gold runes across the panel, just above the border.
            var panelRow = new Vector3[5];
            for (int k = 0; k < panelRow.Length; k++)
                panelRow[k] = Vector3.Lerp(grid[rows - 2, 0], grid[rows - 2, 2], Mathf.Lerp(-0.1f, 1.1f, k / 4f)) + new Vector3(0f, 0.01f * s, 0.008f * s);
            d.Add(Joints.Body, d.pal.brass, CharacterKit.RuneBand(panelRow, p => Vector3.forward, 0.045f * s, 0.005f * s, d.seed + 34), false);
        }

        /// <summary>Long sleeves down to the wrists, ending in fur cuffs, with gold-strapped bracers.</summary>
        /// <summary>Bare arms with leather bracers on the forearms, laced, and a fur cuff at the elbow end.</summary>
        public static void Bracers(Dresser d)
        {
            var f = d.fit;
            float s = d.S;
            float r = 0.05f * s * Mathf.Sqrt(f.width);
            foreach (var fore in new[] { Joints.LeftForearm, Joints.RightForearm })
            {
                d.Add(fore, d.pal.leatherDark, MeshData.Lathe(new[] { new Vector2(r * 0.6f, -f.foreArm + 0.02f * s), new Vector2(r * 0.75f, -f.foreArm * 0.35f) }, 10));
                d.Add(fore, d.pal.leather, CharacterKit.Spiral(-f.foreArm + 0.03f * s, -f.foreArm * 0.38f, r * 0.77f, 1.2f, 0f, 0.0045f * s), false);
                d.Add(fore, d.pal.leather, CharacterKit.Spiral(-f.foreArm + 0.03f * s, -f.foreArm * 0.38f, r * 0.77f, -1.2f, Mathf.PI, 0.0045f * s), false);
                d.Add(fore, d.pal.fur, CharacterKit.FurRing(new Vector3(0f, -f.foreArm * 0.35f, 0f), r * 0.78f, 1f, 0.026f * s, 9, 0.035f * s, d.seed + 7 + fore.Length));
            }
        }

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
        public static void BigCape(Dresser d, float length, float flare) { BigCape(d, length, flare, true); }

        /// <summary>The big cape; <paramref name="drapes"/> adds the parts falling forward over the shoulders.</summary>
        public static void BigCape(Dresser d, float length, float flare, bool drapes)
        {
            var f = d.fit;
            float s = d.S;
            var top = new Vector3(0f, f.shoulderY + 0.02f * s, -f.chestR * f.depth * 0.3f);
            string cape = d.Swing(Joints.Cape, Joints.Body, top, SwingKind.Cape);
            d.Add(cape, d.pal.accent, CharacterKit.Cape(top, f.shoulderX * 2.6f, f.shoulderX * 2f * flare, length * s, f.chestR * 1.3f, 0.17f * s, d.seed + 41, 0.02f * s).Moved(-top));
            // Front drapes falling over each shoulder, open at the chest.
            if (drapes) foreach (float x in new[] { -1f, 1f })
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
            // Big domed discs pinning the cloak, standing out in front of the pelt and the braids as in the concept art.
            float y = f.shoulderY - 0.09f * s, z = f.chestR * f.depth + 0.07f * s, br = 0.062f * s;
            foreach (float x in new[] { -1f, 1f })
            {
                var at = new Vector3(x * f.chestR * 0.8f, y, z);
                d.Add(Joints.Body, d.pal.brass, MeshData.Lathe(new[] { new Vector2(br * 0.96f, -0.01f * s), new Vector2(br, 0f), new Vector2(br * 0.86f, 0.014f * s), new Vector2(0.001f, 0.018f * s) }, 20)
                    .Transformed(at, Quaternion.Euler(90f, 0f, 0f), Vector3.one));
                // Knotwork: an outer and an inner ring, a cross and four bosses.
                var dark = VikingModel.Shade(d.pal.brass, 0.6f);
                foreach (float rr in new[] { 0.78f, 0.42f })
                    d.Add(Joints.Body, dark, MeshData.Lathe(new[] { new Vector2(br * rr, 0f), new Vector2(br * rr, 0.004f * s) }, 18).Transformed(at + new Vector3(0f, 0f, 0.013f * s * (1.4f - rr)), Quaternion.Euler(90f, 0f, 0f), Vector3.one), false);
                d.Add(Joints.Body, dark, MeshData.Box(at + new Vector3(0f, 0f, 0.016f * s), new Vector3(br * 1.3f, 0.007f * s, 0.004f * s)), false);
                d.Add(Joints.Body, dark, MeshData.Box(at + new Vector3(0f, 0f, 0.016f * s), new Vector3(0.007f * s, br * 1.3f, 0.004f * s)), false);
                for (int k = 0; k < 4; k++)
                {
                    float a = (k + 0.5f) * Mathf.PI * 0.5f;
                    d.Add(Joints.Body, d.pal.brass, MeshData.Ellipsoid(at + new Vector3(Mathf.Cos(a) * br * 0.6f, Mathf.Sin(a) * br * 0.6f, 0.013f * s), Vector3.one * 0.008f * s, 5, 3), false);
                }
                // Strap from the brooch across the chest to the opposite hip, with a ring halfway.
                var end = new Vector3(-x * f.waistR * 0.8f, f.waist + 0.03f * s, f.waistR * f.depth + 0.03f);
                var mid = Vector3.Lerp(at, end, 0.5f) + new Vector3(0f, 0f, 0.02f * s);
                d.Add(Joints.Body, d.pal.leather, MeshData.Tube(new[] { at + new Vector3(0f, -0.03f * s, -0.01f * s), mid, end }, new[] { 0.014f * s, 0.015f * s, 0.014f * s }, 5), false);
                d.Add(Joints.Body, d.pal.brass, MeshData.Lathe(new[] { new Vector2(0.02f * s, -0.005f * s), new Vector2(0.02f * s, 0.005f * s) }, 10).Transformed(mid + new Vector3(0f, 0f, 0.016f * s), Quaternion.Euler(90f, 0f, 0f), Vector3.one), false);
            }
        }

        /// <summary>A belt hung with gold rings and dangling straps.</summary>
        public static void RingBelt(Dresser d)
        {
            var f = d.fit;
            float s = d.S;
            // A wide brown belt standing out over the coat, and a thinner dark one slung below it.
            float y = f.waist, r = f.waistR * 1.1f + 0.008f * s, depth = f.depth + 0.1f;
            d.Add(Joints.Body, d.pal.leather, CharacterKit.Band(y, 0.075f * s, r, depth));
            d.Add(Joints.Body, d.pal.leatherDark, CharacterKit.Band(y - 0.075f * s, 0.032f * s, d.SkirtRadius(y - 0.075f * s) * 1.05f, depth + 0.02f));
            // Big open gold rings on the belt, as in the concept art, the side ones hung with straps ending in rings.
            for (int i = -2; i <= 2; i++)
            {
                float a = i * 0.42f;
                var outward = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
                var at = new Vector3(Mathf.Sin(a) * r, y - (i == 0 ? 0f : 0.012f) * s, Mathf.Cos(a) * r * depth + 0.014f);
                float size = i == 0 ? 0.042f : 0.03f;
                d.Add(Joints.Body, d.pal.brass, Ring(at, outward, size * s, 0.008f * s), false);
                if (i != 0)
                {
                    var hang = at + new Vector3(0f, -0.14f * s, 0.02f * s);
                    d.Add(Joints.Body, d.pal.leatherDark, MeshData.Tube(new[] { at + new Vector3(0f, -size * s, 0f), hang }, new[] { 0.009f * s, 0.008f * s }, 5), false);
                    d.Add(Joints.Body, d.pal.brass, Ring(hang + new Vector3(0f, -0.018f * s, 0f), outward, 0.02f * s, 0.006f * s), false);
                }
            }
        }

        /// <summary>An open metal ring (a torus) centred at <paramref name="at"/>, its face turned towards <paramref name="facing"/>.</summary>
        public static MeshData Ring(Vector3 at, Vector3 facing, float radius, float thickness)
        {
            var rot = Quaternion.LookRotation(facing);
            var path = new Vector3[17];
            var radii = new float[17];
            for (int i = 0; i < path.Length; i++)
            {
                float t = i / 16f * Mathf.PI * 2f;
                path[i] = at + rot * new Vector3(Mathf.Cos(t) * radius, Mathf.Sin(t) * radius, 0f);
                radii[i] = thickness;
            }
            return MeshData.Tube(path, radii, 5);
        }

        /// <summary>The jarl's tall black crown-helm: flaring up from a low brim, gold-trimmed, with curved horns and a rune plate.</summary>
        public static void JarlCrown(Dresser d)
        {
            var f = d.fit;
            float r = f.headR, cy = HeroModel.HeadCentre(f), s = d.S;
            // Low on the brow, just above the eyes, like the concept art's crown-helm.
            float brim = cy + 0.02f * r;
            var black = d.pal.clothDark;
            var scale = new Vector3(1.12f, 1.22f, 1.02f);
            var offset = new Vector3(0f, 0f, -0.02f * r);
            // A rounded black helm-crown: straight sides, then a full dome.
            d.Add(Joints.Head, black, MeshData.Lathe(new[] {
                new Vector2(r * 1.12f, brim - 0.08f * r), new Vector2(r * 1.16f, brim + 0.2f * r), new Vector2(r * 1.14f, brim + 0.5f * r),
                new Vector2(r * 0.98f, brim + 0.82f * r), new Vector2(r * 0.62f, brim + 1.04f * r), new Vector2(0.01f, brim + 1.12f * r) }, 24)
                .Transformed(offset, Quaternion.identity, scale));
            // A gold band round the brow, studded.
            d.Add(Joints.Head, d.pal.brass, CharacterKit.Band(brim, 0.045f * s, r * 1.18f, 0.98f, 24).Transformed(offset, Quaternion.identity, scale));
            for (int i = -4; i <= 4; i++)
            {
                float a = i * 0.34f;
                d.Add(Joints.Head, black, MeshData.Ellipsoid(offset + Vector3.Scale(new Vector3(Mathf.Sin(a) * r * 1.21f, brim, Mathf.Cos(a) * r * 1.21f), scale), Vector3.one * 0.008f * s, 5, 3), false);
            }
            // Riveted gold ribs running up over the dome from the band, as in the concept art.
            var ribProfile = new[] { new Vector2(1.17f, 0.05f), new Vector2(1.17f, 0.2f), new Vector2(1.15f, 0.5f), new Vector2(1.02f, 0.78f), new Vector2(0.84f, 0.95f) };
            foreach (float a in new[] { -0.55f, 0.55f, -1.65f, 1.65f, 2.7f, -2.7f })
            {
                var rib = new Vector3[ribProfile.Length];
                var ribR = new float[ribProfile.Length];
                for (int k = 0; k < rib.Length; k++)
                {
                    var p = ribProfile[k];
                    rib[k] = offset + Vector3.Scale(new Vector3(Mathf.Sin(a) * p.x * r, brim + p.y * r, Mathf.Cos(a) * p.x * r), scale);
                    ribR[k] = 0.008f * s * (k == rib.Length - 1 ? 0.6f : 1f);
                }
                d.Add(Joints.Head, d.pal.brass, MeshData.Tube(rib, ribR, 5), false);
                for (int k = 1; k < rib.Length - 1; k++)
                    d.Add(Joints.Head, black, MeshData.Ellipsoid(rib[k] + (rib[k] - offset - new Vector3(0f, brim, 0f)).normalized * 0.014f * s, Vector3.one * 0.006f * s, 4, 3), false);
            }
            // Four curved horns rising from the dome, black with gold edges: big ones at the sides curving out,
            // smaller ones at the front.
            foreach (var h in new[] { new Vector2(-1.25f, 1.2f), new Vector2(1.25f, 1.2f), new Vector2(-0.62f, 0.95f), new Vector2(0.62f, 0.95f) })
            {
                float a = h.x, len = h.y;
                var dir = new Vector3(Mathf.Sin(a) * scale.x, 0f, Mathf.Cos(a) * scale.z);
                var root = offset + new Vector3(0f, brim + 0.55f * r, 0f) + dir * r * 1.06f;
                var path = new[] {
                    root, root + dir * r * 0.32f * len + Vector3.up * r * 0.22f * len,
                    root + dir * r * 0.46f * len + Vector3.up * r * 0.6f * len, root + dir * r * 0.36f * len + Vector3.up * r * 0.95f * len };
                d.Add(Joints.Head, black, MeshData.Tube(path, new[] { 0.045f * s, 0.034f * s, 0.02f * s, 0.003f * s }, 7));
                var edge = new Vector3[path.Length - 1];
                for (int k = 1; k < path.Length; k++) edge[k - 1] = path[k] + dir * 0.016f * s * (1f - k * 0.25f) + Vector3.up * 0.004f * s;
                d.Add(Joints.Head, d.pal.brass, MeshData.Tube(edge, new[] { 0.014f * s, 0.01f * s, 0.003f * s }, 5), false);
            }
            // A tall gold crest on the front, rising from the band, with a rune.
            float pz = r * 1.18f * scale.z + 0.012f * s;
            var plate = new[] {
                new Vector2(-0.06f, 0.0f), new Vector2(0.06f, 0.0f), new Vector2(0.065f, 0.13f), new Vector2(0.04f, 0.17f),
                new Vector2(0.02f, 0.14f), new Vector2(0f, 0.21f), new Vector2(-0.02f, 0.14f), new Vector2(-0.04f, 0.17f), new Vector2(-0.065f, 0.13f) };
            // (A slimmer crest, so the black dome shows round it as in the concept art.)
            const float cs = 1f;
            d.Add(Joints.Head, d.pal.brass, MeshData.Extrude(plate, 0.018f).Transformed(new Vector3(0f, brim + 0.12f * r, pz), Quaternion.Euler(-18f, 90f, 0f), Vector3.one * s * cs));
            var rz = pz + 0.014f * s;
            float ry = brim + 0.12f * r;
            d.Add(Joints.Head, black, MeshData.Box(new Vector3(0f, ry + 0.085f * s * cs, rz), new Vector3(0.009f, 0.09f, 0.004f) * s * cs), false);
            d.Add(Joints.Head, black, MeshData.Box(new Vector3(0.017f * s * cs, ry + 0.11f * s * cs, rz), new Vector3(0.034f, 0.008f, 0.004f) * s * cs), false);
            d.Add(Joints.Head, black, MeshData.Box(new Vector3(-0.012f * s * cs, ry + 0.065f * s * cs, rz), new Vector3(0.026f, 0.008f, 0.004f) * s * cs), false);
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
                float r = 0.048f * s * Mathf.Sqrt(f.width);
                // A slim shaft, pinched at the ankle, opening into a folded-down cuff.
                d.Add(joint, d.pal.leatherDark, MeshData.Lathe(new[] {
                    new Vector2(r * 0.95f, sole), new Vector2(r * 1.08f, sole + 0.035f * s), new Vector2(r * 0.86f, sole + 0.1f * s),
                    new Vector2(r * 1.0f, top - 0.03f * s), new Vector2(r * 1.2f, top) }, 12));
                // A long pointed toe turning up a little, and a heel.
                d.Add(joint, d.pal.leatherDark, MeshData.Tube(new[] {
                    new Vector3(0f, sole + 0.04f * s, -0.01f * s), new Vector3(0f, sole + 0.032f * s, 0.1f * s), new Vector3(0f, sole + 0.035f * s, 0.18f * s), new Vector3(0f, sole + 0.05f * s, 0.23f * s) },
                    new[] { r * 0.95f, r * 0.7f, r * 0.35f, 0.003f }, 10));
                d.Add(joint, VikingModel.Shade(d.pal.leatherDark, 0.7f), MeshData.Box(new Vector3(0f, sole + 0.012f * s, -0.025f * s), new Vector3(r * 1.5f, 0.024f * s, r * 1.2f)), false);
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
            float tw = f.shoulderX * 2.4f, bw = f.shoulderX * 3.8f, len = backLength * s, wrap = f.chestR * 1.4f;
            string cape = d.Swing(Joints.Cape, Joints.Body, top, SwingKind.Cape);
            d.Add(cape, d.pal.accent, CharacterKit.Cape(top, tw, bw, len, wrap, 0.08f * s, d.seed + 51, 0.016f * s).Moved(-top));
            // Embroidered border across the back, above the torn hem.
            var row = new Vector3[15];
            for (int i = 0; i < row.Length; i++)
                row[i] = CharacterKit.CapePoint(top, tw, bw, len, wrap, Mathf.Lerp(-0.9f, 0.9f, i / (float)(row.Length - 1)), 0.8f) + new Vector3(0f, 0f, -0.016f * s) - top;
            d.Add(cape, d.pal.emblem, CharacterKit.ZigZag(row, Vector3.up, 0.018f * s, 0.004f * s), false);
            for (int i = 0; i < row.Length; i++) row[i] += Vector3.down * 0.05f * s;
            d.Add(cape, d.pal.emblem, CharacterKit.ZigZag(row, Vector3.up, 0.012f * s, 0.0035f * s), false);
            // A line of runes between the two zig-zags.
            for (int i = 0; i < row.Length; i++) row[i] += Vector3.up * 0.026f * s;
            d.Add(cape, d.pal.emblem, CharacterKit.RuneBand(row, p => Vector3.back, 0.022f * s, 0.0026f * s, d.seed + 55), false);

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
                    float ang = Mathf.Lerp(0.55f, 2.1f, u) + v * 0.2f;
                    // Flaring out as it falls, most at the outer edge, like heavy wool swinging off the hip.
                    float rad = Mathf.Max(f.TorsoRadius(Mathf.Max(y, f.waist)), d.SkirtRadius(y)) + 0.035f * s + v * (0.05f + 0.12f * u) * s;
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
            // Broad pale woven borders, down the front edge and across the hem, patterned in the cloak's own dark
            // blue, like the concept art.
            var pattern = VikingModel.Shade(d.pal.accent, 0.55f);
            float border = 0.05f * s;
            var hemBand = new Vector3[trim.Length, 2];
            var hemMid = new Vector3[trim.Length];
            for (int i = 0; i < trim.Length; i++)
            {
                hemBand[i, 0] = trim[i] + Vector3.down * 0.01f * s;
                hemBand[i, 1] = trim[i] + Vector3.up * border;
                hemMid[i] = trim[i] + Vector3.up * border * 0.45f + new Vector3(trim[i].x, 0f, trim[i].z).normalized * 0.006f * s;
            }
            d.Add(Joints.Body, d.pal.emblem, CharacterKit.Sheet(hemBand, new Vector3(1f, 0f, 0.6f), 0.004f * s), false);
            d.Add(Joints.Body, pattern, CharacterKit.ZigZag(hemMid, Vector3.up, 0.03f * s, 0.004f * s), false);
            var frontBand = new Vector3[rows - 1, 2];
            var frontMid = new Vector3[rows - 1];
            for (int r = 0; r < rows - 1; r++)
            {
                var p = grid[r, 0];
                var lift = new Vector3(p.x, 0f, p.z).normalized * 0.012f * s;
                var into = (grid[r, 1] - p).normalized;
                frontBand[r, 0] = p + lift;
                frontBand[r, 1] = p + into * border + lift;
                frontMid[r] = p + into * border * 0.5f + lift * 1.5f;
            }
            d.Add(Joints.Body, d.pal.emblem, CharacterKit.Sheet(frontBand, new Vector3(1f, 0f, 0.6f), 0.004f * s), false);
            d.Add(Joints.Body, pattern, CharacterKit.ZigZag(frontMid, new Vector3(1f, 0f, 0.6f).normalized, 0.03f * s, 0.004f * s), false);
            // A thin pale line down the far edge too.
            foreach (int c in new[] { cols - 1 })
            {
                var edge = new Vector3[rows - 2];
                for (int r = 1; r < rows - 1; r++)
                {
                    var p = grid[r, c];
                    int inner = c == 0 ? 1 : cols - 2;
                    // Just inside the edge, lifted off the cloth towards the outside.
                    edge[r - 1] = Vector3.Lerp(p, grid[r, inner], 0.25f) + new Vector3(p.x, 0f, p.z).normalized * 0.012f * s;
                }
                d.Add(Joints.Body, d.pal.emblem, MeshData.Tube(edge, Fill(edge.Length, 0.009f * s), 5), false);
                d.Add(Joints.Body, d.pal.emblem, CharacterKit.ZigZag(edge, new Vector3(1f, 0f, 0.6f).normalized, 0.022f * s, 0.0055f * s), false);
            }
            // Fur along the shoulder where the drape is thrown over.
            d.Add(Joints.Body, d.pal.fur, CharacterKit.FurRing(new Vector3(f.shoulderX * 0.55f, f.shoulderY + 0.02f * s, 0f), f.shoulderX * 0.55f, 0.9f, 0.05f * s, 12, 0.08f * s, d.seed + 53, 0.9f));
        }

        static float[] Fill(int n, float v) { var a = new float[n]; for (int i = 0; i < n; i++) a[i] = v; return a; }

        /// <summary>A fur collar round the neck and shoulders, smaller than a full pelt.</summary>
        public static void FurCollar(Dresser d)
        {
            var f = d.fit;
            float s = d.S;
            d.Add(Joints.Body, d.pal.fur, CharacterKit.FurRing(new Vector3(0f, f.shoulderY - 0.005f * s, 0f), f.chestR * 0.95f, f.depth + 0.05f, 0.055f * s, 16, 0.07f * s, d.seed + 54, 0.8f));
        }

        /// <summary>An under-skirt in the accent colour peeking out below the skirt, its hem trimmed with fur tufts.</summary>
        public static void Underskirt(Dresser d, float below)
        {
            var f = d.fit;
            float s = d.S;
            if (!d.hasSkirt) return;
            float top = d.skirtBottom + 0.15f * s, hem = d.skirtBottom - below * s;
            // Stays inside the skirt; only its hem shows below.
            d.Add(Joints.Body, d.pal.accent, CharacterKit.RaggedSkirt(top, d.SkirtRadius(top) * 0.9f, hem, d.skirtBottomR * 0.93f, d.skirtDepth - 0.03f, 20, 0.03f * s, d.seed + 91));
            d.Add(Joints.Body, d.pal.fur, CharacterKit.FurRing(new Vector3(0f, hem + 0.012f * s, 0f), d.skirtBottomR * 0.92f, d.skirtDepth - 0.03f, 0.022f * s, 18, 0.03f * s, d.seed + 92, 1f));
        }

        /// <summary>A soft cloth bandana tied over the head, a studded leather band across the brow, a rune on the front,
        /// the knot's two tails hanging at the back, and a fringe of hair showing at the temples.</summary>
        public static void Bandana(Dresser d)
        {
            var f = d.fit;
            float r = f.headR, cy = HeroModel.HeadCentre(f), s = d.S;
            float brim = cy + 0.2f * r;
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
            // The knot at the side behind the ear, and two tails flaring out and down past the chin.
            var knot = new Vector3(r * 0.9f, brim + 0.05f * r, -r * 0.45f);
            d.Add(Joints.Head, d.pal.accent, MeshData.Ellipsoid(knot, new Vector3(0.035f, 0.035f, 0.035f) * s, 8, 5));
            for (int t = 0; t < 2; t++)
            {
                var grid = new Vector3[4, 2];
                for (int r2 = 0; r2 < 4; r2++)
                    for (int c = 0; c < 2; c++)
                    {
                        float v = r2 / 3f;
                        grid[r2, c] = knot + new Vector3((0.01f + v * (0.035f + t * 0.03f)) * s, -0.01f * s - v * (0.15f + t * 0.05f) * s,
                            (-0.01f - t * 0.035f) * s + (c * 0.038f - 0.019f) * s * (1f - 0.3f * v));
                    }
                d.Add(Joints.Head, t == 0 ? d.pal.accent : VikingModel.Shade(d.pal.accent, 0.8f), CharacterKit.Sheet(grid, Vector3.right, 0.01f * s));
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
            float y = f.waist - 0.12f * s;
            // Out at the right hip, in front of any cloak falling down that side.
            float ang = 1.2f, rad = d.SkirtRadius(y) + 0.1f * s;
            var at = new Vector3(Mathf.Sin(ang) * rad, y, Mathf.Cos(ang) * rad * (d.hasSkirt ? d.skirtDepth : f.depth));
            // The case: a leather tube hanging at a slant.
            d.Add(Joints.Body, d.pal.leather, MeshData.Lathe(new[] { new Vector2(0.03f * s, -0.12f * s), new Vector2(0.032f * s, 0.1f * s) }, 10)
                .Transformed(at + new Vector3(0.02f * s, -0.05f * s, 0.03f * s), Quaternion.Euler(0f, 0f, 25f), Vector3.one));
            d.Add(Joints.Body, d.pal.brass, MeshData.Lathe(new[] { new Vector2(0.035f * s, -0.01f * s), new Vector2(0.035f * s, 0.01f * s) }, 10)
                .Transformed(at + new Vector3(0.02f * s, -0.05f * s, 0.03f * s), Quaternion.Euler(0f, 0f, 25f), Vector3.one), false);
            // Two loose rolls of parchment tucked in the belt, lying crosswise.
            for (int i = 0; i < 2; i++)
            {
                var c = at + new Vector3(-0.01f * s, (0.07f - i * 0.06f) * s, (0.04f + i * 0.02f) * s);
                d.Add(Joints.Body, d.pal.parchment, MeshData.Lathe(new[] { new Vector2(0.032f * s, -0.1f * s), new Vector2(0.032f * s, 0.1f * s) }, 12)
                    .Transformed(c, Quaternion.Euler(0f, 30f + i * 20f, 80f - i * 15f), Vector3.one));
                // The rolled end showing its spiral, and a cord round the middle.
                d.Add(Joints.Body, VikingModel.Shade(d.pal.parchment, 0.75f), MeshData.Lathe(new[] { new Vector2(0.034f * s, -0.01f * s), new Vector2(0.034f * s, 0.01f * s) }, 12)
                    .Transformed(c, Quaternion.Euler(0f, 30f + i * 20f, 80f - i * 15f), Vector3.one), false);
                d.Add(Joints.Body, VikingModel.Shade(d.pal.parchment, 0.55f), MeshData.Lathe(new[] { new Vector2(0.018f * s, 0.098f * s), new Vector2(0.018f * s, 0.102f * s) }, 10)
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
                        float x = side * 0.02f * s + Mathf.Lerp(0f, 0.16f, v) * s * (side < 0f ? 1f : 0.5f) + (c - 0.5f) * 0.065f * s;
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
            // Below the belt the jerkin is split up the front: how far apart its edges are at a height.
            System.Func<float, float> split = y => y >= f.waist - 0.02f * s || !d.hasSkirt ? 0.045f * s
                : Mathf.Lerp(0.045f * s, 0.075f * s, Mathf.InverseLerp(f.waist, d.skirtBottom, y));
            foreach (float x in new[] { -1f, 1f })
                for (int i = 0; i < 9; i++)
                {
                    float y = Mathf.Lerp(f.chest - 0.04f * s, d.hasSkirt ? d.skirtBottom + 0.03f * s : f.waist, i / 8f);
                    float r = y > f.waist ? f.TorsoRadius(y) * f.depth : d.SkirtRadius(y) * d.skirtDepth;
                    d.Add(Joints.Body, d.pal.brass, MeshData.Ellipsoid(new Vector3(x * split(y), y, r + 0.008f * s), Vector3.one * 0.008f * s, 5, 3), false);
                }
            if (!d.hasSkirt) return;
            // The split itself: the dark under-tunic showing between the jerkin's edges, which are bound in lighter leather.
            const int rows = 6;
            var gap = new Vector3[rows, 2];
            for (int r = 0; r < rows; r++)
            {
                float y = Mathf.Lerp(f.waist - 0.03f * s, d.skirtBottom - 0.005f * s, r / (float)(rows - 1));
                float z = d.SkirtRadius(y) * d.skirtDepth + 0.004f;
                float w = split(y) - 0.012f * s;
                gap[r, 0] = new Vector3(-w, y, z);
                gap[r, 1] = new Vector3(w, y, z);
            }
            d.Add(Joints.Body, d.pal.clothDark, CharacterKit.Sheet(gap, Vector3.forward, 0.006f * s), false);
            // Broad bands of pale leather binding the jerkin's front edges from the chest down, and its hem, with the
            // studs sitting on them, as in the concept art.
            var binding = VikingModel.Shade(d.pal.leather, 1.45f);
            const int brows = 10;
            foreach (float x in new[] { -1f, 1f })
            {
                var band = new Vector3[brows, 2];
                for (int r = 0; r < brows; r++)
                {
                    float y = Mathf.Lerp(f.chest - 0.05f * s, d.skirtBottom - 0.005f * s, r / (float)(brows - 1));
                    float z = (y > f.waist ? f.TorsoRadius(y) * f.depth : d.SkirtRadius(y) * d.skirtDepth) + 0.005f * s;
                    band[r, 0] = new Vector3(x * (split(y) - 0.012f * s), y, z);
                    band[r, 1] = new Vector3(x * (split(y) + 0.014f * s), y, z - 0.002f * s);
                }
                d.Add(Joints.Body, binding, CharacterKit.Sheet(band, Vector3.forward, 0.006f * s), false, SurfaceKind.Leather);
            }
            d.Add(Joints.Body, binding, CharacterKit.Band(d.skirtBottom + 0.035f * s, 0.035f * s, d.SkirtRadius(d.skirtBottom + 0.035f * s) + 0.003f, d.skirtDepth, 24), false, SurfaceKind.Leather);
            for (int i = 0; i < 18; i++)
            {
                float a = i / 18f * Mathf.PI * 2f;
                float y = d.skirtBottom + 0.035f * s, r = d.SkirtRadius(y) + 0.006f;
                d.Add(Joints.Body, d.pal.brass, MeshData.Ellipsoid(new Vector3(Mathf.Sin(a) * r, y, Mathf.Cos(a) * r * d.skirtDepth), Vector3.one * 0.0075f * s, 5, 3), false);
            }
        }

        /// <summary>Dark wool trousers from the hip down into the boots, over the stick legs.</summary>
        public static void Trousers(Dresser d, float bootExtra)
        {
            var f = d.fit;
            float s = d.S;
            float r = 0.034f * s * Mathf.Sqrt(f.width);
            float bottom = f.bootTop + bootExtra * s - f.hip - 0.02f * s;
            foreach (var leg in new[] { Joints.LeftLeg, Joints.RightLeg })
                d.Add(leg, d.pal.clothDark, MeshData.Lathe(new[] {
                    new Vector2(r * 0.8f, bottom), new Vector2(r * 0.85f, (bottom - 0.02f * s) * 0.5f), new Vector2(r, -0.05f * s), new Vector2(r * 1.05f, 0.02f * s) }, 10));
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
            d.skirtTop = f.waist; d.skirtTopR = f.waistR * 0.98f; d.skirtBottom = 0.06f * s; d.skirtBottomR = f.hipR * 2.4f; d.skirtDepth = f.depth + 0.08f;
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
                // Open down the front below the shoulders, like a cloak, so the stole, belt and charms show.
                float gap = t == 0 ? 0f : t == tops.Length - 1 ? 0.3f : 0.5f;
                d.Add(Joints.Body, colour, CharacterKit.Flaps(tops[t], len, 14 + t * 2, 0.95f, depth, surface, 0.01f + t * 0.004f, d.seed + 62 + t, 0.01f * s, gap));
                // A few faded, paler strips caught between the dark ones, like weathered feathers and old wool.
                if (t > 0)
                    d.Add(Joints.Body, VikingModel.Shade(d.pal.cloth, 1.7f), CharacterKit.Flaps(tops[t] - 0.04f * s, len * 0.8f, 4 + t, 0.25f, depth, surface, 0.014f + t * 0.004f, d.seed + 72 + t, 0.008f * s), false);
            }
            // Feathers bristling round the shoulders.
            d.Add(Joints.Body, d.pal.clothDark, CharacterKit.FurRing(new Vector3(0f, f.shoulderY - 0.01f * s, 0f), f.shoulderX + 0.02f * s, 0.8f, 0.05f * s, 24, 0.1f * s, d.seed + 67, 1.4f));
            // Long ragged feathers spreading off the shoulders, points out and down: the seer's spiky outline.
            d.Add(Joints.Body, d.pal.cloth, CharacterKit.FurRing(new Vector3(0f, f.shoulderY - 0.05f * s, -0.01f * s), f.shoulderX + 0.07f * s, 0.75f, 0.035f * s, 20, 0.22f * s, d.seed + 68, 0.45f));
            d.Add(Joints.Body, d.pal.clothDark, CharacterKit.FurRing(new Vector3(0f, f.chest - 0.06f * s, -0.01f * s), f.shoulderX + 0.09f * s, 0.75f, 0.035f * s, 18, 0.24f * s, d.seed + 69, 0.6f, 0.55f));
            // Ragged sleeves hanging from the arms.
            foreach (var arm in new[] { Joints.LeftArm, Joints.RightArm })
            {
                float r = 0.05f * s;
                d.Add(arm, d.pal.cloth, MeshData.Lathe(new[] { new Vector2(r * 1.25f, -f.upperArm - 0.02f * s), new Vector2(r, -0.1f * s), new Vector2(r * 0.9f, 0.03f * s) }, 10));
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
            // Ragged feathers round the hood's edge, lying down along it and hanging off the sides like a mane.
            var rng = new System.Random(d.seed + 70);
            for (int i = 0; i < 18; i++)
            {
                float a = Mathf.Lerp(-0.5f, Mathf.PI + 0.5f, i / 17f);
                var root = new Vector3(Mathf.Cos(a) * r * 1.18f, cy + Mathf.Sin(a) * r * 1.2f, r * 0.15f);
                float side = Mathf.Abs(Mathf.Cos(a));
                var outDir = new Vector3(Mathf.Cos(a) * 0.35f, -1f, -0.15f).normalized;
                float len = (0.05f + side * 0.12f + (float)rng.NextDouble() * 0.05f) * s;
                d.Add(Joints.Head, i % 2 == 0 ? d.pal.clothDark : d.pal.cloth, CharacterKit.Tuft(root, root + outDir * len, 0.022f * s));
            }
            // A soft peak at the top of the hood.
            d.Add(Joints.Head, d.pal.clothDark, MeshData.Tube(new[] { new Vector3(0f, cy + r * 1.1f, -0.25f * r), new Vector3(0f, cy + r * 1.45f, -0.45f * r), new Vector3(0f, cy + r * 1.55f, -0.75f * r) },
                new[] { r * 0.55f, r * 0.25f, 0.004f }, 8));
            // Hair framing the face inside the hood, falling from under its rim down past the cheeks.
            foreach (float x in new[] { -1f, 1f })
            {
                d.Add(Joints.Head, d.pal.hair, CharacterKit.Tuft(new Vector3(x * r * 0.55f, cy + r * 0.85f, r * 0.62f), new Vector3(x * r * 0.98f, cy - r * 0.2f, r * 0.62f), 0.032f * s));
                d.Add(Joints.Head, d.pal.hair, CharacterKit.Tuft(new Vector3(x * r * 0.85f, cy + r * 0.4f, r * 0.58f), new Vector3(x * r * 1.02f, cy - r * 0.75f, r * 0.55f), 0.026f * s));
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
                if (i == 1) RuneDisc(d, Joints.Body, p, 0.042f * s, Quaternion.Euler(90f, 0f, 0f));
                else d.Add(Joints.Body, d.pal.parchment, MeshData.Ellipsoid(p, new Vector3(0.016f, 0.04f, 0.016f) * s, 6, 5));
            }
            // Two long strings of charms hanging from the shoulders down either side of the stole, nearly to the
            // knee: wooden rune discs, bone beads and teeth, like the concept art's seer.
            foreach (float side in new[] { -1f, 1f })
            {
                var cord = new Vector3[8];
                for (int i = 0; i < cord.Length; i++)
                {
                    float v = i / (float)(cord.Length - 1);
                    float y = Mathf.Lerp(f.shoulderY - 0.03f * s, f.waist - 0.32f * s, v);
                    // Out in front of the feather tiers, just beside the stole.
                    float rad = (y > f.waist ? f.TorsoRadius(y) * 1.15f + 0.03f * s : d.SkirtRadius(y) * 1.02f) + 0.02f * s;
                    float x = side * (0.062f + 0.015f * v) * s;
                    float z = Mathf.Sqrt(Mathf.Max(0f, rad * rad - x * x)) * (d.hasSkirt ? d.skirtDepth : f.depth) + 0.02f * s;
                    cord[i] = new Vector3(x, y, z);
                }
                var radii = new float[cord.Length];
                for (int i = 0; i < radii.Length; i++) radii[i] = 0.0025f * s;
                d.Add(Joints.Body, d.pal.leatherDark, MeshData.Tube(cord, radii, 4), false);
                for (int i = 1; i < cord.Length; i++)
                {
                    var at = cord[i] + new Vector3(0f, 0f, 0.006f * s);
                    if (i % 3 == 0) RuneDisc(d, Joints.Body, at, 0.036f * s, Quaternion.Euler(90f, 0f, 0f));
                    else if (i % 3 == 1) d.Add(Joints.Body, d.pal.parchment, MeshData.Ellipsoid(at, new Vector3(0.011f, 0.02f, 0.011f) * s, 6, 4), false);
                    else d.Add(Joints.Body, d.pal.leather, MeshData.Ellipsoid(at, Vector3.one * 0.012f * s, 6, 4), false);
                }
            }
            // Belt: a cord with a big wooden rune disc at the front.
            d.Add(Joints.Body, d.pal.leather, CharacterKit.Band(f.waist, 0.03f * s, d.SkirtRadius(f.waist) * 1.06f, d.skirtDepth + 0.02f));
            RuneDisc(d, Joints.Body, new Vector3(0f, f.waist, d.SkirtRadius(f.waist) * (d.skirtDepth + 0.02f) + 0.06f * s), 0.045f * s, Quaternion.Euler(90f, 0f, 0f));
            // Charms hanging from the belt on cords.
            foreach (float x in new[] { -0.11f, -0.07f, -0.035f, 0.06f, 0.1f })
            {
                var top = new Vector3(x * s, f.waist - 0.02f * s, d.SkirtRadius(f.waist) * d.skirtDepth + 0.02f);
                var end = top + new Vector3(0f, -(0.16f + Mathf.Abs(x) * 1.2f) * s, 0.02f * s);
                d.Add(Joints.Body, d.pal.leatherDark, MeshData.Tube(new[] { top, end }, new[] { 0.003f * s, 0.003f * s }, 4), false);
                if (x > 0f) RuneDisc(d, Joints.Body, end, 0.028f * s, Quaternion.Euler(90f, 0f, 0f));
                else d.Add(Joints.Body, d.pal.parchment, MeshData.Ellipsoid(end, new Vector3(0.018f, 0.028f, 0.016f) * s, 7, 5));
            }
        }

        /// <summary>A round wooden disc with a rune burnt into it (Algiz-like), facing along the rotation's up axis.</summary>
        /// <summary>
        /// The seer's belt: a cord belt with a big carved rune medallion at the front, and at the right hip a bundle
        /// of charms hanging on thongs: a small animal skull, a long rune pendant, a bone and two dark feathers.
        /// </summary>
        public static void SeerBelt(Dresser d)
        {
            var f = d.fit;
            float s = d.S;
            // Worn over the feather tiers and the stole, so it has to sit outside both.
            float y = f.waist + 0.01f * s, depth = 1f;
            float r = f.TorsoRadius(y) * 1.15f + 0.05f * s;
            d.Add(Joints.Body, d.pal.leatherDark, CharacterKit.Band(y, 0.035f * s, r, depth));
            // The medallion: a wooden disc with a ring of dots round a rune.
            var front = new Vector3(0f, y, r * depth + 0.012f * s);
            d.Add(Joints.Body, d.pal.leather, MeshData.Lathe(new[] { new Vector2(0.05f * s, -0.008f * s), new Vector2(0.052f * s, 0f), new Vector2(0.046f * s, 0.01f * s), new Vector2(0.001f, 0.012f * s) }, 18)
                .Transformed(front, Quaternion.Euler(90f, 0f, 0f), Vector3.one));
            RuneDisc(d, Joints.Body, front + new Vector3(0f, 0f, 0.012f * s), 0.028f * s, Quaternion.Euler(90f, 0f, 0f));
            for (int i = 0; i < 10; i++)
            {
                float a = i / 10f * Mathf.PI * 2f;
                d.Add(Joints.Body, d.pal.ink, MeshData.Ellipsoid(front + new Vector3(Mathf.Cos(a) * 0.04f * s, Mathf.Sin(a) * 0.04f * s, 0.011f * s), Vector3.one * 0.0045f * s, 4, 3), false);
            }
            // The bundle at the right hip, hanging out in front of the robe.
            // Just inside the edge of the open cloak, so it isn't lost among the feathers.
            float ang = 0.45f;
            var hip = new Vector3(Mathf.Sin(ang) * r, y - 0.01f * s, Mathf.Cos(ang) * r * depth + 0.03f * s);
            var bone = d.pal.parchment;
            // Thongs down to each charm: (x offset, drop, z offset).
            var hangs = new[] { new Vector3(-0.02f, 0.13f, 0.03f), new Vector3(0.015f, 0.26f, 0.02f), new Vector3(0.04f, 0.19f, 0.01f) };
            foreach (var h in hangs)
            {
                var end = hip + new Vector3(h.x, -h.y, h.z) * s;
                d.Add(Joints.Body, d.pal.leatherDark, MeshData.Tube(new[] { hip, end }, new[] { 0.003f * s, 0.003f * s }, 4), false);
            }
            // The skull: a rounded cranium, a snout, dark eye holes, looking out.
            var skull = hip + new Vector3(-0.02f, -0.15f, 0.045f) * s;
            d.Add(Joints.Body, bone, MeshData.Ellipsoid(skull, new Vector3(0.045f, 0.04f, 0.042f) * s, 9, 6));
            d.Add(Joints.Body, bone, MeshData.Ellipsoid(skull + new Vector3(0f, -0.036f, 0.022f) * s, new Vector3(0.028f, 0.03f, 0.024f) * s, 7, 5));
            foreach (float ex in new[] { -1f, 1f })
                d.Add(Joints.Body, d.pal.ink, MeshData.Ellipsoid(skull + new Vector3(ex * 0.018f, -0.005f, 0.036f) * s, new Vector3(0.011f, 0.01f, 0.006f) * s, 5, 3), false);
            // The rune pendant: a long wooden drop with a rune cut in it.
            var pend = hip + new Vector3(0.015f, -0.3f, 0.03f) * s;
            d.Add(Joints.Body, d.pal.leather, MeshData.Ellipsoid(pend, new Vector3(0.028f, 0.045f, 0.008f) * s, 10, 6));
            d.Add(Joints.Body, d.pal.ink, MeshData.Tube(new[] { pend + new Vector3(0f, 0.025f, 0.009f) * s, pend + new Vector3(0f, -0.025f, 0.009f) * s }, new[] { 0.003f * s, 0.003f * s }, 4), false);
            d.Add(Joints.Body, d.pal.ink, MeshData.Tube(new[] { pend + new Vector3(0f, 0.01f, 0.009f) * s, pend + new Vector3(0.014f, -0.008f, 0.009f) * s }, new[] { 0.003f * s, 0.003f * s }, 4), false);
            // A bone and two dark feathers.
            d.Add(Joints.Body, bone, MeshData.Ellipsoid(hip + new Vector3(0.04f, -0.21f, 0.015f) * s, new Vector3(0.011f, 0.03f, 0.011f) * s, 6, 5));
            for (int k = 0; k < 2; k++)
            {
                var root = hip + new Vector3(0.05f + k * 0.015f, -0.02f, 0.0f) * s;
                var tip = root + new Vector3(0.03f + k * 0.02f, -0.2f + k * 0.03f, 0.01f) * s;
                var grid = new Vector3[5, 2];
                for (int i = 0; i < 5; i++)
                {
                    float v = i / 4f, w = Mathf.Sin(v * Mathf.PI) * 0.018f * s + 0.002f * s;
                    var p = Vector3.Lerp(root, tip, v);
                    grid[i, 0] = p + new Vector3(-w, 0f, 0f);
                    grid[i, 1] = p + new Vector3(w, 0f, 0f);
                }
                d.Add(Joints.Body, d.pal.clothDark, CharacterKit.Sheet(grid, Vector3.forward, 0.004f * s));
            }
        }

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

        /// <summary>
        /// Great branching antlers rising from the hood, lashed to a wooden crossbar behind the head; rune discs, bones
        /// and bead strings hang on cords all along the bar and the antlers.
        /// </summary>
        public static void Antlers(Dresser d)
        {
            var f = d.fit;
            float r = f.headR, cy = HeroModel.HeadCentre(f), s = d.S;
            var horn = d.pal.leather;
            var rng = new System.Random(d.seed + 71);
            // The crossbar, just above the hood behind the head.
            float barY = cy + r * 1.05f, barZ = -r * 0.45f, half = 0.36f * s;
            d.Add(Joints.Head, VikingModel.Shade(horn, 0.9f), MeshData.Tube(new[] { new Vector3(-half, barY - 0.02f * s, barZ), new Vector3(0f, barY + 0.01f * s, barZ), new Vector3(half, barY - 0.02f * s, barZ) }, new[] { 0.016f * s, 0.018f * s, 0.016f * s }, 7));
            foreach (float x in new[] { -1f, 1f })
            {
                var root = new Vector3(x * r * 0.5f, cy + r * 1.05f, -r * 0.35f);
                // A thick beam sweeping out and then curving up and in.
                var beam = new[] {
                    root, root + new Vector3(x * 0.09f, 0.07f, -0.01f) * s, root + new Vector3(x * 0.2f, 0.16f, -0.02f) * s,
                    root + new Vector3(x * 0.28f, 0.28f, -0.02f) * s, root + new Vector3(x * 0.27f, 0.4f, 0.0f) * s, root + new Vector3(x * 0.22f, 0.48f, 0.02f) * s };
                d.Add(Joints.Head, horn, MeshData.Tube(beam, new[] { 0.04f * s, 0.036f * s, 0.03f * s, 0.024f * s, 0.016f * s, 0.006f * s }, 8));
                // Four tines, the lower ones reaching forward-up, the upper ones straight up.
                foreach (var t in new[] { new[] { 1f, 0.15f, 0.05f, 0.06f }, new[] { 2f, 0.18f, -0.04f, 0.04f }, new[] { 3f, 0.16f, 0.06f, 0.02f }, new[] { 4f, 0.12f, -0.05f, 0f } })
                {
                    var at = beam[(int)t[0]];
                    var tip = at + new Vector3(x * t[2], t[1], t[3]) * s;
                    var mid = Vector3.Lerp(at, tip, 0.5f) + new Vector3(x * 0.015f, 0f, 0.01f) * s;
                    d.Add(Joints.Head, horn, MeshData.Tube(new[] { at, mid, tip }, new[] { 0.022f * s, 0.015f * s, 0.004f * s }, 6));
                }
                // Charms on cords from the beam.
                for (int k = 0; k < 3; k++)
                {
                    var hang = Vector3.Lerp(beam[1], beam[3], k / 2f);
                    Charm(d, hang, (0.1f + 0.08f * (k % 2)) * s, k, rng);
                }
            }
            // One long string of charms each side, hanging from the bar clear of the hood down past the shoulder:
            // a rune disc, a bead, another disc, a tooth. (Kept away from the face, which stays clear.)
            foreach (float x in new[] { -1f, 1f })
                foreach (float spread in new[] { 1.75f })
                {
                    var top = new Vector3(x * r * spread, barY - 0.02f * s, r * 0.2f);
                    float len = 0.46f * s;
                    var bottom = top + new Vector3(0f, -len, 0.02f * s);
                    d.Add(Joints.Head, d.pal.leatherDark, MeshData.Tube(new[] { top, bottom }, new[] { 0.0025f * s, 0.0025f * s }, 4), false);
                    for (int k = 0; k < 4; k++)
                    {
                        var at = Vector3.Lerp(top, bottom, 0.3f + k * 0.22f) + new Vector3(0f, 0f, 0.006f * s);
                        if (k % 2 == 0) RuneDisc(d, Joints.Head, at, 0.042f * s, Quaternion.Euler(90f, 0f, 0f));
                        else if (k == 1) d.Add(Joints.Head, d.pal.parchment, MeshData.Ellipsoid(at, Vector3.one * 0.01f * s, 6, 4), false);
                        else d.Add(Joints.Head, d.pal.parchment, MeshData.Tube(new[] { at + new Vector3(0f, 0.015f, 0f) * s, at - new Vector3(0f, 0.025f, 0f) * s }, new[] { 0.008f * s, 0.001f }, 5), false);
                    }
                }
            // Charms hanging along the crossbar.
            for (int k = 0; k < 6; k++)
            {
                float x = Mathf.Lerp(-half * 0.95f, half * 0.95f, k / 5f);
                if (Mathf.Abs(x) < r * 1.3f) continue; // not round the face
                Charm(d, new Vector3(x, barY - 0.02f * s, barZ + 0.02f * s), (0.1f + 0.06f * (k % 3)) * s, k + 3, rng);
            }
        }

        /// <summary>A cord with a rune disc, a bone or a string of beads at the end.</summary>
        static void Charm(Dresser d, Vector3 from, float drop, int kind, System.Random rng)
        {
            float s = d.S;
            var end = from + new Vector3(((float)rng.NextDouble() - 0.5f) * 0.01f * s, -drop, 0f);
            d.Add(Joints.Head, d.pal.leatherDark, MeshData.Tube(new[] { from, end }, new[] { 0.0025f * s, 0.0025f * s }, 4), false);
            switch (kind % 3)
            {
                case 0: RuneDisc(d, Joints.Head, end + Vector3.down * 0.05f * s, 0.05f * s, Quaternion.Euler(90f, 0f, 0f)); break;
                case 1:
                    // A rune disc with a bone bead dangling under it.
                    RuneDisc(d, Joints.Head, end + Vector3.down * 0.045f * s, 0.044f * s, Quaternion.Euler(90f, 0f, 0f));
                    d.Add(Joints.Head, d.pal.leatherDark, MeshData.Tube(new[] { end + Vector3.down * 0.09f * s, end + Vector3.down * 0.12f * s }, new[] { 0.002f * s, 0.002f * s }, 4), false);
                    d.Add(Joints.Head, d.pal.parchment, MeshData.Ellipsoid(end + Vector3.down * 0.14f * s, new Vector3(0.011f, 0.024f, 0.011f) * s, 6, 5));
                    break;
                default:
                    for (int i = 0; i < 4; i++)
                        d.Add(Joints.Head, i % 2 == 0 ? d.pal.leather : d.pal.parchment, MeshData.Ellipsoid(end + Vector3.down * (0.012f + i * 0.018f) * s, Vector3.one * 0.01f * s, 6, 4), false);
                    break;
            }
        }

        // ---------------------------------------------------------------- the scout

        /// <summary>A dark cloth cap pulled low, its crown and brim covered in shaggy fur, worn a little askew.</summary>
        public static void FurCap(Dresser d)
        {
            var f = d.fit;
            float r = f.headR, cy = HeroModel.HeadCentre(f), s = d.S;
            float brim = cy + 0.2f * r;
            var tilt = Quaternion.Euler(-6f, 0f, 12f);
            // A fur hat (pelt side out) with a dark cloth band wound slantwise across it, as in the concept art.
            d.Add(Joints.Head, d.pal.fur, MeshData.Dome(Vector3.zero, new Vector3(r * 1.1f, r * 1.05f, r * 1.1f), 16, 6).Transformed(new Vector3(0f, brim, -0.02f * r), tilt, Vector3.one));
            d.Add(Joints.Head, d.pal.clothDark, CharacterKit.Band(0.38f * r, 0.42f * r, r * 1.09f, 1f, 20)
                .Transformed(new Vector3(0f, brim, -0.02f * r), tilt * Quaternion.Euler(8f, 0f, -24f), Vector3.one));
            d.Add(Joints.Head, d.pal.fur, CharacterKit.FurRing(Vector3.zero, r * 1.12f, 1f, 0.045f * s, 16, 0.03f * s, d.seed + 81, 0.3f).Transformed(new Vector3(0f, brim + 0.025f * s, -0.02f * r), tilt, Vector3.one));
            // Shaggy tufts over the crown in both shades.
            var rng = new System.Random(d.seed + 82);
            for (int i = 0; i < 10; i++)
            {
                float a = 0.9f + (float)rng.NextDouble() * Mathf.PI * 1.1f, e = 0.3f + (float)rng.NextDouble() * 0.9f;
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
                new Vector2(f.chestR * 0.5f, f.neckY - 0.03f * s), new Vector2(f.chestR * 0.85f, f.neckY + 0.01f * s), new Vector2(f.chestR * 0.6f, f.neckY + 0.04f * s) }, 16)
                .Transformed(Vector3.zero, Quaternion.identity, new Vector3(1f, 1f, 0.95f)));
            // A wide capelet over both shoulders, down to the upper arms, falling to a ragged point at the front of the chest.
            float capeBottom = f.shoulderY - 0.1f * s;
            d.Add(Joints.Body, d.pal.accent, CharacterKit.RaggedSkirt(f.neckY + 0.01f * s, f.chestR * 0.7f, capeBottom, f.shoulderX + 0.065f * s, 0.85f, 22, 0.045f * s, d.seed + 83,
                a => { float c = Mathf.Max(0f, Mathf.Cos(a)); return -0.1f * s * c * c * c * c; }, a => { float c = Mathf.Max(0f, Mathf.Cos(a)); return Mathf.Lerp(1f, 0.6f, c * c); }));
            // The hood, down, bunched behind the neck.
            d.Add(Joints.Body, VikingModel.Shade(d.pal.accent, 0.85f), MeshData.Ellipsoid(new Vector3(0f, f.neckY + 0.02f * s, -f.chestR * 0.7f), new Vector3(0.11f, 0.08f, 0.07f) * s, 12, 7));
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
            // Over the left shoulder, so the arrows are drawn with the right hand.
            var at = new Vector3(-0.06f * s, f.chest - 0.02f * s, -f.chestR * f.depth - 0.05f * s);
            var tilt = Quaternion.Euler(0f, 0f, 28f);
            d.Add(Joints.Body, d.pal.leather, MeshData.Lathe(new[] { new Vector2(0.035f * s, -0.22f * s), new Vector2(0.045f * s, 0.14f * s) }, 10).Transformed(at, tilt, Vector3.one));
            d.Add(Joints.Body, d.pal.leatherDark, MeshData.Lathe(new[] { new Vector2(0.048f * s, 0.1f * s), new Vector2(0.048f * s, 0.14f * s) }, 10).Transformed(at, tilt, Vector3.one), false);
            // A fan of arrows standing well up out of the quiver, their red fletching above the shoulder.
            for (int i = 0; i < 6; i++)
            {
                var off = new Vector3((i - 2.5f) * 0.016f, 0f, (i % 2) * 0.016f) * s;
                var fan = Quaternion.Euler(0f, 0f, (i - 2.5f) * 4f);
                var shaftTop = at + tilt * (off + fan * new Vector3(0f, (0.4f + 0.03f * (i % 3)) * s, 0f));
                d.Add(Joints.Body, d.pal.leatherDark, MeshData.Tube(new[] { at + tilt * (off + new Vector3(0f, 0.1f * s, 0f)), shaftTop }, new[] { 0.005f * s, 0.005f * s }, 4), false);
                // Fletching: two red vanes.
                foreach (float side in new[] { -1f, 1f })
                    d.Add(Joints.Body, new Color(0.7f, 0.2f, 0.14f), MeshData.Extrude(new[] { new Vector2(0f, 0f), new Vector2(0.1f, 0f), new Vector2(0.11f, 0.04f), new Vector2(0.03f, 0.04f) }, 0.005f)
                        .Transformed(shaftTop - tilt * new Vector3(0f, 0.1f * s, 0f), tilt * Quaternion.Euler(-90f, side * 90f, 0f), Vector3.one * s));
            }
            // Strap from the left shoulder across the chest to the right hip, worn over the cowl and pelt so it shows
            // as in the concept art, with a buckle on the breast.
            float cz = f.chestR * f.depth;
            var strap = new[] {
                new Vector3(-f.shoulderX * 0.6f, f.shoulderY + 0.03f * s, cz * 0.35f), new Vector3(-f.shoulderX * 0.25f, f.shoulderY - 0.06f * s, cz + 0.05f * s),
                new Vector3(0.02f * s, f.chest - 0.03f * s, cz + 0.05f * s), new Vector3(f.waistR * 0.95f, f.waist + 0.02f * s, f.waistR * f.depth + 0.035f * s) };
            d.Add(Joints.Body, d.pal.leatherDark, MeshData.Tube(strap, new[] { 0.014f * s, 0.016f * s, 0.016f * s, 0.015f * s }, 5), false);
            var buckle = Vector3.Lerp(strap[1], strap[2], 0.5f) + new Vector3(0f, 0f, 0.012f * s);
            d.Add(Joints.Body, d.pal.brass, MeshData.Box(buckle, new Vector3(0.03f, 0.024f, 0.008f) * s), false);
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
            // Loose wavy locks falling to the shoulder on the braid's side.
            d.Add(Joints.Head, d.pal.hair, CharacterKit.Tuft(new Vector3(r * 0.95f, cy + r * 0.1f, r * 0.05f), new Vector3(r * 1.2f, cy - r * 1.1f, r * 0.1f), 0.03f * s));
            d.Add(Joints.Head, d.pal.hair, CharacterKit.Tuft(new Vector3(r * 0.9f, cy, -r * 0.3f), new Vector3(r * 1.3f, cy - r * 1.3f, -r * 0.25f), 0.026f * s));
            // A short braid gathered low at the back and tied, then loosening into a tail of wavy locks that flares
            // out over the right shoulder, as in the concept art.
            var path = new[] { new Vector3(r * 0.6f, cy - r * 0.55f, -r * 0.75f), new Vector3(r * 1.0f, cy - r * 0.95f, -r * 0.62f), new Vector3(r * 1.25f, cy - r * 1.3f, -r * 0.5f) };
            string bj = d.Swing(Joints.LeftBraid, Joints.Head, path[0], SwingKind.Braid);
            var p0 = path[0];
            for (int i = 0; i < path.Length; i++) path[i] -= p0;
            d.Add(bj, d.pal.hair, CharacterKit.Braid(path, 0.032f * s));
            var end = path[path.Length - 1];
            d.Add(bj, d.pal.leatherDark, MeshData.Ellipsoid(end, new Vector3(0.028f, 0.014f, 0.028f) * s, 8, 4), false);
            var locks = new[] { new Vector3(0.07f, -0.13f, 0.02f), new Vector3(0.11f, -0.07f, -0.02f), new Vector3(0.03f, -0.17f, 0.03f), new Vector3(0.09f, -0.16f, -0.01f) };
            for (int i = 0; i < locks.Length; i++)
            {
                // Each lock bends once on the way down, so the tail reads as wavy rather than a stiff brush.
                var tip = end + locks[i] * s;
                var mid = Vector3.Lerp(end, tip, 0.5f) + new Vector3(-0.02f, 0f, 0.01f) * s * ((i & 1) == 0 ? 1f : -1f);
                d.Add(bj, i == 3 ? VikingModel.Shade(d.pal.hair, 0.8f) : d.pal.hair, CharacterKit.Tuft(end, mid, 0.024f * s));
                d.Add(bj, i == 3 ? VikingModel.Shade(d.pal.hair, 0.8f) : d.pal.hair, CharacterKit.Tuft(mid, tip, 0.017f * s));
            }
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
            string bj = d.Swing(Joints.RightBraid, Joints.Head, path[0], SwingKind.Braid);
            var p0 = path[0];
            for (int i = 0; i < path.Length; i++) path[i] -= p0;
            d.Add(bj, d.pal.hair, CharacterKit.Braid(path, 0.038f * s));
            Vector3 end = path[path.Length - 1];
            d.Add(bj, d.pal.leatherDark, MeshData.Ellipsoid(CharacterKit.Along(path, 0.9f), new Vector3(0.03f, 0.013f, 0.03f) * s, 8, 4), false);
            d.Add(bj, d.pal.hair, CharacterKit.Tuft(end, end + new Vector3(0f, -0.07f * s, 0.01f), 0.026f * s));
            d.Add(Joints.Head, d.pal.hair, MeshData.Ellipsoid(new Vector3(0f, cy - r * 0.1f, -r * 0.25f), new Vector3(r * 0.98f, r * 0.8f, r * 0.8f), 12, 8));
        }

        /// <summary>Long braids; <paramref name="wrapped"/> binds the lower part in pale cloth, like the jarl's.</summary>
        /// <summary>
        /// Two braids worn like the raider in the concept art: the right one swung forward over the shoulder and
        /// hanging outside the arm, the left one thrown back down the shoulder blade.
        /// </summary>
        public static void SwungBraids(Dresser d, float length)
        {
            var f = d.fit;
            float r = f.headR, cy = HeroModel.HeadCentre(f), s = d.S;
            float sy = f.shoulderY - f.neckY;
            float back = -f.chestR * f.depth - 0.03f * s;
            var paths = new[] {
                // Right (+X): over the shoulder, out past the arm, hanging free.
                new[] { new Vector3(r * 0.9f, cy - r * 0.15f, -r * 0.35f), new Vector3(r * 1.08f, cy - r * 0.7f, -r * 0.05f),
                        new Vector3(f.shoulderX + 0.04f * s, sy + 0.05f * s, r * 0.15f), new Vector3(f.shoulderX + 0.11f * s, sy - 0.06f * s, r * 0.05f),
                        new Vector3(f.shoulderX + 0.15f * s, sy - length * s, -r * 0.1f) },
                // Left (-X): back behind the shoulder, down the shoulder blade.
                new[] { new Vector3(-r * 0.9f, cy - r * 0.15f, -r * 0.35f), new Vector3(-r * 0.95f, cy - r * 0.7f, -r * 0.6f),
                        new Vector3(-f.shoulderX * 0.6f, sy + 0.03f * s, back), new Vector3(-f.shoulderX * 0.55f, sy - length * 0.8f * s, back - 0.01f * s) } };
            for (int b = 0; b < 2; b++)
            {
                var path = paths[b];
                string bj = d.Swing(b == 0 ? Joints.RightBraid : Joints.LeftBraid, Joints.Head, path[0], SwingKind.Braid);
                var p0 = path[0];
                for (int i = 0; i < path.Length; i++) path[i] -= p0;
                d.Add(bj, d.pal.hair, CharacterKit.Braid(path, 0.03f * s));
                Vector3 tie = CharacterKit.Along(path, 0.88f);
                d.Add(bj, d.pal.leatherDark, MeshData.Ellipsoid(tie, new Vector3(0.026f, 0.012f, 0.026f) * s, 8, 4), false);
                Vector3 end = path[path.Length - 1];
                d.Add(bj, d.pal.hair, CharacterKit.Tuft(end, end + new Vector3(0f, -0.06f * s, 0.01f), 0.02f * s));
            }
            d.Add(Joints.Head, d.pal.hair, MeshData.Ellipsoid(new Vector3(0f, cy - r * 0.1f, -r * 0.25f), new Vector3(r * 0.98f, r * 0.8f, r * 0.8f), 12, 8));
            // Loose red locks falling by the cheeks, fuller on the swung side.
            d.Add(Joints.Head, d.pal.hair, CharacterKit.Tuft(new Vector3(r * 0.92f, cy + r * 0.05f, r * 0.2f), new Vector3(r * 1.08f, cy - r * 0.7f, r * 0.3f), 0.026f * s));
            d.Add(Joints.Head, d.pal.hair, CharacterKit.Tuft(new Vector3(-r * 0.92f, cy + r * 0.05f, r * 0.2f), new Vector3(-r * 1.0f, cy - r * 0.5f, r * 0.3f), 0.02f * s));
        }

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
                string bj = d.Swing(x < 0f ? Joints.LeftBraid : Joints.RightBraid, Joints.Head, path[0], SwingKind.Braid);
                var p0 = path[0];
                for (int i = 0; i < path.Length; i++) path[i] -= p0;
                d.Add(bj, d.pal.hair, CharacterKit.Braid(path, 0.03f * s));
                if (length > 0.55f)
                    for (int k = 0; k < 3; k++)
                        d.Add(bj, k == 1 ? d.pal.parchment : d.pal.leather, MeshData.Lathe(new[] { new Vector2(0.034f * s, -0.012f * s), new Vector2(0.038f * s, 0f), new Vector2(0.034f * s, 0.012f * s) }, 10)
                            .Transformed(CharacterKit.Along(path, 0.45f + k * 0.17f), Quaternion.identity, Vector3.one), false);
                if (wrapped)
                    for (int k = 0; k < 4; k++)
                        d.Add(bj, d.pal.fur, MeshData.Ellipsoid(CharacterKit.Along(path, 0.62f + k * 0.08f), new Vector3(0.034f, 0.024f, 0.034f) * s, 8, 5));
                // Leather ties and a loose tassel at the end.
                Vector3 tie = CharacterKit.Along(path, 0.88f);
                d.Add(bj, d.pal.leatherDark, MeshData.Ellipsoid(tie, new Vector3(0.026f, 0.012f, 0.026f) * s, 8, 4), false);
                Vector3 end = path[path.Length - 1];
                d.Add(bj, d.pal.hair, CharacterKit.Tuft(end, end + new Vector3(0f, -0.06f * s, 0.01f), 0.02f * s));
            }
            // Hair showing at the back of the head under the helmet, and loose locks by the cheeks.
            d.Add(Joints.Head, d.pal.hair, MeshData.Ellipsoid(new Vector3(0f, cy - r * 0.1f, -r * 0.25f), new Vector3(r * 0.98f, r * 0.8f, r * 0.8f), 12, 8));
            foreach (float x in new[] { -1f, 1f })
                d.Add(Joints.Head, d.pal.hair, CharacterKit.Tuft(new Vector3(x * r * 0.92f, cy + r * 0.05f, r * 0.2f), new Vector3(x * r * 1.02f, cy - r * 0.55f, r * 0.35f), 0.022f * s));
        }
    }
}
