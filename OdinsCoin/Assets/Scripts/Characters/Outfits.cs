using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    public enum OutfitId { Raider, Jarl, Navigator, SpearGuard, Seer, Scout }
    public enum HairStyle { None, LongBraids }
    public enum WeaponId { None, TwoHandAxe }

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

        public static CharacterSpec Default(OutfitId outfit)
        {
            var o = Outfits.Get(outfit);
            return new CharacterSpec { outfit = outfit, hair = o.defaultHair, weapon = o.suggestedWeapon };
        }
    }
}
