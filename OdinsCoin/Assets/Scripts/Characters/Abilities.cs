using UnityEngine;

namespace OdinsCoin
{
    /// <summary>
    /// What each outfit's two abilities actually do (the hero screen lists them). Each is a plain number the game
    /// reads at the one place it matters, so they're easy to test and tune.
    /// </summary>
    public static class Abilities
    {
        /// <summary>Raider, Plunderer: walking speed with a chest in your arms (m/s; normally <see cref="Viking.CarrySpeed"/>).</summary>
        public const float PlunderCarrySpeed = 4.2f;
        /// <summary>Raider, Cleave: the swing's arc (degrees; normally <see cref="VikingCombat.Arc"/>).</summary>
        public const float CleaveArc = 170f;
        /// <summary>Jarl, Rally the Crew: how much harder the crew rows.</summary>
        public const float RallyOars = 1.2f;
        /// <summary>Jarl, Tribute: what the chests fetch at home, times their worth.</summary>
        public const float TributePrice = 1.25f;
        /// <summary>Navigator, Currents: how much more drive the sails give with you at the helm.</summary>
        public const float CurrentsSail = 1.12f;
        /// <summary>Spear Guard, Shield Wall: the share of a blow or an arrow from the front that still gets through the braced shield.</summary>
        public const float WallLeak = 0.05f;
        /// <summary>Spear Guard, Long Reach: how much further your blows reach.</summary>
        public const float LongReach = 1.35f;
        /// <summary>Seer, Ward of Runes: how much faster curses wear off.</summary>
        public const float WardCurseRate = 1.6f;
        /// <summary>Scout, Keen Eyes: how much further you see through the haze.</summary>
        public const float KeenSight = 1.5f;
        /// <summary>Scout, Volley: three arrows at once, so each loosed shot hits this much harder.</summary>
        public const float VolleyDamage = 2.2f;

        /// <summary>The outfit you're playing (the player's hero, or the saved choice before the game starts).</summary>
        public static OutfitId Outfit
        {
            get
            {
                var boot = GameBootstrap.Instance;
                if (boot != null && boot.Player != null && boot.Player.Hero != null) return boot.Player.Hero.outfit;
                return OutfitId.Raider;
            }
        }

        /// <summary>Does this outfit have the ability with this id?</summary>
        public static bool Has(OutfitId outfit, string id)
        {
            foreach (var a in Outfits.Get(outfit).abilities) if (a.id == id) return true;
            return false;
        }

        /// <summary>Does the hero you're playing have this ability?</summary>
        public static bool Has(string id) { return Has(Outfit, id); }

        /// <summary>The damage a blow or arrow does to you, after your shield (and the Shield Wall).</summary>
        public static float Taken(float baseDamage, bool blocking, bool front)
        {
            if (blocking && front && Has("wall")) return baseDamage * WallLeak;
            return CombatMath.Damage(baseDamage, 1f, blocking, front);
        }
    }
}
