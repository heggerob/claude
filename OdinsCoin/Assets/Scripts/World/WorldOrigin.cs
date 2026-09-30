using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// A floating origin: the world is thousands of kilometres across, far beyond what floats can place
    /// precisely, so the scene is kept near (0, 0) and this offset says where that is in the whole world.
    /// Global position = scene position + offset (x and z only; heights are never shifted).
    /// </summary>
    public static class WorldOrigin
    {
        public static double OffsetX, OffsetZ;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { OffsetX = OffsetZ = 0.0; Roots.Clear(); Containers.Clear(); Shifted = null; }

        /// <summary>Scene roots that move when the origin shifts (the world, ships, the player...).</summary>
        public static readonly List<Transform> Roots = new List<Transform>();

        /// <summary>Scene objects whose every child moves when the origin shifts (the game's world root).</summary>
        public static readonly List<Transform> Containers = new List<Transform>();

        /// <summary>Told after each shift, with how far everything moved back, so anything keeping scene positions can follow.</summary>
        public static event System.Action<Vector3> Shifted;

        /// <summary>Shifts are whole multiples of this (m), so grid-snapped things (the sea mesh) stay on their grid.</summary>
        public const float ShiftStep = 16f;

        /// <summary>How far from the origin the player may get before the world is shifted back under them.</summary>
        public const float ShiftDistance = 2000f;

        public static double GlobalX(Vector3 scene) { return scene.x + OffsetX; }
        public static double GlobalZ(Vector3 scene) { return scene.z + OffsetZ; }

        /// <summary>A global position (x, z) as a scene position, at height <paramref name="y"/>.</summary>
        public static Vector3 ToScene(double x, double z, float y) { return new Vector3((float)(x - OffsetX), y, (float)(z - OffsetZ)); }

        /// <summary>Should the origin move for someone at this scene position? If so, by how much (x, z).</summary>
        public static bool NeedsShift(Vector3 scene, out Vector3 shift)
        {
            shift = new Vector3(Mathf.Round(scene.x / ShiftStep) * ShiftStep, 0f, Mathf.Round(scene.z / ShiftStep) * ShiftStep);
            return scene.x * scene.x + scene.z * scene.z > ShiftDistance * ShiftDistance;
        }

        /// <summary>Move the origin by <paramref name="shift"/>: everything in the scene moves back by it, and the offset takes it up.</summary>
        public static void Shift(Vector3 shift)
        {
            shift.y = 0f;
            OffsetX += shift.x;
            OffsetZ += shift.z;
            for (int i = Roots.Count - 1; i >= 0; i--)
            {
                var t = Roots[i];
                if (t == null) { Roots.RemoveAt(i); continue; }
                Move(t, shift);
            }
            for (int i = Containers.Count - 1; i >= 0; i--)
            {
                var c = Containers[i];
                if (c == null) { Containers.RemoveAt(i); continue; }
                for (int j = 0; j < c.childCount; j++)
                {
                    var t = c.GetChild(j);
                    if (!Roots.Contains(t)) Move(t, shift);
                }
            }
            if (Shifted != null) Shifted(shift);
        }

        static void Move(Transform t, Vector3 shift)
        {
            var body = t.GetComponent<Rigidbody>();
            t.position -= shift;
            if (body != null) body.position = t.position;
        }
    }

    /// <summary>Watches the player and shifts the origin when they stray too far from it.</summary>
    public class FloatingOrigin : MonoBehaviour
    {
        public Transform follow;

        void LateUpdate()
        {
            if (follow == null) return;
            Vector3 shift;
            if (WorldOrigin.NeedsShift(follow.position, out shift)) WorldOrigin.Shift(shift);
        }
    }
}
