// Paints the in-game sea chart (SeaChart.Paint) with the real places marked, for docs/chart.png.
// Output: width, height, then RGB bytes (north at the top), then one line per place: name x y.
using System;
using System.IO;
using UnityEngine;
using OdinsCoin;

public static class ChartPreview
{
    public static int Main(string[] args)
    {
        var map = WorldMap.FromBytes(File.ReadAllBytes(args[0]));
        int w = int.Parse(args[1]), h = Mathf.RoundToInt(w * (map.Height / (float)map.Width));
        var px = SeaChart.Paint(map, w, h);
        using (var o = new BinaryWriter(File.Create(args[2])))
        {
            o.Write(w); o.Write(h);
            for (int y = h - 1; y >= 0; y--)
                for (int x = 0; x < w; x++)
                {
                    var c = px[y * w + x];
                    o.Write((byte)(Mathf.Clamp01(c.r) * 255f)); o.Write((byte)(Mathf.Clamp01(c.g) * 255f)); o.Write((byte)(Mathf.Clamp01(c.b) * 255f));
                }
        }
        var sheet = new Rect(0f, 0f, w, h);
        using (var t = new StreamWriter(args[3]))
            foreach (var p in Places.All)
            {
                var at = Places.Position(map, p);
                var s = SeaChart.ToScreen(map.Bounds, sheet, at.x, at.z);
                t.WriteLine(p.name + "|" + (PlaceLife.HasMarket(p) ? "market" : p.kind.ToString().ToLower()) + "|" + s.x.ToString(System.Globalization.CultureInfo.InvariantCulture) + "|" + s.y.ToString(System.Globalization.CultureInfo.InvariantCulture));
            }
        return 0;
    }
}
