using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>Marks the ink shell drawn around a mesh, so it's never outlined itself.</summary>
    public class InkShell : MonoBehaviour { }

    /// <summary>Put on a root whose meshes already carry their own ink (the storybook heroes): left alone.</summary>
    public class OwnInk : MonoBehaviour { }

    /// <summary>
    /// Ink lines around the world: every solid storybook mesh (ship, islands, trees, houses, chests...) gets a
    /// shell drawn with <c>OdinsCoin/InkOutline</c>, which pushes it out along smoothed normals by a fixed number of
    /// screen pixels and shows only its back faces, so a dark line hugs the silhouette like a pen stroke.
    /// </summary>
    public static class InkOutline
    {
        public const string ShaderName = "OdinsCoin/InkOutline";
        public static readonly Color Ink = new Color(0.08f, 0.065f, 0.055f);
        /// <summary>Line width in pixels up close (it thins out with distance).</summary>
        public const float Width = 2.2f;

        static Material material;
        static bool looked;
        static readonly Dictionary<Mesh, Mesh> shells = new Dictionary<Mesh, Mesh>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { material = null; looked = false; shells.Clear(); solid.Clear(); }

        static Material Material
        {
            get
            {
                if (looked) return material;
                looked = true;
                var shader = Shader.Find(ShaderName);
                if (shader == null || !shader.isSupported) return null;
                material = new Material(shader);
                material.color = Ink;
                if (material.HasProperty("_Width")) material.SetFloat("_Width", Width);
                return material;
            }
        }

        /// <summary>
        /// Outlines every mesh under <paramref name="root"/> that is drawn in the storybook style and hasn't got a
        /// line yet. Safe to call again and again (new raiders, chests and serpents get theirs on the next pass).
        /// Returns how many new lines were added.
        /// </summary>
        public static int OutlineAll(Transform root)
        {
            var mat = Material;
            if (mat == null || root == null) return 0;
            int added = 0;
            foreach (var r in root.GetComponentsInChildren<MeshRenderer>(true))
            {
                if (!Wants(r)) continue;
                var mf = r.GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null || !IsSolid(mf.sharedMesh)) continue;
                Attach(r.transform, ShellOf(mf.sharedMesh), mat);
                added++;
            }
            return added;
        }

        /// <summary>
        /// An ink line for a mesh the scanner can't judge by itself (a two-sided hull, open terrain): pass just the
        /// outward-facing triangles. Nothing happens if the ink shader isn't available.
        /// </summary>
        public static void AddLine(Transform target, Vector3[] verts, int[] outwardTris, string name)
        {
            var mat = Material;
            if (mat == null || target == null) return;
            var shell = new Mesh();
            shell.name = name + " Ink";
            if (verts.Length > 65000) shell.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            shell.vertices = verts;
            shell.normals = SmoothNormals(verts, outwardTris);
            shell.triangles = outwardTris;
            shell.RecalculateBounds();
            Attach(target, shell, mat);
        }

        static void Attach(Transform target, Mesh shell, Material mat)
        {
            var go = new GameObject("Ink Line");
            go.transform.SetParent(target, false);
            go.AddComponent<InkShell>();
            go.AddComponent<MeshFilter>().sharedMesh = shell;
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = mat;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        static bool Wants(MeshRenderer r)
        {
            if (r.GetComponent<InkShell>() != null || r.GetComponentInParent<OwnInk>() != null) return false;
            var m = r.sharedMaterial;
            // Only solid storybook things: the sea, glass and glowing bits keep a clean edge.
            if (m == null || m.shader == null || m.shader.name != InkStyle.ShaderName) return false;
            for (int i = 0; i < r.transform.childCount; i++)
            {
                var c = r.transform.GetChild(i);
                if (c != null && c.GetComponent<InkShell>() != null) return false;
            }
            return true;
        }

        static readonly Dictionary<Mesh, bool> solid = new Dictionary<Mesh, bool>();

        static bool IsSolid(Mesh mesh)
        {
            bool s;
            if (!solid.TryGetValue(mesh, out s)) solid[mesh] = s = IsSolid(mesh.vertices, mesh.triangles);
            return s;
        }

        /// <summary>
        /// Whether a mesh encloses space. Paper-thin things (sails, banners, two-sided sheets) enclose none, and a
        /// shell around them would paint them black from behind, so they're drawn without a line.
        /// </summary>
        public static bool IsSolid(Vector3[] verts, int[] tris)
        {
            if (verts == null || tris == null || verts.Length == 0 || tris.Length < 12) return false;
            Vector3 min = verts[0], max = verts[0];
            foreach (var v in verts) { min = Vector3.Min(min, v); max = Vector3.Max(max, v); }
            Vector3 size = max - min;
            float thinnest = Mathf.Min(size.x, Mathf.Min(size.y, size.z)), biggest = Mathf.Max(size.x, Mathf.Max(size.y, size.z));
            if (biggest <= 0f || thinnest < biggest * 0.01f) return false;
            float volume = 0f;
            for (int t = 0; t + 2 < tris.Length; t += 3)
                volume += Vector3.Dot(verts[tris[t]], Vector3.Cross(verts[tris[t + 1]], verts[tris[t + 2]])) / 6f;
            return Mathf.Abs(volume) > size.x * size.y * size.z * 0.05f;
        }

        /// <summary>The same triangles with normals averaged over every corner that shares a position, so hard-edged
        /// meshes don't crack open when pushed out. Shared between everything using the same mesh.</summary>
        public static Mesh ShellOf(Mesh source)
        {
            Mesh shell;
            if (shells.TryGetValue(source, out shell) && shell != null) return shell;
            var verts = source.vertices;
            shell = new Mesh();
            shell.name = source.name + " Ink";
            if (verts.Length > 65000) shell.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            shell.vertices = verts;
            shell.normals = SmoothNormals(verts, source.triangles);
            shell.triangles = source.triangles;
            shell.RecalculateBounds();
            shells[source] = shell;
            return shell;
        }

        /// <summary>Area-weighted normals, welded by position (to a millimetre).</summary>
        public static Vector3[] SmoothNormals(Vector3[] verts, int[] tris)
        {
            var key = new int[verts.Length];
            var ids = new Dictionary<long, int>();
            var sums = new List<Vector3>();
            for (int i = 0; i < verts.Length; i++)
            {
                long k = Quantize(verts[i]);
                int id;
                if (!ids.TryGetValue(k, out id)) { id = sums.Count; ids[k] = id; sums.Add(Vector3.zero); }
                key[i] = id;
            }
            for (int t = 0; t + 2 < tris.Length; t += 3)
            {
                Vector3 a = verts[tris[t]], b = verts[tris[t + 1]], c = verts[tris[t + 2]];
                Vector3 n = Vector3.Cross(b - a, c - a); // length = twice the area
                sums[key[tris[t]]] += n; sums[key[tris[t + 1]]] += n; sums[key[tris[t + 2]]] += n;
            }
            var normals = new Vector3[verts.Length];
            for (int i = 0; i < verts.Length; i++)
            {
                Vector3 n = sums[key[i]];
                normals[i] = n.sqrMagnitude > 1e-12f ? n.normalized : Vector3.up;
            }
            return normals;
        }

        static long Quantize(Vector3 v)
        {
            long x = (long)Mathf.Round(v.x * 1000f) & 0x1FFFFF, y = (long)Mathf.Round(v.y * 1000f) & 0x1FFFFF, z = (long)Mathf.Round(v.z * 1000f) & 0x1FFFFF;
            return (x << 42) | (y << 21) | z;
        }
    }

    /// <summary>Keeps the world inked: outlines everything under its object now and every couple of seconds after.</summary>
    public class InkOutliner : MonoBehaviour
    {
        float next;

        void Start() { InkOutline.OutlineAll(transform); }

        void Update()
        {
            if (Time.time < next) return;
            next = Time.time + 2f;
            InkOutline.OutlineAll(transform);
        }
    }
}
