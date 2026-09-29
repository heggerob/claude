using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// A flat-shaded low-poly sea that follows the camera. Every triangle has its own vertices so the
    /// facets catch the light, Sea of Thieves meets Windwaker. Heights come from <see cref="Waves"/>.
    /// </summary>
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class Ocean : MonoBehaviour
    {
        public int cells = 70;
        public float cellSize = 2.5f;

        Mesh mesh;
        Vector3[] vertices;
        Vector2[] gridXZ; // local grid position of every vertex
        Transform follow;

        public static Ocean Create(Transform parent)
        {
            var go = new GameObject("Ocean");
            go.transform.SetParent(parent, false);
            go.AddComponent<MeshFilter>();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = Materials.Get(Materials.SeaDeep, 0.75f);
            var ocean = go.AddComponent<Ocean>();
            ocean.Build();
            return ocean;
        }

        public void Follow(Transform target) { follow = target; }

        void Build()
        {
            int quads = cells * cells;
            vertices = new Vector3[quads * 6];
            gridXZ = new Vector2[vertices.Length];
            var triangles = new int[vertices.Length];
            float half = cells * cellSize / 2f;
            int v = 0;
            for (int z = 0; z < cells; z++)
            {
                for (int x = 0; x < cells; x++)
                {
                    float x0 = x * cellSize - half, z0 = z * cellSize - half, x1 = x0 + cellSize, z1 = z0 + cellSize;
                    // Alternate the diagonal so the facets don't all line up.
                    bool flip = ((x + z) & 1) == 0;
                    Vector2 a = new Vector2(x0, z0), b = new Vector2(x0, z1), c = new Vector2(x1, z1), d = new Vector2(x1, z0);
                    Vector2[] tri = flip ? new[] { a, b, c, a, c, d } : new[] { a, b, d, b, c, d };
                    for (int i = 0; i < 6; i++)
                    {
                        gridXZ[v] = tri[i];
                        triangles[v] = v;
                        v++;
                    }
                }
            }
            mesh = new Mesh();
            mesh.name = "Ocean";
            // More than 65k vertices needs 32-bit indices.
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            UpdateVertices();
            mesh.vertices = vertices;
            mesh.triangles = triangles;
            mesh.RecalculateNormals();
            mesh.bounds = new Bounds(Vector3.zero, new Vector3(cells * cellSize, 20f, cells * cellSize));
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        void LateUpdate()
        {
            if (follow != null)
            {
                // Snap to the grid so waves don't "swim" as the mesh moves along.
                Vector3 p = follow.position;
                float snap = cellSize * 2f;
                transform.position = new Vector3(Mathf.Round(p.x / snap) * snap, 0f, Mathf.Round(p.z / snap) * snap);
            }
            UpdateVertices();
            mesh.vertices = vertices;
            mesh.RecalculateNormals();
        }

        void UpdateVertices()
        {
            Vector3 origin = transform.position;
            float t = Waves.Time;
            for (int i = 0; i < vertices.Length; i++)
            {
                Vector2 g = gridXZ[i];
                vertices[i] = new Vector3(g.x, Waves.Height(origin.x + g.x, origin.z + g.y, t), g.y);
            }
        }
    }
}
