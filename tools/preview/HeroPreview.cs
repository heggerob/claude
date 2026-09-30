// Renders the storybook heroes in a row on a cream background, like the concept sheet, and exports each as .obj.
// A tiny software rasterizer: smooth normals like Unity's RecalculateNormals, back faces culled like Unity
// (so the inverted-hull ink outlines work exactly as they will in the game), soft ground shadows.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using OdinsCoin;

public static class HeroPreview
{
    public class Pose
    {
        public Dictionary<string, Quaternion> rot = new Dictionary<string, Quaternion>();
        public Dictionary<string, Vector3> pos = new Dictionary<string, Vector3>();
        /// <summary>Joints whose world orientation is set directly (props held at an exact angle).</summary>
        public Dictionary<string, Quaternion> worldRot = new Dictionary<string, Quaternion>();
    }

    public class Shot
    {
        public string label;
        public VikingModel model;
        public Pose pose = new Pose();
        public float yaw = 200f;
        public Expression face = Expression.Neutral;
    }

    static readonly Color Paper = new Color(0.965f, 0.945f, 0.9f);

    /// <summary>args: output .rgba, labels .txt, obj folder, which sheet ("style").</summary>
    public static void Main(string[] args)
    {
        var shots = new List<Shot>();
        var raider = CharacterSpec.Default(OutfitId.Raider);
        // The concept sheet pose: the big axe held in the left fist by the shoulder, the haft across the back of
        // the neck and the head over the right shoulder; the right arm hangs loose.
        var model = WithWeapon(HeroModel.Build(raider), HeroModel.BuildWeapon(raider), Joints.OffHand);
        var fit = Fit.Of(raider.body);
        var carry = new Pose();
        // Aim the left arm: elbow down and out, fist up by the shoulder and behind the head.
        Vector3 shoulder = new Vector3(-fit.shoulderX, fit.shoulderY, 0f);
        Vector3 elbow = shoulder + new Vector3(-0.2f, -0.12f, 0.05f).normalized * fit.upperArm;
        Vector3 fist = elbow + new Vector3(0.03f, 0.3f, -0.08f).normalized * fit.foreArm;
        carry.worldRot[Joints.LeftArm] = Quaternion.FromToRotation(Vector3.down, elbow - shoulder);
        carry.worldRot[Joints.LeftForearm] = Quaternion.FromToRotation(Vector3.down, fist - elbow);
        // The haft runs from the fist behind the neck to the other side; the blade faces us.
        var haft = new Vector3(1f, 0.3f, -0.55f).normalized;
        carry.worldRot[Joints.OffHand] = Quaternion.LookRotation(haft, Vector3.Cross(haft, Vector3.forward));
        carry.rot[Joints.LeftLeg] = Quaternion.Euler(0f, -22f, -13f);
        carry.rot[Joints.RightLeg] = Quaternion.Euler(0f, 24f, 12f);
        carry.rot[Joints.RightArm] = Quaternion.Euler(4f, 0f, 14f);
        carry.rot[Joints.RightForearm] = Quaternion.Euler(-12f, 0f, 0f);
        var walk = new Pose();
        walk.rot[Joints.LeftLeg] = Quaternion.Euler(-24f, 0f, 0f);
        walk.rot[Joints.RightLeg] = Quaternion.Euler(20f, 0f, 0f);
        walk.rot[Joints.LeftArm] = Quaternion.Euler(22f, 0f, -6f);
        walk.rot[Joints.RightArm] = Quaternion.Euler(-26f, 0f, 6f);
        walk.rot[Joints.RightForearm] = Quaternion.Euler(-30f, 0f, 0f);
        // The Jarl: sword pointing down in the right hand, shield at his left side.
        var jarl = CharacterSpec.Default(OutfitId.Jarl);
        jarl.body = new BodyShape { height = 1.72f, width = 1.1f, gender = Gender.Male };
        var jarlModel = WithWeapon(WithWeapon(HeroModel.Build(jarl), HeroModel.BuildWeapon(jarl), Joints.Weapon), HeroModel.BuildOffHand(jarl), Joints.OffHand);
        var stand = new Pose();
        stand.rot[Joints.RightArm] = Quaternion.Euler(-12f, 0f, 16f);
        stand.rot[Joints.RightForearm] = Quaternion.Euler(-35f, 0f, -8f);
        stand.worldRot[Joints.Weapon] = Quaternion.LookRotation(new Vector3(-0.15f, -1f, 0.22f).normalized, Vector3.forward);
        stand.rot[Joints.LeftArm] = Quaternion.Euler(12f, 0f, -20f);
        stand.rot[Joints.LeftForearm] = Quaternion.Euler(-10f, 0f, 6f);
        stand.worldRot[Joints.OffHand] = Quaternion.Euler(0f, 55f, 0f);
        stand.rot[Joints.LeftLeg] = Quaternion.Euler(0f, -18f, -7f);
        stand.rot[Joints.RightLeg] = Quaternion.Euler(0f, 20f, 7f);
        shots.Add(new Shot { label = "The Jarl", model = jarlModel, pose = stand, yaw = 202f });
        shots.Add(new Shot { label = "The Raider", model = model, pose = carry, yaw = 202f });
        // The Navigator: holding up a chart in both hands, head tilted towards it.
        var nav = CharacterSpec.Default(OutfitId.Navigator);
        nav.body = new BodyShape { height = 1.57f, width = 0.9f, gender = Gender.Female };
        var navFit = Fit.Of(nav.body);
        var navModel = WithWeapon(HeroModel.Build(nav), HeroModel.BuildOffHand(nav), Joints.OffHand);
        var reading = new Pose();
        Vector3 lShoulder = new Vector3(-navFit.shoulderX, navFit.shoulderY, 0f), rShoulder = new Vector3(navFit.shoulderX, navFit.shoulderY, 0f);
        Vector3 lElbow = lShoulder + new Vector3(-0.1f, -0.2f, 0.06f).normalized * navFit.upperArm;
        Vector3 lFist = new Vector3(-0.24f, navFit.waist + 0.03f, 0.2f);
        Vector3 rElbow = rShoulder + new Vector3(0.02f, -0.22f, 0.08f).normalized * navFit.upperArm;
        Vector3 rFist = new Vector3(-0.05f, navFit.waist + 0.1f, 0.22f);
        reading.worldRot[Joints.LeftArm] = Quaternion.FromToRotation(Vector3.down, lElbow - lShoulder);
        reading.worldRot[Joints.LeftForearm] = Quaternion.FromToRotation(Vector3.down, lFist - lElbow);
        reading.worldRot[Joints.RightArm] = Quaternion.FromToRotation(Vector3.down, rElbow - rShoulder);
        reading.worldRot[Joints.RightForearm] = Quaternion.FromToRotation(Vector3.down, rFist - rElbow);
        reading.worldRot[Joints.OffHand] = Quaternion.Euler(-12f, 18f, 0f);
        reading.rot[Joints.Head] = Quaternion.Euler(6f, -8f, 0f);
        reading.rot[Joints.LeftLeg] = Quaternion.Euler(0f, -16f, -5f);
        reading.rot[Joints.RightLeg] = Quaternion.Euler(0f, 18f, 5f);
        shots.Add(new Shot { label = "The Navigator", model = navModel, pose = reading, yaw = 202f });
        // The Spear Guard: spear upright in the right fist, the big shield held in front on the left.
        var guard = CharacterSpec.Default(OutfitId.SpearGuard);
        guard.body = new BodyShape { height = 1.62f, width = 1f, gender = Gender.Male };
        var gFit = Fit.Of(guard.body);
        var guardModel = WithWeapon(WithWeapon(HeroModel.Build(guard), HeroModel.BuildWeapon(guard), Joints.Weapon), HeroModel.BuildOffHand(guard), Joints.OffHand);
        var guarding = new Pose();
        Vector3 gr = new Vector3(gFit.shoulderX, gFit.shoulderY, 0f), gl = new Vector3(-gFit.shoulderX, gFit.shoulderY, 0f);
        Vector3 grElbow = gr + new Vector3(0.12f, -0.2f, -0.02f).normalized * gFit.upperArm;
        Vector3 grFist = new Vector3(gFit.shoulderX + 0.14f, gFit.chest - 0.02f, 0.06f);
        Vector3 glElbow = gl + new Vector3(-0.08f, -0.2f, 0.1f).normalized * gFit.upperArm;
        Vector3 glFist = new Vector3(-0.24f, gFit.waist - 0.05f, 0.16f);
        guarding.worldRot[Joints.RightArm] = Quaternion.FromToRotation(Vector3.down, grElbow - gr);
        guarding.worldRot[Joints.RightForearm] = Quaternion.FromToRotation(Vector3.down, grFist - grElbow);
        guarding.worldRot[Joints.Weapon] = Quaternion.LookRotation(Vector3.up, new Vector3(-1f, 0f, 0.35f));
        guarding.worldRot[Joints.LeftArm] = Quaternion.FromToRotation(Vector3.down, glElbow - gl);
        guarding.worldRot[Joints.LeftForearm] = Quaternion.FromToRotation(Vector3.down, glFist - glElbow);
        guarding.worldRot[Joints.OffHand] = Quaternion.Euler(4f, -38f, 6f);
        guarding.rot[Joints.LeftLeg] = Quaternion.Euler(0f, -16f, -9f);
        guarding.rot[Joints.RightLeg] = Quaternion.Euler(0f, 18f, 9f);
        shots.Add(new Shot { label = "The Spear Guard", model = guardModel, pose = guarding, yaw = 202f });
        // The Old Seer: the rune staff upright in her left hand, the right hand at her charms.
        var seer = CharacterSpec.Default(OutfitId.Seer);
        seer.body = new BodyShape { height = 1.6f, width = 0.95f, gender = Gender.Female };
        var sFit = Fit.Of(seer.body);
        var seerModel = WithWeapon(HeroModel.Build(seer), HeroModel.BuildWeapon(seer), Joints.OffHand);
        var augur = new Pose();
        Vector3 sl = new Vector3(-sFit.shoulderX, sFit.shoulderY, 0f), sr = new Vector3(sFit.shoulderX, sFit.shoulderY, 0f);
        Vector3 slElbow = sl + new Vector3(-0.1f, -0.2f, 0.02f).normalized * sFit.upperArm;
        Vector3 slFist = new Vector3(-sFit.shoulderX - 0.16f, sFit.chest - 0.02f, 0.06f);
        Vector3 srElbow = sr + new Vector3(0.06f, -0.2f, 0.06f).normalized * sFit.upperArm;
        Vector3 srFist = new Vector3(0.08f, sFit.waist + 0.04f, 0.16f);
        augur.worldRot[Joints.LeftArm] = Quaternion.FromToRotation(Vector3.down, slElbow - sl);
        augur.worldRot[Joints.LeftForearm] = Quaternion.FromToRotation(Vector3.down, slFist - slElbow);
        augur.worldRot[Joints.RightArm] = Quaternion.FromToRotation(Vector3.down, srElbow - sr);
        augur.worldRot[Joints.RightForearm] = Quaternion.FromToRotation(Vector3.down, srFist - srElbow);
        augur.worldRot[Joints.OffHand] = Quaternion.LookRotation(Vector3.up, new Vector3(1f, 0f, 0.3f));
        shots.Add(new Shot { label = "The Old Seer", model = seerModel, pose = augur, yaw = 202f });
        // The Scout: bow held low and slanting in the right hand, left arm loose.
        var scout = CharacterSpec.Default(OutfitId.Scout);
        scout.body = new BodyShape { height = 1.56f, width = 0.85f, gender = Gender.Female };
        var scFit = Fit.Of(scout.body);
        var scoutModel = WithWeapon(HeroModel.Build(scout), HeroModel.BuildWeapon(scout), Joints.OffHand);
        var ready = new Pose();
        Vector3 scl = new Vector3(-scFit.shoulderX, scFit.shoulderY, 0f);
        Vector3 sclElbow = scl + new Vector3(-0.08f, -0.2f, 0.0f).normalized * scFit.upperArm;
        Vector3 sclFist = new Vector3(-scFit.shoulderX - 0.08f, scFit.waist - 0.14f, -0.02f);
        ready.worldRot[Joints.LeftArm] = Quaternion.FromToRotation(Vector3.down, sclElbow - scl);
        ready.worldRot[Joints.LeftForearm] = Quaternion.FromToRotation(Vector3.down, sclFist - sclElbow);
        ready.worldRot[Joints.OffHand] = Quaternion.LookRotation(new Vector3(-0.8f, 0.5f, -0.35f), new Vector3(-0.3f, -0.1f, 1f));
        ready.rot[Joints.RightArm] = Quaternion.Euler(4f, 0f, 12f);
        ready.rot[Joints.RightForearm] = Quaternion.Euler(-25f, 0f, 0f);
        ready.rot[Joints.LeftLeg] = Quaternion.Euler(0f, -16f, -9f);
        ready.rot[Joints.RightLeg] = Quaternion.Euler(0f, 18f, 9f);
        shots.Add(new Shot { label = "The Scout", model = scoutModel, pose = ready, yaw = 202f });
        shots.Add(new Shot { label = "Jarl, three-quarter", model = jarlModel, pose = stand, yaw = 215f });
        shots.Add(new Shot { label = "Navigator, three-quarter", model = navModel, pose = reading, yaw = 150f });
        // Same outfit on other bodies: the player chooses height, build and gender.
        var tall = CharacterSpec.Default(OutfitId.Raider);
        tall.body = new BodyShape { height = 1.85f, width = 1.25f, gender = Gender.Male };
        tall.palette = Outfits.Get(OutfitId.Raider).palette();
        tall.palette.hair = new Color(0.88f, 0.76f, 0.48f); tall.palette.accent = new Color(0.18f, 0.32f, 0.5f); tall.palette.cloth = new Color(0.33f, 0.29f, 0.24f);
        shots.Add(new Shot { label = "Tall, broad (custom colours)", model = HeroModel.Build(tall), yaw = 195f });
        var small = CharacterSpec.Default(OutfitId.Raider);
        small.body = new BodyShape { height = 1.45f, width = 0.85f, gender = Gender.Female };
        shots.Add(new Shot { label = "Short, slim", model = HeroModel.Build(small), yaw = 195f });
        // The people of the world, built from the same outfits (NpcHeroes).
        shots.Add(new Shot { label = "Saxon guard", model = Full(NpcHeroes.Saxon(3)), yaw = 195f });
        shots.Add(new Shot { label = "Danish raider", model = Full(NpcHeroes.DanishRaider(5)), yaw = 195f });
        shots.Add(new Shot { label = "Bjorn the mead-keeper", model = Full(NpcHeroes.Bjorn()), yaw = 195f });
        shots.Add(new Shot { label = "Gunnar the trader", model = Full(NpcHeroes.Gunnar()), yaw = 195f });
        // The in-game axe carry (HeroPose.AxeCarry), built from local joint rotations like the game does.
        var carryGame = new Pose();
        carryGame.rot[Joints.RightArm] = Quaternion.Euler(HeroPose.AxeCarryArm);
        carryGame.rot[Joints.RightForearm] = Quaternion.Euler(HeroPose.AxeCarryForearm);
        carryGame.rot[Joints.Weapon] = HeroPose.AxeCarryWeapon;
        shots.Add(new Shot { label = "Axe carry (in game)", model = Full(CharacterSpec.Default(OutfitId.Raider)), pose = carryGame, yaw = 200f });
        var spearPose = HeroPose.CarryFor(WeaponId.Spear);
        var carrySpear = new Pose();
        carrySpear.rot[Joints.RightArm] = Quaternion.Euler(spearPose.arm);
        carrySpear.rot[Joints.RightForearm] = Quaternion.Euler(spearPose.forearm);
        carrySpear.rot[Joints.Weapon] = HeroPose.WeaponInFist(spearPose);
        var guardSpec = CharacterSpec.Default(OutfitId.SpearGuard);
        var guardFull = Full(guardSpec);
        carrySpear.pos[Joints.Weapon] = guardFull.Find(Joints.Weapon).localPosition + HeroPose.GripSlide(spearPose, Fit.Of(guardSpec.body).s);
        shots.Add(new Shot { label = "Spear carry (in game)", model = guardFull, pose = carrySpear, yaw = 200f });
        foreach (var carried in new[] { OutfitId.Jarl, OutfitId.Scout, OutfitId.Seer })
        {
            var spec = CharacterSpec.Default(carried);
            var cp = HeroPose.CarryFor(spec.weapon);
            var pose = new Pose();
            pose.rot[Joints.RightArm] = Quaternion.Euler(cp.arm);
            pose.rot[Joints.RightForearm] = Quaternion.Euler(cp.forearm);
            pose.rot[Joints.Weapon] = HeroPose.WeaponInFist(cp);
            var full = Full(spec);
            pose.pos[Joints.Weapon] = full.Find(Joints.Weapon).localPosition + HeroPose.GripSlide(cp, Fit.Of(spec.body).s);
            shots.Add(new Shot { label = HeroChoice.Name(spec.weapon) + " carry (in game)", model = full, pose = pose, yaw = 200f });
        }
        // Faces: the dash eyes turn to ^ ^ when happy and > < when hurt.
        shots.Add(new Shot { label = "Happy (heads!)", model = HeroModel.Build(CharacterSpec.Default(OutfitId.Scout)), yaw = 182f, face = Expression.Happy });
        shots.Add(new Shot { label = "Hurt", model = HeroModel.Build(CharacterSpec.Default(OutfitId.Navigator)), yaw = 182f, face = Expression.Hurt });

        const int cellW = 680, cellH = 1080; // 2x supersampled
        int w = cellW * shots.Count, h = cellH;
        var img = new float[w * h * 3];
        for (int i = 0; i < w * h; i++) { img[i * 3] = Paper.r; img[i * 3 + 1] = Paper.g; img[i * 3 + 2] = Paper.b; }
        for (int s = 0; s < shots.Count; s++) Render(img, w, h, s * cellW, cellW, cellH, shots[s]);

        using (var f = new BinaryWriter(File.Create(args[0])))
        {
            f.Write(w); f.Write(h);
            for (int i = 0; i < w * h; i++)
            {
                f.Write((byte)(Mathf.Clamp01(img[i * 3]) * 255)); f.Write((byte)(Mathf.Clamp01(img[i * 3 + 1]) * 255)); f.Write((byte)(Mathf.Clamp01(img[i * 3 + 2]) * 255)); f.Write((byte)255);
            }
        }
        var labels = new List<string>();
        foreach (var s in shots) labels.Add(s.label);
        File.WriteAllLines(args[1], labels.ToArray());
        if (args.Length > 4) MotionStrip(args[3], args[4], jarlModel);
        if (args.Length > 6) SkinSheet(args[5], args[6]);
        if (args.Length > 8) TurnSheet(args[7], args[8]);
        Directory.CreateDirectory(args[2]);
        ExportObj(HeroModel.Build(raider), Path.Combine(args[2], "raider.obj"));
        ExportObj(HeroModel.BuildWeapon(raider), Path.Combine(args[2], "two-hand-axe.obj"));
        Console.WriteLine("hero: " + model.TriangleCount + " triangles");
    }

    /// <summary>
    /// The Jarl running and stopping, seen from the side: the swinging joints are simulated with the same springs the
    /// game uses, so the strip shows how capes and braids really move.
    /// </summary>
    static void MotionStrip(string rgbaPath, string labelPath, VikingModel model)
    {
        var springs = new Dictionary<string, SwingSpring>();
        foreach (var sw in model.Swings) springs[sw.joint] = SwingSpring.For(sw.kind);
        var frames = new List<Shot>();
        var captures = new[] { 0.2f, 0.75f, 2.2f, 2.5f, 2.8f, 4.0f };
        var names = new[] { "Standing", "Setting off", "Running", "Stopping", "Swinging back", "Settled" };
        float speed = 0f, t = 0f, dt = 0.01f, stride = 0f;
        int next = 0;
        while (next < captures.Length)
        {
            float want = t > 0.3f && t < 2.3f ? 5f : 0f;
            float prev = speed;
            speed = want > speed ? Mathf.Min(want, speed + 12f * dt) : Mathf.Max(want, speed - 30f * dt);
            float accel = (speed - prev) / dt;
            foreach (var s in springs.Values) s.Step(dt, new Vector3(0f, 0f, speed), new Vector3(0f, 0f, accel));
            stride += speed * dt * 2.2f;
            t += dt;
            if (t >= captures[next])
            {
                var pose = new Pose();
                foreach (var kv in springs) pose.rot[kv.Key] = kv.Value.Rotation;
                float swing = Mathf.Sin(stride) * Mathf.Clamp01(speed / 5f) * 35f;
                pose.rot[Joints.LeftLeg] = Quaternion.Euler(swing, 0f, 0f);
                pose.rot[Joints.RightLeg] = Quaternion.Euler(-swing, 0f, 0f);
                pose.rot[Joints.LeftArm] = Quaternion.Euler(-swing * 0.8f, 0f, -8f);
                pose.rot[Joints.RightArm] = Quaternion.Euler(swing * 0.8f, 0f, 8f);
                pose.rot[Joints.Body] = Quaternion.Euler(Mathf.Clamp01(speed / 5f) * 8f, 0f, 0f);
                frames.Add(new Shot { label = names[next], model = model, pose = pose, yaw = 265f });
                next++;
            }
        }
        const int cellW = 600, cellH = 1080;
        int w = cellW * frames.Count, h = cellH;
        var img = new float[w * h * 3];
        for (int i = 0; i < w * h; i++) { img[i * 3] = Paper.r; img[i * 3 + 1] = Paper.g; img[i * 3 + 2] = Paper.b; }
        for (int f = 0; f < frames.Count; f++) Render(img, w, h, f * cellW, cellW, cellH, frames[f]);
        using (var fs = new BinaryWriter(File.Create(rgbaPath)))
        {
            fs.Write(w); fs.Write(h);
            for (int i = 0; i < w * h; i++)
            {
                fs.Write((byte)(Mathf.Clamp01(img[i * 3]) * 255)); fs.Write((byte)(Mathf.Clamp01(img[i * 3 + 1]) * 255)); fs.Write((byte)(Mathf.Clamp01(img[i * 3 + 2]) * 255)); fs.Write((byte)255);
            }
        }
        var labels = new List<string>();
        foreach (var f in frames) labels.Add(f.label);
        File.WriteAllLines(labelPath, labels.ToArray());
    }

    /// <summary>Every outfit (rows) from the front, three-quarter, side and back (columns), for docs/turnaround.png.</summary>
    static void TurnSheet(string rgbaPath, string labelPath)
    {
        var outfits = (OutfitId[])Enum.GetValues(typeof(OutfitId));
        float[] yaws = { 180f, 215f, 270f, 0f };
        string[] names = { "front", "three-quarter", "side", "back" };
        const int cellW = 520, cellH = 900;
        int cols = yaws.Length, rows = outfits.Length, w = cellW * cols, h = cellH * rows;
        var img = new float[w * h * 3];
        var labels = new List<string> { "cols=" + cols };
        for (int r = 0; r < rows; r++)
        {
            var row = new float[w * cellH * 3];
            for (int i = 0; i < w * cellH; i++) { row[i * 3] = Paper.r; row[i * 3 + 1] = Paper.g; row[i * 3 + 2] = Paper.b; }
            var model = Full(CharacterSpec.Default(outfits[r]));
            for (int c = 0; c < cols; c++)
            {
                Render(row, w, cellH, c * cellW, cellW, cellH, new Shot { model = model, yaw = yaws[c] });
                labels.Add(Outfits.Get(outfits[r]).title.Replace("The ", "") + ", " + names[c]);
            }
            Array.Copy(row, 0, img, r * w * cellH * 3, row.Length);
        }
        using (var f = new BinaryWriter(File.Create(rgbaPath)))
        {
            f.Write(w); f.Write(h);
            for (int i = 0; i < w * h; i++)
            {
                f.Write((byte)(Mathf.Clamp01(img[i * 3]) * 255)); f.Write((byte)(Mathf.Clamp01(img[i * 3 + 1]) * 255)); f.Write((byte)(Mathf.Clamp01(img[i * 3 + 2]) * 255)); f.Write((byte)255);
            }
        }
        File.WriteAllLines(labelPath, labels.ToArray());
    }

    /// <summary>Every outfit (columns) in every one of its skins (rows), for docs/skins.png.</summary>
    static void SkinSheet(string rgbaPath, string labelPath)
    {
        var outfits = (OutfitId[])Enum.GetValues(typeof(OutfitId));
        int rows = 0;
        foreach (var o in outfits) rows = Math.Max(rows, Skins.For(o).Count);
        const int cellW = 520, cellH = 900;
        int cols = outfits.Length, w = cellW * cols, h = cellH * rows;
        var img = new float[w * h * 3];
        for (int i = 0; i < w * h; i++) { img[i * 3] = Paper.r; img[i * 3 + 1] = Paper.g; img[i * 3 + 2] = Paper.b; }
        var labels = new List<string> { "cols=" + cols };
        for (int r = 0; r < rows; r++)
        {
            var row = new float[w * cellH * 3];
            for (int i = 0; i < w * cellH; i++) { row[i * 3] = Paper.r; row[i * 3 + 1] = Paper.g; row[i * 3 + 2] = Paper.b; }
            for (int c = 0; c < cols; c++)
            {
                var list = Skins.For(outfits[c]);
                if (r >= list.Count) { labels.Add(""); continue; }
                var spec = CharacterSpec.Default(outfits[c]);
                spec.skin = list[r].IsClassic ? null : list[r].id;
                Render(row, w, cellH, c * cellW, cellW, cellH, new Shot { label = list[r].name, model = Full(spec), yaw = 195f });
                labels.Add(Outfits.Get(outfits[c]).title.Replace("The ", "") + ": " + list[r].name + (list[r].rare ? " (rare)" : list[r].cost > 0 ? " (" + list[r].cost + " gold)" : ""));
            }
            Array.Copy(row, 0, img, r * w * cellH * 3, row.Length);
        }
        using (var f = new BinaryWriter(File.Create(rgbaPath)))
        {
            f.Write(w); f.Write(h);
            for (int i = 0; i < w * h; i++)
            {
                f.Write((byte)(Mathf.Clamp01(img[i * 3]) * 255)); f.Write((byte)(Mathf.Clamp01(img[i * 3 + 1]) * 255)); f.Write((byte)(Mathf.Clamp01(img[i * 3 + 2]) * 255)); f.Write((byte)255);
            }
        }
        File.WriteAllLines(labelPath, labels.ToArray());
    }

    /// <summary>Whether a piece is drawn: hidden joints are skipped, except the eyes of the shot's expression.</summary>
    static bool Visible(Shot shot, string joint)
    {
        if (joint == Joints.Eyes) return shot.face == Expression.Neutral;
        if (joint == Joints.EyesHappy) return shot.face == Expression.Happy;
        if (joint == Joints.EyesHurt) return shot.face == Expression.Hurt;
        return !shot.model.Hidden(joint);
    }

    /// <summary>A character with its weapon and off-hand item in its hands.</summary>
    static VikingModel Full(CharacterSpec spec)
    {
        var m = HeroModel.Build(spec);
        if (spec.weapon != WeaponId.None) WithWeapon(m, HeroModel.BuildWeapon(spec), Joints.Weapon);
        if (spec.offHand != OffHandId.None) WithWeapon(m, HeroModel.BuildOffHand(spec), Joints.OffHand);
        return m;
    }

    /// <summary>For pictures only: hang the separately built weapon on the character's weapon hand.</summary>
    static VikingModel WithWeapon(VikingModel character, VikingModel weapon, string hand)
    {
        foreach (var p in weapon.Pieces)
            character.Pieces.Add(new VikingModel.Piece { joint = hand, color = p.color, mesh = p.mesh, outline = p.outline, ink = p.ink, surface = p.surface });
        return character;
    }

    static void World(VikingModel m, Pose pose, string joint, out Vector3 pos, out Quaternion rot)
    {
        var j = m.Find(joint);
        Vector3 local = pose.pos.ContainsKey(joint) ? pose.pos[joint] : j.localPosition;
        Quaternion own = pose.rot.ContainsKey(joint) ? pose.rot[joint] : Quaternion.Euler(j.restEuler);
        if (j.parent == null) { pos = local; rot = own; return; }
        Vector3 pp; Quaternion pr;
        World(m, pose, j.parent, out pp, out pr);
        pos = pp + pr * local;
        rot = pose.worldRot.ContainsKey(joint) ? pose.worldRot[joint] : pr * own;
    }

    static void Render(float[] img, int w, int h, int x0, int cellW, int cellH, Shot shot)
    {
        var cam = Quaternion.Euler(8f, shot.yaw, 0f);
        var inv = new Quaternion(-cam.x, -cam.y, -cam.z, cam.w);
        Vector3 forward = cam * Vector3.forward;
        Vector3 light = new Vector3(-0.5f, 0.75f, 0.45f).normalized;
        float scale = cellH / 2.45f, cx = x0 + cellW / 2f, groundY = cellH * 0.92f;
        var depth = new float[cellW * cellH];
        for (int i = 0; i < depth.Length; i++) depth[i] = float.MaxValue;

        // Soft oval shadow on the ground.
        for (int y = 0; y < cellH; y++)
            for (int x = 0; x < cellW; x++)
            {
                float dx = (x - cellW / 2f) / (cellW * 0.3f), dy = (y - groundY) / (cellH * 0.022f);
                float d = dx * dx + dy * dy;
                if (d >= 1f) continue;
                int i = (y * w + x0 + x) * 3;
                float k = 1f - 0.22f * (1f - d);
                img[i] *= k; img[i + 1] *= k; img[i + 2] *= k;
            }

        foreach (var piece in shot.model.Pieces)
        {
            if (!Visible(shot, piece.joint)) continue;
            Vector3 jp; Quaternion jr;
            World(shot.model, shot.pose, piece.joint, out jp, out jr);
            var mesh = piece.mesh;
            mesh.FillUvs();
            var tex = piece.ink || piece.surface == SurfaceKind.Plain ? null : DrawnTextures.Get(piece.surface);
            int n = mesh.Vertices.Count;
            var world = new Vector3[n];
            var normal = new Vector3[n];
            for (int i = 0; i < n; i++) world[i] = jp + jr * mesh.Vertices[i];
            for (int t = 0; t < mesh.Triangles.Count; t += 3)
            {
                int a = mesh.Triangles[t], b = mesh.Triangles[t + 1], c = mesh.Triangles[t + 2];
                Vector3 fn = Vector3.Cross(world[b] - world[a], world[c] - world[a]);
                normal[a] += fn; normal[b] += fn; normal[c] += fn;
            }
            var screen = new Vector3[n];
            for (int i = 0; i < n; i++)
            {
                normal[i] = normal[i].normalized;
                Vector3 cp = inv * (world[i] - new Vector3(0f, 0.9f, 0f));
                screen[i] = new Vector3(cx + cp.x * scale, groundY - (cp.y + 0.9f) * scale, cp.z);
            }
            for (int t = 0; t < mesh.Triangles.Count; t += 3)
            {
                int a = mesh.Triangles[t], b = mesh.Triangles[t + 1], c = mesh.Triangles[t + 2];
                Vector3 fn = Vector3.Cross(world[b] - world[a], world[c] - world[a]);
                if (Vector3.Dot(fn, forward) >= 0f) continue; // back face, culled like Unity
                Tri(img, depth, w, x0, cellW, cellH, screen[a], screen[b], screen[c], normal[a], normal[b], normal[c], mesh.Uvs[a], mesh.Uvs[b], mesh.Uvs[c], tex, InkStyle.HatchAmount(piece.surface), InkStyle.Flatness(piece.surface), piece.color, piece.ink, light, forward);
            }
        }
    }

    static void Tri(float[] img, float[] depth, int w, int x0, int cellW, int cellH, Vector3 a, Vector3 b, Vector3 c,
        Vector3 na, Vector3 nb, Vector3 nc, Vector2 ua, Vector2 ub, Vector2 uc, float[] tex, float hatch, float flat, Color color, bool ink, Vector3 light, Vector3 forward)
    {
        int minX = Math.Max(x0, (int)Math.Floor(Math.Min(a.x, Math.Min(b.x, c.x))));
        int maxX = Math.Min(x0 + cellW - 1, (int)Math.Ceiling(Math.Max(a.x, Math.Max(b.x, c.x))));
        int minY = Math.Max(0, (int)Math.Floor(Math.Min(a.y, Math.Min(b.y, c.y))));
        int maxY = Math.Min(cellH - 1, (int)Math.Ceiling(Math.Max(a.y, Math.Max(b.y, c.y))));
        float area = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        if (Math.Abs(area) < 1e-7f) return;
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float w0 = ((b.x - px) * (c.y - py) - (b.y - py) * (c.x - px)) / area;
                float w1 = ((c.x - px) * (a.y - py) - (c.y - py) * (a.x - px)) / area;
                float w2 = 1f - w0 - w1;
                if (w0 < 0f || w1 < 0f || w2 < 0f) continue;
                float z = a.z * w0 + b.z * w1 + c.z * w2;
                int di = y * cellW + (x - x0);
                if (z >= depth[di]) continue;
                depth[di] = z;
                float r = color.r, g = color.g, bl = color.b;
                if (tex != null)
                {
                    float k = DrawnTextures.Sample(tex, ua.x * w0 + ub.x * w1 + uc.x * w2, ua.y * w0 + ub.y * w1 + uc.y * w2);
                    r *= k; g *= k; bl *= k;
                }
                if (!ink)
                {
                    // The game's storybook light (InkStyle / InkToon.shader): wrapped diffuse, cool shadows, warm
                    // highlights, pencil hatching on the shadowed side. The picture is drawn at twice the size and
                    // halved, so the strokes are twice as far apart here.
                    Vector3 nrm = (na * w0 + nb * w1 + nc * w2).normalized;
                    float d = InkStyle.Tone(nrm, light);
                    float shade = InkStyle.Shade(d, flat) * Mathf.Lerp(1f, InkStyle.Hatch(d, x, y, InkStyle.Spacing * 2f), hatch) * InkStyle.Paper(x * 0.5f, y * 0.5f);
                    r = r * shade * (0.96f + 0.06f * d); g *= shade; bl = bl * shade * (1.04f - 0.06f * d);
                }
                int i = (y * w + x) * 3;
                img[i] = r; img[i + 1] = g; img[i + 2] = bl;
            }
    }

    /// <summary>Wavefront .obj + .mtl, X mirrored for OBJ's right-handed axes (Unity mirrors it back on import).</summary>
    public static void ExportObj(VikingModel m, string path)
    {
        var inv = CultureInfo.InvariantCulture;
        string mtlPath = Path.ChangeExtension(path, ".mtl");
        using (var mtl = new StreamWriter(mtlPath))
        using (var obj = new StreamWriter(path))
        {
            obj.WriteLine("# Odin's Coin hero, generated by HeroModel.cs. 1 unit = 1 metre, feet at y = 0, facing +Z.");
            obj.WriteLine("mtllib " + Path.GetFileName(mtlPath));
            int offset = 1, index = 0;
            foreach (var p in m.Pieces)
            {
                if (p.ink) continue; // outline shells are a rendering trick, not part of the model
                string name = "m" + index++;
                mtl.WriteLine("newmtl " + name);
                mtl.WriteLine(string.Format(inv, "Kd {0:0.###} {1:0.###} {2:0.###}", p.color.r, p.color.g, p.color.b));
                mtl.WriteLine("Ka 0 0 0\nKs 0.03 0.03 0.03\nNs 8\nd 1\nillum 2\n");
                obj.WriteLine("o " + p.joint.Replace(' ', '_') + "_" + index);
                obj.WriteLine("usemtl " + name);
                Vector3 at = m.RestPosition(p.joint);
                foreach (var v in p.mesh.Vertices)
                {
                    Vector3 wv = at + v;
                    obj.WriteLine(string.Format(inv, "v {0:0.#####} {1:0.#####} {2:0.#####}", -wv.x, wv.y, wv.z));
                }
                for (int t = 0; t < p.mesh.Triangles.Count; t += 3)
                    obj.WriteLine("f " + (p.mesh.Triangles[t] + offset) + " " + (p.mesh.Triangles[t + 1] + offset) + " " + (p.mesh.Triangles[t + 2] + offset));
                offset += p.mesh.Vertices.Count;
            }
        }
    }
}
