using System.Globalization;
using System.Text;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The hero the player has chosen: outfit, body (height, build, gender) and the weapon and off-hand item they
    /// carry. Kept in its own PlayerPrefs key, apart from the voyage save, so starting a new voyage keeps your hero.
    /// </summary>
    public static class HeroChoice
    {
        public const string Key = "odinscoin.hero";

        public static CharacterSpec Load()
        {
            var spec = Parse(PlayerPrefs.GetString(Key, ""));
            // A skin you don't own (a new install, a hand-edited key) falls back to the classic colours.
            if (spec.skin != null && !SkinLocker.Current.Owns(spec.skin)) spec.skin = null;
            return spec;
        }

        public static void Save(CharacterSpec spec)
        {
            PlayerPrefs.SetString(Key, Serialize(spec));
            PlayerPrefs.Save();
        }

        public static string Serialize(CharacterSpec spec)
        {
            var sb = new StringBuilder();
            sb.Append("outfit=").Append(spec.outfit).Append('\n');
            sb.Append("height=").Append(spec.body.height.ToString("0.###", CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("width=").Append(spec.body.width.ToString("0.###", CultureInfo.InvariantCulture)).Append('\n');
            sb.Append("gender=").Append(spec.body.gender).Append('\n');
            sb.Append("hair=").Append(spec.hair).Append('\n');
            sb.Append("weapon=").Append(spec.weapon).Append('\n');
            sb.Append("offhand=").Append(spec.offHand).Append('\n');
            if (spec.skin != null) sb.Append("skin=").Append(spec.skin).Append('\n');
            return sb.ToString();
        }

        /// <summary>
        /// Read a hero back. Anything missing or broken falls back to the outfit's defaults, and the body is clamped,
        /// so a damaged key never gives a broken hero. An empty string gives the default Raider.
        /// </summary>
        public static CharacterSpec Parse(string text)
        {
            var spec = CharacterSpec.Default(OutfitId.Raider);
            if (string.IsNullOrEmpty(text)) return spec;
            string[] lines = text.Split('\n');
            // The outfit first, since it decides the defaults for everything else.
            foreach (var line in lines)
            {
                OutfitId o;
                string v = Value(line, "outfit");
                if (v != null && TryEnum(v, out o)) spec = CharacterSpec.Default(o);
            }
            foreach (var line in lines)
            {
                string v;
                float x;
                if ((v = Value(line, "height")) != null && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out x) && !float.IsNaN(x))
                    spec.body.height = BodyShape.ClampHeight(x);
                else if ((v = Value(line, "width")) != null && float.TryParse(v, NumberStyles.Float, CultureInfo.InvariantCulture, out x) && !float.IsNaN(x))
                    spec.body.width = BodyShape.ClampWidth(x);
                else if ((v = Value(line, "gender")) != null) { Gender g; if (TryEnum(v, out g)) spec.body.gender = g; }
                else if ((v = Value(line, "hair")) != null) { HairStyle h; if (TryEnum(v, out h)) spec.hair = h; }
                else if ((v = Value(line, "weapon")) != null) { WeaponId w; if (TryEnum(v, out w)) spec.weapon = w; }
                else if ((v = Value(line, "offhand")) != null) { OffHandId oh; if (TryEnum(v, out oh)) spec.offHand = oh; }
                else if ((v = Value(line, "skin")) != null) { var s = Skins.Get(v); if (s != null && s.outfit == spec.outfit && s.cost > 0) spec.skin = s.id; }
            }
            return spec;
        }

        static string Value(string line, string key)
        {
            line = line.Trim();
            return line.StartsWith(key + "=") ? line.Substring(key.Length + 1).Trim() : null;
        }

        static bool TryEnum<T>(string s, out T value) where T : struct
        {
            // Only exact names: numbers could name values that don't exist.
            foreach (T v in System.Enum.GetValues(typeof(T)))
                if (v.ToString() == s) { value = v; return true; }
            value = default(T);
            return false;
        }

        /// <summary>The next (or previous) value of an enum, wrapping round: for the ◀ ▶ buttons.</summary>
        public static T Cycle<T>(T current, int step) where T : struct
        {
            var all = (T[])System.Enum.GetValues(typeof(T));
            int i = System.Array.IndexOf(all, current);
            if (i < 0) i = 0;
            int n = all.Length;
            return all[((i + step) % n + n) % n];
        }

        /// <summary>A short English name for a weapon or off-hand item, for the menu.</summary>
        public static string Name(WeaponId w)
        {
            switch (w)
            {
                case WeaponId.TwoHandAxe: return "Bearded axe";
                case WeaponId.Sword: return "Sword";
                case WeaponId.Spear: return "Spear";
                case WeaponId.Staff: return "Rune staff";
                case WeaponId.Bow: return "Bow";
                default: return "Bare hands";
            }
        }

        public static string Name(OffHandId o)
        {
            switch (o)
            {
                case OffHandId.RoundShield: return "Round shield";
                case OffHandId.KnotShield: return "Knot shield";
                case OffHandId.Map: return "Sea chart";
                default: return "Nothing";
            }
        }
    }
}
