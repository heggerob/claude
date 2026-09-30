using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The sea chart (M): the whole North drawn on parchment from the real map. It shows the sea in blues
    /// by depth, the land in ochres by height with the mountains hatched, and ink along every coast. On it are
    /// the real places (plundered ones struck through), home, and your ship with her heading. Scroll or +/- to zoom
    /// in on your ship.
    /// </summary>
    public class SeaChart : MonoBehaviour
    {
        public static SeaChart Instance { get; private set; }
        public static bool IsOpen { get { return Instance != null && Instance.open; } }
        public static readonly float[] Zooms = { 1f, 2f, 4f, 8f, 16f };

        bool open;
        int zoom;
        Texture2D chart;
        GUIStyle label, title;

        void Awake() { Instance = this; }

        // ---- The drawing, as plain maths ----

        /// <summary>The height the map gives everywhere outside its region (tools/world/build_map.py).</summary>
        public const float NoData = -2000f;

        public static readonly Color Parchment = new Color(0.9f, 0.84f, 0.68f), Ink = new Color(0.22f, 0.16f, 0.1f);

        /// <summary>The chart's colour for ground at a height (m): sea blues by depth, land ochres by height.</summary>
        public static Color Tint(float height)
        {
            if (height < 0f)
            {
                float deep = Mathf.Clamp01(-height / 300f);
                return Color.Lerp(new Color(0.72f, 0.8f, 0.76f), new Color(0.46f, 0.58f, 0.64f), Mathf.Sqrt(deep));
            }
            float up = Mathf.Clamp01(height / 1600f);
            return Color.Lerp(new Color(0.88f, 0.8f, 0.6f), new Color(0.62f, 0.5f, 0.34f), up);
        }

        /// <summary>
        /// Paint the chart (w × h pixels, row 0 at the south) from the map: tints, hatching on the mountains,
        /// and an ink line wherever land meets sea.
        /// </summary>
        public static Color[] Paint(WorldMap map, int w, int h) { return Paint(map, w, h, null); }

        /// <summary>
        /// ...leaving what <paramref name="seen"/> says hasn't been charted (global x, z) as blank parchment, the
        /// sea just a wash of blue, so exploring fills the chart in.
        /// </summary>
        public static Color[] Paint(WorldMap map, int w, int h, System.Func<double, double, bool> seen)
        {
            var px = new Color[w * h];
            var world = map.Bounds;
            float sx = (map.Width - 1) / (float)Mathf.Max(1, w - 1), sz = (map.Height - 1) / (float)Mathf.Max(1, h - 1);
            System.Func<int, int, float> at = (x, y) => map.At(Mathf.Clamp(Mathf.RoundToInt(x * sx), 0, map.Width - 1), Mathf.Clamp(Mathf.RoundToInt(y * sz), 0, map.Height - 1));
            // Beyond the map's region the data is a flat -2000 m: leave that parchment, unexplored, with no coast.
            System.Func<int, int, bool> blank = (x, y) => at(x, y) == NoData && at(x + 1, y) == NoData && at(x - 1, y) == NoData && at(x, y + 1) == NoData && at(x, y - 1) == NoData;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float hgt = at(x, y);
                    if (blank(x, y)) { px[y * w + x] = Parchment; continue; }
                    if (seen != null && !seen(world.xMin + x / (float)Mathf.Max(1, w - 1) * world.width, world.yMin + y / (float)Mathf.Max(1, h - 1) * world.height))
                    {
                        // Uncharted: parchment, with only a hint of whether it's land or sea.
                        px[y * w + x] = Color.Lerp(Parchment, Tint(at(x, y)), 0.12f);
                        continue;
                    }
                    var c = Tint(hgt);
                    // Pen hatching on high ground, denser the higher it is.
                    if (hgt > 500f && ((x + y) % Mathf.Max(2, 7 - Mathf.FloorToInt(hgt / 400f)) == 0)) c = Color.Lerp(c, Ink, 0.35f);
                    // The coastline in ink.
                    bool land = hgt >= 0f;
                    if (((at(x + 1, y) >= 0f) != land && !blank(x + 1, y)) || ((at(x, y + 1) >= 0f) != land && !blank(x, y + 1))) c = Color.Lerp(c, Ink, 0.8f);
                    px[y * w + x] = c;
                }
            return px;
        }

        /// <summary>
        /// Where a global position falls on the chart drawn in <paramref name="screen"/>, when it shows the part
        /// of the world in <paramref name="view"/> (screen y grows downwards, the world's z northwards).
        /// </summary>
        public static Vector2 ToScreen(Rect view, Rect screen, double x, double z)
        {
            float u = (float)((x - view.xMin) / view.width), v = (float)((z - view.yMin) / view.height);
            return new Vector2(screen.xMin + u * screen.width, screen.yMax - v * screen.height);
        }

        /// <summary>The part of the world shown at a zoom: all of it at 1x, else centred on the ship and kept on the map.</summary>
        public static Rect View(Rect world, float zoomFactor, double shipX, double shipZ)
        {
            if (zoomFactor <= 1f) return world;
            float w = world.width / zoomFactor, h = world.height / zoomFactor;
            float x = Mathf.Clamp((float)shipX - w / 2f, world.xMin, world.xMax - w);
            float z = Mathf.Clamp((float)shipZ - h / 2f, world.yMin, world.yMax - h);
            return new Rect(x, z, w, h);
        }

        // ---- In the game ----

        int paintedVersion = -1;

        void Update()
        {
            // Chart the coast round the ship as she sails.
            var boot = GameBootstrap.Instance;
            if (boot != null && boot.Ship != null && RealWorld.Active)
            {
                var p = boot.Ship.transform.position;
                ChartReveal.Sail((float)WorldOrigin.GlobalX(p), (float)WorldOrigin.GlobalZ(p));
            }
            if (GameMenu.Blocking) { open = false; return; }
            if (GameInput.Pressed(Key.Chart)) open = !open && WorldMap.Current != null && RealWorld.Active;
            if (!open) return;
            float scroll = GameInput.Scroll();
            if (scroll > 0.1f) zoom = Mathf.Min(Zooms.Length - 1, zoom + 1);
            if (scroll < -0.1f) zoom = Mathf.Max(0, zoom - 1);
        }

        void Build(WorldMap map)
        {
            if (chart != null) Destroy(chart);
            int w = Mathf.Min(1024, map.Width), h = Mathf.Max(2, Mathf.RoundToInt(w * (map.Height / (float)map.Width)));
            chart = new Texture2D(w, h, TextureFormat.RGBA32, false);
            chart.wrapMode = TextureWrapMode.Clamp;
            chart.filterMode = FilterMode.Bilinear;
            var px = Paint(map, w, h, ChartReveal.Seen);
            paintedVersion = ChartReveal.Version;
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++) chart.SetPixel(x, y, px[y * w + x]);
            chart.Apply();
        }

        void OnGUI()
        {
            if (!open) return;
            var map = WorldMap.Current;
            var boot = GameBootstrap.Instance;
            if (map == null || boot == null || boot.Ship == null) return;
            if (ChartReveal.Circles.Count > 0 && chart != null && paintedVersion != ChartReveal.Version) Build(map);
            if (chart == null) { ChartReveal.UseGrid(map.Bounds); Build(map); }
            if (label == null)
            {
                label = new GUIStyle(GUI.skin.label) { fontSize = 12, richText = true };
                label.normal.textColor = Ink;
                title = new GUIStyle(GUI.skin.label) { fontSize = 20, richText = true, alignment = TextAnchor.UpperCenter };
                title.normal.textColor = Ink;
            }

            // The sheet: as big as fits, the map's own shape.
            var world = map.Bounds;
            float aspect = world.width / world.height;
            float sh = Screen.height * 0.86f, sw = sh * aspect;
            if (sw > Screen.width * 0.92f) { sw = Screen.width * 0.92f; sh = sw / aspect; }
            var screen = new Rect((Screen.width - sw) / 2f, (Screen.height - sh) / 2f + 10f, sw, sh);
            double shipX = WorldOrigin.GlobalX(boot.Ship.transform.position), shipZ = WorldOrigin.GlobalZ(boot.Ship.transform.position);
            float z = Zooms[zoom];
            var view = View(world, z, shipX, shipZ);
            var uv = new Rect((view.xMin - world.xMin) / world.width, (view.yMin - world.yMin) / world.height, view.width / world.width, view.height / world.height);
            GUI.Box(new Rect(screen.xMin - 8f, screen.yMin - 34f, screen.width + 16f, screen.height + 42f), GUIContent.none);
            GUI.DrawTextureWithTexCoords(screen, chart, uv);
            GUI.Label(new Rect(screen.xMin, screen.yMin - 30f, screen.width, 26f), "<b>Chart of the North</b>  <size=12>(M to close · scroll to zoom " + z + "x)</size>", title);

            // The places.
            foreach (var place in Places.All)
            {
                var at = Places.Position(map, place);
                var p = ToScreen(view, screen, at.x, at.z);
                if (!screen.Contains(p)) continue;
                bool raided = PlaceLife.Raided.Contains(place.name);
                string mark = PlaceLife.HasMarket(place) ? "◆" : place.kind == PlaceKind.Monastery ? "✚" : "●";
                string name = raided ? "<color=#7a6a55>" + place.name + " (plundered)</color>" : "<b>" + place.name + "</b>";
                // Bjorn's commission stands out in red.
                if (place.name == Upgrades.Current.Commission) { mark = "⚑"; name = "<color=#9a1c10><b>" + place.name + " (Bjorn's commission)</b></color>"; }
                GUI.Label(new Rect(p.x - 5f, p.y - 9f, 220f, 18f), mark + " " + name, label);
            }
            // The wreck with your lost treasure.
            if (Wreck.At.HasValue)
            {
                var wp = ToScreen(view, screen, Wreck.At.Value.x, Wreck.At.Value.z);
                if (screen.Contains(wp)) GUI.Label(new Rect(wp.x - 6f, wp.y - 9f, 200f, 18f), "<color=#9a1c10>✕ <b>Wreck</b> (" + Wreck.Left + " chests)</color>", label);
            }
            // Home.
            var home = HomeHarbour.HomeCentre + HomeHarbour.Drift;
            var hp = ToScreen(view, screen, WorldOrigin.GlobalX(home), WorldOrigin.GlobalZ(home));
            if (screen.Contains(hp)) GUI.Label(new Rect(hp.x - 6f, hp.y - 9f, 160f, 18f), "⌂ <b>Home</b>", label);
            // Your ship and her heading.
            var s = ToScreen(view, screen, shipX, shipZ);
            var head = boot.Ship.Heading * Mathf.Deg2Rad;
            var tip = s + new Vector2(Mathf.Sin(head), -Mathf.Cos(head)) * 14f;
            GUI.Label(new Rect(s.x - 6f, s.y - 10f, 30f, 20f), "<color=#9a1c10><b>▲</b></color>", label);
            GUI.Label(new Rect(tip.x - 3f, tip.y - 8f, 20f, 16f), "<color=#9a1c10>•</color>", label);
            GUI.Label(new Rect(s.x + 10f, s.y - 10f, 200f, 20f), "<color=#9a1c10><b>" + (boot.Ship.Design != null ? boot.Ship.Design.title : "Longship") + "</b> " + boot.Ship.Heading.ToString("000") + "°</color>", label);

            // A scale bar.
            float km = ScaleKm(view.width / 1000f);
            float barPx = km * 1000f / view.width * screen.width;
            var bar = new Rect(screen.xMin + 16f, screen.yMax - 26f, barPx, 4f);
            var old = GUI.color;
            GUI.color = Ink;
            GUI.DrawTexture(bar, Texture2D.whiteTexture);
            GUI.color = old;
            GUI.Label(new Rect(bar.xMin, bar.yMin - 20f, 200f, 18f), km.ToString("0") + " km", label);
        }

        /// <summary>A round length for the scale bar, about a fifth of the width shown (km).</summary>
        public static float ScaleKm(float viewKm)
        {
            float target = viewKm / 5f;
            foreach (float k in new[] { 1f, 2f, 5f, 10f, 20f, 50f, 100f, 200f, 500f })
                if (k >= target) return k;
            return 1000f;
        }
    }
}
