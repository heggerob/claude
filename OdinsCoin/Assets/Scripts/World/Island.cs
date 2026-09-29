using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>Settings for one generated island.</summary>
    public class IslandSpec
    {
        public string name;
        public Vector2 centre;      // world x, z
        public float radius = 40f;
        public float height = 14f;
        public int seed = 1;
        public bool monastery;
        public int trees = 25;
        public int chests = 2;
    }

    /// <summary>
    /// A low-poly island: a noisy dome of land rising out of the sea with a sandy beach, grassy slopes,
    /// rocky crags, pine trees, maybe a monastery to raid, and treasure chests hidden around it.
    /// </summary>
    public class Island : MonoBehaviour
    {
        const int Grid = 56;
        public IslandSpec Spec { get; private set; }
        public readonly List<TreasureChest> Chests = new List<TreasureChest>();

        public static float SandLevel = 1.9f;

        /// <summary>Terrain height (world y) of an island at a world position. Negative = under water.</summary>
        public static float Height(IslandSpec s, float x, float z)
        {
            float dx = x - s.centre.x, dz = z - s.centre.y;
            float d = Mathf.Sqrt(dx * dx + dz * dz);
            // Wobble the coastline so islands aren't circles.
            float angle = Mathf.Atan2(dz, dx);
            float coast = s.radius * (0.8f + 0.35f * Noise(Mathf.Cos(angle) * 1.7f + s.seed, Mathf.Sin(angle) * 1.7f));
            float t = d / coast;
            if (t >= 1.25f) return -4f;
            // Dome shape, then hills and crags on top.
            float dome = Mathf.Clamp01(1f - t * t);
            float hills = Noise(x * 0.035f + s.seed * 13.1f, z * 0.035f) * 0.6f + Noise(x * 0.09f, z * 0.09f + s.seed * 7.7f) * 0.25f;
            float h = s.height * dome * (0.55f + hills) - 3.2f * Mathf.Clamp01((t - 0.75f) * 2f);
            return h + 0.6f;
        }

        // Cheap smooth noise 0..1 (sum of sines, deterministic).
        static float Noise(float x, float y)
        {
            return 0.5f + 0.25f * Mathf.Sin(x * 1.3f + Mathf.Sin(y * 0.7f) * 2f) + 0.25f * Mathf.Sin(y * 1.1f + Mathf.Cos(x * 0.9f) * 2f);
        }

        public static Island Create(Transform parent, IslandSpec spec)
        {
            var go = new GameObject("Island: " + spec.name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(spec.centre.x, 0f, spec.centre.y);
            var island = go.AddComponent<Island>();
            island.Spec = spec;
            island.BuildTerrain();
            island.Decorate();
            return island;
        }

        void BuildTerrain()
        {
            var s = Spec;
            float size = s.radius * 2.8f, step = size / Grid, half = size / 2f;
            var verts = new List<Vector3>();
            var sand = new List<int>();
            var grass = new List<int>();
            var rock = new List<int>();

            System.Func<int, int, Vector3> P = (i, j) =>
            {
                float x = -half + i * step, z = -half + j * step;
                return new Vector3(x, Height(s, s.centre.x + x, s.centre.y + z), z);
            };

            for (int j = 0; j < Grid; j++)
            {
                for (int i = 0; i < Grid; i++)
                {
                    Vector3 a = P(i, j), b = P(i, j + 1), c = P(i + 1, j + 1), d = P(i + 1, j);
                    if (a.y < -3.5f && b.y < -3.5f && c.y < -3.5f && d.y < -3.5f) continue; // deep sea: skip
                    AddTri(verts, sand, grass, rock, a, b, c);
                    AddTri(verts, sand, grass, rock, a, c, d);
                }
            }

            var mesh = new Mesh();
            mesh.name = "Island Terrain";
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            mesh.vertices = verts.ToArray();
            mesh.subMeshCount = 3;
            mesh.SetTriangles(sand.ToArray(), 0);
            mesh.SetTriangles(grass.ToArray(), 1);
            mesh.SetTriangles(rock.ToArray(), 2);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();

            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            gameObject.AddComponent<MeshRenderer>().sharedMaterials = new[] { Materials.Get(Materials.Sand), Materials.Get(Materials.Grass), Materials.Get(Materials.Rock) };
            gameObject.AddComponent<MeshCollider>().sharedMesh = mesh;
        }

        static void AddTri(List<Vector3> verts, List<int> sand, List<int> grass, List<int> rock, Vector3 a, Vector3 b, Vector3 c)
        {
            // Sort each facet by height and steepness: beaches low, rock where it's steep, grass elsewhere.
            float y = (a.y + b.y + c.y) / 3f;
            Vector3 n = Vector3.Cross(b - a, c - a).normalized;
            var list = y < SandLevel ? sand : n.y < 0.72f ? rock : grass;
            MeshKit.Tri(verts, list, a, b, c);
        }

        void Decorate()
        {
            var s = Spec;
            var rng = new System.Random(s.seed * 97 + 3);
            var used = new List<Vector2>();

            // The monastery goes on the highest reasonably flat spot near the middle.
            if (s.monastery)
            {
                Vector2 spot = FlatSpot(rng, 0.35f, used);
                used.Add(spot);
                BuildMonastery(spot, rng);
            }

            for (int i = 0; i < s.chests; i++)
            {
                Vector2 p = RandomLand(rng, 0.15f, 0.8f, used, 3f);
                used.Add(p);
                var chest = TreasureChest.Create(transform, Ground(p), (float)rng.NextDouble() * 360f, 40 + rng.Next(80));
                Chests.Add(chest);
            }

            for (int i = 0; i < s.trees; i++)
            {
                Vector2 p = RandomLand(rng, 0.1f, 0.85f, used, 2.2f);
                used.Add(p);
                Tree(Ground(p), 0.8f + (float)rng.NextDouble() * 0.7f);
            }

            // A few boulders on the beach.
            for (int i = 0; i < 5; i++)
            {
                Vector2 p = RandomLand(rng, 0.8f, 1.05f, used, 2f);
                var rockT = LongshipBuilder.Deco(PrimitiveType.Sphere, transform, Ground(p) - transform.position, Vector3.one * (1f + (float)rng.NextDouble() * 1.6f), Materials.Rock);
                rockT.localRotation = Quaternion.Euler((float)rng.NextDouble() * 40f, (float)rng.NextDouble() * 360f, 0f);
            }
        }

        Vector3 Ground(Vector2 world) { return new Vector3(world.x, Height(Spec, world.x, world.y), world.y); }

        Vector2 RandomLand(System.Random rng, float minT, float maxT, List<Vector2> avoid, float spacing)
        {
            var s = Spec;
            for (int attempt = 0; attempt < 60; attempt++)
            {
                float a = (float)rng.NextDouble() * Mathf.PI * 2f;
                float r = s.radius * Mathf.Lerp(minT, maxT, (float)rng.NextDouble());
                var p = s.centre + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
                if (Height(s, p.x, p.y) < SandLevel + 0.2f && maxT < 0.9f) continue;
                bool clear = true;
                foreach (var q in avoid) if (Vector2.Distance(p, q) < spacing) { clear = false; break; }
                if (clear) return p;
            }
            return s.centre;
        }

        Vector2 FlatSpot(System.Random rng, float maxT, List<Vector2> avoid)
        {
            var s = Spec;
            Vector2 best = s.centre;
            float bestScore = float.MaxValue;
            for (int i = 0; i < 40; i++)
            {
                var p = RandomLand(rng, 0f, maxT, avoid, 12f);
                float h = Height(s, p.x, p.y);
                float slope = 0f;
                foreach (var o in new[] { new Vector2(5f, 0f), new Vector2(-5f, 0f), new Vector2(0f, 5f), new Vector2(0f, -5f) })
                    slope += Mathf.Abs(Height(s, p.x + o.x, p.y + o.y) - h);
                if (slope < bestScore) { bestScore = slope; best = p; }
            }
            return best;
        }

        void Tree(Vector3 at, float size)
        {
            var t = new GameObject("Pine").transform;
            t.SetParent(transform, false);
            t.position = at;
            LongshipBuilder.Deco(PrimitiveType.Cylinder, t, new Vector3(0f, 1.2f * size, 0f), new Vector3(0.35f, 1.2f, 0.35f) * size, Materials.DarkWood);
            var pine = new Color(0.16f, 0.34f, 0.2f);
            MeshKit.Show(MeshKit.Cone(7), t, new Vector3(0f, 1.6f * size, 0f), new Vector3(3.2f, 3f, 3.2f) * size, pine, "Branches");
            MeshKit.Show(MeshKit.Cone(7), t, new Vector3(0f, 3.2f * size, 0f), new Vector3(2.4f, 2.6f, 2.4f) * size, pine * 1.15f, "Branches");
            MeshKit.Show(MeshKit.Cone(7), t, new Vector3(0f, 4.6f * size, 0f), new Vector3(1.5f, 2.2f, 1.5f) * size, pine * 1.3f, "Top");
            var col = t.gameObject.AddComponent<CapsuleCollider>();
            col.center = new Vector3(0f, 1.5f * size, 0f);
            col.radius = 0.35f * size;
            col.height = 3f * size;
        }

        /// <summary>A small stone monastery: walls with a door, a pitched roof and a bell tower. Guards come in item 6.</summary>
        void BuildMonastery(Vector2 spot, System.Random rng)
        {
            var root = new GameObject("Monastery").transform;
            root.SetParent(transform, false);
            float ground = Height(Spec, spot.x, spot.y);
            root.position = new Vector3(spot.x, ground - 0.3f, spot.y);
            root.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);
            var stone = new Color(0.72f, 0.7f, 0.64f);
            var roof = new Color(0.45f, 0.24f, 0.18f);
            const float w = 10f, l = 14f, h = 4.5f, t = 0.7f;

            Wall(root, new Vector3(0f, h / 2f, l / 2f), new Vector3(w, h, t), stone);                   // back
            Wall(root, new Vector3(-w / 2f, h / 2f, 0f), new Vector3(t, h, l), stone);                  // sides
            Wall(root, new Vector3(w / 2f, h / 2f, 0f), new Vector3(t, h, l), stone);
            Wall(root, new Vector3(-w / 2f + 1.9f, h / 2f, -l / 2f), new Vector3(3.8f, h, t), stone);   // front, with a door
            Wall(root, new Vector3(w / 2f - 1.9f, h / 2f, -l / 2f), new Vector3(3.8f, h, t), stone);
            Wall(root, new Vector3(0f, h - 0.6f, -l / 2f), new Vector3(2.4f, 1.2f, t), stone);          // lintel
            Wall(root, new Vector3(0f, 0.15f, 0f), new Vector3(w, 0.3f, l), new Color(0.5f, 0.42f, 0.32f)); // floor
            MeshKit.Show(MeshKit.Roof(), root, new Vector3(0f, h, 0f), new Vector3(w + 1.2f, 3.4f, l + 1f), roof, "Roof");

            // Bell tower with a pointed roof and a cross.
            Wall(root, new Vector3(w / 2f + 1.6f, 4f, l / 2f - 1.6f), new Vector3(3f, 8f, 3f), stone);
            MeshKit.Show(MeshKit.Cone(4), root, new Vector3(w / 2f + 1.6f, 8f, l / 2f - 1.6f), new Vector3(4.2f, 3f, 4.2f), roof, "Spire");
            LongshipBuilder.Deco(PrimitiveType.Cube, root, new Vector3(w / 2f + 1.6f, 11.6f, l / 2f - 1.6f), new Vector3(0.18f, 1.2f, 0.18f), Materials.Gold);
            LongshipBuilder.Deco(PrimitiveType.Cube, root, new Vector3(w / 2f + 1.6f, 11.8f, l / 2f - 1.6f), new Vector3(0.7f, 0.16f, 0.16f), Materials.Gold);

            // The good stuff is inside.
            Chests.Add(TreasureChest.Create(root, root.TransformPoint(new Vector3(0f, 0.3f, l / 2f - 1.5f)), root.eulerAngles.y + 180f, 180 + rng.Next(120)));

            // Guards: a few outside the door, one inside with the treasure.
            foreach (var local in new[] { new Vector3(-2.5f, 0.3f, -l / 2f - 3f), new Vector3(2.5f, 0.3f, -l / 2f - 3f), new Vector3(-w / 2f - 3f, 0.3f, 0f), new Vector3(0f, 0.3f, l / 2f - 4f) })
            {
                Vector3 p = root.TransformPoint(local);
                p.y = Mathf.Max(p.y, Height(Spec, p.x, p.z));
                Saxon.Create(transform, p);
            }
        }

        static void Wall(Transform parent, Vector3 pos, Vector3 size, Color color)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "Wall";
            go.transform.SetParent(parent, false);
            go.transform.localPosition = pos;
            go.transform.localScale = size;
            go.GetComponent<Renderer>().sharedMaterial = Materials.Get(color);
        }
    }
}
