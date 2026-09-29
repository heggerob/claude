using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The storybook look: soft wrapped light with cool shadows, and pencil hatching laid over the shadowed side,
    /// first single strokes, then cross-hatching in the darkest parts. The maths here is the same as in
    /// <c>Resources/Shaders/InkToon.shader</c> so the preview pictures match the game; change both together.
    /// </summary>
    public static class InkStyle
    {
        public const string ShaderName = "OdinsCoin/InkToon";
        /// <summary>Screen pixels between hatching strokes.</summary>
        public const float Spacing = 5f;
        /// <summary>How dark one layer of strokes is.</summary>
        public const float StrokeDark = 0.3f;
        /// <summary>How strongly the paper grain shows through everything.</summary>
        public const float PaperAmount = 0.5f;

        /// <summary>Paper grain multiplier at a screen pixel (the paper texture, 2 screen pixels per texel).</summary>
        public static float Paper(float sx, float sy)
        {
            float g = DrawnTextures.Sample(DrawnTextures.Get(SurfaceKind.Paper), sx / 256f, sy / 256f);
            return Mathf.Lerp(1f, g, PaperAmount);
        }

        /// <summary>Light from 0 (facing away) to 1 (facing the sun), wrapped so the terminator is soft.</summary>
        public static float Tone(Vector3 normal, Vector3 toSun)
        {
            return Mathf.Clamp01((Vector3.Dot(normal, toSun) + 0.35f) / 1.35f);
        }

        /// <summary>Colour multiplier for a tone: 0.55 in full shadow up to 1.1 in full sun.</summary>
        public static float Shade(float tone) { return 0.55f + 0.55f * tone; }

        /// <summary>
        /// Hatching multiplier at a screen pixel: 1 where lit, pencil strokes (↗) below tone 0.55, crossed (↖)
        /// below 0.3. The strokes wobble a little, like a hand drawing them.
        /// </summary>
        public static float Hatch(float tone, float sx, float sy, float spacing = Spacing)
        {
            float k = 1f;
            float wobble = 0.8f * Mathf.Sin(sy * 0.21f) + 0.5f * Mathf.Sin(sx * 0.13f + 1.7f);
            float c1 = Mathf.Clamp01((0.55f - tone) / 0.4f);
            if (c1 > 0f) k *= 1f - StrokeDark * Line((sx + sy + wobble) / spacing, c1 * 0.32f, spacing);
            float c2 = Mathf.Clamp01((0.3f - tone) / 0.3f);
            if (c2 > 0f) k *= 1f - StrokeDark * Line((sx - sy - wobble) / spacing + 0.5f, c2 * 0.28f, spacing);
            return k;
        }

        /// <summary>
        /// How much hatching a surface gets: none on skin, so the faces stay clean and round like the concept
        /// art, a lighter touch on polished metal.
        /// </summary>
        public static float HatchAmount(SurfaceKind surface)
        {
            switch (surface)
            {
                case SurfaceKind.Skin: return 0f;
                case SurfaceKind.Metal: return 0.6f;
                default: return 1f;
            }
        }

        /// <summary>1 on a stroke, 0 between strokes, one pixel of soft edge. <paramref name="halfWidth"/> in periods.</summary>
        static float Line(float t, float halfWidth, float spacing)
        {
            float f = t - Mathf.Floor(t);
            float dist = Mathf.Abs(f - 0.5f) * spacing;
            return Mathf.Clamp01(halfWidth * spacing - dist + 0.5f);
        }

        /// <summary>Tells the ink shader where the sun is and how the hatching is drawn. Call once the sun is set up.</summary>
        public static void SetSun(Light sun)
        {
            if (sun == null) return;
            Vector3 d = -sun.transform.forward;
            Shader.SetGlobalVector("_InkSunDir", new Vector4(d.x, d.y, d.z, 0f));
            Shader.SetGlobalFloat("_InkHatchSpacing", Spacing);
            Shader.SetGlobalFloat("_InkStrokeDark", StrokeDark);
            Shader.SetGlobalTexture("_InkPaper", DrawnTextures.Texture(SurfaceKind.Paper));
            Shader.SetGlobalFloat("_InkPaperAmount", PaperAmount);
        }
    }
}
