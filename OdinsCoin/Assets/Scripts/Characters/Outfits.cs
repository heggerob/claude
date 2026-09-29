using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    public enum OutfitId { Raider, Jarl, Navigator, SpearGuard, Seer, Scout }
    public enum HairStyle { None, LongBraids, WrappedBraids, SideBraid, ShortLocks }
    public enum WeaponId { None, TwoHandAxe, Sword, Spear }
    public enum OffHandId { None, RoundShield, Map, KnotShield }

    /// <summary>A special move or perk that comes with an outfit (used by the game later).</summary>
    public class Ability
    {
        public string id, name, description;
    }

    /// <summary>
    /// An outfit: the clothes and abilities that make a character a Raider, a Jarl, a Seer... It goes on over
    /// any body, comes with a default palette (every colour can be changed) and suggests a weapon, but the
    /// weapon is chosen separately.
    /// </summary>
    public class Outfit
    {
        public OutfitId id;
        public string title;
        public System.Func<Palette> palette;
        public System.Action<Dresser> dress;
        public HairStyle defaultHair;
        public WeaponId suggestedWeapon;
        public OffHandId suggestedOffHand;
        public Ability[] abilities;
    }

    public static class Outfits
    {
        static readonly Dictionary<OutfitId, Outfit> all = new Dictionary<OutfitId, Outfit>
        {
            { OutfitId.Raider, new Outfit {
                id = OutfitId.Raider, title = "The Raider",
                palette = () => new Palette(),
                defaultHair = HairStyle.LongBraids, suggestedWeapon = WeaponId.TwoHandAxe,
                abilities = new[] {
                    new Ability { id = "cleave", name = "Cleave", description = "A wide two-handed swing that hits everyone in front of you." },
                    new Ability { id = "plunder", name = "Plunderer", description = "Carry chests without slowing down as much." } },
                dress = d =>
                {
                    Garments.FurBoots(d);
                    Garments.Gloves(d);
                    Garments.LongSkirt(d, 0.15f, 1.55f);
                    Garments.FurSkirt(d, 0.46f, 0.22f);
                    Garments.Tunic(d, true);
                    Garments.RaiderBelt(d, 4);
                    Garments.Tabard(d, 0.52f, 0.1f);
                    Garments.ShoulderPelt(d, 1.1f);
                    Garments.NasalHelmet(d);
                } } },
            { OutfitId.Jarl, new Outfit {
                id = OutfitId.Jarl, title = "The Jarl",
                palette = () => new Palette {
                    cloth = new Color(0.16f, 0.17f, 0.21f), clothDark = new Color(0.1f, 0.1f, 0.12f),
                    accent = new Color(0.62f, 0.16f, 0.11f), fur = new Color(0.84f, 0.8f, 0.72f), furShadow = new Color(0.58f, 0.53f, 0.46f),
                    brass = new Color(0.82f, 0.62f, 0.26f), hair = new Color(0.74f, 0.3f, 0.12f) },
                defaultHair = HairStyle.WrappedBraids, suggestedWeapon = WeaponId.Sword, suggestedOffHand = OffHandId.RoundShield,
                abilities = new[] {
                    new Ability { id = "rally", name = "Rally the Crew", description = "Your crew rows and fights harder for a while." },
                    new Ability { id = "tribute", name = "Tribute", description = "Chests sold at home are worth more." } },
                dress = d =>
                {
                    Garments.FurBoots(d);
                    Garments.Gloves(d);
                    Garments.JarlCoat(d, 0.2f);
                    Garments.Tunic(d, false);
                    Garments.LongSleeves(d);
                    Garments.RingBelt(d);
                    Garments.BigCape(d, 1.12f, 2.1f);
                    Garments.ShoulderPelt(d, 1.3f);
                    Garments.Brooches(d);
                    Garments.JarlCrown(d);
                } } },
            { OutfitId.Navigator, new Outfit {
                id = OutfitId.Navigator, title = "The Navigator",
                palette = () => new Palette {
                    cloth = new Color(0.3f, 0.22f, 0.16f), clothDark = new Color(0.2f, 0.15f, 0.11f),
                    accent = new Color(0.23f, 0.32f, 0.43f), cloth2 = new Color(0.64f, 0.5f, 0.22f), emblem = new Color(0.87f, 0.84f, 0.74f),
                    hair = new Color(0.9f, 0.74f, 0.44f), leather = new Color(0.5f, 0.34f, 0.2f) },
                defaultHair = HairStyle.SideBraid, suggestedWeapon = WeaponId.None, suggestedOffHand = OffHandId.Map,
                abilities = new[] {
                    new Ability { id = "stars", name = "Read the Stars", description = "Shows the way to the nearest island and treasure." },
                    new Ability { id = "currents", name = "Currents", description = "The ship sails faster with you at the helm." } },
                dress = d =>
                {
                    Garments.ShinBoots(d, 0.3f);
                    Garments.Gloves(d);
                    Garments.LongSkirt(d, 0.33f, 1.35f);
                    Garments.FurSkirt(d, 0.52f, 0.36f);
                    Garments.Tunic(d, false);
                    Garments.LongSleeves(d);
                    Garments.RaiderBelt(d, 2);
                    Garments.Apron(d, 0.3f);
                    Garments.ScrollCase(d);
                    Garments.FurCollar(d);
                    Garments.SideCloak(d, 0.62f, 0.62f);
                    Garments.Bandana(d);
                } } },
            { OutfitId.SpearGuard, new Outfit {
                id = OutfitId.SpearGuard, title = "The Spear Guard",
                palette = () => new Palette {
                    cloth = new Color(0.4f, 0.28f, 0.18f), clothDark = new Color(0.22f, 0.16f, 0.12f),
                    accent = new Color(0.6f, 0.18f, 0.12f), emblem = new Color(0.9f, 0.82f, 0.68f),
                    fur = new Color(0.74f, 0.64f, 0.5f), furShadow = new Color(0.5f, 0.42f, 0.32f),
                    hair = new Color(0.8f, 0.64f, 0.42f), metal = new Color(0.42f, 0.43f, 0.45f) },
                defaultHair = HairStyle.ShortLocks, suggestedWeapon = WeaponId.Spear, suggestedOffHand = OffHandId.KnotShield,
                abilities = new[] {
                    new Ability { id = "wall", name = "Shield Wall", description = "Blocks arrows and blows from the front while braced." },
                    new Ability { id = "reach", name = "Long Reach", description = "Strike enemies from further away." } },
                dress = d =>
                {
                    Garments.FurBoots(d, 0.07f);
                    Garments.Gloves(d);
                    Garments.LongSkirt(d, 0.2f, 1.3f);
                    Garments.Tunic(d, false);
                    Garments.LongSleeves(d);
                    Garments.StuddedTrim(d);
                    Garments.RingBuckleBelt(d);
                    Garments.BigCape(d, 0.9f, 1.4f);
                    Garments.ShoulderPelt(d, 1.2f);
                    Garments.Scarf(d);
                    Garments.NasalHelmet(d);
                } } },
        };

        public static Outfit Get(OutfitId id)
        {
            Outfit o;
            return all.TryGetValue(id, out o) ? o : all[OutfitId.Raider];
        }

        public static bool Has(OutfitId id) { return all.ContainsKey(id); }
    }

    /// <summary>Everything that makes up one character: body, hair, outfit, colours and (separately) a weapon.</summary>
    public class CharacterSpec
    {
        public BodyShape body = new BodyShape();
        public HairStyle hair = HairStyle.LongBraids;
        public OutfitId outfit = OutfitId.Raider;
        /// <summary>Null = the outfit's default palette.</summary>
        public Palette palette;
        public WeaponId weapon = WeaponId.None;
        public OffHandId offHand = OffHandId.None;

        public static CharacterSpec Default(OutfitId outfit)
        {
            var o = Outfits.Get(outfit);
            return new CharacterSpec { outfit = outfit, hair = o.defaultHair, weapon = o.suggestedWeapon, offHand = o.suggestedOffHand };
        }
    }
}
