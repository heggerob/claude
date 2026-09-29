using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    public enum CamoPattern { Solid, Woodland, Digital, Tiger }

    /// <summary>
    /// Procedural pixel art for soldiers and weapons, drawn top-down facing +X (right).
    /// Everything is 16 px per metre, same as the rest of the game.
    /// Grayscale layers are tinted by SpriteRenderer.color; coloured layers are used as-is.
    /// </summary>
    public static class PixelArt
    {
        const int PPU = SpriteFactory.PixelsPerUnit;
        const int Size = 16;

        static readonly Dictionary<CamoPattern, Sprite> bodies = new Dictionary<CamoPattern, Sprite>();
        static readonly Dictionary<string, GunArt> guns = new Dictionary<string, GunArt>();
        static Sprite helmet, cap, boonie, details, foot, shadow;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            bodies.Clear();
            guns.Clear();
            helmet = cap = boonie = details = foot = shadow = null;
        }

        // ================================================================ soldier layers

        /// <summary>Torso, shoulders and arms reaching forward. Tint = uniform colour.</summary>
        public static Sprite Body(CamoPattern pattern)
        {
            Sprite s;
            if (bodies.TryGetValue(pattern, out s)) return s;
            var tex = Blank(Size, Size);
            var rng = new System.Random(1234 + (int)pattern);
            var camo = CamoMask(pattern, rng);

            // Shoulders: an ellipse wider across (y) than deep (x), since we look from above.
            for (int y = 0; y < Size; y++)
            {
                for (int x = 0; x < Size; x++)
                {
                    float dx = (x + 0.5f - 7f) / 3.6f, dy = (y + 0.5f - 8f) / 6.2f;
                    float d = dx * dx + dy * dy;
                    if (d > 1f) continue;
                    float shade = d > 0.78f ? 0.45f : (y >= 8 ? 0.95f : 0.8f);
                    tex.SetPixel(x, y, Gray(shade * camo[x, y]));
                }
            }

            // Arms from the shoulders to where the hands hold a rifle.
            Arm(tex, camo, new Vector2(7f, 3.2f), new Vector2(10.5f, 6.5f));
            Arm(tex, camo, new Vector2(7f, 12.8f), new Vector2(13f, 8.5f));

            s = Make(tex, new Vector2(0.5f, 0.5f));
            bodies[pattern] = s;
            return s;
        }

        /// <summary>Helmet seen from above. Tint = team colour.</summary>
        public static Sprite Helmet
        {
            get
            {
                if (helmet != null) return helmet;
                var tex = Blank(Size, Size);
                Disc(tex, 6.6f, 8f, 3.3f, 0.95f, 0.5f);
                // A little strap line / cover seam.
                for (int y = 6; y <= 10; y++) tex.SetPixel(6, y, Gray(0.72f));
                return helmet = Make(tex, new Vector2(0.5f, 0.5f));
            }
        }

        /// <summary>Baseball cap with a peak pointing forward. Tint = team colour.</summary>
        public static Sprite Cap
        {
            get
            {
                if (cap != null) return cap;
                var tex = Blank(Size, Size);
                Disc(tex, 6.5f, 8f, 3f, 0.95f, 0.5f);
                for (int y = 6; y <= 9; y++) { tex.SetPixel(9, y, Gray(0.7f)); tex.SetPixel(10, y, Gray(0.6f)); }
                return cap = Make(tex, new Vector2(0.5f, 0.5f));
            }
        }

        /// <summary>Floppy boonie hat, wider brim. Tint = team colour.</summary>
        public static Sprite Boonie
        {
            get
            {
                if (boonie != null) return boonie;
                var tex = Blank(Size, Size);
                Disc(tex, 6.6f, 8f, 4.3f, 0.7f, 0.4f);
                Disc(tex, 6.6f, 8f, 2.6f, 0.95f, 0.95f);
                return boonie = Make(tex, new Vector2(0.5f, 0.5f));
            }
        }

        public static Sprite HeadGear(HeadGearStyle style)
        {
            switch (style)
            {
                case HeadGearStyle.Cap: return Cap;
                case HeadGearStyle.Boonie: return Boonie;
                default: return Helmet;
            }
        }

        /// <summary>Goggles (untinted). Drawn over the head gear.</summary>
        public static Sprite Details
        {
            get
            {
                if (details != null) return details;
                var tex = Blank(Size, Size);
                var frame = new Color(0.08f, 0.08f, 0.09f);
                var lens = new Color(0.25f, 0.35f, 0.45f);
                for (int y = 6; y <= 9; y++) tex.SetPixel(9, y, frame);
                tex.SetPixel(10, 6, frame); tex.SetPixel(10, 9, frame);
                tex.SetPixel(10, 7, lens); tex.SetPixel(10, 8, new Color(0.55f, 0.7f, 0.85f));
                return details = Make(tex, new Vector2(0.5f, 0.5f));
            }
        }

        /// <summary>One boot. Two of these are animated under the body when walking.</summary>
        public static Sprite Foot
        {
            get
            {
                if (foot != null) return foot;
                var tex = Blank(4, 3);
                var boot = new Color(0.16f, 0.13f, 0.1f);
                for (int y = 0; y < 3; y++) for (int x = 0; x < 4; x++) tex.SetPixel(x, y, boot);
                tex.SetPixel(3, 0, Color.clear); tex.SetPixel(3, 2, Color.clear);
                return foot = Make(tex, new Vector2(0.5f, 0.5f));
            }
        }

        /// <summary>Soft dark blob under a character.</summary>
        public static Sprite Shadow
        {
            get
            {
                if (shadow != null) return shadow;
                var tex = Blank(Size, Size);
                for (int y = 0; y < Size; y++)
                    for (int x = 0; x < Size; x++)
                    {
                        float dx = (x + 0.5f - 8f) / 6.5f, dy = (y + 0.5f - 8f) / 6.5f;
                        float d = dx * dx + dy * dy;
                        if (d < 1f) tex.SetPixel(x, y, new Color(0f, 0f, 0f, 0.35f * (1f - d * d)));
                    }
                return shadow = Make(tex, new Vector2(0.5f, 0.5f));
            }
        }

        // ================================================================ weapons

        public class GunArt
        {
            public Sprite sprite;
            /// <summary>Where the sprite's pivot (stock end / grip) sits, relative to the body centre, in metres.</summary>
            public Vector2 anchor;
            /// <summary>Distance from the body centre to the muzzle along the aim direction.</summary>
            public float muzzleDistance;
        }

        // Legend: k black, d dark grey, g grey, l light grey, w wood, t tan, o orange tip, h hand, m metal, . empty
        // Rows are listed top (left side of the soldier) to bottom (right side). Guns point right.
        static readonly Dictionary<string, string[]> Designs = new Dictionary<string, string[]>
        {
            { "rifle",    new[] { "kk....ddddddd...", "kkkkhkggggghgkko", "kk...kddddddd..." } },
            { "rifle2",   new[] { "tt....tttttt....", "ttkkhkttttthkkko", "tt...ktttttt...." } },
            { "smg",      new[] { ".....ddd...", "kkkhkggghko", ".....kddd.." } },
            { "sniper",   new[] { "ww.....llllll.......", "wwwwwhwwkkkhkkkkkkko", "ww.....llllll......." } },
            { "dmr",      new[] { "kk....llll........", "kkkkhkkkkkhkkkkkko", "kk...kllll........" } },
            { "shotgun",  new[] { "................", "wwwwhkkkkkkkkkko", "........wwhw...." } },
            { "burst",    new[] { "dd....gggggg...", "ddkkhkkkkkhkkko", "dd...kgggggg..." } },
            { "lmg",      new[] { "kk....ddddd........", "kkkkhkkkkkkhkkkkkko", "....kllll..........", "....llll..........." } },
            { "pistol",   new[] { "hkkkko", "h....." } },
            { "revolver", new[] { ".mm....", "hmmmmmo", "h......" } },
            { "mpistol",  new[] { "hkkkkko", "hk.....", ".k....." } },
            { "knife",    new[] { "hddmmmm" } },
        };

        public static GunArt Gun(WeaponData weapon)
        {
            string key = DesignFor(weapon);
            GunArt art;
            if (guns.TryGetValue(key, out art)) return art;

            string[] rows = Designs[key];
            int w = rows[0].Length, h = rows.Length;
            var tex = Blank(w, h);
            for (int r = 0; r < h; r++)
            {
                string row = rows[r];
                for (int x = 0; x < w && x < row.Length; x++)
                {
                    Color c;
                    if (TryColor(row[x], out c)) tex.SetPixel(x, h - 1 - r, c);
                }
            }

            bool handgun = weapon.weaponClass == WeaponClass.Pistol || weapon.IsMelee;
            // Long guns: stock end at the right shoulder. Handguns and knives: held out front.
            var anchor = handgun ? new Vector2(0.42f, -0.06f) : new Vector2(-0.05f, -0.08f);
            art = new GunArt
            {
                sprite = Make(tex, new Vector2(0f, 0.5f)),
                anchor = anchor,
                muzzleDistance = anchor.x + w / (float)PPU,
            };
            guns[key] = art;
            return art;
        }

        static string DesignFor(WeaponData w)
        {
            switch (w.code)
            {
                case "01-FR7": return "rifle2";
                case "03-RD1": return "dmr";
                case "04-FJ6": return "revolver";
                case "04-BZ9": return "mpistol";
            }
            switch (w.weaponClass)
            {
                case WeaponClass.Melee: return "knife";
                case WeaponClass.SubmachineGun: return "smg";
                case WeaponClass.SniperMarksman: return "sniper";
                case WeaponClass.Pistol: return "pistol";
                case WeaponClass.Shotgun: return "shotgun";
                case WeaponClass.BurstRifle: return "burst";
                case WeaponClass.LightMachineGun: return "lmg";
                default: return "rifle";
            }
        }

        static bool TryColor(char c, out Color color)
        {
            switch (c)
            {
                case 'k': color = new Color(0.1f, 0.1f, 0.11f); return true;
                case 'd': color = new Color(0.2f, 0.2f, 0.21f); return true;
                case 'g': color = new Color(0.33f, 0.34f, 0.35f); return true;
                case 'l': color = new Color(0.5f, 0.52f, 0.54f); return true;
                case 'm': color = new Color(0.62f, 0.64f, 0.66f); return true;
                case 'w': color = new Color(0.45f, 0.29f, 0.16f); return true;
                case 't': color = new Color(0.66f, 0.58f, 0.42f); return true;
                case 'o': color = new Color(1f, 0.45f, 0.05f); return true;
                case 'h': color = new Color(0.93f, 0.74f, 0.58f); return true;
                default: color = Color.clear; return false;
            }
        }

        // ================================================================ helpers

        static void Arm(Texture2D tex, float[,] camo, Vector2 from, Vector2 to)
        {
            int steps = 12;
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(from, to, i / (float)steps);
                for (int oy = 0; oy < 2; oy++)
                {
                    int x = Mathf.RoundToInt(p.x), y = Mathf.RoundToInt(p.y) + oy - 1;
                    if (x < 0 || y < 0 || x >= Size || y >= Size) continue;
                    float shade = oy == 0 ? 0.62f : 0.85f;
                    tex.SetPixel(x, y, Gray(shade * camo[x, y]));
                }
            }
        }

        static float[,] CamoMask(CamoPattern pattern, System.Random rng)
        {
            var m = new float[Size, Size];
            for (int y = 0; y < Size; y++) for (int x = 0; x < Size; x++) m[x, y] = 1f;

            switch (pattern)
            {
                case CamoPattern.Woodland:
                    for (int blob = 0; blob < 7; blob++)
                    {
                        int cx = rng.Next(Size), cy = rng.Next(Size), r = 1 + rng.Next(3);
                        float v = blob % 2 == 0 ? 0.55f : 0.72f;
                        for (int y = cy - r; y <= cy + r; y++)
                            for (int x = cx - r; x <= cx + r; x++)
                                if (x >= 0 && y >= 0 && x < Size && y < Size && (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r + 1)
                                    m[x, y] = v;
                    }
                    break;
                case CamoPattern.Digital:
                    for (int y = 0; y < Size; y += 2)
                        for (int x = 0; x < Size; x += 2)
                        {
                            int n = rng.Next(10);
                            float v = n < 3 ? 0.6f : n < 5 ? 0.78f : 1f;
                            m[x, y] = m[Mathf.Min(x + 1, Size - 1), y] = m[x, Mathf.Min(y + 1, Size - 1)] = m[Mathf.Min(x + 1, Size - 1), Mathf.Min(y + 1, Size - 1)] = v;
                        }
                    break;
                case CamoPattern.Tiger:
                    for (int x = 0; x < Size; x++)
                    {
                        int offset = rng.Next(3);
                        if ((x + offset) % 4 != 0) continue;
                        for (int y = 0; y < Size; y++)
                            if (rng.Next(10) < 7) m[x, y] = 0.5f;
                    }
                    break;
            }
            return m;
        }

        static void Disc(Texture2D tex, float cx, float cy, float r, float fill, float rim)
        {
            for (int y = 0; y < tex.height; y++)
                for (int x = 0; x < tex.width; x++)
                {
                    float d = new Vector2(x + 0.5f - cx, y + 0.5f - cy).magnitude;
                    if (d > r) continue;
                    // Light from the top-left: brighter on the upper side.
                    float shade = d > r - 1f ? rim : (y + 0.5f > cy ? fill : fill * 0.85f);
                    tex.SetPixel(x, y, Gray(shade));
                }
        }

        static Color Gray(float v) { return new Color(v, v, v, 1f); }

        static Texture2D Blank(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) tex.SetPixel(x, y, Color.clear);
            return tex;
        }

        static Sprite Make(Texture2D tex, Vector2 pivot)
        {
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, PPU, 0, SpriteMeshType.FullRect);
        }
    }
}
