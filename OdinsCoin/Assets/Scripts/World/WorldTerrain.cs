using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// One square of terrain as flat-shaded triangles (every triangle its own three vertices, so the facets catch
    /// the light like the sea), split by ground kind so each gets its own material. Where the ground is below
    /// the water it is drawn as a flat sheet just under the waterline in sea colour: the distant sea beyond the
    /// wave mesh. Pure data, so it can be tested.
    /// </summary>
    public class TerrainPatch
    {
        /// <summary>How far below the waterline the far sea sheet lies (so waves near the ship cover it).</summary>
        public const float SeaSheet = -1.2f;

        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<int>[] Triangles;

        public TerrainPatch()
        {
            Triangles = new List<int>[5];
            for (int i = 0; i < Triangles.Length; i++) Triangles[i] = new List<int>();
        }

        /// <summary>
        /// The patch whose south-west corner is at global (x0, z0), <paramref name="size"/> across in
        /// <paramref name="quads"/> squares; vertices are relative to that corner. <paramref name="sink"/> lowers
        /// it (for the far land, so the near chunks drawn over it win).
        /// </summary>
        public static TerrainPatch Build(WorldMap map, double x0, double z0, float size, int quads, float sink = 0f)
        {
            var p = new TerrainPatch();
            float step = size / quads, s = WorldMap.Scale;
            var h = new float[quads + 1, quads + 1];
            for (int j = 0; j <= quads; j++)
                for (int i = 0; i <= quads; i++)
                    h[i, j] = TerrainDetail.Height(map, x0 + i * step, z0 + j * step);
            for (int j = 0; j < quads; j++)
                for (int i = 0; i < quads; i++)
                {
                    bool flip = ((i + j) & 1) == 0;
                    var a = new Vector2Int(i, j); var b = new Vector2Int(i, j + 1); var c = new Vector2Int(i + 1, j + 1); var d = new Vector2Int(i + 1, j);
                    var tris = flip ? new[] { a, b, c, a, c, d } : new[] { a, b, d, b, c, d };
                    for (int t = 0; t < 6; t += 3)
                    {
                        float ha = h[tris[t].x, tris[t].y], hb = h[tris[t + 1].x, tris[t + 1].y], hc = h[tris[t + 2].x, tris[t + 2].y];
                        float mean = (ha + hb + hc) / 3f;
                        float slope = (Mathf.Max(ha, Mathf.Max(hb, hc)) - Mathf.Min(ha, Mathf.Min(hb, hc))) / step;
                        var kind = TerrainDetail.Kind(mean / s, slope);
                        var list = p.Triangles[(int)kind];
                        for (int k = 0; k < 3; k++)
                        {
                            var g = tris[t + k];
                            float y = h[g.x, g.y];
                            // Under the sea: a flat sheet just below the waterline.
                            if (kind == Ground.Seabed) y = SeaSheet * s;
                            else y = Mathf.Max(y, SeaSheet * s * 0.5f);
                            list.Add(p.Vertices.Count);
                            p.Vertices.Add(new Vector3(g.x * step, y - sink, g.y * step));
                        }
                    }
                }
            return p;
        }

        /// <summary>The colour of each kind of ground, in the storybook palette.</summary>
        public static Color ColourOf(Ground g)
        {
            switch (g)
            {
                case Ground.Seabed: return Materials.SeaDeep;
                case Ground.Sand: return Materials.Sand;
                case Ground.Grass: return Materials.Grass;
                case Ground.Rock: return Materials.Rock;
                default: return new Color(0.93f, 0.94f, 0.95f);
            }
        }

        public Mesh ToMesh(string name)
        {
            var mesh = new Mesh();
            mesh.name = name;
            if (Vertices.Count > 65000) mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = Vertices.ToArray();
            mesh.subMeshCount = Triangles.Length;
            for (int i = 0; i < Triangles.Length; i++) mesh.SetTriangles(Triangles[i].ToArray(), i);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    /// <summary>
    /// Streams the real land in around the player: detailed 1 km chunks close by, loaded as you sail and dropped
    /// behind you, over a coarse 1 km-grid sheet of the land out to the horizon (mountains 60 km away still
    /// show). Works in global coordinates and follows the floating origin (<see cref="WorldOrigin"/>).
    /// </summary>
    public class WorldTerrain : MonoBehaviour
    {
        /// <summary>Near chunk size and detail (squares per side).</summary>
        public const float ChunkSize = 1000f;
        public const int ChunkQuads = 40;
        /// <summary>How many chunks out from the player's chunk are kept.</summary>
        public const int Radius = 4;
        /// <summary>The far sheet: its half-size, square count, and how far the player may move before it's rebuilt.</summary>
        public const float FarHalf = 64000f, FarRebuild = 16000f;
        public const int FarQuads = 128;
        /// <summary>At most this many chunks are built per frame, so sailing into new land never stalls.</summary>
        public const int BuildsPerFrame = 2;

        public Transform follow;
        WorldMap map;
        readonly Dictionary<Vector2Int, Transform> chunks = new Dictionary<Vector2Int, Transform>();
        Transform far;
        double farX, farZ;
        Material[] materials;

        public static WorldTerrain Instance { get; private set; }

        /// <summary>Throw away the near chunks round a global spot so they're built again (the ground there changed).</summary>
        public void Rebuild(double x, double z, float radius)
        {
            float size = ChunkSize * WorldMap.Scale;
            var drop = new List<Vector2Int>();
            foreach (var kv in chunks)
            {
                double x0 = kv.Key.x * (double)size, z0 = kv.Key.y * (double)size;
                if (x + radius >= x0 && x - radius <= x0 + size && z + radius >= z0 && z - radius <= z0 + size) drop.Add(kv.Key);
            }
            foreach (var c in drop) { if (chunks[c] != null) Destroy(chunks[c].gameObject); chunks.Remove(c); }
        }

        public static WorldTerrain Create(Transform parent, WorldMap map, Transform follow)
        {
            var go = new GameObject("World Terrain");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<WorldTerrain>();
            Instance = t;
            t.map = map;
            t.follow = follow;
            t.materials = new Material[5];
            for (int i = 0; i < t.materials.Length; i++) t.materials[i] = Materials.Get(TerrainPatch.ColourOf((Ground)i));
            return t;
        }

        /// <summary>The chunk (in chunk units) holding a global position.</summary>
        public static Vector2Int ChunkOf(double x, double z, float chunkSize)
        {
            return new Vector2Int((int)System.Math.Floor(x / chunkSize), (int)System.Math.Floor(z / chunkSize));
        }

        /// <summary>The chunks that should be loaded around a chunk, nearest first.</summary>
        public static List<Vector2Int> Wanted(Vector2Int centre, int radius)
        {
            var list = new List<Vector2Int>();
            for (int dz = -radius; dz <= radius; dz++)
                for (int dx = -radius; dx <= radius; dx++)
                    if (dx * dx + dz * dz <= radius * radius + radius) list.Add(new Vector2Int(centre.x + dx, centre.y + dz));
            list.Sort((a, b) => ((a - centre).sqrMagnitude).CompareTo((b - centre).sqrMagnitude));
            return list;
        }

        void Update()
        {
            if (map == null || follow == null) return;
            double gx = WorldOrigin.GlobalX(follow.position), gz = WorldOrigin.GlobalZ(follow.position);
            float size = ChunkSize * WorldMap.Scale;
            var centre = ChunkOf(gx, gz, size);

            // Load what's missing, nearest first.
            int built = 0;
            foreach (var c in Wanted(centre, Radius))
            {
                if (chunks.ContainsKey(c)) continue;
                if (built++ >= BuildsPerFrame) break;
                var patch = TerrainPatch.Build(map, c.x * (double)size, c.y * (double)size, size, ChunkQuads);
                chunks[c] = Show(patch, "Chunk " + c.x + "," + c.y, true);
            }
            // Drop what's fallen well behind.
            var drop = new List<Vector2Int>();
            foreach (var kv in chunks)
                if ((kv.Key - centre).sqrMagnitude > (Radius + 2) * (Radius + 2)) drop.Add(kv.Key);
            foreach (var c in drop) { Destroy(chunks[c].gameObject); chunks.Remove(c); }

            // The far land: rebuilt around the player now and then.
            float farHalf = FarHalf * WorldMap.Scale;
            if (far == null || System.Math.Abs(gx - farX) > FarRebuild * WorldMap.Scale || System.Math.Abs(gz - farZ) > FarRebuild * WorldMap.Scale)
            {
                if (far != null) Destroy(far.gameObject);
                farX = System.Math.Round(gx / size) * size;
                farZ = System.Math.Round(gz / size) * size;
                var patch = TerrainPatch.Build(map, farX - farHalf, farZ - farHalf, farHalf * 2f, FarQuads, 25f * WorldMap.Scale);
                far = Show(patch, "Far Land", false);
            }

            Place();
        }

        void OnEnable() { WorldOrigin.Shifted += OnShift; }
        void OnDisable() { WorldOrigin.Shifted -= OnShift; }
        void OnShift(Vector3 shift) { Place(); }

        /// <summary>Keep everything where it belongs in the scene as the origin floats.</summary>
        void Place()
        {
            float size = ChunkSize * WorldMap.Scale, farHalf = FarHalf * WorldMap.Scale;
            foreach (var kv in chunks) kv.Value.position = WorldOrigin.ToScene(kv.Key.x * (double)size, kv.Key.y * (double)size, 0f);
            if (far != null) far.position = WorldOrigin.ToScene(farX - farHalf, farZ - farHalf, 0f);
        }

        Transform Show(TerrainPatch patch, string name, bool solid)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var mesh = patch.ToMesh(name);
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterials = materials;
            // The near land is solid: ships ground on it and you can walk ashore.
            if (solid) go.AddComponent<MeshCollider>().sharedMesh = mesh;
            return go.transform;
        }
    }
}
