using UnityEngine;

namespace AirsoftArena
{
    /// <summary>Smoothly follows the player (or shows the whole map in the lobby).</summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow : MonoBehaviour
    {
        public static CameraFollow Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        const float OverviewSize = 15f;

        Camera cam;
        Transform target;
        float targetSize = OverviewSize;

        void Awake()
        {
            Instance = this;
            cam = GetComponent<Camera>();
        }

        public void Follow(Transform t, float orthoSize)
        {
            target = t;
            targetSize = orthoSize;
        }

        public void ShowOverview()
        {
            target = null;
            targetSize = OverviewSize;
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            Vector3 goal = target != null ? target.position : new Vector3(0f, -1f, 0f);
            goal.z = -10f;

            if (target != null && cam != null)
            {
                // Look a little towards the mouse so you can see further where you aim.
                Vector2 mouse = cam.ScreenToWorldPoint(GameInput.MousePosition());
                Vector2 lookAhead = Vector2.ClampMagnitude(mouse - (Vector2)target.position, 8f) * 0.25f;
                goal += (Vector3)lookAhead;
            }

            transform.position = Vector3.Lerp(transform.position, goal, 1f - Mathf.Exp(-8f * dt));
            if (cam != null) cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, targetSize, 1f - Mathf.Exp(-4f * dt));
        }
    }
}
