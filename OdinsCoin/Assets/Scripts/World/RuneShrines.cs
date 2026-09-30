using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>What a solved rune ring gives you: a little more life, more breath for climbing and swimming, or luck at the altar.</summary>
    public enum RuneGift { Vitality, Endurance, Luck }

    /// <summary>
    /// A rune ring's puzzle, as plain logic: a ring of small standing stones, each cut with 1 to N notches. Touch
    /// them in order, one notch to the most, and the ring wakes and gives its gift; touch one out of turn and they
    /// all fall dark again. Simple enough to work out by looking, like Zelda's small shrines.
    /// </summary>
    public class RunePuzzle
    {
        public enum Result { Lit, Wrong, Solved, AlreadySolved }

        /// <summary>How many notches each stone round the ring has (the order to touch them is 1, 2, 3…).</summary>
        public readonly int[] Notches;
        public int Next { get; private set; }
        public bool Solved { get; private set; }

        public RunePuzzle(int stones, int seed)
        {
            // The notch counts shuffled round the ring, so the order isn't simply round and round.
            Notches = new int[stones];
            for (int i = 0; i < stones; i++) Notches[i] = i + 1;
            var rng = new System.Random(seed);
            for (int i = stones - 1; i > 0; i--) { int j = rng.Next(i + 1); int t = Notches[i]; Notches[i] = Notches[j]; Notches[j] = t; }
        }

        /// <summary>Is this stone lit (touched in turn so far)?</summary>
        public bool IsLit(int stone) { return Solved || Notches[stone] <= Next; }

        /// <summary>Is this the stone to touch next (what the Seer's Rune Lore shows)?</summary>
        public bool IsNext(int stone) { return !Solved && Notches[stone] == Next + 1; }

        /// <summary>Touch a stone.</summary>
        public Result Touch(int stone)
        {
            if (Solved) return Result.AlreadySolved;
            if (Notches[stone] != Next + 1) { Next = 0; return Result.Wrong; }
            Next++;
            if (Next == Notches.Length) { Solved = true; return Result.Solved; }
            return Result.Lit;
        }

        /// <summary>Mark it solved without playing it (a ring solved on an earlier voyage).</summary>
        public void SetSolved() { Solved = true; Next = Notches.Length; }
    }

    /// <summary>The rune rings in the world: which gift each place's gives, what the gifts do, and the rings standing now.</summary>
    public static class RuneShrines
    {
        public const int Stones = 5;
        /// <summary>How far round its centre the ring's stones stand (m), and how close you must be to touch one (m).</summary>
        public const float RingRadius = 7f, TouchRange = 1.8f;
        /// <summary>What each gift gives: health, seconds of stamina, and the lift in the altar's odds (capped).</summary>
        public const float VitalityHealth = 15f, EnduranceSeconds = 3f, LuckOdds = 0.02f, MaxOdds = 0.6f;

        public static readonly List<ShrineRing> Standing = new List<ShrineRing>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Standing.Clear(); }

        public static RuneGift GiftOf(Place place) { return (RuneGift)(((place.name.GetHashCode() % 3) + 3) % 3); }

        public static string GiftName(RuneGift gift)
        {
            switch (gift)
            {
                case RuneGift.Vitality: return "Rune of Vitality (+" + VitalityHealth + " health)";
                case RuneGift.Endurance: return "Rune of Endurance (more breath for climbing and swimming)";
                default: return "Rune of Luck (better odds at Odin's altar)";
            }
        }

        /// <summary>Where a ring's stones stand round its centre (local, flat).</summary>
        public static Vector3 StoneAt(int i)
        {
            float a = i / (float)Stones * Mathf.PI * 2f;
            return new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a)) * RingRadius;
        }

        /// <summary>The altar's odds with this many Runes of Luck.</summary>
        public static float StakeOdds(int luck) { return Mathf.Min(MaxOdds, Stake.Odds + LuckOdds * Mathf.Max(0, luck)); }

        /// <summary>The stone within reach, if any.</summary>
        public static bool Near(Vector3 scenePos, out ShrineRing ring, out int stone)
        {
            Standing.RemoveAll(r => r == null);
            foreach (var r in Standing)
                for (int i = 0; i < Stones; i++)
                {
                    var d = r.transform.TransformPoint(StoneAt(i)) - scenePos;
                    d.y = 0f;
                    if (d.magnitude < TouchRange + 0.6f) { ring = r; stone = i; return true; }
                }
            ring = null; stone = -1;
            return false;
        }
    }

    /// <summary>A rune ring standing in the world: five small stones round a place's landmark.</summary>
    public class ShrineRing : MonoBehaviour
    {
        public Place Place { get; private set; }
        public RunePuzzle Puzzle { get; private set; }
        readonly Renderer[] glows = new Renderer[RuneShrines.Stones];
        static readonly Color Stone = new Color(0.55f, 0.53f, 0.5f), Dark = new Color(0.22f, 0.2f, 0.2f), Lit = new Color(0.45f, 0.8f, 1f), Hint = new Color(0.62f, 0.52f, 0.3f);

        public static ShrineRing Build(Transform parent, Place place, Vector3 scenePos, System.Func<Vector3, float> groundAt)
        {
            var t = new GameObject("Rune Ring").transform;
            t.SetParent(parent, false);
            t.position = scenePos;
            var ring = t.gameObject.AddComponent<ShrineRing>();
            ring.Place = place;
            ring.Puzzle = new RunePuzzle(RuneShrines.Stones, place.name.GetHashCode());
            if (Upgrades.Current.Shrines.Contains(place.name)) ring.Puzzle.SetSolved();
            for (int i = 0; i < RuneShrines.Stones; i++)
            {
                var s = new GameObject("Stone " + (i + 1)).transform;
                s.SetParent(t, false);
                var local = RuneShrines.StoneAt(i);
                s.position = new Vector3(scenePos.x + local.x, groundAt(scenePos + local) - 0.2f, scenePos.z + local.z);
                s.rotation = Quaternion.LookRotation(-local.normalized);
                LongshipBuilder.Deco(PrimitiveType.Cube, s, new Vector3(0f, 0.7f, 0f), new Vector3(0.6f, 1.4f, 0.35f), Stone);
                // Its notches, cut on the face turned to the middle, which light up blue when it's touched in turn.
                int notches = ring.Puzzle.Notches[i];
                var glow = new GameObject("Notches").transform;
                glow.SetParent(s, false);
                for (int n = 0; n < notches; n++)
                {
                    var mark = LongshipBuilder.Deco(PrimitiveType.Cube, glow, new Vector3((n - (notches - 1) / 2f) * 0.1f, 1f, 0.18f), new Vector3(0.05f, 0.3f, 0.02f), Dark);
                    if (n == 0) ring.glows[i] = mark.GetComponent<Renderer>();
                }
                var col = s.gameObject.AddComponent<BoxCollider>();
                col.center = new Vector3(0f, 0.7f, 0f);
                col.size = new Vector3(0.6f, 1.4f, 0.35f);
            }
            RuneShrines.Standing.Add(ring);
            ring.Show();
            return ring;
        }

        /// <summary>Light the stones touched in turn (all of them once solved), and, for a Seer, warm the next one.</summary>
        public void Show()
        {
            bool lore = Abilities.Has("runelore");
            for (int i = 0; i < RuneShrines.Stones; i++)
            {
                var notches = glows[i] != null ? glows[i].transform.parent : null;
                if (notches == null) continue;
                foreach (var r in notches.GetComponentsInChildren<Renderer>())
                    r.sharedMaterial = Materials.Get(Puzzle.IsLit(i) ? Lit : lore && Puzzle.IsNext(i) ? Hint : Dark);
            }
        }
    }
}
