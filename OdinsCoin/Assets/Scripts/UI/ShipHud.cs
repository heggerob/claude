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

            if (Player != null && !string.IsNullOrEmpty(Player.Prompt))
            {
                var r = new Rect(Screen.width / 2f - 160f, Screen.height * 0.62f, 320f, 34f);
                GUI.Box(r, GUIContent.none);
                GUI.Label(new Rect(r.x + 10f, r.y + 6f, r.width - 20f, 24f), "<b>" + Player.Prompt + "</b>", style);
            }
            if (Player != null && Player.Swimming)
                GUI.Label(new Rect(Screen.width / 2f - 100f, Screen.height * 0.55f, 200f, 24f), "<b>Swimming... get back to the ship!</b>", style);
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
