using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Home: an island with a wooden jetty, Gunnar the trader at the end of it (he buys every chest you bring
    /// back) and the mead hall up the hill, where Bjørn sells upgrades and plays dice.
    /// The voyage starts here, moored alongside the jetty.
    /// </summary>
    public class HomeHarbour : MonoBehaviour
    {
        public static HomeHarbour Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        public static readonly IslandSpec Spec = new IslandSpec
        {
            name = "Home Fjord", centre = new Vector2(0f, -120f), radius = 52f, height = 20f, seed = 41, trees = 30, chests = 0,
            // The mead hall and its yard, and the beach where the jetty lands.
            keepClear = new[] { new Vector3(-14f, -90f, 15f), new Vector3(0f, -78f, 7f) },
            terraces = new[] { new Vector4(-14f, -91f, 12f, 7f) },
        };

        /// <summary>The mead hall stands up the hill from the jetty, its door facing the water.</summary>
        public static readonly Vector2 HallPosition = new Vector2(-14f, -92f);
        public const float HallYaw = 190f;

        /// <summary>Where Bjørn stands (x, z), just outside the hall door.</summary>
        public static Vector2 KeeperSpot
        {
            get
            {
                Vector3 local = Quaternion.Euler(0f, HallYaw, 0f) * new Vector3(2.4f, 0f, -9.6f);
                return HallPosition + new Vector2(local.x, local.z);
            }
        }

        /// <summary>Where the longship starts: alongside the jetty, bow out to sea.</summary>
        public static readonly Vector3 ShipStart = new Vector3(4.3f, 0.2f, -54f);
        public const float ShipStartHeading = 0f;
        public const float TradeRange = 3.2f;
        public const float CargoRange = 30f;
        /// <summary>The jetty runs along x = 0 from the beach out past the ship's berth.</summary>
        public const float JettyStart = -77f;
        public const int JettyPlanks = 16;
        public const float JettyTop = 1.2f;
        /// <summary>Gunnar stands at the edge of the jetty, close enough to trade over the ship's side.</summary>
        public static readonly Vector3 TraderPosition = new Vector3(1.1f, JettyTop, -56f);

        public const float KeeperRange = 3f;
        public Transform Trader { get; private set; }
        public Transform Keeper { get; private set; }
        public Island Island { get; private set; }

        public static HomeHarbour Build(Transform parent)
        {
            var home = new GameObject("Home Harbour").AddComponent<HomeHarbour>();
            home.transform.SetParent(parent, false);
            Instance = home;
            home.Island = Island.Create(home.transform, Spec);
            WorldGen.Islands.Add(home.Island);
            home.BuildJetty();
            home.BuildLonghouse();
            home.BuildTrader();
            return home;
        }

        void BuildJetty()
        {
            var jetty = new GameObject("Jetty").transform;
            jetty.SetParent(transform, false);
            var planks = new Color(0.55f, 0.4f, 0.26f);
            // Deck of planks from the shore out to deep water, standing on pilings.
            for (int i = 0; i < JettyPlanks; i++)
            {
                float z = JettyStart + i * 1.6f;
                var plank = GameObject.CreatePrimitive(PrimitiveType.Cube);
                plank.name = "Plank";
                plank.transform.SetParent(jetty, false);
                plank.transform.position = new Vector3(0f, 1.1f, z);
                plank.transform.localScale = new Vector3(3.4f, 0.2f, 1.5f);
                plank.GetComponent<Renderer>().sharedMaterial = Materials.Get(i % 2 == 0 ? planks : planks * 0.9f);
                if (i % 3 == 0)
                    foreach (float x in new[] { -1.5f, 1.5f })
                        LongshipBuilder.Deco(PrimitiveType.Cylinder, jetty, new Vector3(x, -1f, z), new Vector3(0.3f, 2.2f, 0.3f), Materials.DarkWood).position = new Vector3(x, -1f, z);
            }
            // Mooring posts.
            LongshipBuilder.Deco(PrimitiveType.Cylinder, jetty, Vector3.zero, new Vector3(0.35f, 0.7f, 0.35f), Materials.DarkWood).position = new Vector3(1.7f, 1.6f, -58f);
            LongshipBuilder.Deco(PrimitiveType.Cylinder, jetty, Vector3.zero, new Vector3(0.35f, 0.7f, 0.35f), Materials.DarkWood).position = new Vector3(1.7f, 1.6f, -67f);
        }

        void BuildLonghouse()
        {
            var house = new GameObject("Longhouse").transform;
            house.SetParent(transform, false);
            float ground = Island.Height(Spec, HallPosition.x, HallPosition.y);
            house.position = new Vector3(HallPosition.x, ground - 0.05f, HallPosition.y);
            // The door (local -z) faces down the hill towards the jetty.
            house.rotation = Quaternion.Euler(0f, HallYaw, 0f);
            var timber = new Color(0.42f, 0.28f, 0.16f);
            var turf = new Color(0.34f, 0.46f, 0.24f);
            foreach (var wall in new[] {
                new[] { 0f, 1.6f, 8f, 16f, 3.2f, 0.5f },
                new[] { -7.7f, 1.6f, 0f, 0.6f, 3.2f, 16f },
                new[] { 7.7f, 1.6f, 0f, 0.6f, 3.2f, 16f },
                new[] { -4.6f, 1.6f, -8f, 6.8f, 3.2f, 0.5f },
                new[] { 4.6f, 1.6f, -8f, 6.8f, 3.2f, 0.5f } })
            {
                var w = GameObject.CreatePrimitive(PrimitiveType.Cube);
                w.transform.SetParent(house, false);
                w.transform.localPosition = new Vector3(wall[0], wall[1], wall[2]);
                w.transform.localScale = new Vector3(wall[3], wall[4], wall[5]);
                w.GetComponent<Renderer>().sharedMaterial = Materials.Get(timber);
            }
            // Turf roof and a little smoke hole.
            MeshKit.Show(MeshKit.Roof(), house, new Vector3(0f, 3.2f, 0f), new Vector3(16.5f, 4.5f, 17f), turf, "Turf Roof");
            LongshipBuilder.Deco(PrimitiveType.Cube, house, new Vector3(0f, 7.8f, 0f), new Vector3(1.2f, 0.3f, 1.2f), new Color(0.2f, 0.18f, 0.16f));
            // Dragon heads on the gable ends.
            LongshipBuilder.Deco(PrimitiveType.Cube, house, new Vector3(0f, 7.6f, -8.6f), new Vector3(0.4f, 0.9f, 0.4f), Materials.DarkWood);
            LongshipBuilder.Deco(PrimitiveType.Cube, house, new Vector3(0f, 7.6f, 8.6f), new Vector3(0.4f, 0.9f, 0.4f), Materials.DarkWood);
            // Warm light spilling out of the door, and a sign-post with a drinking horn.
            var door = new GameObject("Hall Light").AddComponent<Light>();
            door.transform.SetParent(house, false);
            door.transform.localPosition = new Vector3(0f, 1.6f, -7f);
            door.type = LightType.Point;
            door.color = new Color(1f, 0.7f, 0.4f);
            door.range = 7f;
            door.intensity = 1.4f;
            LongshipBuilder.Deco(PrimitiveType.Cube, house, new Vector3(0f, 1.2f, -7.9f), new Vector3(2.2f, 2.6f, 0.1f), new Color(0.12f, 0.08f, 0.05f));

            // Bjørn the mead-keeper, by the door with a barrel and a dice table.
            var k = new GameObject("Bjørn the Mead-Keeper").transform;
            k.SetParent(transform, false);
            Vector2 spot = KeeperSpot;
            k.position = new Vector3(spot.x, Island.Height(Spec, spot.x, spot.y), spot.y);
            k.rotation = house.rotation * Quaternion.Euler(0f, 180f, 0f);
            VikingBuilder.Build(k, new Color(0.5f, 0.2f, 0.15f), new Color(0.55f, 0.3f, 0.15f), new Color(0.2f, 0.3f, 0.6f), false);
            LongshipBuilder.Deco(PrimitiveType.Cylinder, k, new Vector3(-1.1f, 0.5f, 0.2f), new Vector3(0.8f, 0.5f, 0.8f), Materials.Wood);  // mead barrel
            LongshipBuilder.Deco(PrimitiveType.Cube, k, new Vector3(0f, 0.45f, 1f), new Vector3(1.4f, 0.9f, 0.8f), Materials.DarkWood);      // dice table
            for (int i = 0; i < 3; i++)
                LongshipBuilder.Deco(PrimitiveType.Cube, k, new Vector3(-0.3f + i * 0.28f, 0.95f, 1f), new Vector3(0.12f, 0.12f, 0.12f), new Color(0.92f, 0.88f, 0.78f));
            Keeper = k;
        }

        void BuildTrader()
        {
            var t = new GameObject("Gunnar the Trader").transform;
            t.SetParent(transform, false);
            t.position = TraderPosition;
            // Faces the ship moored alongside.
            t.rotation = Quaternion.Euler(0f, 90f, 0f);
            VikingBuilder.Build(t, new Color(0.55f, 0.45f, 0.2f), new Color(0.85f, 0.85f, 0.8f), new Color(0.3f, 0.5f, 0.3f), false);
            // A table with scales for weighing silver.
            LongshipBuilder.Deco(PrimitiveType.Cube, t, new Vector3(0f, 0.45f, 0.9f), new Vector3(1.4f, 0.9f, 0.7f), Materials.Wood);
            LongshipBuilder.Deco(PrimitiveType.Cylinder, t, new Vector3(0.3f, 1f, 0.9f), new Vector3(0.3f, 0.02f, 0.3f), Materials.Gold);
            Trader = t;
        }

        public bool NearTrader(Vector3 p) { return Trader != null && Vector3.Distance(p, Trader.position) < TradeRange; }

        public bool NearKeeper(Vector3 p) { return Keeper != null && Vector3.Distance(p, Keeper.position) < KeeperRange; }

        public bool ShipInRange(Longship ship) { return ship != null && Trader != null && Vector3.Distance(ship.transform.position, Trader.position) <= CargoRange; }

        /// <summary>How many chests are stowed on the ship and what Gunnar would pay for them now.</summary>
        public static int CargoValue(Longship ship, out int count)
        {
            count = 0;
            int gold = 0;
            foreach (var chest in TreasureChest.All)
            {
                if (chest == null || !chest.Stowed(ship)) continue;
                count++;
                gold += chest.Value;
            }
            return gold;
        }

        /// <summary>Buy one chest. Returns the gold paid.</summary>
        public int Sell(TreasureChest chest)
        {
            if (chest == null || chest.Sold) return 0;
            int gold = chest.Value;
            Fortune.Current.Gold += gold;
            Fortune.Current.ChestsSold++;
            Fortune.Current.GoldPlundered += gold;
            Fortune.Current.AddFavour(Ravens.FavourPerChest);
            chest.Sold = true;
            chest.Carried = false;
            chest.gameObject.SetActive(false);
            Destroy(chest.gameObject);
            return gold;
        }

        /// <summary>Buy every chest stowed on the ship, if it's moored close enough. Returns (chests, gold).</summary>
        public int SellCargo(Longship ship, out int count)
        {
            count = 0;
            int gold = 0;
            if (!ShipInRange(ship)) return 0;
            foreach (var chest in TreasureChest.All.ToArray())
            {
                if (chest == null || !chest.Stowed(ship)) continue;
                gold += Sell(chest);
                count++;
            }
            return gold;
        }
    }
}
