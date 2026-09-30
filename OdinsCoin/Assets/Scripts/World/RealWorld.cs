using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The voyage in the real North: the land streamed in from the real map, the real places with their
    /// settlements, a floating origin so positions stay exact however far you sail, and a long view to the
    /// mountains. Home is a little island in open water off <see cref="HomePlace"/> (Kaupang), where the jetty,
    /// the mead hall, Gunnar and Bjorn are.
    /// </summary>
    public static class RealWorld
    {
        /// <summary>Sail the real North (when the map is there); off, the voyage is on the storybook isles.</summary>
        public static bool Enabled = true;
        public const string HomePlace = "Kaupang";
        /// <summary>How much open water the home island needs round it (m).</summary>
        public const float HomeWater = 450f;
        /// <summary>Round the home harbour nothing grows, so its jetty, hall and huts stand clear (m).</summary>
        public const float HomeClearing = 220f;
        /// <summary>The view: the far clip and where the fog starts and ends (m), long enough to see the mountains.</summary>
        public const float FarClip = 70000f, FogStart = 1500f, FogEnd = 45000f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Active = false; }

        /// <summary>Is this voyage in the real North?</summary>
        public static bool Active { get; private set; }

        /// <summary>
        /// Where the home island goes (global): the nearest open water to the place's harbour with at least
        /// <see cref="HomeWater"/> of sea all round it, so the island, the jetty and the ship's berth are all afloat.
        /// </summary>
        public static bool FindHomeWater(WorldMap map, Place place, out Vector3 home)
        {
            Vector3 harbour;
            if (!Places.Harbour(map, place, out harbour)) harbour = Places.Position(map, place);
            float s = WorldMap.Scale;
            for (float r = 0f; r <= 25000f * s; r += 250f * s)
            {
                int n = r < 1f ? 1 : Mathf.Max(8, Mathf.CeilToInt(2f * Mathf.PI * r / (250f * s)));
                for (int i = 0; i < n; i++)
                {
                    float a = i * 2f * Mathf.PI / n;
                    var p = harbour + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                    if (OpenWater(map, p, HomeWater * s)) { home = p; return true; }
                }
            }
            home = harbour;
            return false;
        }

        /// <summary>Is there sea at least 3 m deep everywhere within <paramref name="radius"/> of a point?</summary>
        public static bool OpenWater(WorldMap map, Vector3 p, float radius)
        {
            if (Places.Depth(map, p) < 3f) return false;
            foreach (float k in new[] { 0.33f, 0.66f, 1f })
                for (int i = 0; i < 16; i++)
                {
                    float a = i * Mathf.PI / 8f;
                    var q = p + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * radius * k;
                    if (TerrainDetail.Height(map, q.x, q.z) > -3f * WorldMap.Scale) return false;
                }
            return true;
        }

        /// <summary>
        /// Set the voyage in the real North: put the origin so the home island (built round scene
        /// <paramref name="homeCentre"/>) sits in open water off Kaupang, and bring in the land, the places and the
        /// floating origin. Returns false (and changes nothing) if the map isn't there.
        /// </summary>
        public static bool Setup(Transform world, Transform focus, Camera cam, Vector3 homeCentre)
        {
            Active = false;
            if (!Enabled) return false;
            var map = WorldMap.Current;
            var place = Places.Find(HomePlace);
            if (map == null || place == null) return false;
            Vector3 home;
            FindHomeWater(map, place, out home);
            WorldOrigin.OffsetX = home.x - homeCentre.x;
            WorldOrigin.OffsetZ = home.z - homeCentre.z;
            WorldOrigin.Containers.Add(world);
            WorldTerrain.Create(null, map, focus);
            // Trees, rocks and grass on the land round you, clear of the home harbour's buildings.
            Scenery.Clearings.Add(new Vector3(home.x, home.z, HomeClearing));
            SceneryField.Create(null, map, focus);
            var sites = world.gameObject.AddComponent<PlaceSites>();
            sites.Setup(map, world, focus);
            world.gameObject.AddComponent<FloatingOrigin>().follow = focus;
            if (cam != null)
            {
                cam.farClipPlane = FarClip;
                cam.nearClipPlane = 0.3f;
            }
            RenderSettings.fogStartDistance = GameBootstrap.FogStart = FogStart;
            RenderSettings.fogEndDistance = GameBootstrap.FogEnd = FogEnd;
            Active = true;
            return true;
        }

        /// <summary>The nearest real place to a scene position, and how far it is (m).</summary>
        public static Place Nearest(Vector3 scene, out float distance, out float bearing)
        {
            distance = float.MaxValue;
            bearing = 0f;
            var map = WorldMap.Current;
            if (map == null) return null;
            double gx = WorldOrigin.GlobalX(scene), gz = WorldOrigin.GlobalZ(scene);
            Place best = null;
            foreach (var p in Places.All)
            {
                var at = Places.Position(map, p);
                float dx = (float)(at.x - gx), dz = (float)(at.z - gz);
                float d = Mathf.Sqrt(dx * dx + dz * dz);
                if (d < distance) { distance = d; best = p; bearing = Mathf.Repeat(Mathf.Atan2(dx, dz) * Mathf.Rad2Deg, 360f); }
            }
            return best;
        }
    }

    /// <summary>Puts a built model's pieces into the scene under a transform, drawn with the storybook materials.</summary>
    public static class ModelView
    {
        public static void Show(VikingModel model, Transform root)
        {
            foreach (var piece in model.Pieces)
            {
                var go = new GameObject(piece.ink ? "Ink" : "Part");
                go.transform.SetParent(root, false);
                go.AddComponent<MeshFilter>().sharedMesh = piece.mesh.ToMesh(piece.joint);
                go.AddComponent<MeshRenderer>().sharedMaterial = piece.ink ? Materials.Get(piece.color, 0f) : Materials.GetDrawn(piece.color, piece.surface);
            }
            if (root.GetComponent<OwnInk>() == null) root.gameObject.AddComponent<OwnInk>();
        }
    }

    /// <summary>
    /// Builds the real places' settlements as the ship comes within sight of them, and takes them down again
    /// once it has sailed well away.
    /// </summary>
    public class PlaceSites : MonoBehaviour
    {
        public const float BuildWithin = 25000f, DropBeyond = 35000f;
        WorldMap map;
        Transform world, focus;
        readonly Dictionary<Place, Transform> built = new Dictionary<Place, Transform>();
        readonly Dictionary<Place, Vector3> harbours = new Dictionary<Place, Vector3>();
        readonly Dictionary<Place, List<Plot>> layouts = new Dictionary<Place, List<Plot>>();
        readonly Dictionary<Place, List<TreasureChest>> loot = new Dictionary<Place, List<TreasureChest>>();
        float nextCheck;

        public void Setup(WorldMap map, Transform world, Transform focus)
        {
            this.map = map;
            this.world = world;
            this.focus = focus;
        }

        /// <summary>The settlements standing now.</summary>
        public int Count { get { return built.Count; } }

        void Update()
        {
            if (map == null || focus == null || Time.time < nextCheck) return;
            nextCheck = Time.time + 1f;
            double gx = WorldOrigin.GlobalX(focus.position), gz = WorldOrigin.GlobalZ(focus.position);
            foreach (var place in Places.All)
            {
                Vector3 harbour;
                if (!harbours.TryGetValue(place, out harbour)) { Places.Harbour(map, place, out harbour); harbours[place] = harbour; }
                float d = Mathf.Sqrt((float)((harbour.x - gx) * (harbour.x - gx) + (harbour.z - gz) * (harbour.z - gz)));
                Transform site;
                bool have = built.TryGetValue(place, out site);
                if (!have && d < BuildWithin * WorldMap.Scale) { built[place] = Build(place, harbour); break; } // one a second, no stalls
                if (have && d > DropBeyond * WorldMap.Scale) { if (site != null) Destroy(site.gameObject); built.Remove(place); loot.Remove(place); continue; }
                // Close in, the guards turn out and the plunder is there for the taking.
                if (have && site != null && !loot.ContainsKey(place) && d < PlaceLife.PopulateWithin * WorldMap.Scale) loot[place] = Populate(place, site);
            }
            CheckRaids();
        }

        /// <summary>A place is plundered once every one of its chests has been carried off.</summary>
        void CheckRaids()
        {
            foreach (var kv in loot)
            {
                if (kv.Value.Count == 0 || PlaceLife.Raided.Contains(kv.Key.name)) continue;
                Transform site;
                built.TryGetValue(kv.Key, out site);
                bool left = false;
                foreach (var chest in kv.Value)
                    if (chest != null && !chest.Sold && !chest.Carried && site != null && chest.transform.IsChildOfOrSelf(site)) left = true;
                if (left) continue;
                PlaceLife.Raided.Add(kv.Key.name);
                Fortune.Current.AddFavour(0.1f);
                var up = Upgrades.Current;
                if (up.Commission == kv.Key.name)
                {
                    // Bjorn's commission done: his silver comes by the next ship.
                    Fortune.Current.Gold += up.CommissionReward;
                    CombatHud.Banner(kv.Key.name.ToUpper() + " IS PLUNDERED", "Bjorn's commission is done: +" + up.CommissionReward + " gold. The skalds will sing of it.");
                    up.Commission = null;
                    up.CommissionReward = 0;
                    SaveGame.Save();
                }
                else CombatHud.Banner(kv.Key.name.ToUpper() + " IS PLUNDERED", "The skalds will sing of it. Sell the chests at a market town, or at home.");
            }
        }

        /// <summary>Put a place's chests round its main building and its guards round them (none if already plundered).</summary>
        List<TreasureChest> Populate(Place place, Transform site)
        {
            var chests = new List<TreasureChest>();
            // The people who live there, going about their day (they've fled a place you've plundered).
            List<Plot> folkPlots;
            Vector3 folkHarbour;
            if (!PlaceLife.Raided.Contains(place.name) && layouts.TryGetValue(place, out folkPlots) && harbours.TryGetValue(place, out folkHarbour))
            {
                var stops = PlaceLife.Doorsteps(folkPlots, (x, z) => TerrainDetail.Height(map, x, z));
                int folk = stops.Count > 1 ? PlaceLife.FolkOf(place, folkPlots.Count) : 0;
                int folkSeed = place.name.GetHashCode() & 0x3fffffff;
                for (int f = 0; f < folk; f++) Townsperson.Create(site, folkHarbour, map, folkPlots, stops, folkSeed + f * 17);
            }
            if (PlaceLife.Raided.Contains(place.name)) return chests;
            var plunder = PlaceLife.PlunderOf(place.kind);
            if (plunder.chests == 0 && plunder.guards == 0) return chests;
            List<Plot> plots;
            if (!layouts.TryGetValue(place, out plots)) return chests;
            var main = PlaceLife.MainBuilding(plots);
            int seed = place.name.GetHashCode() & 0x7fffffff;
            var rng = new System.Random(seed);
            foreach (var at in PlaceLife.Stations(map, plots, main, plunder.chests, 3f, seed))
            {
                var scene = WorldOrigin.ToScene(at.x, at.z, TerrainDetail.Height(map, at.x, at.z) + 0.05f);
                chests.Add(TreasureChest.Create(site, scene, (float)rng.NextDouble() * 360f, plunder.minGold + rng.Next(plunder.maxGold - plunder.minGold + 1)));
            }
            int n = 0;
            foreach (var at in PlaceLife.Stations(map, plots, main, plunder.guards, 8f, seed + 1))
            {
                var look = PlaceLife.SaxonGuards(place) ? NpcHeroes.Saxon(seed + n) : NpcHeroes.NorseGuard(seed + n);
                n++;
                Saxon.Create(site, WorldOrigin.ToScene(at.x, at.z, TerrainDetail.Height(map, at.x, at.z) + 0.3f), look);
            }
            return chests;
        }

        Transform Build(Place place, Vector3 harbour)
        {
            var root = new GameObject(place.name).transform;
            root.SetParent(world, false);
            root.position = WorldOrigin.ToScene(harbour.x, harbour.z, 0f);
            var look = new BuildingLook();
            int i = 0;
            var plots = Settlements.Layout(map, place);
            layouts[place] = plots;
            // Nothing grows on the plots.
            foreach (var plot in plots)
                if (plot.kind != BuildingKind.Jetty) Scenery.Clearings.Add(new Vector3(plot.at.x, plot.at.z, Scenery.PlotClearing(plot.kind)));
            foreach (var plot in plots)
            {
                var b = new GameObject(plot.kind.ToString()).transform;
                b.SetParent(root, false);
                b.position = WorldOrigin.ToScene(plot.at.x, plot.at.z, plot.at.y);
                b.rotation = Quaternion.Euler(0f, plot.yaw, 0f);
                ModelView.Show(Buildings.Build(plot.kind, look, i++), b);
                Buildings.AddSolid(b.gameObject, plot.kind);
                // Bollards along both sides of the jetty for visiting ships to make fast to.
                if (plot.kind == BuildingKind.Jetty)
                    foreach (float z in new[] { 18f, 34f })
                        foreach (float x in new[] { -2.2f, 2.2f })
                            Seamanship.AddBollard(b, new Vector3(x, 1.35f, z));
                // A market town's trader waits on the jetty.
                if (plot.kind == BuildingKind.Jetty && PlaceLife.HasMarket(place)) Market.Create(b, place, new Vector3(-1.1f, 1.35f, 12f));
            }
            return root;
        }
    }
}
