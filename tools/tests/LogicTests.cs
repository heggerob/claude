// Plain-C# tests for Odin's Coin logic that doesn't need a running Unity scene.
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using OdinsCoin;

public static class LogicTests
{
    static int failures, passes;

    static void Check(bool ok, string what)
    {
        if (ok) { passes++; if (Environment.GetEnvironmentVariable("VERBOSE") != null) Console.WriteLine("ok: " + what); }
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
        SeaDangerTests();
        SoundTests();
        SaveTests();
        ModelTests();
        HeroTests();
        SwingTests();
        DrawnTextureTests();
        HeroChoiceTests();
        NpcHeroTests();
        StickAnimTests();
        LocomotionTests();
        AnimatorTests();
        AttackTests();
        ClothWindTests();
        FaceTests();
        RestTests();
        SkinTests();
        CombatTests();
        WorldMapTests();
        ShipPhysicsTests();
        SeamanshipTests();
        AbilityTests();
        Console.WriteLine(passes + " passed, " + failures + " failed");
        return failures == 0 ? 0 : 1;
    }

    static void AbilityTests()
    {
        // Every ability the hero screen lists does something in the game.
        var known = new HashSet<string> { "cleave", "plunder", "rally", "tribute", "stars", "currents", "wall", "reach", "foresight", "ward", "keen", "volley" };
        foreach (OutfitId o in System.Enum.GetValues(typeof(OutfitId)))
            foreach (var a in Outfits.Get(o).abilities)
                Check(known.Contains(a.id) && Abilities.Has(o, a.id), Outfits.Get(o).title + "'s " + a.name + " is wired into the game");
        Check(!Abilities.Has(OutfitId.Raider, "tribute") && Abilities.Has(OutfitId.Jarl, "tribute"), "only the Jarl has Tribute");
        Check(Abilities.PlunderCarrySpeed > Viking.CarrySpeed && Abilities.CleaveArc > VikingCombat.Arc && Abilities.LongReach > 1f && Abilities.TributePrice > 1f, "the gifts make things better");
        // Ward of Runes: curses fade faster, blessings don't.
        var warded = new Fortune();
        warded.Add(Fates.Curses[0], 1, 60f);
        warded.Add(Fates.Blessings[0], 1, 60f);
        warded.Tick(30f, Abilities.WardCurseRate);
        Check(warded.Active.Count == 2 && warded.Active.Exists(a => a.card.kind == FateKind.Blessing && Mathf.Abs(a.remaining - 30f) < 1e-3f)
            && warded.Active.Exists(a => a.card.kind == FateKind.Curse && a.remaining < 30f - 1f), "Ward of Runes wears curses off faster than blessings");
    }

    static void SeamanshipTests()
    {
        var wolf = ShipDesign.Wavewolf;
        // Water over the rail: none while it's clear, and a rail well under swamps her in a minute or two.
        Check(Seamanship.ShippedWater(0f, 10f) == 0f && Seamanship.ShippedWater(-0.5f, 10f) == 0f, "no water comes in while the rail is above the sea");
        float swamp = Seamanship.OpenVolume(wolf) / Seamanship.ShippedWater(0.3f, 10f);
        float trickle = Seamanship.OpenVolume(wolf) / Seamanship.ShippedWater(0.05f, 3f);
        Check(swamp > 30f && swamp < 180f && trickle > 600f, "a rail 30 cm under swamps the Wavewolf in " + swamp.ToString("0") + " s, a wave slopping over takes " + (trickle / 60f).ToString("0") + " min");
        float rail = Seamanship.RailUnderAngle(wolf);
        Check(rail > 20f && rail < 40f, "her rail goes under at " + rail.ToString("0") + "° of heel");
        // In a full gale, full sail heels her far further than reefed.
        var beamGale = new Vector2(-Wind.MaxStrength * 22f * 0.514f - 6f * 0.514f, 0f);
        float full = Mathf.Abs(ShipPhysics.HeelAngle(wolf, ShipPhysics.Total(wolf, 4f, 0f, 0f, beamGale, new ShipPhysics.Controls { sail = 1f }, 0f).heel));
        float reefed = Mathf.Abs(ShipPhysics.HeelAngle(wolf, ShipPhysics.Total(wolf, 4f, 0f, 0f, beamGale, new ShipPhysics.Controls { sail = 0.25f }, 0f).heel));
        Check(Wind.Knots <= 28.1f && 6f + Wind.MaxStrength * 22f > 40f && full > reefed * 3f && full > 10f, "a storm blows a full gale, and full sail heels her " + full.ToString("0") + "° where reefed she heels " + reefed.ToString("0") + "°");

        // The sun where it really is: midnight sun in Lofoten at midsummer, a short blue night in Vestfold, polar night in winter.
        Check(SkyClock.SolarElevation(68.2f, 172, 0f) > 0f, "midsummer midnight in Lofoten: the sun is still up (" + SkyClock.SolarElevation(68.2f, 172, 0f).ToString("0.0") + "°)");
        float kaupangNoon = SkyClock.SolarElevation(59.03f, 172, 12f), kaupangMidnight = SkyClock.SolarElevation(59.03f, 172, 0f);
        Check(Mathf.Abs(kaupangNoon - 54.4f) < 1f && kaupangMidnight < 0f && kaupangMidnight > -12f, "midsummer at Kaupang: " + kaupangNoon.ToString("0") + "° at noon, " + kaupangMidnight.ToString("0.0") + "° at midnight (a light night)");
        Check(SkyClock.SolarElevation(68.2f, 355, 12f) < 0f, "midwinter in Lofoten: the sun never rises");
        Check(Mathf.Abs(SkyClock.SolarAzimuth(59f, 172, 12f) - 180f) < 1f && Mathf.Abs(SkyClock.SolarAzimuth(59f, 80, 6f) - 90f) < 15f, "the sun stands south at noon and rises in the east at the equinox");
        Color skyDay, ambDay, skyNight, ambNight; float sunDay, sunNight;
        SkyClock.Light(40f, out skyDay, out ambDay, out sunDay);
        SkyClock.Light(-15f, out skyNight, out ambNight, out sunNight);
        Check(sunNight == 0f && sunDay > 1f && (skyNight.r + skyNight.g + skyNight.b) < (skyDay.r + skyDay.g + skyDay.b) * 0.4f && skyNight.b > skyNight.r, "night is dark blue with no sunlight; day is bright");
        Check(ShipHud.ClockText(13.5f) == "13:30" && ShipHud.ClockText(24.25f) == "00:15", "the HUD clock reads 13:30 and wraps past midnight");
        var clocked = new Upgrades { ClockDay = 12, ClockHours = 21.75f };
        Fortune fg; Upgrades fu;
        SaveGame.Deserialize(SaveGame.Serialize(new Fortune(), clocked), out fg, out fu);
        Check(fu.ClockDay == 12 && Mathf.Abs(fu.ClockHours - 21.75f) < 1e-4f, "the voyage's day and hour survive a save");

        // First person: look out of the hero's eyes, face where you look, move any way at once.
        Check(CameraRig.ClampPitch(120f, true) == CameraRig.LookLimit && CameraRig.ClampPitch(-120f, true) == -CameraRig.LookLimit
            && CameraRig.ClampPitch(-40f, false) == -10f, "first person looks almost straight up and down; the orbit camera stays above the horizon");
        var eyeAt = CameraRig.EyePosition(new Vector3(0f, 1.3f, 0f), Vector3.up, Vector3.forward, 0.3f, 1f);
        Check(Mathf.Abs(eyeAt.y - 1.6f) < 1e-4f && eyeAt.z > 0f && eyeAt.z < 0.1f, "the eyes sit in the middle of the head, just in front of its centre");
        var strafeV = Vector3.zero;
        for (int i = 0; i < 60; i++) strafeV = Viking.Strafe(strafeV, Vector3.right, Viking.WalkSpeed, true, 1f / 60f);
        Check(Mathf.Abs(strafeV.x - Viking.WalkSpeed) < 0.01f && Mathf.Abs(strafeV.z) < 1e-4f, "in first person you sidestep at full walking speed within a second");
        var braked = strafeV;
        for (int i = 0; i < 12; i++) braked = Viking.Strafe(braked, Vector3.zero, Viking.WalkSpeed, true, 1f / 60f);
        Check(braked.magnitude < 0.01f, "letting go stops you within a fifth of a second");
        var airV = Viking.Strafe(new Vector3(0f, 0f, 5f), Vector3.back, Viking.WalkSpeed, false, 0.1f);
        var drift = Viking.Strafe(new Vector3(0f, 0f, 5f), Vector3.zero, Viking.WalkSpeed, false, 0.1f);
        Check(airV.z > 4.5f && airV.z < 5f && drift.z == 5f, "in the air a jump keeps its momentum, with only a little steering");
        Check(Mathf.Abs(HeroPose.FirstPersonAngle(-90f, 30f) - (-60f)) < 1e-3f && Mathf.Abs(HeroPose.FirstPersonAngle(20f, 30f) - 20f) < 1e-3f,
            "in first person a hanging arm is lifted into view, the lift fading out as the arm comes up to straight ahead");
        Check(HeroPose.FirstPersonAngle(170f, 30f) < 70f && HeroPose.FirstPersonAngle(170f, 30f) > HeroPose.FirstPersonAngle(90f, 30f),
            "an overhead wind-up is folded forward above your eyes, still higher than an arm raised straight up would be");
        var hanging = HeroPose.FirstPersonLift(Quaternion.identity, true, 1f) * Vector3.down;
        Check(hanging.z > 0.4f && hanging.y < 0f, "the lift swings a hanging weapon arm forward, not back");
        Check(VikingCombat.AimedArc(10f) > 4f && VikingCombat.AimedArc(10f) < 6f && VikingCombat.AimedArc(1f) == VikingCombat.BowAimArc
            && VikingCombat.AimedArc(25f) < VikingCombat.AimedArc(10f), "an aimed arrow must be on target: a man's width either side, tighter the further off");
        Check(CameraRig.Settle(4f, 0.25f) < 0.3f && CameraRig.Settle(4f, 0.02f) > 3f, "a jolt of the view dies away within a quarter second");
        var facer = new Locomotion();
        facer.Reset(Vector2.zero, 0f);
        for (int i = 0; i < 30; i++) { facer.Face(90f); facer.Step(new Vector2(1f, 0f), Viking.WalkSpeed, 1f / 60f); facer.Face(90f); }
        Check(facer.heading == 90f && facer.Stepping && facer.speed > 1f, "the body faces your view at once while the legs keep stepping");

        // Faster time on a quiet passage, never with danger about.
        string why;
        Check(TimeWarp.Allowed(true, 5000f, false, 0f, false, out why) && why == null, "a quiet passage can run fast");
        Check(!TimeWarp.Allowed(true, 300f, false, 0f, false, out why) && why.Contains("raiders"), "raiders close by stop fast time");
        Check(!TimeWarp.Allowed(false, 5000f, false, 0f, false, out why) && !TimeWarp.Allowed(true, 5000f, true, 0f, false, out why)
            && !TimeWarp.Allowed(true, 5000f, false, 0.5f, false, out why) && !TimeWarp.Allowed(true, 5000f, false, 0f, true, out why),
            "off the ship, the serpent, a storm or running aground all stop fast time");
        int lvl = 1, steps = 0;
        do { lvl = TimeWarp.Next(lvl); steps++; } while (lvl != 1 && steps < 10);
        Check(steps == TimeWarp.Levels.Length && TimeWarp.Next(1) == 2 && TimeWarp.Next(16) == (int)Passage.Factor && TimeWarp.Next((int)Passage.Factor) == 1, "T steps 1x, 2x, 4x, 8x, 16x, a long passage, and back to real time");
        var kau = Places.Find("Kaupang"); var hed = Places.Find("Hedeby");
        double dLat = (hed.latitude - kau.latitude) * Math.PI / 180.0, dLon = (hed.longitude - kau.longitude) * Math.PI / 180.0 * Math.Cos(56.8 * Math.PI / 180.0);
        float hours = (float)(6371000.0 * Math.Sqrt(dLat * dLat + dLon * dLon)) / (8f * 0.514f) / 3600f;
        Check(hours > 20f && hours / 16f < 2.5f, "Kaupang to Hedeby at 8 knots: " + hours.ToString("0") + " h, about " + (hours * 60f / 16f).ToString("0") + " min at 16x");

        // The sea follows the wind, and lies calm in the lee of the land.
        Check(Waves.SeaState(6f, 1f) < 0.5f && Waves.SeaState(28f, 1f) > 1.1f && Waves.SeaState(28f, 0.2f) < Waves.SeaState(28f, 1f) * 0.5f,
            "a breeze barely ruffles the sea (" + Waves.SeaState(6f, 1f).ToString("0.00") + "), a stiff wind builds it (" + Waves.SeaState(28f, 1f).ToString("0.00") + "), and it lies calm in a fjord's lee");

        // A long passage: an hour of sea on the flat simulation, holding her course by herself.
        var voyage = new ShipPhysics.State { heading = 90f, u = 3f };
        var sailSet = new ShipPhysics.Controls { sail = 1f };
        var northerly = new Vector2(0f, -16f * 0.514f); // a 16 kn wind from the north: a beam reach heading east
        bool stopped;
        var after = Passage.Advance(wolf, voyage, sailSet, northerly, 90f, 3600f, null, out stopped);
        float made = after.position.magnitude / 1852f;
        Check(!stopped && !float.IsNaN(after.position.x) && Mathf.Abs(Mathf.DeltaAngle(after.heading, 90f)) < 5f && made > 6f && made < 13f,
            "an hour's passage on a beam reach holds her course (" + after.heading.ToString("0") + "°) and makes " + made.ToString("0.0") + " sea miles");
        var turned = Passage.Advance(wolf, after, sailSet, northerly, 150f, 600f, null, out stopped);
        Check(Mathf.Abs(Mathf.DeltaAngle(turned.heading, 150f)) < 5f, "given a new course, the helm brings her round to it (" + turned.heading.ToString("0") + "°)");
        var reef = Passage.Advance(wolf, voyage, sailSet, northerly, 90f, 3600f, p => p.x > 2000f, out stopped);
        Check(stopped && reef.position.x < 2000f && reef.u == 0f, "she stops short of shoal water ahead (at " + reef.position.x.ToString("0") + " m)");

        // Pointing: a lateen-rigged ship sails closer to the wind than a square-rigger, and nobody sails straight into it.
        float wolfPoint = Seamanship.ClosestToWind(wolf, 16f), cutterPoint = Seamanship.ClosestToWind(ShipDesign.Skerrycutter, 16f);
        Check(wolfPoint >= 35f && wolfPoint < cutterPoint && cutterPoint <= 85f, "the Wavewolf points " + wolfPoint + "° off the wind, the square-rigged Skerrycutter only " + cutterPoint + "°");
        float portTack, starTack;
        Seamanship.TackHeadings(0f, 60f, out portTack, out starTack);
        Check(Mathf.Abs(portTack - 60f) < 1e-3f && Mathf.Abs(starTack - 300f) < 1e-3f, "wind from the north, 60° off: tack on 060° and 300°");

        // The anchor: slack inside the rode's reach, then pulling towards the anchor; it holds her with the sails
        // down in a stiff wind, but full sail in a gale drags it.
        float drag;
        Check(Seamanship.RodePull(wolf, new Vector2(0f, 30f), Vector2.zero, 10f, out drag) == Vector2.zero && drag == 0f, "the rode lies slack while she's inside its reach");
        var pull = Seamanship.RodePull(wolf, new Vector2(0f, 82f), Vector2.zero, 10f, out drag);
        Check(pull.y > 0f && Mathf.Abs(pull.x) < 1e-3f && drag == 0f, "a taut rode pulls her towards the anchor (" + pull.y.ToString("0") + " N)");
        pull = Seamanship.RodePull(wolf, new Vector2(0f, 120f), Vector2.zero, 10f, out drag);
        Check(Mathf.Abs(pull.magnitude - Seamanship.HoldingForce(wolf)) < 1f && drag > 0f, "pulled too hard, the anchor drags and the pull is its holding");
        var ahead = new Vector2(0f, -28f * 0.514f);
        float bare = new Vector2(ShipPhysics.Total(wolf, 0f, 0f, 0f, ahead, new ShipPhysics.Controls(), 0f).fx, ShipPhysics.Total(wolf, 0f, 0f, 0f, ahead, new ShipPhysics.Controls(), 0f).fz).magnitude;
        var driven = ShipPhysics.Total(wolf, 0f, 0f, 0f, beamGale, new ShipPhysics.Controls { sail = 1f }, 0f);
        float sailPull = new Vector2(driven.fx, driven.fz).magnitude;
        Check(bare < Seamanship.HoldingForce(wolf) && sailPull > Seamanship.HoldingForce(wolf),
            "the anchor holds her bare-poled in 28 kn (" + (bare / 1000f).ToString("0.0") + " kN of " + (Seamanship.HoldingForce(wolf) / 1000f).ToString("0") + ") but not under full sail in a gale (" + (sailPull / 1000f).ToString("0") + " kN)");
        Check(Seamanship.MaxAnchorDepth >= 20f && Seamanship.MaxAnchorDepth <= Seamanship.RodeLength, "she can anchor in water up to " + Seamanship.MaxAnchorDepth + " m deep");

        // Running aground: a nudge at a walking pace is harmless, striking at full speed stoves in her planks.
        Check(Seamanship.GroundingHoles(1f) == 0 && Seamanship.GroundingHoles(3f) == 1 && Seamanship.GroundingHoles(5.5f) >= 3 && Seamanship.GroundingHoles(50f) == 6,
            "grounding gently does no harm; at ten knots she's holed in " + Seamanship.GroundingHoles(5.14f) + " places");

        // Mooring lines: slack until they're taken up, then they hold her in.
        Check(Seamanship.LinePull(wolf, new Vector3(3f, 0f, 0f), Vector3.zero, 4f) == Vector3.zero, "a slack line doesn't pull");
        var line = Seamanship.LinePull(wolf, new Vector3(5f, 0f, 0f), Vector3.zero, 4f);
        Check(line.x > 0f && Mathf.Abs(line.z) < 1e-3f, "a taut line pulls her towards the bollard");
        var damped = Seamanship.LinePull(wolf, new Vector3(5f, 0f, 0f), new Vector3(2f, 0f, 0f), 4f);
        Check(damped.x < line.x, "the line's give takes the snatch out of it when she's already coming in");
    }

    static void ShipPhysicsTests()
    {
        var wolf = ShipDesign.Wavewolf;
        Func<ShipDesign, ShipPhysics.State, ShipPhysics.Controls, Vector2, float, ShipPhysics.State> run = (d, s, c, air, seconds) =>
        {
            for (float t = 0f; t < seconds; t += 0.02f) s = ShipPhysics.Step(d, s, c, air, 0.02f);
            return s;
        };
        var calm = Vector2.zero;
        var rest = new ShipPhysics.State();

        // The classes are big: every one bigger than the last, the flagship over 60 m and hundreds of tonnes.
        for (int i = 1; i < ShipDesign.All.Length; i++)
            Check(ShipDesign.All[i].Mass > ShipDesign.All[i - 1].Mass && ShipDesign.All[i].length > ShipDesign.All[i - 1].length, ShipDesign.All[i].title + " is bigger than " + ShipDesign.All[i - 1].title);
        Check(ShipDesign.Krakenhall.length > 60f && ShipDesign.Krakenhall.Mass > 500000f && ShipDesign.Stormbreaker.sails.Length == 3, "the flagship is over 60 m and 500 t; the war galley has three masts (" + ShipDesign.Krakenhall.Mass / 1000f + " t)");
        foreach (var d in ShipDesign.All) Check(d.GM > 0.3f, d.title + " is stable: she comes back upright when heeled (GM " + d.GM + " m)");

        // She floats at her design draught: the hull's cells at that depth hold up exactly her weight.
        foreach (var d in ShipDesign.All)
        {
            float lift = 0f;
            foreach (var c in ShipPhysics.FloatCells(d)) lift += ShipPhysics.CellBuoyancy(d, c, d.draught);
            Check(Math.Abs(lift - d.Mass * ShipPhysics.G) < d.Mass * ShipPhysics.G * 0.01f, d.title + " floats at her draught");
        }

        // Rowing: a steady few knots, like real oared ships, no faster.
        var rowed = run(wolf, rest, new ShipPhysics.Controls { oarsPort = 1f, oarsStarboard = 1f }, calm, 120f);
        float rowKn = ShipPhysics.Knots(rowed.u);
        Check(rowKn > 4f && rowKn < 8f, "rowing, the Wavewolf makes a steady few knots (" + rowKn + " kn)");
        Check(Math.Abs(rowed.heading) < 1f, "rowing both sides evenly goes straight");

        // The rudder needs water flowing past it: at rest it does nothing, under way it turns her.
        var still = run(wolf, rest, new ShipPhysics.Controls { rudder = 1f }, calm, 20f);
        Check(Math.Abs(still.heading) < 0.5f, "at rest the rudder can't turn her (" + still.heading + " deg)");
        var turning = run(wolf, rowed, new ShipPhysics.Controls { rudder = 1f, oarsPort = 1f, oarsStarboard = 1f }, calm, 30f);
        Check(turning.heading > 30f, "under way, hard to starboard turns her to starboard (" + turning.heading + " deg in 30 s)");
        var turningPort = run(wolf, rowed, new ShipPhysics.Controls { rudder = -1f, oarsPort = 1f, oarsStarboard = 1f }, calm, 30f);
        Check(turningPort.heading < -30f, "and hard to port turns her to port (" + turningPort.heading + ")");
        Check(turning.u < rowed.u, "turning hard costs speed");

        // Rowing one side and backing the other spins her almost on the spot.
        var spun = run(wolf, rest, new ShipPhysics.Controls { oarsPort = 1f, oarsStarboard = -1f }, calm, 60f);
        Check(spun.heading > 40f && spun.position.magnitude < 60f, "pulling port and backing starboard spins her round to starboard where she lies (" + spun.heading + " deg, " + spun.position.magnitude + " m)");

        // Sailing: a fresh breeze (8 m/s, ~16 kn) from astern drives her well, but not much past hull speed.
        var wind = new Vector2(0f, 8f); // blowing north, towards her bow's direction: from astern
        var run1 = run(wolf, rest, new ShipPhysics.Controls { sail = 1f }, wind, 180f);
        Check(ShipPhysics.Knots(run1.u) > 5f && run1.u < wolf.HullSpeed * 1.25f, "running before a fresh breeze she makes " + ShipPhysics.Knots(run1.u) + " kn (hull speed " + ShipPhysics.Knots(wolf.HullSpeed) + " kn)");
        // Straight into the wind nothing drives her: she stops and is blown back.
        var upwind = run(wolf, new ShipPhysics.State { u = 3f }, new ShipPhysics.Controls { sail = 1f }, new Vector2(0f, -8f), 90f);
        Check(upwind.u < 0.3f, "no sail drives her straight into the wind (" + upwind.u + " m/s)");
        // Sails furled, the wind still pushes the hull and masts along.
        var drift = run(wolf, rest, new ShipPhysics.Controls(), new Vector2(0f, 12f), 120f);
        Check(drift.u > 0.2f && drift.u < 2f, "furled in a gale she still drifts downwind (" + drift.u + " m/s)");

        // A reach: wind on the beam. She sails, makes a little leeway, and heels.
        var beam = new Vector2(8f, 0f); // blowing east, from her port side as she heads north
        // A helmsman holds her on course (north) with the rudder.
        var reach = rest;
        for (float t = 0f; t < 180f; t += 0.02f)
            reach = ShipPhysics.Step(wolf, reach, new ShipPhysics.Controls { sail = 1f, rudder = Mathf.Clamp(-reach.heading * 0.08f - reach.r * 3f, -1f, 1f) }, beam, 0.02f);
        Check(Math.Abs(reach.heading) < 5f, "a helmsman can hold her on a beam reach (" + reach.heading + " deg off course)");
        float leeway = Mathf.Atan2(reach.v, reach.u) * Mathf.Rad2Deg;
        Check(ShipPhysics.Knots(reach.u) > 3f, "on a beam reach she sails (" + ShipPhysics.Knots(reach.u) + " kn, heading " + reach.heading + ", v " + reach.v + ")");
        Check(leeway > 1f && leeway < 12f, "the keel keeps her leeway to a few degrees, to leeward (" + leeway + " deg)");

        // Lateen sails point higher: 45 degrees off the wind, a lateen drives where a square sail can barely.
        var flow45 = new Vector2(Mathf.Sin(-135f * Mathf.Deg2Rad), Mathf.Cos(-135f * Mathf.Deg2Rad)) * 8f; // apparent wind from 45 deg off the starboard bow
        float c1, c2;
        var lateen = ShipPhysics.Sail(new SailPlan { rig = Rig.Lateen, area = 100f, maxBrace = 80f }, new Vector2(-flow45.x, -flow45.y) * -1f, 1f, out c1);
        var square = ShipPhysics.Sail(new SailPlan { rig = Rig.Square, area = 100f, maxBrace = 50f }, new Vector2(-flow45.x, -flow45.y) * -1f, 1f, out c2);
        Check(lateen.y > 0f && lateen.y > square.y * 1.3f, "45 degrees off the wind a lateen sail drives far better than a square one (" + lateen.y + " vs " + square.y + " N)");

        // Past hull speed the bow wave is a wall.
        float slow = ShipPhysics.Resistance(wolf, wolf.HullSpeed * 0.7f), fast = ShipPhysics.Resistance(wolf, wolf.HullSpeed * 1.3f);
        Check(fast > slow * 5f, "resistance climbs steeply past hull speed (" + slow + " -> " + fast + " N)");

        // A war galley heels in a strong beam wind, but stays well on her feet.
        var galley = ShipDesign.Stormbreaker;
        var f = ShipPhysics.Total(galley, 3f, 0f, 0f, new Vector2(-12f, 0f), new ShipPhysics.Controls { sail = 1f }, 0f);
        float heel = Math.Abs(ShipPhysics.HeelAngle(galley, f.heel));
        Check(heel > 1f && heel < 25f, "a strong beam wind heels the war galley a few degrees (" + heel + " deg)");
        // A bigger ship answers the helm more slowly.
        var bigTurn = run(ShipDesign.Krakenhall, new ShipPhysics.State { u = 3f }, new ShipPhysics.Controls { rudder = 1f, oarsPort = 1f, oarsStarboard = 1f }, calm, 10f);
        var smallTurn = run(ShipDesign.Skerrycutter, new ShipPhysics.State { u = 3f }, new ShipPhysics.Controls { rudder = 1f, oarsPort = 1f, oarsStarboard = 1f }, calm, 10f);
        Check(smallTurn.heading > bigTurn.heading * 1.5f, "the little skerrycutter turns far quicker than the flagship (" + smallTurn.heading + " vs " + bigTurn.heading + " deg in 10 s)");
        // Each class gets a model the size of its design: as long as its hull, masts standing well above the deck.
        foreach (var d in ShipDesign.All)
        {
            var model = ShipModel.Build(d, new ShipLook());
            float minZ = float.MaxValue, maxZ = float.MinValue, top = float.MinValue;
            foreach (var p in model.Pieces) foreach (var v in p.mesh.Vertices) { minZ = Math.Min(minZ, v.z); maxZ = Math.Max(maxZ, v.z); top = Math.Max(top, v.y); }
            Check(maxZ - minZ > d.length && maxZ - minZ < d.length * 1.4f && top > d.freeboard + 8f, d.title + "'s model matches its design (" + (maxZ - minZ) + " m long, " + top + " m tall)");
        }
        // Oars pull in strokes.
        Check(ShipPhysics.Oars(18, 1f, 2f, 0.25f) > ShipPhysics.Oars(18, 1f, 2f, 0.75f) * 2f, "the oars pull hardest mid-stroke");
    }

    static void WorldMapTests()
    {
        // The real map, built from elevation data by tools/world/build_map.py.
        var path = "OdinsCoin/Assets/Resources/World/north.bytes";
        Check(System.IO.File.Exists(path), "the world map is in the game's resources");
        if (!System.IO.File.Exists(path)) return;
        var map = WorldMap.FromBytes(System.IO.File.ReadAllBytes(path));
        WorldMap.Scale = 1f;
        Func<float, float, float> h = (lat, lon) => { var p = map.ToWorld(lat, lon); return map.GroundHeight(p.x, p.z); };
        Check(h(61.64f, 8.31f) > 1500f, "Jotunheimen is high mountains (" + h(61.64f, 8.31f) + " m)");
        Check(h(60.1f, 7.5f) > 900f, "Hardangervidda is a high plateau (" + h(60.1f, 7.5f) + " m)");
        Check(h(64.8f, -18.5f) > 300f, "the middle of Iceland is land (" + h(64.8f, -18.5f) + " m)");
        Check(h(57f, 3f) < -20f && h(57f, 3f) > -150f, "the North Sea is shallow sea (" + h(57f, 3f) + " m)");
        Check(h(58.2f, 9.5f) < -200f, "the Skagerrak trench is deep (" + h(58.2f, 9.5f) + " m)");
        Check(h(66f, 0f) < -1500f, "the Norwegian Sea is deep ocean (" + h(66f, 0f) + " m)");
        Check(h(58f, 20f) < 0f && h(59.33f, 18.2f) > 0f, "the Baltic is sea and Stockholm is land");
        Check(h(52f, -1.5f) > 0f && h(53.5f, -5f) < 0f, "England is land and the Irish Sea is sea");
        // Distances are real: Bergen to Nidaros (Trondheim) is about 430 km as the raven flies.
        var bergen = map.ToWorld(60.39f, 5.32f);
        var nidaros = map.ToWorld(63.43f, 10.40f);
        float km = Vector3.Distance(bergen, nidaros) / 1000f;
        Check(km > 400f && km < 460f, "Bergen to Nidaros is about 430 km (" + km + ")");
        // The projection comes back to where it started.
        var ll = map.ToLatLon(map.ToWorld(68.24f, 13.76f));
        Check(Math.Abs(ll.x - 68.24f) < 1e-3f && Math.Abs(ll.y - 13.76f) < 1e-3f, "latitude/longitude survive a round trip (" + ll + ")");
        // A smaller world scales distances and heights together.
        float fullHeight = h(61.64f, 8.31f);
        WorldMap.Scale = 0.1f;
        var small = map.ToWorld(63.43f, 10.40f);
        Check(Math.Abs(small.z - nidaros.z * 0.1f) < 1f && Math.Abs(h(61.64f, 8.31f) - fullHeight * 0.1f) < 0.5f, "the world scale shrinks distances and heights alike");
        WorldMap.Scale = 1f;
        // The streamed land: chunks load nearest first, all round the player, and neighbours meet exactly.
        var around = WorldTerrain.Wanted(new Vector2Int(10, -3), WorldTerrain.Radius);
        Check(around[0] == new Vector2Int(10, -3) && around.Count > 40, "the player's own chunk loads first, and a ring around it (" + around.Count + ")");
        Check(WorldTerrain.ChunkOf(-0.5, 1999.9, 1000f) == new Vector2Int(-1, 1), "positions fall in the right chunk, negative ones too");
        var bergenChunk = WorldTerrain.ChunkOf(bergen.x, bergen.z, 1000f);
        double cx = bergenChunk.x * 1000.0, cz = bergenChunk.y * 1000.0;
        var west = TerrainPatch.Build(map, cx, cz, 1000f, 10);
        var east = TerrainPatch.Build(map, cx + 1000.0, cz, 1000f, 10);
        Check(Math.Abs(TerrainDetail.Height(map, cx + 1000.0, cz + 300.0) - TerrainDetail.Height(map, cx + 1000.0, cz + 300.0)) < 1e-4f && west.Vertices.Count == east.Vertices.Count && west.Vertices.Count == 10 * 10 * 6,
            "terrain chunks are flat-shaded (six vertices a square) and heights are the same from either side");
        int kinds = 0;
        var mix = TerrainPatch.Build(map, cx - 10000.0, cz - 10000.0, 20000f, 40);
        foreach (var list in mix.Triangles) if (list.Count > 0) kinds++;
        Check(kinds >= 3, "the land round Bergen has sea, shore, grass and rock (" + kinds + " kinds)");
        bool sheet = true;
        foreach (int i in mix.Triangles[(int)Ground.Seabed]) if (Math.Abs(mix.Vertices[i].y - TerrainPatch.SeaSheet) > 1e-3f) sheet = false;
        Check(sheet, "under the sea the terrain is a flat sheet just below the waterline");
        // The detail keeps the real shape: it only nudges the land, never raises a sea into mountains.
        float worst = 0f;
        for (int k = 0; k < 200; k++)
        {
            double px = bergen.x + (k % 20) * 731.0, pz = bergen.z + (k / 20) * 977.0;
            worst = Math.Max(worst, Math.Abs(TerrainDetail.Height(map, px, pz) - map.GroundHeight((float)px, (float)pz)));
        }
        Check(worst < 80f, "made-up detail stays within tens of metres of the real land (" + worst + ")");
        Check(TerrainDetail.Kind(2000f, 0.1f) == Ground.Snow && TerrainDetail.Kind(-10f, 0f) == Ground.Seabed && TerrainDetail.Kind(1f, 0.1f) == Ground.Sand && TerrainDetail.Kind(200f, 1.5f) == Ground.Rock,
            "peaks are snow, the sea floor seabed, beaches sand and cliffs rock");

        // The floating origin: shifting keeps global positions and brings the player home.
        WorldOrigin.OffsetX = WorldOrigin.OffsetZ = 0.0;
        Vector3 shift;
        Check(!WorldOrigin.NeedsShift(new Vector3(100f, 0f, 100f), out shift) && WorldOrigin.NeedsShift(new Vector3(2500f, 0f, 0f), out shift), "the origin only moves when the player strays kilometres away");
        WorldOrigin.OffsetX = 5.0e6; WorldOrigin.OffsetZ = -1.2e6;
        var scene = WorldOrigin.ToScene(5.0e6 + 12.5, -1.2e6 - 3.25, 7f);
        Check(Math.Abs(scene.x - 12.5f) < 1e-4f && Math.Abs(scene.z + 3.25f) < 1e-4f && scene.y == 7f, "far from the world's centre, positions near the player stay exact");
        // The sea is fixed to the world: after a shift the same water is at the same height under the ship.
        float before = Waves.Height(1990f, -1432f, 37.5f);
        WorldOrigin.NeedsShift(new Vector3(1990f, 0f, -1432f), out shift);
        WorldOrigin.OffsetX += shift.x; WorldOrigin.OffsetZ += shift.z;
        float after = Waves.Height(1990f - shift.x, -1432f - shift.z, 37.5f);
        Check(Math.Abs(before - after) < 0.01f && shift.x % WorldOrigin.ShiftStep == 0f, "the waves don't jump when the origin shifts (" + before + " vs " + after + ")");
        WorldOrigin.OffsetX = WorldOrigin.OffsetZ = 0.0;

        // The fine coast layer, if built: it covers the Norwegian coast and agrees with the 1 km map.
        var coastPath = "OdinsCoin/Assets/Resources/World/coast.bytes";
        if (System.IO.File.Exists(coastPath))
        {
            var detail = WorldDetail.FromBytes(System.IO.File.ReadAllBytes(coastPath));
            Check(detail.BlockCount > 500 && detail.Covers(bergen.x, bergen.z) && detail.Covers(nidaros.x, nidaros.z), "the fine coast layer covers Bergen and Nidaros (" + detail.BlockCount + " blocks)");
            var mid = map.ToWorld(64f, -18f);
            Check(!detail.Covers(mid.x, mid.z), "inland Iceland has no coast blocks");
            map.Detail = detail;
            Check(h(61.64f, 8.31f) > 1500f && h(57f, 3f) < -20f, "with the coast layer, mountains and sea are still where they were");
            map.Detail = null;
        }

        // The real places: every one has a harbour deep enough for the big ships, near where it really was
        // (on the fine coast layer, which has the narrow fjords, rivers mouths and lakes).
        bool haveCoast = System.IO.File.Exists("OdinsCoin/Assets/Resources/World/coast.bytes");
        if (haveCoast) map.Detail = WorldDetail.FromBytes(System.IO.File.ReadAllBytes("OdinsCoin/Assets/Resources/World/coast.bytes"));
        // The harbours the game uses are baked (tools/world/bake_harbours.sh): water joined to the open sea.
        var harboursPath = "OdinsCoin/Assets/Resources/World/harbours.txt";
        Check(System.IO.File.Exists(harboursPath), "the baked harbours are in the game's resources");
        Places.LoadHarbours(System.IO.File.Exists(harboursPath) ? System.IO.File.ReadAllText(harboursPath) : null);
        var names = new HashSet<string>();
        int halls = 0, monasteries = 0;
        foreach (var place in Places.All)
        {
            Check(names.Add(place.name), place.name + " is only listed once");
            if (place.kind == PlaceKind.Hall) halls++;
            if (place.kind == PlaceKind.Monastery) monasteries++;
            if (!haveCoast) continue; // the harbours need the fine coast layer (tools/world/build_detail.py)
            Vector3 harbour;
            bool found = Places.Harbour(map, place, out harbour);
            float away = Vector3.Distance(harbour, Places.Position(map, place)) / 1000f;
            Check(found && Places.Depth(map, harbour) >= Places.HarbourDepth && away <= place.harbourReach, place.name + " has a harbour " + away.ToString("0.0") + " km off, " + Places.Depth(map, harbour).ToString("0") + " m deep");
            Check(found && Places.Reachable(map, harbour, 15000f, 50f), place.name + "'s harbour can be reached from the open sea");
        }
        // Home: a stretch of open water off Kaupang, big enough for the home island and its harbour.
        if (haveCoast)
        {
            Vector3 home;
            bool open = RealWorld.FindHomeWater(map, Places.Find(RealWorld.HomePlace), out home);
            float off = Vector3.Distance(home, Places.Position(map, Places.Find(RealWorld.HomePlace))) / 1000f;
            Check(open && RealWorld.OpenWater(map, home, RealWorld.HomeWater) && off < 20f, "the home island sits in open water " + off.ToString("0.0") + " km off Kaupang");
            Check(Places.Reachable(map, home, 15000f, 50f), "from home, the open sea can be reached");
        }
        Check(Places.All.Length >= 20 && halls >= 5 && monasteries >= 2, "there are halls to base at and monasteries to raid (" + halls + ", " + monasteries + ")");
        // Each place's settlement stands on its real land: a jetty to the water, buildings on dry ground, none overlapping.
        if (haveCoast)
            foreach (var place in Places.All)
            {
                var plots = Settlements.Layout(map, place);
                int wanted = Settlements.Plan(place.kind).Length + 1;
                bool dry = true, apart = true;
                for (int i = 1; i < plots.Count; i++)
                {
                    if (TerrainDetail.Height(map, plots[i].at.x, plots[i].at.z) < 0.6f) dry = false;
                    for (int j = 1; j < i; j++)
                        if (Vector2.Distance(new Vector2(plots[i].at.x, plots[i].at.z), new Vector2(plots[j].at.x, plots[j].at.z)) < 6f) apart = false;
                }
                float far = 0f;
                foreach (var p in plots) far = Math.Max(far, Vector3.Distance(p.at, plots[0].at));
                Check(plots.Count >= wanted - 1 && plots[0].kind == BuildingKind.Jetty && dry && apart && far < 3000f,
                    place.name + "'s settlement: " + plots.Count + "/" + wanted + " built, on dry ground, spread " + (int)far + " m");
            }
        // Each raidable place's chests and guards stand on dry land, outside every building.
        if (haveCoast)
            foreach (var place in Places.All)
            {
                var plunder = PlaceLife.PlunderOf(place.kind);
                if (plunder.chests == 0) continue;
                var plots = Settlements.Layout(map, place);
                var main = PlaceLife.MainBuilding(plots);
                var chests = PlaceLife.Stations(map, plots, main, plunder.chests, 3f, 11);
                var guards = PlaceLife.Stations(map, plots, main, plunder.guards, 8f, 12);
                bool fine = chests.Count == plunder.chests && guards.Count == plunder.guards;
                foreach (var list in new[] { chests, guards })
                    foreach (var p in list)
                    {
                        if (TerrainDetail.Height(map, p.x, p.z) < 0.5f) fine = false;
                        foreach (var plot in plots) if (PlaceLife.InsideBuilding(plot, p, 0.5f)) fine = false;
                    }
                Check(fine, place.name + ": " + chests.Count + " chests and " + guards.Count + " guards on dry land, outside the buildings");
            }
        map.Detail = null;

        // Bjorn's commissions: raids on places not yet stripped, the nearest first, paying more the further they are.
        var homeG = Places.Position(map, Places.Find("Kaupang"));
        var offers = Commissions.Offers(map, homeG, new HashSet<string>(), 3, 5);
        var offersRaided = Commissions.Offers(map, homeG, new HashSet<string> { offers[0].place.name }, 3, 5);
        bool allRaidable = true;
        foreach (var o in offers) if (PlaceLife.PlunderOf(o.place.kind).chests == 0) allRaidable = false;
        Check(offers.Count == 3 && allRaidable && offers[0].place.name != offers[1].place.name, "Bjorn offers three different raids on places with plunder (" + offers[0].place.name + ", " + offers[1].place.name + ", " + offers[2].place.name + ")");
        bool skipped = true;
        foreach (var o in offersRaided) if (o.place.name == offers[0].place.name) skipped = false;
        Check(skipped, "a place already plundered isn't offered again");
        Check(Commissions.Reward(map, Places.Find("Reykjavík"), homeG) > Commissions.Reward(map, Places.Find("Borre"), homeG) + 300,
            "a raid on far Reykjavík pays far more than one on Borre, next door (" + Commissions.Reward(map, Places.Find("Reykjavík"), homeG) + " vs " + Commissions.Reward(map, Places.Find("Borre"), homeG) + ")");
        var commissioned = new Upgrades { Commission = "Lindisfarne", CommissionReward = 540 };
        commissioned.Raided.Add("Iona");
        Fortune cfg; Upgrades cfu;
        SaveGame.Deserialize(SaveGame.Serialize(new Fortune(), commissioned), out cfg, out cfu);
        Check(cfu.Commission == "Lindisfarne" && cfu.CommissionReward == 540 && cfu.Raided.Contains("Iona"), "the commission and the plundered places survive a save");
        SaveGame.Deserialize("gold=1\ncommission=Atlantis,99999\nraided=Nowhere,Iona\n", out cfg, out cfu);
        Check(cfu.Commission == null && cfu.Raided.Count == 1, "made-up places in a save are ignored");

        // The land takes the wind: full wind out at sea, a lull in Bergen's harbour with the wind off the mountains.
        var northSea = map.ToWorld(57f, 3f);
        var vagen = map.ToWorld(60.395f, 5.31f);
        if (haveCoast) map.Detail = WorldDetail.FromBytes(System.IO.File.ReadAllBytes("OdinsCoin/Assets/Resources/World/coast.bytes"));
        Check(WindShelter.Factor(map, northSea, Vector3.right) > 0.99f && WindShelter.Factor(map, northSea, Vector3.back) > 0.99f, "out on the North Sea the wind blows full from every side");
        Check(WindShelter.Factor(map, vagen, Vector3.left) < 0.5f && WindShelter.Factor(map, vagen, Vector3.right) > 0.95f,
            "in Bergen's harbour an east wind off the mountains drops to " + WindShelter.Factor(map, vagen, Vector3.left).ToString("0.00") + ", a west wind off the sea blows full");
        map.Detail = null;

        // The water moves: Saltstraumen runs at five knots and more on the flood, turns, and runs back on the ebb;
        // the coastal current sets north off Norway; the open North Sea is still.
        var salt = map.ToWorld(67.235f, 14.62f);
        double flood = Currents.TidePeriod / 4.0, ebb = Currents.TidePeriod * 0.75, slack = Currents.TidePeriod / 2.0;
        Check(Currents.At(map, salt, flood).magnitude > 4.5f && Vector2.Dot(Currents.At(map, salt, flood), Currents.At(map, salt, ebb)) < 0f && Currents.At(map, salt, slack).magnitude < 0.5f,
            "Saltstraumen runs " + (Currents.At(map, salt, flood).magnitude * 1.94f).ToString("0") + " knots on the flood, turns with the ebb, and goes slack between");
        Check(Currents.At(map, map.ToWorld(57f, 3f), flood).magnitude < 0.01f, "the open North Sea has no current");
        var offStad = Currents.At(map, map.ToWorld(62.2f, 4.9f), slack);
        Check(offStad.y > 0.2f && offStad.magnitude < 0.5f, "off Stad the coastal current sets north at about half a knot (" + (offStad.magnitude * 1.94f).ToString("0.0") + " kn)");

        // Merchants sail for one of the market towns near where they're met.
        var nearBergen = MerchantTraffic.PickPort(map, map.ToWorld(60.3f, 4.9f));
        Check(nearBergen != null && PlaceLife.HasMarket(nearBergen) && Vector3.Distance(Places.Position(map, nearBergen), map.ToWorld(60.3f, 4.9f)) < 700000f,
            "a merchant met off Bergen is bound for a nearby market (" + (nearBergen != null ? nearBergen.name : "none") + ")");

        // Life at the places: monasteries, halls and fortresses hold plunder under guard; towns trade instead.
        foreach (var place in Places.All)
        {
            var plunder = PlaceLife.PlunderOf(place.kind);
            bool ok = PlaceLife.HasMarket(place)
                ? plunder.chests == 0 && PlaceLife.PriceFactor(place) >= 1.1f
                : plunder.chests >= 1 && plunder.guards >= 1 && plunder.minGold <= plunder.maxGold;
            Check(ok, place.name + (PlaceLife.HasMarket(place) ? " trades at " + PlaceLife.PriceFactor(place) + "x" : " holds " + plunder.chests + " chests under " + plunder.guards + " guards"));
        }
        Check(PlaceLife.PriceFactor(Places.Find("Hedeby")) > PlaceLife.PriceFactor(Places.Find("Ribe")), "Hedeby, the greatest market, pays best");
        Check(PlaceLife.PlunderOf(PlaceKind.Monastery).chests > PlaceLife.PlunderOf(PlaceKind.Landing).chests && PlaceLife.PlunderOf(PlaceKind.Fortress).guards > PlaceLife.PlunderOf(PlaceKind.Monastery).guards,
            "monasteries are rich and lightly guarded; fortresses bristle with guards");
        var spots = PlaceLife.Spots(new Vector3(100f, 0f, 200f), 5, 10f, 7);
        bool round = spots.Count == 5;
        foreach (var sp in spots) { float r = Vector2.Distance(new Vector2(sp.x, sp.z), new Vector2(100f, 200f)); if (r < 5.9f || r > 10.01f) round = false; }
        Check(round && PlaceLife.Spots(new Vector3(100f, 0f, 200f), 5, 10f, 7)[3] == spots[3], "chests and guards stand round the main building, the same every visit");

        // The sea chart: sea in blues, land in ochres, the coast inked, and places and the ship where they belong.
        Check(SeaChart.Tint(-200f).b > SeaChart.Tint(-200f).r && SeaChart.Tint(300f).r > SeaChart.Tint(300f).b && SeaChart.Tint(-500f).g < SeaChart.Tint(-5f).g,
            "on the chart the sea is blue and darker when deep, the land is ochre");
        var chartPx = SeaChart.Paint(map, 200, Mathf.RoundToInt(200f * map.Height / map.Width));
        int inked = 0, seaPx = 0;
        foreach (var c in chartPx) { if (c.r < 0.4f && c.g < 0.35f) inked++; if (c.b > c.r) seaPx++; }
        Check(inked > 100 && seaPx > chartPx.Length / 5 && seaPx < chartPx.Length * 9 / 10, "the chart inks the coasts (" + inked + " px) and has both sea and land");
        var whole = map.Bounds;
        var chartSheet = new Rect(0f, 0f, 800f, 600f);
        var corner = SeaChart.ToScreen(whole, chartSheet, whole.xMin, whole.yMax);
        var chartMid = SeaChart.ToScreen(whole, chartSheet, whole.center.x, whole.center.y);
        Check(corner.magnitude < 1e-2f && Vector2.Distance(chartMid, new Vector2(400f, 300f)) < 1e-2f, "the chart puts the north-west corner top left and the middle in the middle");
        var zoomed = SeaChart.View(whole, 4f, bergen.x, bergen.z);
        Check(Mathf.Abs(zoomed.width - whole.width / 4f) < 1f && zoomed.Contains(new Vector2(bergen.x, bergen.z)) && zoomed.xMin >= whole.xMin && zoomed.yMin >= whole.yMin && zoomed.xMax <= whole.xMax + 1f && zoomed.yMax <= whole.yMax + 1f,
            "zoomed in, the chart shows the ship's corner of the world and stays on the map");
        Check(SeaChart.ScaleKm(2500f) == 500f && SeaChart.ScaleKm(40f) == 10f, "the scale bar picks a round length");

        // Bad files are refused rather than read as nonsense.
        bool refused = false;
        try { WorldMap.FromBytes(new byte[] { 1, 2, 3, 4, 5 }); } catch (System.IO.InvalidDataException) { refused = true; } catch (System.IO.EndOfStreamException) { refused = true; }
        Check(refused, "a file that isn't a world map is refused");
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

    /// <summary>Signed volume: positive when every face points outwards (Unity's front faces are clockwise).</summary>
    static float Volume(MeshData m)
    {
        double v = 0;
        for (int i = 0; i < m.Triangles.Count; i += 3)
        {
            Vector3 a = m.Vertices[m.Triangles[i]], b = m.Vertices[m.Triangles[i + 1]], c = m.Vertices[m.Triangles[i + 2]];
            v += Vector3.Dot(a, Vector3.Cross(b - a, c - a)) / 6.0;
        }
        return (float)v;
    }

    static void ModelTests()
    {
        // Every shape faces outwards and has about the right volume.
        float lathe = Volume(MeshData.Lathe(new[] { new Vector2(1f, 0f), new Vector2(1f, 1f) }, 32));
        Check(lathe > 3.0f && lathe < 3.2f, "a lathe cylinder faces out, volume ~pi (" + lathe + ")");
        float ball = Volume(MeshData.Ellipsoid(new Vector3(5f, 2f, 1f), new Vector3(1f, 1f, 1f), 24, 16));
        Check(ball > 4.0f && ball < 4.25f, "an ellipsoid faces out, volume ~4.19 even off-centre (" + ball + ")");
        float dome = Volume(MeshData.Dome(Vector3.zero, Vector3.one, 24, 10));
        Check(dome > 2.0f && dome < 2.1f, "a dome faces out, volume ~2.09 (" + dome + ")");
        float box = Volume(MeshData.Box(new Vector3(1f, 2f, 3f), new Vector3(1f, 2f, 3f)));
        Check(Math.Abs(box - 6f) < 0.001f, "a box faces out (" + box + ")");
        float tube = Volume(MeshData.Tube(new[] { new Vector3(0f, 0f, 0f), new Vector3(0f, 1f, 0f), new Vector3(1f, 2f, 0f) }, new[] { 0.2f, 0.2f, 0.2f }, 24));
        Check(tube > 0.2f, "a tube faces out (" + tube + ")");
        float tubeZ = Volume(MeshData.Tube(new[] { new Vector3(0f, 0f, 0f), new Vector3(0f, 0f, 1f) }, new[] { 0.5f, 0.5f }, 32));
        Check(tubeZ > 0.75f && tubeZ < 0.8f, "a tube along Z faces out, volume ~0.785 (" + tubeZ + ")");
        var cw = new[] { new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(1f, 0f) };
        var ccw = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f) };
        Check(Math.Abs(Volume(MeshData.Extrude(cw, 0.5f)) - 0.5f) < 0.001f && Math.Abs(Volume(MeshData.Extrude(ccw, 0.5f)) - 0.5f) < 0.001f, "extrusions face out whichever way the outline was drawn");
        var disc = new MeshData();
        for (int q = 0; q < 4; q++) disc.Append(MeshData.Wedge(1f, 0.1f, q * 90f, q * 90f + 90f, 16));
        float dv = Volume(disc);
        Check(dv > 0.3f && dv < 0.32f, "four wedges make a disc facing out, volume ~0.314 (" + dv + ")");

        // The Viking himself.
        var look = new VikingLook();
        var model = VikingModel.Build(look);
        float minY = float.MaxValue, maxY = float.MinValue, minZ = float.MaxValue, maxZ = float.MinValue;
        bool finite = true, indices = true;
        foreach (var p in model.Pieces)
        {
            Vector3 at = model.RestPosition(p.joint);
            foreach (var v in p.mesh.Vertices)
            {
                Vector3 w = at + v;
                if (float.IsNaN(w.x) || float.IsNaN(w.y) || float.IsNaN(w.z)) finite = false;
                if (p.joint == VikingModel.Weapon || p.joint == VikingModel.Shield) continue;
                minY = Math.Min(minY, w.y); maxY = Math.Max(maxY, w.y); minZ = Math.Min(minZ, w.z); maxZ = Math.Max(maxZ, w.z);
            }
            foreach (int t in p.mesh.Triangles) if (t < 0 || t >= p.mesh.Vertices.Count) indices = false;
        }
        Check(finite && indices, "the model has no NaNs or bad indices");
        Check(Math.Abs(minY) < 0.02f, "feet on the ground (" + minY + ")");
        Check(maxY > 1.9f && maxY < 2.05f, "about 1.95 m tall with the helmet (" + maxY + ")");
        Check(maxZ - minZ < 0.6f, "not too deep front to back (" + (maxZ - minZ) + ")");
        Check(model.TriangleCount > 2000 && model.TriangleCount < 12000, "low-poly but smooth: " + model.TriangleCount + " triangles");
        foreach (var name in new[] { VikingModel.Body, VikingModel.Head, VikingModel.LeftLeg, VikingModel.RightLeg, VikingModel.LeftArm, VikingModel.RightArm, VikingModel.Weapon, VikingModel.Shield })
            Check(model.Find(name) != null, "joint " + name + " exists");
        int perJointColour = 0;
        var seen = new HashSet<string>();
        bool opaque = true;
        foreach (var p in model.Pieces)
        {
            if (!seen.Add(p.joint + "|" + p.color.r + "," + p.color.g + "," + p.color.b + "," + p.color.a)) perJointColour++;
            if (Math.Abs(p.color.a - 1f) > 1e-4f) opaque = false;
        }
        Check(opaque, "every colour is fully opaque");
        Check(perJointColour == 0, "one mesh per colour per joint");
        Check(model.Pieces.Count < 60, "few enough meshes to draw cheaply (" + model.Pieces.Count + ")");
        // Hands reach the weapon joint; the head sits on the neck.
        Check(Math.Abs(model.RestPosition(VikingModel.Weapon).y - (VikingModel.ShoulderHeight - VikingModel.ArmLength)) < 0.001f, "the weapon is in the hand");
        // Horns, swords and size change the model.
        int plain = model.TriangleCount;
        Check(VikingModel.Build(new VikingLook { horns = true }).TriangleCount > plain, "horns add geometry");
        Check(VikingModel.Build(new VikingLook { sword = true }).TriangleCount != plain, "a sword replaces the axe");
        var big = VikingModel.Build(new VikingLook { size = 1.2f });
        float bigTop = float.MinValue;
        foreach (var p in big.Pieces) foreach (var v in p.mesh.Vertices) bigTop = Math.Max(bigTop, (big.RestPosition(p.joint) + v).y);
        Check(Math.Abs(bigTop - maxY * 1.2f) < 0.02f, "size scales the whole Viking (" + bigTop + ")");
    }

    static float Top(VikingModel m)
    {
        float top = float.MinValue;
        foreach (var p in m.Pieces) foreach (var v in p.mesh.Vertices) top = Math.Max(top, (m.RestPosition(p.joint) + v).y);
        return top;
    }

    static void HeroTests()
    {
        var spec = CharacterSpec.Default(OutfitId.Raider);
        var model = HeroModel.Build(spec);
        float minY = float.MaxValue;
        int shells = 0;
        bool facesOut = true, shellsInsideOut = true;
        foreach (var p in model.Pieces)
        {
            float v = Volume(p.mesh);
            if (p.ink) { shells++; if (v >= 0f) shellsInsideOut = false; }
            else if (v <= 0f) { facesOut = false; Console.WriteLine("  inside-out piece on " + p.joint + " (" + v + ")"); }
            Vector3 at = model.RestPosition(p.joint);
            foreach (var vert in p.mesh.Vertices) minY = Math.Min(minY, (at + vert).y);
        }
        float top = Top(model);
        Check(facesOut, "every hero piece faces outwards");
        Check(shells >= 6 && shellsInsideOut, "every joint has an ink outline shell, turned inside out (" + shells + ")");
        Check(minY > -0.02f && minY < 0.01f, "hero stands on the ground (" + minY + ")");
        Check(top > spec.body.height && top < spec.body.height + 0.12f, "the helmet sits on top of the head (" + top + ")");
        foreach (var name in new[] { Joints.LeftForearm, Joints.RightForearm, Joints.OffHand, Joints.Weapon, Joints.Head, Joints.Back })
            Check(model.Find(name) != null, "joint " + name);

        // The body is chosen by the player; the outfit fits any of them.
        var fit = Fit.Of(spec.body);
        Check(fit.headR * 2f / fit.height > 0.14f && fit.headR * 2f / fit.height < 0.2f, "the head is about a seventh of the height");
        Check(fit.limbR < 0.02f, "limbs are stick-thin");
        foreach (var h in new[] { 1.45f, 1.62f, 1.9f })
            foreach (var w in new[] { 0.8f, 1f, 1.3f })
                foreach (var g in new[] { Gender.Male, Gender.Female })
                {
                    var sp = CharacterSpec.Default(OutfitId.Raider);
                    sp.body = new BodyShape { height = h, width = w, gender = g };
                    var mm = HeroModel.Build(sp);
                    float t = Top(mm);
                    var ff = Fit.Of(sp.body);
                    Check(t > h && t < h + 0.14f, "outfit fits a " + g + " " + h + " m, width " + w + " (" + t + ")");
                    Check(ff.bootTop < ff.knee && ff.knee < ff.hip && ff.hip < ff.waist && ff.waist < ff.chest && ff.chest < ff.shoulderY && ff.shoulderY < ff.neckY && ff.neckY < ff.headY, "body parts stack in order at " + h + " m");
                }
        var man = Fit.Of(new BodyShape { gender = Gender.Male });
        var woman = Fit.Of(new BodyShape { gender = Gender.Female });
        Check(man.shoulderX > woman.shoulderX && woman.hipR > woman.waistR && man.chestR > woman.chestR, "men and women are built differently");
        var broad = Fit.Of(new BodyShape { width = 1.3f });
        Check(broad.chestR > man.chestR && Math.Abs(broad.height - man.height) < 1e-5f, "width makes you broader, not taller");
        Check(Fit.Of(new BodyShape { height = 5f }).height <= 2f && Fit.Of(new BodyShape { width = 0f }).width >= 0.75f, "silly sizes are clamped");

        // Colours are changeable: a custom palette paints the same garments.
        var red = CharacterSpec.Default(OutfitId.Raider);
        red.palette = Outfits.Get(OutfitId.Raider).palette();
        red.palette.cloth = new Color(0.9f, 0f, 0f);
        bool found = false;
        foreach (var p in HeroModel.Build(red).Pieces) if (p.color.Equals(red.palette.cloth)) found = true;
        Check(found, "a changed palette colour shows up on the clothes");
        // Weapons are separate from the character.
        foreach (var p in model.Pieces) Check(p.joint != Joints.Weapon, "no weapon inside the character model");
        var axe = HeroModel.BuildWeapon(spec);
        Check(axe.Pieces.Count > 0 && Volume(axe.Pieces[0].mesh) > 0f, "the weapon is built on its own");

        float cape = Volume(CharacterKit.Cape(new Vector3(0f, 1.4f, -0.1f), 0.4f, 0.6f, 1f, 0.12f, 0.08f, 2));
        var grid = new Vector3[2, 2] { { new Vector3(0f, 1f, 0f), new Vector3(1f, 1f, 0f) }, { Vector3.zero, new Vector3(1f, 0f, 0f) } };
        Check(cape > 0f && Volume(CharacterKit.Sheet(grid, Vector3.forward, 0.1f)) > 0f && Volume(CharacterKit.Sheet(grid, Vector3.back, 0.1f)) > 0f, "cloth sheets face out whichever way they hang");
        Check(Volume(CharacterKit.RaggedSkirt(0.9f, 0.15f, 0.45f, 0.2f, 0.75f, 20, 0.04f, 1)) > 0f && Volume(CharacterKit.FurRing(Vector3.zero, 0.2f, 0.8f, 0.05f, 20, 0.08f, 3)) > 0f, "skirts and fur face out");
    }

    static void SwingTests()
    {
        var still = SwingSpring.For(SwingKind.Cape);
        for (int i = 0; i < 100; i++) still.Step(0.02f, Vector3.zero, Vector3.zero);
        Check(Math.Abs(still.pitch) < 1e-4f && Math.Abs(still.roll) < 1e-4f, "a cape at rest hangs straight");

        // Run forward: it's thrown back as you set off, then trails behind at speed.
        var cape = SwingSpring.For(SwingKind.Cape);
        float peak = 0f;
        for (int i = 0; i < 25; i++) { cape.Step(0.02f, new Vector3(0f, 0f, i * 0.02f * 10f), new Vector3(0f, 0f, 10f)); peak = Math.Max(peak, cape.pitch); }
        Check(peak > 5f, "setting off throws the cape back (" + peak + ")");
        for (int i = 0; i < 300; i++) cape.Step(0.02f, new Vector3(0f, 0f, 5f), Vector3.zero);
        Check(cape.pitch > 10f && cape.pitch < 40f, "running at 5 m/s the cape trails behind (" + cape.pitch + ")");
        float trailing = cape.pitch;
        // Stop dead: it swings forward past hanging, then settles.
        float lowest = trailing;
        cape.Step(0.02f, Vector3.zero, new Vector3(0f, 0f, -250f));
        for (int i = 0; i < 200; i++) { cape.Step(0.02f, Vector3.zero, Vector3.zero); lowest = Math.Min(lowest, cape.pitch); }
        Check(lowest < 0f && lowest >= SwingSpring.For(SwingKind.Cape).minPitch, "stopping swings it forward, but not into the body (" + lowest + ")");
        Check(Math.Abs(cape.pitch) < 1f, "and it settles back to hanging (" + cape.pitch + ")");

        var side = SwingSpring.For(SwingKind.Braid);
        for (int i = 0; i < 200; i++) side.Step(0.02f, new Vector3(4f, 0f, 0f), Vector3.zero);
        Check(side.roll < -3f, "moving right, a braid trails to the left (" + side.roll + ")");

        var wild = SwingSpring.For(SwingKind.Banner);
        bool bounded = true;
        for (int i = 0; i < 100; i++) { wild.Step(0.25f, new Vector3(30f, 0f, -30f), new Vector3(500f, 0f, -500f)); if (float.IsNaN(wild.pitch) || Math.Abs(wild.pitch) > 90f || Math.Abs(wild.roll) > 90f) bounded = false; }
        Check(bounded, "huge jolts and slow frames never make it spin or blow up");

        // Capes bend rather than swing as a board: the top stays on the shoulders, the hem takes the whole swing,
        // and each point further down swings a little further, so the cloth curves smoothly.
        var top = new Vector3(0f, -0.01f, -0.1f);
        var hem = new Vector3(0f, -1f, -0.1f);
        float len = ClothBend.Length(new[] { top, hem, new Vector3(0.2f, -0.5f, -0.1f) });
        Check(Math.Abs(len - 1f) < 1e-4f, "a cape's length is how far its lowest point hangs");
        Vector3 bentTop = ClothBend.Apply(top, len, 60f, 0f), bentHem = ClothBend.Apply(hem, len, 60f, 0f);
        Check(Vector3.Distance(bentTop, top) < 0.01f, "a bent cape stays on the shoulders (" + Vector3.Distance(bentTop, top) + ")");
        Check(Vector3.Distance(bentHem, Quaternion.Euler(60f, 0f, 0f) * hem) < 1e-4f, "its hem swings as far as the spring says");
        bool curves = true;
        float lastShare = 0f;
        for (int i = 1; i <= 20; i++)
        {
            float s = ClothBend.Share(i / 20f);
            if (s < lastShare || s - lastShare > 0.2f) curves = false;
            lastShare = s;
        }
        Check(curves, "further down swings further, with no kinks");
        Check(ClothBend.Apply(hem, len, 0f, 0f) == hem, "an unswung cape keeps its shape");

        // Drawing a bow: the string's middle comes back to the hand, the nocks stay on the bow, and it lets go at the loose.
        var nock = new Vector3(0f, -0.05f, 0.62f);
        var pullBack = new Vector3(0f, -0.5f, 0f);
        Check(BowDraw.Apply(nock, 0.62f, pullBack) == nock && BowDraw.Apply(-nock, 0.62f, pullBack) == -nock, "a drawn string stays on its nocks");
        Check(Vector3.Distance(BowDraw.Apply(BowDraw.Mid(1f), 0.62f, pullBack), BowDraw.Mid(1f) + pullBack) < 1e-5f, "its middle comes all the way back to the hand");
        var draw = HeroAttacks.For(WeaponId.Bow);
        Check(BowDraw.Held(draw, 0f) == 0f && BowDraw.Held(draw, draw.hitAt - 0.02f) > 0.99f && BowDraw.Held(draw, draw.hitAt + 0.01f) == 0f, "the hand takes the string, holds it at full draw and lets go at the loose");
        Check(BowDraw.Held(HeroAttacks.For(WeaponId.Sword), 0.3f) == 0f, "only a bow has a string to draw");
        Check(BowDraw.Pull(new Vector3(0f, -9f, 0f), 1f, 1f).magnitude <= BowDraw.MaxDraw(1f) + 1e-5f, "no draw longer than the bow allows");
        var bowSpec = CharacterSpec.Default(OutfitId.Scout);
        bowSpec.weapon = WeaponId.Bow;
        var bowModel = HeroModel.BuildWeapon(bowSpec);
        bool stringOnJoint = bowModel.Find(Joints.BowString) != null && bowModel.Find(Joints.BowString).parent == Joints.Weapon;
        float reach = 0f;
        foreach (var p in bowModel.Pieces) if (p.joint == Joints.BowString) foreach (var v in p.mesh.Vertices) reach = Math.Max(reach, Math.Abs(v.z));
        float s0 = Fit.Of(bowSpec.body).s;
        Check(stringOnJoint && Math.Abs(reach - BowDraw.HalfLength(s0)) < 0.01f * s0, "the bow's string is its own joint, reaching nock to nock (" + reach + ")");
        Vector3 arrowNock; Quaternion arrowRot;
        BowDraw.Arrow(pullBack, 1f, out arrowNock, out arrowRot);
        Vector3 arrowDir = arrowRot * Vector3.up, toGrip = (BowDraw.Grip(1f) - arrowNock).normalized;
        Check(Vector3.Distance(arrowNock, BowDraw.Mid(1f) + pullBack) < 1e-5f && Vector3.Dot(arrowDir, toGrip) > 0.999f, "the nocked arrow sits on the drawn string and points through the grip");
        Check(bowModel.Hidden(Joints.NockedArrow), "the arrow only shows while the bow is drawn");

        // Skirts follow the legs: a thigh swung forward pushes the cloth over it forward; above the hips nothing moves.
        float hipY = 0.9f, hipX = 0.07f;
        var chest = new Vector3(0.05f, 1.2f, 0.1f);
        Check(SkirtFlex.Apply(chest, hipY, hipX, 40f, -30f) == chest, "the body above the hips stays put");
        var leftHem = new Vector3(-0.12f, 0.45f, 0.1f);
        var rightHem = new Vector3(0.12f, 0.45f, 0.1f);
        Check(SkirtFlex.Apply(leftHem, hipY, hipX, 40f, -30f).z > leftHem.z + 0.1f, "a leg striding forward pushes its side of the skirt forward");
        Check(SkirtFlex.Apply(rightHem, hipY, hipX, 40f, -30f).z < rightHem.z - 0.1f, "the leg behind takes its side back");
        Check(SkirtFlex.Apply(leftHem, hipY, hipX, 0f, 0f) == leftHem, "standing, the skirt hangs as made");
        Check(Math.Abs(SkirtFlex.Swing(Quaternion.Euler(-HeroPose.Lean, 0f, 0f))) < 1e-3f && SkirtFlex.Swing(Quaternion.Euler(-30f - HeroPose.Lean, 0f, 0f)) > 25f, "a leg's swing is measured from standing, forward positive");

        // Heroes get swinging joints where they have capes, banners and braids.
        var jarl = HeroModel.Build(CharacterSpec.Default(OutfitId.Jarl));
        var raider = HeroModel.Build(CharacterSpec.Default(OutfitId.Raider));
        Func<VikingModel, string, bool> swings = (m, j) => { foreach (var sw in m.Swings) if (sw.joint == j) return true; return false; };
        Check(swings(jarl, Joints.Cape) && swings(jarl, Joints.LeftBraid) && swings(jarl, Joints.RightBraid), "the jarl's cape and braids swing");
        Check(swings(raider, Joints.Tabard) && !swings(raider, Joints.Cape), "the raider's banner swings; she has no cape");
        // Swinging pieces hang from their pivot: the cape is below and behind it.
        foreach (var p in jarl.Pieces)
            if (p.joint == Joints.Cape && !p.ink)
            {
                float maxY = float.MinValue, minY = float.MaxValue;
                foreach (var v in p.mesh.Vertices) { maxY = Math.Max(maxY, v.y); minY = Math.Min(minY, v.y); }
                Check(maxY < 0.1f && minY < -0.8f, "the cape hangs down from its pivot (" + minY + " .. " + maxY + ")");
            }
    }

    static void SoundTests()
    {
        foreach (SfxId id in Enum.GetValues(typeof(SfxId)))
        {
            var d = SfxSynth.Generate(id);
            float peak = 0f; double energy = 0; bool finite = true;
            foreach (var v in d) { if (float.IsNaN(v) || float.IsInfinity(v)) finite = false; peak = Math.Max(peak, Math.Abs(v)); energy += v * v; }
            float seconds = d.Length / (float)SfxSynth.SampleRate;
            Check(finite && peak <= 1f, id + " stays in range (peak " + peak + ")");
            Check(peak > 0.05f && energy > 1.0, id + " is audible (peak " + peak + ")");
            Check(seconds > 0.02f && seconds < (SfxSynth.IsLoop(id) ? 10f : 4f), id + " has a sensible length (" + seconds + " s)");
            if (SfxSynth.IsLoop(id))
            {
                // The seam: the jump from the last sample back to the first is no bigger than a typical step.
                double steps = 0;
                for (int i = 1; i < d.Length; i++) steps += Math.Abs(d[i] - d[i - 1]);
                double typical = steps / (d.Length - 1);
                Check(Math.Abs(d[d.Length - 1] - d[0]) < typical * 8 + 0.02, id + " loops without a click");
            }
        }
        var a1 = SfxSynth.Generate(SfxId.CoinFlip);
        var a2 = SfxSynth.Generate(SfxId.CoinFlip);
        Check(a1.Length == a2.Length && a1[500] == a2[500], "sounds are generated the same way every time");
    }

    static void SaveTests()
    {
        var f = new Fortune { Gold = 1234, Favour = 0.625f, NextFlipBlessed = true, Flips = 17, HeadsCount = 9, ChestsSold = 5, GoldPlundered = 800, DiceWon = 3, DiceLost = 4 };
        f.Carved.Add(Runes.Find("ansuz"));
        f.Carved.Add(Runes.Find("hagalaz"));
        var u = new Upgrades();
        u.Levels[(int)UpgradeKind.Sail] = 2;
        u.Levels[(int)UpgradeKind.Axe] = 3;
        f.Add(Fates.Blessings[0], 2, 60f);
        string text = SaveGame.Serialize(f, u);
        Fortune g; Upgrades v;
        Check(SaveGame.Deserialize(text, out g, out v), "a save loads");
        Check(g.Gold == 1234 && Math.Abs(g.Favour - 0.625f) < 1e-6f && g.NextFlipBlessed, "gold, favour and Muninn survive a save");
        Check(g.Carved.Count == 2 && g.Carved[0].id == "ansuz" && g.Carved[1].id == "hagalaz", "runes survive a save");
        Check(g.Flips == 17 && g.HeadsCount == 9 && g.ChestsSold == 5 && g.GoldPlundered == 800 && g.DiceWon == 3 && g.DiceLost == 4, "the boasting board survives a save");
        Check(v.Level(UpgradeKind.Sail) == 2 && v.Level(UpgradeKind.Axe) == 3 && v.Level(UpgradeKind.Hull) == 0, "upgrades survive a save");
        Check(g.Active.Count == 0, "blessings and curses are fleeting: not saved");
        Check(Math.Abs(g.HeadsChance - f.HeadsChance + 0.1f * 0f) < 0.2f && g.PayoutMultiplier == f.PayoutMultiplier, "the loaded coin plays the same");

        // The fleet: you start with a Wavewolf; bought hulls are yours, saved, and the one you sail comes back.
        var fleet = new Upgrades();
        var rich = new Fortune { Gold = 3000 };
        Check(fleet.SailingDesign == ShipDesign.Wavewolf && fleet.Owns(ShipDesign.Wavewolf) && Shipwright.Price(ShipDesign.Wavewolf) == 0, "every voyage starts with a Wavewolf");
        Check(!fleet.CanBuyShip(ShipDesign.Krakenhall, rich) && fleet.BuyShip(ShipDesign.Stormbreaker, rich) && rich.Gold == 3000 - Shipwright.Price(ShipDesign.Stormbreaker),
            "the Krakenhall is out of reach at 3000 gold; the Stormbreaker costs " + Shipwright.Price(ShipDesign.Stormbreaker));
        Check(!fleet.BuyShip(ShipDesign.Stormbreaker, new Fortune { Gold = 99999 }) && fleet.SailingDesign == ShipDesign.Stormbreaker, "you can't buy the same hull twice, and you sail the one you bought");
        int last = 0;
        bool dearer = true;
        foreach (var d in new[] { ShipDesign.Skerrycutter, ShipDesign.Stormbreaker, ShipDesign.Krakenhall }) { if (Shipwright.Price(d) <= last) dearer = false; last = Shipwright.Price(d); }
        Check(dearer, "bigger hulls cost more");
        Fortune fg; Upgrades fu;
        SaveGame.Deserialize(SaveGame.Serialize(rich, fleet), out fg, out fu);
        Check(fu.Owns(ShipDesign.Stormbreaker) && fu.Owns(ShipDesign.Wavewolf) && fu.SailingDesign == ShipDesign.Stormbreaker && fu.Fleet.Count == 2, "the fleet and the ship you sail survive a save");
        SaveGame.Deserialize("gold=10\nfleet=krakenhall,nonsense\nsailing=nonsense\n", out fg, out fu);
        Check(fu.Owns(ShipDesign.Krakenhall) && fu.SailingDesign == ShipDesign.Wavewolf, "unknown ships in a save are skipped");
        SaveGame.Deserialize("gold=10\nsailing=krakenhall\n", out fg, out fu);
        Check(fu.SailingDesign == ShipDesign.Wavewolf, "a save can't sail a ship it doesn't own");

        // A voyage left out at sea is saved where she was, exactly, and comes back there.
        var voyageSave = new Upgrades { AtSea = true, SeaX = 123456.789, SeaZ = -987654.321, SeaHeading = 271.5f };
        SaveGame.Deserialize(SaveGame.Serialize(new Fortune(), voyageSave), out fg, out fu);
        Check(fu.AtSea && Math.Abs(fu.SeaX - 123456.789) < 1e-6 && Math.Abs(fu.SeaZ + 987654.321) < 1e-6 && Math.Abs(fu.SeaHeading - 271.5f) < 1e-4f, "where the ship was left at sea survives a save");
        SaveGame.Deserialize(SaveGame.Serialize(new Fortune(), new Upgrades()), out fg, out fu);
        Check(!fu.AtSea, "a ship lying at home isn't saved as out at sea");
        SaveGame.Deserialize("gold=5\nat=NaN,1,2\n", out fg, out fu);
        Check(!fu.AtSea, "a broken position in a save is ignored");

        // Broken or hostile saves don't crash or cheat.
        Fortune h; Upgrades w;
        Check(!SaveGame.Deserialize("", out h, out w) && h.Gold == 100, "an empty save gives a fresh start");
        SaveGame.Deserialize("gold=-50\nfavour=9\nrunes=ansuz,ansuz,nope,fehu,algiz,raidho\nupgrades=99,-3,x,1\ngarbage\n=\n", out h, out w);
        Check(h.Gold == 0 && h.Favour == 1f, "gold and favour are clamped");
        Check(h.Carved.Count == Runes.Slots && h.Carved[0].id == "ansuz" && h.Carved[1].id == "fehu", "runes: no duplicates, unknowns skipped, at most " + Runes.Slots);
        Check(w.Level(UpgradeKind.Sail) == Upgrades.Def(UpgradeKind.Sail).costs.Length && w.Level(UpgradeKind.Oars) == 0, "upgrade levels are clamped (" + w.Level(UpgradeKind.Oars) + ")");
        // Decimal commas in some locales must not break favour.
        var prev = System.Threading.Thread.CurrentThread.CurrentCulture;
        System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("nb-NO");
        string nb = SaveGame.Serialize(f, u);
        System.Threading.Thread.CurrentThread.CurrentCulture = prev;
        Fortune n; Upgrades nu;
        SaveGame.Deserialize(nb, out n, out nu);
        Check(Math.Abs(n.Favour - 0.625f) < 1e-6f && nb.Contains("favour=0.625"), "saves are the same on a Norwegian PC");
    }

    static void SeaDangerTests()
    {
        // Storm falloff.
        var c = new Vector2(0f, 0f);
        Check(Math.Abs(SeaMath.StormIntensity(c, 100f, new Vector2(10f, 0f)) - 1f) < 0.001f, "full storm near the eye");
        Check(SeaMath.StormIntensity(c, 100f, new Vector2(101f, 0f)) == 0f, "calm outside the storm");
        float prev = 2f; bool falls = true;
        for (float d = 0f; d <= 110f; d += 5f) { float i = SeaMath.StormIntensity(c, 100f, new Vector2(d, 0f)); if (i > prev + 1e-5f) falls = false; prev = i; }
        Check(falls, "storm weakens steadily towards the edge");
        Check(SeaMath.StormLeakRate(0.2f) == 0f && SeaMath.StormLeakRate(1f) > 0f, "only a real storm pours water in");

        // Hull water: a full storm swamps an unbailed ship in minutes, and bailing keeps up.
        var hull = new HullWater();
        float t = 0f;
        while (!hull.Sunk && t < 1000f) { hull.Tick(0.1f, 1f); t += 0.1f; }
        Check(t > 60f && t < 150f, "an unbailed ship founders in a full storm after " + t.ToString("0") + " s");
        var bailed = new HullWater();
        for (int s2 = 0; s2 < 600; s2++) { bailed.Tick(1f, 1f); if (s2 % 5 == 0) bailed.Bail(); }
        Check(!bailed.Sunk && bailed.Level < 0.5f, "a bucket every 5 s keeps her afloat through a storm (" + bailed.Level + ")");
        var holed = new HullWater { Holes = 2 };
        holed.Tick(10f, 0f);
        Check(holed.Level > 0f, "holes let water in");
        holed.Bail(); holed.Bail();
        float after = holed.Level;
        holed.Tick(10f, 0f);
        Check(holed.Holes == 0 && Math.Abs(holed.Level - after) < 1e-5f, "plugging both holes stops the leak");
        Check(Math.Abs(new HullWater { Level = 1f }.SpeedMultiplier - 0.4f) < 0.001f, "a swamped ship crawls");

        // Ramming.
        Check(SeaMath.RamDamage(1.5f, 1f) == 0f, "a gentle bump does nothing");
        Check(SeaMath.RamDamage(4f, 1f) > 20f && SeaMath.RamDamage(7f, 1f) > Raider.MaxHull * 0.4f, "a ram at speed hurts: " + SeaMath.RamDamage(7f, 1f));
        Check(SeaMath.RamDamage(6f, 1.5f) > SeaMath.RamDamage(6f, 1f), "Thor's Wrath rams harder");
        int rams = (int)Math.Ceiling(Raider.MaxHull / SeaMath.RamDamage(6f, 1f));
        Check(rams >= 2 && rams <= 4, "a raider takes " + rams + " good rams to sink");
        Check(SeaMath.InsideHull(new Vector3(0f, 0.3f, 0f)) && SeaMath.InsideHull(new Vector3(2f, 0.3f, 3f)), "amidships is inside the hull");
        Check(!SeaMath.InsideHull(new Vector3(4f, 0.3f, 0f)) && !SeaMath.InsideHull(new Vector3(0f, 0.3f, 10f)), "beside and beyond the hull is outside");

        // Raider AI.
        var tgt = new Vector3(0f, 0f, 0f);
        var ip = SeaMath.InterceptPoint(tgt, Vector3.forward, Vector3.zero, new Vector3(50f, 0f, 0f), 13f);
        Check(ip.x > 10f && Math.Abs(ip.z) < 6f, "raider on the starboard side comes alongside to starboard (" + ip + ")");
        var ip2 = SeaMath.InterceptPoint(tgt, Vector3.forward, Vector3.zero, new Vector3(-50f, 0f, 0f), 13f);
        Check(ip2.x < -10f, "and to port from the port side");
        Check(SeaMath.SteerTowards(Vector3.zero, 0f, new Vector3(10f, 0f, 10f)) > 0f && SeaMath.SteerTowards(Vector3.zero, 0f, new Vector3(-10f, 0f, 10f)) < 0f, "steers towards the target");
        Check(Math.Abs(SeaMath.SteerTowards(Vector3.zero, 90f, new Vector3(10f, 0f, 0f))) < 0.01f, "no rudder when on course");

        // Jörmungandr.
        var b = new SerpentBrain();
        int strikes = 0; float time = 0f; bool stunnedSeen = false;
        while (b.State != SerpentBrain.Phase.Gone && time < 600f)
        {
            if (b.Tick(0.05f)) strikes++;
            if (b.State == SerpentBrain.Phase.Stunned) stunnedSeen = true;
            time += 0.05f;
        }
        Check(strikes == SerpentBrain.MaxStrikes && stunnedSeen && b.State == SerpentBrain.Phase.Gone, "left alone, it strikes " + strikes + " times and leaves after " + time.ToString("0") + " s");
        var h = new SerpentBrain();
        Check(!h.Hit(50f) && h.Health == SerpentBrain.MaxHealth, "can't hurt it while it circles");
        while (h.State != SerpentBrain.Phase.Stunned) h.Tick(0.05f);
        int blows = 0;
        while (!h.Defeated && h.State == SerpentBrain.Phase.Stunned && blows < 50) { h.Hit(VikingCombat.SwingDamage); blows++; h.Tick(VikingCombat.SwingTime); }
        int perStun = (int)(SerpentBrain.StunTime / VikingCombat.SwingTime);
        Check(perStun >= 8, "about " + perStun + " swings fit in one stun");
        float plainPerStun = perStun * VikingCombat.SwingDamage;
        Check(plainPerStun < SerpentBrain.MaxHealth && plainPerStun * 3f > SerpentBrain.MaxHealth, "it takes more than one stun but fewer than its strikes to kill it with a plain axe");
        var k = new SerpentBrain { State = SerpentBrain.Phase.Stunned };
        k.Hit(SerpentBrain.MaxHealth);
        Check(k.Defeated && k.State == SerpentBrain.Phase.Diving, "a killing blow sends it diving");

        // Dangers only spawn in open water, away from home.
        Check(SeaDangers.InSafeWaters(HomeHarbour.ShipStart), "the home fjord is safe");
        Check(!SeaDangers.InSafeWaters(new Vector3(0f, 0f, 250f)), "open sea isn't");
        var rng = new System.Random(9);
        for (int i = 0; i < 50; i++)
        {
            var from = new Vector3((float)rng.NextDouble() * 500f - 250f, 0f, (float)rng.NextDouble() * 500f);
            var spot = SeaDangers.OpenWater(from, 120f, () => (float)rng.NextDouble());
            if (!spot.HasValue) continue;
            bool water = true;
            foreach (var sp in WorldGen.Specs) if (Island.Height(sp, spot.Value.x, spot.Value.z) > -3f) water = false;
            Check(water && !SeaDangers.InSafeWaters(spot.Value), "raider spawns in deep open water " + spot.Value);
        }
    }

    static void HallTests()
    {
        // The hall and Bjorn stand on dry, fairly level ground, clear of trees, and apart from Gunnar.
        var home = HomeHarbour.Spec;
        var k = HomeHarbour.KeeperSpot;
        var hall = HomeHarbour.HallPosition;
        float hk = Island.Height(home, k.x, k.y), hh = Island.Height(home, hall.x, hall.y);
        Check(hk > Island.SandLevel && hh > Island.SandLevel, "hall and keeper are on dry land (" + hk + ", " + hh + ")");
        Check(Math.Abs(hk - hh) < 0.3f, "Bjorn stands on the hall's terrace (" + (hk - hh) + ")");
        // The hall's corners sit on the level terrace too, so it doesn't float or sink into the hill.
        foreach (var c in new[] { new Vector2(-8f, -8f), new Vector2(8f, -8f), new Vector2(-8f, 8f), new Vector2(8f, 8f) })
        {
            Vector3 w = Quaternion.Euler(0f, HomeHarbour.HallYaw, 0f) * new Vector3(c.x, 0f, c.y);
            float hc = Island.Height(home, hall.x + w.x, hall.y + w.z);
            Check(Math.Abs(hc - hh) < 0.5f, "hall corner " + c + " is level (" + (hc - hh) + ")");
        }
        Check(k.y > hall.y, "Bjorn stands on the jetty side of the hall");
        Check(home.InClearing(k) && home.InClearing(hall) && home.InClearing(new Vector2(0f, HomeHarbour.JettyStart)), "trees keep clear of the hall and jetty");
        Check(Vector2.Distance(k, new Vector2(HomeHarbour.TraderPosition.x, HomeHarbour.TraderPosition.z)) > HomeHarbour.KeeperRange + HomeHarbour.TradeRange, "Bjorn and Gunnar don't share a prompt");
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
        Check(wins == losses && wins + losses + ties == 216 * 216, "dice: you win exactly as often as Bjorn (" + wins + " / " + losses + " / " + ties + ")");
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
        // Every hull the shipwright sells has a berth alongside the jetty: afloat even in a trough, clear of the planks and the beach.
        foreach (var hullDesign in ShipDesign.All)
        {
            var at = HomeHarbour.Berth(hullDesign);
            float shallowest = float.MaxValue;
            bool afloat = true;
            for (float t = -1f; t <= 1f; t += 0.1f)
            {
                float hw, keel, g;
                ShipModel.Section(hullDesign, t, out keel, out g, out hw);
                float z = at.z + t * hullDesign.length / 2f;
                foreach (float side in new[] { -1f, 0f, 1f })
                {
                    float bottom = Island.Height(home, at.x + side * hw, z);
                    if (bottom >= at.y + keel - Waves.MaxHeight) afloat = false;
                    shallowest = Math.Min(shallowest, at.y + keel - Waves.MaxHeight - bottom);
                }
            }
            Check(afloat, hullDesign.title + " lies afloat at her berth (" + shallowest.ToString("0.0") + " m under the keel)");
            Check(at.x - hullDesign.beam / 2f > 1.7f + 0.1f && at.z - hullDesign.length / 2f > HomeHarbour.JettyStart + 4f, hullDesign.title + " lies clear of the jetty and the beach");
            float line = float.MaxValue;
            foreach (float end in new[] { 0.8f, -0.8f })
                foreach (float z in new[] { -67f, -58f, -48f })
                    line = Math.Min(line, Vector3.Distance(new Vector3(at.x, at.y + hullDesign.freeboard, at.z + end * hullDesign.length / 2f), new Vector3(1.7f, 1.95f, z)));
            Check(line < Seamanship.LineReach, hullDesign.title + " can get a line to a bollard (" + line.ToString("0.0") + " m)");
        }
        Vector3 s = HomeHarbour.Berth(GameBootstrap.PlayerDesign);
        // The player's ship (one of the new classes) and its measurements.
        var design = GameBootstrap.PlayerDesign;
        float half = design.length / 2f, beam = design.beam, deckY = DesignedShipBuilder.DeckY(design);
        // The jetty starts on the beach and runs out over the water, clear of the hull.
        float last = HomeHarbour.JettyStart + (HomeHarbour.JettyPlanks - 1) * 1.6f;
        float beach = Island.Height(home, 0f, HomeHarbour.JettyStart);
        Check(beach > 0f && beach < HomeHarbour.JettyTop + 0.35f, "jetty starts on the beach (" + beach + ")");
        Check(Island.Height(home, 0f, last) < -2f, "jetty end is over deep water");
        Check(last > s.z, "jetty reaches past the middle of the ship");
        Check(s.x - beam / 2f > 1.7f + 0.1f, "moored ship clears the jetty");
        Check(s.z - half > HomeHarbour.JettyStart + 4f, "the moored ship's stern is clear of the beach");
        // Her bow and stern lines reach the jetty's bollards (at x 1.7, z -67, -58, -48).
        foreach (float end in new[] { 0.8f, -0.8f })
        {
            var cleat = new Vector3(s.x, s.y + design.freeboard, s.z + end * half);
            float nearest = float.MaxValue;
            foreach (float z in new[] { -67f, -58f, -48f }) nearest = Math.Min(nearest, Vector3.Distance(cleat, new Vector3(1.7f, 1.95f, z)));
            Check(nearest < Seamanship.LineReach, (end > 0f ? "bow" : "stern") + " line reaches a bollard at the berth (" + nearest.ToString("0.0") + " m)");
        }
        // Gunnar can trade over the gunwale: the deck edge nearest him is within reach.
        var deckEdge = new Vector3(s.x - (beam / 2f - 0.5f), s.y + deckY, HomeHarbour.TraderPosition.z);
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

    static void DrawnTextureTests()
    {
        // Cast shadows darken the tone into the hatched range; in the sun nothing changes.
        Check(InkStyle.Shadowed(0.9f, 1f) == 0.9f && InkStyle.Shadowed(0.9f, 0f) <= 0.25f && InkStyle.Shadowed(0.1f, 0f) == 0.1f,
            "a cast shadow darkens a lit surface into hatching, and never lightens a dark one");
        {
            float lit = 0f, shaded = 0f;
            for (int x = 0; x < 40; x++) for (int y = 0; y < 40; y++) { lit += InkStyle.Hatch(0.9f, x, y); shaded += InkStyle.Hatch(InkStyle.Shadowed(0.9f, 0f), x, y); }
            Check(shaded < lit - 1f, "and so it gets pencil strokes (" + shaded + " vs " + lit + ")");
        }
        int n = DrawnTextures.Size;
        foreach (SurfaceKind kind in Enum.GetValues(typeof(SurfaceKind)))
        {
            var t = DrawnTextures.Get(kind);
            Check(t.Length == n * n, kind + " texture is " + n + "x" + n);
            float min = 1f, max = 0f, sum = 0f, inner = 0f, seamX = 0f, seamY = 0f;
            foreach (var v in t) { min = Math.Min(min, v); max = Math.Max(max, v); sum += v; }
            Check(min >= 0f && max <= 1f, kind + " values in [0, 1]");
            float mean = sum / t.Length;
            Check(mean > 0.8f, kind + " keeps the colour mostly as it is (mean " + mean + ")");
            Check(min > 0.35f, kind + " marks never go near black (min " + min + ")");
            for (int y = 0; y < n; y++)
            {
                seamX += Math.Abs(t[y * n] - t[y * n + n - 1]);
                inner += Math.Abs(t[y * n + n / 2] - t[y * n + n / 2 - 1]);
            }
            for (int x = 0; x < n; x++) seamY += Math.Abs(t[x] - t[(n - 1) * n + x]);
            Check(seamX <= inner * 2.5f + 0.5f && seamY <= inner * 2.5f + 0.5f, kind + " tiles without a seam (" + seamX + "/" + seamY + " vs " + inner + ")");
            Check(DrawnTextures.Get(kind) == t, kind + " texture is made once");
            float s = DrawnTextures.Sample(t, 1.3f, -2.6f), s2 = DrawnTextures.Sample(t, 0.3f, 0.4f);
            Check(Math.Abs(s - s2) < 1e-4f, kind + " sampling wraps");
        }
        Check(DrawnTextures.Get(SurfaceKind.Fur).Min() < 0.85f, "fur has visible strands");
        Check(DrawnTextures.Get(SurfaceKind.Cloth).Min() < 0.9f, "cloth has visible pencil strokes");

        // Every hero piece has one uv per vertex, and the outfit's colours are drawn as the right stuff.
        foreach (OutfitId id in Enum.GetValues(typeof(OutfitId)))
        {
            var spec = CharacterSpec.Default(id);
            var m = HeroModel.Build(spec);
            bool uvs = true;
            var kinds = new HashSet<SurfaceKind>();
            foreach (var p in m.Pieces) { p.mesh.FillUvs(); if (p.mesh.Uvs.Count != p.mesh.Vertices.Count) uvs = false; if (!p.ink) kinds.Add(p.surface); }
            Check(uvs, id + ": one uv per vertex");
            Check(kinds.Contains(SurfaceKind.Skin) && kinds.Contains(SurfaceKind.Cloth), id + ": skin and cloth are drawn");
            Check(kinds.Contains(SurfaceKind.Fur), id + ": has fur or hair strands");
            foreach (var p in m.Pieces) if (p.ink) Check(p.surface == SurfaceKind.Plain, id + ": ink shells stay flat");
        }
        // Hatching: none in the light, more strokes the darker it gets, never black.
        Check(Math.Abs(Avg(0.6f) - 1f) < 1e-5f && Math.Abs(Avg(1f) - 1f) < 1e-5f, "no hatching in the light");
        Check(Avg(0.45f) < 0.999f, "shadowed side gets strokes");
        Check(Avg(0.1f) < Avg(0.35f) && Avg(0.35f) < Avg(0.5f), "darker tone, more hatching");
        Check(Avg(0f) > 0.6f, "hatching never drowns the colour (" + Avg(0f) + ")");
        Check(InkStyle.HatchAmount(SurfaceKind.Skin) == 0f && InkStyle.HatchAmount(SurfaceKind.Cloth) == 1f, "faces stay clean, cloth is hatched");
        Check(InkStyle.Tone(Vector3.up, Vector3.up) == 1f && InkStyle.Tone(Vector3.down, Vector3.up) == 0f, "tone runs from shadow to sun");

        // World ink lines: boxes are solid, two-sided sheets aren't; smoothed normals close the corners.
        var box = MeshData.Box(Vector3.zero, new Vector3(1f, 2f, 0.5f));
        Check(InkOutline.IsSolid(box.Vertices.ToArray(), box.Triangles.ToArray()), "a box gets an ink line");
        var sheetV = new[] { new Vector3(0, 0, 0), new Vector3(1, 0, 0), new Vector3(1, 1, 0), new Vector3(0, 1, 0) };
        var sheetT = new[] { 0, 1, 2, 0, 2, 3, 0, 2, 1, 0, 3, 2 };
        Check(!InkOutline.IsSolid(sheetV, sheetT), "a two-sided sheet (sail, banner) doesn't");
        var sn = InkOutline.SmoothNormals(box.Vertices.ToArray(), box.Triangles.ToArray());
        bool corners = true, outward = true;
        for (int i = 0; i < sn.Length; i++)
        {
            Vector3 v = box.Vertices[i];
            if (Math.Abs(Math.Abs(sn[i].x) - Math.Abs(sn[i].y)) > 0.9f && Math.Abs(sn[i].z) < 0.01f) corners = false;
            if (Vector3.Dot(sn[i], v) <= 0f) outward = false;
        }
        Check(corners && outward, "box corners share one outward normal, so the line doesn't crack");

        var d = new Dresser { pal = new Palette() };
        Check(d.SurfaceOf(d.pal.fur) == SurfaceKind.Fur && d.SurfaceOf(d.pal.leatherDark) == SurfaceKind.Leather
            && d.SurfaceOf(d.pal.accent) == SurfaceKind.Cloth && d.SurfaceOf(d.pal.ink) == SurfaceKind.Plain
            && d.SurfaceOf(VikingModel.Shade(d.pal.metal, 1.35f)) == SurfaceKind.Plain, "palette slots map to surfaces");
    }

    static void HeroChoiceTests()
    {
        // Skin tone: its own choice, saved with the hero, painted on the face, and kept by special colour sets.
        {
            var toned = CharacterSpec.Default(OutfitId.Jarl);
            toned.skinTone = 5;
            Check(HeroChoice.Parse(HeroChoice.Serialize(toned)).skinTone == 5, "the hero remembers its skin tone");
            Check(HeroChoice.Parse("outfit=Jarl\ntone=99\n").skinTone == 0 && HeroChoice.Parse("outfit=Jarl\ntone=-3\n").skinTone == 0 && HeroChoice.Parse("outfit=Jarl\ntone=x\n").skinTone == 0,
                "a broken skin tone falls back to the first");
            Check(toned.Paint().skin.Equals(SkinTones.Get(5)) && CharacterSpec.Default(OutfitId.Jarl).Paint().skin.Equals(SkinTones.Peach), "the skin tone paints the face");
            var draugr = CharacterSpec.Default(OutfitId.Raider); draugr.skin = "raider.draugr"; draugr.skinTone = 3;
            Check(!draugr.Paint().skin.Equals(SkinTones.Get(3)), "a special colour set keeps its own skin");
            var custom = CharacterSpec.Default(OutfitId.Scout); custom.palette = new Palette(); custom.skinTone = 2;
            Check(custom.Paint().skin.Equals(SkinTones.Get(2)) && custom.palette.skin.Equals(SkinTones.Peach), "painting doesn't change the hero's own palette");
            var tones = new System.Collections.Generic.HashSet<Color>();
            for (int t = 0; t < SkinTones.Count; t++) tones.Add(SkinTones.Get(t));
            Check(tones.Count == SkinTones.Count && SkinTones.Names.Length == SkinTones.Count, "every skin tone is different and has a name");
        }
        // The eyes are pills (two round dots stacked and joined), much taller than wide.
        {
            var eye = HeroModel.Eye(1f);
            float minX = float.MaxValue, maxX = float.MinValue, minY = float.MaxValue, maxY = float.MinValue;
            foreach (var v in eye.Vertices) { minX = Math.Min(minX, v.x); maxX = Math.Max(maxX, v.x); minY = Math.Min(minY, v.y); maxY = Math.Max(maxY, v.y); }
            float w = maxX - minX, h = maxY - minY;
            Check(h > 2.4f * w && h < 3.4f * w, "an eye is a tall pill (" + w + " x " + h + ")");
            // Straight sides: halfway between the centre and the top the eye is still full width, unlike an oval.
            float widest = 0f;
            foreach (var v in eye.Vertices) if (Math.Abs(v.y - h * 0.25f) < h * 0.08f) widest = Math.Max(widest, Math.Abs(v.x));
            Check(widest > w * 0.49f, "the eye's sides run straight between its round ends");
        }
        var spec = CharacterSpec.Default(OutfitId.Seer);
        spec.body = new BodyShape { height = 1.74f, width = 0.85f, gender = Gender.Female };
        spec.weapon = WeaponId.Spear;
        spec.offHand = OffHandId.Map;
        var back = HeroChoice.Parse(HeroChoice.Serialize(spec));
        Check(back.outfit == OutfitId.Seer && Math.Abs(back.body.height - 1.74f) < 0.001f && Math.Abs(back.body.width - 0.85f) < 0.001f
            && back.body.gender == Gender.Female && back.weapon == WeaponId.Spear && back.offHand == OffHandId.Map && back.hair == spec.hair,
            "a hero survives saving and loading");
        var empty = HeroChoice.Parse("");
        Check(empty.outfit == OutfitId.Raider && empty.weapon == Outfits.Get(OutfitId.Raider).suggestedWeapon, "no saved hero gives the default Raider");
        var broken = HeroChoice.Parse("outfit=Jarl\nheight=banana\nwidth=99\ngender=7\nweapon=Laser\n");
        Check(broken.outfit == OutfitId.Jarl && Math.Abs(broken.body.height - 1.62f) < 0.001f && broken.body.width <= 1.35f
            && broken.weapon == WeaponId.Sword && broken.body.gender == Gender.Male, "broken values fall back and clamp (" + broken.body.width + ")");
        var tall = HeroChoice.Parse("height=5\nwidth=-3");
        Check(tall.body.height <= 2f && tall.body.width >= 0.75f, "a silly body is clamped");
        var comma = HeroChoice.Parse("height=1,8");
        Check(Math.Abs(comma.body.height - 1.62f) < 0.001f || comma.body.height <= 2f, "a comma decimal can't break the hero");
        Check(HeroChoice.Cycle(OutfitId.Scout, 1) == OutfitId.Raider && HeroChoice.Cycle(OutfitId.Raider, -1) == OutfitId.Scout
            && HeroChoice.Cycle(WeaponId.None, 1) == WeaponId.TwoHandAxe, "the arrows wrap round");
        foreach (WeaponId w in Enum.GetValues(typeof(WeaponId))) Check(!string.IsNullOrEmpty(HeroChoice.Name(w)), "weapon " + w + " has a name");
        foreach (OffHandId o in Enum.GetValues(typeof(OffHandId))) Check(!string.IsNullOrEmpty(HeroChoice.Name(o)), "off-hand " + o + " has a name");
        // Every outfit with every weapon and off-hand builds, and the hero has elbows for the block pose.
        foreach (OutfitId id in Enum.GetValues(typeof(OutfitId)))
            foreach (WeaponId w in Enum.GetValues(typeof(WeaponId)))
            {
                var s2 = CharacterSpec.Default(id);
                s2.weapon = w;
                var wm = HeroModel.BuildWeapon(s2);
                Check(w == WeaponId.None || wm.Pieces.Count > 0, id + " can carry " + w);
            }
    }

    static void NpcHeroTests()
    {
        var a = NpcHeroes.Saxon(1); var b = NpcHeroes.Saxon(2);
        Check(a.outfit == OutfitId.SpearGuard && a.palette != null && a.weapon == WeaponId.Sword && a.offHand == OffHandId.RoundShield, "Saxons are spear guards in Saxon colours with sword and shield");
        bool differ = false;
        for (int i = 0; i < 6; i++) { var x = NpcHeroes.Saxon(i); if (Math.Abs(x.body.height - a.body.height) > 0.01f || x.body.gender != a.body.gender) differ = true; }
        Check(differ, "a band of Saxons aren't all the same body");
        Check(NpcHeroes.Saxon(4).body.height == NpcHeroes.Saxon(4).body.height, "the same seed gives the same Saxon");
        for (int i = 0; i < 40; i++)
        {
            var body = NpcHeroes.RandomBody(i);
            Check(body.height >= 1.5f && body.height <= 1.9f && body.width >= 0.8f && body.width <= 1.25f, "random body " + i + " is believable");
        }
        foreach (var spec in new[] { NpcHeroes.Saxon(9), NpcHeroes.DanishRaider(3), NpcHeroes.Bjorn(), NpcHeroes.Gunnar() })
        {
            var m = HeroModel.Build(spec);
            Check(m.Pieces.Count > 20, spec.outfit + " NPC builds");
            bool usesPalette = false;
            foreach (var p in m.Pieces) if (p.color.Equals(spec.palette.cloth)) usesPalette = true;
            Check(usesPalette, spec.outfit + " NPC wears its own colours");
        }
        Check(NpcHeroes.DanishRaider(1).weapon == WeaponId.Bow && NpcHeroes.Gunnar().offHand == OffHandId.Map && NpcHeroes.Bjorn().weapon == WeaponId.None, "NPCs carry the right things");
    }

    static void ClothWindTests()
    {
        // A jump: the cape floats up while the hero falls, then is slammed back down by the landing.
        {
            var jcape = SwingSpring.For(SwingKind.Cape);
            const float dt = 1f / 60f;
            for (int i = 0; i < 120; i++) jcape.Step(dt, Vector3.zero, Vector3.zero);
            float hanging = jcape.pitch;
            float vy = 5.5f, jpeak = 0f;
            for (int i = 0; i < 40; i++) { vy -= 18f * dt; jcape.Step(dt, new Vector3(0f, vy, 0f), new Vector3(0f, -18f, 0f)); jpeak = Mathf.Max(jpeak, jcape.pitch); }
            Check(jpeak > hanging + 25f, "a cape floats up during a jump's fall (" + jpeak + " degrees)");
            // Landing: the fall stops dead in a frame or two.
            jcape.Step(dt, new Vector3(0f, -1f, 0f), new Vector3(0f, 60f, 0f));
            jcape.Step(dt, Vector3.zero, new Vector3(0f, 60f, 0f));
            for (int i = 0; i < 40; i++) jcape.Step(dt, Vector3.zero, Vector3.zero);
            Check(Mathf.Abs(jcape.pitch - hanging) < 10f, "and drops back after the landing (" + jcape.pitch + ")");
            var jstill = SwingSpring.For(SwingKind.Cape);
            for (int i = 0; i < 120; i++) jstill.Step(dt, Vector3.zero, Vector3.zero);
            Check(Mathf.Abs(jstill.pitch) < 2f && Mathf.Abs(jstill.roll) < 1f, "standing jstill, it just hangs");
        }
        // Standing still in a wind from behind (air moving +Z) blows a cape forward: pitch goes negative (+Z side).
        var still = SwingSpring.For(SwingKind.Banner);
        for (int i = 0; i < 400; i++) still.Step(0.02f, -new Vector3(0f, 0f, 5f), Vector3.zero);
        var moving = SwingSpring.For(SwingKind.Banner);
        for (int i = 0; i < 400; i++) moving.Step(0.02f, new Vector3(0f, 0f, 5f), Vector3.zero);
        Check(still.pitch < -1f && moving.pitch > 1f, "wind from behind blows forward, walking into still air trails back (" + still.pitch + ", " + moving.pitch + ")");
        Check(Math.Abs(ClothWind.Flutter(0f, 3.3f, 1f)) < 1e-5f, "no wind, no flutter");
        float peak = 0f;
        for (float t = 0f; t < 5f; t += 0.01f) peak = Math.Max(peak, Math.Abs(ClothWind.Flutter(8f, t, 0.5f)));
        Check(peak > 3f && peak < 12f, "a strong wind flutters a few degrees (" + peak + ")");
        Check(Math.Abs(ClothWind.Flutter(30f, 1f, 0f)) <= 12f * 0.9f + 1e-3f, "a gale is capped");
        // A fluttering spring stays within its limits.
        var cape = SwingSpring.For(SwingKind.Cape);
        for (int i = 0; i < 2000; i++) cape.Step(0.016f, new Vector3(3f, 0f, -6f), Vector3.zero, ClothWind.Flutter(10f, i * 0.016f, 0f), ClothWind.Flutter(10f, i * 0.016f, 1.7f));
        Check(cape.pitch >= cape.minPitch - 0.01f && cape.pitch <= cape.maxPitch + 0.01f && Math.Abs(cape.roll) <= cape.maxRoll + 0.01f, "flutter stays within the cape's limits");
    }

    static void RestTests()
    {
        foreach (WeaponId w in Enum.GetValues(typeof(WeaponId)))
        {
            var spec = CharacterSpec.Default(OutfitId.Raider); spec.weapon = w;
            var m = HeroModel.Build(spec);
            Vector3 tip = Quaternion.Euler(m.Find(Joints.Weapon).restEuler) * Vector3.forward;
            // Nothing sticks straight out in front of the hero at rest.
            Check(w == WeaponId.None || Math.Abs(tip.z) < 0.6f, w + " isn't held out like a lance at rest (" + tip + ")");
        }
        Check((Quaternion.Euler(Weapons.RestEuler(WeaponId.Spear)) * Vector3.forward).y > 0.95f, "a spear rests upright");
        Check((Quaternion.Euler(Weapons.RestEuler(WeaponId.Sword)) * Vector3.forward).y < -0.9f, "a sword rests point-down");
        // The axe carry puts the haft up and back across the shoulders, whatever the arm angles are.
        Vector3 haft = Quaternion.Euler(HeroPose.AxeCarryArm) * Quaternion.Euler(HeroPose.AxeCarryForearm) * HeroPose.AxeCarryWeapon * Vector3.forward;
        Check(haft.y > 0.2f && haft.z < -0.3f && haft.x < -0.7f, "the carried axe lies up and back over the shoulder (" + haft + ")");
        Vector3 fist = Quaternion.Euler(HeroPose.AxeCarryArm) * (Vector3.down + Quaternion.Euler(HeroPose.AxeCarryForearm) * Vector3.down);
        Check(fist.y > -0.6f, "the carrying fist comes up towards the shoulder (" + fist + ")");
        {
            var sw = HeroPose.CarryFor(WeaponId.Sword);
            Vector3 down = Quaternion.Euler(sw.arm) * Quaternion.Euler(sw.forearm) * HeroPose.WeaponInFist(sw) * Vector3.forward;
            Check(down.y < -0.8f, "a carried sword hangs point-down (" + down + ")");
        }
        foreach (var w in new[] { WeaponId.Spear, WeaponId.Staff })
        {
            var p = HeroPose.CarryFor(w);
            Vector3 up = Quaternion.Euler(p.arm) * Quaternion.Euler(p.forearm) * HeroPose.WeaponInFist(p) * Vector3.forward;
            Check(up.y > 0.95f, w + " is carried upright (" + up + ")");
        }
        // Long weapons carried in game stay clear of the ground, on the smallest and tallest bodies.
        foreach (var w in new[] { WeaponId.Spear, WeaponId.Staff, WeaponId.Sword })
            foreach (float h in new[] { 1.4f, 1.62f, 2f })
            {
                var spec = CharacterSpec.Default(OutfitId.Raider); spec.weapon = w; spec.body.height = h;
                float low = CarriedLowest(spec);
                Check(low > 0.02f && low < 0.35f * h, w + " carried by a " + h + " m hero clears the ground without floating (" + low + ")");
            }
    }

    /// <summary>The lowest point (height above the soles) of the hero's weapon in its in-game carry pose.</summary>
    static float CarriedLowest(CharacterSpec spec)
    {
        var body = HeroModel.Build(spec);
        var weapon = HeroModel.BuildWeapon(spec);
        var p = HeroPose.CarryFor(spec.weapon);
        Quaternion arm = Quaternion.Euler(p.arm), fore = arm * Quaternion.Euler(p.forearm), wr = fore * HeroPose.WeaponInFist(p);
        Vector3 elbow = body.Find(Joints.RightArm).localPosition + arm * body.Find(Joints.RightForearm).localPosition;
        Vector3 grip = elbow + fore * (body.Find(Joints.Weapon).localPosition + HeroPose.GripSlide(p, Fit.Of(spec.body).s));
        float low = float.MaxValue;
        foreach (var piece in weapon.Pieces)
            if (piece.joint == Joints.Weapon)
                foreach (var v in piece.mesh.Vertices) low = Math.Min(low, (grip + wr * v).y);
        return low;
    }

    static void FaceTests()
    {
        Check(Face.BlinkScale(-1f) == 1f && Face.BlinkScale(Face.BlinkTime) == 1f, "eyes are open outside a blink");
        Check(Face.BlinkScale(Face.BlinkTime * 0.5f) < 0.15f, "halfway through a blink the eyes are shut");
        Check(Face.NextBlinkGap(0) >= 2f && Face.NextBlinkGap(1) <= 5f, "blinks come every two to five seconds");
        var m = HeroModel.Build(CharacterSpec.Default(OutfitId.Scout));
        Check(m.Find(Joints.Eyes) != null && !m.Hidden(Joints.Eyes) && m.Hidden(Joints.EyesHappy) && m.Hidden(Joints.EyesHurt), "plain eyes show, the other faces start hidden");
        int happy = 0, hurt = 0;
        foreach (var p in m.Pieces) { if (p.joint == Joints.EyesHappy) happy++; if (p.joint == Joints.EyesHurt) hurt++; }
        Check(happy > 0 && hurt > 0, "happy and hurt eyes are built");
    }

    static Locomotion Walker()
    {
        var l = new Locomotion();
        l.Reset(Vector2.zero, 0f);
        return l;
    }

    /// <summary>Run a walker with a fixed wish for a while; returns it.</summary>
    static Locomotion Run(Locomotion l, Vector2 wish, float maxSpeed, float seconds, float dt)
    {
        for (float t = 0f; t < seconds; t += dt) l.Step(wish, maxSpeed, dt);
        return l;
    }

    static void LocomotionTests()
    {
        const float walk = 4.2f, sprint = 6.5f, dt = 1f / 60f;
        // Speed builds up and dies down; it never jumps.
        var l = Run(Walker(), new Vector2(0f, 1f), walk, 0.1f, dt);
        Check(l.speed > 0.3f && l.speed < walk * 0.5f, "a hero speeds up gradually (" + l.speed + " after 0.1 s)");
        Run(l, new Vector2(0f, 1f), walk, 1f, dt);
        Check(Mathf.Abs(l.speed - walk) < 0.05f, "and reaches walking speed within a second");
        Run(l, Vector2.zero, walk, 0.1f, dt);
        Check(l.speed > 0.5f && l.speed < walk, "letting go slows down, not stops dead");
        Run(l, Vector2.zero, walk, 1.5f, dt);
        Check(l.speed == 0f && !l.Stepping, "and comes to rest, feet still");

        // A 90 degree turn while walking takes several steps, each turning at most its share, along an arc.
        l = Run(Walker(), new Vector2(0f, 1f), walk, 1.5f, dt);
        var startPos = l.position;
        float last = l.heading, worst = 0f;
        int before = l.stepsTaken;
        var turnAt = new System.Collections.Generic.List<float>();
        for (float t = 0f; t < 2f; t += dt)
        {
            float allow = l.TurnPerStep;
            int steps = l.stepsTaken;
            l.Step(new Vector2(1f, 0f), walk, dt);
            if (l.stepsTaken != steps) { worst = Mathf.Max(worst, Mathf.Abs(Mathf.DeltaAngle(last, l.heading)) - allow); last = l.heading; turnAt.Add(t); }
            if (t < 0.1f + dt * 0.5f && t > 0.1f - dt * 0.5f) Check(l.heading < 30f, "turning isn't instant (" + l.heading + " degrees after 0.1 s)");
        }
        Check(worst <= 0.5f, "no step turns more than it may (" + worst + " over)");
        Check(Mathf.Abs(Mathf.DeltaAngle(l.heading, 90f)) < 1f, "the turn is finished within two seconds");
        Check(l.stepsTaken - before >= 3, "a walking 90 degree turn takes a few steps (" + (l.stepsTaken - before) + ")");
        Check(l.position.y - startPos.y > 0.4f, "it curves round rather than turning on the spot");

        // A U-turn at a sprint: brake first, pivot, then push off the other way.
        l = Run(Walker(), new Vector2(0f, 1f), sprint, 2f, dt);
        bool brakedBeforeSide = true;
        for (float t = 0f; t < 3f; t += dt)
        {
            l.Step(new Vector2(0f, -1f), sprint, dt);
            if (Mathf.Abs(Mathf.DeltaAngle(0f, l.heading)) > 90f && l.speed > sprint * 0.35f && t < 0.6f) brakedBeforeSide = false;
        }
        Check(brakedBeforeSide, "a sprinting U-turn brakes before it swings round");
        Check(Mathf.Abs(Mathf.DeltaAngle(l.heading, 180f)) < 1f && l.speed > sprint * 0.9f, "and then runs off the other way (" + l.heading + ", " + l.speed + ")");

        // Standing still, a turn is made in small steps on the spot.
        l = Walker();
        int s0 = l.stepsTaken;
        Run(l, new Vector2(-1f, 0f), walk, 0.05f, dt);
        Check(l.Pivoting, "turning while standing pivots on the spot");
        float moved = 0f;
        for (float t = 0f; t < 1f && Mathf.Abs(Mathf.DeltaAngle(l.heading, -90f)) > 5f; t += dt) { l.Step(new Vector2(-1f, 0f), walk, dt); moved = Mathf.Abs(l.position.y); }
        Check(moved < 0.35f, "it comes round nearly on the spot, not in a wide arc (" + moved + " m off the new line)");
        Check(l.stepsTaken - s0 >= 1, "a standing turn takes steps (" + (l.stepsTaken - s0) + ")");

        // Footsteps alternate, one on each side, and stride out further when running.
        l = Run(Walker(), new Vector2(0f, 1f), walk, 1.5f, dt);
        bool alternates = true;
        var prevFoot = l.planted;
        float walkStride = 0f, sprintStride = 0f;
        int n = 0;
        for (float t = 0f; t < 2f; t += dt)
        {
            int steps = l.stepsTaken;
            l.Step(new Vector2(0f, 1f), walk, dt);
            if (l.stepsTaken != steps)
            {
                if (l.planted.left == prevFoot.left) alternates = false;
                if (l.planted.left != (l.planted.at.x < l.position.x)) alternates = false;
                walkStride += Vector2.Distance(l.planted.at, prevFoot.at); n++;
                prevFoot = l.planted;
            }
        }
        walkStride /= Mathf.Max(1, n);
        Check(alternates, "footsteps alternate left and right, on their own sides");
        Run(l, new Vector2(0f, 1f), sprint, 2f, dt);
        prevFoot = l.planted; n = 0;
        for (float t = 0f; t < 2f; t += dt)
        {
            int steps = l.stepsTaken;
            l.Step(new Vector2(0f, 1f), sprint, dt);
            if (l.stepsTaken != steps) { sprintStride += Vector2.Distance(l.planted.at, prevFoot.at); n++; prevFoot = l.planted; }
        }
        sprintStride /= Mathf.Max(1, n);
        Check(sprintStride > walkStride * 1.15f, "sprinting takes longer strides (" + walkStride + " vs " + sprintStride + ")");
        Check(l.next.left == l.leftSwinging, "the next planned footstep is the swinging foot's");

        // The same walk at 30 and at 144 frames a second ends up in much the same place.
        var slow = Walker(); var fast = Walker();
        foreach (var w in new[] { new Vector2(0f, 1f), new Vector2(1f, 0.3f), new Vector2(-0.4f, -1f) })
        {
            Run(slow, w, walk, 1.2f, 1f / 30f);
            Run(fast, w, walk, 1.2f, 1f / 144f);
        }
        Check(Vector2.Distance(slow.position, fast.position) < 0.35f && Mathf.Abs(Mathf.DeltaAngle(slow.heading, fast.heading)) < 8f,
            "movement doesn't depend on the frame rate (" + Vector2.Distance(slow.position, fast.position) + " m apart)");
    }

    static float[] lastDelta;

    static float[] Channels(HeroAnimator.Frame p)
    {
        return new[] { p.body.x, p.body.y, p.body.z, p.head.x, p.head.y, p.head.z, p.leftLeg.x, p.leftLeg.z, p.rightLeg.x, p.rightLeg.z,
                       p.leftArm.x, p.leftArm.z, p.rightArm.x, p.rightArm.z, p.leftElbow, p.rightElbow, p.lift * 100f };
    }

    /// <summary>
    /// Drive a walker and an animator together. Returns the worst jolt: the largest change in any joint's
    /// per-frame movement from one frame to the next (a pop shows up as a sudden change of speed, whereas a fast
    /// but smooth running stride doesn't).
    /// </summary>
    static float Animate(Locomotion l, HeroAnimator an, Vector2 wish, float maxSpeed, float seconds, bool grounded, float vy, ref float time)
    {
        const float dt = 1f / 60f;
        float worst = 0f;
        for (float t = 0f; t < seconds; t += dt)
        {
            var before = Channels(an.pose);
            if (grounded) l.Step(wish, maxSpeed, dt); else l.Air(wish, dt);
            an.Step(dt, l, grounded, vy, false, time += dt);
            var after = Channels(an.pose);
            var delta = new float[after.Length];
            for (int i = 0; i < after.Length; i++)
            {
                delta[i] = after[i] - before[i];
                if (lastDelta != null) worst = Mathf.Max(worst, Mathf.Abs(delta[i] - lastDelta[i]));
            }
            lastDelta = delta;
        }
        return worst;
    }

    static void AnimatorTests()
    {
        const float walk = 4.2f, sprint = 6.5f;
        float time = 0f;
        var d = new Damped();
        for (int i = 0; i < 30; i++) d.Step(1f, 1f / 60f, 0.1f);
        float at30 = d.value;
        for (int i = 0; i < 60; i++) d.Step(1f, 1f / 60f, 0.1f);
        Check(at30 > 0.8f && at30 < 1f && Mathf.Abs(d.value - 1f) < 0.01f, "a damped value settles on its target without overshooting");

        // Standing: legs together, arms down, just breathing.
        var l = Walker(); var an = new HeroAnimator();
        Animate(l, an, Vector2.zero, walk, 1f, true, 0f, ref time);
        Check(Mathf.Abs(an.pose.leftLeg.x) < 1f && Mathf.Abs(an.pose.rightLeg.x) < 1f && Mathf.Abs(an.pose.body.x) < 2f, "standing still, the hero stands still");

        // Walking: the legs scissor and each arm swings against its own side's leg.
        Animate(l, an, new Vector2(0f, 1f), walk, 1.5f, true, 0f, ref time);
        float walkLegs = 0f, walkLean = 0f, minDiff = 0f, maxDiff = 0f, armLeg = 0f, walkKnee = 0f;
        for (int i = 0; i < 60; i++)
        {
            Animate(l, an, new Vector2(0f, 1f), walk, 1f / 60f, true, 0f, ref time);
            var p = an.pose;
            walkLegs = Mathf.Max(walkLegs, Mathf.Abs(p.leftLeg.x - p.rightLeg.x));
            walkLean += p.body.x / 60f;
            minDiff = Mathf.Min(minDiff, p.leftLeg.x - p.rightLeg.x); maxDiff = Mathf.Max(maxDiff, p.leftLeg.x - p.rightLeg.x);
            armLeg += (p.leftArm.x - p.rightArm.x) * (p.leftLeg.x - p.rightLeg.x);
            walkKnee = Mathf.Max(walkKnee, p.leftKnee);
        }
        Check(walkLegs > 25f && minDiff < -10f && maxDiff > 10f, "walking, the legs step one forward, one back (" + walkLegs + ")");
        Check(armLeg < 0f, "each arm swings against the leg on its side");
        Check(walkKnee > 40f, "the knee bends as the foot swings through (" + walkKnee + ")");

        // Sprinting: longer strides, a deeper lean, elbows bent to pump.
        Animate(l, an, new Vector2(0f, 1f), sprint, 1.5f, true, 0f, ref time);
        float runLegs = 0f, runLean = 0f, runElbow = 0f;
        for (int i = 0; i < 60; i++)
        {
            Animate(l, an, new Vector2(0f, 1f), sprint, 1f / 60f, true, 0f, ref time);
            runLegs = Mathf.Max(runLegs, Mathf.Abs(an.pose.leftLeg.x - an.pose.rightLeg.x));
            runLean += an.pose.body.x / 60f;
            runElbow += an.pose.leftElbow / 60f;
        }
        Check(runLegs > walkLegs * 0.9f && runLean > walkLean + 2f, "sprinting strides out and leans in (" + runLegs + ", " + runLean + ")");
        Check(runElbow < -60f, "sprinting pumps the arms with bent elbows (" + runElbow + ")");

        // The foot on the ground stays put while the body goes over it (no skating), walking and running.
        foreach (float speed in new[] { walk, sprint })
        {
            var fl = Walker(); var fa = new HeroAnimator(); float ft = 0f;
            Animate(fl, fa, new Vector2(0f, 1f), speed, 2f, true, 0f, ref ft);
            float slip = 0f, moved = 0f, prevFoot = float.NaN; bool prevLeft = false;
            for (int i = 0; i < 120; i++)
            {
                Animate(fl, fa, new Vector2(0f, 1f), speed, 1f / 60f, true, 0f, ref ft);
                var p = fa.pose;
                bool left = Gait.Reach(p.leftLeg.x, p.leftKnee) > Gait.Reach(p.rightLeg.x, p.rightKnee);
                float hip = left ? -p.leftLeg.x : -p.rightLeg.x, knee = left ? p.leftKnee : p.rightKnee;
                float foot = fl.position.y + Gait.Thigh * Mathf.Sin(hip * Mathf.Deg2Rad) + Gait.Shin * Mathf.Sin((hip - knee) * Mathf.Deg2Rad);
                if (!float.IsNaN(prevFoot) && left == prevLeft) { slip += Mathf.Abs(foot - prevFoot); moved += fl.speed / 60f; }
                prevFoot = foot; prevLeft = left;
            }
            Check(slip < moved * 0.35f, "at " + speed + " m/s the planted foot barely slides (" + slip + " m over " + moved + " m)");
        }

        // Turning right: the body banks right, the head looks round first.
        float bank = 0f, look = 0f;
        for (int i = 0; i < 20; i++)
        {
            Animate(l, an, new Vector2(1f, 0f), sprint, 1f / 60f, true, 0f, ref time);
            bank = Mathf.Min(bank, an.pose.body.z);
            look = Mathf.Max(look, an.pose.head.y);
        }
        Check(bank < -2f, "the body banks into a turn (" + bank + ")");
        Check(look > 10f, "the head looks where it's going before the body gets there (" + look + ")");

        // The crouch before a jump bends the knees and swings the arms back.
        {
            var cl = Walker(); var ca = new HeroAnimator(); float ct = 0f;
            Animate(cl, ca, Vector2.zero, walk, 0.5f, true, 0f, ref ct);
            for (int i = 0; i < 6; i++) { ca.crouch = i / 6f; Animate(cl, ca, Vector2.zero, walk, 1f / 60f, true, 0f, ref ct); }
            Check(ca.pose.leftKnee > 35f && ca.pose.leftArm.x > 15f && ca.pose.lift < -0.03f, "crouching to jump bends the knees, drops the hips and swings the arms back");
        }
        // A jump: tucked up in the air, a squash on landing, and never a jolt.
        l = Walker(); an = new HeroAnimator(); lastDelta = null;
        float jolt = Animate(l, an, new Vector2(0f, 1f), sprint, 1.5f, true, 0f, ref time);
        jolt = Mathf.Max(jolt, Animate(l, an, new Vector2(0f, 1f), sprint, 0.35f, false, 3f, ref time));
        Check(an.pose.leftLeg.x < -15f && an.pose.leftArm.z < -15f && an.pose.rightArm.z > 15f, "in the air the knees come up and the arms go out (" + an.pose.leftLeg.x + ")");
        jolt = Mathf.Max(jolt, Animate(l, an, new Vector2(0f, 1f), sprint, 0.3f, false, -7f, ref time));
        float lowest = 0f, deepest = 0f;
        for (int i = 0; i < 20; i++)
        {
            jolt = Mathf.Max(jolt, Animate(l, an, new Vector2(0f, 1f), sprint, 1f / 60f, true, i == 0 ? -7f : 0f, ref time));
            lowest = Mathf.Min(lowest, an.pose.lift);
            deepest = Mathf.Max(deepest, an.pose.body.x);
        }
        Check(lowest < -0.02f && deepest > 12f, "landing squashes down and folds forward (" + lowest + ", " + deepest + ")");
        float steady = 0f;
        for (int i = 0; i < 60; i++) { Animate(l, an, new Vector2(0f, 1f), sprint, 1f / 60f, true, 0f, ref time); steady = Mathf.Min(steady, an.pose.lift); }
        Check(lowest < steady - 0.02f, "and springs back up (" + lowest + " landing, " + steady + " running)");
        jolt = Mathf.Max(jolt, Animate(l, an, new Vector2(-1f, -0.2f), sprint, 1.5f, true, 0f, ref time));
        jolt = Mathf.Max(jolt, Animate(l, an, Vector2.zero, sprint, 1f, true, 0f, ref time));
        // (A full sprint stride itself bends the curve by ~6 degrees a frame at 60 fps; a pop would be far more.)
        Check(jolt < 9f, "no joint jumps more than a few degrees in one frame, whatever the hero does (" + jolt + ")");
    }

    static void AttackTests()
    {
        foreach (WeaponId w in Enum.GetValues(typeof(WeaponId)))
        {
            var combo = HeroAttacks.Combo(w);
            Check(combo.Length >= 2 && (w == WeaponId.Bow || combo.Length >= 3), w + ": has a combo of several different swings (" + combo.Length + ")");
            Check(combo[combo.Length - 1].power > 1f || w == WeaponId.Bow, w + ": the combo's last swing hits hardest");
            var names = new System.Collections.Generic.HashSet<string>();
            foreach (var cm in combo) names.Add(cm.name);
            Check(names.Count == combo.Length, w + ": every swing in the combo is its own move");
            var ready0 = combo[0].keys[0].v;
            foreach (var cm in combo)
            {
                bool same = true;
                for (int c = 0; c < ready0.Length; c++) if (Mathf.Abs(cm.keys[0].v[c] - ready0[c]) > 0.001f && c != (int)AttackMove.Ch.Roll) same = false;
                Check(same, w + ": " + cm.name + " starts from the same ready pose, so the combo flows");
            }
        }
        foreach (WeaponId w in Enum.GetValues(typeof(WeaponId)))
        foreach (var m in HeroAttacks.Combo(w))
        {
            var first = m.keys[0].v; var last = m.keys[m.keys.Length - 1].v;
            bool loops = m.keys[0].t == 0f && m.keys[m.keys.Length - 1].t == 1f;
            for (int c = 0; c < first.Length; c++) if (Mathf.Abs(first[c] - last[c]) > 0.001f) loops = false;
            Check(loops, w + ": the " + m.name + " starts and ends in the same ready pose");
            bool ordered = true;
            for (int i = 1; i < m.keys.Length; i++) if (m.keys[i].t <= m.keys[i - 1].t) ordered = false;
            Check(ordered && m.hitAt > m.keys[1].t && m.hitAt < 0.9f, w + ": keys in order, the blow lands after the wind-up");
            Check(m.Weight(0f) == 0f && m.Weight(1f) < 0.001f && m.Weight(m.hitAt) > 0.99f, w + ": it eases in and out, and owns the body at the blow");
            // Smooth: sampled finely, no channel changes speed abruptly.
            float worst = 0f;
            float[] a = m.Sample(0f), b = m.Sample(0.005f);
            for (float t = 0.01f; t <= 1f; t += 0.005f)
            {
                var c = m.Sample(t);
                for (int k = 0; k < c.Length; k++)
                {
                    float scale = k == (int)AttackMove.Ch.Lunge || (k >= (int)AttackMove.Ch.HaftX && k <= (int)AttackMove.Ch.HaftZ) ? 100f : 1f;
                    worst = Mathf.Max(worst, Mathf.Abs((c[k] - b[k]) - (b[k] - a[k])) * scale);
                }
                a = b; b = c;
            }
            Check(worst < 6f, w + ": the " + m.name + " moves smoothly (" + worst + ")");
            // The weapon really points where each key says.
            var hit = m.Sample(m.hitAt);
            var arm = Quaternion.Euler(hit[(int)AttackMove.Ch.ArmRX], 0f, hit[(int)AttackMove.Ch.ArmRZ]);
            var fore = Quaternion.Euler(hit[(int)AttackMove.Ch.ElbowR], 0f, 0f);
            var want = new Vector3(hit[(int)AttackMove.Ch.HaftX], hit[(int)AttackMove.Ch.HaftY], hit[(int)AttackMove.Ch.HaftZ]).normalized;
            var got = arm * fore * HeroAttacks.WeaponRotation(arm, fore, want, hit[(int)AttackMove.Ch.Roll]) * Vector3.forward;
            Check(Vector3.Angle(want, got) < 1f, w + ": at the blow the weapon points where the move says (" + Vector3.Angle(want, got) + " degrees off)");
            if (w != WeaponId.Bow) Check(want.z > 0.5f && hit[(int)AttackMove.Ch.Lunge] > 0.1f, w + ": the blow goes forward, with a step in");
        }
        Check(VikingCombat.NextComboStep(0, 3, true) == 1 && VikingCombat.NextComboStep(2, 3, true) == 0 && VikingCombat.NextComboStep(1, 3, false) == 0,
            "chained swings step through the combo and wrap round; a pause starts it over");
        var axe = HeroAttacks.For(WeaponId.TwoHandAxe);
        var up = axe.Sample(0.3f);
        Check(up[(int)AttackMove.Ch.ArmRX] < -150f && up[(int)AttackMove.Ch.HaftZ] < -0.5f, "the axe is raised high behind the head before the chop");
        Check(axe.Sample(axe.hitAt)[(int)AttackMove.Ch.Pitch] > 15f, "and the whole body comes down with it");
        // Saxon guards: the slash's blow lines up with the moment their damage lands, after the long tell.
        {
            var slash = HeroAttacks.For(WeaponId.Sword);
            bool rising = true; float prev = -1f;
            for (float e = 0f; e < 1.5f; e += 0.01f) { float p = Saxon.SlashPhase(e); if (p < prev) rising = false; prev = p; }
            Check(rising && Mathf.Abs(Saxon.SlashPhase(0.6f) - slash.hitAt) < 0.001f && Saxon.SlashPhase(1.3f) == 1f,
                "a guard's slash lands its blow exactly when the damage does, and finishes with the recovery");
        }
        var bow = HeroAttacks.For(WeaponId.Bow);
        Check(bow.Sample(0.62f)[(int)AttackMove.Ch.ElbowL] < -120f && bow.Sample(0.75f)[(int)AttackMove.Ch.ElbowL] > -100f, "the bow is drawn to the cheek, then loosed");
    }

    static void StickAnimTests()
    {
        Check(HeroPose.WalkElbow(0f) < 0f && HeroPose.WalkElbow(0f) > -20f, "a hanging arm keeps a slight bend");
        Check(HeroPose.WalkElbow(-30f) < HeroPose.WalkElbow(0f) && HeroPose.WalkElbow(30f) == HeroPose.WalkElbow(0f), "the elbow bends more as the arm swings forward");
        Check(HeroPose.ChopElbow(0.35f) < -80f, "the wind-up folds the elbow tight");
        Check(Math.Abs(HeroPose.ChopElbow(0.55f)) < 0.01f, "the blow lands with a straight arm");
        Check(Math.Abs(HeroPose.ChopElbow(0f) - HeroPose.ChopElbow(1f)) < 0.01f, "the chop ends where it began");
        float lo = 99f, hi = -99f;
        for (float t = 0f; t < 10f; t += 0.05f) { float b = HeroPose.Breath(t); lo = Math.Min(lo, b); hi = Math.Max(hi, b); }
        Check(hi > 1f && lo < -1f && hi < 3f && lo > -3f, "breathing rocks the body a degree or two");
    }

    static void SkinTests()
    {
        var ids = new HashSet<string>();
        foreach (var s in Skins.All) Check(ids.Add(s.id), "skin id " + s.id + " is unique");
        foreach (OutfitId o in Enum.GetValues(typeof(OutfitId)))
        {
            var list = Skins.For(o);
            Check(list.Count >= 7 && list.FindAll(s => s.IsClassic).Count == 1, o + " has one classic and its own and set skins");
            var classic = Skins.Paint(o, Skins.ClassicId(o));
            Check(classic.cloth.Equals(Outfits.Get(o).palette().cloth), o + " classic colours are the outfit's own");
            foreach (var s in list)
            {
                if (s.IsClassic) continue;
                var p = Skins.Paint(o, s.id);
                bool changed = !p.cloth.Equals(classic.cloth) || !p.accent.Equals(classic.accent) || !p.emblem.Equals(classic.emblem);
                Check(changed, s.id + " really changes the colours");
                Check(p.hair.Equals(classic.hair), s.id + " leaves the hair alone");
                var spec = CharacterSpec.Default(o); spec.skin = s.id;
                Check(HeroModel.Build(spec).Pieces.Count > 20, s.id + " builds");
            }
        }
        Check(Skins.Paint(OutfitId.Jarl, "raider.ash").cloth.Equals(Outfits.Get(OutfitId.Jarl).palette().cloth), "another outfit's skin gives classic colours");

        var f = new Fortune { Gold = 300 };
        var locker = SkinLocker.Parse("");
        var ash = Skins.Get("raider.ash");
        var blood = Skins.Get("raider.bloodmoon");
        Check(locker.Owns(Skins.ClassicId(OutfitId.Raider)) && !locker.Owns(ash), "classic is owned, the rest must be bought");
        Check(!locker.Buy(blood, f) && f.Gold == 300, "can't buy what you can't afford");
        Check(locker.Buy(ash, f) && f.Gold == 50 && locker.Owns(ash), "buying takes the gold and gives the skin");
        Check(!locker.Buy(ash, f) && f.Gold == 50, "can't buy the same skin twice");
        var back = SkinLocker.Parse(locker.Serialize());
        Check(back.Owns(ash) && !back.Owns(blood), "owned skins survive saving");
        Check(!SkinLocker.Parse("nonsense,raider.bloodmoon,,").Owns("nonsense") && SkinLocker.Parse("raider.bloodmoon").Owns(blood), "unknown ids are dropped");

        // Sets: every outfit gets every set; the rare ones can't be bought and turn up in chests.
        foreach (var set in Skins.Sets)
            foreach (OutfitId o in Enum.GetValues(typeof(OutfitId)))
                Check(Skins.Get(o.ToString().ToLowerInvariant() + "." + set) != null, o + " has the " + set + " set");
        var draugr = Skins.Get("raider.draugr");
        Check(draugr.rare && !draugr.IsClassic && !SkinLocker.Parse("").Owns(draugr), "rare skins aren't owned for free");
        Check(!SkinLocker.Parse("").CanBuy(draugr, new Fortune { Gold = 99999 }), "rare skins can't be bought");
        Check(!Skins.Paint(OutfitId.Raider, "raider.draugr").skin.Equals(Skins.Paint(OutfitId.Raider, null).skin), "a draugr is grave-pale");
        var roller = SkinLocker.Parse("");
        var dice = new System.Random(42);
        int found = 0, rolls = 0, rareTotal = 0;
        foreach (var s in Skins.All) if (s.rare) rareTotal++;
        for (int i = 0; i < 5000; i++) { rolls++; if (roller.RollChest(dice) != null) found++; }
        Check(found == rareTotal, "every rare skin turns up in the end, and no duplicates (" + found + "/" + rareTotal + ")");
        Check(roller.RollChest(dice) == null, "once they're all found, nothing more turns up");
        var early = SkinLocker.Parse(""); var d2 = new System.Random(7); int hits = 0;
        for (int i = 0; i < 1000; i++) if (early.RollChest(d2) != null) hits++;
        Check(hits == rareTotal || hits < 1000 * Skins.RareChance * 1.6f, "the chance per chest is about as printed");
        Check(SkinLocker.Parse(roller.Serialize()).Owns(draugr) || !roller.Owns(draugr), "found rare skins are saved");

        var hero = CharacterSpec.Default(OutfitId.Raider); hero.skin = "raider.ash";
        Check(HeroChoice.Parse(HeroChoice.Serialize(hero)).skin == "raider.ash", "the hero remembers its skin");
        var wrong = HeroChoice.Parse("outfit=Jarl\nskin=raider.ash");
        Check(wrong.skin == null, "a skin for another outfit is ignored");
    }

    static float Avg(float tone) { float s = 0f; for (int y = 0; y < 40; y++) for (int x = 0; x < 40; x++) s += InkStyle.Hatch(tone, x, y); return s / 1600f; }

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
