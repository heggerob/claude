using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Shapes for the storybook characters: stick limbs, fur tufts, jagged hems, hanging cloth, braids,
    /// straps wound around boots. All plain <see cref="MeshData"/>, facing outwards.
    /// </summary>
    public static class CharacterKit
    {
        /// <summary>A thin straight rod (the black stick arms and legs).</summary>
        public static MeshData Stick(Vector3 a, Vector3 b, float radius, int segments = 6)
        {
            return MeshData.Tube(new[] { a, b }, new[] { radius, radius }, segments);
        }

        /// <summary>A pointed tuft: a narrow cone from <paramref name="root"/> towards <paramref name="tip"/>.</summary>
        public static MeshData Tuft(Vector3 root, Vector3 tip, float width)
        {
            Vector3 mid = Vector3.Lerp(root, tip, 0.45f) + Vector3.down * 0.15f * Vector3.Distance(root, tip);
            return MeshData.Tube(new[] { root, mid, tip }, new[] { width, width * 0.7f, 0.002f }, 4);
        }

        /// <summary>
        /// A shaggy ring of fur: a soft roll plus tufts sticking out and drooping, like a pelt collar or a boot cuff.
        /// The ring is an ellipse (radius x, depth factor) around a centre, at the centre's height.
        /// </summary>
        public static MeshData FurRing(Vector3 centre, float radius, float depth, float thickness, int tufts, float tuftLength, int seed, float droop = 0.6f)
        {
            var rng = new System.Random(seed);
            var m = new MeshData();
            // The roll.
            m.Append(MeshData.Lathe(new[] { new Vector2(radius * 0.92f, -thickness * 0.7f), new Vector2(radius * 1.05f, -thickness * 0.2f), new Vector2(radius * 1.05f, thickness * 0.3f), new Vector2(radius * 0.9f, thickness * 0.7f) }, 20)
                .Transformed(centre, Quaternion.identity, new Vector3(1f, 1f, depth)));
            // Soft clumps along the roll, and tufts of varied length hanging out of it, so it reads as fluffy
            // pelt rather than a row of teeth.
            for (int i = 0; i < tufts; i++)
            {
                float a = (i + (float)rng.NextDouble() * 0.6f) / tufts * Mathf.PI * 2f;
                Vector3 dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a) * depth);
                float y = ((float)rng.NextDouble() - 0.6f) * thickness * 0.8f;
                Vector3 root = centre + dir * radius * 0.95f + Vector3.up * y;
                float lump = thickness * (0.45f + (float)rng.NextDouble() * 0.3f);
                m.Append(MeshData.Ellipsoid(root, new Vector3(lump, lump * 0.8f, lump), 7, 4));
                if (rng.NextDouble() < 0.25) continue; // not every clump has a tuft
                // Broad, short, drooping locks rather than long spikes.
                float len = tuftLength * (0.35f + (float)rng.NextDouble() * 0.6f);
                float fall = droop * (0.8f + (float)rng.NextDouble() * 0.6f);
                Vector3 tip = root + (dir * (1f - fall * 0.5f) + Vector3.down * fall).normalized * len;
                m.Append(Tuft(root, tip, thickness * (0.55f + (float)rng.NextDouble() * 0.35f)));
            }
            return m;
        }

        /// <summary>
        /// A skirt or tunic tail: an elliptical cone from the waist down to a ragged hem. Closed underneath
        /// (the legs come out through the bottom), so it's a solid that shades and outlines cleanly.
        /// </summary>
        public static MeshData RaggedSkirt(float topY, float topRadius, float bottomY, float bottomRadius, float depth, int segments, float jag, int seed)
        {
            return RaggedSkirt(topY, topRadius, bottomY, bottomRadius, depth, segments, jag, seed, null);
        }

        /// <summary>
        /// A ragged skirt whose hem can be raised by angle: <paramref name="hemRaise"/> takes the angle (0 = front,
        /// radians) and returns how far to lift the hem there (a pelt that's short in front, long at the back).
        /// </summary>
        public static MeshData RaggedSkirt(float topY, float topRadius, float bottomY, float bottomRadius, float depth, int segments, float jag, int seed, System.Func<float, float> hemRaise)
        {
            return RaggedSkirt(topY, topRadius, bottomY, bottomRadius, depth, segments, jag, seed, hemRaise, null);
        }

        /// <summary>...and <paramref name="hemScale"/> scales the hem's radius by angle (hugging the chest in front).</summary>
        public static MeshData RaggedSkirt(float topY, float topRadius, float bottomY, float bottomRadius, float depth, int segments, float jag, int seed, System.Func<float, float> hemRaise, System.Func<float, float> hemScale)
        {
            var rng = new System.Random(seed);
            const int rows = 5;
            var m = new MeshData();
            var hem = new float[segments];
            for (int s = 0; s < segments; s++) hem[s] = ((s & 1) == 0 ? 1f : 0.2f) * jag * (0.5f + (float)rng.NextDouble());
            for (int r = 0; r <= rows; r++)
            {
                float k = r / (float)rows; // 0 = hem, 1 = waist
                for (int s = 0; s < segments; s++)
                {
                    float a = s / (float)segments * Mathf.PI * 2f;
                    float raise = hemRaise != null ? hemRaise(a) : 0f;
                    float y = Mathf.Lerp(bottomY - hem[s] + raise, topY, k);
                    // Slight bell: widest just above the hem.
                    float hr = bottomRadius * (hemScale != null ? hemScale(a) : 1f);
                    float rad = Mathf.Lerp(hr, topRadius, k * k) * (1f + 0.04f * Mathf.Sin(k * Mathf.PI));
                    m.Vertices.Add(new Vector3(Mathf.Sin(a) * rad, y, Mathf.Cos(a) * rad * depth));
                    m.Uvs.Add(new Vector2(s / (float)segments * 5f, y * MeshData.UvScale));
                }
            }
            for (int r = 0; r < rows; r++)
                for (int s = 0; s < segments; s++)
                {
                    int a = r * segments + s, b = r * segments + (s + 1) % segments;
                    int c = a + segments, d = b + segments;
                    m.Triangles.Add(a); m.Triangles.Add(b); m.Triangles.Add(c);
                    m.Triangles.Add(b); m.Triangles.Add(d); m.Triangles.Add(c);
                }
            // Caps: underneath (following the hem) and on top.
            int under = m.Vertices.Count;
            m.Vertices.Add(new Vector3(0f, bottomY + jag, 0f));
            for (int s = 0; s < segments; s++) { m.Triangles.Add(under); m.Triangles.Add((s + 1) % segments); m.Triangles.Add(s); }
            int over = m.Vertices.Count;
            m.Vertices.Add(new Vector3(0f, topY, 0f));
            int top = rows * segments;
            for (int s = 0; s < segments; s++) { m.Triangles.Add(over); m.Triangles.Add(top + s); m.Triangles.Add(top + (s + 1) % segments); }
            return m;
        }

        /// <summary>
        /// A piece of cloth with thickness from a grid of points (rows top to bottom, columns left to right as seen
        /// from the side the cloth faces). <paramref name="facing"/> points out of the front face.
        /// </summary>
        public static MeshData Sheet(Vector3[,] grid, Vector3 facing, float thickness)
        {
            int rows = grid.GetLength(0), cols = grid.GetLength(1);
            var m = new MeshData();
            Vector3 half = facing.normalized * thickness * 0.5f;
            for (int side = 0; side < 2; side++)
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                    {
                        m.Vertices.Add(grid[r, c] + (side == 0 ? half : -half));
                        m.Uvs.Add(new Vector2(c / (float)Mathf.Max(1, cols - 1) * Vector3.Distance(grid[r, 0], grid[r, cols - 1]) * MeshData.UvScale, -grid[r, c].y * MeshData.UvScale));
                    }
            int back = rows * cols;
            System.Func<int, int, int> F = (r, c) => r * cols + c;
            // Whichever way the grid runs, wind the faces so the front one faces `facing`.
            Vector3 across = grid[0, cols - 1] - grid[0, 0], down = grid[rows - 1, 0] - grid[0, 0];
            bool flip = Vector3.Dot(Vector3.Cross(across, down), facing) < 0f;
            System.Action<int, int, int> T = (x, y, z) =>
            {
                m.Triangles.Add(x);
                m.Triangles.Add(flip ? z : y);
                m.Triangles.Add(flip ? y : z);
            };
            for (int r = 0; r < rows - 1; r++)
                for (int c = 0; c < cols - 1; c++)
                {
                    int a = F(r, c), b = F(r, c + 1), d = F(r + 1, c), e = F(r + 1, c + 1);
                    // Front face.
                    T(a, b, d);
                    T(b, e, d);
                    // Back face (reversed).
                    T(back + a, back + d, back + b);
                    T(back + b, back + d, back + e);
                }
            // Edges all the way round.
            var ring = new System.Collections.Generic.List<int>();
            for (int c = 0; c < cols; c++) ring.Add(F(0, c));
            for (int r = 1; r < rows; r++) ring.Add(F(r, cols - 1));
            for (int c = cols - 2; c >= 0; c--) ring.Add(F(rows - 1, c));
            for (int r = rows - 2; r > 0; r--) ring.Add(F(r, 0));
            for (int i = 0; i < ring.Count; i++)
            {
                int a = ring[i], b = ring[(i + 1) % ring.Count];
                T(a, back + a, b);
                T(b, back + a, back + b);
            }
            return m;
        }

        /// <summary>
        /// A cape hanging from the shoulders down the back: curved around the body, flaring towards a ragged hem.
        /// Local to the body; <paramref name="top"/> is the middle of the neckline.
        /// </summary>
        public static MeshData Cape(Vector3 top, float topWidth, float bottomWidth, float length, float wrap, float jag, int seed, float thickness = 0.018f)
        {
            var rng = new System.Random(seed);
            const int rows = 10, cols = 25;
            // A torn hem: many narrow strips of uneven length rather than a few big teeth.
            var hem = new float[cols];
            for (int c = 0; c < cols; c++)
            {
                float r0 = (float)rng.NextDouble();
                hem[c] = ((c & 1) == 0 ? 0.55f + r0 * 0.6f : r0 * 0.25f) * jag;
            }
            // Folds: soft vertical waves across the width, deeper towards the hem.
            float phase = (float)rng.NextDouble() * 6f;
            var grid = new Vector3[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    float u = c / (float)(cols - 1) * 2f - 1f;  // across the back
                    float v = r / (float)(rows - 1);            // 0 top .. 1 hem
                    float width = Mathf.Lerp(topWidth, bottomWidth, v) * 0.5f;
                    float drop = v * length + (r == rows - 1 ? hem[c] : v * v * hem[c] * 0.3f);
                    // Wraps around the shoulders at the top, hangs flatter and further back lower down.
                    float back = -(wrap * (1f - u * u) * (1f - 0.5f * v)) - v * 0.1f;
                    float fold = Mathf.Sin(u * 9f + phase) * 0.018f * v + Mathf.Sin(u * 17f + phase * 2f) * 0.006f * v;
                    grid[r, c] = top + new Vector3(-u * width, -drop, back + fold);
                }
            return Sheet(grid, Vector3.back, thickness);
        }

        /// <summary>A point on the cape's surface (u -1..1 across, v 0 top..1 hem, ignoring the ragged hem), for trims and patterns.</summary>
        public static Vector3 CapePoint(Vector3 top, float topWidth, float bottomWidth, float length, float wrap, float u, float v)
        {
            float width = Mathf.Lerp(topWidth, bottomWidth, v) * 0.5f;
            float back = -(wrap * (1f - u * u) * (1f - 0.5f * v)) - v * 0.1f;
            return top + new Vector3(-u * width, -v * length, back);
        }

        /// <summary>A zig-zag line (embroidery) through a list of points, alternating up and down by <paramref name="amplitude"/>.</summary>
        public static MeshData ZigZag(Vector3[] along, Vector3 up, float amplitude, float radius)
        {
            var pts = new Vector3[along.Length];
            var radii = new float[along.Length];
            for (int i = 0; i < along.Length; i++) { pts[i] = along[i] + up * ((i & 1) == 0 ? amplitude : -amplitude); radii[i] = radius; }
            return MeshData.Tube(pts, radii, 4);
        }

        /// <summary>
        /// Loose flaps of pelt or torn cloth hanging around a body at a height: separate ragged pieces with gaps
        /// between them, each following a surface given by <paramref name="radiusAt"/> (x radius at a height).
        /// </summary>
        public static MeshData Flaps(float topY, float length, int count, float coverage, float depth, System.Func<float, float> radiusAt, float lift, int seed, float thickness)
        {
            var rng = new System.Random(seed);
            var m = new MeshData();
            for (int i = 0; i < count; i++)
            {
                float centre = (i + (float)rng.NextDouble() * 0.6f) / count * Mathf.PI * 2f;
                float half = coverage * Mathf.PI / count * (0.7f + (float)rng.NextDouble() * 0.6f);
                float len = length * (0.6f + (float)rng.NextDouble() * 0.7f);
                float top = topY + ((float)rng.NextDouble() - 0.5f) * length * 0.3f;
                const int rows = 4, cols = 7;
                var grid = new Vector3[rows, cols];
                var hang = new float[cols];
                for (int c = 0; c < cols; c++)
                {
                    float edge = Mathf.Abs(c / (float)(cols - 1) * 2f - 1f);
                    // Zig-zag torn hem: long points and short notches, shorter towards the sides.
                    hang[c] = len * (((c & 1) == 0 ? 0.95f : 0.62f) + (float)rng.NextDouble() * 0.25f) * (1f - 0.35f * edge * edge);
                }
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < cols; c++)
                    {
                        float v = r / (float)(rows - 1), u = c / (float)(cols - 1) * 2f - 1f;
                        float a = centre + u * half * (1f - 0.25f * v);
                        float y = top - v * hang[c];
                        float rad = radiusAt(y) + lift + v * lift * 0.8f + v * v * 0.012f;
                        grid[r, c] = new Vector3(Mathf.Sin(a) * rad, y, Mathf.Cos(a) * rad * depth);
                    }
                Vector3 outward = new Vector3(Mathf.Sin(centre), 0f, Mathf.Cos(centre));
                m.Append(Sheet(grid, outward, thickness));
            }
            return m;
        }

        /// <summary>A braid: a chain of lumps along a path, with a bead near the end.</summary>
        public static MeshData Braid(Vector3[] path, float radius)
        {
            var m = new MeshData();
            float total = 0f;
            for (int i = 1; i < path.Length; i++) total += Vector3.Distance(path[i - 1], path[i]);
            // Overlapping lumps that lean alternately left and right, like the crossing strands of a braid,
            // over a thinner core so there are no gaps.
            int lumps = Mathf.Max(3, Mathf.RoundToInt(total / (radius * 1.05f)));
            var core = new Vector3[Mathf.Max(2, lumps / 2)];
            var coreR = new float[core.Length];
            for (int k = 0; k < core.Length; k++) { float t = k / (float)(core.Length - 1); core[k] = Along(path, t); coreR[k] = radius * Mathf.Lerp(0.7f, 0.45f, t); }
            m.Append(MeshData.Tube(core, coreR, 7));
            for (int k = 0; k < lumps; k++)
            {
                float t = k / (float)(lumps - 1);
                Vector3 p = Along(path, t);
                Vector3 dir = (Along(path, Mathf.Min(1f, t + 0.02f)) - Along(path, Mathf.Max(0f, t - 0.02f))).normalized;
                if (dir.sqrMagnitude < 0.5f) dir = Vector3.down;
                Vector3 side = Vector3.Cross(dir, Vector3.forward);
                if (side.sqrMagnitude < 0.01f) side = Vector3.right;
                side.Normalize();
                float r = radius * Mathf.Lerp(1f, 0.65f, t);
                float lean = (k & 1) == 0 ? 1f : -1f;
                var lump = MeshData.Ellipsoid(Vector3.zero, new Vector3(r * 0.85f, r * 1.35f, r * 0.8f), 8, 5);
                var rot = Quaternion.FromToRotation(Vector3.up, dir) * Quaternion.Euler(0f, 0f, 28f * lean);
                m.Append(lump.Transformed(p + side * lean * r * 0.28f, rot, Vector3.one));
            }
            return m;
        }

        /// <summary>A point a fraction of the way along a polyline.</summary>
        public static Vector3 Along(Vector3[] path, float t)
        {
            float total = 0f;
            for (int i = 1; i < path.Length; i++) total += Vector3.Distance(path[i - 1], path[i]);
            float want = Mathf.Clamp01(t) * total;
            for (int i = 1; i < path.Length; i++)
            {
                float seg = Vector3.Distance(path[i - 1], path[i]);
                if (want <= seg || i == path.Length - 1) return Vector3.Lerp(path[i - 1], path[i], seg > 0f ? Mathf.Clamp01(want / seg) : 0f);
                want -= seg;
            }
            return path[path.Length - 1];
        }

        /// <summary>A ring band (belts, trims, bracelets) around the Y axis, elliptical.</summary>
        public static MeshData Band(float y, float height, float radius, float depth, int segments = 18)
        {
            return MeshData.Lathe(new[] { new Vector2(radius, y - height / 2f), new Vector2(radius, y + height / 2f) }, segments)
                .Transformed(Vector3.zero, Quaternion.identity, new Vector3(1f, 1f, depth));
        }

        /// <summary>A strap wound around a boot or a leg in a spiral, from one height to another.</summary>
        public static MeshData Spiral(float fromY, float toY, float radius, float turns, float phase, float thickness)
        {
            int n = Mathf.Max(8, Mathf.RoundToInt(turns * 14f));
            var path = new Vector3[n + 1];
            var radii = new float[n + 1];
            for (int i = 0; i <= n; i++)
            {
                float t = i / (float)n;
                float a = phase + t * turns * Mathf.PI * 2f;
                path[i] = new Vector3(Mathf.Sin(a) * radius, Mathf.Lerp(fromY, toY, t), Mathf.Cos(a) * radius);
                radii[i] = thickness;
            }
            return MeshData.Tube(path, radii, 5);
        }
    }
}
