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
