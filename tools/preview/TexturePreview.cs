using System;
using System.IO;
using OdinsCoin;
using UnityEngine;

/// <summary>Writes every hand-drawn surface texture, tiled 2×2 and tinted with a typical colour, for docs/textures.png.</summary>
public static class TexturePreview
{
    public static void Main(string[] args)
    {
        var kinds = (SurfaceKind[])Enum.GetValues(typeof(SurfaceKind));
        var pal = new Palette();
        Color[] tint = { new Color(0.8f, 0.8f, 0.8f), pal.cloth2, pal.fur, pal.leather, pal.metal, new Color(0.45f, 0.29f, 0.16f), pal.skin, pal.parchment, Materials.Grass, Materials.Sand, Materials.Rock };
        int n = DrawnTextures.Size * 2, cells = kinds.Length;
        using (var w = new BinaryWriter(File.Create(args[0])))
        {
            w.Write(n * cells); w.Write(n);
            for (int y = n - 1; y >= 0; y--)
                for (int k = 0; k < cells; k++)
                {
                    var t = DrawnTextures.Get(kinds[k]);
                    for (int x = 0; x < n; x++)
                    {
                        float v = t[(y % DrawnTextures.Size) * DrawnTextures.Size + x % DrawnTextures.Size];
                        Color c = tint[k];
                        w.Write((byte)(Mathf.Clamp01(c.r * v) * 255)); w.Write((byte)(Mathf.Clamp01(c.g * v) * 255)); w.Write((byte)(Mathf.Clamp01(c.b * v) * 255)); w.Write((byte)255);
                    }
                }
        }
        File.WriteAllLines(args[1], Array.ConvertAll(kinds, k => k.ToString()));
    }
}
