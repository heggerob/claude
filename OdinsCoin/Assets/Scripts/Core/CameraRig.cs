using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Third-person orbit camera. Mouse turns it, scroll zooms. Stays above the waves.
    /// The cursor is locked while playing; menus and the coin/hall screens free it.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraRig : MonoBehaviour
    {
        public static CameraRig Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        public Transform Target;
        public float Distance = 9f;
        public float MinDistance = 3f, MaxDistance = 40f;
        public float Height = 1.6f;
        public float Sensitivity = 2.2f;
        public bool InvertY;

        public float Yaw { get; private set; }
        float pitch = 18f;
        Vector3 smoothedTarget;
        /// <summary>The coin and hall screens set this to free the cursor and stop mouse-look.</summary>
        public bool CursorFree;
        /// <summary>The title and pause menus set this.</summary>
        public bool MenuOpen;

        /// <summary>Horizontal forward direction of the camera, for movement relative to the view.</summary>
        public Vector3 FlatForward
        {
            get
            {
                var f = transform.forward;
                f.y = 0f;
                return f.sqrMagnitude > 0.001f ? f.normalized : Vector3.forward;
            }
        }

        void Awake() { Instance = this; }

        void LateUpdate()
        {
            bool look = !CursorFree && !MenuOpen;
            Cursor.lockState = look ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !look;

            if (look)
            {
                Vector2 d = GameInput.MouseDelta();
                Yaw += d.x * Sensitivity;
                pitch = Mathf.Clamp(pitch - d.y * Sensitivity * (InvertY ? -1f : 1f), -10f, 75f);
            }
            if (!MenuOpen) Distance = Mathf.Clamp(Distance - GameInput.Scroll() * 1.5f, MinDistance, MaxDistance);

            if (Target == null) return;
            Vector3 goal = Target.position + Vector3.up * Height;
            smoothedTarget = Vector3.Lerp(smoothedTarget, goal, 1f - Mathf.Exp(-12f * Time.deltaTime));

            Quaternion rot = Quaternion.Euler(pitch, Yaw, 0f);
            Vector3 pos = smoothedTarget - rot * Vector3.forward * Distance;
            // Never dip under the sea surface.
            float water = Waves.Height(pos.x, pos.z) + 0.6f;
            if (pos.y < water) pos.y = water;
            transform.position = pos;
            transform.rotation = Quaternion.LookRotation(smoothedTarget - pos);
        }

        public void SnapTo(Transform target, float yaw)
        {
            Target = target;
            Yaw = yaw;
            smoothedTarget = target.position + Vector3.up * Height;
        }
    }
}
