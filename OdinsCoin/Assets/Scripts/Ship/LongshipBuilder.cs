using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Builds a low-poly Viking longship from code: a lofted clinker-style hull with rising stem and stern,
    /// deck planks, mast, striped square sail, a row of shields along each side, a dragon prow and a steering oar.
    /// Local space: +Z is the bow, +Y up, the waterline at about y = 0.
    /// </summary>
    public static class LongshipBuilder
    {
        public const float Length = 18f;
        public const float Beam = 4.6f;
        public const float Depth = 1.3f;       // keel below the waterline
        public const float Freeboard = 0.9f;   // gunwale above the waterline, amidships
        public const float DeckHeight = 0.15f;
        public const float MastHeight = 10f;

        public class Parts
        {
            public Transform root;
            public Transform yard;      // turns to catch the wind
            public Transform sail;      // scaled when raising / lowering
            public Transform rudder;    // the steering oar
            public Transform helm;      // where the helmsman stands
            public Transform altar;     // Odin's coin goes here
            public Transform deck;      // what the crew stands on (the ship root)
            public Vector3[] floatPoints;
            public Transform[] rigs;    // the new ship classes: each sail's turning rig and its cloth
            public Transform[] cloths;
        }

        /// <summary>Half-width, keel depth and gunwale height at a station s in -1 (stern) .. 1 (bow).</summary>
        public static void Station(float s, out float halfWidth, out float keel, out float gunwale)
        {
            float a = Mathf.Abs(s);
            halfWidth = Beam / 2f * Mathf.Pow(Mathf.Max(0f, 1f - a * a), 0.55f);
            keel = -Depth * (1f - 0.55f * Mathf.Pow(a, 3f));
            gunwale = Freeboard + 1.5f * Mathf.Pow(a, 5f); // stem and stern sweep up
        }

        public static Parts Build(Transform root) { return Build(root, Materials.Sail, Materials.SailStripe); }

        public static Parts Build(Transform root, Color sailColor, Color stripeColor)
        {
            var parts = new Parts { root = root };

            var hull = new GameObject("Hull");
            hull.transform.SetParent(root, false);
            int[] hullOutside;
            var hullMesh = HullMesh(out hullOutside);
            hull.AddComponent<MeshFilter>().sharedMesh = hullMesh;
            // The hull is two-sided, so it gets its ink line from the outer skin only.
            InkOutline.AddLine(hull.transform, hullMesh.vertices, hullOutside, "Longship Hull");
            var mr = hull.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new[] { Materials.Get(Materials.Wood), Materials.Get(Materials.DarkWood) };

            // Deck planks.
            for (int i = -3; i <= 3; i++)
            {
                float z = i * 2f;
                float hw, k, g;
                Station(z / (Length / 2f), out hw, out k, out g);
                Deco(PrimitiveType.Cube, root, new Vector3(0f, DeckHeight, z), new Vector3(hw * 1.9f, 0.1f, 1.95f), i % 2 == 0 ? Materials.Wood : Materials.DarkWood * 1.2f);
            }

            // Mast, yard and sail.
            Deco(PrimitiveType.Cylinder, root, new Vector3(0f, MastHeight / 2f, 0.5f), new Vector3(0.28f, MastHeight / 2f, 0.28f), Materials.DarkWood);
            var yard = new GameObject("Yard").transform;
            yard.SetParent(root, false);
            yard.localPosition = new Vector3(0f, MastHeight - 1.2f, 0.5f);
            Deco(PrimitiveType.Cylinder, yard, Vector3.zero, new Vector3(0.18f, 3.6f, 0.18f), Materials.DarkWood).localRotation = Quaternion.Euler(0f, 0f, 90f);
            var sail = new GameObject("Sail").transform;
            sail.SetParent(yard, false);
            const int stripes = 7;
            float sailWidth = 6.8f, sailHeight = 6f;
            for (int i = 0; i < stripes; i++)
            {
                float x = -sailWidth / 2f + sailWidth * (i + 0.5f) / stripes;
                Deco(PrimitiveType.Cube, sail, new Vector3(x, -sailHeight / 2f, 0.12f), new Vector3(sailWidth / stripes, sailHeight, 0.04f), i % 2 == 0 ? sailColor : stripeColor);
            }
            parts.yard = yard;
            parts.sail = sail;

            // Shields along the rails, alternating colours.
            Color[] shieldColors = { new Color(0.85f, 0.75f, 0.2f), new Color(0.2f, 0.3f, 0.55f), new Color(0.7f, 0.15f, 0.12f), new Color(0.9f, 0.88f, 0.8f) };
            int n = 0;
            for (float z = -5.5f; z <= 5.6f; z += 1.1f)
            {
                float hw, k, g;
                Station(z / (Length / 2f), out hw, out k, out g);
                for (int side = -1; side <= 1; side += 2)
                {
                    var shield = Deco(PrimitiveType.Cylinder, root, new Vector3(side * (hw + 0.02f), g - 0.15f, z), new Vector3(0.75f, 0.03f, 0.75f), shieldColors[n++ % shieldColors.Length]);
                    shield.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    Deco(PrimitiveType.Sphere, shield, new Vector3(0f, 1.2f, 0f), new Vector3(0.25f, 0.6f, 0.25f), new Color(0.6f, 0.6f, 0.62f));
                }
            }

            // Dragon prow: a curling neck of blocks and a head with red eyes.
            float bowZ = Length / 2f - 0.2f;
            Vector3 p = new Vector3(0f, Freeboard + 1.5f, bowZ);
            for (int i = 0; i < 5; i++)
            {
                p += new Vector3(0f, 0.35f, 0.18f - i * 0.05f);
                Deco(PrimitiveType.Cube, root, p, new Vector3(0.35f - i * 0.02f, 0.4f, 0.35f), Materials.DarkWood).localRotation = Quaternion.Euler(-20f + i * 12f, 0f, 0f);
            }
            var head = Deco(PrimitiveType.Cube, root, p + new Vector3(0f, 0.15f, 0.35f), new Vector3(0.4f, 0.35f, 0.8f), Materials.DarkWood);
            Deco(PrimitiveType.Cube, head, new Vector3(0.52f, 0.2f, 0.25f), new Vector3(0.2f, 0.25f, 0.12f), new Color(0.9f, 0.15f, 0.1f));
            Deco(PrimitiveType.Cube, head, new Vector3(-0.52f, 0.2f, 0.25f), new Vector3(0.2f, 0.25f, 0.12f), new Color(0.9f, 0.15f, 0.1f));
            // Stern post curls up too.
            Deco(PrimitiveType.Cube, root, new Vector3(0f, Freeboard + 2.2f, -Length / 2f + 0.3f), new Vector3(0.3f, 1.4f, 0.3f), Materials.DarkWood).localRotation = Quaternion.Euler(-25f, 0f, 0f);

            // Steering oar on the starboard (right) side at the stern: "styrbord".
            var rudder = new GameObject("Steering Oar").transform;
            rudder.SetParent(root, false);
            rudder.localPosition = new Vector3(Beam / 2f - 0.5f, Freeboard, -Length / 2f + 2.2f);
            Deco(PrimitiveType.Cube, rudder, new Vector3(0.25f, -1f, -0.3f), new Vector3(0.12f, 2.2f, 0.7f), Materials.DarkWood).localRotation = Quaternion.Euler(20f, 0f, 0f);
            Deco(PrimitiveType.Cylinder, rudder, new Vector3(-0.4f, 0.5f, 0.2f), new Vector3(0.1f, 0.7f, 0.1f), Materials.Wood).localRotation = Quaternion.Euler(0f, 0f, 80f);
            parts.rudder = rudder;

            var helm = new GameObject("Helm").transform;
            helm.SetParent(root, false);
            helm.localPosition = new Vector3(Beam / 2f - 1.3f, DeckHeight + 0.05f, -Length / 2f + 2.6f);
            parts.helm = helm;

            var altar = new GameObject("Coin Altar").transform;
            altar.SetParent(root, false);
            altar.localPosition = new Vector3(0f, DeckHeight + 0.05f, 3.2f);
            Deco(PrimitiveType.Cube, altar, new Vector3(0f, 0.45f, 0f), new Vector3(0.9f, 0.9f, 0.9f), Materials.Rock);
            Deco(PrimitiveType.Cylinder, altar, new Vector3(0f, 0.95f, 0f), new Vector3(0.55f, 0.04f, 0.55f), Materials.Gold);
            parts.altar = altar;

            // Float points: four pairs along the hull, a little inside the sides, near the waterline.
            var points = new List<Vector3>();
            foreach (float s in new[] { -0.65f, -0.22f, 0.22f, 0.65f })
            {
                float hw, k, g;
                Station(s, out hw, out k, out g);
                points.Add(new Vector3(-hw * 0.7f, 0f, s * Length / 2f));
                points.Add(new Vector3(hw * 0.7f, 0f, s * Length / 2f));
            }
            parts.floatPoints = points.ToArray();

            // Hull body (its top is the deck people stand on).
            var body = root.gameObject.AddComponent<BoxCollider>();
            body.center = new Vector3(0f, DeckHeight - 0.65f, 0f);
            body.size = new Vector3(Beam * 0.85f, 1.3f, Length * 0.85f);

            // Invisible rails along the gunwales so you don't slide overboard by accident (you can still jump over).
            const int segments = 6;
            for (int i = 0; i < segments; i++)
            {
                float s0 = -0.8f + 1.6f * i / segments, s1 = -0.8f + 1.6f * (i + 1) / segments;
                float hw0, hw1, k, g;
                Station(s0, out hw0, out k, out g);
                Station(s1, out hw1, out k, out g);
                float z0 = s0 * Length / 2f, z1 = s1 * Length / 2f;
                for (int side = -1; side <= 1; side += 2)
                {
                    var rail = new GameObject("Rail").transform;
                    rail.SetParent(root, false);
                    Vector3 a0 = new Vector3(side * hw0 * 0.92f, 0f, z0), a1 = new Vector3(side * hw1 * 0.92f, 0f, z1);
                    rail.localPosition = (a0 + a1) / 2f + Vector3.up * (DeckHeight + 0.35f);
                    rail.localRotation = Quaternion.LookRotation(a1 - a0);
                    var col = rail.gameObject.AddComponent<BoxCollider>();
                    col.size = new Vector3(0.15f, 0.7f, (a1 - a0).magnitude + 0.1f);
                }
            }
            parts.deck = root;
            return parts;
        }

        static Mesh HullMesh(out int[] outsideSkin)
        {
            const int stations = 24;
            // Profile from keel (0) to gunwale (last), for the starboard side: fraction of width, fraction of height.
            float[] widthAt = { 0f, 0.45f, 0.78f, 0.95f, 1f };
            float[] heightAt = { 0f, 0.18f, 0.45f, 0.75f, 1f };
            int rows = widthAt.Length;

            var outside = new List<Vector3>();
            var outsideTris = new List<int>();
            var insideTris = new List<int>();
            var verts = new List<Vector3>();

            System.Func<int, int, int, Vector3> point = (st, row, side) =>
            {
                float s = -1f + 2f * st / stations;
                float hw, keel, gun;
                Station(s, out hw, out keel, out gun);
                float y = Mathf.Lerp(keel, gun, heightAt[row]);
                return new Vector3(side * hw * widthAt[row], y, s * Length / 2f);
            };

            for (int st = 0; st < stations; st++)
            {
                for (int side = -1; side <= 1; side += 2)
                {
                    for (int row = 0; row < rows - 1; row++)
                    {
                        Vector3 a = point(st, row, side), b = point(st + 1, row, side), c = point(st + 1, row + 1, side), d = point(st, row + 1, side);
                        // Outward-facing quad (winding depends on side), then the inside face.
                        if (side > 0) { Tri(verts, outsideTris, a, b, c); Tri(verts, outsideTris, a, c, d); Tri(verts, insideTris, a, c, b); Tri(verts, insideTris, a, d, c); }
                        else { Tri(verts, outsideTris, a, c, b); Tri(verts, outsideTris, a, d, c); Tri(verts, insideTris, a, b, c); Tri(verts, insideTris, a, c, d); }
                    }
                }
            }

            var mesh = new Mesh();
            mesh.name = "Longship Hull";
            mesh.vertices = verts.ToArray();
            mesh.subMeshCount = 2;
            mesh.SetTriangles(outsideTris.ToArray(), 0);
            mesh.SetTriangles(insideTris.ToArray(), 1);
            outsideSkin = outsideTris.ToArray();
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        static void Tri(List<Vector3> verts, List<int> tris, Vector3 a, Vector3 b, Vector3 c)
        {
            // Own vertices per triangle: flat-shaded facets.
            tris.Add(verts.Count); verts.Add(a);
            tris.Add(verts.Count); verts.Add(b);
            tris.Add(verts.Count); verts.Add(c);
        }

        /// <summary>A coloured primitive with its collider removed (decoration only).</summary>
        public static Transform Deco(PrimitiveType type, Transform parent, Vector3 localPos, Vector3 scale, Color color)
        {
            var go = GameObject.CreatePrimitive(type);
            var col = go.GetComponent<Collider>();
            if (col != null) Object.DestroyImmediate(col);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            go.GetComponent<Renderer>().sharedMaterial = Materials.Get(color);
            return go.transform;
        }
    }
}
