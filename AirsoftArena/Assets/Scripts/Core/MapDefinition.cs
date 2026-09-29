using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    public enum GroundStyle { Grass, Concrete, ForestFloor }

    public enum PieceKind
    {
        /// <summary>Plywood wall, 3 m. Blocks everything.</summary>
        Wall,
        /// <summary>Shipping container, 3 m.</summary>
        Container,
        /// <summary>Sandbags, 0.95 m. BBs fly over, you can see over.</summary>
        Sandbags,
        /// <summary>Warehouse shelving, 2.2 m.</summary>
        Shelf,
        /// <summary>Stack of wooden crates, 1.1 m.</summary>
        Crates,
        /// <summary>Tree: solid trunk, canopy drawn above players.</summary>
        Tree,
        /// <summary>Bush: walk through it, blocks sight, sometimes stops a BB.</summary>
        Bush,
        /// <summary>Fallen log, 0.6 m. Go prone... well, crouch.</summary>
        Log,
        /// <summary>Boulder, 1.2 m.</summary>
        Rock,
    }

    public struct MapPiece
    {
        public PieceKind kind;
        public Vector2 center;
        public Vector2 size;

        public MapPiece(PieceKind kind, Vector2 center, Vector2 size)
        {
            this.kind = kind;
            this.center = center;
            this.size = size;
        }
    }

    /// <summary>A playable field: size, spawns, objective points and everything placed on it. 1 unit = 1 metre.</summary>
    public class MapDefinition
    {
        public string id;
        public string name;
        public string description;
        public bool indoor;
        public GroundStyle ground;
        public Rect bounds;
        /// <summary>Blue, Red.</summary>
        public Rect[] spawnZones;
        /// <summary>Capture the Flag: where each team's flag stands (Blue, Red).</summary>
        public Vector2[] flagPoints;
        /// <summary>King of the Hill: the centre of the hill zone.</summary>
        public Vector2 hill;
        public float hillRadius = 3.5f;
        public readonly List<MapPiece> pieces = new List<MapPiece>();

        public void Add(PieceKind kind, float x, float y, float w, float h)
        {
            pieces.Add(new MapPiece(kind, new Vector2(x, y), new Vector2(w, h)));
        }

        /// <summary>Adds the piece and its mirror image across x = 0, so both teams get the same field.</summary>
        public void Mirrored(PieceKind kind, float x, float y, float w, float h)
        {
            Add(kind, x, y, w, h);
            Add(kind, -x, y, w, h);
        }
    }

    public static class MapLibrary
    {
        static List<MapDefinition> all;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { all = null; }

        public static List<MapDefinition> All
        {
            get
            {
                if (all == null) all = new List<MapDefinition> { PalletYard(), Warehouse(), Forest() };
                return all;
            }
        }

        public static MapDefinition Get(string id)
        {
            foreach (var m in All) if (m.id == id) return m;
            return All[0];
        }

        static MapDefinition PalletYard()
        {
            var m = new MapDefinition
            {
                id = "pallet_yard",
                name = "Pallet Yard",
                description = "Outdoor yard with shipping containers and sandbags. Medium range, all-rounder.",
                ground = GroundStyle.Grass,
                bounds = new Rect(-22f, -13f, 44f, 26f),
                spawnZones = new[] { new Rect(-21f, -4f, 4f, 8f), new Rect(17f, -4f, 4f, 8f) },
                flagPoints = new[] { new Vector2(-19f, 9f), new Vector2(19f, -9f) },
                hill = Vector2.zero,
            };
            Border(m, PieceKind.Wall);
            m.Mirrored(PieceKind.Wall, -14f, 6f, 1f, 6f);
            m.Mirrored(PieceKind.Wall, -14f, -6f, 1f, 6f);
            m.Mirrored(PieceKind.Sandbags, -11f, 0f, 1f, 3f);
            m.Mirrored(PieceKind.Sandbags, -10f, 9.5f, 3f, 1f);
            m.Mirrored(PieceKind.Sandbags, -10f, -9.5f, 3f, 1f);
            m.Mirrored(PieceKind.Container, -6.5f, 4.5f, 5f, 2f);
            m.Mirrored(PieceKind.Container, -6.5f, -4.5f, 5f, 2f);
            m.Mirrored(PieceKind.Crates, -6f, 0f, 1f, 1.5f);
            m.Mirrored(PieceKind.Sandbags, -3.5f, 10f, 1f, 3f);
            m.Mirrored(PieceKind.Sandbags, -3.5f, -10f, 1f, 3f);
            m.Add(PieceKind.Wall, 0f, 2.5f, 4f, 1f);
            m.Add(PieceKind.Wall, 0f, -2.5f, 4f, 1f);
            m.Add(PieceKind.Sandbags, 0f, 7f, 2.5f, 1f);
            m.Add(PieceKind.Sandbags, 0f, -7f, 2.5f, 1f);
            return m;
        }

        static MapDefinition Warehouse()
        {
            var m = new MapDefinition
            {
                id = "warehouse",
                name = "Warehouse",
                description = "Indoor CQB between shelving rows. Short range: SMGs, shotguns and knives shine.",
                indoor = true,
                ground = GroundStyle.Concrete,
                bounds = new Rect(-18f, -11f, 36f, 22f),
                spawnZones = new[] { new Rect(-17.5f, -3f, 3.5f, 6f), new Rect(14f, -3f, 3.5f, 6f) },
                flagPoints = new[] { new Vector2(-16f, 8.5f), new Vector2(16f, -8.5f) },
                hill = Vector2.zero,
                hillRadius = 3f,
            };
            Border(m, PieceKind.Wall);
            // Office walls around each spawn, with doors.
            m.Mirrored(PieceKind.Wall, -12.5f, 7f, 1f, 8f);
            m.Mirrored(PieceKind.Wall, -12.5f, -7f, 1f, 8f);
            m.Mirrored(PieceKind.Crates, -10.5f, 0f, 1f, 2f);
            // Shelving rows.
            m.Mirrored(PieceKind.Shelf, -7.5f, 6.5f, 5f, 1f);
            m.Mirrored(PieceKind.Shelf, -7.5f, -6.5f, 5f, 1f);
            m.Mirrored(PieceKind.Shelf, -7.5f, 2.5f, 3f, 1f);
            m.Mirrored(PieceKind.Shelf, -7.5f, -2.5f, 3f, 1f);
            m.Mirrored(PieceKind.Crates, -4f, 9f, 1.5f, 1.5f);
            m.Mirrored(PieceKind.Crates, -4f, -9f, 1.5f, 1.5f);
            m.Mirrored(PieceKind.Wall, -3.5f, 4.5f, 1f, 1f); // pillars
            m.Mirrored(PieceKind.Wall, -3.5f, -4.5f, 1f, 1f);
            // Centre: a parked container and long shelves.
            m.Add(PieceKind.Container, 0f, 0f, 2.5f, 3f);
            m.Add(PieceKind.Shelf, 0f, 8f, 6f, 1f);
            m.Add(PieceKind.Shelf, 0f, -8f, 6f, 1f);
            m.Add(PieceKind.Crates, 0f, 4.5f, 1.5f, 1f);
            m.Add(PieceKind.Crates, 0f, -4.5f, 1.5f, 1f);
            return m;
        }

        static MapDefinition Forest()
        {
            var m = new MapDefinition
            {
                id = "forest",
                name = "Forest",
                description = "Norwegian pine forest. Trees, bushes to hide in and long sight lines. Snipers welcome.",
                ground = GroundStyle.ForestFloor,
                bounds = new Rect(-24f, -15f, 48f, 30f),
                spawnZones = new[] { new Rect(-23f, -4f, 4f, 8f), new Rect(19f, -4f, 4f, 8f) },
                flagPoints = new[] { new Vector2(-21f, 11f), new Vector2(21f, -11f) },
                hill = new Vector2(0f, 0f),
                hillRadius = 4f,
            };
            Border(m, PieceKind.Wall);

            // Scatter trees, bushes, logs and rocks over the left half with a fixed seed, then mirror.
            var rng = new System.Random(2024);
            var placed = new List<Vector2>();
            int attempts = 0;
            while (placed.Count < 40 && attempts++ < 2000)
            {
                float x = (float)(rng.NextDouble() * 20.5 - 22.5);
                float y = (float)(rng.NextDouble() * 27.0 - 13.5);
                var p = new Vector2(x, y);
                if (x < -18.5f && Mathf.Abs(y) < 5.5f) continue; // keep spawn clear
                if (x > -1.5f) continue;                          // centre lane stays open-ish
                // Keep flag spots clear on both sides (the right flag is the mirror of (-21, -11)).
                if (Vector2.Distance(p, new Vector2(-21f, 11f)) < 3f || Vector2.Distance(p, new Vector2(-21f, -11f)) < 3f) continue;
                bool tooClose = false;
                foreach (var q in placed) if (Vector2.Distance(p, q) < 2.6f) { tooClose = true; break; }
                if (tooClose) continue;
                placed.Add(p);

                int roll = rng.Next(100);
                if (roll < 50) m.Mirrored(PieceKind.Tree, x, y, 1f, 1f);
                else if (roll < 78) m.Mirrored(PieceKind.Bush, x, y, 2f, 2f);
                else if (roll < 92)
                {
                    bool horizontal = rng.Next(2) == 0;
                    m.Mirrored(PieceKind.Log, x, y, horizontal ? 3f : 0.8f, horizontal ? 0.8f : 3f);
                }
                else m.Mirrored(PieceKind.Rock, x, y, 1.6f, 1.4f);
            }
            // A hunting stand and some rocks around the middle.
            m.Add(PieceKind.Rock, 0f, 0f, 2f, 2f);
            m.Add(PieceKind.Log, 0f, 7f, 4f, 0.8f);
            m.Add(PieceKind.Log, 0f, -7f, 4f, 0.8f);
            return m;
        }

        static void Border(MapDefinition m, PieceKind kind)
        {
            var b = m.bounds;
            m.Add(kind, b.center.x, b.yMax + 0.5f, b.width + 2f, 1f);
            m.Add(kind, b.center.x, b.yMin - 0.5f, b.width + 2f, 1f);
            m.Add(kind, b.xMin - 0.5f, b.center.y, 1f, b.height);
            m.Add(kind, b.xMax + 0.5f, b.center.y, 1f, b.height);
        }
    }
}
