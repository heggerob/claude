using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The colours a character is painted in, by slot. Every outfit has a default palette; players (and skins)
    /// can change any slot. Garments only ever use these slots, never fixed colours.
    /// </summary>
    public class Palette
    {
        /// <summary>The warm peach of the concept art's faces (the first of the <see cref="SkinTones"/>).</summary>
        public Color skin = SkinTones.Peach;
        public Color ink = new Color(0.08f, 0.065f, 0.055f);
        public Color hair = new Color(0.72f, 0.26f, 0.12f);
        /// <summary>Main cloth (tunic, skirt).</summary>
        public Color cloth = new Color(0.27f, 0.26f, 0.26f);
        public Color clothDark = new Color(0.17f, 0.16f, 0.16f);
        /// <summary>Secondary cloth (aprons, sashes, linings).</summary>
        public Color cloth2 = new Color(0.62f, 0.47f, 0.2f);
        /// <summary>Accent cloth (banners, capes, scarves).</summary>
        public Color accent = new Color(0.62f, 0.17f, 0.12f);
        /// <summary>Embroidery and symbols on the accent cloth.</summary>
        public Color emblem = new Color(0.9f, 0.84f, 0.7f);
        public Color fur = new Color(0.86f, 0.77f, 0.6f);
        public Color furShadow = new Color(0.56f, 0.43f, 0.3f);
        public Color leather = new Color(0.46f, 0.3f, 0.17f);
        public Color leatherDark = new Color(0.23f, 0.15f, 0.1f);
        /// <summary>Buckles, rings, trims.</summary>
        public Color brass = new Color(0.76f, 0.57f, 0.25f);
        public Color metal = new Color(0.45f, 0.47f, 0.5f);
        /// <summary>Parchment, bone, rope: pale natural things.</summary>
        public Color parchment = new Color(0.86f, 0.76f, 0.56f);

        public Palette Copy() { return (Palette)MemberwiseClone(); }
    }

    /// <summary>The skin tones a hero can have, chosen on the hero screen apart from the clothes' colours.</summary>
    public static class SkinTones
    {
        public static readonly Color Peach = new Color(0.97f, 0.78f, 0.62f);
        public static readonly string[] Names = { "Peach", "Fair", "Rosy", "Olive", "Tan", "Brown", "Deep" };
        static readonly Color[] colors = {
            Peach, new Color(0.98f, 0.86f, 0.76f), new Color(0.96f, 0.74f, 0.66f), new Color(0.84f, 0.7f, 0.52f),
            new Color(0.85f, 0.62f, 0.44f), new Color(0.64f, 0.44f, 0.3f), new Color(0.44f, 0.3f, 0.21f) };

        public static int Count { get { return colors.Length; } }

        /// <summary>The tone's colour; any index outside the list gives the first (peach).</summary>
        public static Color Get(int tone) { return tone >= 0 && tone < colors.Length ? colors[tone] : colors[0]; }

        public static string Name(int tone) { return tone >= 0 && tone < Names.Length ? Names[tone] : Names[0]; }

        public static int Clamp(int tone) { return tone >= 0 && tone < colors.Length ? tone : 0; }

        /// <summary>
        /// Paint the tone onto a palette, unless its skin slot was changed on purpose (a draugr's grave-pale skin, say):
        /// those special colour sets keep their own skin.
        /// </summary>
        public static void Apply(Palette p, int tone)
        {
            var c = p.skin;
            if (Mathf.Abs(c.r - Peach.r) < 0.002f && Mathf.Abs(c.g - Peach.g) < 0.002f && Mathf.Abs(c.b - Peach.b) < 0.002f) p.skin = Get(tone);
        }
    }
}
