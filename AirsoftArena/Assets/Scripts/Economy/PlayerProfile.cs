using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    /// <summary>
    /// Everything the game remembers about you: money (in-game only), skill rating, honor, and your career as a referee.
    /// Stored in PlayerPrefs for the prototype; move to Steam Cloud / a server later.
    /// </summary>
    [System.Serializable]
    public class PlayerProfile
    {
        const string SaveKey = "AirsoftArena.Profile.v1";
        public const int StartingMoney = 500;

        public string playerName = "You";
        public int money = StartingMoney;

        [Header("Progression")]
        public int xp;
        public int dailyStreak;
        public string lastDailyDay = "";

        [Header("As a player")]
        public float skillRating = 1000f;
        [Tooltip("0..100. Goes down when the referee catches you not calling hits, shooting players who are out, or shooting the ref.")]
        public float honor = 100f;
        public int matches;
        public int wins;

        [Header("As a referee")]
        public float refStarsTotal;
        public int refRatings;
        public int refMatches;
        public int refCorrectCalls;
        public int refWrongCalls;
        public int refMissed;

        [Header("Loadout")]
        public List<string> ownedWeapons = new List<string>();
        public string primaryWeapon = "01-VK4";
        public string secondaryWeapon = "04-PP2";
        public int cratesOpened;
        public string mapId = "pallet_yard";
        public GameMode mode = GameMode.TeamDeathmatch;
        public int teamSize = 4;

        [Header("Cosmetics")]
        public List<string> ownedItems = new List<string>();
        public string equippedCamo = CosmeticCatalog.DefaultId(CosmeticSlot.Camo);
        public string equippedUniform = CosmeticCatalog.DefaultId(CosmeticSlot.Uniform);
        public string equippedHeadGear = CosmeticCatalog.DefaultId(CosmeticSlot.HeadGear);
        public string equippedTracer = CosmeticCatalog.DefaultId(CosmeticSlot.Tracer);

        public float RefStars { get { return refRatings > 0 ? refStarsTotal / refRatings : 3f; } }

        public int Level { get { return Progression.LevelForXp(xp); } }
        public string RankName { get { return Progression.RankName(Level); } }

        public bool OwnsWeapon(WeaponData weapon) { return weapon != null && (weapon.price <= 0 || ownedWeapons.Contains(weapon.code)); }

        /// <summary>The saved primary, falling back to the first free one if it's missing or not owned.</summary>
        public WeaponData Primary { get { return OwnedOr(primaryWeapon, WeaponCatalog.Primaries); } }
        public WeaponData Secondary { get { return OwnedOr(secondaryWeapon, WeaponCatalog.Secondaries); } }

        WeaponData OwnedOr(string code, List<WeaponData> list)
        {
            var w = WeaponCatalog.Get(code);
            if (w != null && list.Contains(w) && OwnsWeapon(w)) return w;
            foreach (var x in list) if (OwnsWeapon(x)) return x;
            return list[0];
        }

        public bool Owns(CosmeticItem item) { return item != null && (item.starter || ownedItems.Contains(item.id)); }

        public void Unlock(CosmeticItem item)
        {
            if (item != null && !Owns(item)) ownedItems.Add(item.id);
        }

        public string Equipped(CosmeticSlot slot)
        {
            switch (slot)
            {
                case CosmeticSlot.Camo: return equippedCamo;
                case CosmeticSlot.Uniform: return equippedUniform;
                case CosmeticSlot.HeadGear: return equippedHeadGear;
                default: return equippedTracer;
            }
        }

        /// <summary>Wears an owned item. Returns false if you don't own it.</summary>
        public bool Equip(CosmeticItem item)
        {
            if (!Owns(item)) return false;
            switch (item.slot)
            {
                case CosmeticSlot.Camo: equippedCamo = item.id; break;
                case CosmeticSlot.Uniform: equippedUniform = item.id; break;
                case CosmeticSlot.HeadGear: equippedHeadGear = item.id; break;
                default: equippedTracer = item.id; break;
            }
            return true;
        }

        public SoldierLook Look { get { return CosmeticCatalog.BuildLook(equippedCamo, equippedUniform, equippedHeadGear, equippedTracer); } }
        public int RefFeePerPlayer { get { return RefereeProfile.FeeForStars(RefStars); } }

        static PlayerProfile current;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { current = null; }

        public static PlayerProfile Current
        {
            get
            {
                if (current == null)
                {
                    string json = PlayerPrefs.GetString(SaveKey, "");
                    if (!string.IsNullOrEmpty(json)) current = JsonUtility.FromJson<PlayerProfile>(json);
                    if (current == null) current = new PlayerProfile();
                }
                return current;
            }
        }

        public static void Save()
        {
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(Current));
            PlayerPrefs.Save();
        }

        /// <summary>Wipes the profile and referee market. Handy while testing.</summary>
        public static void ResetAll()
        {
            PlayerPrefs.DeleteAll();
            RefereeMarket.Reload();
            current = new PlayerProfile();
            Save();
        }
    }
}
