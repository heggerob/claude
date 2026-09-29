// Plain-C# tests for Odin's Coin logic that doesn't need a running Unity scene.
using System;
using System.Collections.Generic;
using UnityEngine;
using OdinsCoin;

public static class LogicTests
{
    static int failures, passes;

    static void Check(bool ok, string what)
    {
        if (ok) passes++;
        else { failures++; Console.WriteLine("FAIL: " + what); }
    }

    public static int Main()
    {
        WavesTests();
        Console.WriteLine(passes + " passed, " + failures + " failed");
        return failures == 0 ? 0 : 1;
    }

    static void WavesTests()
    {
        float max = Waves.MaxHeight * 1.2f;
        var rng = new System.Random(3);
        for (int i = 0; i < 2000; i++)
        {
            float x = (float)(rng.NextDouble() * 400 - 200), z = (float)(rng.NextDouble() * 400 - 200), t = (float)(rng.NextDouble() * 100);
            float h = Waves.Height(x, z, t);
            Check(!float.IsNaN(h) && Math.Abs(h) <= max, "wave height bounded at " + x + "," + z);
        }
        Check(Math.Abs(Waves.Height(3f, 4f, 10f) - Waves.Height(3f, 4f, 10f)) < 1e-6f, "waves are deterministic");
        Check(Math.Abs(Waves.Height(0f, 0f, 0f) - Waves.Height(0f, 0f, 1f)) > 1e-4f, "waves move over time");
    }
}
