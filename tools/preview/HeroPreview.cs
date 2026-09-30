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
        /// <summary>Capes and braids bent by a swing (pitch, roll in degrees), as the game's ClothSway bends them.</summary>
        public Dictionary<string, Vector2> bend = new Dictionary<string, Vector2>();
        /// <summary>A bow's string drawn back: the pull on its middle, in the string joint's space (as BowString does).</summary>
        public Dictionary<string, Vector3> pull = new Dictionary<string, Vector3>();
        /// <summary>Joints hidden at rest but shown in this pose (a nocked arrow).</summary>
        public HashSet<string> show = new HashSet<string>();
    }

    public class Shot
    {
        public string label;
        public VikingModel model;
        public Pose pose = new Pose();
        public float yaw = 200f;
        public Expression face = Expression.Neutral;
        /// <summary>Less than 1 draws the hero smaller, leaving room for big swings.</summary>
        public float zoom = 1f;
        /// <summary>How far the camera looks down (degrees).</summary>
        public float pitch = 8f;
        /// <summary>A real perspective camera at <see cref="eye"/> (first person), instead of the drawn front view.</summary>
        public bool perspective;
        public Vector3 eye;
        public float fov = 75f;
        /// <summary>Joints not drawn (the hero's own head, seen from inside it).</summary>
        public HashSet<string> hide = new HashSet<string>();
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
        // The free arm hangs down held a little out from the side, fist by the hip, as in the concept art.
        carry.rot[Joints.RightArm] = Quaternion.Euler(3f, 0f, 24f);
        carry.rot[Joints.RightForearm] = Quaternion.Euler(-8f, 0f, -4f);
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
        stand.rot[Joints.RightArm] = Quaternion.Euler(-8f, 0f, 22f);
        stand.rot[Joints.RightForearm] = Quaternion.Euler(-18f, 0f, -12f);
        stand.worldRot[Joints.Weapon] = Quaternion.LookRotation(new Vector3(0.35f, -1f, 0.6f).normalized, Vector3.forward);
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
        Vector3 lFist = new Vector3(-0.2f, navFit.chest - 0.04f, 0.22f);
        Vector3 rElbow = rShoulder + new Vector3(0.02f, -0.22f, 0.08f).normalized * navFit.upperArm;
        Vector3 rFist = new Vector3(-0.02f, navFit.chest - 0.08f, 0.24f);
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
        seer.body = new BodyShape { height = 1.7f, width = 0.9f, gender = Gender.Female };
        var sFit = Fit.Of(seer.body);
        var seerModel = WithWeapon(HeroModel.Build(seer), HeroModel.BuildWeapon(seer), Joints.OffHand);
        var augur = new Pose();
        Vector3 sl = new Vector3(-sFit.shoulderX, sFit.shoulderY, 0f), sr = new Vector3(sFit.shoulderX, sFit.shoulderY, 0f);
        Vector3 slElbow = sl + new Vector3(-0.1f, -0.2f, 0.02f).normalized * sFit.upperArm;
        Vector3 slFist = new Vector3(-sFit.shoulderX - 0.16f, sFit.chest - 0.02f, 0.06f);
        Vector3 srElbow = sr + new Vector3(0.06f, -0.2f, 0.06f).normalized * sFit.upperArm;
        Vector3 srFist = new Vector3(sFit.shoulderX + 0.02f, sFit.waist - 0.04f, 0.12f);
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
        // Skin tones, chosen on the hero screen apart from the clothes' colours.
        for (int t = 0; t < SkinTones.Count; t++)
        {
            var toned = CharacterSpec.Default(OutfitId.Scout);
            toned.skinTone = t;
            shots.Add(new Shot { label = "Skin: " + SkinTones.Name(t), model = HeroModel.Build(toned), yaw = 190f });
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
        if (args.Length > 10) AttackSheet(args[9], args[10]);
        if (args.Length > 11) FootstepTrace(args[11]);
        if (args.Length > 13) ShipSheet(args[12], args[13]);
        if (args.Length > 15) BuildingSheet(args[14], args[15]);
        if (args.Length > 16) HarbourScene(args[16]);
        if (args.Length > 18) FirstPersonSheet(args[17], args[18]);
        Directory.CreateDirectory(args[2]);
        ExportObj(HeroModel.Build(raider), Path.Combine(args[2], "raider.obj"));
        ExportObj(HeroModel.BuildWeapon(raider), Path.Combine(args[2], "two-hand-axe.obj"));
        Console.WriteLine("hero: " + model.TriangleCount + " triangles");
    }

    /// <summary>A pose from the game's animator (see HeroAnimator.Apply), on top of the standing posture.</summary>
    static Pose FromAnimator(HeroAnimator.Frame f)
    {
        var pose = new Pose();
        pose.rot[Joints.Body] = Quaternion.Euler(f.body);
        pose.pos[Joints.Body] = new Vector3(0f, f.lift, 0f);
        pose.rot[Joints.LeftLeg] = Quaternion.Euler(f.leftLeg);
        pose.rot[Joints.RightLeg] = Quaternion.Euler(f.rightLeg);
        pose.rot[Joints.Head] = Quaternion.Euler(f.head);
        pose.rot[Joints.LeftArm] = Quaternion.Euler(f.leftArm);
        pose.rot[Joints.RightArm] = Quaternion.Euler(f.rightArm);
        pose.rot[Joints.LeftForearm] = Quaternion.Euler(f.leftElbow, 0f, 0f);
        pose.rot[Joints.RightForearm] = Quaternion.Euler(f.rightElbow, 0f, 0f);
        pose.rot[Joints.LeftShin] = Quaternion.Euler(f.leftKnee, 0f, 0f);
        pose.rot[Joints.RightShin] = Quaternion.Euler(f.rightKnee, 0f, 0f);
        pose.rot[Joints.LeftFoot] = Quaternion.Euler(f.leftAnkle, 0f, 0f);
        pose.rot[Joints.RightFoot] = Quaternion.Euler(f.rightAnkle, 0f, 0f);
        return pose;
    }

    /// <summary>
    /// The Jarl through a little run, driven by the game's own footstep locomotion and animator: setting off,
    /// walking, sprinting, banking into a turn (seen from the front), a jump, the landing and the stop. The swinging
    /// joints are simulated with the same springs the game uses, so capes and braids move as they will in play.
    /// </summary>
    static void MotionStrip(string rgbaPath, string labelPath, VikingModel model)
    {
        var springs = new Dictionary<string, SwingSpring>();
        var bent = new HashSet<string>();
        foreach (var sw in model.Swings) { springs[sw.joint] = SwingSpring.For(sw.kind); if (sw.kind != SwingKind.Banner) bent.Add(sw.joint); }
        var frames = new List<Shot>();
        var captures = new[] { 0.2f, 1.30f, 1.39f, 1.48f, 1.57f, 2.40f, 2.47f, 2.54f, 2.61f, 2.9f, 3.14f, 3.2f, 3.38f, 3.56f, 3.66f, 3.76f, 4.9f };
        var names = new[] { "Standing", "Walk 1", "Walk 2", "Walk 3", "Walk 4", "Run 1", "Run 2", "Run 3", "Run 4", "Banking into a turn",
            "Crouch to jump", "Jump: push-off", "Tucked at the top", "Reaching down", "Landing", "Soaking it up", "Stopping" };
        var loco = new Locomotion { sprintSpeed = 6.5f };
        loco.Reset(Vector2.zero, 0f);
        var anim = new HeroAnimator();
        float t = 0f, dt = 1f / 120f, y = 0f, vy = 0f;
        bool jumped = false;
        int next = 0;
        while (next < captures.Length)
        {
            // The script: walk, sprint, swing right, jump, keep running, let go.
            Vector2 wish = t < 0.3f ? Vector2.zero : t < 4.3f ? new Vector2(0f, 1f) : Vector2.zero;
            if (t > 2.6f && t < 3.0f) wish = new Vector2(1f, 1f);
            // Walk frames are a real stroll (Ctrl), the run frames a sprint (Shift).
            float maxSpeed = t > 1.6f ? 6.5f : 1.7f;
            // The crouch first (as in the game), then the leap.
            anim.crouch = t >= 3.05f && t < 3.15f ? (t - 3.05f) / 0.1f : 0f;
            if (!jumped && t >= 3.15f) { jumped = true; vy = 5.5f; }
            bool grounded = y <= 0f && vy <= 0f;
            if (grounded) loco.Step(wish, maxSpeed, dt); else loco.Air(wish, dt);
            float vyBefore = vy;
            vy -= 18f * dt;
            y += vy * dt;
            if (y <= 0f) { y = 0f; if (vy < 0f && !grounded) { } }
            grounded = y <= 0f;
            anim.Step(dt, loco, grounded, vy, false, t);
            if (grounded) vy = Mathf.Max(vy, 0f);
            // The springs feel the jump too, as in the game (which clamps the landing jolt the same way).
            float ay = Mathf.Clamp((vy - vyBefore) / dt, -60f, 60f);
            foreach (var s in springs.Values) s.Step(dt, new Vector3(0f, grounded ? 0f : vy, loco.speed), new Vector3(0f, ay, loco.acceleration));
            t += dt;
            if (t >= captures[next])
            {
                var pose = FromAnimator(anim.pose);
                // Capes and braids bend (as in the game), banners swing stiffly.
                foreach (var kv in springs)
                    if (bent.Contains(kv.Key)) pose.bend[kv.Key] = new Vector2(kv.Value.pitch, kv.Value.roll);
                    else pose.rot[kv.Key] = kv.Value.Rotation;
                // (The jump is drawn at half height so the whole hero stays in the frame.)
                pose.pos[Joints.Body] += new Vector3(0f, y * 0.5f, 0f);
                // Side-on, except the turn, which is seen from the front to show the bank and the head looking round.
                frames.Add(new Shot { label = names[next], model = model, pose = pose, yaw = next == 9 ? 180f : 265f });
                next++;
            }
        }
        const int cellW = 580, cellH = 1080;
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

    /// <summary>
    /// Footprints seen from above, from the game's own Locomotion, for docs/footsteps.png: a walking turn, a
    /// sprinting turn, a U-turn at a sprint and a turn from standing. Lines: "scenario name", then "body x z" and
    /// "foot x z heading L|R" rows.
    /// </summary>
    static void FootstepTrace(string path)
    {
        var lines = new List<string>();
        var scenarios = new[] {
            new { name = "Walking, then a right turn", speed = 4.2f, first = new Vector2(0f, 1f), then = new Vector2(1f, 0f), before = 1.4f, after = 1.8f },
            new { name = "Sprinting, then a right turn", speed = 6.5f, first = new Vector2(0f, 1f), then = new Vector2(1f, 0f), before = 1.4f, after = 1.6f },
            new { name = "U-turn at a sprint", speed = 6.5f, first = new Vector2(0f, 1f), then = new Vector2(0f, -1f), before = 1.4f, after = 1.8f },
            new { name = "Turning round from standing", speed = 4.2f, first = Vector2.zero, then = new Vector2(0f, -1f), before = 0.3f, after = 1.5f } };
        const float dt = 1f / 120f;
        foreach (var sc in scenarios)
        {
            lines.Add("scenario " + sc.name);
            var l = new Locomotion { sprintSpeed = 6.5f };
            l.Reset(Vector2.zero, 0f);
            int steps = l.stepsTaken, n = 0;
            for (float t = 0f; t < sc.before + sc.after; t += dt)
            {
                l.Step(t < sc.before ? sc.first : sc.then, sc.speed, dt);
                if (n++ % 6 == 0) lines.Add("body " + F(l.position.x) + " " + F(l.position.y));
                if (l.stepsTaken != steps)
                {
                    steps = l.stepsTaken;
                    lines.Add("foot " + F(l.planted.at.x) + " " + F(l.planted.at.y) + " " + F(l.planted.heading) + " " + (l.planted.left ? "L" : "R"));
                }
            }
        }
        File.WriteAllLines(path, lines.ToArray());
    }

    /// <summary>The ship classes side by side (docs/ships.png), each framed to fit, keel on the ground line.</summary>
    static void ShipSheet(string rgbaPath, string labelPath)
    {
        const int cellW = 1000, cellH = 760;
        var designs = ShipDesign.All;
        int w = cellW * designs.Length, h = cellH;
        // Two rows: each ship framed to fill its cell, and below, all four to the same scale (the Krakenhall's).
        var rows = new[] { new float[w * h * 3], new float[w * h * 3] };
        foreach (var img0 in rows) for (int i = 0; i < w * h; i++) { img0[i * 3] = Paper.r; img0[i * 3 + 1] = Paper.g; img0[i * 3 + 2] = Paper.b; }
        var labels = new List<string>();
        var zooms = new float[designs.Length];
        float common = float.MaxValue;
        for (int k = 0; k < designs.Length; k++)
        {
            var d = designs[k];
            float mast = 0f;
            foreach (var s in d.sails) mast = Mathf.Max(mast, s.height * 1.75f * 1.05f + (d.length >= 25f ? 0.12f * d.length : 0f));
            float top = mast + d.draught + d.freeboard;
            zooms[k] = Mathf.Min(0.84f * 2.45f / (top * 1.12f), 0.8f * cellW * 2.45f / (cellH * d.length * 1.25f));
            common = Mathf.Min(common, zooms[k]);
        }
        for (int k = 0; k < designs.Length; k++)
        {
            var d = designs[k];
            var pose = new Pose();
            pose.pos[ShipModel.Joint] = new Vector3(0f, d.draught, 0f);
            var model = ShipModel.Build(d, new ShipLook());
            Render(rows[0], w, h, k * cellW, cellW, cellH, new Shot { model = model, pose = pose, yaw = 235f, zoom = zooms[k] });
            Render(rows[1], w, h, k * cellW, cellW, cellH, new Shot { model = model, pose = pose, yaw = 235f, zoom = common });
            labels.Add(d.title + " (" + d.length.ToString("0") + " m, " + (d.Mass / 1000f).ToString("0") + " t)");
        }
        h = cellH * 2;
        var img = new float[w * h * 3];
        System.Array.Copy(rows[0], 0, img, 0, rows[0].Length);
        System.Array.Copy(rows[1], 0, img, rows[0].Length, rows[1].Length);
        using (var fs = new BinaryWriter(File.Create(rgbaPath)))
        {
            fs.Write(w); fs.Write(h);
            for (int i = 0; i < w * h; i++)
            {
                fs.Write((byte)(Mathf.Clamp01(img[i * 3]) * 255)); fs.Write((byte)(Mathf.Clamp01(img[i * 3 + 1]) * 255)); fs.Write((byte)(Mathf.Clamp01(img[i * 3 + 2]) * 255)); fs.Write((byte)255);
            }
        }
        File.WriteAllLines(labelPath, labels.ToArray());
    }

    /// <summary>The buildings side by side (docs/buildings.png), each framed to fit.</summary>
    static void BuildingSheet(string rgbaPath, string labelPath)
    {
        const int cellW = 700, cellH = 620;
        var kinds = new[] { BuildingKind.GreatHall, BuildingKind.Longhouse, BuildingKind.Boathouse, BuildingKind.Storehouse, BuildingKind.Watchtower, BuildingKind.Palisade, BuildingKind.Jetty, BuildingKind.Church };
        var names = new[] { "Great hall", "Longhouse", "Boathouse", "Storehouse", "Watchtower", "Palisade", "Jetty", "Church" };
        int w = cellW * kinds.Length, h = cellH;
        var img = new float[w * h * 3];
        for (int i = 0; i < w * h; i++) { img[i * 3] = Paper.r; img[i * 3 + 1] = Paper.g; img[i * 3 + 2] = Paper.b; }
        for (int k = 0; k < kinds.Length; k++)
        {
            var f = Buildings.Footprint(kinds[k]);
            float size = Mathf.Max(f.y * 1.25f, Mathf.Max(f.x, f.z) * 1.6f);
            float zoom = 0.84f * 2.45f / size;
            var pose = new Pose();
            pose.pos[Buildings.Joint] = new Vector3(0f, kinds[k] == BuildingKind.Jetty ? 4f : 0f, 0f);
            Render(img, w, h, k * cellW, cellW, cellH, new Shot { model = Buildings.Build(kinds[k], new BuildingLook()), pose = pose, yaw = 215f, zoom = zoom });
        }
        using (var fs = new BinaryWriter(File.Create(rgbaPath)))
        {
            fs.Write(w); fs.Write(h);
            for (int i = 0; i < w * h; i++)
            {
                fs.Write((byte)(Mathf.Clamp01(img[i * 3]) * 255)); fs.Write((byte)(Mathf.Clamp01(img[i * 3 + 1]) * 255)); fs.Write((byte)(Mathf.Clamp01(img[i * 3 + 2]) * 255)); fs.Write((byte)255);
            }
        }
        File.WriteAllLines(labelPath, names);
    }

    /// <summary>
    /// A real place as it might look in the game (docs/scene-kaupang.png): the land from the real map, the
    /// settlement's buildings where they're laid out, the jetty, and a Wavewolf lying alongside it.
    /// </summary>
    static void HarbourScene(string rgbaPath)
    {
        var mapPath = "OdinsCoin/Assets/Resources/World/north.bytes";
        if (!File.Exists(mapPath)) return;
        var map = WorldMap.FromBytes(File.ReadAllBytes(mapPath));
        map.Detail = WorldDetail.FromBytes(File.ReadAllBytes("OdinsCoin/Assets/Resources/World/coast.bytes"));
        Places.LoadHarbours(File.ReadAllText("OdinsCoin/Assets/Resources/World/harbours.txt"));
        var place = Places.Find("Kaupang");
        var plots = Settlements.Layout(map, place);
        var jetty = plots[0];
        var jettyTurn = Quaternion.Euler(0f, jetty.yaw, 0f);
        var centre = jetty.at + jettyTurn * new Vector3(0f, 0f, 20f);
        const string J = "Scene";
        var m = new VikingModel();
        m.AddJoint(J, null, Vector3.zero);
        // The land: a patch of the real terrain round the jetty.
        const float half = 230f;
        var patch = TerrainPatch.Build(map, centre.x - half, centre.z - half, half * 2f, 80);
        for (int k = 0; k < patch.Triangles.Length; k++)
        {
            if (k == (int)Ground.Seabed) continue;
            var md = new MeshData();
            foreach (int idx in patch.Triangles[k]) { md.Triangles.Add(md.Vertices.Count); md.Vertices.Add(patch.Vertices[idx] - new Vector3(half, 0f, half)); }
            m.Add(J, TerrainPatch.ColourOf((Ground)k), md, false, k == (int)Ground.Rock ? SurfaceKind.Plain : SurfaceKind.Plain);
        }
        // The sea.
        m.Add(J, new Color(0.36f, 0.52f, 0.6f), MeshData.Box(new Vector3(0f, -0.05f, 0f), new Vector3(half * 2f, 0.1f, half * 2f)), false);
        // The settlement.
        var look = new BuildingLook();
        int n = 0;
        foreach (var plot in plots)
        {
            var model = Buildings.Build(plot.kind, look, n++);
            var turn = Quaternion.Euler(0f, plot.yaw, 0f);
            var at = plot.at - centre;
            foreach (var piece in model.Pieces)
                m.Pieces.Add(new VikingModel.Piece { joint = J, color = piece.color, mesh = piece.mesh.Transformed(at, turn, Vector3.one), surface = piece.surface, outline = piece.outline, ink = piece.ink });
        }
        // Trees, rocks and grass, as the game scatters them, clear of the plots.
        Scenery.Clearings.Clear();
        foreach (var plot in plots)
            if (plot.kind != BuildingKind.Jetty) Scenery.Clearings.Add(new Vector3(plot.at.x, plot.at.z, Scenery.PlotClearing(plot.kind)));
        var t0 = WorldTerrain.ChunkOf(centre.x - half, centre.z - half, Scenery.TileSize);
        var t1 = WorldTerrain.ChunkOf(centre.x + half, centre.z + half, Scenery.TileSize);
        int propCount = 0;
        for (int tz = t0.y; tz <= t1.y; tz++)
            for (int tx = t0.x; tx <= t1.x; tx++)
            {
                var props = Scenery.Plan(tx, tz, (x, z) => SceneryField.GroundHeight(map, x, z), (x, z) => SceneryField.GroundKind(map, x, z));
                propCount += props.Count;
                foreach (var piece in Scenery.Model(props, centre.x, centre.z).Pieces)
                    m.Pieces.Add(new VikingModel.Piece { joint = J, color = piece.color, mesh = piece.mesh, surface = piece.surface, outline = piece.outline, ink = piece.ink });
            }
        Console.WriteLine("scenery props: " + propCount);
        // A Wavewolf alongside the jetty, bow to seaward.
        var wolf = ShipDesign.Wavewolf;
        var berth = jetty.at + jettyTurn * new Vector3(2f + wolf.beam / 2f + 0.5f, 0.2f, 22f) - centre;
        foreach (var piece in ShipModel.Build(wolf, new ShipLook()).Pieces)
            m.Pieces.Add(new VikingModel.Piece { joint = J, color = piece.color, mesh = piece.mesh.Transformed(berth, jettyTurn, Vector3.one), surface = piece.surface, outline = piece.outline, ink = piece.ink });

        const int w = 1800, h = 1000;
        var img = new float[w * h * 3];
        for (int i = 0; i < w * h; i++) { img[i * 3] = Paper.r; img[i * 3 + 1] = Paper.g; img[i * 3 + 2] = Paper.b; }
        var pose = new Pose();
        pose.pos[J] = new Vector3(0f, 0f, 0f);
        Render(img, w, h, 0, w, h, new Shot { model = m, pose = pose, yaw = jetty.yaw + 150f, zoom = 0.018f, pitch = 34f });
        WriteRgba(rgbaPath, img, w, h);

        // And as you'd see it in first person: standing at the shore end of the jetty, looking at the town.
        var town = Vector3.zero;
        for (int i = 1; i < plots.Count; i++) town += plots[i].at;
        if (plots.Count > 1) town /= plots.Count - 1;
        var eye = jetty.at - centre + new Vector3(0f, 2.3f, 0f);
        var toTown = town - jetty.at;
        float gaze = Mathf.Atan2(toTown.x, toTown.z) * Mathf.Rad2Deg - 25f;
        var fp = new float[w * h * 3];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                // A pale northern sky, brighter towards the horizon.
                float k = y / (float)h;
                int i = (y * w + x) * 3;
                fp[i] = Mathf.Lerp(0.62f, 0.9f, k); fp[i + 1] = Mathf.Lerp(0.74f, 0.9f, k); fp[i + 2] = Mathf.Lerp(0.86f, 0.88f, k);
            }
        Render(fp, w, h, 0, w, h, new Shot { model = m, pose = pose, perspective = true, eye = eye, yaw = gaze, pitch = 3f, fov = 70f });
        WriteRgba(rgbaPath + ".fp", fp, w, h);
    }

    static void WriteRgba(string path, float[] img, int w, int h)
    {
        using (var fs = new BinaryWriter(File.Create(path)))
        {
            fs.Write(w); fs.Write(h);
            for (int i = 0; i < w * h; i++)
            {
                fs.Write((byte)(Mathf.Clamp01(img[i * 3]) * 255)); fs.Write((byte)(Mathf.Clamp01(img[i * 3 + 1]) * 255)); fs.Write((byte)(Mathf.Clamp01(img[i * 3 + 2]) * 255)); fs.Write((byte)255);
            }
        }
    }

    static string F(float v) { return v.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture); }

    /// <summary>
    /// Every weapon's attack (rows) through its key moments (columns): ready, wind-up, the swing, the blow, the
    /// follow-through and back, as the game's HeroAttacks poses them, for docs/attacks.png.
    /// </summary>
    /// <summary>The hero at moment <paramref name="t"/> of an attack, as the game poses it.</summary>
    static Pose AttackPose(CharacterSpec spec, VikingModel model, Vector3 restGrip, HeroPose.CarryPose carry, AttackMove move, float t)
    {
        float wgt = move.Weight(t);
        var v = move.Sample(t);
        var pose = new Pose();
        // Under the move: the weapon carried as when walking (or plain rest).
        Quaternion baseArmR = carry.set ? Quaternion.Euler(carry.arm) : Quaternion.identity;
        Quaternion baseForeR = carry.set ? Quaternion.Euler(carry.forearm) : Quaternion.identity;
        var armR = Quaternion.Slerp(baseArmR, Quaternion.Euler(v[(int)AttackMove.Ch.ArmRX], 0f, v[(int)AttackMove.Ch.ArmRZ]), wgt);
        var foreR = Quaternion.Slerp(baseForeR, Quaternion.Euler(v[(int)AttackMove.Ch.ElbowR], 0f, 0f), wgt);
        float lw = move.twoHanded ? wgt : wgt * 0.5f;
        pose.rot[Joints.RightArm] = armR;
        pose.rot[Joints.RightForearm] = foreR;
        pose.rot[Joints.LeftArm] = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(v[(int)AttackMove.Ch.ArmLX], 0f, v[(int)AttackMove.Ch.ArmLZ]), lw);
        pose.rot[Joints.LeftForearm] = Quaternion.Slerp(Quaternion.identity, Quaternion.Euler(v[(int)AttackMove.Ch.ElbowL], 0f, 0f), lw);
        var baseWeapon = carry.set ? HeroPose.WeaponInFist(carry) : Quaternion.Euler(Weapons.RestEuler(spec.weapon));
        var haft = new Vector3(v[(int)AttackMove.Ch.HaftX], v[(int)AttackMove.Ch.HaftY], v[(int)AttackMove.Ch.HaftZ]);
        var inFist = HeroAttacks.WeaponRotation(armR, foreR, haft, v[(int)AttackMove.Ch.Roll]);
        pose.rot[Joints.Weapon] = Quaternion.Slerp(baseWeapon, inFist, wgt);
        var baseGrip = restGrip + (carry.set ? HeroPose.GripSlide(carry, Fit.Of(spec.body).s) : Vector3.zero);
        pose.pos[Joints.Weapon] = Vector3.Lerp(baseGrip, restGrip + inFist * Vector3.forward * (move.slide * Fit.Of(spec.body).s), wgt);
        pose.rot[Joints.Body] = Quaternion.Euler(v[(int)AttackMove.Ch.Pitch] * wgt, v[(int)AttackMove.Ch.Yaw] * wgt, 0f);
        pose.pos[Joints.Body] = new Vector3(0f, 0f, v[(int)AttackMove.Ch.Lunge] * wgt);
        pose.rot[Joints.LeftLeg] = Quaternion.Euler(-12f * wgt, 0f, -6f);
        pose.rot[Joints.RightLeg] = Quaternion.Euler(10f * wgt, 0f, 6f);
        // A bow's string drawn back to the hand, as the game does it.
        if (model.Find(Joints.BowString) != null)
        {
            float sc = Fit.Of(spec.body).s;
            Vector3 fp, bp; Quaternion fr, br;
            World(model, pose, Joints.LeftForearm, out fp, out fr);
            World(model, pose, Joints.BowString, out bp, out br);
            var hand = fp + fr * new Vector3(0f, -Fit.Of(spec.body).foreArm, 0f);
            var drawPull = BowDraw.Pull(Quaternion.Inverse(br) * (hand - bp), sc, BowDraw.Held(move, t));
            pose.pull[Joints.BowString] = drawPull;
            if (drawPull.sqrMagnitude > 1e-8f)
            {
                Vector3 nock; Quaternion arrowRot;
                BowDraw.Arrow(drawPull, sc, out nock, out arrowRot);
                pose.show.Add(Joints.NockedArrow);
                pose.pos[Joints.NockedArrow] = nock;
                pose.rot[Joints.NockedArrow] = arrowRot;
            }
        }
        return pose;
    }

    /// <summary>
    /// What you see in first person: out of the hero's eyes (the head hidden, as the game hides it), a Saxon
    /// guard in front, at the wind-up and at the blow of each hero's first attack.
    /// </summary>
    static void FirstPersonSheet(string rgbaPath, string labelPath)
    {
        var heroes = new[] { OutfitId.Raider, OutfitId.Jarl, OutfitId.SpearGuard, OutfitId.Scout };
        const int cellW = 960, cellH = 600;
        int cols = 2, w = cellW * cols, h = cellH * heroes.Length;
        var img = new float[w * h * 3];
        for (int i = 0; i < w * h; i++) { img[i * 3] = Paper.r; img[i * 3 + 1] = Paper.g; img[i * 3 + 2] = Paper.b; }
        var labels = new List<string> { "cols=" + cols };
        var foeSpec = NpcHeroes.Saxon(7);
        var foe = Full(foeSpec);
        string foeRoot = foe.Joints[0].name;
        for (int r = 0; r < heroes.Length; r++)
        {
            var spec = CharacterSpec.Default(heroes[r]);
            var model = Full(spec);
            var restGrip = model.Find(Joints.Weapon).localPosition;
            var fit = Fit.Of(spec.body);
            var hide = new HashSet<string>();
            foreach (var j in model.Joints)
            {
                // The head and everything hung from it (hat, hair, braids, eyes).
                for (var k = j; k != null; k = k.parent != null ? model.Find(k.parent) : null)
                    if (k.name == Joints.Head) { hide.Add(j.name); break; }
            }
            var move = HeroAttacks.Combo(spec.weapon)[0];
            float windUp = move.keys[1].t;
            foreach (var k in move.keys) if (k.t < move.hitAt && k.t > 0f) windUp = k.t;
            for (int c = 0; c < cols; c++)
            {
                float t = c == 0 ? windUp : move.hitAt;
                var pose = AttackPose(spec, model, restGrip, HeroPose.CarryFor(spec.weapon), move, t);
                HeroPose.FirstPersonArms(pose.rot, 1f);
                Vector3 hp; Quaternion hr;
                World(model, pose, Joints.Head, out hp, out hr);
                var eye = CameraRig.EyePosition(hp, Vector3.up, Vector3.forward, HeroModel.HeadCentre(fit), fit.s);
                var cell = new float[w * cellH * 3];
                Array.Copy(img, r * w * cellH * 3, cell, 0, cell.Length);
                // The foe first (further away), then your own arms and weapon over it.
                var foePose = new Pose();
                foePose.pos[foeRoot] = new Vector3(0.3f, 0f, 2.6f);
                foePose.rot[foeRoot] = Quaternion.Euler(0f, 180f, 0f);
                Render(cell, w, cellH, c * cellW, cellW, cellH, new Shot { model = foe, pose = foePose, perspective = true, eye = eye, yaw = 0f, pitch = 4f });
                Render(cell, w, cellH, c * cellW, cellW, cellH, new Shot { model = model, pose = pose, perspective = true, eye = eye, yaw = 0f, pitch = 4f, hide = hide });
                Array.Copy(cell, 0, img, r * w * cellH * 3, cell.Length);
                labels.Add(Outfits.Get(heroes[r]).title + ": " + move.name + (c == 0 ? ", wind-up" : ", blow"));
            }
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

    static void AttackSheet(string rgbaPath, string labelPath)
    {
        var rows = new[] { OutfitId.Raider, OutfitId.Jarl, OutfitId.SpearGuard, OutfitId.Scout, OutfitId.Seer };
        const int cols = 6, cellW = 760, cellH = 900;
        int w = cellW * cols, h = cellH * rows.Length;
        var img = new float[w * h * 3];
        var labels = new List<string> { "cols=" + cols };
        for (int r = 0; r < rows.Length; r++)
        {
            var spec = CharacterSpec.Default(rows[r]);
            var restGrip = Full(spec).Find(Joints.Weapon).localPosition;
            var model = Full(spec);
            var combo = HeroAttacks.Combo(spec.weapon);
            var carry = HeroPose.CarryFor(spec.weapon);
            var row = new float[w * cellH * 3];
            for (int i = 0; i < w * cellH; i++) { row[i * 3] = Paper.r; row[i * 3 + 1] = Paper.g; row[i * 3 + 2] = Paper.b; }
            for (int c = 0; c < cols; c++)
            {
                // Each swing of the combo: its wind-up, then its blow.
                if (c / 2 >= combo.Length) { labels.Add(""); continue; }
                var move = combo[c / 2];
                // The wind-up is the last key before the hit: the most drawn-back moment (a bow at full draw).
                float windUp = move.keys[1].t;
                foreach (var k in move.keys) if (k.t < move.hitAt && k.t > 0f) windUp = k.t;
                float t = c % 2 == 0 ? windUp : move.hitAt;
                var pose = AttackPose(spec, model, restGrip, carry, move, t);
                Render(row, w, cellH, c * cellW, cellW, cellH, new Shot { model = model, pose = pose, yaw = 235f, zoom = 0.66f });
                labels.Add((c / 2 + 1) + ". " + move.name + (c % 2 == 0 ? ": wind-up" : ": blow"));
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
        if (shot.pose.show.Contains(joint)) return true;
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
        // The add-on's own root joint becomes the hand; any joints of its own (a bow's string) hang from it.
        string root = weapon.Joints.Count > 0 ? weapon.Joints[0].name : hand;
        foreach (var j in weapon.Joints)
            if (j.name != root && character.Find(j.name) == null)
                character.Joints.Add(new VikingModel.Joint { name = j.name, parent = j.parent == root ? hand : j.parent, localPosition = j.localPosition, restEuler = j.restEuler, hidden = j.hidden });
        foreach (var p in weapon.Pieces)
            character.Pieces.Add(new VikingModel.Piece { joint = p.joint == root ? hand : p.joint, color = p.color, mesh = p.mesh, outline = p.outline, ink = p.ink, surface = p.surface });
        return character;
    }

    static void World(VikingModel m, Pose pose, string joint, out Vector3 pos, out Quaternion rot)
    {
        var j = m.Find(joint);
        Vector3 local = pose.pos.ContainsKey(joint) ? pose.pos[joint] : j.localPosition;
        // The posture joints (body, legs, head) keep their standing-tall rest under any pose; others are set outright.
        bool posture = joint == Joints.Body || joint == Joints.LeftLeg || joint == Joints.RightLeg || joint == Joints.Head;
        Quaternion own = pose.rot.ContainsKey(joint) ? (posture ? Quaternion.Euler(j.restEuler) * pose.rot[joint] : pose.rot[joint]) : Quaternion.Euler(j.restEuler);
        if (j.parent == null) { pos = local; rot = own; return; }
        Vector3 pp; Quaternion pr;
        World(m, pose, j.parent, out pp, out pr);
        pos = pp + pr * local;
        rot = pose.worldRot.ContainsKey(joint) ? pose.worldRot[joint] : pr * own;
    }

    static void Render(float[] img, int w, int h, int x0, int cellW, int cellH, Shot shot)
    {
        var cam = Quaternion.Euler(shot.pitch, shot.yaw, 0f);
        var inv = new Quaternion(-cam.x, -cam.y, -cam.z, cam.w);
        Vector3 forward = cam * Vector3.forward;
        Vector3 light = new Vector3(-0.5f, 0.75f, 0.45f).normalized;
        float scale = cellH / 2.45f * shot.zoom, cx = x0 + cellW / 2f, groundY = cellH * (shot.zoom < 1f ? 0.84f : 0.92f);
        var depth = new float[cellW * cellH];
        for (int i = 0; i < depth.Length; i++) depth[i] = float.MaxValue;

        // Soft oval shadow on the ground.
        float focal = cellH / 2f / Mathf.Tan(shot.fov * 0.5f * Mathf.Deg2Rad);
        if (!shot.perspective)
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

        // How far each bent cape hangs, over all its pieces (as the game measures it).
        var bendLength = new Dictionary<string, float>();
        foreach (var piece in shot.model.Pieces)
            if (shot.pose.bend.ContainsKey(piece.joint))
            {
                float l;
                bendLength.TryGetValue(piece.joint, out l);
                bendLength[piece.joint] = Mathf.Max(l, ClothBend.Length(piece.mesh.Vertices));
            }
        // Skirts and coats follow the legs, as the game's SkirtFlexer bends them.
        var leftLegJ = shot.model.Find(Joints.LeftLeg);
        float hipY = leftLegJ != null ? leftLegJ.localPosition.y : 0f, hipX = leftLegJ != null ? -leftLegJ.localPosition.x : 0f;
        float leftSwing = shot.pose.rot.ContainsKey(Joints.LeftLeg) ? SkirtFlex.LegPitch(shot.pose.rot[Joints.LeftLeg]) : 0f;
        float rightSwing = shot.pose.rot.ContainsKey(Joints.RightLeg) ? SkirtFlex.LegPitch(shot.pose.rot[Joints.RightLeg]) : 0f;
        bool flex = leftLegJ != null && (Mathf.Abs(leftSwing) > 0.2f || Mathf.Abs(rightSwing) > 0.2f);
        foreach (var piece in shot.model.Pieces)
        {
            if (!Visible(shot, piece.joint) || shot.hide.Contains(piece.joint)) continue;
            Vector3 jp; Quaternion jr;
            World(shot.model, shot.pose, piece.joint, out jp, out jr);
            var mesh = piece.mesh;
            mesh.FillUvs();
            var tex = piece.ink || piece.surface == SurfaceKind.Plain ? null : DrawnTextures.Get(piece.surface);
            int n = mesh.Vertices.Count;
            var world = new Vector3[n];
            var normal = new Vector3[n];
            Vector2 bend;
            bool bent = shot.pose.bend.TryGetValue(piece.joint, out bend);
            Vector3 pull;
            bool drawn = shot.pose.pull.TryGetValue(piece.joint, out pull);
            float half = 0f;
            if (drawn) foreach (var v in mesh.Vertices) half = Mathf.Max(half, Mathf.Abs(v.z));
            for (int i = 0; i < n; i++)
            {
                var v = mesh.Vertices[i];
                if (bent) v = ClothBend.Apply(v, bendLength[piece.joint], bend.x, bend.y);
                if (drawn) v = BowDraw.Apply(v, half, pull);
                if (flex && piece.joint == Joints.Body) v = SkirtFlex.Apply(v, hipY, hipX, leftSwing, rightSwing);
                world[i] = jp + jr * v;
            }
            for (int t = 0; t < mesh.Triangles.Count; t += 3)
            {
                int a = mesh.Triangles[t], b = mesh.Triangles[t + 1], c = mesh.Triangles[t + 2];
                Vector3 fn = Vector3.Cross(world[b] - world[a], world[c] - world[a]);
                normal[a] += fn; normal[b] += fn; normal[c] += fn;
            }
            var screen = new Vector3[n];
            var camSpace = shot.perspective ? new Vector3[n] : null;
            for (int i = 0; i < n; i++)
            {
                normal[i] = normal[i].normalized;
                if (shot.perspective)
                {
                    camSpace[i] = inv * (world[i] - shot.eye);
                    screen[i] = Project(camSpace[i], cx, cellH, focal);
                    continue;
                }
                Vector3 cp = inv * (world[i] - new Vector3(0f, 0.9f, 0f));
                screen[i] = new Vector3(cx + cp.x * scale, groundY - (cp.y + 0.9f) * scale, cp.z);
            }
            for (int t = 0; t < mesh.Triangles.Count; t += 3)
            {
                int a = mesh.Triangles[t], b = mesh.Triangles[t + 1], c = mesh.Triangles[t + 2];
                Vector3 fn = Vector3.Cross(world[b] - world[a], world[c] - world[a]);
                if (shot.perspective)
                {
                    // Facing away from the eye: culled. Crossing the near plane: clipped to the part in front of it.
                    if (Vector3.Dot(fn, world[a] - shot.eye) >= 0f) continue;
                    if (screen[a].z < Near || screen[b].z < Near || screen[c].z < Near)
                    {
                        var poly = ClipNear(new[] { a, b, c }, camSpace, normal, mesh.Uvs);
                        for (int k = 1; k + 1 < poly.Count; k++)
                            Tri(img, depth, w, x0, cellW, cellH, Project(poly[0].p, cx, cellH, focal), Project(poly[k].p, cx, cellH, focal), Project(poly[k + 1].p, cx, cellH, focal),
                                poly[0].n, poly[k].n, poly[k + 1].n, poly[0].uv, poly[k].uv, poly[k + 1].uv, tex, InkStyle.HatchAmount(piece.surface), InkStyle.Flatness(piece.surface), piece.color, piece.ink, light, forward);
                        continue;
                    }
                }
                else if (Vector3.Dot(fn, forward) >= 0f) continue; // back face, culled like Unity
                Tri(img, depth, w, x0, cellW, cellH, screen[a], screen[b], screen[c], normal[a], normal[b], normal[c], mesh.Uvs[a], mesh.Uvs[b], mesh.Uvs[c], tex, InkStyle.HatchAmount(piece.surface), InkStyle.Flatness(piece.surface), piece.color, piece.ink, light, forward);
            }
        }
    }

    /// <summary>The perspective camera's near plane (m in front of the eye).</summary>
    const float Near = 0.05f;

    static Vector3 Project(Vector3 p, float cx, int cellH, float focal)
    {
        float z = Mathf.Max(p.z, 1e-4f);
        return new Vector3(cx + p.x / z * focal, cellH / 2f - p.y / z * focal, p.z);
    }

    struct ClipVert { public Vector3 p, n; public Vector2 uv; }

    /// <summary>A triangle cut down to the part in front of the near plane (0 to 4 corners, in camera space).</summary>
    static List<ClipVert> ClipNear(int[] tri, Vector3[] cam, Vector3[] normal, List<Vector2> uvs)
    {
        var outList = new List<ClipVert>();
        for (int i = 0; i < 3; i++)
        {
            int a = tri[i], b = tri[(i + 1) % 3];
            bool ina = cam[a].z >= Near, inb = cam[b].z >= Near;
            if (ina) outList.Add(new ClipVert { p = cam[a], n = normal[a], uv = uvs[a] });
            if (ina != inb)
            {
                float t = (Near - cam[a].z) / (cam[b].z - cam[a].z);
                outList.Add(new ClipVert { p = Vector3.Lerp(cam[a], cam[b], t), n = Vector3.Lerp(normal[a], normal[b], t).normalized, uv = Vector2.Lerp(uvs[a], uvs[b], t) });
            }
        }
        return outList;
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
