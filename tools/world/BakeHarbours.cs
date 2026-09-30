// Works out every real place's harbour the thorough way (Places.FloodHarbour: water joined to the open sea) and
// writes them to OdinsCoin/Assets/Resources/World/harbours.txt as "name|lat|lon", for the game to load.
using System;
using System.IO;
using System.Globalization;
using UnityEngine;
using OdinsCoin;

public static class BakeHarbours
{
    public static int Main(string[] args)
    {
        var map = WorldMap.FromBytes(File.ReadAllBytes(args[0]));
        map.Detail = WorldDetail.FromBytes(File.ReadAllBytes(Path.Combine(Path.GetDirectoryName(args[0]), "coast.bytes")));
        Places.LoadHarbours(null);
        var ic = CultureInfo.InvariantCulture;
        using (var w = new StreamWriter(args[1]))
            foreach (var p in Places.All)
            {
                Vector3 h;
                if (!Places.FloodHarbour(map, p, out h)) { Console.WriteLine(p.name + ": no harbour joined to the sea within " + p.harbourReach + " km"); continue; }
                var ll = map.ToLatLon(h);
                float km = Vector3.Distance(h, Places.Position(map, p)) / 1000f;
                Console.WriteLine(string.Format(ic, "{0}: {1:0.00} km off, {2:0.0} m deep", p.name, km, Places.Depth(map, h)));
                w.WriteLine(string.Format(ic, "{0}|{1:R}|{2:R}", p.name, ll.x, ll.y));
            }
        return 0;
    }
}
