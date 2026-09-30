using UnityEngine;

namespace OdinsCoin
{
    /// <summary>Temporary sailing HUD: speed, heading, sail and a wind arrow relative to the ship.</summary>
    public class ShipHud : MonoBehaviour
    {
        public Longship Ship;
        public Viking Player;
        GUIStyle style;

        void OnGUI()
        {
            if (Ship == null || GameMenu.OnTitle) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true };
                style.normal.textColor = Color.white;
            }
            float relWind = Mathf.DeltaAngle(Ship.Heading, Wind.Angle);
            string arrow = Arrow(relWind);
            string text = string.Format(
                "<b>{0:0.0} knots</b>   heading {1:000}°   <size=13>day {8}, {9}</size>\nSail {2:0}%{3}\nWind {4:0} kn  {5} ({6})\n\n<size=12>{7}</size>",
                Ship.SpeedKnots, Ship.Heading, Ship.SailAmount * 100f, (Ship.Rowing ? "   ROWING" : Ship.Moored ? "   MOORED" : Ship.Anchored ? "   AT ANCHOR" : "") + (TimeWarp.OnPassage ? "   <color=#ffd060>PASSAGE, course " + Ship.PassageCourse.ToString("000") + "°</color>" : TimeWarp.Factor > 1 ? "   <color=#ffd060>TIME x" + TimeWarp.Factor + "</color>" : ""), Wind.Knots * Ship.Lee, arrow, WindWord(relWind) + (Ship.Lee < 0.75f ? ", in the lee of the land" : ""),
                Player != null && Player.AtHelm
                    ? "At the helm: A/D steer · R raise sail · Q lower / reef sail · W row (sail down) · G anchor / make fast · T faster time · M chart · E let go"
                    : Player != null && Player.Carrying != null
                        ? "Carrying a chest: put it down on deck (E) to stow it, sell it to Gunnar in the Home Fjord"
                        : "WASD move · mouse look · Shift run · Ctrl walk · Space jump · LMB attack (again for a combo) · RMB shield · E use / pick up / bail · V first / third person · Esc menu",
                SkyClock.Day + 1, ClockText(SkyClock.Hours));
            GUI.Box(new Rect(10, 10, 380, 120), GUIContent.none);
            GUI.Label(new Rect(20, 16, 370, 110), text, style);

            DrawFortune();
            DrawDangers();

            // Nearest land.
            // The lead line: the depth under her keel, called out when the bottom comes up.
            if (Ship != null && Ship.Design != null && RealWorld.Active)
            {
                float depth = Longship.DepthAt(Ship.transform.position) - Ship.Design.draught;
                if (depth < 25f)
                {
                    string colour = depth < 1.5f ? "#ff7a5a" : depth < 5f ? "#ffd060" : "#cfe3ff";
                    GUI.Label(new Rect(20f, Screen.height - 106f, 560f, 22f), string.Format("<color={0}>Lead line: {1:0.0} m under the keel{2}</color>", colour, Mathf.Max(0f, depth), depth < 1.5f ? " — shoal water!" : ""), style);
                }
            }
            // A current setting the ship.
            if (Ship != null && Ship.Stream.magnitude > 0.25f)
            {
                float set = Mathf.Repeat(Mathf.Atan2(Ship.Stream.x, Ship.Stream.z) * Mathf.Rad2Deg, 360f);
                GUI.Label(new Rect(20f, Screen.height - 82f, 560f, 22f), string.Format("<color=#9fd0ff>Current:</color> {0:0.0} kn setting {1:000}°", Ship.Stream.magnitude * 1.9438f, set), style);
            }
            // Bjorn's commission: where it is from here.
            var commission = Upgrades.Current.Commission;
            if (!string.IsNullOrEmpty(commission) && RealWorld.Active && WorldMap.Current != null && Places.Find(commission) != null && Ship != null)
            {
                var target = Places.Position(WorldMap.Current, Places.Find(commission));
                var here = Ship.transform.position;
                float dx = target.x - (float)WorldOrigin.GlobalX(here), dz = target.z - (float)WorldOrigin.GlobalZ(here);
                float bearing = Mathf.Repeat(Mathf.Atan2(dx, dz) * Mathf.Rad2Deg, 360f);
                GUI.Label(new Rect(20f, Screen.height - 34f, 560f, 22f), string.Format("<color=#ffd060>Commission:</color> plunder <b>{0}</b>, {1}, bearing {2:000}°", commission, Distance(Mathf.Sqrt(dx * dx + dz * dz)), bearing), style);
            }
            // The Navigator's Read the Stars: the way to the nearest place with plunder left.
            if (Player != null && RealWorld.Active && WorldMap.Current != null && Abilities.Has("stars"))
            {
                var here = Player.transform.position;
                double gx = WorldOrigin.GlobalX(here), gz = WorldOrigin.GlobalZ(here);
                Place treasure = null;
                float best = float.MaxValue;
                foreach (var p in Places.All)
                {
                    if (PlaceLife.PlunderOf(p.kind).chests == 0 || PlaceLife.Raided.Contains(p.name)) continue;
                    var at = Places.Position(WorldMap.Current, p);
                    float d = Mathf.Sqrt((float)((at.x - gx) * (at.x - gx) + (at.z - gz) * (at.z - gz)));
                    if (d < best) { best = d; treasure = p; }
                }
                if (treasure != null)
                {
                    var at = Places.Position(WorldMap.Current, treasure);
                    float b = Mathf.Repeat(Mathf.Atan2(at.x - (float)gx, at.z - (float)gz) * Mathf.Rad2Deg, 360f);
                    GUI.Label(new Rect(20f, Screen.height - 58f, 560f, 22f), string.Format("<color=#aaccff>The stars:</color> treasure at <b>{0}</b>, {1}, bearing {2:000}°", treasure.name, Distance(best), b), style);
                }
            }
            if (Player != null && RealWorld.Active)
            {
                float dist, bearing;
                var place = RealWorld.Nearest(Player.transform.position, out dist, out bearing);
                if (place != null)
                {
                    string what = PlaceLife.Raided.Contains(place.name) ? "plundered" : PlaceLife.HasMarket(place) ? "market"
                        : place.kind == PlaceKind.Monastery ? "monastery" : place.kind == PlaceKind.Fortress ? "fortress" : place.kind == PlaceKind.Hall ? "jarl's hall" : "landing";
                    GUI.Label(new Rect(20, 134, 440, 22), string.Format("Nearest: <b>{0}</b> ({1}, {2}) {3}, bearing {4:000}°", place.name, place.modern, what, Distance(dist), bearing), style);
                }
            }
            else if (Player != null)
            {
                float dist;
                var island = WorldGen.Nearest(Player.transform.position, out dist);
                if (island != null)
                {
                    Vector2 to = island.Spec.centre - new Vector2(Player.transform.position.x, Player.transform.position.z);
                    float bearing = Mathf.Repeat(Mathf.Atan2(to.x, to.y) * Mathf.Rad2Deg, 360f);
                    string what = island.Spec.monastery ? "  (monastery!)" : "";
                    GUI.Label(new Rect(20, 134, 380, 22), string.Format("Nearest land: <b>{0}</b> {1:0} m, bearing {2:000}°{3}", island.Spec.name, Mathf.Max(0f, dist - island.Spec.radius), bearing, what), style);
                }
            }

            if (Player != null && !string.IsNullOrEmpty(Player.Prompt))
            {
                var r = new Rect(Screen.width / 2f - 160f, Screen.height * 0.62f, 320f, 34f);
                GUI.Box(r, GUIContent.none);
                GUI.Label(new Rect(r.x + 10f, r.y + 6f, r.width - 20f, 24f), "<b>" + Player.Prompt + "</b>", style);
            }
            if (Player != null && Player.Swimming)
                GUI.Label(new Rect(Screen.width / 2f - 100f, Screen.height * 0.55f, 200f, 24f), "<b>Swimming... get back to the ship!</b>", style);
        }

        /// <summary>Gold, Odin's favour and every active blessing / curse with its time left.</summary>
        void DrawFortune()
        {
            var f = Fortune.Current;
            float x = Screen.width - 300f, y = 10f;
            var ravens = Ravens.Instance;
            bool huginn = ravens != null && ravens.Marked.Count > 0 && Player != null;
            int lines = 2 + f.Active.Count + (huginn ? 1 : 0) + (f.Carved.Count > 0 ? 1 : 0);
            GUI.Box(new Rect(x, y, 290f, 28f + lines * 22f), GUIContent.none);
            int cargo;
            int cargoGold = HomeHarbour.CargoValue(Ship, out cargo);
            GUI.Label(new Rect(x + 10f, y + 6f, 280f, 22f), "<b>" + f.Gold + " gold</b>" + (cargo > 0 ? string.Format("   <size=12>cargo: {0} chest{1} ≈ {2}</size>", cargo, cargo == 1 ? "" : "s", cargoGold) : ""), style);
            GUI.Label(new Rect(x + 10f, y + 28f, 100f, 22f), f.CanCallRavens ? "<size=12><color=#ffd060>Ravens ready!</color></size>" : "<size=12>Odin's favour</size>", style);
            GUI.Box(new Rect(x + 110f, y + 34f, 170f, 10f), GUIContent.none);
            var old = GUI.color;
            GUI.color = new Color(1f, 0.8f, 0.3f);
            GUI.DrawTexture(new Rect(x + 111f, y + 35f, 168f * f.Favour, 8f), Texture2D.whiteTexture);
            GUI.color = old;
            float row = y + 52f + f.Active.Count * 22f;
            if (f.Carved.Count > 0)
            {
                var runes = new System.Text.StringBuilder();
                foreach (var r in f.Carved) runes.Append(r.glyph).Append(' ');
                GUI.Label(new Rect(x + 10f, row, 280f, 22f), "<size=12>Runes on the coin:</size> <color=#ffd060>" + runes + "</color>", style);
                row += 22f;
            }
            if (huginn)
            {
                float dist;
                var chest = ravens.NearestMarked(Player.transform.position, out dist);
                if (chest != null)
                {
                    Vector3 to = chest.transform.position - Player.transform.position;
                    float bearing = Mathf.Repeat(Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, 360f);
                    GUI.Label(new Rect(x + 10f, row, 280f, 22f), string.Format("<size=12>Huginn circles treasure: <b>{0:0} m</b>, bearing {1:000}° · {2:0}s</size>", dist, bearing, ravens.HuginnLeft), style);
                }
            }
            for (int i = 0; i < f.Active.Count; i++)
            {
                var a = f.Active[i];
                string colour = a.card.kind == FateKind.Blessing ? "#ffd060" : "#88ff88";
                GUI.Label(new Rect(x + 10f, y + 52f + i * 22f, 280f, 22f),
                    string.Format("<color={0}>{1}</color> <size=12>tier {2} · {3}:{4:00}</size>", colour, a.card.name, a.tier, Mathf.FloorToInt(a.remaining / 60f), Mathf.FloorToInt(a.remaining % 60f)), style);
            }
        }

        /// <summary>Hull water, the storm, and the health of whatever is attacking you.</summary>
        void DrawDangers()
        {
            float y = 162f;
            // Seamanship warnings: the rail under, the anchor dragging.
            string warn = Ship.Shipping > 0.05f ? "Shipping water over the rail! Reef (Q) or bear away."
                : Ship.AnchorDragging > 0.05f ? "The anchor is dragging! Take in sail or find shallower water."
                : null;
            if (warn != null) { GUI.Label(new Rect(20f, y, 420f, 22f), "<color=#ff9a6a><b>" + warn + "</b></color>", style); y += 26f; }
            var hull = Ship.Hull;
            if (hull.Level > 0.01f || hull.Holes > 0)
            {
                GUI.Label(new Rect(20f, y, 380f, 22f), string.Format("<color=#88ccff><b>Water in the hull</b></color> {0:0}%{1}", hull.Level * 100f,
                    hull.Holes > 0 ? "   <color=#ff8866>" + hull.Holes + " hole" + (hull.Holes == 1 ? "" : "s") + "!</color>" : ""), style);
                Bar(new Rect(20f, y + 24f, 200f, 10f), hull.Level, hull.Level > 0.7f ? new Color(1f, 0.35f, 0.3f) : new Color(0.4f, 0.7f, 1f));
                y += 40f;
            }
            var storm = Storm.Instance;
            if (storm != null && storm.Intensity > 0.25f)
            {
                GUI.Label(new Rect(20f, y, 380f, 22f), storm.Intensity > 0.6f ? "<color=#aabbff><b>STORM!</b> Furl the sail and bail.</color>" : "<color=#aabbff>The sky darkens: a storm is on you.</color>", style);
                y += 24f;
            }
            foreach (var r in Raider.All)
            {
                if (r.Sinking) continue;
                float d = Vector3.Distance(r.transform.position, Ship.transform.position);
                GUI.Label(new Rect(20f, y, 380f, 22f), string.Format("<color=#ff8866>Raider</color> {0:0} m{1}", d, r.Ramming ? "  <b>RAMMING!</b>" : ""), style);
                Bar(new Rect(160f, y + 7f, 120f, 8f), r.Hull / Raider.MaxHull, new Color(0.8f, 0.25f, 0.2f));
                y += 24f;
            }
            var serpent = Serpent.Instance;
            if (serpent != null)
            {
                string what = serpent.Brain.State == SerpentBrain.Phase.Stunned ? "<b>STUNNED: hit its head!</b>"
                            : serpent.Brain.State == SerpentBrain.Phase.Rearing ? "<b>REARING UP: get clear!</b>" : "circling...";
                GUI.Label(new Rect(Screen.width / 2f - 200f, 12f, 400f, 22f), "<color=#99ff99><b>JÖRMUNGANDR</b></color>  " + what, style);
                Bar(new Rect(Screen.width / 2f - 200f, 36f, 400f, 10f), serpent.Brain.Health / SerpentBrain.MaxHealth, new Color(0.3f, 0.7f, 0.35f));
            }
        }

        static void Bar(Rect r, float fill, Color colour)
        {
            GUI.Box(r, GUIContent.none);
            var old = GUI.color;
            GUI.color = colour;
            GUI.DrawTexture(new Rect(r.x + 1f, r.y + 1f, (r.width - 2f) * Mathf.Clamp01(fill), r.height - 2f), Texture2D.whiteTexture);
            GUI.color = old;
        }

        static string Arrow(float relative)
        {
            string[] arrows = { "↑", "↗", "→", "↘", "↓", "↙", "←", "↖" };
            return arrows[Mathf.RoundToInt(Mathf.Repeat(relative, 360f) / 45f) % 8];
        }

        string WindWord(float relative)
        {
            if (Ship != null && Ship.Design != null)
            {
                // The new ships: the point of sail, and when she's too close to the wind, the tacks to take.
                float from = 180f - Mathf.Abs(relative);
                float limit = Seamanship.ClosestToWind(Ship.Design, Wind.Knots);
                if (from < limit)
                {
                    float port, starboard;
                    Seamanship.TackHeadings(Wind.Angle + 180f, limit + 5f, out port, out starboard);
                    return string.Format("in irons: tack to {0:000}° or {1:000}°, or row", port, starboard);
                }
                if (from < limit + 15f) return "close-hauled";
                if (from < 110f) return "beam reach";
                if (from < 150f) return "broad reach";
                return "running before the wind";
            }
            float a = Mathf.Abs(relative);
            if (a < 35f) return "tailwind: full speed";
            if (a < 120f) return "wind from the side";
            return "headwind: take the sail down and row";
        }

        /// <summary>The time of day as the HUD shows it (24-hour clock).</summary>
        public static string ClockText(float hours)
        {
            int minutes = Mathf.FloorToInt(Mathf.Repeat(hours, 24f) * 60f);
            return string.Format("{0:00}:{1:00}", minutes / 60, minutes % 60);
        }

        /// <summary>A distance for the HUD: metres close in, kilometres and then sea miles further out.</summary>
        public static string Distance(float metres)
        {
            if (metres < 1000f) return string.Format("{0:0} m", metres);
            if (metres < 20000f) return string.Format("{0:0.0} km", metres / 1000f);
            return string.Format("{0:0} km ({1:0} nm)", metres / 1000f, metres / 1852f);
        }
    }
}
