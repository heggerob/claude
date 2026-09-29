// Renders the Viking model (several looks, angles and a fighting pose) with a tiny software rasterizer, and
// exports the default Viking as a Wavefront .obj so it can be opened in any 3D viewer or imported into Unity.
// Faces are culled exactly as Unity would (outward = clockwise), so an inside-out mesh shows up as a hole.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using UnityEngine;
using OdinsCoin;

public static class VikingPreview
{
    const int CellW = 600, CellH = 840; // 2x supersampled; the script halves it
    const int Cols = 4, Rows = 2;

    class Pose
    {
        public Dictionary<string, Quaternion> rot = new Dictionary<string, Quaternion>();
        public Dictionary<string, Vector3> pos = new Dictionary<string, Vector3>();
    }

    public static void Main(string[] args)
    {
        string rgbaPath = args[0], objPath = args[1];
        var img = new float[CellW * Cols * CellH * Rows * 3];
        // Sky gradient background.
        for (int y = 0; y < CellH * Rows; y++)
            for (int x = 0; x < CellW * Cols; x++)
            {
                float t = (y % CellH) / (float)CellH;
                int i = (y * CellW * Cols + x) * 3;
                img[i] = 0.62f + 0.14f * t; img[i + 1] = 0.7f + 0.1f * t; img[i + 2] = 0.78f + 0.04f * t;
            }

        var viking = new VikingLook();
        var fight = new Pose();
        fight.rot[VikingModel.RightArm] = Quaternion.Euler(-135f, 0f, -12f);
        fight.rot[VikingModel.LeftArm] = Quaternion.Euler(-75f, 0f, 0f);
        fight.rot[VikingModel.LeftLeg] = Quaternion.Euler(-22f, 0f, 0f);
        fight.rot[VikingModel.RightLeg] = Quaternion.Euler(18f, 0f, 0f);
        fight.pos[VikingModel.Shield] = new Vector3(-0.3f, 1.3f, 0.5f);
        fight.rot[VikingModel.Shield] = Quaternion.Euler(0f, 180f, 0f);

        var jarl = new VikingLook { horns = true, tunic = new Color(0.55f, 0.12f, 0.12f), hair = new Color(0.9f, 0.8f, 0.55f), fur = new Color(0.85f, 0.83f, 0.8f), shield = new Color(0.1f, 0.25f, 0.55f), size = 1.08f };
        var saxon = new VikingLook { tunic = new Color(0.3f, 0.45f, 0.25f), hair = new Color(0.35f, 0.25f, 0.15f), shield = new Color(0.9f, 0.85f, 0.3f), sword = true, beardLength = 0.2f };
        var raider = new VikingLook { tunic = new Color(0.35f, 0.25f, 0.18f), hair = new Color(0.15f, 0.12f, 0.1f), shield = new Color(0.1f, 0.1f, 0.1f), shieldStripe = new Color(0.6f, 0.12f, 0.1f), sword = true };

        var none = new Pose();
        Draw(img, 0, 0, VikingModel.Build(viking), none, 180f);  // front
        Draw(img, 1, 0, VikingModel.Build(viking), none, 215f);  // three-quarter
        Draw(img, 2, 0, VikingModel.Build(viking), none, 270f);  // side
        Draw(img, 3, 0, VikingModel.Build(viking), none, 20f);   // back
        Draw(img, 0, 1, VikingModel.Build(viking), fight, 210f); // fighting
        Draw(img, 1, 1, VikingModel.Build(jarl), none, 200f);    // horned jarl
        Draw(img, 2, 1, VikingModel.Build(saxon), none, 160f);   // Saxon guard
        Draw(img, 3, 1, VikingModel.Build(raider), fight, 150f); // Danish raider

        using (var f = new BinaryWriter(File.Create(rgbaPath)))
        {
            f.Write(CellW * Cols); f.Write(CellH * Rows);
            for (int i = 0; i < img.Length; i += 3)
            {
                f.Write((byte)(Mathf.Clamp01(img[i]) * 255)); f.Write((byte)(Mathf.Clamp01(img[i + 1]) * 255)); f.Write((byte)(Mathf.Clamp01(img[i + 2]) * 255)); f.Write((byte)255);
            }
        }
        ExportObj(VikingModel.Build(viking), objPath);
        Console.WriteLine("viking: " + VikingModel.Build(viking).TriangleCount + " triangles");
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
        rot = pr * own;
    }

    static void Draw(float[] img, int col, int row, VikingModel m, Pose pose, float yaw)
    {
        // Orthographic camera looking at the Viking from `yaw`, a little from above.
        var cam = Quaternion.Euler(12f, yaw, 0f);
        var inv = new Quaternion(-cam.x, -cam.y, -cam.z, cam.w);
        Vector3 forward = cam * Vector3.forward;
        Vector3 light = new Vector3(-0.45f, 0.8f, 0.4f).normalized;
        float scale = CellH / 2.35f, ox = col * CellW + CellW / 2f, oy = row * CellH + CellH * 0.9f;
        var depth = new float[CellW * CellH];
        for (int i = 0; i < depth.Length; i++) depth[i] = float.MaxValue;

        // A soft shadow on the ground.
        for (int y = 0; y < CellH; y++)
            for (int x = 0; x < CellW; x++)
            {
                float dx = (x - CellW / 2f) / (CellW * 0.28f), dy = (y - CellH * 0.9f) / (CellH * 0.035f);
                float d = dx * dx + dy * dy;
                if (d < 1f) { int i = ((row * CellH + y) * CellW * Cols + col * CellW + x) * 3; float k = 1f - 0.25f * (1f - d); img[i] *= k; img[i + 1] *= k; img[i + 2] *= k; }
            }

        foreach (var piece in m.Pieces)
        {
            Vector3 jp; Quaternion jr;
            World(m, pose, piece.joint, out jp, out jr);
            var mesh = piece.mesh;
            int n = mesh.Vertices.Count;
            var world = new Vector3[n];
            var normal = new Vector3[n];
            for (int i = 0; i < n; i++) world[i] = jp + jr * mesh.Vertices[i];
            // Smooth normals like Unity's RecalculateNormals: averaged over triangles sharing a vertex.
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
                Vector3 cp = inv * (world[i] - new Vector3(0f, 0.95f, 0f));
                screen[i] = new Vector3(ox + cp.x * scale, oy - (cp.y + 0.95f) * scale, cp.z);
            }
            for (int t = 0; t < mesh.Triangles.Count; t += 3)
            {
                int a = mesh.Triangles[t], b = mesh.Triangles[t + 1], c = mesh.Triangles[t + 2];
                Vector3 fn = Vector3.Cross(world[b] - world[a], world[c] - world[a]);
                if (Vector3.Dot(fn, forward) >= 0f) continue; // back face: Unity wouldn't draw it either
                Raster(img, depth, col, row, screen[a], screen[b], screen[c], normal[a], normal[b], normal[c], piece.color, light, forward);
            }
        }
    }

    static void Raster(float[] img, float[] depth, int col, int row, Vector3 a, Vector3 b, Vector3 c, Vector3 na, Vector3 nb, Vector3 nc, Color color, Vector3 light, Vector3 forward)
    {
        int x0 = Math.Max(col * CellW, (int)Math.Floor(Math.Min(a.x, Math.Min(b.x, c.x))));
        int x1 = Math.Min(col * CellW + CellW - 1, (int)Math.Ceiling(Math.Max(a.x, Math.Max(b.x, c.x))));
        int y0 = Math.Max(row * CellH, (int)Math.Floor(Math.Min(a.y, Math.Min(b.y, c.y))));
        int y1 = Math.Min(row * CellH + CellH - 1, (int)Math.Ceiling(Math.Max(a.y, Math.Max(b.y, c.y))));
        float area = (b.x - a.x) * (c.y - a.y) - (b.y - a.y) * (c.x - a.x);
        if (Math.Abs(area) < 1e-6f) return;
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                float px = x + 0.5f, py = y + 0.5f;
                float w0 = ((b.x - px) * (c.y - py) - (b.y - py) * (c.x - px)) / area;
                float w1 = ((c.x - px) * (a.y - py) - (c.y - py) * (a.x - px)) / area;
                float w2 = 1f - w0 - w1;
                if (w0 < 0f || w1 < 0f || w2 < 0f) continue;
                float z = a.z * w0 + b.z * w1 + c.z * w2;
                int di = (y - row * CellH) * CellW + (x - col * CellW);
                if (z >= depth[di]) continue;
                depth[di] = z;
                Vector3 nrm = (na * w0 + nb * w1 + nc * w2).normalized;
                float diffuse = Math.Max(0f, Vector3.Dot(nrm, light));
                float rim = (float)Math.Pow(1f - Math.Abs(Vector3.Dot(nrm, forward)), 3) * 0.25f;
                float shade = 0.42f + 0.68f * diffuse + rim;
                int i = (y * CellW * Cols + x) * 3;
                img[i] = color.r * shade; img[i + 1] = color.g * shade; img[i + 2] = color.b * shade;
            }
    }

    /// <summary>Wavefront .obj + .mtl. OBJ is right-handed, so X is mirrored (Unity mirrors it back on import).</summary>
    static void ExportObj(VikingModel m, string path)
    {
        var inv = CultureInfo.InvariantCulture;
        string mtlPath = Path.ChangeExtension(path, ".mtl");
        using (var mtl = new StreamWriter(mtlPath))
        using (var obj = new StreamWriter(path))
        {
            obj.WriteLine("# Odin's Coin Viking, generated by VikingModel.cs. 1 unit = 1 metre, feet at y = 0, facing +Z.");
            obj.WriteLine("mtllib " + Path.GetFileName(mtlPath));
            int offset = 1, index = 0;
            foreach (var p in m.Pieces)
            {
                string name = "m" + index++;
                mtl.WriteLine("newmtl " + name);
                mtl.WriteLine(string.Format(inv, "Kd {0:0.###} {1:0.###} {2:0.###}", p.color.r, p.color.g, p.color.b));
                mtl.WriteLine("Ka 0 0 0\nKs 0.05 0.05 0.05\nNs 10\nd 1\nillum 2\n");
                obj.WriteLine("o " + p.joint.Replace(' ', '_') + "_" + index);
                obj.WriteLine("usemtl " + name);
                Vector3 at = m.RestPosition(p.joint);
                foreach (var v in p.mesh.Vertices)
                {
                    Vector3 w = at + v;
                    obj.WriteLine(string.Format(inv, "v {0:0.#####} {1:0.#####} {2:0.#####}", -w.x, w.y, w.z));
                }
                for (int t = 0; t < p.mesh.Triangles.Count; t += 3)
                    obj.WriteLine("f " + (p.mesh.Triangles[t] + offset) + " " + (p.mesh.Triangles[t + 1] + offset) + " " + (p.mesh.Triangles[t + 2] + offset));
                offset += p.mesh.Vertices.Count;
            }
        }
    }
}
