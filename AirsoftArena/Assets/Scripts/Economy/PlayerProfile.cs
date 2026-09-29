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

        public float RefStars { get { return refRatings > 0 ? refStarsTotal / refRatings : 3f; } }
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
