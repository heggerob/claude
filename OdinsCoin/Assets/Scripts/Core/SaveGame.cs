using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Saves what you've earned: gold, Odin's favour, the runes on your coin, ship and gear upgrades and the
    /// boasting board. Blessings and curses are fleeting and aren't kept; neither is cargo still at sea.
    /// Stored in PlayerPrefs as simple "key=value" lines, with a version so old saves can be read later.
    /// </summary>
    public static class SaveGame
    {
        public const string Key = "odinscoin.save";
        public const int Version = 1;
        public const float AutosaveEvery = 30f;

        public static bool Exists { get { return PlayerPrefs.HasKey(Key); } }

        public static string Serialize(Fortune f, Upgrades u)
        {
            var sb = new StringBuilder();
            Line(sb, "version", Version.ToString(CultureInfo.InvariantCulture));
            Line(sb, "gold", f.Gold.ToString(CultureInfo.InvariantCulture));
            Line(sb, "favour", f.Favour.ToString("R", CultureInfo.InvariantCulture));
            Line(sb, "muninn", f.NextFlipBlessed ? "1" : "0");
            var runes = new List<string>();
            foreach (var r in f.Carved) runes.Add(r.id);
            Line(sb, "runes", string.Join(",", runes.ToArray()));
            Line(sb, "flips", f.Flips + "," + f.HeadsCount);
            Line(sb, "plunder", f.ChestsSold + "," + f.GoldPlundered);
            Line(sb, "dice", f.DiceWon + "," + f.DiceLost);
            var levels = new string[u.Levels.Length];
            for (int i = 0; i < levels.Length; i++) levels[i] = u.Levels[i].ToString(CultureInfo.InvariantCulture);
            Line(sb, "upgrades", string.Join(",", levels));
            Line(sb, "fleet", string.Join(",", u.Fleet.ToArray()));
            Line(sb, "sailing", u.Sailing);
            Line(sb, "clock", u.ClockDay.ToString(CultureInfo.InvariantCulture) + "," + u.ClockHours.ToString("R", CultureInfo.InvariantCulture));
            if (u.AtSea)
                Line(sb, "at", u.SeaX.ToString("R", CultureInfo.InvariantCulture) + "," + u.SeaZ.ToString("R", CultureInfo.InvariantCulture) + "," + u.SeaHeading.ToString("R", CultureInfo.InvariantCulture));
            return sb.ToString();
        }

        static void Line(StringBuilder sb, string key, string value) { sb.Append(key).Append('=').Append(value).Append('\n'); }

        /// <summary>Read a save into a fresh fortune and upgrades. Unknown or broken lines are skipped.</summary>
        public static bool Deserialize(string text, out Fortune f, out Upgrades u)
        {
            f = new Fortune();
            u = new Upgrades();
            if (string.IsNullOrEmpty(text)) return false;
            bool any = false;
            foreach (var raw in text.Split('\n'))
            {
                int eq = raw.IndexOf('=');
                if (eq <= 0) continue;
                string key = raw.Substring(0, eq).Trim(), value = raw.Substring(eq + 1).Trim();
                int[] nums = Ints(value);
                switch (key)
                {
                    case "gold": if (nums.Length > 0) { f.Gold = Mathf.Max(0, nums[0]); any = true; } break;
                    case "favour":
                        float fav;
                        if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out fav)) f.Favour = Mathf.Clamp01(fav);
                        break;
                    case "muninn": f.NextFlipBlessed = value == "1"; break;
                    case "runes":
                        foreach (var id in value.Split(','))
                        {
                            var r = Runes.Find(id.Trim());
                            if (r != null && !f.Carved.Contains(r) && f.Carved.Count < Runes.Slots) f.Carved.Add(r);
                        }
                        break;
                    case "flips": if (nums.Length == 2) { f.Flips = nums[0]; f.HeadsCount = nums[1]; } break;
                    case "plunder": if (nums.Length == 2) { f.ChestsSold = nums[0]; f.GoldPlundered = nums[1]; } break;
                    case "dice": if (nums.Length == 2) { f.DiceWon = nums[0]; f.DiceLost = nums[1]; } break;
                    case "clock":
                        var clock = value.Split(',');
                        int cd; float ch;
                        if (clock.Length == 2 && int.TryParse(clock[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out cd)
                            && float.TryParse(clock[1], NumberStyles.Float, CultureInfo.InvariantCulture, out ch) && !float.IsNaN(ch))
                        { u.ClockDay = Mathf.Clamp(cd, 0, 100000); u.ClockHours = Mathf.Repeat(ch, 24f); }
                        break;
                    case "at":
                        var parts = value.Split(',');
                        double sx, sz; float sh;
                        if (parts.Length == 3
                            && double.TryParse(parts[0], NumberStyles.Float, CultureInfo.InvariantCulture, out sx)
                            && double.TryParse(parts[1], NumberStyles.Float, CultureInfo.InvariantCulture, out sz)
                            && float.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out sh)
                            && !double.IsNaN(sx) && !double.IsNaN(sz) && System.Math.Abs(sx) < 1e8 && System.Math.Abs(sz) < 1e8)
                        { u.AtSea = true; u.SeaX = sx; u.SeaZ = sz; u.SeaHeading = Mathf.Repeat(sh, 360f); }
                        break;
                    case "fleet":
                        foreach (var id in value.Split(','))
                        {
                            var d = Shipwright.Find(id.Trim());
                            if (d != null && !u.Owns(d)) u.Fleet.Add(d.id);
                        }
                        break;
                    case "sailing":
                        var ship = Shipwright.Find(value);
                        if (ship != null) u.Sailing = ship.id;
                        break;
                    case "upgrades":
                        for (int i = 0; i < nums.Length && i < u.Levels.Length; i++)
                            u.Levels[i] = Mathf.Clamp(nums[i], 0, Upgrades.All[i].costs.Length);
                        break;
                }
            }
            return any;
        }

        static int[] Ints(string value)
        {
            var list = new List<int>();
            foreach (var part in value.Split(','))
            {
                int n;
                if (int.TryParse(part.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out n)) list.Add(n);
            }
            return list.ToArray();
        }

        public static void Save()
        {
            RecordVoyage(Upgrades.Current);
            PlayerPrefs.SetString(Key, Serialize(Fortune.Current, Upgrades.Current));
            PlayerPrefs.Save();
        }

        /// <summary>Load the save into the running game. False if there's none (or it's unreadable).</summary>
        public static bool Load()
        {
            Fortune f;
            Upgrades u;
            if (!Deserialize(PlayerPrefs.GetString(Key, ""), out f, out u)) return false;
            Fortune.SetCurrent(f);
            Upgrades.SetCurrent(u);
            return true;
        }

        /// <summary>Note where the ship is, if she's out in the real North rather than lying at home.</summary>
        public static void RecordVoyage(Upgrades u)
        {
            if (SkyClock.Instance != null) { u.ClockDay = SkyClock.Day; u.ClockHours = SkyClock.Hours; }
            var boot = GameBootstrap.Instance;
            u.AtSea = false;
            if (boot == null || boot.Ship == null || !RealWorld.Active) return;
            var home = HomeHarbour.Instance;
            if (home != null && home.ShipInRange(boot.Ship)) return; // at home: she starts at the jetty anyway
            var p = boot.Ship.transform.position;
            u.AtSea = true;
            u.SeaX = WorldOrigin.GlobalX(p);
            u.SeaZ = WorldOrigin.GlobalZ(p);
            u.SeaHeading = boot.Ship.Heading;
        }

        public static void NewGame()
        {
            Fortune.SetCurrent(new Fortune());
            Upgrades.SetCurrent(new Upgrades());
            Save();
        }
    }
}
