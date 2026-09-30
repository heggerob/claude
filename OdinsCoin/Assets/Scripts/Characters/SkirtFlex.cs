using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Skirts, coats and tunics that follow the legs: everything on the body below the hips is swung about the
    /// hip by part of each thigh's swing, the left side with the left leg and the right side with the right, so
    /// a stride pushes the cloth forward instead of the knee going through it. Pure maths, shared by the game
    /// (<see cref="SkirtFlexer"/>) and the preview.
    /// </summary>
    public static class SkirtFlex
    {
        /// <summary>How much of a thigh's swing the cloth over it takes.</summary>
        public const float Follow = 0.75f;

        /// <summary>How far forward a leg points (degrees; negative is back), from its local rotation.</summary>
        public static float LegPitch(Quaternion legLocal)
        {
            Vector3 d = legLocal * Vector3.down;
            return Mathf.Atan2(d.z, -d.y) * Mathf.Rad2Deg;
        }

        /// <summary>A leg's swing away from standing (its rest turn under the body's lean).</summary>
        public static float Swing(Quaternion legLocal) { return LegPitch(legLocal) - LegPitch(Quaternion.Euler(-HeroPose.Lean, 0f, 0f)); }

        /// <summary>
        /// The turn for a point <paramref name="p"/> (body space) below the hips (at <paramref name="hipY"/>, legs
        /// <paramref name="hipX"/> out to each side), for the left and right legs' swings (degrees forward).
        /// </summary>
        public static Quaternion Turn(Vector3 p, float hipY, float hipX, float leftSwing, float rightSwing)
        {
            if (p.y >= hipY) return Quaternion.identity;
            // Which leg's cloth it is: the left side follows the left leg, the right the right, blending across the middle.
            float right = Mathf.SmoothStep(0f, 1f, (p.x + hipX * 1.2f) / (hipX * 2.4f));
            // Eased in just below the hips so the belt line doesn't crease.
            float depth = Mathf.Clamp01((hipY - p.y) / 0.12f);
            float swing = Mathf.Lerp(leftSwing, rightSwing, right) * Follow * depth;
            return Quaternion.Euler(-swing, 0f, 0f);
        }

        public static Vector3 Apply(Vector3 p, float hipY, float hipX, float leftSwing, float rightSwing)
        {
            var hip = new Vector3(0f, hipY, 0f);
            return hip + Turn(p, hipY, hipX, leftSwing, rightSwing) * (p - hip);
        }
    }

    /// <summary>Bends a hero's body meshes below the hips with the legs each frame (see <see cref="SkirtFlex"/>).</summary>
    public class SkirtFlexer : MonoBehaviour
    {
        class Flexed
        {
            public Mesh mesh;
            public Vector3[] rest, restNormals, verts, normals;
            public int[] below;
        }

        readonly List<Flexed> meshes = new List<Flexed>();
        Transform leftLeg, rightLeg;
        float hipY, hipX, lastLeft = float.NaN, lastRight = float.NaN;

        public void Init(Transform body, Transform left, Transform right, float hip, float legX)
        {
            leftLeg = left;
            rightLeg = right;
            hipY = hip;
            hipX = legX;
            // Only the pieces sitting straight on the body (not the arms, head or legs, which have their own joints).
            for (int c = 0; c < body.childCount; c++)
            {
                var mf = body.GetChild(c).GetComponent<MeshFilter>();
                if (mf == null || mf.sharedMesh == null || mf.sharedMesh.vertices == null) continue;
                var f = new Flexed { mesh = mf.sharedMesh, rest = mf.sharedMesh.vertices, restNormals = mf.sharedMesh.normals };
                var idx = new List<int>();
                for (int i = 0; i < f.rest.Length; i++) if (f.rest[i].y < hipY) idx.Add(i);
                if (idx.Count == 0) continue;
                f.below = idx.ToArray();
                f.verts = (Vector3[])f.rest.Clone();
                if (f.restNormals != null && f.restNormals.Length == f.rest.Length) f.normals = (Vector3[])f.restNormals.Clone();
                meshes.Add(f);
            }
        }

        void LateUpdate()
        {
            if (leftLeg == null || rightLeg == null || meshes.Count == 0) return;
            float l = SkirtFlex.Swing(leftLeg.localRotation), r = SkirtFlex.Swing(rightLeg.localRotation);
            // Standing still: nothing to redo.
            if (Mathf.Abs(l - lastLeft) < 0.2f && Mathf.Abs(r - lastRight) < 0.2f) return;
            lastLeft = l;
            lastRight = r;
            var hip = new Vector3(0f, hipY, 0f);
            foreach (var f in meshes)
            {
                foreach (int i in f.below)
                {
                    var turn = SkirtFlex.Turn(f.rest[i], hipY, hipX, l, r);
                    f.verts[i] = hip + turn * (f.rest[i] - hip);
                    if (f.normals != null) f.normals[i] = turn * f.restNormals[i];
                }
                f.mesh.vertices = f.verts;
                if (f.normals != null) f.mesh.normals = f.normals;
                f.mesh.RecalculateBounds();
            }
        }
    }
}
