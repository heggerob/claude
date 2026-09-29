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
        Soldier soldier;
        Vector3 smoothed = new Vector3(0f, -1f, -10f);
        float trauma;
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
            soldier = t != null ? t.GetComponent<Soldier>() : null;
        }

        /// <summary>Kick the camera. Amount ~0.05 for a shot, ~0.4 for getting hit. Respects the screen shake setting.</summary>
        public static void Shake(float amount)
        {
            if (Instance == null || !GameSettings.ScreenShake) return;
            Instance.trauma = Mathf.Min(1f, Instance.trauma + amount);
        }

        public void ShowOverview()
        {
            target = null;
            soldier = null;
            targetSize = OverviewSize;
        }

        void LateUpdate()
        {
            float dt = Time.unscaledDeltaTime;
            Vector3 goal = target != null ? target.position : new Vector3(0f, -1f, 0f);
            goal.z = -10f;

            float size = targetSize;
            if (target != null && cam != null)
            {
                // Scroll to zoom in and out (remembered between matches).
                float scroll = GameInput.Scroll();
                if (Mathf.Abs(scroll) > 0.01f)
                {
                    GameSettings.ViewDistance = Mathf.Clamp(GameSettings.ViewDistance - scroll * 0.8f, GameSettings.MinViewDistance, GameSettings.MaxViewDistance);
                    GameSettings.Save();
                }
                size += GameSettings.ViewDistance - GameSettings.DefaultViewDistance;

                // Look towards the mouse: a little normally, further when aiming, much further through a scope.
                bool aiming = soldier != null && soldier.Aiming;
                bool scoped = soldier != null && soldier.Scoped;
                float reach = scoped ? 30f : aiming ? 14f : 8f;
                float share = scoped ? 0.7f : aiming ? 0.45f : 0.25f;
                if (scoped) size += 5f;
                else if (aiming) size += 1.5f;
                Vector2 mouse = cam.ScreenToWorldPoint(GameInput.MousePosition());
                Vector2 lookAhead = Vector2.ClampMagnitude(mouse - (Vector2)target.position, reach) * share;
                goal += (Vector3)lookAhead;
            }

            smoothed = Vector3.Lerp(smoothed, goal, 1f - Mathf.Exp(-8f * dt));
            // Shake grows with trauma squared, so small kicks stay subtle.
            float shake = trauma * trauma * 0.6f;
            Vector3 offset = new Vector3(Random.Range(-1f, 1f), Random.Range(-1f, 1f), 0f) * shake;
            trauma = Mathf.Max(0f, trauma - dt * 1.8f);
            transform.position = smoothed + offset;
            if (cam != null) cam.orthographicSize = Mathf.Lerp(cam.orthographicSize, size, 1f - Mathf.Exp(-4f * dt));
        }
    }
}
