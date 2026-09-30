// Works out what to draw over a top-down picture of a real place: its buildings (rotated footprints), the jetty,
// the harbour, and where the chests and guards stand. First line: the picture's centre (lat lon); then one
// line per mark in pixels. Used by tools/world/coast.sh for docs/place-*.png.
using System;
using System.IO;
using System.Globalization;
using UnityEngine;
using OdinsCoin;

public static class PlacePreview
{
    public static int Main(string[] args)
    {
        var map = WorldMap.FromBytes(File.ReadAllBytes(args[0]));
        var coast = Path.Combine(Path.GetDirectoryName(args[0]), "coast.bytes");
        if (File.Exists(coast)) map.Detail = WorldDetail.FromBytes(File.ReadAllBytes(coast));
        var place = Places.Find(args[1]);
        float km = float.Parse(args[2], CultureInfo.InvariantCulture);
        int n = int.Parse(args[3]);
        var plots = Settlements.Layout(map, place);
        Vector3 harbour;
        Places.Harbour(map, place, out harbour);
        // Centre on the settlement.
        var centre = plots.Count > 1 ? plots[PlotIndex(plots)].at : harbour;
        var ll = map.ToLatLon(centre);
        float step = km * 1000f / n;
        Func<Vector3, Vector2> px = p => new Vector2(n / 2 + (p.x - centre.x) / step, n / 2 - (p.z - centre.z) / step);
        var ic = CultureInfo.InvariantCulture;
        using (var w = new StreamWriter(args[4]))
        {
            w.WriteLine(ll.x.ToString(ic) + " " + ll.y.ToString(ic));
            foreach (var plot in plots)
            {
                var f = Buildings.Footprint(plot.kind);
                var c = plot.at;
                if (plot.kind == BuildingKind.Jetty) { c = plot.at + Quaternion.Euler(0f, plot.yaw, 0f) * new Vector3(0f, 0f, 20f); f = new Vector3(2f, 0f, 20f); }
                var p = px(c);
                w.WriteLine(string.Format(ic, "plot {0} {1} {2} {3} {4} {5}", plot.kind, p.x, p.y, f.x / step, f.z / step, plot.yaw));
            }
            var hp = px(harbour);
            w.WriteLine(string.Format(ic, "harbour {0} {1}", hp.x, hp.y));
            var plunder = PlaceLife.PlunderOf(place.kind);
            if (plunder.chests > 0)
            {
                var main = PlaceLife.MainBuilding(plots);
                foreach (var s in PlaceLife.Stations(map, plots, main, plunder.chests, 3f, 11)) { var q = px(s); w.WriteLine(string.Format(ic, "chest {0} {1}", q.x, q.y)); }
                foreach (var s in PlaceLife.Stations(map, plots, main, plunder.guards, 8f, 12)) { var q = px(s); w.WriteLine(string.Format(ic, "guard {0} {1}", q.x, q.y)); }
            }
            if (place.name == RealWorld.HomePlace)
            {
                Vector3 home;
                RealWorld.FindHomeWater(map, place, out home);
                var q = px(home);
                w.WriteLine(string.Format(ic, "home {0} {1} {2}", q.x, q.y, RealWorld.HomeWater / step));
            }
        }
        return 0;
    }

    static int PlotIndex(System.Collections.Generic.List<Plot> plots) { return plots.Count > 1 ? 1 : 0; }
}
