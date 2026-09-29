// Renders every MapDefinition top-down to an image so layouts can be checked without Unity.
using System;
using System.IO;
using UnityEngine;
using AirsoftArena;

public static class MapPreview
{
    const int PxPerMetre = 12;

    public static void RenderAll(string dir)
    {
        foreach (var map in MapLibrary.All) Render(map, Path.Combine(dir, "map_" + map.id + ".rgba"));
    }

    static void Render(MapDefinition map, string path)
    {
        var b = map.bounds;
        float margin = 2f;
        int W = (int)((b.width + margin * 2) * PxPerMetre), H = (int)((b.height + margin * 2) * PxPerMetre);
        var canvas = new Color[W * H];
        Func<Vector2, Vector2> toPx = p => new Vector2((p.x - b.xMin + margin) * PxPerMetre, (p.y - b.yMin + margin) * PxPerMetre);

        var ground = map.ground == GroundStyle.Concrete ? SpriteFactory.Concrete : map.ground == GroundStyle.ForestFloor ? SpriteFactory.ForestFloor : SpriteFactory.Grass;
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                var t = ground.texture;
                // 16 texture px per metre, sampled at canvas resolution.
                int tx = (int)(x * 16f / PxPerMetre) % t.width, ty = (int)(y * 16f / PxPerMetre) % t.height;
                canvas[y * W + x] = t.GetPixel(tx, ty);
            }

        Rect[] zones = map.spawnZones;
        FillRect(canvas, W, H, toPx(zones[0].min), zones[0].size * PxPerMetre, new Color(0.3f, 0.55f, 1f, 0.3f));
        FillRect(canvas, W, H, toPx(zones[1].min), zones[1].size * PxPerMetre, new Color(1f, 0.33f, 0.28f, 0.3f));

        foreach (var p in map.pieces)
        {
            var c = toPx(p.center);
            switch (p.kind)
            {
                case PieceKind.Tree:
                    Stamp(canvas, W, H, SpriteFactory.Canopy, c, 3f * PxPerMetre, new Color(1, 1, 1, 0.92f));
                    break;
                case PieceKind.Bush:
                    Stamp(canvas, W, H, SpriteFactory.Bush, c, Mathf.Max(p.size.x, p.size.y) * PxPerMetre, Color.white);
                    break;
                case PieceKind.Rock:
                    Stamp(canvas, W, H, SpriteFactory.Rock, c, Mathf.Max(p.size.x, p.size.y) * PxPerMetre, Color.white);
                    break;
                default:
                    Sprite s; Color tint;
                    TileFor(p.kind, map.indoor, out s, out tint);
                    Tile(canvas, W, H, s, c - p.size * PxPerMetre / 2f, p.size * PxPerMetre, tint);
                    break;
            }
        }

        // Objectives: flags as team-coloured squares, hill as a ring.
        FillRect(canvas, W, H, toPx(map.flagPoints[0]) - Vector2.one * 5, Vector2.one * 10, new Color(0.3f, 0.55f, 1f, 1f));
        FillRect(canvas, W, H, toPx(map.flagPoints[1]) - Vector2.one * 5, Vector2.one * 10, new Color(1f, 0.33f, 0.28f, 1f));
        var hc = toPx(map.hill);
        float hr = map.hillRadius * PxPerMetre;
        for (int a = 0; a < 720; a++)
        {
            float ang = a / 720f * Mathf.PI * 2f;
            int x = (int)(hc.x + Mathf.Cos(ang) * hr), y = (int)(hc.y + Mathf.Sin(ang) * hr);
            if (x >= 0 && y >= 0 && x < W && y < H) canvas[y * W + x] = new Color(1f, 0.85f, 0.3f, 1f);
        }

        Write(path, canvas, W, H);
    }

    static void TileFor(PieceKind kind, bool indoor, out Sprite s, out Color tint)
    {
        switch (kind)
        {
            case PieceKind.Container: s = SpriteFactory.Crate; tint = new Color(0.36f, 0.45f, 0.5f); break;
            case PieceKind.Sandbags: s = SpriteFactory.Sandbag; tint = new Color(0.78f, 0.7f, 0.5f); break;
            case PieceKind.Shelf: s = SpriteFactory.Shelf; tint = Color.white; break;
            case PieceKind.Crates: s = SpriteFactory.Crate; tint = new Color(0.72f, 0.55f, 0.36f); break;
            case PieceKind.Log: s = SpriteFactory.Log; tint = Color.white; break;
            default: s = SpriteFactory.Crate; tint = indoor ? new Color(0.62f, 0.6f, 0.56f) : new Color(0.55f, 0.42f, 0.3f); break;
        }
    }


    static void Blend(Color[] canvas, int W, int H, int x, int y, Color c)
    {
        if (x < 0 || y < 0 || x >= W || y >= H || c.a <= 0f) return;
        var d = canvas[y * W + x];
        canvas[y * W + x] = new Color(d.r + (c.r - d.r) * c.a, d.g + (c.g - d.g) * c.a, d.b + (c.b - d.b) * c.a, 1f);
    }

    static void FillRect(Color[] canvas, int W, int H, Vector2 pos, Vector2 size, Color c)
    {
        for (int y = (int)pos.y; y < (int)(pos.y + size.y); y++)
            for (int x = (int)pos.x; x < (int)(pos.x + size.x); x++) Blend(canvas, W, H, x, y, c);
    }

    static void Tile(Color[] canvas, int W, int H, Sprite s, Vector2 pos, Vector2 size, Color tint)
    {
        var t = s.texture;
        for (int y = 0; y < (int)size.y; y++)
            for (int x = 0; x < (int)size.x; x++)
            {
                var c = t.GetPixel((int)(x * 16f / PxPerMetre) % t.width, (int)(y * 16f / PxPerMetre) % t.height);
                Blend(canvas, W, H, (int)pos.x + x, (int)pos.y + y, new Color(c.r * tint.r, c.g * tint.g, c.b * tint.b, c.a * tint.a));
            }
    }

    static void Stamp(Color[] canvas, int W, int H, Sprite s, Vector2 center, float sizePx, Color tint)
    {
        var t = s.texture;
        int n = (int)sizePx;
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                var c = t.GetPixel(x * t.width / n, y * t.height / n);
                Blend(canvas, W, H, (int)(center.x - n / 2f) + x, (int)(center.y - n / 2f) + y, new Color(c.r * tint.r, c.g * tint.g, c.b * tint.b, c.a * tint.a));
            }
    }

    static void Write(string path, Color[] canvas, int W, int H)
    {
        using (var f = new BinaryWriter(File.Create(path)))
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
}
