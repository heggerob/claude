using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    public enum PropKind { Pine, Birch, Bush, Tuft, Rock, Boulder, Driftwood }

    /// <summary>One thing standing on the ground: where (global x, z and ground height), which way and how big.</summary>
    public struct Prop
    {
        public PropKind kind;
        public double x, z;
        public float y, yaw, size;
    }

    /// <summary>
    /// What grows and lies about on the land at close range, so the world isn't bare when you walk it in first
    /// person: pine and birch woods where the noise says forest, bushes, grass tufts, rocks and boulders, driftwood
    /// on the beaches. It's all decided from the global position alone (the same spot always has the same tree),
    /// kept clear of the towns' buildings, and drawn in the storybook style, merged into a few meshes per tile.
    /// </summary>
    public static class Scenery
    {
        /// <summary>A tile of scenery (m across) and the grid (m) things may stand on within it.</summary>
        public const float TileSize = 64f, Cell = 4f;
        /// <summary>Things only stand on ground at least this far above the sea (m).</summary>
        public const float DryAbove = 0.15f;

        /// <summary>Round the towns and home: nothing grows on the plots (global x, z and radius, m).</summary>
        public static readonly List<Vector3> Clearings = new List<Vector3>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Clearings.Clear(); }

        /// <summary>How far round a building nothing grows (m): past its far corner, with a yard beyond.</summary>
        public static float PlotClearing(BuildingKind kind)
        {
            var foot = Buildings.Footprint(kind);
            return Mathf.Sqrt(foot.x * foot.x + foot.z * foot.z) + 4f;
        }

        /// <summary>Whether a spot is inside one of the clearings.</summary>
        public static bool Cleared(double x, double z)
        {
            foreach (var c in Clearings)
            {
                double dx = x - c.x, dz = z - c.y;
                if (dx * dx + dz * dz < c.z * c.z) return true;
            }
            return false;
        }

        /// <summary>A repeatable number in [0, 1) for a grid cell and a salt.</summary>
        public static float Hash(long i, long j, int salt)
        {
            unchecked
            {
                ulong h = (ulong)(i * 73856093L) ^ (ulong)(j * 19349663L) ^ (ulong)(salt * 83492791L);
                h ^= h >> 33; h *= 0xff51afd7ed558ccdUL; h ^= h >> 33; h *= 0xc4ceb9fe1a85ec53UL; h ^= h >> 33;
                return (h & 0xffffff) / (float)0x1000000;
            }
        }

        /// <summary>How wooded the land is here, 0..1: big patches of forest with open ground between.</summary>
        public static float Forest(double x, double z)
        {
            return Mathf.Clamp01(TerrainDetail.Fbm(x / 520.0 + 11.3, z / 520.0 - 4.1, 3) * 1.6f + 0.35f);
        }

        /// <summary>
        /// What stands in tile (tx, tz), from the ground under each cell: <paramref name="height"/> gives the ground
        /// height at a global spot and <paramref name="kindAt"/> what the ground is there.
        /// </summary>
        public static List<Prop> Plan(int tx, int tz, System.Func<double, double, float> height, System.Func<double, double, Ground> kindAt)
        {
            var props = new List<Prop>();
            int n = Mathf.RoundToInt(TileSize / Cell);
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    long ci = (long)tx * n + i, cj = (long)tz * n + j;
                    // Somewhere inside the cell, not on its corner, so nothing lines up in rows.
                    double x = (ci + 0.15 + 0.7 * Hash(ci, cj, 1)) * Cell, z = (cj + 0.15 + 0.7 * Hash(ci, cj, 2)) * Cell;
                    float roll = Hash(ci, cj, 3);
                    var ground = kindAt(x, z);
                    PropKind kind;
                    if (!Pick(ground, Forest(x, z), roll, out kind)) continue;
                    if (Cleared(x, z)) continue;
                    // Nothing below the tide line.
                    float y = height(x, z);
                    if (y < DryAbove) continue;
                    props.Add(new Prop
                    {
                        kind = kind, x = x, z = z, y = y,
                        yaw = Hash(ci, cj, 4) * 360f,
                        size = 0.75f + 0.55f * Hash(ci, cj, 5),
                    });
                }
            return props;
        }

        /// <summary>
        /// What, if anything, stands on a cell of this ground: in woods mostly trees (pine on the whole, birch at
        /// the edges), in the open grass tufts, bushes and the odd rock, boulders among the scree, driftwood and
        /// pebbles on the beach. <paramref name="roll"/> is the cell's own random number.
        /// </summary>
        public static bool Pick(Ground ground, float forest, float roll, out PropKind kind)
        {
            kind = PropKind.Tuft;
            switch (ground)
            {
                case Ground.Grass:
                    {
                        float trees = forest * 0.22f;
                        if (roll < trees) { kind = roll < trees * (0.35f + 0.5f * forest) ? PropKind.Pine : PropKind.Birch; return true; }
                        roll -= trees;
                        if (roll < 0.03f) { kind = PropKind.Bush; return true; }
                        roll -= 0.03f;
                        if (roll < 0.012f) { kind = PropKind.Rock; return true; }
                        roll -= 0.012f;
                        if (roll < 0.16f * (1f - forest * 0.6f)) { kind = PropKind.Tuft; return true; }
                        return false;
                    }
                case Ground.Rock:
                    if (roll < 0.05f) { kind = PropKind.Boulder; return true; }
                    if (roll < 0.14f) { kind = PropKind.Rock; return true; }
                    if (roll < 0.17f) { kind = PropKind.Pine; return true; }
                    return false;
                case Ground.Sand:
                    if (roll < 0.012f) { kind = PropKind.Driftwood; return true; }
                    if (roll < 0.03f) { kind = PropKind.Rock; return true; }
                    if (roll < 0.06f) { kind = PropKind.Tuft; return true; }
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>Whether you walk into it (trees and big stones) rather than through it (grass and bushes).</summary>
        public static bool Solid(PropKind kind) { return kind == PropKind.Pine || kind == PropKind.Birch || kind == PropKind.Boulder; }

        /// <summary>How wide the solid part is (m, radius) at size 1: a trunk, or a boulder.</summary>
        public static float SolidRadius(PropKind kind) { return kind == PropKind.Boulder ? 1.3f : 0.25f; }

        // ---------------------------------------------------------------- drawing

        static readonly Color PineGreen = new Color(0.2f, 0.36f, 0.22f), PineDark = new Color(0.13f, 0.25f, 0.16f),
            Bark = new Color(0.34f, 0.24f, 0.16f), BirchBark = new Color(0.9f, 0.88f, 0.82f), BirchMark = new Color(0.2f, 0.18f, 0.16f),
            BirchLeaf = new Color(0.46f, 0.62f, 0.3f), BushGreen = new Color(0.3f, 0.45f, 0.24f), Grass = new Color(0.5f, 0.62f, 0.3f),
            Stone = new Color(0.56f, 0.55f, 0.52f), StoneDark = new Color(0.42f, 0.41f, 0.4f), Drift = new Color(0.66f, 0.6f, 0.52f);

        /// <summary>
        /// The tile's props as one model, relative to (<paramref name="ox"/>, <paramref name="oz"/>): each colour's
        /// pieces merged into one mesh, so a whole tile is only a handful of drawn parts.
        /// </summary>
        /// <summary>Foliage drawn at a leaf's scale: one tile of the leaf texture about every metre and a half.</summary>
        static MeshData Leafy(MeshData mesh)
        {
            mesh.Uvs.Clear();
            foreach (var v in mesh.Vertices) mesh.Uvs.Add(new Vector2(v.x + v.z * 0.7f, v.y + v.z * 0.3f) / 1.5f);
            return mesh;
        }

        public static VikingModel Model(List<Prop> props, double ox, double oz)
        {
            var merged = new Dictionary<Color, MeshData>();
            var surfaces = new Dictionary<Color, SurfaceKind>();
            System.Action<Color, SurfaceKind, MeshData> put = (c, surface, mesh) =>
            {
                MeshData into;
                if (!merged.TryGetValue(c, out into)) { into = new MeshData(); merged[c] = into; surfaces[c] = surface; }
                into.Append(mesh);
            };
            foreach (var p in props)
            {
                var at = new Vector3((float)(p.x - ox), p.y, (float)(p.z - oz));
                var turn = Quaternion.Euler(0f, p.yaw, 0f);
                var s = Vector3.one * p.size;
                System.Action<Color, SurfaceKind, MeshData> add = (c, surface, mesh) => put(c, surface, mesh.Transformed(at, turn, s));
                switch (p.kind)
                {
                    case PropKind.Pine:
                        add(Bark, SurfaceKind.Wood, MeshData.Lathe(new[] { new Vector2(0.24f, -0.3f), new Vector2(0.2f, 1.2f), new Vector2(0.1f, 3.4f) }, 7));
                        // Tiers of drooping boughs, narrowing to the top, in two greens.
                        for (int t = 0; t < 4; t++)
                        {
                            float y = 1.3f + t * 1.25f, r = 2.1f - t * 0.45f;
                            add(t % 2 == 0 ? PineGreen : PineDark, SurfaceKind.Leaves, Leafy(MeshData.Lathe(new[] {
                                new Vector2(r, y), new Vector2(r * 0.85f, y + 0.15f), new Vector2(0.05f, y + 1.9f) }, 9)));
                        }
                        break;
                    case PropKind.Birch:
                        add(BirchBark, SurfaceKind.Wood, MeshData.Lathe(new[] { new Vector2(0.18f, -0.3f), new Vector2(0.14f, 2.4f), new Vector2(0.07f, 4.4f) }, 7));
                        for (int m = 0; m < 4; m++)
                            add(BirchMark, SurfaceKind.Plain, MeshData.Box(new Vector3(0f, 0.5f + m * 0.75f, 0.15f - m * 0.01f), new Vector3(0.12f, 0.05f, 0.02f)).Transformed(Vector3.zero, Quaternion.Euler(0f, m * 70f, 0f), Vector3.one));
                        // A light, lumpy crown.
                        add(BirchLeaf, SurfaceKind.Leaves, Leafy(MeshData.Ellipsoid(new Vector3(0f, 4.2f, 0f), new Vector3(1.4f, 1.6f, 1.4f), 9, 6)));
                        add(BirchLeaf, SurfaceKind.Leaves, Leafy(MeshData.Ellipsoid(new Vector3(0.7f, 3.4f, 0.3f), new Vector3(0.9f, 0.9f, 0.9f), 8, 5)));
                        add(BirchLeaf, SurfaceKind.Leaves, Leafy(MeshData.Ellipsoid(new Vector3(-0.6f, 3.6f, -0.4f), new Vector3(0.9f, 1f, 0.9f), 8, 5)));
                        break;
                    case PropKind.Bush:
                        add(BushGreen, SurfaceKind.Leaves, Leafy(MeshData.Ellipsoid(new Vector3(0f, 0.35f, 0f), new Vector3(0.8f, 0.55f, 0.7f), 8, 5)));
                        add(BushGreen, SurfaceKind.Leaves, Leafy(MeshData.Ellipsoid(new Vector3(0.45f, 0.3f, 0.2f), new Vector3(0.5f, 0.4f, 0.5f), 7, 4)));
                        break;
                    case PropKind.Tuft:
                        // A clump of long grass: thin blades leaning out every way.
                        for (int b = 0; b < 5; b++)
                        {
                            var lean = Quaternion.Euler(18f + b * 4f, b * 72f, 0f);
                            add(Grass, SurfaceKind.Plain, MeshData.Lathe(new[] { new Vector2(0.05f, 0f), new Vector2(0.001f, 0.55f) }, 3).Transformed(Vector3.zero, lean, Vector3.one));
                        }
                        break;
                    case PropKind.Rock:
                        add(Stone, SurfaceKind.Stone, MeshData.Ellipsoid(new Vector3(0f, 0.12f, 0f), new Vector3(0.45f, 0.3f, 0.35f), 7, 4));
                        break;
                    case PropKind.Boulder:
                        add(StoneDark, SurfaceKind.Stone, MeshData.Ellipsoid(new Vector3(0f, 0.5f, 0f), new Vector3(1.5f, 1.1f, 1.2f), 8, 5));
                        add(Stone, SurfaceKind.Stone, MeshData.Ellipsoid(new Vector3(0.9f, 0.25f, 0.5f), new Vector3(0.7f, 0.5f, 0.6f), 7, 4));
                        break;
                    case PropKind.Driftwood:
                        add(Drift, SurfaceKind.Wood, MeshData.Tube(new[] { new Vector3(-1.4f, 0.12f, 0f), new Vector3(0f, 0.15f, 0.15f), new Vector3(1.3f, 0.12f, -0.1f) }, new[] { 0.14f, 0.12f, 0.07f }, 6));
                        break;
                }
            }
            var model = new VikingModel();
            model.AddJoint(Joint, null, Vector3.zero);
            foreach (var kv in merged) model.Add(Joint, kv.Key, kv.Value, true, surfaces[kv.Key]);
            return model;
        }

        public const string Joint = "Scenery";
    }

    /// <summary>
    /// Keeps the scenery standing in the tiles round the player (built a few per frame, dropped once well behind),
    /// with trunks and boulders solid to walk into, moved with the floating origin.
    /// </summary>
    public class SceneryField : MonoBehaviour
    {
        /// <summary>How many tiles out from the player's are kept, and at most how many are built per frame.</summary>
        public const int Radius = 5, BuildsPerFrame = 1;

        public Transform follow;
        WorldMap map;
        readonly Dictionary<Vector2Int, Transform> tiles = new Dictionary<Vector2Int, Transform>();

        public static SceneryField Create(Transform parent, WorldMap map, Transform follow)
        {
            var go = new GameObject("Scenery");
            go.transform.SetParent(parent, false);
            var f = go.AddComponent<SceneryField>();
            f.map = map;
            f.follow = follow;
            return f;
        }

        /// <summary>The ground's height and kind at a global spot, as the terrain chunks build it.</summary>
        public static float GroundHeight(WorldMap map, double x, double z) { return TerrainDetail.Height(map, x, z); }

        public static Ground GroundKind(WorldMap map, double x, double z)
        {
            float h = TerrainDetail.Height(map, x, z);
            float dx = TerrainDetail.Height(map, x + 3.0, z) - h, dz = TerrainDetail.Height(map, x, z + 3.0) - h;
            float slope = Mathf.Sqrt(dx * dx + dz * dz) / 3f;
            return TerrainDetail.Kind(h / WorldMap.Scale, slope, TerrainDetail.Shore(x, z));
        }

        void Update()
        {
            if (map == null || follow == null || TimeWarp.OnPassage) return;
            double gx = WorldOrigin.GlobalX(follow.position), gz = WorldOrigin.GlobalZ(follow.position);
            var centre = WorldTerrain.ChunkOf(gx, gz, Scenery.TileSize);
            int built = 0;
            foreach (var t in WorldTerrain.Wanted(centre, Radius))
            {
                if (tiles.ContainsKey(t)) continue;
                if (built++ >= BuildsPerFrame) break;
                tiles[t] = Build(t);
            }
            var drop = new List<Vector2Int>();
            foreach (var kv in tiles)
                if ((kv.Key - centre).sqrMagnitude > (Radius + 2) * (Radius + 2)) drop.Add(kv.Key);
            foreach (var t in drop) { if (tiles[t] != null) Destroy(tiles[t].gameObject); tiles.Remove(t); }
            Place();
        }

        Transform Build(Vector2Int t)
        {
            var root = new GameObject("Scenery " + t.x + "," + t.y).transform;
            root.SetParent(transform, false);
            var props = Scenery.Plan(t.x, t.y, (x, z) => GroundHeight(map, x, z), (x, z) => GroundKind(map, x, z));
            if (props.Count == 0) return root;
            double ox = t.x * (double)Scenery.TileSize, oz = t.y * (double)Scenery.TileSize;
            ModelView.Show(Scenery.Model(props, ox, oz), root);
            foreach (var p in props)
            {
                if (!Scenery.Solid(p.kind)) continue;
                var col = new GameObject(p.kind.ToString()).AddComponent<CapsuleCollider>();
                col.transform.SetParent(root, false);
                col.transform.localPosition = new Vector3((float)(p.x - ox), p.y, (float)(p.z - oz));
                col.radius = Scenery.SolidRadius(p.kind) * p.size;
                col.height = p.kind == PropKind.Boulder ? 2.2f * p.size : 4f * p.size;
                col.center = new Vector3(0f, col.height / 2f, 0f);
            }
            return root;
        }

        void OnEnable() { WorldOrigin.Shifted += OnShift; }
        void OnDisable() { WorldOrigin.Shifted -= OnShift; }
        void OnShift(Vector3 shift) { Place(); }

        void Place()
        {
            foreach (var kv in tiles)
                if (kv.Value != null) kv.Value.position = WorldOrigin.ToScene(kv.Key.x * (double)Scenery.TileSize, kv.Key.y * (double)Scenery.TileSize, 0f);
        }
    }
}
