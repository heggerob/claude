using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>Health bar, floating damage numbers, a red flash when hurt and big banners (death, rank, loot).</summary>
    public class CombatHud : MonoBehaviour
    {
        class Floating { public Vector3 world; public string text; public Color color; public float born; }

        static readonly List<Floating> numbers = new List<Floating>();
        static string bannerTitle, bannerText;
        static float bannerAt = -10f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { numbers.Clear(); bannerAt = -10f; }

        public VikingCombat Player;
        GUIStyle numberStyle, titleStyle, textStyle;

        public static void Number(Vector3 world, string text, Color color)
        {
            numbers.Add(new Floating { world = world, text = text, color = color, born = Time.time });
        }

        public static void Banner(string title, string text)
        {
            bannerTitle = title;
            bannerText = text;
            bannerAt = Time.time;
        }

        void OnGUI()
        {
            if (numberStyle == null)
            {
                numberStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 40, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleCenter };
                textStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, alignment = TextAnchor.MiddleCenter };
            }
            var cam = Camera.main;

            // Floating numbers rise and fade.
            for (int i = numbers.Count - 1; i >= 0; i--)
            {
                var n = numbers[i];
                float age = Time.time - n.born;
                if (age > 1.2f) { numbers.RemoveAt(i); continue; }
                if (cam == null) continue;
                Vector3 sp = cam.WorldToScreenPoint(n.world + Vector3.up * age * 0.8f);
                if (sp.z < 0f) continue;
                var c = n.color;
                c.a = 1f - age / 1.2f;
                numberStyle.normal.textColor = new Color(0f, 0f, 0f, c.a * 0.7f);
                GUI.Label(new Rect(sp.x - 99f, Screen.height - sp.y - 11f, 200f, 24f), n.text, numberStyle);
                numberStyle.normal.textColor = c;
                GUI.Label(new Rect(sp.x - 100f, Screen.height - sp.y - 12f, 200f, 24f), n.text, numberStyle);
            }

            if (Player != null)
            {
                var h = Player.Health;
                // Red flash when hurt.
                float since = Time.time - h.LastHitTime;
                if (since < 0.35f)
                {
                    var old = GUI.color;
                    GUI.color = new Color(1f, 0f, 0f, 0.3f * (1f - since / 0.35f));
                    GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
                    GUI.color = old;
                }
                // Health bar, bottom centre.
                float w = 300f, x = (Screen.width - w) / 2f, y = Screen.height - 40f;
                GUI.Box(new Rect(x - 4f, y - 4f, w + 8f, 22f), GUIContent.none);
                var oldC = GUI.color;
                GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
                GUI.DrawTexture(new Rect(x, y, w, 14f), Texture2D.whiteTexture);
                GUI.color = h.Current / h.Max > 0.35f ? new Color(0.75f, 0.2f, 0.18f) : new Color(1f, 0.3f, 0.2f);
                GUI.DrawTexture(new Rect(x, y, w * h.Current / Mathf.Max(1f, h.Max), 14f), Texture2D.whiteTexture);
                GUI.color = oldC;
                if (Player.Blocking) GUI.Label(new Rect(x, y - 26f, w, 22f), "SHIELD UP", textStyle);
            }

            // Banner.
            float bAge = Time.time - bannerAt;
            if (bAge < 3f)
            {
                float a = bAge < 2.4f ? 1f : 1f - (bAge - 2.4f) / 0.6f;
                titleStyle.normal.textColor = new Color(1f, 0.85f, 0.35f, a);
                textStyle.normal.textColor = new Color(1f, 1f, 1f, a);
                GUI.Label(new Rect(0, Screen.height * 0.3f, Screen.width, 50f), bannerTitle, titleStyle);
                GUI.Label(new Rect(0, Screen.height * 0.3f + 52f, Screen.width, 26f), bannerText, textStyle);
            }
        }
    }
}
