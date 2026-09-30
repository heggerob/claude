using UnityEngine;

namespace OdinsCoin
{
    /// <summary>A ship's colours: the tarred hull, its painted strakes, the sails and their stripes, the shields.</summary>
    public class ShipLook
    {
        public Color hull = new Color(0.2f, 0.15f, 0.12f);
        public Color strake = new Color(0.42f, 0.3f, 0.2f);
        public Color deck = new Color(0.52f, 0.4f, 0.27f);
        public Color sail = new Color(0.55f, 0.1f, 0.08f);
        public Color stripe = new Color(0.1f, 0.08f, 0.08f);
        public Color rune = new Color(0.92f, 0.86f, 0.7f);
        public Color shieldA = new Color(0.55f, 0.1f, 0.08f), shieldB = new Color(0.12f, 0.1f, 0.1f);
        public Color gold = new Color(0.85f, 0.65f, 0.25f);
        public Color iron = new Color(0.25f, 0.25f, 0.27f);
        public Color dragon = new Color(0.16f, 0.2f, 0.18f);
    }

    /// <summary>
    /// Builds a ship class's look from its <see cref="ShipDesign"/>: a lofted clinker hull with a rising sheer,
    /// a dragon-headed stem and curled stern post, a stern castle on the big ones, masts with striped square
    /// sails and lateen mizzens, banks of oars, shields along the rail and a stern-hung rudder. Storybook-drawn
    /// (flat colours, ink outlines); the numbers come from the design, so the look and the physics agree.
    /// Ship frame: x starboard, y up (0 = waterline), z forward. Everything is on one joint, "Ship".
    /// </summary>
    public static class ShipModel
    {
        public const string Joint = "Ship";

        /// <summary>Keel and gunwale heights and half-beam at a point along the hull, t = -1 (stern) .. 1 (bow).</summary>
        public static void Section(ShipDesign d, float t, out float keel, out float gunwale, out float halfBeam)
        {
            float a = Mathf.Abs(t);
            halfBeam = d.beam / 2f * Mathf.Pow(Mathf.Max(0f, 1f - Mathf.Pow(a, 2.4f)), 0.55f);
            keel = -d.draught * (1f - 0.45f * t * t);
            // The sheer sweeps up to high ends, higher at the bow.
            float rise = t > 0f ? 1.4f : 1.0f;
            gunwale = d.freeboard + d.freeboard * rise * Mathf.Pow(a, 3f);
        }

        public static VikingModel Build(ShipDesign d, ShipLook look)
        {
            var m = new VikingModel();
            m.AddJoint(Joint, null, Vector3.zero);
            Hull(m, d, look);
            Posts(m, d, look);
            if (d.length >= 40f) SternCastle(m, d, look);
            Rigging(m, d, look);
            Oars(m, d, look);
            if (d.length >= 25f) Shields(m, d, look);
            RudderBlade(m, d, look);
            if (d.length >= 55f) DeckHall(m, d, look);
            m.AddOutlines(0.06f * Mathf.Sqrt(d.length / 20f), new Color(0.08f, 0.06f, 0.05f));
            return m;
        }

        static void Hull(VikingModel m, ShipDesign d, ShipLook look)
        {
            const int stations = 26, around = 11;
            var grid = new Vector3[stations, around];
            for (int i = 0; i < stations; i++)
            {
                float t = Mathf.Lerp(-1f, 1f, i / (float)(stations - 1));
                float keel, gun, hb;
                Section(d, t, out keel, out gun, out hb);
                float z = t * d.length / 2f;
                for (int j = 0; j < around; j++)
                {
                    // Round the section from the port gunwale, down to the keel, up to the starboard gunwale.
                    float th = Mathf.Lerp(-Mathf.PI / 2f, Mathf.PI / 2f, j / (float)(around - 1));
                    float s = Mathf.Sin(th), c = Mathf.Cos(th);
                    float x = hb * Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), 0.55f);
                    float y = keel + (gun - keel) * (1f - Mathf.Pow(Mathf.Abs(c), 0.7f));
                    grid[i, j] = new Vector3(x, y, z);
                }
            }
            m.Add(Joint, look.hull, CharacterKit.Sheet(grid, Vector3.down, 0.12f), true, SurfaceKind.Wood);
            // Clinker strakes: paler bands along the hull.
            foreach (float level in new[] { 0.35f, 0.55f, 0.72f, 0.86f })
                foreach (float side in new[] { -1f, 1f })
                {
                    var path = new Vector3[stations];
                    var radii = new float[stations];
                    for (int i = 0; i < stations; i++)
                    {
                        float t = Mathf.Lerp(-0.98f, 0.98f, i / (float)(stations - 1));
                        float keel, gun, hb;
                        Section(d, t, out keel, out gun, out hb);
                        float th = side * level * Mathf.PI / 2f;
                        float s = Mathf.Sin(th), c = Mathf.Cos(th);
                        path[i] = new Vector3(hb * Mathf.Sign(s) * Mathf.Pow(Mathf.Abs(s), 0.55f) * 1.02f + side * 0.03f,
                            keel + (gun - keel) * (1f - Mathf.Pow(Mathf.Abs(c), 0.7f)), t * d.length / 2f);
                        radii[i] = 0.05f + 0.004f * d.length;
                    }
                    m.Add(Joint, level > 0.8f ? look.gold : look.strake, MeshData.Tube(path, radii, 5), false, SurfaceKind.Wood);
                }
            // The deck: planks a little below the rail, following the hull's shape.
            var deck = new Vector3[21, 2];
            for (int i = 0; i <= 20; i++)
            {
                float t = Mathf.Lerp(-0.92f, 0.92f, i / 20f), k, g, hb;
                Section(d, t, out k, out g, out hb);
                deck[i, 0] = new Vector3(-hb * 0.92f, d.freeboard - 0.6f, t * d.length / 2f);
                deck[i, 1] = new Vector3(hb * 0.92f, d.freeboard - 0.6f, t * d.length / 2f);
            }
            m.Add(Joint, look.deck, CharacterKit.Sheet(deck, Vector3.up, 0.15f), false, SurfaceKind.Wood);
        }

        static void Posts(VikingModel m, ShipDesign d, ShipLook look)
        {
            float k, g, hb;
            float r = 0.12f + 0.006f * d.length;
            // The stem sweeps up from the keel into a dragon's neck.
            Section(d, 1f, out k, out g, out hb);
            float bowZ = d.length / 2f;
            var neck = new[] {
                new Vector3(0f, -d.draught * 0.55f, bowZ - 0.5f), new Vector3(0f, g * 0.6f, bowZ + 0.4f),
                new Vector3(0f, g + d.freeboard * 0.8f, bowZ + 1.2f), new Vector3(0f, g + d.freeboard * 1.8f, bowZ + 1.1f),
                new Vector3(0f, g + d.freeboard * 2.3f, bowZ + 1.8f) };
            m.Add(Joint, look.dragon, MeshData.Tube(neck, new[] { r * 1.2f, r * 1.2f, r, r * 0.9f, r * 0.9f }, 8), true, SurfaceKind.Wood);
            // The dragon's head: long snout, open jaw, horns swept back, gold eyes.
            var head = neck[neck.Length - 1];
            float hs = 0.35f + 0.02f * d.length;
            m.Add(Joint, look.dragon, MeshData.Ellipsoid(head + new Vector3(0f, 0f, hs * 0.6f), new Vector3(hs * 0.55f, hs * 0.5f, hs * 1.2f), 12, 8), true, SurfaceKind.Wood);
            m.Add(Joint, look.dragon, MeshData.Ellipsoid(head + new Vector3(0f, -hs * 0.45f, hs * 0.9f), new Vector3(hs * 0.4f, hs * 0.18f, hs * 0.9f), 10, 6), true, SurfaceKind.Wood);
            foreach (float side in new[] { -1f, 1f })
            {
                m.Add(Joint, look.gold, MeshData.Ellipsoid(head + new Vector3(side * hs * 0.42f, hs * 0.22f, hs * 0.9f), Vector3.one * hs * 0.12f, 6, 4), false, SurfaceKind.Metal);
                m.Add(Joint, look.dragon, MeshData.Tube(new[] { head + new Vector3(side * hs * 0.3f, hs * 0.35f, 0f), head + new Vector3(side * hs * 0.6f, hs * 0.9f, -hs * 0.8f), head + new Vector3(side * hs * 0.5f, hs * 1.1f, -hs * 1.6f) },
                    new[] { hs * 0.15f, hs * 0.09f, hs * 0.02f }, 6), true, SurfaceKind.Wood);
                // Teeth along the jaw.
                for (int t = 0; t < 4; t++)
                    m.Add(Joint, look.rune, MeshData.Tube(new[] { head + new Vector3(side * hs * 0.3f, -hs * 0.2f, hs * (1.1f + 0.2f * t)), head + new Vector3(side * hs * 0.3f, -hs * 0.4f, hs * (1.1f + 0.2f * t)) },
                        new[] { hs * 0.06f, hs * 0.01f }, 4), false);
            }
            // The stern post curls up and over like a tail.
            Section(d, -1f, out k, out g, out hb);
            float sternZ = -d.length / 2f;
            var tail = new Vector3[12];
            var tr = new float[12];
            for (int i = 0; i < tail.Length; i++)
            {
                float u = i / (float)(tail.Length - 1);
                float a = u * Mathf.PI * 1.6f;
                float rad = d.freeboard * 1.2f * (1f - 0.55f * u);
                tail[i] = new Vector3(0f, g + rad * Mathf.Sin(a) - (u < 0.2f ? (0.2f - u) * 10f : 0f), sternZ - 0.8f - rad * (1f - Mathf.Cos(a)) * 0.6f);
                tr[i] = r * Mathf.Lerp(1.2f, 0.35f, u);
            }
            tail[0] = new Vector3(0f, -d.draught * 0.5f, sternZ + 0.4f);
            m.Add(Joint, look.dragon, MeshData.Tube(tail, tr, 8), true, SurfaceKind.Wood);
        }

        static void SternCastle(VikingModel m, ShipDesign d, ShipLook look)
        {
            float k, g, hb;
            Section(d, -0.78f, out k, out g, out hb);
            float z0 = -d.length / 2f * 0.95f, z1 = -d.length / 2f * 0.58f;
            float floor = g + 0.2f, h = 2.4f + d.length * 0.02f, w = hb * 1.9f;
            // A raised, walled deck with a carved rail, a gold band and lanterns at the corners.
            m.Add(Joint, look.hull, MeshData.Box(new Vector3(0f, floor + h * 0.35f, (z0 + z1) / 2f), new Vector3(w, h * 0.7f, z1 - z0)), true, SurfaceKind.Wood);
            m.Add(Joint, look.deck, MeshData.Box(new Vector3(0f, floor + h * 0.72f, (z0 + z1) / 2f), new Vector3(w * 0.96f, 0.15f, (z1 - z0) * 0.98f)), false, SurfaceKind.Wood);
            m.Add(Joint, look.gold, MeshData.Box(new Vector3(0f, floor + h * 0.55f, (z0 + z1) / 2f), new Vector3(w * 1.02f, 0.18f, (z1 - z0) * 1.01f)), false, SurfaceKind.Metal);
            int posts = Mathf.RoundToInt((z1 - z0) / 1.2f);
            foreach (float side in new[] { -1f, 1f })
                for (int i = 0; i <= posts; i++)
                {
                    float z = Mathf.Lerp(z0, z1, i / (float)posts);
                    m.Add(Joint, look.strake, MeshData.Box(new Vector3(side * w * 0.48f, floor + h * 0.72f + 0.5f, z), new Vector3(0.15f, 1f, 0.15f)), false, SurfaceKind.Wood);
                }
            foreach (float side in new[] { -1f, 1f })
                m.Add(Joint, look.strake, MeshData.Box(new Vector3(side * w * 0.48f, floor + h * 0.72f + 1f, (z0 + z1) / 2f), new Vector3(0.12f, 0.12f, z1 - z0)), false, SurfaceKind.Wood);
            foreach (float side in new[] { -1f, 1f })
            {
                var at = new Vector3(side * w * 0.5f, floor + h * 0.72f + 1.4f, z0 + 0.3f);
                m.Add(Joint, look.iron, MeshData.Box(at, new Vector3(0.35f, 0.5f, 0.35f)), true, SurfaceKind.Metal);
                m.Add(Joint, new Color(1f, 0.8f, 0.4f), MeshData.Box(at, new Vector3(0.25f, 0.35f, 0.4f)), false);
            }
        }

        static void Rigging(VikingModel m, ShipDesign d, ShipLook look)
        {
            float deckY = d.freeboard - 0.5f;
            foreach (var s in d.sails)
            {
                float height = s.height * 1.75f;
                float r = 0.12f + s.height * 0.012f;
                m.Add(Joint, look.strake, MeshData.Tube(new[] { new Vector3(0f, deckY, s.x), new Vector3(0f, height, s.x) }, new[] { r, r * 0.6f }, 8), true, SurfaceKind.Wood);
                if (s.rig == Rig.Square) SquareSail(m, s, deckY, height, look);
                else LateenSail(m, s, deckY, height, look);
                // Stays down to the rail, fore and aft.
                m.Add(Joint, look.iron, MeshData.Tube(new[] { new Vector3(0f, height, s.x), new Vector3(0f, d.freeboard, s.x + height * 0.5f) }, new[] { 0.03f, 0.03f }, 4), false);
                m.Add(Joint, look.iron, MeshData.Tube(new[] { new Vector3(0f, height, s.x), new Vector3(0f, d.freeboard, s.x - height * 0.45f) }, new[] { 0.03f, 0.03f }, 4), false);
            }
        }

        /// <summary>A striped square sail hung from a yard across the mast, bellied forward, with a rune sewn on the middle.</summary>
        static void SquareSail(VikingModel m, SailPlan s, float deckY, float mastTop, ShipLook look)
        {
            float top = mastTop * 0.93f;
            float h = Mathf.Sqrt(s.area * 0.85f), w = s.area / h;
            float bottom = top - h;
            m.Add(Joint, look.strake, MeshData.Tube(new[] { new Vector3(-w * 0.58f, top, s.x + 0.2f), new Vector3(0f, top + 0.15f, s.x + 0.25f), new Vector3(w * 0.58f, top, s.x + 0.2f) }, new[] { 0.1f, 0.14f, 0.1f }, 6), true, SurfaceKind.Wood);
            const int stripes = 6, rows = 6;
            for (int k = 0; k < stripes; k++)
            {
                var grid = new Vector3[rows, 2];
                for (int r = 0; r < rows; r++)
                    for (int c = 0; c < 2; c++)
                    {
                        float u = (k + c) / (float)stripes, v = r / (float)(rows - 1);
                        float x = (u - 0.5f) * w * (1f + 0.08f * v);
                        float belly = Mathf.Sin(v * Mathf.PI) * Mathf.Cos((u - 0.5f) * Mathf.PI) * h * 0.14f;
                        grid[r, c] = new Vector3(x, top - v * h, s.x + 0.35f + belly);
                    }
                m.Add(Joint, (k & 1) == 0 ? look.sail : look.stripe, CharacterKit.Sheet(grid, Vector3.forward, 0.05f), true, SurfaceKind.Cloth);
            }
            // A big pale rune (a raven-ish Othala-like diamond with legs) on the front of the sail.
            float cy = top - h * 0.45f, cz = s.x + 0.35f + h * 0.14f + 0.08f, rs = h * 0.22f;
            var rune = new[] { new Vector3(0f, rs, 0f), new Vector3(rs * 0.7f, 0f, 0f), new Vector3(0f, -rs * 0.6f, 0f), new Vector3(-rs * 0.7f, 0f, 0f), new Vector3(0f, rs, 0f) };
            for (int i = 0; i < rune.Length; i++) rune[i] += new Vector3(0f, cy, cz);
            var rr = new float[rune.Length];
            for (int i = 0; i < rr.Length; i++) rr[i] = h * 0.03f;
            m.Add(Joint, look.rune, MeshData.Tube(rune, rr, 5), false, SurfaceKind.Cloth);
            foreach (float side in new[] { -1f, 1f })
                m.Add(Joint, look.rune, MeshData.Tube(new[] { new Vector3(side * rs * 0.35f, cy - rs * 0.35f, cz), new Vector3(side * rs * 0.8f, cy - rs * 1.1f, cz) }, new[] { h * 0.03f, h * 0.03f }, 5), false, SurfaceKind.Cloth);
        }

        /// <summary>A lateen: a long yard slanting up from low forward to high aft, a triangle of sail beneath it.</summary>
        static void LateenSail(VikingModel m, SailPlan s, float deckY, float mastTop, ShipLook look)
        {
            float span = Mathf.Sqrt(s.area * 2.6f);
            var fore = new Vector3(0f, deckY + 1.5f, s.x + span * 0.45f);
            var aft = new Vector3(0f, mastTop * 1.05f, s.x - span * 0.55f);
            m.Add(Joint, look.strake, MeshData.Tube(new[] { fore, aft }, new[] { 0.1f, 0.06f }, 6), true, SurfaceKind.Wood);
            var clew = new Vector3(0f, deckY + 1.2f, s.x - span * 0.35f);
            const int rows = 6;
            var grid = new Vector3[rows, 2];
            for (int r = 0; r < rows; r++)
            {
                float v = r / (float)(rows - 1);
                var onYard = Vector3.Lerp(fore, aft, v);
                var foot = Vector3.Lerp(fore, clew, v);
                float belly = Mathf.Sin(v * Mathf.PI) * span * 0.05f;
                grid[r, 0] = onYard + new Vector3(0.12f, 0f, 0f);
                grid[r, 1] = Vector3.Lerp(onYard, foot, 0.98f) + new Vector3(0.12f + belly, 0f, 0f);
            }
            m.Add(Joint, look.stripe, CharacterKit.Sheet(grid, Vector3.right, 0.05f), true, SurfaceKind.Cloth);
        }

        static void Oars(VikingModel m, ShipDesign d, ShipLook look)
        {
            if (d.oarsPerSide <= 0) return;
            float span = d.length * 0.62f;
            float oar = d.beam * 0.5f + d.freeboard * 1.6f + 1.5f;
            for (int i = 0; i < d.oarsPerSide; i++)
            {
                float z = Mathf.Lerp(-span / 2f, span / 2f, (i + 0.5f) / d.oarsPerSide) + d.length * 0.03f;
                float k, g, hb;
                Section(d, 2f * z / d.length, out k, out g, out hb);
                foreach (float side in new[] { -1f, 1f })
                {
                    var port = new Vector3(side * hb, g - 0.35f, z);
                    var blade = port + new Vector3(side * oar * 0.85f, -g - 0.1f, -oar * 0.1f);
                    m.Add(Joint, look.strake, MeshData.Tube(new[] { port - new Vector3(side * 0.6f, -0.2f, 0f), blade }, new[] { 0.06f, 0.05f }, 5), false, SurfaceKind.Wood);
                    m.Add(Joint, look.deck, MeshData.Ellipsoid(blade, new Vector3(0.08f, 0.35f, 0.6f), 6, 4).Transformed(Vector3.zero, Quaternion.identity, Vector3.one), false, SurfaceKind.Wood);
                }
            }
        }

        static void Shields(VikingModel m, ShipDesign d, ShipLook look)
        {
            float span = d.length * 0.66f;
            int n = Mathf.RoundToInt(span / 1.15f);
            for (int i = 0; i < n; i++)
            {
                float z = Mathf.Lerp(-span / 2f, span / 2f, (i + 0.5f) / n) + d.length * 0.03f;
                float k, g, hb;
                Section(d, 2f * z / d.length, out k, out g, out hb);
                foreach (float side in new[] { -1f, 1f })
                {
                    var at = new Vector3(side * (hb + 0.08f), g + 0.05f, z);
                    var face = Quaternion.Euler(0f, side * 90f, 0f);
                    m.Add(Joint, (i & 1) == 0 ? look.shieldA : look.shieldB, MeshData.Lathe(new[] { new Vector2(0.45f, 0f), new Vector2(0.45f, 0.06f), new Vector2(0.001f, 0.1f) }, 14).Transformed(at, face * Quaternion.Euler(90f, 0f, 0f), Vector3.one), true, SurfaceKind.Wood);
                    m.Add(Joint, look.iron, MeshData.Dome(Vector3.zero, new Vector3(0.12f, 0.1f, 0.12f), 8, 3).Transformed(at + new Vector3(side * 0.1f, 0f, 0f), face * Quaternion.Euler(90f, 0f, 0f), Vector3.one), false, SurfaceKind.Metal);
                }
            }
        }

        static void RudderBlade(VikingModel m, ShipDesign d, ShipLook look)
        {
            float area = d.rudderArea, depth = Mathf.Sqrt(area * d.rudderAspect), chord = area / depth;
            float z = -d.length / 2f - 0.4f;
            m.Add(Joint, look.hull, MeshData.Box(new Vector3(0f, -d.draught * 0.6f + depth * 0.2f, z - chord / 2f), new Vector3(0.2f, depth * 1.1f, chord)), true, SurfaceKind.Wood);
            m.Add(Joint, look.iron, MeshData.Box(new Vector3(0f, d.freeboard * 1.4f, z - 0.2f), new Vector3(0.18f, 0.18f, 2.5f)), false, SurfaceKind.Metal);
        }

        static void DeckHall(VikingModel m, ShipDesign d, ShipLook look)
        {
            // A little hall on deck behind the main mast: dark walls, a steep roof, gables crossed with dragon heads.
            float y = d.freeboard - 0.5f, len = d.length * 0.16f, w = d.beam * 0.5f, z = -d.length * 0.2f;
            m.Add(Joint, look.hull, MeshData.Box(new Vector3(0f, y + 1.2f, z), new Vector3(w, 2.4f, len)), true, SurfaceKind.Wood);
            var roof = new[] { new Vector2(-w * 0.62f, 0f), new Vector2(w * 0.62f, 0f), new Vector2(0f, 2.2f) };
            m.Add(Joint, look.sail, MeshData.Extrude(roof, len * 1.08f).Transformed(new Vector3(0f, y + 2.4f, z), Quaternion.Euler(0f, 90f, 0f), Vector3.one), true, SurfaceKind.Wood);
            foreach (float end in new[] { -1f, 1f })
                foreach (float side in new[] { -1f, 1f })
                    m.Add(Joint, look.gold, MeshData.Tube(new[] { new Vector3(0f, y + 4.5f, z + end * len * 0.54f), new Vector3(side * 0.8f, y + 5.4f, z + end * len * 0.56f) }, new[] { 0.1f, 0.04f }, 5), false, SurfaceKind.Wood);
        }
    }
}
