using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    public enum CamoPattern { Solid, Woodland, Digital, Tiger }

    /// <summary>
    /// Procedural pixel art for soldiers and weapons, drawn top-down facing +X (right), 32 px per metre.
    /// Grayscale layers are tinted by SpriteRenderer.color; coloured layers are used as-is.
    /// Soldier layers, bottom to top: shadow, boots, body (uniform + arms), gear (vest, pouches, pack),
    /// armband (team), gun, head gear (team), goggles.
    /// </summary>
    public static class PixelArt
    {
        const int PPU = SpriteFactory.PixelsPerUnit;
        const int Size = 32;

        static readonly Dictionary<CamoPattern, Sprite> bodies = new Dictionary<CamoPattern, Sprite>();
        static readonly Dictionary<string, GunArt> guns = new Dictionary<string, GunArt>();
        static Sprite helmet, cap, boonie, details, foot, shadow, gear, armband;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            bodies.Clear();
            guns.Clear();
            helmet = cap = boonie = details = foot = shadow = gear = armband = null;
        }

        // ================================================================ soldier layers

        /// <summary>Torso, shoulders and both arms reaching forward to the gun. Tint = uniform colour.</summary>
        public static Sprite Body(CamoPattern pattern)
        {
            Sprite s;
            if (bodies.TryGetValue(pattern, out s)) return s;
            var tex = Blank(Size, Size);
            var camo = CamoMask(pattern, new System.Random(1234 + (int)pattern));

            // Arms first so the shoulders overlap them.
            Arm(tex, camo, new Vector2(15f, 6f), new Vector2(21.5f, 12.5f), 3.2f);   // right arm to the pistol grip
            Arm(tex, camo, new Vector2(15f, 26f), new Vector2(26.5f, 17.5f), 3.2f);  // left arm to the handguard

            // Shoulders: an ellipse wider across (y) than deep (x), since we look from above.
            for (int y = 0; y < Size; y++)
                for (int x = 0; x < Size; x++)
                {
                    float dx = (x + 0.5f - 14f) / 6.8f, dy = (y + 0.5f - 16f) / 12.2f;
                    float d = dx * dx + dy * dy;
                    if (d > 1f) continue;
                    // Light from the upper left: brighter on the left shoulder, darker rim.
                    float shade = d > 0.85f ? 0.42f : Mathf.Lerp(0.72f, 1f, (y / (float)Size) * 0.8f + (1f - x / (float)Size) * 0.2f);
                    // A fold line down the back.
                    if (x == 9 && y > 8 && y < 24) shade *= 0.85f;
                    tex.SetPixel(x, y, Gray(shade * camo[x, y]));
                }

            s = Make(tex, new Vector2(0.5f, 0.5f));
            bodies[pattern] = s;
            return s;
        }

        /// <summary>Plate carrier, magazine pouches, backpack and radio antenna. Untinted.</summary>
        public static Sprite Gear
        {
            get
            {
                if (gear != null) return gear;
                var tex = Blank(Size, Size);
                Color vest = new Color32(70, 74, 60, 255), vestDark = new Color32(52, 55, 45, 255), vestLight = new Color32(88, 92, 76, 255);
                Color pouch = new Color32(96, 88, 64, 255), pouchDark = new Color32(70, 64, 46, 255);
                Color pack = new Color32(58, 62, 50, 255), packDark = new Color32(42, 45, 36, 255);
                Color strap = new Color32(34, 36, 30, 255);

                // Vest over the chest and back.
                for (int y = 8; y <= 23; y++)
                    for (int x = 9; x <= 19; x++)
                    {
                        bool edge = x == 9 || x == 19 || y == 8 || y == 23;
                        bool molle = (y - 8) % 3 == 0 && x > 10 && x < 18;
                        tex.SetPixel(x, y, edge ? vestDark : molle ? vestLight : vest);
                    }
                // Shoulder straps.
                for (int x = 12; x <= 17; x++) { tex.SetPixel(x, 6, strap); tex.SetPixel(x, 7, strap); tex.SetPixel(x, 24, strap); tex.SetPixel(x, 25, strap); }
                // Three magazine pouches on the front.
                for (int p = 0; p < 3; p++)
                {
                    int y0 = 10 + p * 4;
                    for (int y = y0; y < y0 + 3; y++)
                        for (int x = 18; x <= 21; x++)
                            tex.SetPixel(x, y, x == 21 || y == y0 ? pouchDark : pouch);
                }
                // Backpack with a radio and antenna trailing back.
                for (int y = 11; y <= 20; y++)
                    for (int x = 4; x <= 8; x++)
                        tex.SetPixel(x, y, x == 4 || y == 11 || y == 20 ? packDark : pack);
                tex.SetPixel(6, 14, new Color32(30, 30, 30, 255)); tex.SetPixel(6, 15, new Color32(30, 30, 30, 255));
                for (int i = 0; i < 5; i++) tex.SetPixel(5 - i, 19 + i, strap);
                return gear = Make(tex, new Vector2(0.5f, 0.5f));
            }
        }

        /// <summary>Armband on the left upper arm. Tint = team colour, so teams read at a glance.</summary>
        public static Sprite Armband
        {
            get
            {
                if (armband != null) return armband;
                var tex = Blank(Size, Size);
                for (int y = 24; y <= 27; y++)
                    for (int x = 16; x <= 18; x++)
                        tex.SetPixel(x, y, Gray(y == 24 ? 0.75f : 1f));
                return armband = Make(tex, new Vector2(0.5f, 0.5f));
            }
        }

        /// <summary>Helmet seen from above, with cover seams and an NVG mount. Tint = team colour.</summary>
        public static Sprite Helmet
        {
            get
            {
                if (helmet != null) return helmet;
                var tex = Blank(Size, Size);
                Disc(tex, 15f, 16f, 6.1f, 0.95f, 0.5f);
                for (int y = 12; y <= 20; y++) tex.SetPixel(13, y, Gray(0.7f));   // cover seam
                for (int x = 10; x <= 19; x++) tex.SetPixel(x, 16, Gray(0.8f));   // top ridge
                // NVG shroud at the front (stays dark whatever the team colour).
                for (int y = 14; y <= 17; y++) { tex.SetPixel(20, y, Gray(0.3f)); tex.SetPixel(21, y, Gray(0.25f)); }
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
                Disc(tex, 15f, 16f, 6f, 0.95f, 0.5f);
                for (int y = 12; y <= 19; y++)
                    for (int x = 20; x <= 23; x++)
                        tex.SetPixel(x, y, Gray(x == 23 ? 0.55f : 0.72f));
                tex.SetPixel(15, 16, Gray(0.6f)); // button
                return cap = Make(tex, new Vector2(0.5f, 0.5f));
            }
        }

        /// <summary>Floppy boonie hat, wide brim. Tint = team colour.</summary>
        public static Sprite Boonie
        {
            get
            {
                if (boonie != null) return boonie;
                var tex = Blank(Size, Size);
                Disc(tex, 15f, 16f, 9f, 0.72f, 0.42f);
                Disc(tex, 15f, 16f, 5.5f, 0.95f, 0.8f);
                // Brim wobble.
                tex.SetPixel(7, 20, Color.clear); tex.SetPixel(23, 11, Color.clear); tex.SetPixel(19, 24, Color.clear);
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

        /// <summary>Goggles with a lens glint (untinted). Drawn over the head gear.</summary>
        public static Sprite Details
        {
            get
            {
                if (details != null) return details;
                var tex = Blank(Size, Size);
                var frame = new Color32(20, 20, 22, 255);
                for (int y = 11; y <= 20; y++) tex.SetPixel(20, y, frame);
                for (int y = 11; y <= 20; y++)
                {
                    bool bridge = y == 15 || y == 16;
                    tex.SetPixel(21, y, bridge ? (Color)frame : y < 15 ? (Color)new Color32(70, 100, 130, 255) : (Color)new Color32(60, 88, 118, 255));
                }
                tex.SetPixel(21, 12, new Color32(170, 205, 235, 255));
                tex.SetPixel(21, 18, new Color32(150, 190, 225, 255));
                return details = Make(tex, new Vector2(0.5f, 0.5f));
            }
        }

        /// <summary>One boot with sole and laces. Two of these shuffle under the body when walking.</summary>
        public static Sprite Foot
        {
            get
            {
                if (foot != null) return foot;
                var tex = Blank(8, 6);
                Color boot = new Color32(44, 36, 28, 255), toe = new Color32(60, 50, 38, 255), sole = new Color32(24, 20, 16, 255);
                for (int y = 0; y < 6; y++)
                    for (int x = 0; x < 8; x++)
                    {
                        bool corner = x == 7 && (y == 0 || y == 5);
                        if (corner) continue;
                        tex.SetPixel(x, y, y == 0 || y == 5 ? sole : x >= 5 ? toe : boot);
                    }
                tex.SetPixel(3, 2, new Color32(90, 80, 60, 255)); tex.SetPixel(3, 3, new Color32(90, 80, 60, 255));
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
                        float dx = (x + 0.5f - 16f) / 13f, dy = (y + 0.5f - 16f) / 13f;
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

        // Legend: k black, d dark grey, g grey, l light grey, m metal, b optic glass, w wood, t tan,
        //         o orange tip, n glove, r red dot, . empty.
        // Rows run top (the soldier's left) to bottom (right). The middle row is the barrel line. Guns point right.
        static readonly Dictionary<string, string[]> Designs = new Dictionary<string, string[]>
        {
            { "rifle", new[] {
                "kkk........ddddd.....nnnn",
                "kkkkkkkkkkgglllggkkkgggggggkk",
                "kddkkkkkkkkbbrbbkkkkkkkkkkkkkko",
                "kkkkkkkkkkgglllggkkkgggggggkk",
                "kkk......nnnkkk" } },
            { "rifle2", new[] {
                "ttt........ttttt.....nnnn",
                "tttttttttttgllltttttttttttkk",
                "tddtttttttkkbbrbkkktttttttkkkko",
                "tttttttttttgllltttttttttttkk",
                "ttt......nnntttk" } },
            { "smg", new[] {
                "......ddd....nnn",
                "kkkkkkgggkkkkkkkk",
                "kddkkkllrkkkkkkkkkko",
                "kkkkkkgggkkkkkkkk",
                "......nnnkk",
                "........kk" } },
            { "sniper", new[] {
                "www...........bbbbbbb.......nnn",
                "wwwwwwwwwwkkkklllllllkkkkkkkkkkkkk",
                "wwwwwwwwwwkkkkmmmmmmmkkkkkkkkkkkkkkkko",
                "wwwwwwwwwwkkkklllllllkkkkkkkkkkkkk",
                "www.........nnn..m" } },
            { "dmr", new[] {
                "kk...........bbbbb.....nnn",
                "kkkkkkkkkkkklllllkkkkkkkkkkkkk",
                "kddkkkkkkkkkmmmmmkkkkkkkkkkkkkkkko",
                "kkkkkkkkkkkklllllkkkkkkkkkkkkk",
                "kk.......nnnkk" } },
            { "shotgun", new[] {
                "www...................nnn",
                "wwwwwwwkkkkkkkkkkkkkkkkkkkk",
                "wwwwwwwkkkkmmkkkkwwwwwwkkkkkkko",
                "wwwwwwwkkkkkkkkkkwwwwwwkkkk",
                ".......nnn" } },
            { "burst", new[] {
                "dd.........ggg.....nnn",
                "ddddddddkkkggggkkkkkkkkkk",
                "dkkddddkkkkllrlkkkkkkkkkkkko",
                "ddddddddkkkggggkkkkkkkkkk",
                "dd......nnnkk" } },
            { "lmg", new[] {
                "kk........dddddd.........nnn",
                "kkkkkkkkkkkggggggkkkkkkkkkkkkkkk",
                "kddkkkkkkkklllrlkkkkkkkkkkkkkkkkko",
                "kkkkkkkkkkkggggggkkkkkkkkkkkkkkk",
                "kk.....nnnkttttttt",
                "...........ttttttt" } },
            { "pistol", new[] {
                "nnkkkkkkkk",
                "nnkgggggkko",
                "nnkkkkkkkk" } },
            { "revolver", new[] {
                "nn.mmm",
                "nnkmmmmmmmmo",
                "nn.mmm" } },
            { "mpistol", new[] {
                "nnkkkkkkkkk",
                "nnkggggggkkko",
                "nnkkkkkkkkk",
                "..kk",
                "..kk" } },
            { "knife", new[] {
                "nndddmmmmm",
                "nndddmmmmml" } },
        };

        public static GunArt Gun(WeaponData weapon)
        {
            string key = DesignFor(weapon);
            GunArt art;
            if (guns.TryGetValue(key, out art)) return art;

            string[] rows = Designs[key];
            int w = 0;
            foreach (var r in rows) w = Mathf.Max(w, r.Length);
            int h = rows.Length;
            // The barrel line is the row with the orange tip; the sprite pivots on it.
            int barrelRow = h / 2;
            for (int r = 0; r < h; r++) if (rows[r].IndexOf('o') >= 0) barrelRow = r;
            var tex = Blank(w, h);
            for (int r = 0; r < h; r++)
            {
                string row = rows[r];
                for (int x = 0; x < row.Length; x++)
                {
                    Color c;
                    if (TryColor(row[x], out c)) tex.SetPixel(x, h - 1 - r, c);
                }
            }

            bool handgun = weapon.weaponClass == WeaponClass.Pistol || weapon.IsMelee;
            // Long guns: stock end at the right shoulder. Handguns and knives: held out front.
            var anchor = handgun ? new Vector2(0.42f, -0.06f) : new Vector2(-0.02f, -0.1f);
            float pivotY = (h - 1 - barrelRow + 0.5f) / h;
            art = new GunArt
            {
                sprite = Make(tex, new Vector2(0f, pivotY)),
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
                case 'b': color = new Color(0.25f, 0.4f, 0.55f); return true;
                case 'r': color = new Color(0.95f, 0.15f, 0.1f); return true;
                case 'w': color = new Color(0.45f, 0.29f, 0.16f); return true;
                case 't': color = new Color(0.66f, 0.58f, 0.42f); return true;
                case 'o': color = new Color(1f, 0.45f, 0.05f); return true;
                case 'n': color = new Color(0.2f, 0.2f, 0.17f); return true;
                case 'h': color = new Color(0.93f, 0.74f, 0.58f); return true;
                default: color = Color.clear; return false;
            }
        }

        // ================================================================ helpers

        static void Arm(Texture2D tex, float[,] camo, Vector2 from, Vector2 to, float thickness)
        {
            int steps = 30;
            Vector2 dir = (to - from).normalized, side = new Vector2(-dir.y, dir.x);
            for (int i = 0; i <= steps; i++)
            {
                Vector2 p = Vector2.Lerp(from, to, i / (float)steps);
                for (float o = -thickness / 2f; o <= thickness / 2f; o += 0.5f)
                {
                    Vector2 q = p + side * o;
                    int x = Mathf.FloorToInt(q.x), y = Mathf.FloorToInt(q.y);
                    if (x < 0 || y < 0 || x >= Size || y >= Size) continue;
                    // Round the sleeve: darker at the edges, a highlight along the top.
                    float edge = Mathf.Abs(o) / (thickness / 2f);
                    float shade = edge > 0.8f ? 0.5f : o > 0f ? 0.95f : 0.78f;
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
                    for (int blob = 0; blob < 16; blob++)
                    {
                        int cx = rng.Next(Size), cy = rng.Next(Size), r = 2 + rng.Next(4);
                        float v = blob % 3 == 0 ? 0.5f : blob % 3 == 1 ? 0.68f : 0.82f;
                        for (int y = cy - r; y <= cy + r; y++)
                            for (int x = cx - r; x <= cx + r; x++)
                                if (x >= 0 && y >= 0 && x < Size && y < Size && (x - cx) * (x - cx) + (y - cy) * (y - cy) * 2 <= r * r + rng.Next(3))
                                    m[x, y] = v;
                    }
                    break;
                case CamoPattern.Digital:
                    for (int y = 0; y < Size; y += 2)
                        for (int x = 0; x < Size; x += 2)
                        {
                            int n = rng.Next(10);
                            float v = n < 3 ? 0.58f : n < 5 ? 0.76f : 1f;
                            for (int oy = 0; oy < 2; oy++) for (int ox = 0; ox < 2; ox++) m[x + ox, y + oy] = v;
                        }
                    break;
                case CamoPattern.Tiger:
                    for (int x = 0; x < Size; x++)
                    {
                        if ((x + rng.Next(3)) % 5 != 0) continue;
                        int wobble = 0;
                        for (int y = 0; y < Size; y++)
                        {
                            wobble = Mathf.Clamp(wobble + rng.Next(3) - 1, -1, 1);
                            int xx = Mathf.Clamp(x + wobble, 0, Size - 1);
                            if (rng.Next(10) < 8) { m[xx, y] = 0.48f; if (xx + 1 < Size) m[xx + 1, y] = 0.6f; }
                        }
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
                    // Light from the upper left: a soft highlight and darker lower right.
                    float light = Mathf.Clamp01(0.5f + ((y + 0.5f - cy) - (x + 0.5f - cx)) / (2f * r));
                    float shade = d > r - 1.2f ? rim : fill * Mathf.Lerp(0.78f, 1.05f, light);
                    tex.SetPixel(x, y, Gray(Mathf.Min(1f, shade)));
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
