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
        SeaDangerTests();
        SoundTests();
        SaveTests();
        ModelTests();
        HeroTests();
        SwingTests();
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
