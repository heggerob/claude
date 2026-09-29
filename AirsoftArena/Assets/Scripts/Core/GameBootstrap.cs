using UnityEngine;

namespace AirsoftArena
{
    /// <summary>
    /// Builds the whole prototype from code, so it runs in any scene: open a scene, press Play.
    /// (Starts automatically. To stop that in a scene, put a GameBootstrap there yourself with autoBoot off.)
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        public static GameBootstrap Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void AutoBoot()
        {
            if (Instance != null) return;
            new GameObject("Airsoft Arena").AddComponent<GameBootstrap>();
        }

        [Tooltip("Turn off to keep this scene free of the prototype.")]
        public bool autoBoot = true;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (!autoBoot) return;

            Physics2D.gravity = Vector2.zero;
            Time.timeScale = 1f;

            var cam = SetupCamera();
            if (cam.GetComponent<CameraFollow>() == null) cam.gameObject.AddComponent<CameraFollow>();

            MapBuilder.Build(transform, MapLibrary.Get(PlayerProfile.Current.mapId));
            gameObject.AddComponent<BBSystem>();
            gameObject.AddComponent<Effects>();
            gameObject.AddComponent<Sfx>();
            gameObject.AddComponent<MatchManager>();
            gameObject.AddComponent<GameHUD>();
        }

        static Camera SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
            }
            cam.orthographic = true;
            cam.orthographicSize = 15f;
            cam.transform.position = new Vector3(0f, -1f, -10f);
            cam.transform.rotation = Quaternion.identity;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.07f, 0.08f, 0.07f);
            return cam;
        }
    }
}
