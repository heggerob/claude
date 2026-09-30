using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// The stone altar on the longship's deck with Odin's coin on it. Handles the flip animation:
    /// the coin spins up into the air and lands Odin's-eye-up (blessing) or serpent-up (curse).
    /// Also applies fates that act on the world (fair wind, a leaking hull).
    /// </summary>
    public class CoinAltar : MonoBehaviour
    {
        public static CoinAltar Instance { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Instance = null; }

        public const float FlipTime = 1.6f;
        public const float UseRange = 2f;
        /// <summary>Seconds before the coin can be flipped again: Odin doesn't like to be pestered.</summary>
        public const float Cooldown = 10f;

        public Longship Ship;
        public bool Flipping { get { return flipStart >= 0f; } }
        /// <summary>Seconds until the coin can be flipped again (0 = ready).</summary>
        public float ReadyIn { get { return Mathf.Max(0f, readyAt - Time.time); } }
        public FlipResult LastResult { get; private set; }
        /// <summary>Raised when the coin has landed and the result is shown.</summary>
        public event System.Action<FlipResult> Landed;

        Transform coin;
        Vector3 rest;
        float flipStart = -1f;
        float readyAt;
        readonly List<Transform> runeMarks = new List<Transform>();
        int shownRunes = -1;
        FlipResult pending;
        Light glow;
        // A stake of treasure in the air: how it will land, and what to do when it has.
        bool stakeHeads;
        System.Action<bool> stakeDone;
        /// <summary>Seconds before the altar takes another stake.</summary>
        public const float StakeCooldown = 1f;

        public static CoinAltar Create(Longship ship)
        {
            var altar = ship.Parts.altar;
            var a = altar.gameObject.AddComponent<CoinAltar>();
            a.Ship = ship;
            a.BuildCoin();
            Instance = a;
            return a;
        }

        void BuildCoin()
        {
            coin = new GameObject("Odin's Coin").transform;
            coin.SetParent(transform, false);
            rest = new Vector3(0f, 1.02f, 0f);
            coin.localPosition = rest;
            LongshipBuilder.Deco(PrimitiveType.Cylinder, coin, Vector3.zero, new Vector3(0.36f, 0.025f, 0.36f), Materials.Gold);
            // Heads: Odin's single eye. Tails: the coiled serpent.
            LongshipBuilder.Deco(PrimitiveType.Cylinder, coin, new Vector3(0f, 0.026f, 0f), new Vector3(0.14f, 0.004f, 0.08f), new Color(0.95f, 0.95f, 0.9f));
            LongshipBuilder.Deco(PrimitiveType.Cylinder, coin, new Vector3(0f, 0.03f, 0f), new Vector3(0.06f, 0.004f, 0.06f), new Color(0.15f, 0.25f, 0.5f));
            for (int i = 0; i < 6; i++)
            {
                float a = i / 6f * Mathf.PI * 2f;
                LongshipBuilder.Deco(PrimitiveType.Cube, coin, new Vector3(Mathf.Cos(a) * 0.1f, -0.026f, Mathf.Sin(a) * 0.1f), new Vector3(0.05f, 0.004f, 0.04f), new Color(0.2f, 0.45f, 0.2f));
            }

            // Rune slots around the rim: small dark notches, lit gold once a rune is carved.
            for (int i = 0; i < Runes.Slots; i++)
            {
                float a = (i / (float)Runes.Slots) * Mathf.PI * 2f + 0.5f;
                var mark = LongshipBuilder.Deco(PrimitiveType.Cube, coin, new Vector3(Mathf.Cos(a) * 0.15f, 0.028f, Mathf.Sin(a) * 0.15f), new Vector3(0.03f, 0.004f, 0.05f), new Color(0.35f, 0.25f, 0.08f));
                mark.localRotation = Quaternion.Euler(0f, -a * Mathf.Rad2Deg, 0f);
                runeMarks.Add(mark);
            }

            glow = new GameObject("Coin Glow").AddComponent<Light>();
            glow.transform.SetParent(transform, false);
            glow.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            glow.type = LightType.Point;
            glow.range = 4f;
            glow.intensity = 0f;
        }

        // The next throw's fall, decided before it's made (so the Seer's Foresight can see it).
        float nextRoll = -1f, nextPick;

        void RollAhead() { if (nextRoll < 0f) { nextRoll = Random.value; nextPick = Random.value; } }

        /// <summary>How the next throw will land, as the Seer foresees it: true for Odin's eye (heads).</summary>
        public bool ForeseeHeads()
        {
            RollAhead();
            return nextRoll < (Fortune.Current.NextFlipBlessed ? 1f : Fortune.Current.HeadsChance);
        }

        /// <summary>Start a flip. The outcome is decided up front; the animation just shows it.</summary>
        public FlipResult Flip(int wager)
        {
            if (Flipping || ReadyIn > 0f) return null;
            readyAt = Time.time + FlipTime + Cooldown;
            RollAhead();
            pending = Fortune.Current.Flip(wager, nextRoll, nextPick);
            nextRoll = -1f;
            flipStart = Time.time;
            Sfx.At(SfxId.CoinFlip, transform.position + Vector3.up);
            return pending;
        }

        /// <summary>
        /// Throw the coin for a stake of treasure: Odin's eye with probability <paramref name="chance"/>. The
        /// outcome is decided now; <paramref name="done"/> hears it when the coin lands.
        /// </summary>
        public bool FlipForStake(float chance, System.Action<bool> done)
        {
            if (Flipping || Time.time < stakeReadyAt) return false;
            stakeHeads = NextStakeRoll() < chance;
            stakeRoll = -1f;
            stakeDone = done;
            pending = null;
            flipStart = Time.time;
            stakeReadyAt = Time.time + FlipTime + StakeCooldown;
            Sfx.At(SfxId.CoinFlip, transform.position + Vector3.up);
            return true;
        }

        float stakeReadyAt;
        /// <summary>The next stake's throw, rolled ahead so the Seer can foresee it (-1 until it's rolled).</summary>
        float stakeRoll = -1f;

        float NextStakeRoll() { if (stakeRoll < 0f) stakeRoll = Random.value; return stakeRoll; }

        /// <summary>How the next stake will land at these odds, as the Seer foresees it: true for Odin's eye.</summary>
        public bool ForeseeStake(float chance) { return NextStakeRoll() < chance; }

        /// <summary>Whether the altar will take a stake now.</summary>
        public bool ReadyForStake { get { return !Flipping && Time.time >= stakeReadyAt; } }

        void Update()
        {
            AnimateFlip();
            ApplyWorldFates();
            ShowRunes();
            // Blessings glow gold, curses glow sickly green, while any are active.
            var fortune = Fortune.Current;
            bool blessed = false, cursed = false;
            foreach (var f in fortune.Active) { if (f.card.kind == FateKind.Blessing) blessed = true; else cursed = true; }
            glow.color = cursed && !blessed ? new Color(0.4f, 1f, 0.5f) : new Color(1f, 0.8f, 0.35f);
            glow.intensity = Mathf.Lerp(glow.intensity, blessed || cursed ? 1.2f + Mathf.Sin(Time.time * 3f) * 0.3f : 0f, Time.deltaTime * 3f);
        }

        void AnimateFlip()
        {
            if (!Flipping) return;
            float t = (Time.time - flipStart) / FlipTime;
            bool heads = pending != null ? pending.heads : stakeHeads;
            if (t >= 1f)
            {
                coin.localPosition = rest;
                coin.localRotation = heads ? Quaternion.identity : Quaternion.Euler(180f, 0f, 0f);
                flipStart = -1f;
                Sfx.At(SfxId.CoinLand, transform.position + Vector3.up);
                Sfx.Play(heads ? SfxId.Blessing : SfxId.Curse, 0.7f);
                if (pending == null)
                {
                    var done = stakeDone;
                    stakeDone = null;
                    if (done != null) done(heads);
                    return;
                }
                LastResult = pending;
                if (Landed != null) Landed(pending);
                return;
            }
            // Up and down in a parabola, spinning end over end, settling on the right face.
            float height = 4f * 1.6f * t * (1f - t);
            int turns = 7;
            float spin = t * (turns * 360f + (heads ? 0f : 180f));
            coin.localPosition = rest + Vector3.up * height;
            coin.localRotation = Quaternion.Euler(spin, t * 90f, 0f);
        }

        void ShowRunes()
        {
            int carved = Fortune.Current.Carved.Count;
            if (carved == shownRunes) return;
            shownRunes = carved;
            for (int i = 0; i < runeMarks.Count; i++)
                runeMarks[i].GetComponent<Renderer>().sharedMaterial = Materials.Get(i < carved ? new Color(1f, 0.85f, 0.4f) : new Color(0.35f, 0.25f, 0.08f));
        }

        void ApplyWorldFates()
        {
            var fortune = Fortune.Current;
            // Njord's Breeze: keep the wind right behind the ship.
            if (fortune.Has(FateEffect.FairWind, FateKind.Blessing) && Ship != null)
                Wind.Set(Ship.Heading, 0.75f + 0.12f * fortune.Strength(FateEffect.FairWind, FateKind.Blessing), 3f);
        }
    }
}
