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
        Vector3 elbow = shoulder + new Vector3(-0.15f, -0.17f, 0.04f).normalized * fit.upperArm;
        Vector3 fist = elbow + new Vector3(0.08f, 0.25f, -0.17f).normalized * fit.foreArm;
        carry.worldRot[Joints.LeftArm] = Quaternion.FromToRotation(Vector3.down, elbow - shoulder);
        carry.worldRot[Joints.LeftForearm] = Quaternion.FromToRotation(Vector3.down, fist - elbow);
        carry.worldRot[Joints.OffHand] = Quaternion.LookRotation(new Vector3(1f, 0.42f, -0.45f).normalized, new Vector3(0f, 1f, 0.3f));
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
        stand.worldRot[Joints.OffHand] = Quaternion.Euler(0f, 38f, 0f);
        stand.rot[Joints.LeftLeg] = Quaternion.Euler(0f, 0f, -4f);
        stand.rot[Joints.RightLeg] = Quaternion.Euler(0f, 0f, 5f);
        shots.Add(new Shot { label = "The Jarl", model = jarlModel, pose = stand, yaw = 188f });
        shots.Add(new Shot { label = "The Raider", model = model, pose = carry, yaw = 188f });
        shots.Add(new Shot { label = "Jarl, three-quarter", model = jarlModel, pose = stand, yaw = 215f });
        shots.Add(new Shot { label = "Jarl, back", model = jarlModel, pose = stand, yaw = 20f });
        shots.Add(new Shot { label = "Raider, three-quarter", model = model, pose = carry, yaw = 215f });
        // Same outfit on other bodies: the player chooses height, build and gender.
        var tall = CharacterSpec.Default(OutfitId.Raider);
        tall.body = new BodyShape { height = 1.85f, width = 1.25f, gender = Gender.Male };
        tall.palette = Outfits.Get(OutfitId.Raider).palette();
        tall.palette.hair = new Color(0.88f, 0.76f, 0.48f); tall.palette.accent = new Color(0.18f, 0.32f, 0.5f); tall.palette.cloth = new Color(0.33f, 0.29f, 0.24f);
        shots.Add(new Shot { label = "Tall, broad (custom colours)", model = HeroModel.Build(tall), yaw = 195f });
        var small = CharacterSpec.Default(OutfitId.Raider);
        small.body = new BodyShape { height = 1.45f, width = 0.85f, gender = Gender.Female };
        shots.Add(new Shot { label = "Short, slim", model = HeroModel.Build(small), yaw = 195f });

        const int cellW = 560, cellH = 1080; // 2x supersampled
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
        Directory.CreateDirectory(args[2]);
        ExportObj(HeroModel.Build(raider), Path.Combine(args[2], "raider.obj"));
        ExportObj(HeroModel.BuildWeapon(raider), Path.Combine(args[2], "two-hand-axe.obj"));
        Console.WriteLine("hero: " + model.TriangleCount + " triangles");
    }

    /// <summary>For pictures only: hang the separately built weapon on the character's weapon hand.</summary>
    static VikingModel WithWeapon(VikingModel character, VikingModel weapon, string hand)
    {
        foreach (var p in weapon.Pieces)
            character.Pieces.Add(new VikingModel.Piece { joint = hand, color = p.color, mesh = p.mesh, outline = p.outline, ink = p.ink });
        return character;
    }

    static void World(VikingModel m, Pose pose, string joint, out Vector3 pos, out Quaternion rot)
    {
        var j = m.Find(joint);
        Vector3 local = pose.pos.ContainsKey(joint) ? pose.pos[joint] : j.localPosition;
        Quaternion own = pose.rot.ContainsKey(joint) ? pose.rot[joint] : Quaternion.identity;
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
        float scale = cellH / 2.25f, cx = x0 + cellW / 2f, groundY = cellH * 0.9f;
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
            Vector3 jp; Quaternion jr;
            World(shot.model, shot.pose, piece.joint, out jp, out jr);
            var mesh = piece.mesh;
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
                Tri(img, depth, w, x0, cellW, cellH, screen[a], screen[b], screen[c], normal[a], normal[b], normal[c], piece.color, piece.ink, light, forward);
            }
        }
    }

    static void Tri(float[] img, float[] depth, int w, int x0, int cellW, int cellH, Vector3 a, Vector3 b, Vector3 c,
        Vector3 na, Vector3 nb, Vector3 nc, Color color, bool ink, Vector3 light, Vector3 forward)
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
                if (!ink)
                {
                    // Soft painterly light: wrapped diffuse, cool shadows, warm highlights.
                    Vector3 nrm = (na * w0 + nb * w1 + nc * w2).normalized;
                    float d = (Vector3.Dot(nrm, light) + 0.35f) / 1.35f;
                    d = Mathf.Clamp01(d);
                    float shade = 0.55f + 0.55f * d;
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
