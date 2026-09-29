// Renders the procedural pixel art into a contact sheet (tools/preview/render.sh), so it can be
// looked at without opening Unity. Uses the functional fake UnityEngine in tools/unity-stub.
using System;
using System.IO;
using UnityEngine;
using AirsoftArena;

public static class SpritePreview
{
    const int Scale = 6;
    static int W = 1100, H = 760;
    static Color[] canvas;

    public static void Main(string[] args)
    {
        canvas = new Color[W * H];
        for (int i = 0; i < canvas.Length; i++) canvas[i] = new Color(0.29f, 0.44f, 0.23f, 1f);

        // Soldiers: every camo x head gear, alternating teams, each holding a different primary.
        var primaries = WeaponCatalog.Primaries;
        int n = 0;
        foreach (CamoPattern camo in Enum.GetValues(typeof(CamoPattern)))
        {
            foreach (HeadGearStyle hg in Enum.GetValues(typeof(HeadGearStyle)))
            {
                var look = new SoldierLook { camo = camo, headGear = hg, uniform = SoldierLook.UniformColors[n % SoldierLook.UniformColors.Length] };
                var team = n % 2 == 0 ? Team.Blue : Team.Red;
                float cx = 70 + (n % 6) * 170, cy = 90 + (n / 6) * 150;
                DrawSoldier(look, team, primaries[n % primaries.Count], cx, cy);
                n++;
            }
        }

        // Every weapon on its own.
        int g = 0;
        var all = new System.Collections.Generic.List<WeaponData>();
        all.AddRange(WeaponCatalog.Primaries); all.AddRange(WeaponCatalog.Secondaries); all.AddRange(WeaponCatalog.MeleeWeapons);
        foreach (var w in all)
        {
            var art = PixelArt.Gun(w);
            float x = 30 + (g % 4) * 240, y = H - (400 + (g / 4) * 70);
            Blit(art.sprite, Color.white, x, y);
            g++;
        }

        // Tiles.
        Blit(SpriteFactory.Grass, Color.white, 1000, H - 420);
        Blit(SpriteFactory.Crate, new Color(0.55f, 0.42f, 0.3f), 1000, H - 540);
        Blit(SpriteFactory.Sandbag, new Color(0.78f, 0.7f, 0.5f), 1000, H - 660);

        string outPath = args.Length > 0 ? args[0] : "preview.rgba";
        using (var f = new BinaryWriter(File.Create(outPath)))
        {
            f.Write(W); f.Write(H);
            for (int y = H - 1; y >= 0; y--)
                for (int x = 0; x < W; x++)
                {
                    var c = canvas[y * W + x];
                    f.Write((byte)(Mathf.Clamp01(c.r) * 255)); f.Write((byte)(Mathf.Clamp01(c.g) * 255));
                    f.Write((byte)(Mathf.Clamp01(c.b) * 255)); f.Write((byte)255);
                }
        }
    }

    static void DrawSoldier(SoldierLook look, Team team, WeaponData weapon, float cx, float cy)
    {
        float m = SpriteFactory.PixelsPerUnit * Scale; // canvas px per metre
        cy = H - cy;
        Blit(PixelArt.Shadow, Color.white, cx + 0.05f * m, cy - 0.08f * m);
        Blit(PixelArt.Foot, Color.white, cx + 0.1f * m, cy + 0.16f * m);
        Blit(PixelArt.Foot, Color.white, cx - 0.1f * m, cy - 0.16f * m);
        Blit(PixelArt.Body(look.camo), look.uniform, cx, cy);
        var art = PixelArt.Gun(weapon);
        Blit(art.sprite, Color.white, cx + art.anchor.x * m, cy + art.anchor.y * m);
        Blit(PixelArt.HeadGear(look.headGear), Teams.Color(team), cx, cy);
        Blit(PixelArt.Details, Color.white, cx, cy);
    }

    // Draws a sprite with its pivot at canvas (px, py), y up.
    static void Blit(Sprite s, Color tint, float px, float py)
    {
        var t = s.texture;
        float ox = px - s.pivot.x * t.width * Scale, oy = py - s.pivot.y * t.height * Scale;
        for (int y = 0; y < t.height; y++)
            for (int x = 0; x < t.width; x++)
            {
                var c = t.GetPixel(x, y);
                c = new Color(c.r * tint.r, c.g * tint.g, c.b * tint.b, c.a * tint.a);
                if (c.a <= 0f) continue;
                for (int sy = 0; sy < Scale; sy++)
                    for (int sx = 0; sx < Scale; sx++)
                    {
                        int X = (int)ox + x * Scale + sx, Y = (int)oy + y * Scale + sy;
                        if (X < 0 || Y < 0 || X >= W || Y >= H) continue;
                        var d = canvas[Y * W + X];
                        canvas[Y * W + X] = new Color(d.r + (c.r - d.r) * c.a, d.g + (c.g - d.g) * c.a, d.b + (c.b - d.b) * c.a, 1f);
                    }
            }
    }
}
