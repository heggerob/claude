using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Decides when the sea turns against you: Danish raiders out in open water, Jörmungandr when its gaze
    /// is on you (or, rarely, in the deep far from land). The home fjord is always safe. Also shows the water
    /// sloshing in your hull, and sends you home if the ship founders.
    /// </summary>
    public class SeaDangers : MonoBehaviour
    {
        public const float SafeRadius = 110f;      // around the home fjord
        public const float DeepSeaDistance = 260f; // from home, where the serpent may come uninvited

        float nextRaider = 120f, nextSerpentCheck = 60f, gazeTimer;
        Transform bilge;

        public static bool InSafeWaters(Vector3 p)
        {
            return Vector2.Distance(new Vector2(p.x, p.z), HomeHarbour.CentreNow) < SafeRadius + HomeHarbour.Spec.radius * 0.5f;
        }

        /// <summary>A spot this far from <paramref name="near"/> that's open water, or null if none found.</summary>
        public static Vector3? OpenWater(Vector3 near, float distance, System.Func<float> random)
        {
            for (int i = 0; i < 12; i++)
            {
                float a = random() * Mathf.PI * 2f;
                var p = near + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * distance;
                bool clear = !InSafeWaters(p);
                if (RealWorld.Active)
                {
                    // The real coast: deep water well clear of any shore.
                    var map = WorldMap.Current;
                    double gx = WorldOrigin.GlobalX(p), gz = WorldOrigin.GlobalZ(p);
                    if (map == null || !RealWorld.OpenWater(map, new Vector3((float)gx, 0f, (float)gz), 40f)) clear = false;
                }
                else
                    foreach (var s in WorldGen.Specs) if (Island.Height(s, p.x, p.z) > -3f || Vector2.Distance(s.centre, new Vector2(p.x, p.z)) < s.radius * 1.3f + 10f) clear = false;
                if (clear) return new Vector3(p.x, 0.2f, p.z);
            }
            return null;
        }

        void Update()
        {
            var boot = GameBootstrap.Instance;
            if (boot == null || boot.Ship == null) return;
            var ship = boot.Ship;
            float dt = Time.deltaTime;
            Vector3 pos = ship.transform.position;
            bool safe = InSafeWaters(pos);

            // Raiders: one at a time, out in open water.
            if (!safe && Raider.All.Count == 0)
            {
                nextRaider -= dt;
                if (nextRaider <= 0f)
                {
                    var spot = OpenWater(pos, 120f, () => Random.value);
                    if (spot.HasValue)
                    {
                        Vector3 to = pos - spot.Value;
                        Raider.Spawn(boot.transform, spot.Value, Mathf.Atan2(to.x, to.z) * Mathf.Rad2Deg, ship);
                        Sfx.Play(SfxId.WarHorn, 0.5f);
                        CombatHud.Banner("A SAIL ON THE HORIZON", "Black and red: Danish raiders. They've seen you.");
                    }
                    nextRaider = Random.Range(150f, 300f);
                }
            }

            // Jörmungandr: its gaze calls it within half a minute; the deep sea sometimes does too.
            if (Serpent.Instance == null && !safe)
            {
                if (Fortune.Current.Has(FateEffect.SerpentWakes, FateKind.Curse))
                {
                    gazeTimer += dt;
                    if (gazeTimer > 25f) { Serpent.Spawn(boot.transform, ship); gazeTimer = 0f; }
                }
                nextSerpentCheck -= dt;
                if (nextSerpentCheck <= 0f)
                {
                    nextSerpentCheck = 60f;
                    float fromHome = Vector2.Distance(new Vector2(pos.x, pos.z), HomeHarbour.CentreNow);
                    if (fromHome > DeepSeaDistance && Random.value < 0.1f) Serpent.Spawn(boot.transform, ship);
                }
            }

            ShowBilge(ship);
            if (ship.Hull.Sunk) Founder(boot);
        }

        /// <summary>A dark water plane in the hull that rises as she fills.</summary>
        void ShowBilge(Longship ship)
        {
            if (bilge == null)
            {
                bilge = LongshipBuilder.Deco(PrimitiveType.Cube, ship.transform, Vector3.zero, new Vector3(ship.Beam * 0.78f, 0.02f, ship.HalfLength * 1.45f), new Color(0.12f, 0.25f, 0.3f));
                bilge.name = "Bilge Water";
            }
            float level = ship.Hull.Level;
            bilge.gameObject.SetActive(level > 0.02f);
            bilge.localPosition = new Vector3(0f, ship.DeckY + 0.06f + level * 0.55f, 0f);
        }

        /// <summary>The ship fills and goes down. The cargo lies awash at the wreck; the crew wakes up back home.</summary>
        void Founder(GameBootstrap boot)
        {
            var ship = boot.Ship;
            var onDeck = Stake.OnDeck(ship);
            int lost = onDeck.Count;
            Wreck.Sink(boot.transform, ship.transform.position, onDeck);
            foreach (var r in Raider.All.ToArray()) Destroy(r.gameObject);
            if (Serpent.Instance != null) Destroy(Serpent.Instance.gameObject);
            ship.Hull.Reset();
            Sfx.Play(SfxId.Splash, 1f);
            ship.Relocate(HomeHarbour.BerthNow(ship.Design), HomeHarbour.ShipStartHeading);
            if (boot.Player != null) boot.Player.ReturnToShip();
            nextRaider = 180f;
            CombatHud.Banner("RÁN TAKES YOUR SHIP", lost > 0
                ? lost + " chest" + (lost == 1 ? " lies" : "s lie") + " awash where she sank. Your kin patch her up at home: sail back and fetch them."
                : "Your kin tow the wreck home and patch it up.");
        }
    }
}
