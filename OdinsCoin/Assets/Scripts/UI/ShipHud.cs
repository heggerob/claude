using UnityEngine;

namespace OdinsCoin
{
    /// <summary>Temporary sailing HUD: speed, heading, sail and a wind arrow relative to the ship.</summary>
    public class ShipHud : MonoBehaviour
    {
        public Longship Ship;
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
                "<b>{0:0.0} knots</b>   heading {1:000}°\nSail {2:0}%{3}\nWind {4:0} kn  {5} ({6})\n\n<size=12>A/D steer · R raise sail · Q lower sail · W row (sail down) · mouse look · scroll zoom · Esc cursor</size>",
                Ship.SpeedKnots, Ship.Heading, Ship.SailAmount * 100f, Ship.Rowing ? "   ROWING" : "", Wind.Knots, arrow, WindWord(relWind));
            GUI.Box(new Rect(10, 10, 380, 120), GUIContent.none);
            GUI.Label(new Rect(20, 16, 370, 110), text, style);
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
