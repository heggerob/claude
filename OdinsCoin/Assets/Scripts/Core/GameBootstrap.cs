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
        public const float SunIntensity = 1.15f;
        /// <summary>Where the fog starts and ends (m); the real North sees much further.</summary>
        public static float FogStart = 40f, FogEnd = 220f;
        /// <summary>The ship the voyage starts with: one of the new classes (null for the classic longship).</summary>
        public static ShipDesign PlayerDesign = ShipDesign.Wavewolf;

        void Update()
        {
            Wind.Tick(Time.deltaTime);
            Fortune.Current.Tick(Time.deltaTime, Abilities.Has("ward") ? Abilities.WardCurseRate : 1f);
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
            // The real North (streamed from the real map) when it's there; else the storybook isles.
            bool real = RealWorld.Enabled && WorldMap.Current != null;
            if (!real) WorldGen.Build(transform);
            HomeHarbour.Build(transform);

            // The voyage starts moored at the home jetty, sail furled.
            Ship = PlayerDesign != null
                ? Longship.Create(transform, HomeHarbour.Berth(PlayerDesign), HomeHarbour.ShipStartHeading, PlayerDesign, PlayerLook())
                : Longship.Create(transform, HomeHarbour.ShipStart, HomeHarbour.ShipStartHeading);
            Ship.Furl();
            Ship.MakeFast(); // lines out to the jetty's bollards: cast off with G at the helm
            Ship.PlayerShip = true;
            // The helm only listens to the keyboard while the Viking holds the steering oar.
            Ship.gameObject.AddComponent<ShipKeyboardHelm>().enabled = false;
            Player = Viking.Create(transform, Ship);
            Player.gameObject.AddComponent<VikingCombat>();
            gameObject.AddComponent<CombatHud>().Player = Player.GetComponent<VikingCombat>();
            CoinAltar.Create(Ship);
            gameObject.AddComponent<Ravens>();
            gameObject.AddComponent<MeadHallUI>().Ship = Ship;
            gameObject.AddComponent<Storm>();
            gameObject.AddComponent<SeaDangers>();
            gameObject.AddComponent<TimeWarp>();
            gameObject.AddComponent<SkyClock>();
            gameObject.AddComponent<MerchantTraffic>();
            gameObject.AddComponent<SeaChart>();
            gameObject.AddComponent<Sfx>();
            // Pen lines around everything solid in the world (the heroes draw their own).
            gameObject.AddComponent<InkOutliner>();
            var hud = gameObject.AddComponent<ShipHud>();
            hud.Ship = Ship;
            hud.Player = Player;
            Focus = Player.transform;
            if (real) RealWorld.Setup(transform, Focus, cam, HomeHarbour.HomeCentre);

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
            // The title screen goes up last, over the live harbour.
            gameObject.AddComponent<GameMenu>();
        }

        /// <summary>
        /// Put a different hull at the home jetty for the player to sail (bought from the shipwright, or the one the
        /// save says). The cargo is carried across, the coin altar goes aboard, and the crew's ship is the new one.
        /// </summary>
        public void SwapShip(ShipDesign design)
        {
            var old = Ship;
            if (design == null || (old != null && old.Design == design)) return;
            var ship = Longship.Create(transform, HomeHarbour.BerthNow(design), HomeHarbour.ShipStartHeading, design, PlayerLook());
            ship.Furl();
            ship.PlayerShip = true;
            ship.gameObject.AddComponent<ShipKeyboardHelm>().enabled = false;
            bool aboard = Player != null && Player.OnShip;
            if (old != null)
            {
                // The chests stowed on deck go across to the new ship.
                foreach (var chest in TreasureChest.All.ToArray())
                {
                    if (chest == null || !chest.Stowed(old)) continue;
                    var local = old.transform.InverseTransformPoint(chest.transform.position);
                    float hw, k, g;
                    ship.Station(Mathf.Clamp(local.z / ship.HalfLength, -0.6f, 0.6f), out hw, out k, out g);
                    chest.transform.SetParent(ship.transform, false);
                    chest.transform.localPosition = new Vector3(Mathf.Clamp(local.x, -hw + 0.8f, hw - 0.8f), ship.DeckY, Mathf.Clamp(local.z, -ship.HalfLength * 0.6f, ship.HalfLength * 0.6f));
                }
                Destroy(old.gameObject);
            }
            Ship = ship;
            CoinAltar.Create(ship);
            if (Player != null)
            {
                Player.Ship = ship;
                if (aboard) Player.ReturnToShip();
            }
            var hall = GetComponent<MeadHallUI>();
            if (hall != null) hall.Ship = ship;
            var hud = GetComponent<ShipHud>();
            if (hud != null) hud.Ship = ship;
            ship.MakeFast();
        }

        /// <summary>
        /// Carry on a saved voyage: put the ship back where she was left out in the real North (if that's still open
        /// water) with the crew aboard. False if she stays at home.
        /// </summary>
        public bool ResumeAt(double x, double z, float heading)
        {
            var map = WorldMap.Current;
            if (!RealWorld.Active || map == null || Ship == null) return false;
            if (!RealWorld.OpenWater(map, new Vector3((float)x, 0f, (float)z), Ship.HalfLength)) return false;
            Ship.Relocate(WorldOrigin.ToScene(x, z, 0.2f), heading);
            // Shift the world under her now, so she's near the origin and the land streams in round her.
            Vector3 shift;
            if (WorldOrigin.NeedsShift(Ship.transform.position, out shift)) WorldOrigin.Shift(shift);
            if (Player != null) Player.ReturnToShip();
            var rig = Camera.main != null ? Camera.main.GetComponent<CameraRig>() : null;
            if (rig != null && Focus != null) rig.SnapTo(Focus, heading);
            return true;
        }

        /// <summary>The player's ship in the home colours: the red-and-black sail of the jarl's house.</summary>
        static ShipLook PlayerLook()
        {
            var look = new ShipLook();
            look.sail = Materials.Sail;
            look.stripe = Materials.SailStripe;
            return look;
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
            InkStyle.SetSun(sun);
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
