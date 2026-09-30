using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>What sort of place it is, which decides what's built there and what happens.</summary>
    public enum PlaceKind
    {
        /// <summary>A market town: traders, a busy harbour, somewhere to sell plunder.</summary>
        Town,
        /// <summary>A jarl's or king's hall: a base to winter at, men to hire, ships to buy.</summary>
        Hall,
        /// <summary>A rich monastery, poorly guarded: the target of raids.</summary>
        Monastery,
        /// <summary>A sea fortress with a walled harbour.</summary>
        Fortress,
        /// <summary>A small trading post or landing.</summary>
        Landing,
    }

    /// <summary>A real place of the Viking age, where it really was.</summary>
    public class Place
    {
        public string name, modern, blurb;
        public PlaceKind kind;
        public float latitude, longitude;
        /// <summary>How far from the place its harbour may be (km): some towns lay up rivers or behind islands.</summary>
        public float harbourReach = 4f;
        public string Title { get { return name; } }
    }

    /// <summary>
    /// The real world's ports of call, raid targets and bases: Viking-age towns, halls, monasteries and fortresses
    /// at their real positions. Each gets a harbour: the nearest sea deep enough for a big ship, found on the real
    /// map (<see cref="Harbour"/>).
    /// </summary>
    public static class Places
    {
        public static readonly Place[] All =
        {
            // Norway.
            new Place { name = "Kaupang", modern = "Larvik", kind = PlaceKind.Town, latitude = 59.03f, longitude = 10.10f, blurb = "Skiringssal's market town, where the traders of the Vík meet ships from the whole North." },
            new Place { name = "Nidaros", modern = "Trondheim", kind = PlaceKind.Town, latitude = 63.43f, longitude = 10.395f, blurb = "At the mouth of the Nid, the kings' town of the Trøndelag." },
            new Place { name = "Bjørgvin", modern = "Bergen", kind = PlaceKind.Town, latitude = 60.395f, longitude = 5.325f, blurb = "Among seven mountains, the harbour of the west coast and its stockfish." },
            new Place { name = "Avaldsnes", modern = "Karmøy", kind = PlaceKind.Hall, latitude = 59.352f, longitude = 5.279f, blurb = "The king's hall on Karmsund, taking toll from every ship on the North Way." },
            new Place { name = "Borg", modern = "Lofoten", kind = PlaceKind.Hall, latitude = 68.244f, longitude = 13.787f, harbourReach = 6f, blurb = "The chieftain's great hall in Lofoten, eighty metres long." },
            new Place { name = "Borre", modern = "Horten", kind = PlaceKind.Hall, latitude = 59.39f, longitude = 10.46f, blurb = "The burial mounds and hall of the kings of Vestfold." },
            new Place { name = "Tønsberg", modern = "Tønsberg", kind = PlaceKind.Town, latitude = 59.267f, longitude = 10.408f, harbourReach = 6f, blurb = "An old town on the Vestfold shore, sheltered behind its islands." },
            new Place { name = "Bjarkøy", modern = "Harstad", kind = PlaceKind.Hall, latitude = 68.996f, longitude = 16.556f, harbourReach = 6f, blurb = "The seat of Tore Hund, trading furs with the Sámi and beyond." },
            new Place { name = "Stavanger", modern = "Stavanger", kind = PlaceKind.Landing, latitude = 58.97f, longitude = 5.733f, blurb = "Where Harald Fairhair's fleets gathered for Hafrsfjord." },
            // Denmark and the south.
            new Place { name = "Hedeby", modern = "Haithabu", kind = PlaceKind.Town, latitude = 54.491f, longitude = 9.566f, harbourReach = 8f, blurb = "The greatest market of the North, behind the Danevirke on the Schlei." },
            new Place { name = "Ribe", modern = "Ribe", kind = PlaceKind.Town, latitude = 55.328f, longitude = 8.762f, harbourReach = 10f, blurb = "Denmark's oldest town, facing the North Sea and Frisia." },
            new Place { name = "Roskilde", modern = "Roskilde", kind = PlaceKind.Hall, latitude = 55.642f, longitude = 12.08f, harbourReach = 10f, blurb = "The Danish kings' seat at the head of its fjord." },
            new Place { name = "Aros", modern = "Aarhus", kind = PlaceKind.Town, latitude = 56.156f, longitude = 10.21f, blurb = "A walled town on the east coast of Jutland." },
            new Place { name = "Jomsborg", modern = "Wolin", kind = PlaceKind.Fortress, latitude = 53.84f, longitude = 14.62f, harbourReach = 12f, blurb = "The Jomsvikings' sea fortress, its harbour inside the walls." },
            // Sweden and the Baltic.
            new Place { name = "Birka", modern = "Björkö", kind = PlaceKind.Town, latitude = 59.335f, longitude = 17.543f, harbourReach = 8f, blurb = "The Svear's island town on Lake Mälaren, trading east to Miklagard." },
            new Place { name = "Sigtuna", modern = "Sigtuna", kind = PlaceKind.Town, latitude = 59.617f, longitude = 17.723f, harbourReach = 8f, blurb = "The Swedish kings' new town, minting its own coins." },
            new Place { name = "Visby", modern = "Gotland", kind = PlaceKind.Town, latitude = 57.64f, longitude = 18.296f, blurb = "Gotland's harbour, rich with silver from the east." },
            // The west over the sea.
            new Place { name = "Lindisfarne", modern = "Holy Island", kind = PlaceKind.Monastery, latitude = 55.669f, longitude = -1.801f, blurb = "The holy island whose raid in 793 began it all." },
            new Place { name = "Iona", modern = "Iona", kind = PlaceKind.Monastery, latitude = 56.335f, longitude = -6.392f, blurb = "Columba's monastery on its island off Mull, rich in gold and books." },
            new Place { name = "Jorvik", modern = "York", kind = PlaceKind.Town, latitude = 53.958f, longitude = -1.08f, harbourReach = 60f, blurb = "The Viking kingdom's capital, up the Ouse from the Humber." },
            new Place { name = "Dyflin", modern = "Dublin", kind = PlaceKind.Town, latitude = 53.345f, longitude = -6.267f, harbourReach = 8f, blurb = "The Norse slave and silver market at the black pool on the Liffey." },
            new Place { name = "Kirkwall", modern = "Orkney", kind = PlaceKind.Hall, latitude = 58.984f, longitude = -2.96f, blurb = "The Orkney jarls' seat, halfway to everywhere." },
            new Place { name = "Jarlshof", modern = "Shetland", kind = PlaceKind.Landing, latitude = 59.869f, longitude = -1.29f, blurb = "A farm and landing at the south tip of Shetland." },
            new Place { name = "Tinganes", modern = "Tórshavn", kind = PlaceKind.Hall, latitude = 62.01f, longitude = -6.77f, blurb = "The Faroes' thing site on its rocky point." },
            new Place { name = "Reykjavík", modern = "Reykjavík", kind = PlaceKind.Landing, latitude = 64.147f, longitude = -21.94f, blurb = "Ingólfr's smoky bay, where Iceland's settlement began." },
            new Place { name = "Lundenwic", modern = "London", kind = PlaceKind.Town, latitude = 51.512f, longitude = -0.12f, harbourReach = 70f, blurb = "The English trading town far up the Thames." },
        };

        public static Place Find(string name)
        {
            foreach (var p in All) if (p.name == name) return p;
            return null;
        }

        /// <summary>A place's position in the world (global game coordinates, y = 0).</summary>
        public static Vector3 Position(WorldMap map, Place p) { return map.ToWorld(p.latitude, p.longitude); }

        /// <summary>The least depth a harbour needs for the big ships (m).</summary>
        public const float HarbourDepth = 2.5f;

        // Harbours worked out ahead of time (tools/world/bake_harbours.sh) by FloodHarbour: name -> lat, lon.
        static Dictionary<string, Vector2> baked;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { baked = null; }

        /// <summary>Use these baked harbours ("name|lat|lon" lines); null or empty clears them.</summary>
        public static void LoadHarbours(string text)
        {
            baked = new Dictionary<string, Vector2>();
            if (string.IsNullOrEmpty(text)) return;
            foreach (var line in text.Split('\n'))
            {
                var parts = line.Trim().Split('|');
                float lat, lon;
                if (parts.Length == 3 && float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out lat)
                    && float.TryParse(parts[2], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out lon))
                    baked[parts[0]] = new Vector2(lat, lon);
            }
        }

        static void EnsureBaked()
        {
            if (baked != null) return;
            var asset = Resources.Load<TextAsset>("World/harbours");
            LoadHarbours(asset != null ? asset.text : null);
        }

        /// <summary>
        /// The place's harbour: the baked one (water joined to the open sea, found by <see cref="FloodHarbour"/>) if
        /// there is one, else the nearest spot of sea at least <see cref="HarbourDepth"/> deep within its reach,
        /// searched outward in rings on the real map. False if there is none.
        /// </summary>
        public static bool Harbour(WorldMap map, Place p, out Vector3 harbour)
        {
            EnsureBaked();
            Vector2 ll;
            if (baked.TryGetValue(p.name, out ll)) { harbour = map.ToWorld(ll.x, ll.y); return true; }
            var at = Position(map, p);
            harbour = at;
            float step = 150f * WorldMap.Scale, reach = p.harbourReach * 1000f * WorldMap.Scale;
            if (Depth(map, at) >= HarbourDepth) return true;
            for (float r = step; r <= reach; r += step)
            {
                int n = Mathf.Max(8, Mathf.CeilToInt(2f * Mathf.PI * r / step));
                float bestDepth = 0f;
                Vector3 best = at;
                for (int i = 0; i < n; i++)
                {
                    float a = i * 2f * Mathf.PI / n;
                    var q = at + new Vector3(Mathf.Cos(a) * r, 0f, Mathf.Sin(a) * r);
                    float d = Depth(map, q);
                    if (d >= HarbourDepth && d > bestDepth) { bestDepth = d; best = q; }
                }
                if (bestDepth > 0f) { harbour = best; return true; }
            }
            return false;
        }

        /// <summary>
        /// The place's harbour, the thorough way: flood the water that joins the open sea (anything at least
        /// <see cref="HarbourDepth"/> deep, spreading in from the deep sea and the edges of the search), then take the
        /// flooded spot nearest the place, within its reach. Slow (a flood over tens of kilometres), so it's baked.
        /// </summary>
        public static bool FloodHarbour(WorldMap map, Place p, out Vector3 harbour)
        {
            float s = WorldMap.Scale;
            var at = Position(map, p);
            harbour = at;
            float reach = p.harbourReach * 1000f;
            float cell = 100f;
            int n = Mathf.CeilToInt((reach + 20000f) / cell); // far enough out that a lagoon rarely touches the edge
            int size = 2 * n + 1;
            var depth = new float[size * size];
            for (int j = 0; j < size; j++)
                for (int i = 0; i < size; i++)
                    depth[j * size + i] = -TerrainDetail.Height(map, at.x + (i - n) * cell * s, at.z + (j - n) * cell * s) / s;
            var wet = new bool[size * size];
            var queue = new Queue<int>();
            for (int j = 0; j < size; j++)
                for (int i = 0; i < size; i++)
                {
                    int id = j * size + i;
                    bool edge = i == 0 || j == 0 || i == size - 1 || j == size - 1;
                    if ((edge && depth[id] >= HarbourDepth) || depth[id] >= 15f) { wet[id] = true; queue.Enqueue(id); }
                }
            while (queue.Count > 0)
            {
                int c = queue.Dequeue(), i = c % size, j = c / size;
                for (int k = 0; k < 8; k++)
                {
                    // All eight neighbours, so a narrow channel running on the diagonal still joins up.
                    int ni = i + Dx[k], nj = j + Dz[k];
                    if (ni < 0 || nj < 0 || ni >= size || nj >= size) continue;
                    int id = nj * size + ni;
                    if (wet[id] || depth[id] < HarbourDepth) continue;
                    wet[id] = true;
                    queue.Enqueue(id);
                }
            }
            float best = float.MaxValue;
            for (int j = 0; j < size; j++)
                for (int i = 0; i < size; i++)
                {
                    if (!wet[j * size + i]) continue;
                    // Not right on the edge of the deep: a spot with a little room round it.
                    float d = ((i - n) * (i - n) + (j - n) * (j - n)) * cell * cell;
                    if (d < best) { best = d; harbour = at + new Vector3((i - n) * cell * s, 0f, (j - n) * cell * s); }
                }
            return best <= reach * reach;
        }

        /// <summary>
        /// Can a ship get from this spot out to open water (at least <paramref name="openDepth"/> deep) without
        /// crossing anything shallower than <see cref="HarbourDepth"/>? A flood fill over the detailed land on a
        /// grid of <paramref name="cell"/> metres, out to <paramref name="radius"/>; reaching the edge afloat counts.
        /// </summary>
        public static bool Reachable(WorldMap map, Vector3 from, float radius = 15000f, float cell = 80f, float openDepth = 15f)
        {
            float s = WorldMap.Scale;
            int n = Mathf.CeilToInt(radius / cell);
            int size = 2 * n + 1;
            var seen = new bool[size * size];
            var queue = new Queue<int>();
            System.Func<int, int, float> depth = (i, j) => -TerrainDetail.Height(map, from.x + (i - n) * cell * s, from.z + (j - n) * cell * s) / s;
            if (depth(n, n) < HarbourDepth) return false;
            queue.Enqueue(n * size + n);
            seen[n * size + n] = true;
            while (queue.Count > 0)
            {
                int c = queue.Dequeue(), i = c % size, j = c / size;
                if (i == 0 || j == 0 || i == size - 1 || j == size - 1) return true;
                if (depth(i, j) >= openDepth) return true;
                for (int k = 0; k < 8; k++)
                {
                    // All eight neighbours, so a narrow channel running on the diagonal still joins up.
                    int ni = i + Dx[k], nj = j + Dz[k];
                    int id = nj * size + ni;
                    if (seen[id]) continue;
                    seen[id] = true;
                    if (depth(ni, nj) >= HarbourDepth) queue.Enqueue(id);
                }
            }
            return false;
        }

        static readonly int[] Dx = { 1, -1, 0, 0, 1, 1, -1, -1 }, Dz = { 0, 0, 1, -1, 1, -1, 1, -1 };

        /// <summary>Depth of water (m, real) at a global position; 0 on land.</summary>
        public static float Depth(WorldMap map, Vector3 p) { return Mathf.Max(0f, -TerrainDetail.Height(map, p.x, p.z) / WorldMap.Scale); }
    }
}
