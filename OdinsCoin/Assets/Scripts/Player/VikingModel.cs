using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>Plain triangle data (no Unity objects), so models can be built, tested and previewed outside Unity.</summary>
    public class MeshData
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<int> Triangles = new List<int>();
        /// <summary>Texture coordinates for the drawn textures, in tiles (about four per metre).</summary>
        public readonly List<Vector2> Uvs = new List<Vector2>();

        /// <summary>Texture tiles per metre.</summary>
        public const float UvScale = 4f;

        /// <summary>Gives any vertex without texture coordinates a flat projection of where it was made.</summary>
        public void FillUvs()
        {
            for (int i = Uvs.Count; i < Vertices.Count; i++)
            {
                var v = Vertices[i];
                Uvs.Add(new Vector2((v.x + v.z) * UvScale, v.y * UvScale));
            }
            if (Uvs.Count > Vertices.Count) Uvs.RemoveRange(Vertices.Count, Uvs.Count - Vertices.Count);
        }

        public void Append(MeshData other)
        {
            FillUvs();
            other.FillUvs();
            int start = Vertices.Count;
            Vertices.AddRange(other.Vertices);
            Uvs.AddRange(other.Uvs);
            foreach (int t in other.Triangles) Triangles.Add(start + t);
        }

        /// <summary>Move, turn and stretch everything (scale first, then rotate, then move).</summary>
        public MeshData Transformed(Vector3 position, Quaternion rotation, Vector3 scale)
        {
            FillUvs();
            var m = new MeshData();
            foreach (var v in Vertices) m.Vertices.Add(position + rotation * Vector3.Scale(v, scale));
            m.Triangles.AddRange(Triangles);
            m.Uvs.AddRange(Uvs);
            return m;
        }

        public MeshData Moved(Vector3 position) { return Transformed(position, Quaternion.identity, Vector3.one); }

        /// <summary>Vertex normals averaged over every face touching the same position (welds hard edges too).</summary>
        public Vector3[] WeldedNormals()
        {
            var sum = new Dictionary<long, Vector3>();
            var keys = new long[Vertices.Count];
            for (int i = 0; i < Vertices.Count; i++)
            {
                var v = Vertices[i];
                keys[i] = ((long)Mathf.RoundToInt(v.x * 2000f) * 73856093L) ^ ((long)Mathf.RoundToInt(v.y * 2000f) * 19349663L) ^ ((long)Mathf.RoundToInt(v.z * 2000f) * 83492791L);
            }
            for (int t = 0; t < Triangles.Count; t += 3)
            {
                int a = Triangles[t], b = Triangles[t + 1], c = Triangles[t + 2];
                Vector3 n = Vector3.Cross(Vertices[b] - Vertices[a], Vertices[c] - Vertices[a]);
                foreach (int i in new[] { a, b, c })
                {
                    Vector3 cur;
                    sum.TryGetValue(keys[i], out cur);
                    sum[keys[i]] = cur + n;
                }
            }
            var normals = new Vector3[Vertices.Count];
            for (int i = 0; i < normals.Length; i++)
            {
                Vector3 n;
                sum.TryGetValue(keys[i], out n);
                normals[i] = n.sqrMagnitude > 1e-12f ? n.normalized : Vector3.up;
            }
            return normals;
        }

        /// <summary>An inside-out copy pushed out along the normals by <paramref name="width"/>: an ink outline shell.</summary>
        public MeshData Outline(float width)
        {
            var normals = WeldedNormals();
            var m = new MeshData();
            for (int i = 0; i < Vertices.Count; i++) m.Vertices.Add(Vertices[i] + normals[i] * width);
            for (int t = 0; t < Triangles.Count; t += 3)
            {
                m.Triangles.Add(Triangles[t]); m.Triangles.Add(Triangles[t + 2]); m.Triangles.Add(Triangles[t + 1]);
            }
            return m;
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh();
            mesh.name = name;
            FillUvs();
            mesh.vertices = Vertices.ToArray();
            mesh.triangles = Triangles.ToArray();
            mesh.uv = Uvs.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        // ---------------------------------------------------------------- shapes

        /// <summary>
        /// A body of revolution around the Y axis: <paramref name="profile"/> is (radius, height) from bottom to top.
        /// Rings share vertices, so it shades smoothly. The ends are capped.
        /// </summary>
        public static MeshData Lathe(Vector2[] profile, int segments)
        {
            var m = new MeshData();
            for (int r = 0; r < profile.Length; r++)
                for (int s = 0; s < segments; s++)
                {
                    float a = s / (float)segments * Mathf.PI * 2f;
                    m.Vertices.Add(new Vector3(Mathf.Sin(a) * profile[r].x, profile[r].y, Mathf.Cos(a) * profile[r].x));
                    m.Uvs.Add(new Vector2(s / (float)segments * 4f, profile[r].y * UvScale));
                }
            for (int r = 0; r < profile.Length - 1; r++)
                for (int s = 0; s < segments; s++)
                {
                    int a = r * segments + s, b = r * segments + (s + 1) % segments;
                    int c = a + segments, d = b + segments;
                    m.Triangles.Add(a); m.Triangles.Add(b); m.Triangles.Add(c);
                    m.Triangles.Add(b); m.Triangles.Add(d); m.Triangles.Add(c);
                }
            Cap(m, 0, segments, profile[0].y, false);
            Cap(m, (profile.Length - 1) * segments, segments, profile[profile.Length - 1].y, true);
            return m;
        }

        static void Cap(MeshData m, int ring, int segments, float y, bool top)
        {
            if (m.Vertices[ring].sqrMagnitude - y * y < 1e-8f) return; // closed to a point already
            int centre = m.Vertices.Count;
            m.Vertices.Add(new Vector3(0f, y, 0f));
            for (int s = 0; s < segments; s++)
            {
                int a = ring + s, b = ring + (s + 1) % segments;
                if (top) { m.Triangles.Add(centre); m.Triangles.Add(a); m.Triangles.Add(b); }
                else { m.Triangles.Add(centre); m.Triangles.Add(b); m.Triangles.Add(a); }
            }
        }

        /// <summary>A smooth ellipsoid of the given radii.</summary>
        public static MeshData Ellipsoid(Vector3 centre, Vector3 radii, int segments = 12, int rings = 8)
        {
            var profile = new Vector2[rings + 1];
            for (int i = 0; i <= rings; i++)
            {
                float a = -Mathf.PI / 2f + Mathf.PI * i / rings;
                profile[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            }
            return Lathe(profile, segments).Transformed(centre, Quaternion.identity, radii);
        }

        /// <summary>The top half of an ellipsoid (helmets, bosses), open side down and capped.</summary>
        public static MeshData Dome(Vector3 centre, Vector3 radii, int segments = 14, int rings = 5)
        {
            var profile = new Vector2[rings + 1];
            for (int i = 0; i <= rings; i++)
            {
                float a = Mathf.PI / 2f * i / rings;
                profile[i] = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            }
            return Lathe(profile, segments).Transformed(centre, Quaternion.identity, radii);
        }

        /// <summary>A tapered tube: rings of the given radii along a path of points, capped at both ends.</summary>
        public static MeshData Tube(Vector3[] path, float[] radii, int segments = 8)
        {
            var m = new MeshData();
            float along = 0f;
            for (int i = 0; i < path.Length; i++)
            {
                if (i > 0) along += Vector3.Distance(path[i - 1], path[i]);
                Vector3 dir = (i < path.Length - 1 ? path[i + 1] - path[i] : path[i] - path[i - 1]).normalized;
                Vector3 side = Vector3.Cross(dir, Mathf.Abs(dir.y) > 0.9f ? Vector3.forward : Vector3.up).normalized;
                Vector3 up = Vector3.Cross(side, dir);
                for (int s = 0; s < segments; s++)
                {
                    float a = s / (float)segments * Mathf.PI * 2f;
                    m.Vertices.Add(path[i] + (side * Mathf.Cos(a) + up * Mathf.Sin(a)) * radii[i]);
                    m.Uvs.Add(new Vector2(s / (float)segments * 2f, along * UvScale));
                }
            }
            for (int r = 0; r < path.Length - 1; r++)
                for (int s = 0; s < segments; s++)
                {
                    int a = r * segments + s, b = r * segments + (s + 1) % segments;
                    int c = a + segments, d = b + segments;
                    m.Triangles.Add(a); m.Triangles.Add(c); m.Triangles.Add(b);
                    m.Triangles.Add(b); m.Triangles.Add(c); m.Triangles.Add(d);
                }
            for (int end = 0; end < 2; end++)
            {
                int ring = end == 0 ? 0 : (path.Length - 1) * segments;
                int centre = m.Vertices.Count;
                m.Vertices.Add(path[end == 0 ? 0 : path.Length - 1]);
                for (int s = 0; s < segments; s++)
                {
                    int a = ring + s, b = ring + (s + 1) % segments;
                    if (end == 0) { m.Triangles.Add(centre); m.Triangles.Add(a); m.Triangles.Add(b); }
                    else { m.Triangles.Add(centre); m.Triangles.Add(b); m.Triangles.Add(a); }
                }
            }
            return m;
        }

        /// <summary>A box with its own vertices per face (hard edges).</summary>
        public static MeshData Box(Vector3 centre, Vector3 size)
        {
            var m = new MeshData();
            Vector3 h = size * 0.5f;
            Vector3[] n = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
            foreach (var f in n)
            {
                Vector3 u = Mathf.Abs(f.y) > 0.5f ? Vector3.right : Vector3.up;
                Vector3 v = Vector3.Cross(f, u);
                int i = m.Vertices.Count;
                m.Vertices.Add(centre + Vector3.Scale(f + u + v, h));
                m.Vertices.Add(centre + Vector3.Scale(f + u - v, h));
                m.Vertices.Add(centre + Vector3.Scale(f - u - v, h));
                m.Vertices.Add(centre + Vector3.Scale(f - u + v, h));
                m.Triangles.Add(i); m.Triangles.Add(i + 2); m.Triangles.Add(i + 1);
                m.Triangles.Add(i); m.Triangles.Add(i + 3); m.Triangles.Add(i + 2);
            }
            return m;
        }

        /// <summary>A flat outline (in the Y-Z plane) pushed out along X: axe heads, blades.</summary>
        public static MeshData Extrude(Vector2[] outline, float thickness)
        {
            var m = new MeshData();
            int n = outline.Length;
            float hx = thickness / 2f;
            // Work with a counter-clockwise outline (Z right, Y up) whichever way it was drawn.
            float area = 0f;
            for (int i = 0; i < n; i++) { var p0 = outline[i]; var p1 = outline[(i + 1) % n]; area += p0.x * p1.y - p1.x * p0.y; }
            if (area < 0f) { outline = (Vector2[])outline.Clone(); System.Array.Reverse(outline); }
            // Faces (fan from the first point; the outline must be convex-ish from there).
            for (int side = 0; side < 2; side++)
            {
                int start = m.Vertices.Count;
                float x = side == 0 ? hx : -hx;
                foreach (var p in outline) m.Vertices.Add(new Vector3(x, p.y, p.x));
                for (int i = 1; i < n - 1; i++)
                {
                    if (side == 0) { m.Triangles.Add(start); m.Triangles.Add(start + i + 1); m.Triangles.Add(start + i); }
                    else { m.Triangles.Add(start); m.Triangles.Add(start + i); m.Triangles.Add(start + i + 1); }
                }
            }
            // Edges.
            for (int i = 0; i < n; i++)
            {
                var a = outline[i];
                var b = outline[(i + 1) % n];
                int s = m.Vertices.Count;
                m.Vertices.Add(new Vector3(hx, a.y, a.x));
                m.Vertices.Add(new Vector3(hx, b.y, b.x));
                m.Vertices.Add(new Vector3(-hx, b.y, b.x));
                m.Vertices.Add(new Vector3(-hx, a.y, a.x));
                m.Triangles.Add(s); m.Triangles.Add(s + 1); m.Triangles.Add(s + 2);
                m.Triangles.Add(s); m.Triangles.Add(s + 2); m.Triangles.Add(s + 3);
            }
            return m;
        }

        /// <summary>A pie slice of a thick disc, lying in the X-Y plane and facing +Z (shield boards).</summary>
        public static MeshData Wedge(float radius, float thickness, float fromDeg, float toDeg, int segments)
        {
            var m = new MeshData();
            float hz = thickness / 2f;
            for (int side = 0; side < 2; side++)
            {
                float z = side == 0 ? hz : -hz;
                int centre = m.Vertices.Count;
                m.Vertices.Add(new Vector3(0f, 0f, z));
                for (int s = 0; s <= segments; s++)
                {
                    float a = Mathf.Lerp(fromDeg, toDeg, s / (float)segments) * Mathf.Deg2Rad;
                    m.Vertices.Add(new Vector3(Mathf.Cos(a) * radius, Mathf.Sin(a) * radius, z));
                }
                for (int s = 0; s < segments; s++)
                {
                    if (side == 0) { m.Triangles.Add(centre); m.Triangles.Add(centre + s + 1); m.Triangles.Add(centre + s + 2); }
                    else { m.Triangles.Add(centre); m.Triangles.Add(centre + s + 2); m.Triangles.Add(centre + s + 1); }
                }
            }
            // Rim.
            int front = 1, back = segments + 3;
            for (int s = 0; s < segments; s++)
            {
                int a = front + s, b = front + s + 1, c = back + s + 1, d = back + s;
                m.Triangles.Add(a); m.Triangles.Add(c); m.Triangles.Add(b);
                m.Triangles.Add(a); m.Triangles.Add(d); m.Triangles.Add(c);
            }
            return m;
        }
    }

    /// <summary>How a Viking (or a Saxon) looks. Everyone is built from the same model with different colours.</summary>
    public class VikingLook
    {
        public Color skin = new Color(0.93f, 0.74f, 0.6f);
        public Color tunic = new Color(0.25f, 0.4f, 0.6f);
        public Color trousers = new Color(0.36f, 0.33f, 0.27f);
        public Color leather = new Color(0.36f, 0.24f, 0.14f);
        public Color hair = new Color(0.78f, 0.42f, 0.16f);
        public Color fur = new Color(0.52f, 0.43f, 0.33f);
        public Color iron = new Color(0.55f, 0.56f, 0.6f);
        public Color shield = new Color(0.75f, 0.15f, 0.12f);
        public Color shieldStripe = new Color(0.93f, 0.88f, 0.76f);
        public bool helmet = true;
        public bool horns;
        public bool sword;
        /// <summary>0 = stubble, 1 = a proper Viking beard down to the chest.</summary>
        public float beardLength = 1f;
        /// <summary>Overall size: 1 = 1.9 m.</summary>
        public float size = 1f;
    }

    /// <summary>
    /// The Viking as data: joints (hips, shoulders, neck, hand, back) and the smooth low-poly pieces hanging
    /// from each, grouped by colour. <see cref="VikingBuilder"/> turns it into GameObjects; the preview tool
    /// renders it to a picture and an .obj file.
    /// </summary>
    public class VikingModel
    {
        public class Joint
        {
            public string name, parent;
            public Vector3 localPosition;
            /// <summary>Built but switched off at the start (alternative faces, for example).</summary>
            public bool hidden;
        }

        /// <summary>Whether a joint, or any joint it hangs from, starts switched off.</summary>
        public bool Hidden(string joint)
        {
            for (var j = Find(joint); j != null; j = j.parent == null ? null : Find(j.parent))
                if (j.hidden) return true;
            return false;
        }

        public class Piece
        {
            public string joint;
            public Color color;
            public MeshData mesh;
            /// <summary>What it's made of, which picks the drawn texture.</summary>
            public SurfaceKind surface;
            /// <summary>Gets an ink outline (small details like eyes and thin trims don't).</summary>
            public bool outline = true;
            /// <summary>This piece is itself an ink outline shell.</summary>
            public bool ink;
        }

        public readonly List<Joint> Joints = new List<Joint>();
        public readonly List<Piece> Pieces = new List<Piece>();

        /// <summary>A joint that swings on a spring (capes, banners, braids).</summary>
        public class Swing
        {
            public string joint;
            public SwingKind kind;
        }

        public readonly List<Swing> Swings = new List<Swing>();

        public const string Body = "Body", Head = "Head", LeftLeg = "Left Leg", RightLeg = "Right Leg",
            LeftArm = "Left Arm", RightArm = "Right Arm", Weapon = "Weapon", Shield = "Shield";

        public const float HipHeight = 0.9f, ShoulderHeight = 1.46f, NeckHeight = 1.56f, ShoulderWidth = 0.34f, ArmLength = 0.62f;

        public Joint Find(string name)
        {
            foreach (var j in Joints) if (j.name == name) return j;
            return null;
        }

        /// <summary>Where a joint sits in the model's own space (all joints are only offset, never turned, at rest).</summary>
        public Vector3 RestPosition(string name)
        {
            Vector3 p = Vector3.zero;
            for (var j = Find(name); j != null; j = j.parent == null ? null : Find(j.parent)) p += j.localPosition;
            return p;
        }

        public void AddJoint(string name, string parent, Vector3 local) { Joints.Add(new Joint { name = name, parent = parent, localPosition = local }); }

        /// <summary>Add a piece to a joint, merging with others of the same colour (one mesh and material per colour).</summary>
        public void Add(string joint, Color color, MeshData mesh) { Add(joint, color, mesh, true); }

        public void Add(string joint, Color color, MeshData mesh, bool outline) { Add(joint, color, mesh, outline, SurfaceKind.Plain); }

        public void Add(string joint, Color color, MeshData mesh, bool outline, SurfaceKind surface)
        {
            foreach (var p in Pieces)
                if (p.joint == joint && p.color.Equals(color) && p.outline == outline && p.surface == surface && !p.ink) { p.mesh.Append(mesh); return; }
            var piece = new Piece { joint = joint, color = color, mesh = new MeshData(), outline = outline, surface = surface };
            piece.mesh.Append(mesh);
            Pieces.Add(piece);
        }

        /// <summary>
        /// The ink line around everything: for each piece a slightly inflated copy turned inside out, so only its
        /// far side draws, peeking out around the edges (the "inverted hull" trick; needs no special shader).
        /// </summary>
        public void AddOutlines(float width, Color ink)
        {
            var shells = new Dictionary<string, MeshData>();
            foreach (var p in Pieces)
            {
                if (!p.outline || p.ink) continue;
                MeshData shell;
                if (!shells.TryGetValue(p.joint, out shell)) { shell = new MeshData(); shells[p.joint] = shell; }
                shell.Append(p.mesh.Outline(width));
            }
            foreach (var kv in shells) Pieces.Add(new Piece { joint = kv.Key, color = ink, mesh = kv.Value, outline = false, ink = true });
        }

        public int TriangleCount
        {
            get
            {
                int n = 0;
                foreach (var p in Pieces) n += p.mesh.Triangles.Count / 3;
                return n;
            }
        }

        static Vector2 P(float radius, float y) { return new Vector2(radius, y); }

        /// <summary>Darker or lighter, keeping the colour opaque (Color * float would scale alpha too).</summary>
        public static Color Shade(Color c, float k) { return new Color(Mathf.Clamp01(c.r * k), Mathf.Clamp01(c.g * k), Mathf.Clamp01(c.b * k), 1f); }

        /// <summary>A lathe stretched front-to-back (depth &lt; 1 flattens it).</summary>
        static MeshData Oval(Vector2[] profile, int segments, float depth, Vector3 at)
        {
            return MeshData.Lathe(profile, segments).Transformed(at, Quaternion.identity, new Vector3(1f, 1f, depth));
        }

        public static VikingModel Build(VikingLook look)
        {
            var m = new VikingModel();
            float k = look.size;
            m.AddJoint(Body, null, Vector3.zero);
            m.AddJoint(LeftLeg, Body, new Vector3(-0.12f, HipHeight, 0f) * k);
            m.AddJoint(RightLeg, Body, new Vector3(0.12f, HipHeight, 0f) * k);
            m.AddJoint(LeftArm, Body, new Vector3(-ShoulderWidth, ShoulderHeight, 0f) * k);
            m.AddJoint(RightArm, Body, new Vector3(ShoulderWidth, ShoulderHeight, 0f) * k);
            m.AddJoint(Head, Body, new Vector3(0f, NeckHeight, 0f) * k);
            m.AddJoint(Weapon, RightArm, new Vector3(0f, -ArmLength, 0.03f) * k);
            m.AddJoint(Shield, Body, new Vector3(0f, 1.2f, -0.24f) * k);

            m.BuildTorso(look);
            m.BuildLeg(LeftLeg, look);
            m.BuildLeg(RightLeg, look);
            m.BuildArm(LeftArm, look, -1f);
            m.BuildArm(RightArm, look, 1f);
            m.BuildHead(look);
            if (look.sword) m.BuildSword(look); else m.BuildAxe(look);
            m.BuildShield(look);

            if (k != 1f)
                foreach (var p in m.Pieces)
                {
                    var scaled = p.mesh.Transformed(Vector3.zero, Quaternion.identity, Vector3.one * k);
                    p.mesh.Vertices.Clear();
                    p.mesh.Vertices.AddRange(scaled.Vertices);
                }
            return m;
        }

        void BuildTorso(VikingLook look)
        {
            // Tunic: a barrel chest, narrower waist, flaring into a skirt over the thighs.
            Add(Body, look.tunic, Oval(new[] {
                P(0.25f, 0.62f), P(0.24f, 0.72f), P(0.21f, 0.86f), P(0.2f, 0.98f), P(0.23f, 1.16f),
                P(0.26f, 1.32f), P(0.25f, 1.44f), P(0.18f, 1.52f), P(0.08f, 1.56f) }, 16, 0.72f, Vector3.zero));
            // Leather belt with a gold buckle, and a pouch on the hip.
            Add(Body, look.leather, Oval(new[] { P(0.215f, 0.9f), P(0.215f, 0.97f) }, 16, 0.76f, Vector3.zero));
            Add(Body, new Color(0.85f, 0.68f, 0.28f), MeshData.Box(new Vector3(0f, 0.935f, 0.165f), new Vector3(0.08f, 0.06f, 0.02f)));
            Add(Body, look.leather, MeshData.Ellipsoid(new Vector3(0.17f, 0.86f, 0.08f), new Vector3(0.05f, 0.07f, 0.035f), 8, 6));
            // Tunic trim along the hem.
            Add(Body, Shade(look.tunic, 0.7f), Oval(new[] { P(0.252f, 0.62f), P(0.252f, 0.66f) }, 16, 0.72f, Vector3.zero));
            // Fur mantle over the shoulders.
            Add(Body, look.fur, MeshData.Ellipsoid(new Vector3(0f, 1.44f, -0.02f), new Vector3(0.34f, 0.1f, 0.22f), 14, 6));
            // Neck.
            Add(Body, look.skin, MeshData.Lathe(new[] { P(0.07f, 1.5f), P(0.065f, 1.64f) }, 10));
        }

        void BuildLeg(string joint, VikingLook look)
        {
            float top = 0f, knee = -0.42f, ankle = -0.74f, sole = -HipHeight;
            // Baggy trousers, tapering to the knee and shin.
            Add(joint, look.trousers, MeshData.Lathe(new[] { P(0.07f, ankle + 0.02f), P(0.075f, knee), P(0.1f, knee + 0.12f), P(0.115f, top - 0.1f), P(0.11f, top + 0.02f) }, 12));
            // Leg wraps (winingas) from knee to ankle.
            for (int i = 0; i < 4; i++)
            {
                float y = ankle + 0.04f + i * 0.075f;
                Add(joint, Shade(look.leather, 1.15f), MeshData.Lathe(new[] { P(0.08f, y), P(0.083f, y + 0.035f) }, 12));
            }
            // Boot with a turned-up toe.
            Add(joint, look.leather, MeshData.Lathe(new[] { P(0.07f, sole), P(0.085f, sole + 0.06f), P(0.08f, ankle + 0.03f) }, 12));
            Add(joint, look.leather, MeshData.Ellipsoid(new Vector3(0f, sole + 0.05f, 0.08f), new Vector3(0.075f, 0.055f, 0.14f), 10, 6));
        }

        void BuildArm(string joint, VikingLook look, float side)
        {
            // Shoulder, sleeve, bare forearm with a leather bracer, and a fist.
            Add(joint, look.tunic, MeshData.Ellipsoid(new Vector3(0f, -0.02f, 0f), new Vector3(0.1f, 0.1f, 0.1f), 10, 6));
            Add(joint, look.tunic, MeshData.Lathe(new[] { P(0.07f, -0.32f), P(0.085f, -0.15f), P(0.09f, -0.02f) }, 10));
            Add(joint, look.skin, MeshData.Lathe(new[] { P(0.05f, -0.54f), P(0.065f, -0.33f), P(0.07f, -0.3f) }, 10));
            Add(joint, look.leather, MeshData.Lathe(new[] { P(0.058f, -0.52f), P(0.068f, -0.4f) }, 10));
            Add(joint, look.skin, MeshData.Ellipsoid(new Vector3(0f, -0.59f, 0.01f), new Vector3(0.05f, 0.065f, 0.058f), 10, 6));
            // A silver arm ring on the upper arm.
            Add(joint, new Color(0.82f, 0.82f, 0.85f), MeshData.Lathe(new[] { P(0.088f, -0.22f), P(0.088f, -0.19f) }, 10));
        }

        void BuildHead(VikingLook look)
        {
            // Local to the neck.
            Add(Head, look.skin, MeshData.Ellipsoid(new Vector3(0f, 0.2f, 0.01f), new Vector3(0.14f, 0.17f, 0.155f), 14, 10));
            Add(Head, Shade(look.skin, 0.95f), MeshData.Ellipsoid(new Vector3(0f, 0.19f, 0.16f), new Vector3(0.033f, 0.05f, 0.045f), 8, 6));   // nose
            Add(Head, look.skin, MeshData.Ellipsoid(new Vector3(-0.14f, 0.19f, 0.0f), new Vector3(0.025f, 0.045f, 0.03f), 6, 4));       // ears
            var cheek = new Color(Mathf.Clamp01(look.skin.r * 1.02f), look.skin.g * 0.82f, look.skin.b * 0.78f);
            Add(Head, cheek, MeshData.Ellipsoid(new Vector3(-0.08f, 0.19f, 0.12f), new Vector3(0.035f, 0.025f, 0.025f), 6, 4));
            Add(Head, cheek, MeshData.Ellipsoid(new Vector3(0.08f, 0.19f, 0.12f), new Vector3(0.035f, 0.025f, 0.025f), 6, 4));
            Add(Head, look.skin, MeshData.Ellipsoid(new Vector3(0.14f, 0.19f, 0.0f), new Vector3(0.025f, 0.045f, 0.03f), 6, 4));
            // Eyes: white with a blue iris, under bushy brows.
            foreach (float x in new[] { -0.055f, 0.055f })
            {
                Add(Head, new Color(0.95f, 0.95f, 0.93f), MeshData.Ellipsoid(new Vector3(x, 0.232f, 0.143f), new Vector3(0.03f, 0.024f, 0.02f), 8, 5));
                Add(Head, new Color(0.2f, 0.4f, 0.65f), MeshData.Ellipsoid(new Vector3(x, 0.232f, 0.16f), new Vector3(0.014f, 0.016f, 0.008f), 6, 4));
                Add(Head, new Color(0.08f, 0.08f, 0.1f), MeshData.Ellipsoid(new Vector3(x, 0.232f, 0.166f), new Vector3(0.006f, 0.007f, 0.004f), 5, 3));
                Add(Head, Shade(look.hair, 0.85f), MeshData.Box(new Vector3(x, 0.262f, 0.155f), new Vector3(0.068f, 0.02f, 0.025f)));
            }

            // Hair at the back, with two braids down the neck.
            Add(Head, look.hair, MeshData.Ellipsoid(new Vector3(0f, 0.2f, -0.04f), new Vector3(0.155f, 0.17f, 0.14f), 12, 8));
            foreach (float x in new[] { -0.09f, 0.09f })
                Add(Head, look.hair, MeshData.Tube(new[] { new Vector3(x, 0.14f, -0.1f), new Vector3(x * 1.2f, 0.0f, -0.13f), new Vector3(x * 1.25f, -0.14f, -0.12f) }, new[] { 0.035f, 0.03f, 0.02f }, 6));

            // Beard: a big wedge from the cheeks to the chest, a moustache, and a braided tip with a gold bead.
            float len = Mathf.Lerp(0.04f, 0.3f, Mathf.Clamp01(look.beardLength));
            Add(Head, look.hair, MeshData.Lathe(new[] { P(0.02f, 0.13f - len), P(0.07f, 0.12f - len * 0.6f), P(0.115f, 0.09f), P(0.13f, 0.135f), P(0.1f, 0.165f) }, 12)
                .Transformed(new Vector3(0f, 0f, 0.07f), Quaternion.identity, new Vector3(1f, 1f, 0.62f)));
            Add(Head, look.hair, MeshData.Ellipsoid(new Vector3(-0.045f, 0.15f, 0.158f), new Vector3(0.05f, 0.018f, 0.025f), 8, 4));
            Add(Head, look.hair, MeshData.Ellipsoid(new Vector3(0.045f, 0.15f, 0.158f), new Vector3(0.05f, 0.018f, 0.025f), 8, 4));
            if (look.beardLength > 0.5f)
            {
                Add(Head, Shade(look.hair, 0.9f), MeshData.Tube(new[] { new Vector3(0f, 0.12f - len, 0.08f), new Vector3(0f, 0.04f - len, 0.09f) }, new[] { 0.025f, 0.018f }, 6));
                Add(Head, new Color(0.9f, 0.72f, 0.3f), MeshData.Lathe(new[] { P(0.024f, 0.06f - len), P(0.024f, 0.09f - len) }, 8).Moved(new Vector3(0f, 0f, 0.087f)));
            }

            if (!look.helmet) return;
            // Spangenhelm: an iron dome with a brow band, a crest ridge and a nose guard.
            Add(Head, look.iron, MeshData.Dome(new Vector3(0f, 0.27f, 0.005f), new Vector3(0.162f, 0.15f, 0.172f), 16, 6));
            Add(Head, Shade(look.iron, 0.75f), MeshData.Lathe(new[] { P(0.166f, 0.26f), P(0.168f, 0.3f) }, 16).Transformed(new Vector3(0f, 0f, 0.005f), Quaternion.identity, new Vector3(1f, 1f, 1.06f)));
            Add(Head, Shade(look.iron, 0.75f), MeshData.Tube(new[] { new Vector3(0f, 0.3f, 0.18f), new Vector3(0f, 0.395f, 0.1f), new Vector3(0f, 0.425f, 0f), new Vector3(0f, 0.395f, -0.1f), new Vector3(0f, 0.3f, -0.17f) }, new[] { 0.014f, 0.016f, 0.016f, 0.016f, 0.014f }, 6));
            Add(Head, Shade(look.iron, 0.75f), MeshData.Box(new Vector3(0f, 0.225f, 0.185f), new Vector3(0.028f, 0.12f, 0.02f)));
            if (look.horns)
                foreach (float x in new[] { -1f, 1f })
                    Add(Head, new Color(0.9f, 0.86f, 0.74f), MeshData.Tube(new[] {
                        new Vector3(x * 0.14f, 0.33f, 0f), new Vector3(x * 0.25f, 0.38f, 0.02f), new Vector3(x * 0.31f, 0.48f, 0.05f), new Vector3(x * 0.31f, 0.58f, 0.08f) },
                        new[] { 0.045f, 0.035f, 0.022f, 0.006f }, 8));
        }

        /// <summary>Weapons are held tilted a little down from straight ahead.</summary>
        static readonly Quaternion Grip = Quaternion.Euler(30f, 0f, 0f);

        void AddWeapon(Color color, MeshData mesh) { Add(Weapon, color, mesh.Transformed(Vector3.zero, Grip, Vector3.one)); }

        void BuildAxe(VikingLook look)
        {
            // Held in the fist, haft pointing forward: a long ash haft and a bearded iron head.
            AddWeapon(Shade(look.leather, 1.4f), MeshData.Tube(new[] { new Vector3(0f, 0f, -0.12f), new Vector3(0f, 0f, 0.66f) }, new[] { 0.022f, 0.02f }, 8));
            AddWeapon(look.leather, MeshData.Lathe(new[] { P(0.026f, 0f), P(0.026f, 0.12f) }, 8).Transformed(new Vector3(0f, 0f, -0.08f), Quaternion.Euler(90f, 0f, 0f), Vector3.one));
            AddWeapon(look.iron, MeshData.Extrude(new[] {
                new Vector2(0.54f, 0.035f), new Vector2(0.64f, 0.035f), new Vector2(0.68f, -0.03f), new Vector2(0.72f, -0.17f),
                new Vector2(0.62f, -0.14f), new Vector2(0.57f, -0.06f), new Vector2(0.54f, -0.035f) }, 0.025f));
            // A bright honed edge.
            AddWeapon(new Color(0.85f, 0.87f, 0.9f), MeshData.Extrude(new[] {
                new Vector2(0.675f, -0.02f), new Vector2(0.69f, -0.03f), new Vector2(0.73f, -0.175f), new Vector2(0.715f, -0.17f) }, 0.018f));
        }

        void BuildSword(VikingLook look)
        {
            AddWeapon(look.leather, MeshData.Tube(new[] { new Vector3(0f, 0f, -0.08f), new Vector3(0f, 0f, 0.06f) }, new[] { 0.02f, 0.02f }, 8));
            AddWeapon(Shade(look.iron, 0.8f), MeshData.Ellipsoid(new Vector3(0f, 0f, -0.1f), new Vector3(0.035f, 0.03f, 0.03f), 8, 5));   // pommel
            AddWeapon(Shade(look.iron, 0.8f), MeshData.Box(new Vector3(0f, 0f, 0.08f), new Vector3(0.18f, 0.035f, 0.035f)));             // guard
            AddWeapon(new Color(0.82f, 0.84f, 0.88f), MeshData.Extrude(new[] {
                new Vector2(0.1f, 0.022f), new Vector2(0.82f, 0.018f), new Vector2(0.88f, 0f), new Vector2(0.82f, -0.018f), new Vector2(0.1f, -0.022f) }, 0.01f)
                .Transformed(Vector3.zero, Quaternion.Euler(0f, 0f, 90f), Vector3.one));
        }

        void BuildShield(VikingLook look)
        {
            // Round shield of painted boards on the back, facing out: four quarters, an iron boss and a rawhide rim.
            var back = Quaternion.Euler(0f, 180f, 0f);
            for (int q = 0; q < 4; q++)
                Add(Shield, q % 2 == 0 ? look.shield : look.shieldStripe, MeshData.Wedge(0.38f, 0.03f, q * 90f + 45f, q * 90f + 135f, 6).Transformed(Vector3.zero, back, Vector3.one));
            Add(Shield, look.iron, MeshData.Dome(Vector3.zero, new Vector3(0.08f, 0.07f, 0.08f), 10, 4).Transformed(new Vector3(0f, 0f, -0.015f), Quaternion.Euler(-90f, 0f, 0f), Vector3.one));
            var rim = new Vector3[25];
            var radii = new float[25];
            for (int i = 0; i < rim.Length; i++)
            {
                float a = i / 24f * Mathf.PI * 2f;
                rim[i] = new Vector3(Mathf.Cos(a) * 0.38f, Mathf.Sin(a) * 0.38f, 0f);
                radii[i] = 0.02f;
            }
            Add(Shield, look.leather, MeshData.Tube(rim, radii, 6));
        }
    }
}
