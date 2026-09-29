// Renders every MapDefinition top-down to images so layouts and ground detail can be checked without Unity.
// map_<id>.png is the whole field at 12 px/m; map_<id>_detail.png is a 1:1 (32 px/m) crop of the middle.
using System;
using System.IO;
using UnityEngine;
using AirsoftArena;

public static class MapPreview
{
    public static void RenderAll(string dir)
    {
        foreach (var map in MapLibrary.All)
        {
            var b = map.bounds;
            Render(map, new Rect(b.xMin - 2f, b.yMin - 2f, b.width + 4f, b.height + 4f), 12, Path.Combine(dir, "map_" + map.id + ".rgba"));
            Vector2 c = new Vector2(map.spawnZones[0].xMax + 12f, 0f);
            if (map.trainingOnly) continue;
            Render(map, new Rect(c.x - 14f, c.y - 8f, 28f, 16f), 32, Path.Combine(dir, "map_" + map.id + "_detail.rgba"));
        }
    }

    static void Render(MapDefinition map, Rect view, int ppm, string path)
    {
        int W = (int)(view.width * ppm), H = (int)(view.height * ppm);
        var ground = GroundPainter.Paint(map, view.xMin, view.yMin, W, H, ppm);
        var canvas = new Color[W * H];
        for (int i = 0; i < canvas.Length; i++) canvas[i] = ground[i];
        Func<Vector2, Vector2> toPx = p => new Vector2((p.x - view.xMin) * ppm, (p.y - view.yMin) * ppm);

        FillRect(canvas, W, H, toPx(map.spawnZones[0].min), map.spawnZones[0].size * ppm, new Color(0.3f, 0.55f, 1f, 0.22f));
        FillRect(canvas, W, H, toPx(map.spawnZones[1].min), map.spawnZones[1].size * ppm, new Color(1f, 0.33f, 0.28f, 0.22f));

        // Shadows first, then pieces, then tree crowns on top.
        foreach (var p in map.pieces)
        {
            if (p.kind == PieceKind.Bush || p.kind == PieceKind.Pallet || p.kind == PieceKind.Tree) continue;
            float len = 0.3f * ppm;
            FillRect(canvas, W, H, toPx(p.center - p.size / 2f) + new Vector2(len, -len), p.size * ppm, new Color(0f, 0f, 0f, 0.3f));
        }
        foreach (var p in map.pieces)
        {
            var c = toPx(p.center);
            float size = Mathf.Max(p.size.x, p.size.y) * ppm;
            switch (p.kind)
            {
                case PieceKind.Tree: break;
                case PieceKind.Bush: Stamp(canvas, W, H, SpriteFactory.Bush, c, size, Color.white); break;
                case PieceKind.Rock: Stamp(canvas, W, H, SpriteFactory.Rock, c, size, Color.white); break;
                case PieceKind.Barrel: Stamp(canvas, W, H, MapArt.Barrel, c, size, new Color(0.3f, 0.45f, 0.75f)); break;
                case PieceKind.Tires: Stamp(canvas, W, H, MapArt.Tires, c, size, Color.white); break;
                default:
                    Sprite s; Color tint;
                    TileFor(p.kind, map.indoor, out s, out tint);
                    Tile(canvas, W, H, s, c - p.size * ppm / 2f, p.size * ppm, tint, ppm, p.size.y > p.size.x);
                    break;
            }
        }
        foreach (var p in map.pieces)
            if (p.kind == PieceKind.Tree) Stamp(canvas, W, H, SpriteFactory.Canopy, toPx(p.center), 3f * ppm, new Color(1, 1, 1, 0.92f));

        if (ppm < 20)
        {
            FillRect(canvas, W, H, toPx(map.flagPoints[0]) - Vector2.one * 5, Vector2.one * 10, new Color(0.3f, 0.55f, 1f, 1f));
            FillRect(canvas, W, H, toPx(map.flagPoints[1]) - Vector2.one * 5, Vector2.one * 10, new Color(1f, 0.33f, 0.28f, 1f));
            var hc = toPx(map.hill);
            float hr = map.hillRadius * ppm;
            for (int a = 0; a < 720; a++)
            {
                float ang = a / 720f * Mathf.PI * 2f;
                Blend(canvas, W, H, (int)(hc.x + Mathf.Cos(ang) * hr), (int)(hc.y + Mathf.Sin(ang) * hr), new Color(1f, 0.85f, 0.3f, 1f));
            }
        }
        Write(path, canvas, W, H);
    }

    static void TileFor(PieceKind kind, bool indoor, out Sprite s, out Color tint)
    {
        tint = Color.white;
        switch (kind)
        {
            case PieceKind.Container: s = MapArt.Container; tint = new Color(0.42f, 0.55f, 0.62f); break;
            case PieceKind.Sandbags: s = MapArt.Sandbags; break;
            case PieceKind.Shelf: s = MapArt.Shelf; break;
            case PieceKind.Crates: s = MapArt.Crates; break;
            case PieceKind.Log: s = MapArt.Log; break;
            case PieceKind.HayBale: s = MapArt.Hay; break;
            case PieceKind.Fence: s = MapArt.Fence; break;
            case PieceKind.Netting: s = MapArt.Netting; break;
            case PieceKind.Pallet: s = MapArt.Pallet; break;
            default: s = indoor ? MapArt.IndoorWall : MapArt.Plywood; break;
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

    // Tiles are 32 px per metre; vertical pieces are drawn rotated like in the game.
    static void Tile(Color[] canvas, int W, int H, Sprite s, Vector2 pos, Vector2 size, Color tint, int ppm, bool vertical)
    {
        var t = s.texture;
        for (int y = 0; y < (int)size.y; y++)
            for (int x = 0; x < (int)size.x; x++)
            {
                int u = vertical ? y : x, v = vertical ? (int)size.x - 1 - x : y;
                var c = t.GetPixel((int)(u * 32f / ppm) % t.width, (int)(v * 32f / ppm) % t.height);
                Blend(canvas, W, H, (int)pos.x + x, (int)pos.y + y, new Color(c.r * tint.r, c.g * tint.g, c.b * tint.b, c.a * tint.a));
            }
    }

    static void Stamp(Color[] canvas, int W, int H, Sprite s, Vector2 center, float sizePx, Color tint)
    {
        var t = s.texture;
        int n = Math.Max(1, (int)sizePx);
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
