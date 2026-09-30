using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The day in the real North: a clock that runs with the ship's own time (so faster time and long passages make
    /// the days go by), and a sun placed where it really is for the ship's latitude and the day of the year. Midsummer
    /// in Lofoten, the sun circles all night; further south it dips below the sea and the sky goes blue and dark. The
    /// sky, fog and light follow the sun (the storm darkens them further on top).
    /// </summary>
    public class SkyClock : MonoBehaviour
    {
        /// <summary>The raiding season starts at the end of May, at eight in the morning.</summary>
        public const int StartDay = 150;
        public const float StartHour = 8f;

        public static SkyClock Instance { get; private set; }
        /// <summary>Days since the voyage began, and the local solar time (0..24 h).</summary>
        public static int Day { get; private set; }
        public static float Hours { get; private set; }
        /// <summary>The sky, fog, ambient light and sunlight for now, before any storm.</summary>
        public static Color Sky = GameBootstrap.SkyColor, Ambient = GameBootstrap.AmbientColor;
        public static float SunLight = GameBootstrap.SunIntensity;
        /// <summary>How high the sun stands (degrees; below 0 it has set).</summary>
        public static float Elevation { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; Day = 0; Hours = StartHour; Sky = GameBootstrap.SkyColor; Ambient = GameBootstrap.AmbientColor; SunLight = GameBootstrap.SunIntensity; }

        void Awake() { Instance = this; }

        /// <summary>Set the clock (loading a save).</summary>
        public static void Set(int day, float hours) { Day = Mathf.Max(0, day); Hours = Mathf.Repeat(hours, 24f); }

        // ---- The sun, as plain maths ----

        /// <summary>The sun's declination on a day of the year (degrees).</summary>
        public static float Declination(int dayOfYear) { return 23.44f * Mathf.Sin(2f * Mathf.PI * (284 + dayOfYear) / 365f); }

        /// <summary>How high the sun stands (degrees) at a latitude, day of the year and local solar time.</summary>
        public static float SolarElevation(float latitude, int dayOfYear, float solarHours)
        {
            float phi = latitude * Mathf.Deg2Rad, dec = Declination(dayOfYear) * Mathf.Deg2Rad, h = (solarHours - 12f) * 15f * Mathf.Deg2Rad;
            return Mathf.Asin(Mathf.Sin(phi) * Mathf.Sin(dec) + Mathf.Cos(phi) * Mathf.Cos(dec) * Mathf.Cos(h)) * Mathf.Rad2Deg;
        }

        /// <summary>The sun's bearing (degrees from north, clockwise): east in the morning, south at noon, west in the evening.</summary>
        public static float SolarAzimuth(float latitude, int dayOfYear, float solarHours)
        {
            float phi = latitude * Mathf.Deg2Rad, dec = Declination(dayOfYear) * Mathf.Deg2Rad, h = (solarHours - 12f) * 15f * Mathf.Deg2Rad;
            float az = Mathf.Atan2(Mathf.Sin(h), Mathf.Cos(h) * Mathf.Sin(phi) - Mathf.Tan(dec) * Mathf.Cos(phi)) * Mathf.Rad2Deg;
            return Mathf.Repeat(az + 180f, 360f);
        }

        /// <summary>
        /// The sky for a sun this high: day blue-grey above 10°, a warm glow low in the sky, deep blue twilight while
        /// it's under the horizon, and night below -12° (the night is never black: there's the moon and the stars).
        /// </summary>
        public static void Light(float elevation, out Color sky, out Color ambient, out float sun)
        {
            var night = new Color(0.07f, 0.09f, 0.17f);
            var dusk = new Color(0.36f, 0.3f, 0.42f);
            var glow = new Color(0.86f, 0.62f, 0.46f);
            if (elevation >= 10f) sky = GameBootstrap.SkyColor;
            else if (elevation >= 2f) sky = Color.Lerp(glow, GameBootstrap.SkyColor, (elevation - 2f) / 8f);
            else if (elevation >= -4f) sky = Color.Lerp(dusk, glow, (elevation + 4f) / 6f);
            else sky = Color.Lerp(night, dusk, Mathf.Clamp01((elevation + 12f) / 8f));
            float day = Mathf.Clamp01((elevation + 6f) / 16f);
            ambient = Color.Lerp(new Color(0.12f, 0.14f, 0.22f), GameBootstrap.AmbientColor, day);
            sun = GameBootstrap.SunIntensity * Mathf.Clamp01(elevation / 12f);
        }

        // ---- In the game ----

        void Update()
        {
            // The ship's own time: faster time speeds it up, and a long passage far more.
            float dt = TimeWarp.OnPassage ? Time.unscaledDeltaTime * Passage.Factor : Time.deltaTime;
            Hours += dt / 3600f;
            while (Hours >= 24f) { Hours -= 24f; Day++; }

            var boot = GameBootstrap.Instance;
            var map = WorldMap.Current;
            float latitude = 59f;
            if (boot != null && boot.Ship != null && map != null && RealWorld.Active)
            {
                var p = boot.Ship.transform.position;
                latitude = map.ToLatLon(new Vector3((float)WorldOrigin.GlobalX(p), 0f, (float)WorldOrigin.GlobalZ(p))).x;
            }
            int doy = (StartDay + Day) % 365;
            Elevation = SolarElevation(latitude, doy, Hours);
            float azimuth = SolarAzimuth(latitude, doy, Hours);
            Light(Elevation, out Sky, out Ambient, out SunLight);

            if (boot != null && boot.Sun != null)
            {
                // The light shines from the sun (a set sun still lights from just under the horizon, faintly).
                boot.Sun.transform.rotation = Quaternion.Euler(Mathf.Max(4f, Elevation), azimuth + 180f, 0f);
                InkStyle.SetSun(boot.Sun);
            }
            // With no storm about, the sky is just the day's (the storm blends from these when it's on).
            if (Storm.Instance == null)
            {
                RenderSettings.fogColor = Sky;
                RenderSettings.ambientLight = Ambient;
                if (Camera.main != null) Camera.main.backgroundColor = Sky;
                if (boot != null && boot.Sun != null) boot.Sun.intensity = SunLight;
            }
        }
    }
}
