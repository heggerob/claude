// Draws the land at close range (the real map plus the made-up detail) as a shaded relief picture, seen from
// above, for docs/coast-*.png. Output: width, height, then RGB bytes.
using System;
using System.IO;
using UnityEngine;
using OdinsCoin;

public static class CoastPreview
{
    public static int Main(string[] args)
    {
        var map = WorldMap.FromBytes(File.ReadAllBytes(args[0]));
        var coast = Path.Combine(Path.GetDirectoryName(args[0]), "coast.bytes");
        if (File.Exists(coast)) map.Detail = WorldDetail.FromBytes(File.ReadAllBytes(coast));
        float lat = float.Parse(args[1], System.Globalization.CultureInfo.InvariantCulture);
        float lon = float.Parse(args[2], System.Globalization.CultureInfo.InvariantCulture);
        float km = float.Parse(args[3], System.Globalization.CultureInfo.InvariantCulture);
        int n = int.Parse(args[4]);
        var c = map.ToWorld(lat, lon);
        float step = km * 1000f / n;
        var h = new float[n, n];
        for (int j = 0; j < n; j++)
            for (int i = 0; i < n; i++)
                h[i, j] = TerrainDetail.Height(map, c.x + (i - n / 2) * step, c.z + (n / 2 - j) * step);
        using (var w = new BinaryWriter(File.Create(args[5])))
        {
            w.Write(n); w.Write(n);
            var sun = new Vector3(-0.6f, 0.7f, 0.4f).normalized;
            for (int j = 0; j < n; j++)
                for (int i = 0; i < n; i++)
                {
                    float hx = h[Math.Min(i + 1, n - 1), j] - h[Math.Max(i - 1, 0), j];
                    float hz = h[i, Math.Max(j - 1, 0)] - h[i, Math.Min(j + 1, n - 1)];
                    var nrm = new Vector3(-hx / (2f * step), 1f, -hz / (2f * step)).normalized;
                    float light = Mathf.Clamp01(Vector3.Dot(nrm, sun) * 0.8f + 0.35f);
                    float slope = Mathf.Sqrt(hx * hx + hz * hz) / (2f * step);
                    var g = TerrainDetail.Kind(h[i, j], slope);
                    Color col = TerrainPatch.ColourOf(g);
                    if (g == Ground.Seabed) { float d = Mathf.Clamp01(-h[i, j] / 400f); col = Color.Lerp(new Color(0.55f, 0.75f, 0.85f), new Color(0.12f, 0.3f, 0.45f), d); light = 1f; }
                    w.Write((byte)(Mathf.Clamp01(col.r * light) * 255)); w.Write((byte)(Mathf.Clamp01(col.g * light) * 255)); w.Write((byte)(Mathf.Clamp01(col.b * light) * 255));
                }
        }
        return 0;
    }
}
