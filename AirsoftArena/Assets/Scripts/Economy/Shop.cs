using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    /// <summary>A loot crate bought with in-game money. Odds are shown to the player before buying.</summary>
    public class CrateType
    {
        public string id;
        public string name;
        public string description;
        public int price;
        /// <summary>Chance per rarity, Common..Legendary. Sums to 1.</summary>
        public float[] odds;
        /// <summary>Fraction of the crate price paid back when you already own the item.</summary>
        public float duplicateRefund;
    }

    public class CrateResult
    {
        public CrateType crate;
        public CosmeticItem item;
        public bool duplicate;
        public int refund;
    }

    public enum PurchaseResult { Ok, AlreadyOwned, NotEnoughMoney, Unknown }

    /// <summary>
    /// Everything that costs in-game money: cosmetics, weapons and crates.
    /// No real money anywhere: crates are bought with money earned by playing or refereeing.
    /// </summary>
    public static class Shop
    {
        public static readonly CrateType[] Crates =
        {
            new CrateType
            {
                id = "crate_field", name = "Field Crate", price = 150, duplicateRefund = 0.4f,
                description = "A dusty ammo box from the field shop.",
                odds = new[] { 0.70f, 0.23f, 0.06f, 0.01f },
            },
            new CrateType
            {
                id = "crate_operator", name = "Operator Crate", price = 400, duplicateRefund = 0.5f,
                description = "Better odds. No commons inside.",
                odds = new[] { 0f, 0.65f, 0.28f, 0.07f },
            },
        };

        public static PurchaseResult Buy(PlayerProfile profile, CosmeticItem item)
        {
            if (item == null) return PurchaseResult.Unknown;
            if (profile.Owns(item)) return PurchaseResult.AlreadyOwned;
            if (profile.money < item.price) return PurchaseResult.NotEnoughMoney;
            profile.money -= item.price;
            profile.Unlock(item);
            PlayerProfile.Save();
            return PurchaseResult.Ok;
        }

        public static PurchaseResult Buy(PlayerProfile profile, WeaponData weapon)
        {
            if (weapon == null) return PurchaseResult.Unknown;
            if (profile.OwnsWeapon(weapon)) return PurchaseResult.AlreadyOwned;
            if (profile.money < weapon.price) return PurchaseResult.NotEnoughMoney;
            profile.money -= weapon.price;
            profile.ownedWeapons.Add(weapon.code);
            PlayerProfile.Save();
            return PurchaseResult.Ok;
        }

        /// <summary>Pays for and opens a crate. Returns null if you can't afford it.</summary>
        public static CrateResult OpenCrate(PlayerProfile profile, CrateType crate)
        {
            if (profile.money < crate.price) return null;
            profile.money -= crate.price;

            var rarity = RollRarity(crate.odds, Random.value);
            var pool = new List<CosmeticItem>();
            foreach (var c in CosmeticCatalog.All)
                if (c.rarity == rarity && !c.starter) pool.Add(c);
            var item = pool[Random.Range(0, pool.Count)];

            var result = new CrateResult { crate = crate, item = item, duplicate = profile.Owns(item) };
            if (result.duplicate)
            {
                result.refund = Mathf.RoundToInt(crate.price * crate.duplicateRefund);
                profile.money += result.refund;
            }
            else
            {
                profile.Unlock(item);
            }
            profile.cratesOpened++;
            PlayerProfile.Save();
            return result;
        }

        /// <summary>Maps a 0..1 roll onto the rarity odds table.</summary>
        public static Rarity RollRarity(float[] odds, float roll)
        {
            for (int i = 0; i < odds.Length; i++)
            {
                roll -= odds[i];
                if (roll < 0f && odds[i] > 0f) return (Rarity)i;
            }
            // Floating point leftovers land on the best rarity that has a chance.
            for (int i = odds.Length - 1; i >= 0; i--) if (odds[i] > 0f) return (Rarity)i;
            return Rarity.Common;
        }

        /// <summary>Random cosmetics for the crate "reel" animation, weighted like the crate's odds.</summary>
        public static CosmeticItem RandomReelItem(CrateType crate)
        {
            var rarity = RollRarity(crate.odds, Random.value);
            var pool = new List<CosmeticItem>();
            foreach (var c in CosmeticCatalog.All) if (c.rarity == rarity) pool.Add(c);
            return pool[Random.Range(0, pool.Count)];
        }
    }
}
