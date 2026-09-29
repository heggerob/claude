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
        /// <summary>Oil drum, 1 m. Round low cover.</summary>
        Barrel,
        /// <summary>Stack of old tyres, 0.8 m.</summary>
        Tires,
        /// <summary>Wooden pallet lying flat: decoration only, walk and shoot over it.</summary>
        Pallet,
        /// <summary>Field boundary: posts with safety netting. Stops BBs and players, but you can see through it.</summary>
        Netting,
        /// <summary>Round hay bale, 1.2 m.</summary>
        HayBale,
        /// <summary>Wooden farm fence, 1.1 m.</summary>
        Fence,
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
        /// <summary>Only used by the training range, hidden from the map picker.</summary>
        public bool trainingOnly;
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
        /// <summary>Paths, puddles and floor markings painted on the ground.</summary>
        public readonly List<GroundMark> marks = new List<GroundMark>();

        public void Path(float width, params Vector2[] points)
        {
            marks.Add(new GroundMark { kind = MarkKind.Path, points = points, width = width });
        }

        public void Puddle(float x, float y, float radius)
        {
            marks.Add(new GroundMark { kind = MarkKind.Puddle, points = new[] { new Vector2(x, y) }, width = radius });
        }

        public void Stain(float x, float y, float radius)
        {
            marks.Add(new GroundMark { kind = MarkKind.Stain, points = new[] { new Vector2(x, y) }, width = radius });
        }

        public void PaintLine(Color color, float width, params Vector2[] points)
        {
            marks.Add(new GroundMark { kind = MarkKind.PaintLine, points = points, width = width, color = color });
        }

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
                if (all == null) all = new List<MapDefinition> { PalletYard(), Warehouse(), Forest(), OldFarm(), Range() };
                return all;
            }
        }

        public static MapDefinition Get(string id)
        {
            foreach (var m in All) if (m.id == id) return m;
            return All[0];
        }

        // Everything is designed for the left half and mirrored across x = 0, so both teams get the same field.

        static MapDefinition PalletYard()
        {
            var m = new MapDefinition
            {
                id = "pallet_yard",
                name = "Pallet Yard",
                description = "Big outdoor yard with container rows, sandbags and dirt tracks. Medium range, all-rounder.",
                ground = GroundStyle.Grass,
                bounds = new Rect(-40f, -24f, 80f, 48f),
                spawnZones = new[] { new Rect(-39f, -5f, 5f, 10f), new Rect(34f, -5f, 5f, 10f) },
                flagPoints = new[] { new Vector2(-36f, 18f), new Vector2(36f, -18f) },
                hill = Vector2.zero,
                hillRadius = 5f,
            };
            Border(m, PieceKind.Netting);

            // Tracks worn into the grass.
            m.Path(2.4f, new Vector2(-38f, 0f), new Vector2(-28f, 0.5f), new Vector2(-18f, 2f), new Vector2(-8f, 0.5f), new Vector2(0f, 0f), new Vector2(8f, -0.5f), new Vector2(18f, -2f), new Vector2(28f, -0.5f), new Vector2(38f, 0f));
            m.Path(1.6f, new Vector2(-33f, 17f), new Vector2(-20f, 19f), new Vector2(-8f, 18f), new Vector2(8f, 18f), new Vector2(20f, 19f), new Vector2(33f, 17f));
            m.Path(1.6f, new Vector2(-33f, -17f), new Vector2(-20f, -19f), new Vector2(-8f, -18f), new Vector2(8f, -18f), new Vector2(20f, -19f), new Vector2(33f, -17f));
            m.Path(1.2f, new Vector2(-18f, 2f), new Vector2(-17f, 10f), new Vector2(-20f, 19f));
            m.Path(1.2f, new Vector2(18f, -2f), new Vector2(17f, -10f), new Vector2(20f, -19f));
            m.Puddle(-12f, -18f, 1.5f); m.Puddle(12f, 18f, 1.5f);
            m.Puddle(-4f, 8f, 1f); m.Puddle(4f, -8f, 1f);
            m.Puddle(-27f, 1f, 0.9f); m.Puddle(27f, -1f, 0.9f);

            // Spawn walls with a gap in the middle, covered by sandbags.
            m.Mirrored(PieceKind.Wall, -30f, 10f, 1f, 12f);
            m.Mirrored(PieceKind.Wall, -30f, -10f, 1f, 12f);
            m.Mirrored(PieceKind.Sandbags, -27f, 0f, 1f, 3f);
            m.Mirrored(PieceKind.Tires, -26f, 14f, 1.1f, 1.1f);
            m.Mirrored(PieceKind.Tires, -26f, -14f, 1.1f, 1.1f);

            // Container rows and the clutter around them.
            m.Mirrored(PieceKind.Container, -22f, 12f, 6f, 2.4f);
            m.Mirrored(PieceKind.Container, -22f, -12f, 6f, 2.4f);
            m.Mirrored(PieceKind.Container, -14f, 6f, 2.4f, 6f);
            m.Mirrored(PieceKind.Container, -14f, -6f, 2.4f, 6f);
            m.Mirrored(PieceKind.Crates, -18f, 0f, 1.5f, 1.5f);
            m.Mirrored(PieceKind.Barrel, -20f, 5.5f, 0.9f, 0.9f);
            m.Mirrored(PieceKind.Barrel, -21f, 6.3f, 0.9f, 0.9f);
            m.Mirrored(PieceKind.Barrel, -20f, -5.5f, 0.9f, 0.9f);
            m.Mirrored(PieceKind.Pallet, -24.5f, 19f, 1.2f, 1f);
            m.Mirrored(PieceKind.Pallet, -23f, 19.4f, 1.2f, 1f);
            m.Mirrored(PieceKind.Pallet, -17f, -16f, 1.2f, 1f);
            m.Mirrored(PieceKind.Crates, -10f, 12.5f, 1.2f, 1.2f);
            m.Mirrored(PieceKind.Crates, -10f, -12.5f, 1.2f, 1.2f);

            // Mid field.
            m.Mirrored(PieceKind.Sandbags, -8f, 16f, 4f, 1f);
            m.Mirrored(PieceKind.Sandbags, -8f, -16f, 4f, 1f);
            m.Mirrored(PieceKind.Sandbags, -6f, 0f, 1f, 3f);
            m.Mirrored(PieceKind.Tires, -5f, 21f, 1.1f, 1.1f);
            m.Mirrored(PieceKind.Tires, -5f, -21f, 1.1f, 1.1f);

            // Centre bunker.
            m.Add(PieceKind.Wall, 0f, 4f, 6f, 1f);
            m.Add(PieceKind.Wall, 0f, -4f, 6f, 1f);
            m.Add(PieceKind.Crates, 0f, 12f, 2f, 2f);
            m.Add(PieceKind.Crates, 0f, -12f, 2f, 2f);
            m.Add(PieceKind.Sandbags, 0f, 19.5f, 3f, 1f);
            m.Add(PieceKind.Sandbags, 0f, -19.5f, 3f, 1f);
            return m;
        }

        static MapDefinition Warehouse()
        {
            var m = new MapDefinition
            {
                id = "warehouse",
                name = "Warehouse",
                description = "Big indoor CQB: offices, shelving aisles and a loading bay. SMGs, shotguns and knives shine.",
                indoor = true,
                ground = GroundStyle.Concrete,
                bounds = new Rect(-30f, -18f, 60f, 36f),
                spawnZones = new[] { new Rect(-29.5f, -4f, 5f, 8f), new Rect(24.5f, -4f, 5f, 8f) },
                flagPoints = new[] { new Vector2(-27f, 14f), new Vector2(27f, -14f) },
                hill = Vector2.zero,
                hillRadius = 4f,
            };
            Border(m, PieceKind.Wall);

            var yellow = new Color32(214, 176, 44, 255);
            var white = new Color32(210, 210, 204, 255);
            // Walkway lines along the aisles, a loading bay box in the middle, hazard stripes at the offices.
            m.PaintLine(yellow, 0.18f, new Vector2(-22f, 9f), new Vector2(22f, 9f));
            m.PaintLine(yellow, 0.18f, new Vector2(-22f, -9f), new Vector2(22f, -9f));
            m.PaintLine(yellow, 0.18f, new Vector2(-22f, 4f), new Vector2(22f, 4f));
            m.PaintLine(yellow, 0.18f, new Vector2(-22f, -4f), new Vector2(22f, -4f));
            m.PaintLine(white, 0.12f, new Vector2(-4f, -6f), new Vector2(4f, -6f), new Vector2(4f, 6f), new Vector2(-4f, 6f), new Vector2(-4f, -6f));
            m.Stain(-12f, 1f, 0.8f); m.Stain(12f, -1f, 0.8f); m.Stain(-3f, 14f, 0.6f); m.Stain(3f, -14f, 0.6f); m.Stain(-18f, -12f, 0.5f); m.Stain(18f, 12f, 0.5f);

            // Office walls around each spawn with a door, and an office room holding the flag.
            m.Mirrored(PieceKind.Wall, -23f, 11f, 1f, 14f);
            m.Mirrored(PieceKind.Wall, -23f, -11f, 1f, 14f);
            m.Mirrored(PieceKind.Wall, -28.5f, 8f, 3f, 0.6f);
            m.Mirrored(PieceKind.Wall, -28.5f, -8f, 3f, 0.6f);
            m.Mirrored(PieceKind.Crates, -20f, 0f, 1f, 2.5f);

            // Shelving aisles.
            m.Mirrored(PieceKind.Shelf, -14f, 13f, 8f, 1f);
            m.Mirrored(PieceKind.Shelf, -14f, 6.5f, 8f, 1f);
            m.Mirrored(PieceKind.Shelf, -14f, -6.5f, 8f, 1f);
            m.Mirrored(PieceKind.Shelf, -14f, -13f, 8f, 1f);
            m.Mirrored(PieceKind.Shelf, -5.5f, 11f, 5f, 1f);
            m.Mirrored(PieceKind.Shelf, -5.5f, -11f, 5f, 1f);
            m.Mirrored(PieceKind.Wall, -9f, 2.3f, 1f, 1f); // pillars
            m.Mirrored(PieceKind.Wall, -9f, -2.3f, 1f, 1f);
            m.Mirrored(PieceKind.Crates, -12f, 0f, 1.5f, 1.5f);
            m.Mirrored(PieceKind.Crates, -17f, 3f, 1.2f, 1.2f);
            m.Mirrored(PieceKind.Crates, -17f, -3f, 1.2f, 1.2f);
            m.Mirrored(PieceKind.Barrel, -6f, 16f, 0.9f, 0.9f);
            m.Mirrored(PieceKind.Barrel, -7f, 16.2f, 0.9f, 0.9f);
            m.Mirrored(PieceKind.Barrel, -6f, -16f, 0.9f, 0.9f);
            m.Mirrored(PieceKind.Pallet, -19f, 16f, 1.2f, 1f);
            m.Mirrored(PieceKind.Pallet, -19f, -16f, 1.2f, 1f);
            m.Mirrored(PieceKind.Pallet, -2f, 1.5f, 1.2f, 1f);

            // Centre: a parked container in the loading bay.
            m.Add(PieceKind.Container, 0f, 0f, 3f, 6f);
            m.Add(PieceKind.Shelf, 0f, 15.5f, 8f, 1f);
            m.Add(PieceKind.Shelf, 0f, -15.5f, 8f, 1f);
            m.Add(PieceKind.Crates, 0f, 8f, 2f, 1.2f);
            m.Add(PieceKind.Crates, 0f, -8f, 2f, 1.2f);
            return m;
        }

        static MapDefinition Forest()
        {
            var m = new MapDefinition
            {
                id = "forest",
                name = "Forest",
                description = "Huge Norwegian pine forest. Trees, bushes to hide in, a muddy trail and long sight lines.",
                ground = GroundStyle.ForestFloor,
                bounds = new Rect(-48f, -30f, 96f, 60f),
                spawnZones = new[] { new Rect(-47f, -5f, 5f, 10f), new Rect(42f, -5f, 5f, 10f) },
                flagPoints = new[] { new Vector2(-44f, 24f), new Vector2(44f, -24f) },
                hill = Vector2.zero,
                hillRadius = 5f,
            };
            Border(m, PieceKind.Netting);

            // A winding trail from base to base, plus a side track to each flag.
            var trail = new[] { new Vector2(-44f, 0f), new Vector2(-34f, 4f), new Vector2(-24f, -2f), new Vector2(-12f, 5f), new Vector2(0f, 0f), new Vector2(12f, -5f), new Vector2(24f, 2f), new Vector2(34f, -4f), new Vector2(44f, 0f) };
            m.Path(2f, trail);
            var northTrack = new[] { new Vector2(-34f, 4f), new Vector2(-40f, 14f), new Vector2(-44f, 24f) };
            var southTrack = new[] { new Vector2(34f, -4f), new Vector2(40f, -14f), new Vector2(44f, -24f) };
            m.Path(1.3f, northTrack);
            m.Path(1.3f, southTrack);
            m.Puddle(-24f, -2.5f, 1.4f); m.Puddle(24f, 2.5f, 1.4f);
            m.Puddle(-12f, 5.5f, 1f); m.Puddle(12f, -5.5f, 1f);
            m.Puddle(-30f, -20f, 1.8f); m.Puddle(30f, 20f, 1.8f);

            // Scatter trees, bushes, logs and rocks over the left half with a fixed seed, then mirror.
            var rng = new System.Random(2024);
            var placed = new List<Vector2>();
            int attempts = 0;
            while (placed.Count < 115 && attempts++ < 8000)
            {
                float x = (float)(rng.NextDouble() * 45 - 46.5);
                float y = (float)(rng.NextDouble() * 57.0 - 28.5);
                var p = new Vector2(x, y);
                if (x < -40f && Mathf.Abs(y) < 7.5f) continue;   // keep spawn clear
                if (x > -2f) continue;                            // centre lane stays open-ish
                if (Vector2.Distance(p, new Vector2(-44f, 24f)) < 4f || Vector2.Distance(p, new Vector2(-44f, -24f)) < 4f) continue; // flag spots (right flag mirrors to -44,-24)
                // Stay off the trail (and its mirror image on this side).
                var mirrored = new Vector2(-x, y);
                if (GroundPainter.DistanceToPolyline(p, trail) < 2.2f || GroundPainter.DistanceToPolyline(mirrored, trail) < 2.2f) continue;
                if (GroundPainter.DistanceToPolyline(p, northTrack) < 1.8f || GroundPainter.DistanceToPolyline(mirrored, southTrack) < 1.8f) continue;
                bool tooClose = false;
                foreach (var q in placed) if (Vector2.Distance(p, q) < 2.8f) { tooClose = true; break; }
                if (tooClose) continue;
                placed.Add(p);

                int roll = rng.Next(100);
                if (roll < 52) m.Mirrored(PieceKind.Tree, x, y, 1f, 1f);
                else if (roll < 78) m.Mirrored(PieceKind.Bush, x, y, 2.2f, 2.2f);
                else if (roll < 92)
                {
                    bool horizontal = rng.Next(2) == 0;
                    m.Mirrored(PieceKind.Log, x, y, horizontal ? 3f : 0.8f, horizontal ? 0.8f : 3f);
                }
                else m.Mirrored(PieceKind.Rock, x, y, 1.8f, 1.6f);
            }
            // Rocks and logs around the middle.
            m.Add(PieceKind.Rock, 0f, 0f, 2.4f, 2.4f);
            m.Add(PieceKind.Log, 0f, 9f, 5f, 0.9f);
            m.Add(PieceKind.Log, 0f, -9f, 5f, 0.9f);
            m.Add(PieceKind.Rock, 0f, 18f, 2f, 1.8f);
            m.Add(PieceKind.Rock, 0f, -18f, 2f, 1.8f);
            return m;
        }

        static MapDefinition OldFarm()
        {
            var m = new MapDefinition
            {
                id = "old_farm",
                name = "Old Farm",
                description = "Abandoned farm: a ruined farmhouse and a barn on each side, hay bales, fences and a well in the yard.",
                ground = GroundStyle.Grass,
                bounds = new Rect(-45f, -28f, 90f, 56f),
                spawnZones = new[] { new Rect(-44f, -5f, 5f, 10f), new Rect(39f, -5f, 5f, 10f) },
                // Each team's flag sits in its own farmhouse (the layout is a mirror image).
                flagPoints = new[] { new Vector2(-24f, 12f), new Vector2(24f, 12f) },
                hill = Vector2.zero,
                hillRadius = 5f,
            };
            Border(m, PieceKind.Netting);

            m.Path(3f, new Vector2(-43f, 0f), new Vector2(-30f, 0f), new Vector2(-16f, 2f), new Vector2(0f, 1f), new Vector2(16f, 2f), new Vector2(30f, 0f), new Vector2(43f, 0f));
            foreach (float side in new[] { -1f, 1f })
            {
                m.Path(1.8f, new Vector2(side * 16f, 2f), new Vector2(side * 22f, 7f), new Vector2(side * 23.5f, 8.2f));
                m.Path(1.8f, new Vector2(side * 16f, 2f), new Vector2(side * 19f, -8f), new Vector2(side * 20f, -10f));
            }
            m.Puddle(-8f, 3f, 1.6f); m.Puddle(8f, -3f, 1.6f);
            m.Puddle(-33f, -14f, 1.2f); m.Puddle(33f, 14f, 1.2f);

            // Ruined farmhouse (flag inside), door facing the yard.
            m.Mirrored(PieceKind.Wall, -24f, 16.4f, 10f, 0.8f);
            m.Mirrored(PieceKind.Wall, -27f, 8f, 4f, 0.8f);
            m.Mirrored(PieceKind.Wall, -20.5f, 8f, 3f, 0.8f);
            m.Mirrored(PieceKind.Wall, -29f, 12.2f, 0.8f, 7.6f);
            m.Mirrored(PieceKind.Wall, -19f, 14f, 0.8f, 4f);
            m.Mirrored(PieceKind.Crates, -27.5f, 14.8f, 1f, 1f);

            // Barn with big doors facing the yard, hay inside.
            m.Mirrored(PieceKind.Wall, -24.5f, -10f, 3f, 0.8f);
            m.Mirrored(PieceKind.Wall, -15.5f, -10f, 3f, 0.8f);
            m.Mirrored(PieceKind.Wall, -20f, -18f, 12f, 0.8f);
            m.Mirrored(PieceKind.Wall, -26f, -14f, 0.8f, 8.8f);
            m.Mirrored(PieceKind.Wall, -14f, -14f, 0.8f, 8.8f);
            m.Mirrored(PieceKind.HayBale, -23f, -16f, 2f, 1.2f);
            m.Mirrored(PieceKind.HayBale, -18f, -13f, 1.2f, 2f);
            m.Mirrored(PieceKind.Barrel, -24.5f, -12f, 0.9f, 0.9f);

            // Fences along the road and around the fields.
            m.Mirrored(PieceKind.Fence, -32f, 5f, 8f, 0.3f);
            m.Mirrored(PieceKind.Fence, -32f, -5f, 8f, 0.3f);
            m.Mirrored(PieceKind.Fence, -8f, 22f, 10f, 0.3f);
            m.Mirrored(PieceKind.Fence, -8f, -22f, 10f, 0.3f);
            m.Mirrored(PieceKind.Fence, -36f, 18f, 0.3f, 8f);
            m.Mirrored(PieceKind.Fence, -36f, -18f, 0.3f, 8f);

            // Hay bales and odds and ends in the fields.
            m.Mirrored(PieceKind.HayBale, -34f, 12f, 2f, 1.2f);
            m.Mirrored(PieceKind.HayBale, -34f, -12f, 2f, 1.2f);
            m.Mirrored(PieceKind.HayBale, -10f, 14f, 1.2f, 2f);
            m.Mirrored(PieceKind.HayBale, -10f, -14f, 1.2f, 2f);
            m.Mirrored(PieceKind.HayBale, -6f, 7f, 2f, 1.2f);
            m.Mirrored(PieceKind.HayBale, -6f, -7f, 2f, 1.2f);
            m.Mirrored(PieceKind.Tires, -12f, 24f, 1.1f, 1.1f);
            m.Mirrored(PieceKind.Pallet, -39f, 20f, 1.2f, 1f);
            m.Mirrored(PieceKind.Pallet, -39f, -20f, 1.2f, 1f);
            m.Mirrored(PieceKind.Tree, -40f, 25f, 1f, 1f);
            m.Mirrored(PieceKind.Tree, -40f, -25f, 1f, 1f);
            m.Mirrored(PieceKind.Tree, -2f, 25f, 1f, 1f);
            m.Mirrored(PieceKind.Bush, -12f, 19f, 2.2f, 2.2f);
            m.Mirrored(PieceKind.Bush, -12f, -19f, 2.2f, 2.2f);

            // The well in the middle of the yard.
            m.Add(PieceKind.Rock, 0f, 0f, 2.2f, 2.2f);
            m.Add(PieceKind.HayBale, 0f, 10f, 2f, 1.2f);
            m.Add(PieceKind.HayBale, 0f, -10f, 2f, 1.2f);
            return m;
        }

        public static MapDefinition TrainingRange { get { return Get("range"); } }

        static MapDefinition Range()
        {
            var m = new MapDefinition
            {
                id = "range",
                name = "Training Range",
                description = "Steel poppers every 10 m out to 60 m.",
                trainingOnly = true,
                ground = GroundStyle.Grass,
                bounds = new Rect(-6f, -7f, 76f, 14f),
                spawnZones = new[] { new Rect(-5f, -3f, 4f, 6f), new Rect(66f, -3f, 3f, 6f) },
                flagPoints = new[] { new Vector2(-3f, 5f), new Vector2(67f, 5f) },
                hill = new Vector2(30f, 0f),
            };
            Border(m, PieceKind.Netting);
            m.Path(3f, new Vector2(-4f, 0f), new Vector2(68f, 0f));
            // Shooting bench: sandbags to crouch behind.
            m.Add(PieceKind.Sandbags, 0f, 4.5f, 1f, 2f);
            m.Add(PieceKind.Sandbags, 0f, -4.5f, 1f, 2f);
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
