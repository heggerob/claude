using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// A merchant sailing between the real market towns: a Skerrycutter in plain trading colours, on the same
    /// physics as every ship, steering for her port, rowing when the wind's ahead and bearing away from shoal water.
    /// Leave her be, or come alongside: outnumbered, she strikes her sail and her cargo is there for the taking.
    /// </summary>
    [RequireComponent(typeof(Longship))]
    public class Merchant : MonoBehaviour
    {
        public static readonly List<Merchant> All = new List<Merchant>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }

        /// <summary>How close your ship must come for her to give up (m), and how near her port she's arrived (m).</summary>
        public const float StrikeRange = 30f, ArriveRange = 400f;

        public Longship Ship { get; private set; }
        public Place Destination { get; private set; }
        public bool Struck { get; private set; }

        public static Merchant Spawn(Transform parent, Vector3 scenePos, float heading, Place destination, int seed)
        {
            var rng = new System.Random(seed);
            Color[] sails = { new Color(0.2f, 0.32f, 0.55f), new Color(0.3f, 0.45f, 0.3f), new Color(0.55f, 0.45f, 0.2f) };
            var look = new ShipLook();
            look.sail = sails[rng.Next(sails.Length)];
            look.stripe = new Color(0.88f, 0.84f, 0.72f);
            look.dragon = new Color(0.35f, 0.26f, 0.18f);
            var ship = Longship.Create(parent, scenePos, heading, ShipDesign.Skerrycutter, look);
            ship.gameObject.name = "Merchant for " + destination.name;
            ship.SailTarget = 1f;
            var m = ship.gameObject.AddComponent<Merchant>();
            m.Ship = ship;
            m.Destination = destination;
            // Her cargo, lashed on deck.
            int chests = 1 + rng.Next(2);
            for (int i = 0; i < chests; i++)
                TreasureChest.Create(ship.transform, ship.transform.TransformPoint(new Vector3(0f, ship.DeckY, (i - 0.5f) * 2.4f)), heading + 90f, 60 + rng.Next(100));
            // A trader at the steering oar.
            var t = new GameObject("Merchant Skipper").transform;
            t.SetParent(ship.transform, false);
            t.localPosition = ship.Parts.helm.localPosition;
            var spec = NpcHeroes.Merchant(seed);
            HeroIdle.Add(t, HeroBuilder.Build(t, spec), spec.weapon);
            return m;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Update()
        {
            var boot = GameBootstrap.Instance;
            var map = WorldMap.Current;
            if (Ship == null || boot == null || map == null) return;
            var me = transform.position;

            // Come alongside and she gives up.
            if (!Struck && boot.Ship != null && Vector3.Distance(boot.Ship.transform.position, me) < StrikeRange + Ship.HalfLength + boot.Ship.HalfLength)
            {
                Struck = true;
                CombatHud.Banner("A MERCHANT STRIKES HER SAIL", "Bound for " + Destination.name + ", she gives up her cargo without a fight. Take it aboard.");
            }
            if (Struck)
            {
                Ship.SailTarget = 0f;
                Ship.Rowing = false;
                Ship.RudderInput = 0f;
                return;
            }

            // Steer for her port, but never onto the shallows.
            Vector3 harbour;
            Places.Harbour(map, Destination, out harbour);
            var goal = WorldOrigin.ToScene(harbour.x, harbour.z, 0f);
            if (Vector3.Distance(new Vector3(goal.x, me.y, goal.z), me) < ArriveRange) { Destroy(gameObject); return; }
            float rudder = SeaMath.SteerTowards(me, Ship.Heading, goal);
            float safe = Ship.Design.draught + 1.5f;
            var ahead = me + transform.forward * (60f + Mathf.Max(0f, Ship.SpeedKnots) * 12f);
            if (Longship.DepthAt(ahead) < safe)
            {
                float port = Longship.DepthAt(me + Quaternion.Euler(0f, -45f, 0f) * transform.forward * 80f);
                float star = Longship.DepthAt(me + Quaternion.Euler(0f, 45f, 0f) * transform.forward * 80f);
                rudder = star >= port ? 1f : -1f;
            }
            Ship.RudderInput = rudder;

            // Sail when she can; row when the wind is too far ahead.
            float fromWind = 180f - Mathf.Abs(Mathf.DeltaAngle(Ship.Heading, Wind.Angle));
            bool inIrons = fromWind < Seamanship.ClosestToWind(Ship.Design, Wind.Knots) + 5f;
            Ship.SailTarget = inIrons ? 0f : 1f;
            Ship.Rowing = inIrons;
        }
    }

    /// <summary>Keeps a few merchants sailing between the market towns within sight of you, out on the real sea.</summary>
    public class MerchantTraffic : MonoBehaviour
    {
        public const int MaxShips = 3;
        public const float SpawnMin = 3000f, SpawnMax = 6000f, DropBeyond = 15000f;
        float next = 30f;

        void Update()
        {
            if (!RealWorld.Active || TimeWarp.OnPassage) return; // on a long passage she's past them before they're hull-up
            next -= Time.deltaTime;
            if (next > 0f) return;
            next = 20f;
            var boot = GameBootstrap.Instance;
            var map = WorldMap.Current;
            if (boot == null || boot.Ship == null || map == null) return;
            var ship = boot.Ship.transform.position;
            foreach (var m in Merchant.All.ToArray())
                if (m != null && Vector3.Distance(m.transform.position, ship) > DropBeyond) Destroy(m.gameObject);
            if (Merchant.All.Count >= MaxShips || Random.value > 0.5f) return;

            // Somewhere out in open water, a few kilometres off, bound for one of the nearer markets.
            for (int tries = 0; tries < 6; tries++)
            {
                float a = Random.value * Mathf.PI * 2f, r = Random.Range(SpawnMin, SpawnMax);
                var at = ship + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * r;
                var global = new Vector3((float)WorldOrigin.GlobalX(at), 0f, (float)WorldOrigin.GlobalZ(at));
                if (!RealWorld.OpenWater(map, global, 60f)) continue;
                var port = PickPort(map, global);
                if (port == null) return;
                Vector3 harbour;
                Places.Harbour(map, port, out harbour);
                float heading = Mathf.Atan2(harbour.x - global.x, harbour.z - global.z) * Mathf.Rad2Deg;
                Merchant.Spawn(boot.transform, new Vector3(at.x, 0.2f, at.z), heading, port, Random.Range(0, 100000));
                return;
            }
        }

        /// <summary>One of the three market towns nearest a point, at random.</summary>
        public static Place PickPort(WorldMap map, Vector3 global)
        {
            var markets = new List<Place>();
            foreach (var p in Places.All) if (PlaceLife.HasMarket(p)) markets.Add(p);
            markets.Sort((a, b) => (Places.Position(map, a) - global).sqrMagnitude.CompareTo((Places.Position(map, b) - global).sqrMagnitude));
            if (markets.Count == 0) return null;
            return markets[Random.Range(0, Mathf.Min(3, markets.Count))];
        }
    }
}
