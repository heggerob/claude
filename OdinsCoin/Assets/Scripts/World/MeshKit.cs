using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>Small helpers for building flat-shaded low-poly meshes from code.</summary>
    public static class MeshKit
    {
        static readonly Dictionary<string, Mesh> cache = new Dictionary<string, Mesh>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cache.Clear(); }

        /// <summary>A cone standing on y = 0, tip at y = 1, radius 0.5 (scale it). Used for pine trees and tents.</summary>
        public static Mesh Cone(int sides)
        {
            string key = "cone" + sides;
            Mesh m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            Vector3 tip = new Vector3(0f, 1f, 0f), centre = Vector3.zero;
            for (int i = 0; i < sides; i++)
            {
                float a0 = i / (float)sides * Mathf.PI * 2f, a1 = (i + 1) / (float)sides * Mathf.PI * 2f;
                Vector3 p0 = new Vector3(Mathf.Cos(a0) * 0.5f, 0f, Mathf.Sin(a0) * 0.5f);
                Vector3 p1 = new Vector3(Mathf.Cos(a1) * 0.5f, 0f, Mathf.Sin(a1) * 0.5f);
                Tri(verts, tris, p0, tip, p1);
                Tri(verts, tris, p1, centre, p0);
            }
            m = Build("Cone", verts, tris);
            cache[key] = m;
            return m;
        }

        /// <summary>A triangular prism for pitched roofs: 1 wide (x), 1 high (y), 1 long (z), ridge on top.</summary>
        public static Mesh Roof()
        {
            Mesh m;
            if (cache.TryGetValue("roof", out m) && m != null) return m;
            var verts = new List<Vector3>();
            var tris = new List<int>();
            Vector3 a = new Vector3(-0.5f, 0f, -0.5f), b = new Vector3(0.5f, 0f, -0.5f), c = new Vector3(0f, 1f, -0.5f);
            Vector3 d = new Vector3(-0.5f, 0f, 0.5f), e = new Vector3(0.5f, 0f, 0.5f), f = new Vector3(0f, 1f, 0.5f);
            Tri(verts, tris, a, c, b); // gable ends
            Tri(verts, tris, d, e, f);
            Quad(verts, tris, a, d, f, c); // roof slopes
            Quad(verts, tris, b, c, f, e);
            Quad(verts, tris, a, b, e, d); // underside
            m = Build("Roof", verts, tris);
            cache["roof"] = m;
            return m;
        }

        public static void Tri(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c)
        {
            tris.Add(verts.Count); verts.Add(a);
            tris.Add(verts.Count); verts.Add(b);
            tris.Add(verts.Count); verts.Add(c);
        }

        public static void Quad(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
        {
            Tri(verts, tris, a, b, c);
            Tri(verts, tris, a, c, d);
        }

        static Mesh Build(string name, List<Vector3> verts, List<int> tris)
        {
            var m = new Mesh();
            m.name = name;
            m.vertices = verts.ToArray();
            m.triangles = tris.ToArray();
            m.RecalculateNormals();
            m.RecalculateBounds();
            return m;
        }

        /// <summary>A GameObject showing a mesh in a colour, without a collider.</summary>
        public static Transform Show(Mesh mesh, Transform parent, Vector3 localPos, Vector3 scale, Color color, string name = "Mesh")
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.AddComponent<MeshFilter>().sharedMesh = mesh;
            go.AddComponent<MeshRenderer>().sharedMaterial = Materials.Get(color);
            return go.transform;
        }
    }
}
