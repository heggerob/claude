using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    public enum OutfitId { Raider, Jarl, Navigator, SpearGuard, Seer, Scout }
    public enum HairStyle { None, LongBraids, WrappedBraids, SideBraid, ShortLocks, VeryLongBraids, LowBraid }
    public enum WeaponId { None, TwoHandAxe, Sword, Spear, Staff, Bow }
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
                    Garments.ShoulderPelt(d, 1.1f, 0f, 0.95f, 2.6f);
                    Garments.NasalHelmet(d);
                } } },
            { OutfitId.Jarl, new Outfit {
                id = OutfitId.Jarl, title = "The Jarl",
                palette = () => new Palette {
                    cloth = new Color(0.19f, 0.19f, 0.2f), clothDark = new Color(0.11f, 0.11f, 0.12f),
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
                    Garments.ShoulderPelt(d, 1.3f, 0f, 0.92f, 2.2f);
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
                    Garments.ShinBoots(d, 0.24f);
                    Garments.Gloves(d);
                    Garments.LongSkirt(d, 0.2f, 1.35f);
                    Garments.Underskirt(d, 0.08f);
                    Garments.Tunic(d, false);
                    Garments.LongSleeves(d);
                    Garments.RaiderBelt(d, 2);
                    Garments.Apron(d, 0.3f);
                    Garments.ScrollCase(d);
                    Garments.ShoulderPelt(d, 1.0f);
                    Garments.SideCloak(d, 0.62f, 0.85f);
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
                    Garments.FurBoots(d, 0.13f);
                    Garments.Trousers(d, 0.13f);
                    Garments.Gloves(d);
                    Garments.LongSkirt(d, 0.3f, 1.3f);
                    Garments.Tunic(d, false);
                    Garments.LongSleeves(d);
                    Garments.StuddedTrim(d);
                    Garments.RingBuckleBelt(d);
                    Garments.BigCape(d, 0.9f, 1.4f, false);
                    Garments.ShoulderPelt(d, 1.2f, 0f, 0.9f, 2.2f);
                    Garments.Scarf(d);
                    Garments.NasalHelmet(d);
                } } },
            { OutfitId.Seer, new Outfit {
                id = OutfitId.Seer, title = "The Old Seer",
                palette = () => new Palette {
                    cloth = new Color(0.2f, 0.22f, 0.27f), clothDark = new Color(0.12f, 0.13f, 0.16f),
                    cloth2 = new Color(0.7f, 0.6f, 0.44f), hair = new Color(0.9f, 0.88f, 0.82f),
                    leather = new Color(0.46f, 0.34f, 0.22f), leatherDark = new Color(0.25f, 0.18f, 0.12f),
                    parchment = new Color(0.88f, 0.84f, 0.74f), emblem = new Color(0.55f, 0.82f, 1f) },
                defaultHair = HairStyle.VeryLongBraids, suggestedWeapon = WeaponId.Staff,
                abilities = new[] {
                    new Ability { id = "foresight", name = "Foresight", description = "See how Odin's coin will land before you wager." },
                    new Ability { id = "ward", name = "Ward of Runes", description = "Curses on you and your crew wear off faster." } },
                dress = d =>
                {
                    Garments.ShinBoots(d, 0.2f);
                    Garments.Gloves(d);
                    Garments.Robe(d);
                    Garments.Tunic(d, false);
                    Garments.FeatherCloak(d);
                    Garments.Stole(d);
                    Garments.Charms(d);
                    Garments.Hood(d);
                    Garments.Antlers(d);
                } } },
            { OutfitId.Scout, new Outfit {
                id = OutfitId.Scout, title = "The Scout",
                palette = () => new Palette {
                    cloth = new Color(0.3f, 0.23f, 0.17f), clothDark = new Color(0.19f, 0.15f, 0.12f),
                    accent = new Color(0.34f, 0.4f, 0.28f), emblem = new Color(0.8f, 0.66f, 0.36f),
                    fur = new Color(0.82f, 0.76f, 0.64f), hair = new Color(0.42f, 0.28f, 0.17f) },
                defaultHair = HairStyle.LowBraid, suggestedWeapon = WeaponId.Bow,
                abilities = new[] {
                    new Ability { id = "keen", name = "Keen Eyes", description = "Spot treasure and enemies from much further away." },
                    new Ability { id = "volley", name = "Volley", description = "Loose three arrows at once." } },
                dress = d =>
                {
                    Garments.FurBoots(d, 0.04f);
                    Garments.Gloves(d);
                    Garments.LongSkirt(d, 0.3f, 1.3f);
                    Garments.FurSkirt(d, 0.55f, 0.4f);
                    Garments.Tunic(d, false);
                    Garments.Bracers(d);
                    Garments.RaiderBelt(d, 1);
                    Garments.Tabard(d, 0.42f, 0.09f);
                    Garments.BeltKnife(d);
                    Garments.Quiver(d);
                    Garments.ShoulderPelt(d, 1.0f, 1f);
                    Garments.Cowl(d);
                    Garments.FurCap(d);
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
