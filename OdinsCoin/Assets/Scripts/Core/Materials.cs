using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Creates simple coloured materials that work in URP (Lit) and the built-in pipeline (Standard).
    /// Everything in the prototype is flat-shaded low-poly, so colour + smoothness is all we need.
    /// </summary>
    public static class Materials
    {
        static readonly Dictionary<string, Material> cache = new Dictionary<string, Material>();
        static Shader lit;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { cache.Clear(); lit = null; ink = null; inkLooked = false; }

        static Shader Lit
        {
            get
            {
                if (lit != null) return lit;
                lit = Shader.Find("Universal Render Pipeline/Lit");
                if (lit == null) lit = Shader.Find("Standard");
                if (lit == null) lit = Shader.Find("Diffuse");
                return lit;
            }
        }

        /// <summary>
        /// Draw solid things in the storybook style (<see cref="InkStyle"/>): hatched shadows, drawn textures. Shiny
        /// things (the sea) keep the lit shader. Falls back to lit everywhere if the ink shader isn't available.
        /// </summary>
        public static bool Storybook = true;

        static Shader ink;
        static bool inkLooked;

        static Shader Ink
        {
            get
            {
                if (inkLooked) return ink;
                inkLooked = true;
                ink = Shader.Find(InkStyle.ShaderName);
                if (ink != null && !ink.isSupported) ink = null;
                return ink;
            }
        }

        public static Material Get(Color color, float smoothness = 0.1f)
        {
            string key = ColorUtility.ToHtmlStringRGBA(color) + smoothness.ToString("0.00");
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(Storybook && smoothness < 0.5f && Ink != null ? Ink : Lit);
            m.color = color;
            if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", color);
            if (m.HasProperty("_Smoothness")) m.SetFloat("_Smoothness", smoothness);
            if (m.HasProperty("_Glossiness")) m.SetFloat("_Glossiness", smoothness);
            cache[key] = m;
            return m;
        }

        /// <summary>
        /// A coloured material with the hand-drawn texture of <paramref name="surface"/> on it (pencil strokes,
        /// weave, fur strands...). Plain surfaces get the flat material.
        /// </summary>
        public static Material GetDrawn(Color color, SurfaceKind surface, float smoothness = 0.1f)
        {
            if (surface == SurfaceKind.Plain) return Get(color, smoothness);
            string key = ColorUtility.ToHtmlStringRGBA(color) + smoothness.ToString("0.00") + surface;
            Material m;
            if (cache.TryGetValue(key, out m) && m != null) return m;
            m = new Material(Get(color, smoothness));
            var tex = DrawnTextures.Texture(surface);
            m.mainTexture = tex;
            if (m.HasProperty("_BaseMap")) m.SetTexture("_BaseMap", tex);
            if (m.HasProperty("_Hatch")) m.SetFloat("_Hatch", InkStyle.HatchAmount(surface));
            if (m.HasProperty("_Flat")) m.SetFloat("_Flat", InkStyle.Flatness(surface));
            cache[key] = m;
            return m;
        }

        // Palette.
        public static readonly Color SeaDeep = new Color(0.06f, 0.24f, 0.33f);
        public static readonly Color Wood = new Color(0.45f, 0.29f, 0.16f);
        public static readonly Color DarkWood = new Color(0.3f, 0.19f, 0.11f);
        public static readonly Color Sail = new Color(0.85f, 0.2f, 0.15f);
        public static readonly Color SailStripe = new Color(0.93f, 0.88f, 0.76f);
        public static readonly Color Sand = new Color(0.86f, 0.78f, 0.56f);
        public static readonly Color Grass = new Color(0.36f, 0.56f, 0.26f);
        public static readonly Color Rock = new Color(0.47f, 0.47f, 0.45f);
        public static readonly Color Gold = new Color(1f, 0.78f, 0.25f);
    }
}
