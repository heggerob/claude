using UnityEngine;

namespace OdinsCoin
{
    /// <summary>The kinds of building a place is made of.</summary>
    public enum BuildingKind { Longhouse, GreatHall, Boathouse, Storehouse, Watchtower, Palisade, Jetty, Church }

    /// <summary>The colours of the north's buildings: tarred timber, turf and shingle roofs, carved gables.</summary>
    public class BuildingLook
    {
        public Color wall = new Color(0.36f, 0.25f, 0.17f);
        public Color timber = new Color(0.25f, 0.17f, 0.11f);
        public Color turf = new Color(0.38f, 0.48f, 0.25f);
        public Color shingle = new Color(0.3f, 0.2f, 0.15f);
        public Color stone = new Color(0.5f, 0.5f, 0.48f);
        public Color door = new Color(0.55f, 0.18f, 0.1f);
        public Color gold = new Color(0.85f, 0.65f, 0.25f);
        public Color plaster = new Color(0.85f, 0.82f, 0.74f);
    }

    /// <summary>
    /// (Profiles are drawn as x across, y up and extruded along z: <see cref="MeshData.Extrude"/> lays its outline in
    /// the z–y plane, so each is turned a quarter round.)
    /// Builds the north's buildings in the storybook style, as plain mesh data (one joint, "Building"; x across,
    /// z along, y up, ground at 0): longhouses with bowed walls and turf roofs, a jarl's great hall with dragon
    /// gables, boathouses, storehouses on stilts, watchtowers, palisades, jetties, and a little stone church for
    /// the monasteries. Sizes are real (a longhouse is ~25 m, a great hall ~50 m).
    /// </summary>
    public static class Buildings
    {
        public const string Joint = "Building";

        public static VikingModel Build(BuildingKind kind, BuildingLook look, int seed = 0)
        {
            var m = new VikingModel();
            m.AddJoint(Joint, null, Vector3.zero);
            switch (kind)
            {
                case BuildingKind.Longhouse: Longhouse(m, look, 24f, 6.5f, false); break;
                case BuildingKind.GreatHall: Longhouse(m, look, 48f, 11f, true); break;
                case BuildingKind.Boathouse: Boathouse(m, look); break;
                case BuildingKind.Storehouse: Storehouse(m, look); break;
                case BuildingKind.Watchtower: Watchtower(m, look); break;
                case BuildingKind.Palisade: Palisade(m, look, 20f, seed); break;
                case BuildingKind.Jetty: Jetty(m, look, 40f); break;
                case BuildingKind.Church: Church(m, look); break;
            }
            m.AddOutlines(0.06f, new Color(0.08f, 0.06f, 0.05f));
            return m;
        }

        /// <summary>
        /// Make a built building solid: a box round its walls (or, for a jetty, its deck, so you can walk out along
        /// it to your ship). On the building's own transform.
        /// </summary>
        public static void AddSolid(GameObject building, BuildingKind kind)
        {
            var box = building.AddComponent<BoxCollider>();
            if (kind == BuildingKind.Jetty)
            {
                box.center = new Vector3(0f, 1.2f, 20f);
                box.size = new Vector3(4f, 0.3f, 40f);
                return;
            }
            var f = Footprint(kind);
            box.center = new Vector3(0f, f.y / 2f, 0f);
            box.size = new Vector3(2f * f.x * 0.9f, f.y, 2f * f.z * 0.9f);
        }

        /// <summary>How far a building reaches (half-size x, height, half-size z), for laying out a place.</summary>
        public static Vector3 Footprint(BuildingKind kind)
        {
            switch (kind)
            {
                case BuildingKind.Longhouse: return new Vector3(4f, 7f, 13f);
                case BuildingKind.GreatHall: return new Vector3(6.5f, 12f, 25f);
                case BuildingKind.Boathouse: return new Vector3(4.5f, 6f, 11f);
                case BuildingKind.Storehouse: return new Vector3(2.5f, 5.5f, 2.5f);
                case BuildingKind.Watchtower: return new Vector3(2.5f, 14f, 2.5f);
                case BuildingKind.Palisade: return new Vector3(1f, 4f, 10f);
                case BuildingKind.Jetty: return new Vector3(2.5f, 1.5f, 20f);
                default: return new Vector3(4f, 10f, 7f);
            }
        }

        /// <summary>
        /// A longhouse: walls bowed out amidships like a boat's sides, a steep roof of turf (or shingle, on a hall)
        /// that nearly reaches the ground, a door at each end; the great hall gets crossed dragon heads on its gables.
        /// </summary>
        static void Longhouse(VikingModel m, BuildingLook look, float length, float width, bool hall)
        {
            const int n = 12;
            float wallH = hall ? 3.4f : 2.2f, roofH = width * (hall ? 0.75f : 0.65f);
            // Bowed walls: a lathe-like ring of posts, joined into two curved wall sheets.
            foreach (float side in new[] { -1f, 1f })
            {
                var grid = new Vector3[n + 1, 2];
                for (int i = 0; i <= n; i++)
                {
                    float t = i / (float)n * 2f - 1f;
                    float x = side * width / 2f * (1f - 0.12f * t * t);
                    grid[i, 0] = new Vector3(x, 0f, t * length / 2f);
                    grid[i, 1] = new Vector3(x, wallH, t * length / 2f);
                }
                m.Add(Joint, look.wall, CharacterKit.Sheet(grid, new Vector3(side, 0f, 0f), 0.25f), true, SurfaceKind.Wood);
                // Outer posts leaning in against the walls.
                for (int i = 1; i < n; i += 2)
                {
                    float t = i / (float)n * 2f - 1f;
                    float x = side * width / 2f * (1f - 0.12f * t * t);
                    m.Add(Joint, look.timber, MeshData.Tube(new[] { new Vector3(x + side * 0.6f, 0f, t * length / 2f), new Vector3(x + side * 0.1f, wallH * 0.9f, t * length / 2f) }, new[] { 0.14f, 0.11f }, 5), false, SurfaceKind.Wood);
                }
            }
            // Gable ends.
            foreach (float end in new[] { -1f, 1f })
            {
                var gable = new[] { new Vector2(-width / 2f * 0.88f, 0f), new Vector2(width / 2f * 0.88f, 0f), new Vector2(width / 2f * 0.88f, wallH), new Vector2(0f, wallH + roofH), new Vector2(-width / 2f * 0.88f, wallH) };
                m.Add(Joint, look.wall, MeshData.Extrude(gable, 0.25f).Transformed(new Vector3(0f, 0f, end * length / 2f), Quaternion.Euler(0f, 90f, 0f), Vector3.one), true, SurfaceKind.Wood);
                m.Add(Joint, look.door, MeshData.Box(new Vector3(0f, 1.1f, end * (length / 2f + 0.15f)), new Vector3(1.2f, 2.2f, 0.12f)), false, SurfaceKind.Wood);
            }
            // The roof: two curved slopes hanging past the walls, ridge a little saddle-backed (lower in the middle).
            var roofColour = hall ? look.shingle : look.turf;
            foreach (float side in new[] { -1f, 1f })
            {
                var grid = new Vector3[n + 1, 2];
                for (int i = 0; i <= n; i++)
                {
                    float t = i / (float)n * 2f - 1f;
                    float z = t * (length / 2f + 0.8f);
                    float ridge = wallH + roofH - 0.4f * (1f - t * t) * (hall ? 1.5f : 1f);
                    grid[i, 0] = new Vector3(0f, ridge, z);
                    grid[i, 1] = new Vector3(side * (width / 2f + 1f), wallH - 0.8f, z);
                }
                m.Add(Joint, roofColour, CharacterKit.Sheet(grid, new Vector3(side, 1f, 0f), 0.35f), true, hall ? SurfaceKind.Wood : SurfaceKind.Fur);
            }
            // A smoke hole's little hood on the ridge.
            m.Add(Joint, look.timber, MeshData.Box(new Vector3(0f, wallH + roofH - 0.1f, length * 0.12f), new Vector3(1f, 0.6f, 1.2f)), true, SurfaceKind.Wood);
            if (!hall) return;
            // Crossed dragon heads on the gables, gilded.
            foreach (float end in new[] { -1f, 1f })
                foreach (float side in new[] { -1f, 1f })
                {
                    var from = new Vector3(0f, wallH + roofH - 0.2f, end * (length / 2f + 0.8f));
                    var to = from + new Vector3(side * 1.4f, 2.2f, end * 0.6f);
                    m.Add(Joint, look.timber, MeshData.Tube(new[] { from, to, to + new Vector3(side * 0.9f, 0.3f, end * 0.2f) }, new[] { 0.22f, 0.16f, 0.08f }, 6), true, SurfaceKind.Wood);
                    m.Add(Joint, look.gold, MeshData.Ellipsoid(to + new Vector3(side * 0.9f, 0.3f, end * 0.2f), new Vector3(0.3f, 0.25f, 0.45f), 8, 5), false, SurfaceKind.Metal);
                }
            // Carved door posts with a gold band.
            foreach (float end in new[] { -1f, 1f })
                foreach (float side in new[] { -1f, 1f })
                    m.Add(Joint, look.gold, MeshData.Box(new Vector3(side * 0.9f, 1.4f, end * (length / 2f + 0.2f)), new Vector3(0.2f, 2.8f, 0.15f)), false, SurfaceKind.Metal);
        }

        /// <summary>A boathouse (naust) at the water's edge: low stone side walls, a steep roof, open to the sea.</summary>
        static void Boathouse(VikingModel m, BuildingLook look)
        {
            float len = 22f, w = 9f;
            foreach (float side in new[] { -1f, 1f })
                m.Add(Joint, look.stone, MeshData.Box(new Vector3(side * w / 2f, 0.7f, 0f), new Vector3(1.2f, 1.4f, len)), true, SurfaceKind.Plain);
            var back = new[] { new Vector2(-w / 2f, 0f), new Vector2(w / 2f, 0f), new Vector2(w / 2f, 1.4f), new Vector2(0f, 5.8f), new Vector2(-w / 2f, 1.4f) };
            m.Add(Joint, look.wall, MeshData.Extrude(back, 0.3f).Transformed(new Vector3(0f, 0f, -len / 2f), Quaternion.Euler(0f, 90f, 0f), Vector3.one), true, SurfaceKind.Wood);
            foreach (float side in new[] { -1f, 1f })
            {
                var grid = new Vector3[2, 2] { { new Vector3(0f, 5.8f, -len / 2f), new Vector3(side * (w / 2f + 0.8f), 1.1f, -len / 2f) }, { new Vector3(0f, 5.8f, len / 2f), new Vector3(side * (w / 2f + 0.8f), 1.1f, len / 2f) } };
                m.Add(Joint, look.turf, CharacterKit.Sheet(grid, new Vector3(side, 1f, 0f), 0.3f), true, SurfaceKind.Fur);
            }
        }

        /// <summary>A storehouse (stabbur) up on posts with flat stones on top so mice can't climb in.</summary>
        static void Storehouse(VikingModel m, BuildingLook look)
        {
            foreach (float x in new[] { -1.4f, 1.4f })
                foreach (float z in new[] { -1.4f, 1.4f })
                {
                    m.Add(Joint, look.timber, MeshData.Box(new Vector3(x, 0.5f, z), new Vector3(0.3f, 1f, 0.3f)), true, SurfaceKind.Wood);
                    m.Add(Joint, look.stone, MeshData.Box(new Vector3(x, 1.05f, z), new Vector3(0.6f, 0.12f, 0.6f)), false);
                }
            m.Add(Joint, look.wall, MeshData.Box(new Vector3(0f, 2.3f, 0f), new Vector3(4f, 2.4f, 4f)), true, SurfaceKind.Wood);
            var roof = new[] { new Vector2(-2.7f, 0f), new Vector2(2.7f, 0f), new Vector2(0f, 2.2f) };
            m.Add(Joint, look.shingle, MeshData.Extrude(roof, 5f).Transformed(new Vector3(0f, 3.5f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one), true, SurfaceKind.Wood);
            m.Add(Joint, look.door, MeshData.Box(new Vector3(0f, 2.1f, 2.05f), new Vector3(1f, 1.7f, 0.1f)), false, SurfaceKind.Wood);
        }

        /// <summary>A watchtower of four leaning legs, a platform with a rail, a little roof and a signal fire basket.</summary>
        static void Watchtower(VikingModel m, BuildingLook look)
        {
            float top = 10f;
            foreach (float x in new[] { -1f, 1f })
                foreach (float z in new[] { -1f, 1f })
                    m.Add(Joint, look.timber, MeshData.Tube(new[] { new Vector3(x * 2.4f, 0f, z * 2.4f), new Vector3(x * 1.4f, top + 2.2f, z * 1.4f) }, new[] { 0.22f, 0.16f }, 6), true, SurfaceKind.Wood);
            m.Add(Joint, look.wall, MeshData.Box(new Vector3(0f, top, 0f), new Vector3(3.6f, 0.3f, 3.6f)), true, SurfaceKind.Wood);
            foreach (float s in new[] { -1f, 1f })
            {
                m.Add(Joint, look.timber, MeshData.Box(new Vector3(s * 1.75f, top + 0.8f, 0f), new Vector3(0.12f, 0.12f, 3.6f)), false, SurfaceKind.Wood);
                m.Add(Joint, look.timber, MeshData.Box(new Vector3(0f, top + 0.8f, s * 1.75f), new Vector3(3.6f, 0.12f, 0.12f)), false, SurfaceKind.Wood);
            }
            var roof = new[] { new Vector2(-2.3f, 0f), new Vector2(2.3f, 0f), new Vector2(0f, 1.6f) };
            m.Add(Joint, look.shingle, MeshData.Extrude(roof, 4.2f).Transformed(new Vector3(0f, top + 2.2f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one), true, SurfaceKind.Wood);
            m.Add(Joint, look.stone, MeshData.Lathe(new[] { new Vector2(0.5f, 0f), new Vector2(0.7f, 0.5f) }, 10).Transformed(new Vector3(1.1f, top + 0.15f, 1.1f), Quaternion.identity, Vector3.one), true);
        }

        /// <summary>A stretch of palisade: sharpened stakes of uneven height with a walkway behind.</summary>
        static void Palisade(VikingModel m, BuildingLook look, float length, int seed)
        {
            var rng = new System.Random(seed + 17);
            for (float z = -length / 2f; z <= length / 2f; z += 0.45f)
            {
                float h = 3.4f + (float)rng.NextDouble() * 0.5f;
                m.Add(Joint, look.timber, MeshData.Tube(new[] { new Vector3(0f, 0f, z), new Vector3(0f, h, z), new Vector3(0f, h + 0.5f, z) }, new[] { 0.22f, 0.22f, 0.02f }, 6), true, SurfaceKind.Wood);
            }
            m.Add(Joint, look.wall, MeshData.Box(new Vector3(-1f, 2f, 0f), new Vector3(1.6f, 0.2f, length)), false, SurfaceKind.Wood);
        }

        /// <summary>A jetty on piles running out from the shore (+z), with mooring posts.</summary>
        static void Jetty(VikingModel m, BuildingLook look, float length)
        {
            m.Add(Joint, look.wall, MeshData.Box(new Vector3(0f, 1.2f, length / 2f), new Vector3(4f, 0.3f, length)), true, SurfaceKind.Wood);
            for (float z = 2f; z <= length; z += 4f)
                foreach (float x in new[] { -1.8f, 1.8f })
                {
                    m.Add(Joint, look.timber, MeshData.Tube(new[] { new Vector3(x, -4f, z), new Vector3(x, 1.8f, z) }, new[] { 0.2f, 0.18f }, 6), true, SurfaceKind.Wood);
                }
        }

        /// <summary>A small stone church with a bell gable: what the monasteries look like to a raider.</summary>
        static void Church(VikingModel m, BuildingLook look)
        {
            m.Add(Joint, look.plaster, MeshData.Box(new Vector3(0f, 2.5f, 0f), new Vector3(7f, 5f, 12f)), true, SurfaceKind.Plain);
            var roof = new[] { new Vector2(-4.2f, 0f), new Vector2(4.2f, 0f), new Vector2(0f, 3.4f) };
            m.Add(Joint, look.shingle, MeshData.Extrude(roof, 12.6f).Transformed(new Vector3(0f, 5f, 0f), Quaternion.Euler(0f, 90f, 0f), Vector3.one), true, SurfaceKind.Wood);
            m.Add(Joint, look.plaster, MeshData.Box(new Vector3(0f, 7f, 6.3f), new Vector3(2.2f, 4f, 0.8f)), true, SurfaceKind.Plain);
            m.Add(Joint, look.gold, MeshData.Box(new Vector3(0f, 9.6f, 6.3f), new Vector3(0.18f, 1.4f, 0.18f)), false, SurfaceKind.Metal);
            m.Add(Joint, look.gold, MeshData.Box(new Vector3(0f, 9.9f, 6.3f), new Vector3(0.8f, 0.18f, 0.18f)), false, SurfaceKind.Metal);
            m.Add(Joint, look.door, MeshData.Box(new Vector3(0f, 1.3f, 6.05f), new Vector3(1.4f, 2.6f, 0.12f)), false, SurfaceKind.Wood);
        }
    }
}
