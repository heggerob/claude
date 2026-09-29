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
        IslandTests();
        HarbourTests();
        RuneTests();
        HallTests();
        CombatTests();
        Console.WriteLine(passes + " passed, " + failures + " failed");
        return failures == 0 ? 0 : 1;
    }

    static void CombatTests()
    {
        var o = Vector3.zero;
        Check(CombatMath.InArc(o, Vector3.forward, new Vector3(0f, 0f, 1.5f), 2f, 90f), "target straight ahead is hit");
        Check(!CombatMath.InArc(o, Vector3.forward, new Vector3(0f, 0f, 2.5f), 2f, 90f), "out of reach is missed");
        Check(!CombatMath.InArc(o, Vector3.forward, new Vector3(0f, 0f, -1f), 2f, 90f), "behind you is missed");
        Check(CombatMath.InArc(o, Vector3.forward, new Vector3(0.9f, 0.5f, 1f), 2f, 100f), "height doesn't matter for the arc");
        Check(Math.Abs(CombatMath.Damage(20f, 1.5f, false, true) - 30f) < 0.01f, "blessing multiplies damage");
        Check(CombatMath.Damage(20f, 1f, true, true) < 4f, "shield blocks most damage from the front");
        Check(Math.Abs(CombatMath.Damage(20f, 1f, true, false) - 20f) < 0.01f, "shield doesn't help against a hit from behind");
        Check(CombatMath.FromFront(o, Vector3.forward, new Vector3(0f, 0f, 3f)), "attacker in front counts as front");
        Check(!CombatMath.FromFront(o, Vector3.forward, new Vector3(0f, 0f, -3f)), "attacker behind isn't front");

        // How many swings to drop a guard, with and without Thor.
        int plain = (int)Math.Ceiling(60f / VikingCombat.SwingDamage);
        int thor = (int)Math.Ceiling(60f / (VikingCombat.SwingDamage * 1.7f));
        Check(plain == 3 && thor == 2, "a guard takes 3 swings, 2 with Thor's Wrath tier 3 (" + plain + ", " + thor + ")");
    }

    static void IslandTests()
    {
        var all = new List<IslandSpec>(WorldGen.Specs);
        all.Add(HomeHarbour.Spec);
        var start = new Vector2(HomeHarbour.ShipStart.x, HomeHarbour.ShipStart.z);
        foreach (var spec in all)
        {
            Check(Island.Height(spec, spec.centre.x, spec.centre.y) > 3f, spec.name + " rises out of the sea in the middle");
            Check(Island.Height(spec, spec.centre.x + spec.radius * 1.4f, spec.centre.y) < 0f, spec.name + " is sea well past its radius");
            if (spec != HomeHarbour.Spec)
                Check(Vector2.Distance(spec.centre, start) > spec.radius * 1.3f + 12f, spec.name + " leaves room for the ship at the start");
            foreach (var other in all)
                if (other != spec) Check(Vector2.Distance(spec.centre, other.centre) > (spec.radius + other.radius) * 1.25f, spec.name + " doesn't overlap " + other.name);
        }
    }

    static void HallTests()
    {
        // The hall and Bjørn stand on dry, fairly level ground, clear of trees, and apart from Gunnar.
        var home = HomeHarbour.Spec;
        var k = HomeHarbour.KeeperSpot;
        var hall = HomeHarbour.HallPosition;
        float hk = Island.Height(home, k.x, k.y), hh = Island.Height(home, hall.x, hall.y);
        Check(hk > Island.SandLevel && hh > Island.SandLevel, "hall and keeper are on dry land (" + hk + ", " + hh + ")");
        Check(Math.Abs(hk - hh) < 0.3f, "Bjørn stands on the hall's terrace (" + (hk - hh) + ")");
        // The hall's corners sit on the level terrace too, so it doesn't float or sink into the hill.
        foreach (var c in new[] { new Vector2(-8f, -8f), new Vector2(8f, -8f), new Vector2(-8f, 8f), new Vector2(8f, 8f) })
        {
            Vector3 w = Quaternion.Euler(0f, HomeHarbour.HallYaw, 0f) * new Vector3(c.x, 0f, c.y);
            float hc = Island.Height(home, hall.x + w.x, hall.y + w.z);
            Check(Math.Abs(hc - hh) < 0.5f, "hall corner " + c + " is level (" + (hc - hh) + ")");
        }
        Check(k.y > hall.y, "Bjørn stands on the jetty side of the hall");
        Check(home.InClearing(k) && home.InClearing(hall) && home.InClearing(new Vector2(0f, HomeHarbour.JettyStart)), "trees keep clear of the hall and jetty");
        Check(Vector2.Distance(k, new Vector2(HomeHarbour.TraderPosition.x, HomeHarbour.TraderPosition.z)) > HomeHarbour.KeeperRange + HomeHarbour.TradeRange, "Bjørn and Gunnar don't share a prompt");
        foreach (var other in WorldGen.Specs) Check(!other.InClearing(other.centre), other.name + " has no clearings");

        // Upgrades.
        var f = new Fortune { Gold = 10000 };
        var up = new Upgrades();
        Check(up.SailMultiplier == 1f && up.OarMultiplier == 1f && up.HealthBonus == 0f && up.AxeMultiplier == 1f, "no upgrades: no bonuses");
        int before = f.Gold;
        Check(up.Buy(UpgradeKind.Sail, f) && f.Gold == before - 150 && Math.Abs(up.SailMultiplier - 1.1f) < 0.001f, "first sail upgrade: 150 gold, +10%");
        while (up.Buy(UpgradeKind.Sail, f)) { }
        Check(up.Maxed(UpgradeKind.Sail) && up.NextCost(UpgradeKind.Sail) == -1 && Math.Abs(up.SailMultiplier - 1.3f) < 0.001f, "sail maxes out at +30%");
        var poor = new Fortune { Gold = 50 };
        Check(!up.Buy(UpgradeKind.Axe, poor) && poor.Gold == 50 && up.Level(UpgradeKind.Axe) == 0, "can't buy what you can't afford");
        foreach (var d in Upgrades.All)
        {
            Check(d.levels.Length == d.costs.Length, d.name + " has a name for every level");
            for (int i = 1; i < d.costs.Length; i++) Check(d.costs[i] > d.costs[i - 1], d.name + " gets pricier per level");
        }
        var leaky = new Fortune();
        leaky.Add(Fates.Curses[1], 3, 60f); // Rán's Net, tier 3: -50%
        var hull = new Upgrades();
        Check(Math.Abs(hull.LeakMultiplier(leaky) - 0.5f) < 0.001f, "Rán's Net tier 3 halves the speed");
        hull.Buy(UpgradeKind.Hull, f); hull.Buy(UpgradeKind.Hull, f);
        Check(Math.Abs(hull.LeakMultiplier(leaky) - 0.85f) < 0.001f, "oak strakes shrug off 70% of it (" + hull.LeakMultiplier(leaky) + ")");
        Check(Math.Abs(hull.LeakMultiplier(new Fortune()) - 1f) < 0.001f, "no curse, no slowdown");

        // Dice.
        Check(MeadDice.Rank(new[] { 1, 1, 1 }) > MeadDice.Rank(new[] { 6, 6, 5 }), "any triple beats any total");
        Check(MeadDice.Rank(new[] { 2, 2, 2 }) < MeadDice.Rank(new[] { 3, 3, 3 }), "higher triple wins");
        Check(MeadDice.Compare(new[] { 6, 4, 1 }, new[] { 5, 5, 1 }) == 0, "equal totals tie");
        Check(MeadDice.Compare(new[] { 6, 4, 2 }, new[] { 5, 5, 1 }) == 1 && MeadDice.Compare(new[] { 1, 2, 3 }, new[] { 2, 2, 3 }) == -1, "higher total wins");
        Check(MeadDice.Lowest(new[] { 4, 1, 6 }) == 1, "lowest die found");
        // Enumerate every pair of hands: the game is exactly even.
        int wins = 0, losses = 0, ties = 0;
        var all = new List<int[]>();
        for (int a = 1; a <= 6; a++) for (int b = 1; b <= 6; b++) for (int c = 1; c <= 6; c++) all.Add(new[] { a, b, c });
        foreach (var x in all) foreach (var y in all) { int o = MeadDice.Compare(x, y); if (o > 0) wins++; else if (o < 0) losses++; else ties++; }
        Check(wins == losses && wins + losses + ties == 216 * 216, "dice: you win exactly as often as Bjørn (" + wins + " / " + losses + " / " + ties + ")");
        // With Odin's Favour, rerolling the lowest die when not ahead helps.
        var rng = new System.Random(5);
        int favWins = 0, favLosses = 0;
        for (int i = 0; i < 20000; i++)
        {
            var me = MeadDice.Roll(rng); var him = MeadDice.Roll(rng);
            if (MeadDice.Compare(me, him) <= 0) me[MeadDice.Lowest(me)] = rng.Next(1, 7);
            int o = MeadDice.Compare(me, him);
            if (o > 0) favWins++; else if (o < 0) favLosses++;
        }
        Check(favWins > favLosses, "Odin's Favour tilts the dice your way (" + favWins + " vs " + favLosses + ")");
        var d10 = new Fortune { Gold = 100 };
        Check(MeadDice.Settle(d10, 25, 1) == 25 && d10.Gold == 125 && d10.DiceWon == 1, "a win pays the stake");
        Check(MeadDice.Settle(d10, 25, -1) == -25 && d10.Gold == 100 && d10.DiceLost == 1, "a loss costs the stake");
        Check(MeadDice.Settle(d10, 25, 0) == 0 && d10.Gold == 100, "a tie costs nothing");
    }

    static void RuneTests()
    {
        var f = new Fortune { Gold = 1000 };
        Check(Math.Abs(f.ExpectedReturn - 1f) < 0.001f, "the bare coin is fair: 1 gold back per gold wagered on average");
        var ansuz = Runes.Find("ansuz");
        var hail = Runes.Find("hagalaz");
        var fehu = Runes.Find("fehu");
        Check(f.Carve(ansuz) && f.Gold == 1000 - ansuz.cost, "carving a rune costs its gold");
        Check(!f.Carve(ansuz), "the same rune can't be carved twice");
        Check(Math.Abs(f.HeadsChance - 0.56f) < 0.001f, "Ansuz: +6% (" + f.HeadsChance + ")");
        Check(f.Carve(hail) && Math.Abs(f.HeadsChance - 0.46f) < 0.001f && Math.Abs(f.PayoutMultiplier - 2.75f) < 0.001f, "Hagalaz: -10% but 2.75x");
        Check(f.Carve(fehu) && Math.Abs(f.PayoutMultiplier - 3f) < 0.001f, "Fehu adds 0.25x");
        Check(f.Carved.Count == Runes.Slots && !f.CanCarve(Runes.Find("algiz")), "only " + Runes.Slots + " runes fit on the rim");
        Check(f.GrindOff(hail) && f.CanCarve(Runes.Find("algiz")), "grinding a rune off frees its slot");
        // No single rune turns the coin into a gold mine; even the greediest set stays a gamble.
        foreach (var r in Runes.All)
        {
            var g = new Fortune { Gold = 1000 };
            g.Carve(r);
            Check(g.ExpectedReturn <= 1.15f, r.name + " alone keeps the coin near fair (" + g.ExpectedReturn + ")");
        }
        var greedy = new Fortune { Gold = 5000 };
        greedy.Carve(ansuz); greedy.Carve(fehu); greedy.Carve(hail);
        Check(greedy.ExpectedReturn < 1.45f && greedy.HeadsChance < 0.5f, "Ansuz+Fehu+Hagalaz: pays well, but you lose more often than you win");

        // Algiz shortens curses, Thurisaz lengthens blessings.
        var w = new Fortune { Gold = 1000 };
        w.Carve(Runes.Find("algiz"));
        var cursed = w.Flip(0, 0.99f, 0f);
        Check(!cursed.heads && Math.Abs(cursed.fate.remaining - cursed.fate.card.baseDuration * 0.7f) < 0.01f, "Algiz: curses 30% shorter");
        var t = new Fortune { Gold = 1000 };
        t.Carve(Runes.Find("thurisaz"));
        var blessed = t.Flip(0, 0f, 0f);
        Check(blessed.heads && Math.Abs(blessed.fate.remaining - blessed.fate.card.baseDuration * 1.3f) < 0.01f, "Thurisaz: blessings 30% longer");

        // Odin's favour and the ravens.
        var fav = new Fortune();
        fav.AddFavour(0.5f);
        Check(!fav.CanCallRavens && !fav.SpendFavour() && Math.Abs(fav.Favour - 0.5f) < 0.001f, "half favour can't call the ravens");
        fav.AddFavour(0.7f);
        Check(fav.CanCallRavens && fav.Favour <= 1f, "favour fills up to 1");
        Check(fav.SpendFavour() && fav.Favour == 0f && !fav.CanCallRavens, "calling the ravens empties the favour");
        var raid = new Fortune { Gold = 1000 };
        raid.Carve(Runes.Find("raidho"));
        raid.AddFavour(0.2f);
        Check(Math.Abs(raid.Favour - 0.3f) < 0.001f, "Raidho: favour fills 50% faster");
        int kills = (int)Math.Ceiling(1f / Ravens.FavourPerKill);
        Check(kills >= 8 && kills <= 16, "a raid of " + kills + " Saxons fills the favour");
        // Muninn: the next flip is Odin's eye even with the worst roll, then the coin is back to normal.
        var mun = new Fortune { Gold = 100 };
        mun.NextFlipBlessed = true;
        var sure = mun.Flip(50, 0.9999f, 0.5f);
        Check(sure.heads && mun.Gold == 150 && !mun.NextFlipBlessed, "Muninn's flip is Odin's eye and pays out");
        Check(!mun.Flip(0, 0.9999f, 0.5f).heads, "only one flip is blessed");

        // Huginn picks the nearest treasures.
        var pts = new List<Vector3> { new Vector3(100f, 0f, 0f), new Vector3(10f, 0f, 0f), new Vector3(-30f, 0f, 0f), new Vector3(0f, 0f, 400f), new Vector3(5f, 0f, 5f) };
        var near = Ravens.Nearest(pts, Vector3.zero, 3);
        Check(near.Count == 3 && near[0] == 4 && near[1] == 1 && near[2] == 2, "Huginn finds the three nearest chests in order");
        Check(Ravens.Nearest(pts, Vector3.zero, 10).Count == pts.Count, "never more targets than chests");
    }

    static void HarbourTests()
    {
        var home = HomeHarbour.Spec;
        Vector3 s = HomeHarbour.ShipStart;
        // The berth is deep enough for the keel even in a wave trough, along the whole hull.
        for (float t = -1f; t <= 1f; t += 0.1f)
        {
            float hw, keel, g;
            LongshipBuilder.Station(t, out hw, out keel, out g);
            float z = s.z + t * LongshipBuilder.Length / 2f;
            foreach (float side in new[] { -1f, 0f, 1f })
            {
                float bottom = Island.Height(home, s.x + side * hw, z);
                Check(bottom < s.y + keel - Waves.MaxHeight, "berth is deep enough at station " + t.ToString("0.0") + " side " + side + " (" + bottom + ")");
            }
        }
        // The jetty starts on the beach and runs out over the water, clear of the hull.
        float last = HomeHarbour.JettyStart + (HomeHarbour.JettyPlanks - 1) * 1.6f;
        float beach = Island.Height(home, 0f, HomeHarbour.JettyStart);
        Check(beach > 0f && beach < HomeHarbour.JettyTop + 0.35f, "jetty starts on the beach (" + beach + ")");
        Check(Island.Height(home, 0f, last) < -2f, "jetty end is over deep water");
        Check(last > s.z, "jetty reaches past the middle of the ship");
        Check(s.x - LongshipBuilder.Beam / 2f > 1.7f + 0.1f, "moored ship clears the jetty");
        // Gunnar can trade over the gunwale: the deck edge nearest him is within reach.
        var deckEdge = new Vector3(s.x - (LongshipBuilder.Beam / 2f - 0.5f), s.y + LongshipBuilder.DeckHeight, HomeHarbour.TraderPosition.z);
        Check(Vector3.Distance(deckEdge, HomeHarbour.TraderPosition) < HomeHarbour.TradeRange, "Gunnar is in reach from the deck");
        Check(Vector3.Distance(s, HomeHarbour.TraderPosition) < HomeHarbour.CargoRange, "a moored ship counts for selling cargo");

        // What chests are worth under blessings and curses.
        var f = new Fortune();
        Check(TreasureChest.Worth(100, f) == 100, "a plain chest is worth its gold");
        f.Add(Fates.Blessings[2], 3, 60f);
        Check(TreasureChest.Worth(100, f) == 170, "Freya's Gift tier 3: +70% (" + TreasureChest.Worth(100, f) + ")");
        var c = new Fortune();
        c.Add(Fates.Curses[2], 1, 60f);
        Check(TreasureChest.Worth(100, c) == 75, "Fenrir's Hunger tier 1: -25% (" + TreasureChest.Worth(100, c) + ")");
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
