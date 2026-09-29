using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Builds the whole prototype from code, so it runs in any scene: open a scene, press Play.
    /// Starts automatically; put a GameBootstrap in a scene with autoBoot off to keep that scene clean.
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
            new GameObject("Odin's Coin").AddComponent<GameBootstrap>();
        }

        [Tooltip("Turn off to keep this scene free of the prototype.")]
        public bool autoBoot = true;

        /// <summary>Nordic overcast: the sky and the fog share a colour so the horizon melts away.</summary>
        public static readonly Color SkyColor = new Color(0.62f, 0.7f, 0.76f);

        public Ocean Ocean { get; private set; }
        public Transform Focus { get; private set; }
        public Longship Ship { get; private set; }
        public Viking Player { get; private set; }
        public Light Sun { get; private set; }
        public static readonly Color AmbientColor = new Color(0.42f, 0.48f, 0.53f);
        public const float SunIntensity = 1.15f, FogStart = 40f, FogEnd = 220f;

        void Update()
        {
            Wind.Tick(Time.deltaTime);
            Fortune.Current.Tick(Time.deltaTime);
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            if (!autoBoot) return;

            Time.timeScale = 1f;
            var cam = SetupCamera();
            Sun = SetupLighting();

            Ocean = Ocean.Create(transform);
            WorldGen.Build(transform);
            HomeHarbour.Build(transform);

            // The voyage starts moored at the home jetty, sail furled.
            Ship = Longship.Create(transform, HomeHarbour.ShipStart, HomeHarbour.ShipStartHeading);
            Ship.Furl();
            Ship.PlayerShip = true;
            // The helm only listens to the keyboard while the Viking holds the steering oar.
            Ship.gameObject.AddComponent<ShipKeyboardHelm>().enabled = false;
            Player = Viking.Create(transform, Ship);
            Player.gameObject.AddComponent<VikingCombat>();
            gameObject.AddComponent<CombatHud>().Player = Player.GetComponent<VikingCombat>();
            CoinAltar.Create(Ship);
            gameObject.AddComponent<CoinUI>();
            gameObject.AddComponent<Ravens>();
            gameObject.AddComponent<MeadHallUI>().Ship = Ship;
            gameObject.AddComponent<Storm>();
            gameObject.AddComponent<SeaDangers>();
            var hud = gameObject.AddComponent<ShipHud>();
            hud.Ship = Ship;
            hud.Player = Player;
            Focus = Player.transform;

            // A few barrels drifting around the harbour mouth, to show the swell.
            for (int i = 0; i < 8; i++)
            {
                var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                barrel.name = "Barrel";
                barrel.transform.SetParent(transform, false);
                barrel.transform.position = HomeHarbour.ShipStart + new Vector3(0f, 0f, 40f) + Quaternion.Euler(0f, i * 45f, 0f) * Vector3.forward * Random.Range(8f, 22f);
                barrel.transform.localScale = new Vector3(0.7f, 0.5f, 0.7f);
                barrel.GetComponent<Renderer>().sharedMaterial = Materials.Get(Materials.Wood);
                barrel.AddComponent<Floater>();
            }

            var rig = cam.GetComponent<CameraRig>();
            if (rig == null) rig = cam.gameObject.AddComponent<CameraRig>();
            rig.Distance = 7f;
            rig.Height = 1.6f;
            rig.SnapTo(Focus, HomeHarbour.ShipStartHeading);
            Ocean.Follow(cam.transform);
        }

        static Camera SetupCamera()
        {
            var cam = Camera.main;
            if (cam == null)
            {
                var go = new GameObject("Main Camera");
                go.tag = "MainCamera";
                cam = go.AddComponent<Camera>();
                go.AddComponent<AudioListener>();
            }
            cam.orthographic = false;
            cam.fieldOfView = 60f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 600f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SkyColor;
            return cam;
        }

        static Light SetupLighting()
        {
            Light sun = null;
            foreach (var l in FindLights()) if (l.type == LightType.Directional) { sun = l; break; }
            if (sun == null)
            {
                sun = new GameObject("Sun").AddComponent<Light>();
                sun.type = LightType.Directional;
            }
            // Low northern sun.
            sun.transform.rotation = Quaternion.Euler(28f, -35f, 0f);
            sun.color = new Color(1f, 0.93f, 0.82f);
            sun.intensity = SunIntensity;
            sun.shadows = LightShadows.Soft;

            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = SkyColor;
            RenderSettings.fogStartDistance = FogStart;
            RenderSettings.fogEndDistance = FogEnd;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = AmbientColor;
            return sun;
        }

        static Light[] FindLights()
        {
#if UNITY_2023_1_OR_NEWER
            return Object.FindObjectsByType<Light>(FindObjectsSortMode.None);
#else
            return Object.FindObjectsOfType<Light>();
#endif
        }
    }
}
