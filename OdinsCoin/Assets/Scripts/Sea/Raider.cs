using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// A Danish raider: a longship with a black-and-red sail and a crew of archers. It comes alongside and
    /// shoots, then turns to ram. Sink it by ramming it bow-first at speed or hacking at its strakes with your
    /// axe; it goes down leaving a chest of plunder floating on the waves.
    /// </summary>
    [RequireComponent(typeof(Longship))]
    public class Raider : MonoBehaviour
    {
        public static readonly List<Raider> All = new List<Raider>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { All.Clear(); }

        public const float MaxHull = 120f, StandOff = 13f, ArrowRange = 45f, VolleyEvery = 2.6f;
        const float ShadowTime = 22f, RamTime = 12f, GiveUpDistance = 380f;

        public Longship Ship { get; private set; }
        public float Hull { get; private set; }
        public bool Sinking { get; private set; }
        public bool Ramming { get; private set; }

        Longship target;
        readonly List<Transform> archers = new List<Transform>();
        float nextVolley, modeTimer, sinkTime, lastRamHit = -10f;

        public static Raider Spawn(Transform parent, Vector3 position, float heading, Longship target)
        {
            var ship = Longship.Create(parent, position, heading, new Color(0.12f, 0.1f, 0.1f), new Color(0.6f, 0.12f, 0.1f));
            ship.gameObject.name = "Raider";
            var r = ship.gameObject.AddComponent<Raider>();
            r.Ship = ship;
            r.target = target;
            r.Hull = MaxHull;
            r.nextVolley = Time.time + 4f;
            // Archers along the deck: leather jerkins, dark beards, black shields.
            for (int i = 0; i < 4; i++)
            {
                var a = new GameObject("Archer").transform;
                a.SetParent(ship.transform, false);
                a.localPosition = new Vector3(i % 2 == 0 ? -1f : 1f, LongshipBuilder.DeckHeight + 0.05f, -4.5f + i * 2.6f);
                VikingBuilder.Build(a, new Color(0.35f, 0.25f, 0.18f), new Color(0.15f, 0.12f, 0.1f), new Color(0.1f, 0.1f, 0.1f), true);
                r.archers.Add(a);
            }
            return r;
        }

        void OnEnable() { All.Add(this); }
        void OnDisable() { All.Remove(this); }

        void Update()
        {
            if (Sinking) { Sink(); return; }
            if (target == null) return;
            Vector3 me = transform.position, them = target.transform.position;
            float dist = Vector3.Distance(me, them);
            if (dist > GiveUpDistance) { Destroy(gameObject); return; }

            // Shadow the ship to shoot, then peel off and ram it.
            modeTimer += Time.deltaTime;
            if (!Ramming && modeTimer > ShadowTime && dist < 40f) { Ramming = true; modeTimer = 0f; if (dist < 90f) CombatHud.Banner("THE RAIDER TURNS TO RAM!", "Get out of the way, or ram it first."); }
            else if (Ramming && modeTimer > RamTime) { Ramming = false; modeTimer = 0f; }

            Vector3 goal = Ramming ? them : SeaMath.InterceptPoint(them, target.transform.forward, Compat.Velocity(target.Body), me, StandOff);
            Ship.RudderInput = SeaMath.SteerTowards(me, Ship.Heading, goal);
            // Sail when the wind helps, row when it doesn't.
            bool sailing = ShipTuning.SailEfficiency(transform.forward, Wind.Direction) > 0.35f;
            Ship.SailTarget = sailing ? (Ramming || dist > 30f ? 1f : 0.5f) : 0f;
            Ship.Rowing = !sailing || Ramming;

            // The crew face the enemy.
            foreach (var a in archers)
            {
                Vector3 look = them - a.position;
                look.y = 0f;
                if (look.sqrMagnitude > 0.1f) a.rotation = Quaternion.LookRotation(look);
            }

            if (!Ramming && dist < ArrowRange && Time.time >= nextVolley)
            {
                nextVolley = Time.time + VolleyEvery;
                Volley();
            }
            CheckRams();
        }

        void Volley()
        {
            float hw, k, g;
            foreach (var a in archers)
            {
                if (Random.value < 0.35f) continue;
                float z = Random.Range(-6f, 6f);
                LongshipBuilder.Station(z / (LongshipBuilder.Length / 2f), out hw, out k, out g);
                Vector3 local = new Vector3(Random.Range(-hw + 0.4f, hw - 0.4f), LongshipBuilder.DeckHeight + 0.2f, z);
                // Aim at the player when they're on deck, more or less.
                var player = GameBootstrap.Instance != null ? GameBootstrap.Instance.Player : null;
                Vector3 aim = player != null && player.OnShip && Random.value < 0.6f
                    ? player.transform.position + Vector3.up * 1.2f + Random.insideUnitSphere * 1.2f
                    : target.transform.TransformPoint(local);
                Arrow.Shoot(transform.parent, a.position + Vector3.up * 1.6f, aim, target);
            }
        }

        /// <summary>Bow-first into the other ship at speed: whoever's bow it is does the damage.</summary>
        void CheckRams()
        {
            if (Time.time - lastRamHit < 1.5f) return;
            float half = LongshipBuilder.Length / 2f - 0.5f;
            // The player rams us.
            Vector3 theirBow = target.transform.TransformPoint(new Vector3(0f, 0.3f, half));
            if (SeaMath.InsideHull(transform.InverseTransformPoint(theirBow)))
            {
                float closing = Vector3.Dot(target.Body.GetPointVelocity(theirBow) - Ship.Body.GetPointVelocity(theirBow), target.transform.forward);
                float dmg = SeaMath.RamDamage(closing, Fortune.Current.MeleeDamageMultiplier);
                if (dmg > 0f)
                {
                    lastRamHit = Time.time;
                    TakeDamage(dmg, theirBow);
                    Sfx.At(SfxId.RamCrash, theirBow);
                    CombatHud.Number(theirBow + Vector3.up * 2f, "RAMMED! -" + Mathf.RoundToInt(dmg), new Color(1f, 0.8f, 0.3f));
                    return;
                }
            }
            // We ram the player.
            Vector3 ourBow = transform.TransformPoint(new Vector3(0f, 0.3f, half));
            if (SeaMath.InsideHull(target.transform.InverseTransformPoint(ourBow)))
            {
                float closing = Vector3.Dot(Ship.Body.GetPointVelocity(ourBow) - target.Body.GetPointVelocity(ourBow), transform.forward);
                if (SeaMath.RamDamage(closing, 1f) > 0f)
                {
                    lastRamHit = Time.time;
                    target.Hull.Holes += closing > 4f ? 2 : 1;
                    target.Hull.Flood(0.08f);
                    Sfx.At(SfxId.RamCrash, ourBow);
                    CombatHud.Banner("RAMMED!", "The strakes are stove in: bail and plug the holes [E].");
                    Ramming = false;
                    modeTimer = 0f;
                }
            }
        }

        /// <summary>Is a point within axe reach of our hull (for hacking at it from alongside or aboard)?</summary>
        public bool WithinReach(Vector3 p, float reach)
        {
            Vector3 local = transform.InverseTransformPoint(p);
            float hw, k, g;
            LongshipBuilder.Station(Mathf.Clamp(local.z / (LongshipBuilder.Length / 2f), -1f, 1f), out hw, out k, out g);
            return Mathf.Abs(local.z) < LongshipBuilder.Length / 2f + reach && Mathf.Abs(local.x) < hw + reach && local.y < g + 2.5f;
        }

        public void TakeDamage(float amount, Vector3 at)
        {
            if (Sinking) return;
            Hull -= amount;
            if (Hull > 0f) return;
            Sinking = true;
            sinkTime = Time.time;
            Ship.SailTarget = 0f;
            Ship.Rowing = false;
            Ship.RudderInput = 0f;
            Ship.Body.isKinematic = true;
            // Plunder floats free.
            var chest = TreasureChest.Create(transform.parent, transform.position + Vector3.up, Random.Range(0f, 360f), Random.Range(140, 260));
            chest.gameObject.AddComponent<Floater>().sink = 0.35f;
            Fortune.Current.AddFavour(0.12f);
            CombatHud.Banner("RAIDER SUNK!", "Their plunder floats free. Grab the chest before it drifts off.");
        }

        void Sink()
        {
            float t = Time.time - sinkTime;
            transform.position += Vector3.down * Time.deltaTime * (0.4f + t * 0.15f);
            transform.rotation = Quaternion.Slerp(transform.rotation, transform.rotation * Quaternion.Euler(0f, 0f, 25f), Time.deltaTime * 0.3f);
            if (t > 14f) Destroy(gameObject);
        }
    }

    /// <summary>An arrow in flight: a ballistic arc to a point, then it sticks in the deck (or hits you).</summary>
    public class Arrow : MonoBehaviour
    {
        public const float Damage = 9f, HitRadius = 1.3f, FlightTime = 1.1f;

        Vector3 from, to;
        float start;
        Longship ship;
        Vector3 shipLocalTo;
        bool landed;

        public static Arrow Shoot(Transform parent, Vector3 from, Vector3 to, Longship targetShip)
        {
            var go = new GameObject("Arrow");
            go.transform.SetParent(parent, false);
            go.transform.position = from;
            LongshipBuilder.Deco(PrimitiveType.Cube, go.transform, Vector3.zero, new Vector3(0.03f, 0.03f, 0.8f), Materials.Wood);
            LongshipBuilder.Deco(PrimitiveType.Cube, go.transform, new Vector3(0f, 0f, -0.38f), new Vector3(0.1f, 0.01f, 0.12f), new Color(0.9f, 0.9f, 0.9f));
            Sfx.At(SfxId.ArrowWhoosh, from, 0.6f, 0.15f);
            var a = go.AddComponent<Arrow>();
            a.from = from;
            a.to = to;
            a.start = Time.time;
            a.ship = targetShip;
            // Aim at where that spot on the ship will be: track it in ship space.
            a.shipLocalTo = targetShip != null ? targetShip.transform.InverseTransformPoint(to) : to;
            return a;
        }

        void Update()
        {
            if (landed) return;
            float t = (Time.time - start) / FlightTime;
            Vector3 end = ship != null ? ship.transform.TransformPoint(shipLocalTo) : to;
            Vector3 p = Vector3.Lerp(from, end, Mathf.Clamp01(t)) + Vector3.up * 4f * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) * Vector3.Distance(from, end) / 40f;
            Vector3 dir = p - transform.position;
            if (dir.sqrMagnitude > 1e-5f) transform.rotation = Quaternion.LookRotation(dir);
            transform.position = p;
            if (t < 1f) return;

            landed = true;
            var player = GameBootstrap.Instance != null ? GameBootstrap.Instance.Player : null;
            if (player != null && Vector3.Distance(player.transform.position + Vector3.up, end) < HitRadius)
            {
                var combat = player.GetComponent<VikingCombat>();
                if (combat != null)
                {
                    bool front = CombatMath.FromFront(player.transform.position, player.transform.forward, from);
                    float dmg = CombatMath.Damage(Damage, 1f, combat.Blocking, front);
                    if (dmg < Damage) { CombatHud.Number(player.transform.position + Vector3.up * 2.4f, "BLOCKED", Color.white); Sfx.At(SfxId.ShieldBlock, end); }
                    combat.Health.TakeDamage(dmg, from);
                }
                Destroy(gameObject);
                return;
            }
            // Stuck quivering in the planks (or lost in the sea).
            Sfx.At(ship != null ? SfxId.ArrowThunk : SfxId.Splash, end, 0.5f, 0.15f);
            if (ship != null) transform.SetParent(ship.transform, true);
            Destroy(gameObject, 12f);
        }
    }
}
