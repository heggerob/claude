using UnityEngine;

namespace AirsoftArena
{
    /// <summary>Player options, saved in PlayerPrefs.</summary>
    public static class GameSettings
    {
        const string Prefix = "AirsoftArena.Settings.";

        public static bool AutoCallHits
        {
            get { return PlayerPrefs.GetInt(Prefix + "autoCall", 0) == 1; }
            set { PlayerPrefs.SetInt(Prefix + "autoCall", value ? 1 : 0); }
        }

        public static bool ScreenShake
        {
            get { return PlayerPrefs.GetInt(Prefix + "shake", 1) == 1; }
            set { PlayerPrefs.SetInt(Prefix + "shake", value ? 1 : 0); }
        }

        public static bool NameTags
        {
            get { return PlayerPrefs.GetInt(Prefix + "nameTags", 1) == 1; }
            set { PlayerPrefs.SetInt(Prefix + "nameTags", value ? 1 : 0); }
        }

        /// <summary>0..1</summary>
        public static float Volume
        {
            get { return PlayerPrefs.GetFloat(Prefix + "volume", 0.8f); }
            set { PlayerPrefs.SetFloat(Prefix + "volume", Mathf.Clamp01(value)); }
        }

        public static readonly string[] CrosshairStyles = { "Cross", "Dot", "Circle", "Cross + dot" };
        public static readonly Color[] CrosshairColors =
        {
            Color.white, new Color(0.4f, 1f, 0.4f), new Color(1f, 0.9f, 0.3f), new Color(0.4f, 0.95f, 1f), new Color(1f, 0.45f, 0.9f), new Color(1f, 0.35f, 0.3f),
        };
        public static readonly string[] CrosshairColorNames = { "White", "Green", "Yellow", "Cyan", "Pink", "Red" };

        public static int CrosshairStyle
        {
            get { return Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "xhairStyle", 0), 0, CrosshairStyles.Length - 1); }
            set { PlayerPrefs.SetInt(Prefix + "xhairStyle", value); }
        }

        public static int CrosshairColor
        {
            get { return Mathf.Clamp(PlayerPrefs.GetInt(Prefix + "xhairColor", 0), 0, CrosshairColors.Length - 1); }
            set { PlayerPrefs.SetInt(Prefix + "xhairColor", value); }
        }

        /// <summary>Show distance to the cursor and where the BB will be at that range.</summary>
        public static bool RangeFinder
        {
            get { return PlayerPrefs.GetInt(Prefix + "rangeFinder", 1) == 1; }
            set { PlayerPrefs.SetInt(Prefix + "rangeFinder", value ? 1 : 0); }
        }

        public const float DefaultViewDistance = 11f, MinViewDistance = 7f, MaxViewDistance = 18f;

        /// <summary>Camera zoom while playing (orthographic size): bigger = see further.</summary>
        public static float ViewDistance
        {
            get { return PlayerPrefs.GetFloat(Prefix + "viewDistance", DefaultViewDistance); }
            set { PlayerPrefs.SetFloat(Prefix + "viewDistance", Mathf.Clamp(value, MinViewDistance, MaxViewDistance)); }
        }

        public static void Save() { PlayerPrefs.Save(); }
    }
}
