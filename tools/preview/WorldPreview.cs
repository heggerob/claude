// Renders the sea and every island top-down (1 px = 1 m) so the world layout can be checked without Unity.
using System;
using System.IO;
using UnityEngine;
using OdinsCoin;

public static class WorldPreview
{
    public static void Main(string[] args)
    {
        int size = 800; float half = size / 2f;
        using (var f = new BinaryWriter(File.Create(args[0])))
        {
            f.Write(size); f.Write(size);
            for (int py = size - 1; py >= 0; py--)
                for (int px = 0; px < size; px++)
                {
                    float x = px - half + 0f, z = py - half + 200f;
                    float h = -10f;
                    foreach (var s in WorldGen.Specs) h = Math.Max(h, Island.Height(s, x, z));
                    h = Math.Max(h, Island.Height(HomeHarbour.Spec, x, z));
                    Color c;
                    if (h < 0f) c = Color.Lerp(new Color(0.08f, 0.2f, 0.3f), new Color(0.2f, 0.45f, 0.55f), Mathf.Clamp01((h + 4f) / 4f));
                    else if (h < Island.SandLevel) c = new Color(0.86f, 0.78f, 0.56f);
                    else c = Color.Lerp(new Color(0.36f, 0.56f, 0.26f), new Color(0.6f, 0.6f, 0.58f), Mathf.Clamp01((h - 6f) / 16f));
                    // The home jetty (brown) and the ship's berth (red).
                    float jettyEnd = HomeHarbour.JettyStart + HomeHarbour.JettyPlanks * 1.6f;
                    if (Math.Abs(x) <= 2f && z >= HomeHarbour.JettyStart && z <= jettyEnd) c = new Color(0.5f, 0.34f, 0.2f);
                    Vector3 berth = HomeHarbour.ShipStart;
                    if (Math.Abs(x - berth.x) < 2.5f && Math.Abs(z - berth.z) < 9f) c = new Color(0.9f, 0.2f, 0.15f);
                    f.Write((byte)(c.r * 255)); f.Write((byte)(c.g * 255)); f.Write((byte)(c.b * 255)); f.Write((byte)255);
                }
        }
    }
}
