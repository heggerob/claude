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
