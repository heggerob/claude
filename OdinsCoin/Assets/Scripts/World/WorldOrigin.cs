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
        static void ResetStatics() { OffsetX = OffsetZ = 0.0; Roots.Clear(); }

        /// <summary>Scene roots that move when the origin shifts (the world, ships, the player...).</summary>
        public static readonly List<Transform> Roots = new List<Transform>();

        /// <summary>How far from the origin the player may get before the world is shifted back under them.</summary>
        public const float ShiftDistance = 2000f;

        public static double GlobalX(Vector3 scene) { return scene.x + OffsetX; }
        public static double GlobalZ(Vector3 scene) { return scene.z + OffsetZ; }

        /// <summary>A global position (x, z) as a scene position, at height <paramref name="y"/>.</summary>
        public static Vector3 ToScene(double x, double z, float y) { return new Vector3((float)(x - OffsetX), y, (float)(z - OffsetZ)); }

        /// <summary>Should the origin move for someone at this scene position? If so, by how much (x, z).</summary>
        public static bool NeedsShift(Vector3 scene, out Vector3 shift)
        {
            shift = new Vector3(scene.x, 0f, scene.z);
            return shift.sqrMagnitude > ShiftDistance * ShiftDistance;
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
                var body = t.GetComponent<Rigidbody>();
                t.position -= shift;
                if (body != null) body.position = t.position;
            }
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
