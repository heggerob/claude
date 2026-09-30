using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// Your crew: a few hands standing along the deck in homespun, watching the sea. They take a stake at Odin's
    /// altar to heart: they cheer with their arms up when Odin smiles, and hang their heads when he takes it.
    /// </summary>
    public class Crew : MonoBehaviour
    {
        /// <summary>The crew you start with; each level of the Oars upgrade brings two more rowers.</summary>
        public const int Count = 4, PerOarLevel = 2;

        public static int CountFor(int oarLevel) { return Count + PerOarLevel * Mathf.Max(0, oarLevel); }
        /// <summary>How long a cheer or a groan lasts (s).</summary>
        public const float ReactTime = 2.5f;

        public static Crew Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        /// <summary>
        /// Where the hands stand (ship space) and which way they face (yaw): alternately to port and starboard, spread
        /// along the waist of the ship, a step in from the side, facing inboard.
        /// </summary>
        public static List<KeyValuePair<Vector3, float>> Stations(ShipDesign d) { return Stations(d, Count); }

        /// <summary>...for a crew of <paramref name="count"/>.</summary>
        public static List<KeyValuePair<Vector3, float>> Stations(ShipDesign d, int count)
        {
            var list = new List<KeyValuePair<Vector3, float>>();
            float deck = DesignedShipBuilder.DeckY(d);
            for (int i = 0; i < count; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                // Pairs, one to port and one to starboard, spread along the waist.
                int row = i / 2, rows = (count + 1) / 2;
                float z = Mathf.Lerp(-0.2f, 0.22f, rows > 1 ? row / (float)(rows - 1) : 0.5f) * d.length + (side > 0f ? 0.35f : 0f);
                list.Add(new KeyValuePair<Vector3, float>(new Vector3(side * d.beam * 0.28f, deck, z), side > 0f ? -90f : 90f));
            }
            return list;
        }

        class Hand
        {
            public Transform root;
            public VikingBuilder.Parts parts;
            public readonly HeroAnimator animator = new HeroAnimator();
            public readonly Locomotion loco = new Locomotion();
        }

        readonly List<Hand> hands = new List<Hand>();
        Longship ship;
        float reactUntil = -1f;
        bool cheering;

        /// <summary>Put the crew aboard a ship (replacing any crew she had).</summary>
        public static Crew Create(Longship ship)
        {
            if (Instance != null) Destroy(Instance.gameObject);
            var go = new GameObject("Crew");
            go.transform.SetParent(ship.transform, false);
            var crew = go.AddComponent<Crew>();
            crew.ship = ship;
            int n = 0;
            foreach (var station in Stations(ship.Design, CountFor(Upgrades.Current.Level(UpgradeKind.Oars))))
            {
                var h = new Hand();
                h.root = new GameObject("Hand").transform;
                h.root.SetParent(go.transform, false);
                h.root.localPosition = station.Key;
                h.root.localRotation = Quaternion.Euler(0f, station.Value, 0f);
                h.loco.Reset(Vector2.zero, 0f);
                h.parts = HeroBuilder.Build(h.root, NpcHeroes.Townsfolk(ship.Design.id.GetHashCode() + n * 13));
                crew.hands.Add(h);
                n++;
            }
            Instance = crew;
            return crew;
        }

        /// <summary>The crew sees how a stake went.</summary>
        public void React(bool won)
        {
            cheering = won;
            reactUntil = Time.time + ReactTime;
            foreach (var h in hands) Face.On(h.root, won ? Expression.Happy : Expression.Hurt, ReactTime);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            bool reacting = Time.time < reactUntil;
            for (int i = 0; i < hands.Count; i++)
            {
                var h = hands[i];
                if (h.parts == null) continue;
                h.loco.Step(Vector2.zero, 0f, dt);
                h.animator.Step(dt, h.loco, true, 0f, false, Time.time + i * 1.7f);
                h.animator.Apply(h.parts);
                if (!reacting && ship != null && ship.Rowing)
                {
                    // Pulling at the oars in time with the stroke: reach forward, lean back and haul.
                    float s = Mathf.Sin(ship.StrokePhase * Mathf.PI * 2f);
                    if (h.parts.leftArm != null) h.parts.leftArm.localRotation = Quaternion.Euler(-70f - s * 30f, 0f, -12f);
                    if (h.parts.rightArm != null) h.parts.rightArm.localRotation = Quaternion.Euler(-70f - s * 30f, 0f, 12f);
                    if (h.parts.body != null) h.parts.body.localRotation = Quaternion.Euler(8f + s * 14f, 0f, 0f);
                    continue;
                }
                if (!reacting)
                {
                    // Idle: looking out to sea, now one way, now the other.
                    if (h.parts.head != null) h.parts.head.localRotation *= Quaternion.Euler(0f, Mathf.Sin(Time.time * 0.23f + i * 2.1f) * 55f, 0f);
                    continue;
                }
                if (cheering)
                {
                    // Both arms flung up, pumping.
                    float pump = Mathf.Sin(Time.time * 11f + i) * 15f;
                    if (h.parts.leftArm != null) h.parts.leftArm.localRotation = Quaternion.Euler(-165f + pump, 0f, -18f);
                    if (h.parts.rightArm != null) h.parts.rightArm.localRotation = Quaternion.Euler(-165f - pump, 0f, 18f);
                }
                else if (h.parts.head != null)
                {
                    // Heads down, shoulders slumped.
                    h.parts.head.localRotation = Quaternion.Euler(28f, 0f, 0f);
                    if (h.parts.body != null) h.parts.body.localRotation = Quaternion.Euler(10f, 0f, 0f);
                }
            }
        }
    }
}
