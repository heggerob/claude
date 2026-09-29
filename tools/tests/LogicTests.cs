// Plain-C# tests for game logic that does not need a running Unity scene.
// Run with tools/tests/run.sh. Uses the functional fake UnityEngine in tools/unity-stub.
using System;
using System.Collections.Generic;
using UnityEngine;
using AirsoftArena;

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
        Cosmetics();
        Weapons();
        ShopTests();
        Maps();
        ProgressionTests();
        Sounds();
        Console.WriteLine(passes + " passed, " + failures + " failed");
        return failures == 0 ? 0 : 1;
    }

    static void Cosmetics()
    {
        var p = new PlayerProfile();
        var gold = CosmeticCatalog.Get("uni_gold");
        var woodland = CosmeticCatalog.Get("camo_woodland");
        Check(gold != null && woodland != null, "catalogue has gold uniform and woodland camo");
        Check(p.Owns(woodland), "starter items are owned");
        Check(!p.Owns(gold), "legendary not owned at start");
        Check(!p.Equip(gold), "cannot equip what you don't own");
        p.Unlock(gold);
        Check(p.Equip(gold), "can equip after unlocking");
        Check(p.equippedUniform == "uni_gold", "equipped id saved");
        var look = p.Look;
        Check(Math.Abs(look.uniform.r - gold.color.r) < 0.001f, "look uses equipped uniform colour");
        p.Unlock(gold);
        Check(p.ownedItems.Count == 1, "unlocking twice doesn't duplicate");

        var ids = new HashSet<string>();
        foreach (var c in CosmeticCatalog.All) Check(ids.Add(c.id), "unique cosmetic id " + c.id);
        foreach (CosmeticSlot slot in Enum.GetValues(typeof(CosmeticSlot)))
            Check(CosmeticCatalog.Get(CosmeticCatalog.DefaultId(slot)).starter, "default for " + slot + " is a starter item");
        var fallback = CosmeticCatalog.BuildLook("nope", null, "bad", "");
        Check(fallback.headGear == HeadGearStyle.Helmet, "unknown ids fall back to defaults");
    }

    static void ShopTests()
    {
        var p = new PlayerProfile();
        p.money = 1000;
        var lmg = WeaponCatalog.Get("07-TH6");
        Check(!p.OwnsWeapon(lmg), "LMG locked at start");
        Check(Shop.Buy(p, lmg) == PurchaseResult.RankTooLow, "LMG needs a higher rank");
        p.xp = Progression.XpForLevel(lmg.requiredRank);
        Check(p.OwnsWeapon(WeaponCatalog.Get("01-VK4")), "starter rifle owned");
        Check(Shop.Buy(p, lmg) == PurchaseResult.Ok && p.money == 1000 - lmg.price, "buying LMG deducts price");
        Check(Shop.Buy(p, lmg) == PurchaseResult.AlreadyOwned, "can't buy twice");
        p.primaryWeapon = "07-TH6";
        Check(p.Primary == lmg, "owned primary is used");
        p.primaryWeapon = "03-LB2";
        Check(p.Primary.price == 0, "locked primary falls back to a free one");
        p.money = 10;
        Check(Shop.Buy(p, CosmeticCatalog.Get("uni_gold")) == PurchaseResult.NotEnoughMoney, "not enough money");
        Check(Shop.OpenCrate(p, Shop.Crates[0]) == null && p.money == 10, "can't open crate without money");

        // Rarity table edges.
        var odds = Shop.Crates[0].odds;
        Check(Shop.RollRarity(odds, 0f) == Rarity.Common, "roll 0 -> common");
        Check(Shop.RollRarity(odds, 0.999999f) == Rarity.Legendary, "roll ~1 -> legendary");
        Check(Shop.RollRarity(Shop.Crates[1].odds, 0f) == Rarity.Rare, "operator crate never gives common");
        foreach (var c in Shop.Crates)
        {
            float sum = 0f; foreach (var o in c.odds) sum += o;
            Check(Math.Abs(sum - 1f) < 0.001f, c.name + " odds sum to 1");
        }

        // Open lots of crates: every item eventually unlocks, duplicates refund, money is consistent.
        var hist = new int[4];
        p = new PlayerProfile();
        p.money = 1000000;
        int spent = 0, refunded = 0;
        for (int i = 0; i < 4000; i++)
        {
            var r = Shop.OpenCrate(p, Shop.Crates[0]);
            spent += r.crate.price; refunded += r.refund;
            hist[(int)r.item.rarity]++;
            Check(r.duplicate == (r.refund > 0), "refund only on duplicates");
        }
        Check(p.money == 1000000 - spent + refunded, "money adds up after 4000 crates");
        Check(hist[0] > hist[1] && hist[1] > hist[2] && hist[2] > hist[3] && hist[3] > 0, "rarity histogram is ordered: " + string.Join(",", hist));
        foreach (var c in CosmeticCatalog.All) Check(p.Owns(c), "crates can unlock " + c.id);
    }

    static void Sounds()
    {
        foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
        {
            var data = SfxSynth.Generate(id);
            float peak = 0f;
            bool finite = true;
            foreach (var v in data) { peak = Math.Max(peak, Math.Abs(v)); if (float.IsNaN(v) || float.IsInfinity(v)) finite = false; }
            Check(data.Length > 50, id + " has samples");
            Check(finite, id + " has no NaN/inf");
            Check(peak <= 1f && peak > 0.05f, id + " peak in range: " + peak);
            Check(data.Length < SfxSynth.SampleRate * 2, id + " shorter than 2 s");
        }
    }

    static void ProgressionTests()
    {
        Check(Progression.LevelForXp(0) == 1, "0 XP is rank 1");
        Check(Progression.LevelForXp(249) == 1 && Progression.LevelForXp(250) == 2, "rank 2 at 250 XP");
        Check(Progression.LevelForXp(750) == 3, "rank 3 at 750 XP");
        Check(Progression.LevelForXp(int.MaxValue / 2) == Progression.MaxLevel, "rank is capped");
        for (int l = 2; l <= Progression.MaxLevel; l++) Check(Progression.XpForLevel(l) > Progression.XpForLevel(l - 1), "XP thresholds increase at " + l);

        var p = new PlayerProfile();
        int money = p.money;
        var lines = new List<string>();
        int gained = Progression.AddXp(p, 800, lines);
        Check(gained == 2 && p.Level == 3, "800 XP gives two rank-ups");
        Check(p.money == money + Progression.RankUpReward(2) + Progression.RankUpReward(3), "rank-ups pay cash");
        Check(lines.Count == 2, "a line per rank-up");

        // Rank-gated weapons.
        var sniper = WeaponCatalog.Get("03-LB2");
        var q = new PlayerProfile { money = 100000 };
        Check(Shop.Buy(q, sniper) == PurchaseResult.RankTooLow, "sniper needs rank " + sniper.requiredRank);
        q.xp = Progression.XpForLevel(sniper.requiredRank);
        Check(Shop.Buy(q, sniper) == PurchaseResult.Ok, "sniper buyable at required rank");
        foreach (var w in WeaponCatalog.All) Check(w.price > 0 || w.requiredRank == 1, w.code + " free weapons need no rank");

        // Match XP.
        var stats = new SoldierStats { pointsScored = 5, hitsCalled = 3, captures = 1 };
        int xp = Progression.SoldierXp(stats, true, false, null);
        Check(xp == 100 + 60 + 5 * 12 + 3 * 8 + 60, "soldier XP adds up: " + xp);
        Check(Progression.RefereeXp(0, 10, null) >= 50, "referee XP never below 50");

        // Daily bonus streaks.
        var d = new PlayerProfile();
        var day1 = new DateTime(2026, 10, 1);
        Check(Progression.ClaimDaily(d, day1) == 50 && d.dailyStreak == 1, "first daily bonus");
        Check(Progression.ClaimDaily(d, day1) == 0, "only once per day");
        Check(Progression.ClaimDaily(d, day1.AddDays(1)) == 75 && d.dailyStreak == 2, "streak grows next day");
        for (int i = 2; i < 12; i++) Progression.ClaimDaily(d, day1.AddDays(i));
        Check(d.dailyStreak == Progression.MaxStreak, "streak capped");
        Check(Progression.ClaimDaily(d, day1.AddDays(20)) == 50 && d.dailyStreak == 1, "missing days resets streak");
    }

    static void Maps()
    {
        var ids = new HashSet<string>();
        foreach (var m in MapLibrary.All)
        {
            Check(ids.Add(m.id), "unique map id " + m.id);
            Check(m.spawnZones.Length == 2 && m.flagPoints.Length == 2, m.id + " has two spawns and two flags");
            foreach (var p in m.pieces)
            {
                if (p.kind == PieceKind.Bush) continue; // walk-through
                var r = new Rect(p.center.x - p.size.x / 2f, p.center.y - p.size.y / 2f, p.size.x, p.size.y);
                foreach (var z in m.spawnZones) Check(!Overlaps(r, z), m.id + ": " + p.kind + " at " + p.center + " blocks a spawn zone");
                foreach (var f in m.flagPoints) Check(!r.Contains(f), m.id + ": flag " + f + " inside " + p.kind);
            }
            foreach (var z in m.spawnZones) Check(m.bounds.Contains(z.center), m.id + " spawn inside bounds");
            foreach (var f in m.flagPoints) Check(m.bounds.Contains(f), m.id + " flag inside bounds");
            var mini = Minimap.For(m);
            Check(mini.width == Mathf.CeilToInt(m.bounds.width * Minimap.PixelsPerMetre), m.id + " minimap width");
            var spawnPx = Minimap.Normalized(m, m.spawnZones[0].center);
            Check(spawnPx.x > 0f && spawnPx.x < 0.5f && spawnPx.y > 0f && spawnPx.y < 1f, m.id + " blue spawn on the left of the minimap");
        }
    }

    static bool Overlaps(Rect a, Rect b)
    {
        return a.xMin < b.xMax && a.xMax > b.xMin && a.yMin < b.yMax && a.yMax > b.yMin;
    }

    static void Weapons()
    {
        foreach (var w in WeaponCatalog.Primaries)
        {
            var inst = new WeaponInstance(w);
            Check(w.Allows(inst.Mode), w.code + " starts in an allowed fire mode");
            Check(PixelArt.Gun(w).sprite != null, w.code + " has pixel art");
        }
        var rifle = new WeaponInstance(WeaponCatalog.Primaries[0]);
        int fired = rifle.PullTrigger(true, true, 0f);
        Check(fired == 1, "first trigger pull fires");
        Check(rifle.PullTrigger(true, false, 0.001f) == 0, "rate of fire limits the next shot");
        Check(rifle.StartReload(1f) && rifle.IsReloading, "can reload a partly used mag");
        rifle.Tick(1f + rifle.Data.reloadTime + 0.01f);
        Check(!rifle.IsReloading && rifle.AmmoInMag == rifle.Data.magCapacity, "reload refills the mag");
    }
}
