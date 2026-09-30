using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    public enum FateKind { Blessing, Curse }

    /// <summary>What a blessing or curse does. Systems ask <see cref="Fortune"/> for these.</summary>
    public enum FateEffect
    {
        MeleeDamage,     // Thor's Wrath / Loki's Trick (misses)
        FairWind,        // Njord's Breeze: wind swings behind the ship
        Loot,            // Freya's Gift / Fenrir's Hunger
        SeeThroughFog,   // Heimdall's Eye
        Luck,            // Odin's Favour: better odds on the next flips
        Leak,            // Rán's Net: the ship takes on water and slows down
        Frailty,         // Hel's Chill: less health
        SerpentWakes,    // Jörmungandr's Gaze: sea monsters are drawn to you
    }

    public class FateCard
    {
        public string id;
        public string name;
        public string god;
        public string description;
        public FateKind kind;
        public FateEffect effect;
        /// <summary>Seconds at tier 1. Higher wagers make it last longer (and hit harder).</summary>
        public float baseDuration;
    }

    public class ActiveFate
    {
        public FateCard card;
        /// <summary>1..3, from the size of the wager.</summary>
        public int tier;
        public float remaining;
        public float Strength { get { return tier == 3 ? 2f : tier == 2 ? 1.5f : 1f; } }
    }

    public class FlipResult
    {
        public bool heads;          // Odin's eye = blessing
        public int wager;
        public int payout;          // gold back (0 on tails)
        public ActiveFate fate;
        public float headsChance;
    }

    /// <summary>All blessings and curses Odin's coin can give.</summary>
    public static class Fates
    {
        public static readonly FateCard[] Blessings =
        {
            new FateCard { id = "thor", name = "Thor's Wrath", god = "Thor", kind = FateKind.Blessing, effect = FateEffect.MeleeDamage, baseDuration = 120f,
                description = "Your axe hits like thunder: more melee damage." },
            new FateCard { id = "njord", name = "Njord's Breeze", god = "Njord", kind = FateKind.Blessing, effect = FateEffect.FairWind, baseDuration = 90f,
                description = "The wind swings round behind your sail." },
            new FateCard { id = "freya", name = "Freya's Gift", god = "Freya", kind = FateKind.Blessing, effect = FateEffect.Loot, baseDuration = 180f,
                description = "Chests hold more gold." },
            new FateCard { id = "heimdall", name = "Heimdall's Eye", god = "Heimdall", kind = FateKind.Blessing, effect = FateEffect.SeeThroughFog, baseDuration = 120f,
                description = "Enemies and treasure show through fog and walls." },
            new FateCard { id = "odin", name = "Odin's Favour", god = "Odin", kind = FateKind.Blessing, effect = FateEffect.Luck, baseDuration = 240f,
                description = "The All-Father smiles: better odds on your next flips." },
        };

        public static readonly FateCard[] Curses =
        {
            new FateCard { id = "loki", name = "Loki's Trick", god = "Loki", kind = FateKind.Curse, effect = FateEffect.MeleeDamage, baseDuration = 90f,
                description = "Your weapon slips: some swings miss completely." },
            new FateCard { id = "ran", name = "Rán's Net", god = "Rán", kind = FateKind.Curse, effect = FateEffect.Leak, baseDuration = 90f,
                description = "The sea goddess grabs the hull: the ship leaks and slows down." },
            new FateCard { id = "fenrir", name = "Fenrir's Hunger", god = "Fenrir", kind = FateKind.Curse, effect = FateEffect.Loot, baseDuration = 150f,
                description = "The wolf eats into your plunder: chests hold less gold." },
            new FateCard { id = "hel", name = "Hel's Chill", god = "Hel", kind = FateKind.Curse, effect = FateEffect.Frailty, baseDuration = 120f,
                description = "Cold from the underworld: you have less health." },
            new FateCard { id = "jormungandr", name = "Jörmungandr's Gaze", god = "Jörmungandr", kind = FateKind.Curse, effect = FateEffect.SerpentWakes, baseDuration = 150f,
                description = "The World Serpent has noticed you..." },
        };
    }

    /// <summary>
    /// The player's fortune: gold, Odin's favour, runes carved into the coin and every blessing or curse
    /// currently active. Pure logic so it can be tested without Unity scenes.
    /// </summary>
    public class Fortune
    {
        public const float BaseHeadsChance = 0.5f;
        public const float MaxHeadsChance = 0.75f;
        public static readonly int[] Wagers = { 0, 25, 50, 100, 200 };

        public int Gold = 100;
        /// <summary>0..1, filled by winning flips, raids and plunder. When full, Odin's ravens answer your call.</summary>
        public float Favour;
        /// <summary>Extra heads chance from other sources than runes (tests, future upgrades).</summary>
        public float RuneBonus;
        /// <summary>How much curses are shortened by other sources than runes, 0..0.5.</summary>
        public float CurseWard;
        /// <summary>Runes carved into the coin, at most <see cref="Runes.Slots"/>.</summary>
        public readonly List<RuneCard> Carved = new List<RuneCard>();
        /// <summary>Muninn remembers a flip that went right: the next one is Odin's eye for sure.</summary>
        public bool NextFlipBlessed;
        public readonly List<ActiveFate> Active = new List<ActiveFate>();
        public int Flips, HeadsCount;
        /// <summary>Chests sold at home and the gold they brought in, for the mead hall's boasting board.</summary>
        public int ChestsSold, GoldPlundered;
        public int DiceWon, DiceLost;

        static Fortune current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { current = null; }

        public static Fortune Current { get { return current ?? (current = new Fortune()); } }

        /// <summary>Start over, or swap in a loaded fortune.</summary>
        public static void SetCurrent(Fortune f) { current = f; }

        public static int Tier(int wager) { return wager >= 200 ? 3 : wager >= 50 ? 2 : 1; }

        public float HeadsChance
        {
            get
            {
                float luck = Has(FateEffect.Luck, FateKind.Blessing) ? 0.1f * Strength(FateEffect.Luck, FateKind.Blessing) : 0f;
                float runes = (HasRune(RuneEffect.Odds) ? 0.06f : 0f) - (HasRune(RuneEffect.Hail) ? 0.1f : 0f);
                return Mathf.Clamp(BaseHeadsChance + RuneBonus + runes + luck, 0.05f, MaxHeadsChance);
            }
        }

        public bool CanAfford(int wager) { return wager >= 0 && wager <= Gold; }

        /// <summary>
        /// Flip the coin. Heads (Odin's eye): wager paid back double and a blessing.
        /// Tails (the serpent): wager lost and a curse. <paramref name="roll"/> is 0..1.
        /// </summary>
        public FlipResult Flip(int wager, float roll, float pick)
        {
            wager = Mathf.Clamp(wager, 0, Gold);
            float chance = NextFlipBlessed ? 1f : HeadsChance;
            bool heads = roll < chance;
            NextFlipBlessed = false;
            int tier = Tier(wager);
            var pool = heads ? Fates.Blessings : Fates.Curses;
            var card = pool[Mathf.Clamp(Mathf.FloorToInt(pick * pool.Length), 0, pool.Length - 1)];

            Gold -= wager;
            int payout = heads ? Mathf.RoundToInt(wager * PayoutMultiplier) : 0;
            Gold += payout;

            float duration = card.baseDuration * (tier == 3 ? 2f : tier == 2 ? 1.5f : 1f);
            if (heads) duration *= HasRune(RuneEffect.LongBlessings) ? 1.3f : 1f;
            else duration *= 1f - TotalCurseWard;
            var fate = Add(card, tier, duration);

            Flips++;
            if (heads) { HeadsCount++; AddFavour(0.15f * tier); }
            else Favour = Mathf.Max(0f, Favour - 0.1f);

            return new FlipResult { heads = heads, wager = wager, payout = payout, fate = fate, headsChance = chance };
        }

        /// <summary>Adds a blessing or curse; the same one again refreshes it and keeps the higher tier.</summary>
        public ActiveFate Add(FateCard card, int tier, float duration)
        {
            foreach (var a in Active)
            {
                if (a.card != card) continue;
                a.tier = Mathf.Max(a.tier, tier);
                a.remaining = Mathf.Max(a.remaining, duration);
                return a;
            }
            var fate = new ActiveFate { card = card, tier = tier, remaining = duration };
            Active.Add(fate);
            return fate;
        }

        // ---------------------------------------------------------------- runes

        public bool HasRune(RuneEffect effect)
        {
            foreach (var r in Carved) if (r.effect == effect) return true;
            return false;
        }

        public bool CanCarve(RuneCard rune)
        {
            return rune != null && !Carved.Contains(rune) && Carved.Count < Runes.Slots && Gold >= rune.cost;
        }

        /// <summary>Pay the rune-carver and cut the rune into the rim.</summary>
        public bool Carve(RuneCard rune)
        {
            if (!CanCarve(rune)) return false;
            Gold -= rune.cost;
            Carved.Add(rune);
            return true;
        }

        /// <summary>Grind a rune off to free its slot. The gold is gone.</summary>
        public bool GrindOff(RuneCard rune) { return Carved.Remove(rune); }

        /// <summary>Gold paid back per gold wagered when Odin's eye comes up.</summary>
        public float PayoutMultiplier
        {
            get
            {
                float m = HasRune(RuneEffect.Hail) ? 2.75f : 2f;
                if (HasRune(RuneEffect.Wealth)) m += 0.25f;
                return m;
            }
        }

        public float TotalCurseWard { get { return Mathf.Clamp(CurseWard + (HasRune(RuneEffect.Ward) ? 0.3f : 0f), 0f, 0.5f); } }

        /// <summary>Gold back per gold wagered, on average (1 = a fair coin).</summary>
        public float ExpectedReturn { get { return HeadsChance * PayoutMultiplier; } }

        // ---------------------------------------------------------------- Odin's favour and the ravens

        public const float RavenCost = 1f;
        public bool CanCallRavens { get { return Favour >= RavenCost - 0.001f; } }

        public void AddFavour(float amount)
        {
            if (amount > 0f && HasRune(RuneEffect.Favour)) amount *= 1.5f;
            Favour = Mathf.Clamp01(Favour + amount);
        }

        /// <summary>Spend a full favour meter. Returns false if it isn't full.</summary>
        public bool SpendFavour()
        {
            if (!CanCallRavens) return false;
            Favour = 0f;
            return true;
        }

        public void Tick(float dt) { Tick(dt, 1f); }

        /// <summary>Let time pass; curses wear off <paramref name="curseRate"/> times as fast (the Seer's Ward of Runes).</summary>
        public void Tick(float dt, float curseRate)
        {
            for (int i = Active.Count - 1; i >= 0; i--)
            {
                Active[i].remaining -= Active[i].card.kind == FateKind.Curse ? dt * curseRate : dt;
                if (Active[i].remaining <= 0f) Active.RemoveAt(i);
            }
        }

        public bool Has(FateEffect effect, FateKind kind) { return Strength(effect, kind) > 0f; }

        /// <summary>Strength of the strongest active fate of this effect and kind (0 if none).</summary>
        public float Strength(FateEffect effect, FateKind kind)
        {
            float best = 0f;
            foreach (var a in Active)
                if (a.card.effect == effect && a.card.kind == kind) best = Mathf.Max(best, a.Strength);
            return best;
        }

        // ---------------------------------------------------------------- what the rest of the game asks

        /// <summary>Melee damage multiplier (Thor's Wrath).</summary>
        public float MeleeDamageMultiplier { get { return 1f + 0.35f * Strength(FateEffect.MeleeDamage, FateKind.Blessing); } }

        /// <summary>Chance a swing just misses (Loki's Trick).</summary>
        public float MissChance { get { return 0.15f * Strength(FateEffect.MeleeDamage, FateKind.Curse); } }

        /// <summary>Gold multiplier on loot (Freya's Gift vs Fenrir's Hunger).</summary>
        public float LootMultiplier
        {
            get { return (1f + 0.35f * Strength(FateEffect.Loot, FateKind.Blessing)) * (1f - 0.25f * Strength(FateEffect.Loot, FateKind.Curse)); }
        }

        /// <summary>Multiplier on the ship's speed from the sail and oars (Rán's Net slows you).</summary>
        public float ShipThrustMultiplier { get { return 1f - 0.25f * Strength(FateEffect.Leak, FateKind.Curse); } }

        /// <summary>Multiplier on max health (Hel's Chill).</summary>
        public float HealthMultiplier { get { return 1f - 0.2f * Strength(FateEffect.Frailty, FateKind.Curse); } }
    }
}
