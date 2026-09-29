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
            if (Ship == null) return;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.label) { fontSize = 16, richText = true };
                style.normal.textColor = Color.white;
            }
            float relWind = Mathf.DeltaAngle(Ship.Heading, Wind.Angle);
            string arrow = Arrow(relWind);
            string text = string.Format(
                "<b>{0:0.0} knots</b>   heading {1:000}°\nSail {2:0}%{3}\nWind {4:0} kn  {5} ({6})\n\n<size=12>{7}</size>",
                Ship.SpeedKnots, Ship.Heading, Ship.SailAmount * 100f, Ship.Rowing ? "   ROWING" : "", Wind.Knots, arrow, WindWord(relWind),
                Player != null && Player.AtHelm
                    ? "At the helm: A/D steer · R raise sail · Q lower sail · W row (sail down) · E let go"
                    : "WASD walk · Shift run · Space jump · E use · mouse look · scroll zoom · Esc cursor");
            GUI.Box(new Rect(10, 10, 380, 120), GUIContent.none);
            GUI.Label(new Rect(20, 16, 370, 110), text, style);

            DrawFortune();

            // Nearest land.
            if (Player != null)
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
            int lines = 2 + f.Active.Count;
            GUI.Box(new Rect(x, y, 290f, 28f + lines * 22f), GUIContent.none);
            GUI.Label(new Rect(x + 10f, y + 6f, 280f, 22f), "<b>" + f.Gold + " gold</b>", style);
            GUI.Label(new Rect(x + 10f, y + 28f, 90f, 22f), "<size=12>Odin's favour</size>", style);
            GUI.Box(new Rect(x + 110f, y + 34f, 170f, 10f), GUIContent.none);
            var old = GUI.color;
            GUI.color = new Color(1f, 0.8f, 0.3f);
            GUI.DrawTexture(new Rect(x + 111f, y + 35f, 168f * f.Favour, 8f), Texture2D.whiteTexture);
            GUI.color = old;
            for (int i = 0; i < f.Active.Count; i++)
            {
                var a = f.Active[i];
                string colour = a.card.kind == FateKind.Blessing ? "#ffd060" : "#88ff88";
                GUI.Label(new Rect(x + 10f, y + 52f + i * 22f, 280f, 22f),
                    string.Format("<color={0}>{1}</color> <size=12>tier {2} · {3}:{4:00}</size>", colour, a.card.name, a.tier, Mathf.FloorToInt(a.remaining / 60f), Mathf.FloorToInt(a.remaining % 60f)), style);
            }
        }

        static string Arrow(float relative)
        {
            string[] arrows = { "↑", "↗", "→", "↘", "↓", "↙", "←", "↖" };
            return arrows[Mathf.RoundToInt(Mathf.Repeat(relative, 360f) / 45f) % 8];
        }

        static string WindWord(float relative)
        {
            float a = Mathf.Abs(relative);
            if (a < 35f) return "tailwind: full speed";
            if (a < 120f) return "wind from the side";
            return "headwind: take the sail down and row";
        }
    }
}
