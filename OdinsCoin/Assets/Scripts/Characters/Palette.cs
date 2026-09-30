using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The colours a character is painted in, by slot. Every outfit has a default palette; players (and skins)
    /// can change any slot. Garments only ever use these slots, never fixed colours.
    /// </summary>
    public class Palette
    {
        public Color skin = new Color(0.97f, 0.87f, 0.74f);
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
}
