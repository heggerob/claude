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
        ShipTests();
        CoinTests();
        Console.WriteLine(passes + " passed, " + failures + " failed");
        return failures == 0 ? 0 : 1;
    }

    static void CoinTests()
    {
        var f = new Fortune { Gold = 100 };
        Check(Math.Abs(f.HeadsChance - 0.5f) < 1e-6f, "fair coin to start");
        var win = f.Flip(50, 0.1f, 0f);
        Check(win.heads && f.Gold == 150 && win.payout == 100, "heads pays the wager back double");
        Check(win.fate.card.kind == FateKind.Blessing && win.fate.tier == 2, "heads gives a tier 2 blessing for 50 gold");
        var lose = f.Flip(100, 0.9f, 0.3f);
        Check(!lose.heads && f.Gold == 50 && lose.fate.card.kind == FateKind.Curse, "tails loses the wager and curses");
        Check(f.Flip(500, 0.9f, 0f).wager == 50, "can't wager more gold than you have");
        Check(f.Gold == 0, "all-in and lost");
        Check(Fortune.Tier(0) == 1 && Fortune.Tier(49) == 1 && Fortune.Tier(50) == 2 && Fortune.Tier(200) == 3, "wager tiers");

        // Same fate again refreshes instead of stacking.
        var g = new Fortune();
        g.Add(Fates.Blessings[0], 1, 10f);
        g.Add(Fates.Blessings[0], 3, 5f);
        Check(g.Active.Count == 1 && g.Active[0].tier == 3 && g.Active[0].remaining == 10f, "refresh keeps the best tier and time");
        g.Tick(11f);
        Check(g.Active.Count == 0, "fates expire");

        // Effects the rest of the game reads.
        var h = new Fortune();
        Check(h.MeleeDamageMultiplier == 1f && h.ShipThrustMultiplier == 1f && h.LootMultiplier == 1f, "neutral without fates");
        h.Add(Fates.Blessings[0], 3, 10f); // Thor
        h.Add(Fates.Curses[1], 1, 10f);    // Rán
        Check(h.MeleeDamageMultiplier > 1.5f, "Thor's Wrath tier 3 hits much harder");
        Check(h.ShipThrustMultiplier < 1f, "Rán's Net slows the ship");
        h.Add(Fates.Blessings[4], 1, 10f); // Odin's Favour
        Check(h.HeadsChance > 0.5f, "Odin's Favour improves the odds");
        h.RuneBonus = 1f;
        Check(h.HeadsChance <= Fortune.MaxHeadsChance, "odds are capped");

        // Over many free-rolling flips the coin is fair and the gold adds up.
        var rng = new System.Random(9);
        var k = new Fortune { Gold = 100000 };
        int start = k.Gold, heads = 0;
        long wagered = 0, paid = 0;
        for (int i = 0; i < 20000; i++)
        {
            k.Active.Clear(); // measure the bare coin (Odin's Favour would otherwise tilt later flips)
            var r = k.Flip(25, (float)rng.NextDouble(), (float)rng.NextDouble());
            wagered += r.wager; paid += r.payout;
            if (r.heads) heads++;
        }
        Check(k.Gold == start - wagered + paid, "gold adds up over 20000 flips");
        Check(Math.Abs(heads / 20000f - 0.5f) < 0.02f, "about half heads: " + heads);
        foreach (var c in Fates.Blessings) Check(c.kind == FateKind.Blessing && c.baseDuration > 0f, c.id + " is a blessing with a duration");
        foreach (var c in Fates.Curses) Check(c.kind == FateKind.Curse && c.baseDuration > 0f, c.id + " is a curse with a duration");
    }

    static void ShipTests()
    {
        // At rest every float point is RestDraft deep: buoyancy must carry the whole ship.
        float lift = ShipTuning.Buoyancy(ShipTuning.RestDraft, 0f) * ShipTuning.BuoyancyPoints;
        Check(Math.Abs(lift - ShipTuning.Mass * 9.81f) < 1f, "ship floats at rest draft: " + lift);
        Check(ShipTuning.Buoyancy(-0.1f, 0f) == 0f, "no buoyancy out of the water");
        Check(ShipTuning.Buoyancy(0.5f, 3f) < ShipTuning.Buoyancy(0.5f, 0f), "rising fast is damped");
        Check(ShipTuning.Buoyancy(0.5f, -3f) > ShipTuning.Buoyancy(0.5f, 0f), "sinking fast pushes back harder");

        var north = Vector3.forward;
        Check(Math.Abs(ShipTuning.SailEfficiency(north, north) - 1f) < 0.001f, "tailwind is full efficiency");
        Check(Math.Abs(ShipTuning.SailEfficiency(north, Vector3.right) - 0.5f) < 0.001f, "beam wind is half");
        Check(ShipTuning.SailEfficiency(north, Vector3.back) <= 0.1f, "headwind is nearly useless");

        // Top speed under full sail, tailwind, strongest wind: thrust = forward drag.
        float vmax = (float)Math.Sqrt(ShipTuning.MaxSailThrust / ShipTuning.ForwardDrag);
        float knots = ShipTuning.MetresPerSecondToKnots(vmax);
        Check(knots > 10f && knots < 20f, "top speed is a believable longship speed: " + knots + " kn");

        // Hull shape: widest amidships, rises at the ends.
        float hw0, k0, g0, hw1, k1, g1;
        LongshipBuilder.Station(0f, out hw0, out k0, out g0);
        LongshipBuilder.Station(0.95f, out hw1, out k1, out g1);
        Check(hw0 > hw1 && g1 > g0 && k0 < k1, "hull is widest and deepest amidships, stem rises");
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
