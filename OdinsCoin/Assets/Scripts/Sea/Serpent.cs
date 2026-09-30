using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Jörmungandr, the World Serpent. Drawn by the curse of its gaze (or by sailing too far into the deep),
    /// it rises beside the ship, circles, rears up and slams down onto the deck, stoving in the strakes.
    /// While it lies stunned with its head on the gunwale, hit it with your axe. Hurt it enough and it sinks
    /// back to the deep, leaving a scale worth a fortune; otherwise it loses interest after a few strikes.
    /// </summary>
    public class Serpent : MonoBehaviour
    {
        public static Serpent Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        const int Segments = 16;
        const float CircleRadius = 20f, StrikeDamage = 30f, StrikeRadius = 3f, Reward = 300;

        public readonly SerpentBrain Brain = new SerpentBrain();
        public Transform Head { get; private set; }

        Longship ship;
        readonly List<Transform> body = new List<Transform>();
        readonly List<Vector3> trail = new List<Vector3>();
        float angle, side = 1f, strikeZ;
        Vector3 headPos;
        void OnEnable() { WorldOrigin.Shifted += OnShift; }
        void OnDisable() { WorldOrigin.Shifted -= OnShift; }
        void OnShift(Vector3 shift) { headPos -= shift; }
        bool warned;

        public static Serpent Spawn(Transform parent, Longship ship)
        {
            var s = new GameObject("Jörmungandr").AddComponent<Serpent>();
            s.transform.SetParent(parent, false);
            s.ship = ship;
            s.angle = Random.Range(0f, 360f);
            s.Build();
            Instance = s;
            s.headPos = s.CirclePoint(-8f);
            Sfx.Play(SfxId.SerpentRoar, 0.6f);
            CombatHud.Banner("THE SEA BOILS...", "Something enormous moves beneath the ship.");
            return s;
        }

        void Build()
        {
            var scales = new Color(0.16f, 0.3f, 0.22f);
            var belly = new Color(0.62f, 0.58f, 0.3f);
            for (int i = 0; i < Segments; i++)
            {
                float size = Mathf.Lerp(2.6f, 0.9f, i / (float)(Segments - 1));
                var seg = new GameObject("Coil").transform;
                seg.SetParent(transform, false);
                LongshipBuilder.Deco(PrimitiveType.Sphere, seg, Vector3.zero, new Vector3(size, size, size * 1.6f), scales);
                LongshipBuilder.Deco(PrimitiveType.Sphere, seg, new Vector3(0f, -size * 0.2f, 0f), new Vector3(size * 0.8f, size * 0.7f, size * 1.4f), belly);
                // A ridge of spines.
                LongshipBuilder.Deco(PrimitiveType.Cube, seg, new Vector3(0f, size * 0.5f, 0f), new Vector3(0.12f, size * 0.45f, size * 0.6f), new Color(0.35f, 0.12f, 0.1f)).localRotation = Quaternion.Euler(20f, 0f, 0f);
                body.Add(seg);
            }
            Head = new GameObject("Head").transform;
            Head.SetParent(transform, false);
            LongshipBuilder.Deco(PrimitiveType.Cube, Head, new Vector3(0f, 0f, 0.8f), new Vector3(2f, 1.5f, 3.4f), scales);
            LongshipBuilder.Deco(PrimitiveType.Cube, Head, new Vector3(0f, -0.9f, 1f), new Vector3(1.8f, 0.4f, 3f), belly);             // jaw
            LongshipBuilder.Deco(PrimitiveType.Cube, Head, new Vector3(0.75f, 0.5f, 1.6f), new Vector3(0.35f, 0.25f, 0.35f), new Color(1f, 0.85f, 0.1f)); // eyes
            LongshipBuilder.Deco(PrimitiveType.Cube, Head, new Vector3(-0.75f, 0.5f, 1.6f), new Vector3(0.35f, 0.25f, 0.35f), new Color(1f, 0.85f, 0.1f));
            for (int i = 0; i < 4; i++)
                LongshipBuilder.Deco(PrimitiveType.Cube, Head, new Vector3(-0.6f + i * 0.4f, -0.6f, 2.35f), new Vector3(0.1f, 0.4f, 0.1f), new Color(0.95f, 0.93f, 0.85f)); // fangs
            LongshipBuilder.Deco(PrimitiveType.Cube, Head, new Vector3(0.6f, 1f, -0.4f), new Vector3(0.2f, 1.1f, 0.2f), new Color(0.35f, 0.12f, 0.1f)).localRotation = Quaternion.Euler(-40f, 0f, 0f); // horns
            LongshipBuilder.Deco(PrimitiveType.Cube, Head, new Vector3(-0.6f, 1f, -0.4f), new Vector3(0.2f, 1.1f, 0.2f), new Color(0.35f, 0.12f, 0.1f)).localRotation = Quaternion.Euler(-40f, 0f, 0f);
        }

        Vector3 CirclePoint(float height)
        {
            Vector3 c = ship.transform.position;
            float a = angle * Mathf.Deg2Rad;
            Vector3 p = c + new Vector3(Mathf.Cos(a), 0f, Mathf.Sin(a)) * CircleRadius;
            p.y = Waves.Height(p.x, p.z) + height;
            return p;
        }

        void Update()
        {
            if (ship == null) { Destroy(gameObject); return; }
            float dt = Time.deltaTime;
            var phase = Brain.State;
            bool struck = Brain.Tick(dt);
            Vector3 goal = headPos;
            float follow = 3f;

            switch (Brain.State)
            {
                case SerpentBrain.Phase.Rising:
                    goal = CirclePoint(Mathf.Lerp(-8f, 2f, Brain.Timer / SerpentBrain.RiseTime));
                    break;
                case SerpentBrain.Phase.Circling:
                    angle += dt * 22f;
                    goal = CirclePoint(2f + Mathf.Sin(Time.time * 1.3f) * 1.5f);
                    follow = 4f;
                    break;
                case SerpentBrain.Phase.Rearing:
                    if (phase != SerpentBrain.Phase.Rearing)
                    {
                        // Pick the side it's on, and a spot along the deck to smash.
                        side = Mathf.Sign(ship.transform.InverseTransformPoint(headPos).x);
                        if (side == 0f) side = 1f;
                        Sfx.Play(SfxId.SerpentRoar, 0.9f);
                        var player = GameBootstrap.Instance != null ? GameBootstrap.Instance.Player : null;
                        strikeZ = player != null && player.OnShip ? Mathf.Clamp(ship.transform.InverseTransformPoint(player.transform.position).z, -6f, 6f) : Random.Range(-5f, 5f);
                        if (!warned) { warned = true; CombatHud.Banner("JÖRMUNGANDR REARS UP!", "Get clear of where it looks, then hit its head while it's stunned."); }
                    }
                    goal = ship.transform.TransformPoint(new Vector3(side * 9f, 13f + Mathf.Sin(Time.time * 9f) * 0.4f, strikeZ));
                    follow = 2.5f;
                    break;
                case SerpentBrain.Phase.Striking:
                case SerpentBrain.Phase.Stunned:
                    goal = ship.transform.TransformPoint(new Vector3(side * ship.Beam * 0.35f, ship.Freeboard + 0.4f, strikeZ));
                    follow = Brain.State == SerpentBrain.Phase.Striking ? 14f : 20f;
                    break;
                case SerpentBrain.Phase.Diving:
                    goal = headPos + Vector3.down * 6f * dt + ship.transform.right * side * 4f * dt;
                    follow = 10f;
                    break;
                case SerpentBrain.Phase.Gone:
                    Leave();
                    return;
            }
            headPos = Vector3.Lerp(headPos, goal, Mathf.Clamp01(dt * follow));
            if (struck) Strike();
            PlaceBody(dt);
        }

        void Strike()
        {
            ship.Hull.Holes += 1;
            ship.Hull.Flood(0.15f);
            Vector3 impact = ship.transform.TransformPoint(new Vector3(side * ship.Beam * 0.26f, ship.DeckY, strikeZ));
            ship.Body.AddForceAtPosition(Vector3.down * ship.Body.mass * 2.5f, impact, ForceMode.Impulse);
            var player = GameBootstrap.Instance != null ? GameBootstrap.Instance.Player : null;
            if (player != null && Vector3.Distance(player.transform.position, impact) < StrikeRadius)
            {
                var combat = player.GetComponent<VikingCombat>();
                if (combat != null) combat.Health.TakeDamage(combat.Blocking ? StrikeDamage * 0.5f : StrikeDamage, impact);
            }
            CombatHud.Number(impact + Vector3.up * 2f, "CRASH!", new Color(0.6f, 1f, 0.6f));
            Sfx.At(SfxId.RamCrash, impact, 1f);
        }

        /// <summary>An axe blow at the head. Returns true if it landed.</summary>
        public bool TakeHit(float damage)
        {
            if (!Brain.Hit(damage)) return false;
            CombatHud.Number(Head.position + Vector3.up * 1.5f, "-" + Mathf.RoundToInt(damage), new Color(1f, 0.9f, 0.4f));
            if (Brain.Defeated)
            {
                int gold = Mathf.RoundToInt(Reward * Fortune.Current.LootMultiplier);
                Fortune.Current.Gold += gold;
                Fortune.Current.AddFavour(0.35f);
                CombatHud.Banner("THE WORLD SERPENT SINKS BACK INTO THE DEEP", "A scale as big as a shield is left on deck: +" + gold + " gold.");
            }
            return true;
        }

        public bool HeadInReach(Vector3 from, Vector3 forward, float reach, float arc)
        {
            return Brain.State == SerpentBrain.Phase.Stunned && CombatMath.InArc(from, forward, Head.position, reach + 1.2f, arc);
        }

        void PlaceBody(float dt)
        {
            Head.position = headPos;
            Vector3 fwd = trail.Count > 0 ? headPos - trail[0] : Vector3.forward;
            fwd.y *= 0.3f;
            if (Brain.State == SerpentBrain.Phase.Stunned || Brain.State == SerpentBrain.Phase.Striking)
                fwd = ship.transform.TransformDirection(new Vector3(-side, -0.4f, 0f));
            if (fwd.sqrMagnitude > 1e-4f) Head.rotation = Quaternion.Slerp(Head.rotation, Quaternion.LookRotation(fwd), dt * 6f);

            // The body follows the head's path, arching in and out of the waves.
            if (trail.Count == 0 || (trail[0] - headPos).sqrMagnitude > 1.4f * 1.4f) trail.Insert(0, headPos);
            if (trail.Count > Segments * 2 + 2) trail.RemoveAt(trail.Count - 1);
            for (int i = 0; i < body.Count; i++)
            {
                int k = Mathf.Min(trail.Count - 1, (i + 1) * 2);
                Vector3 p = trail[k];
                float water = Waves.Height(p.x, p.z);
                float arch = Mathf.Sin(i * 0.7f - Time.time * 2f) * 1.6f;
                p.y = Mathf.Lerp(p.y, water + arch, Mathf.Clamp01(i / 4f));
                body[i].position = Vector3.Lerp(body[i].position, p, Mathf.Clamp01(dt * 8f));
                Vector3 ahead = i == 0 ? Head.position : body[i - 1].position;
                Vector3 d = ahead - body[i].position;
                if (d.sqrMagnitude > 1e-4f) body[i].rotation = Quaternion.LookRotation(d);
            }
        }

        void Leave()
        {
            if (!Brain.Defeated) CombatHud.Banner("JÖRMUNGANDR LOSES INTEREST", "It slides back into the deep. For now.");
            Instance = null;
            Destroy(gameObject);
        }

        void OnDestroy() { if (Instance == this) Instance = null; }
    }
}
