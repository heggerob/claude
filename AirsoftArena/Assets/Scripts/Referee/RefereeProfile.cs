using System.Collections.Generic;
using UnityEngine;

namespace AirsoftArena
{
    /// <summary>
    /// A referee for hire. Players see the star rating and the price; the real skill is hidden.
    /// Better-rated referees charge more, so a good referee earns more per match.
    /// </summary>
    [System.Serializable]
    public class RefereeProfile
    {
        public string name;
        public string tagline;
        [Tooltip("Hidden true ability 0..1: how often they spot cheaters and how rarely they make wrong calls.")]
        public float skill;
        public float starsTotal;
        public int ratings;
        public int matches;
        public int caught;
        public int missed;
        public int wrongCalls;

        public float Stars { get { return ratings > 0 ? starsTotal / ratings : 3f; } }
        public int FeePerPlayer { get { return FeeForStars(Stars); } }

        /// <summary>1 star ~ $8, 3 stars ~ $34, 5 stars ~ $85 per player.</summary>
        public static int FeeForStars(float stars)
        {
            return Mathf.RoundToInt(5f + stars * stars * 3.2f);
        }

        public void AddRating(float stars)
        {
            starsTotal += Mathf.Clamp(stars, 1f, 5f);
            ratings++;
        }

        public static string StarText(float stars)
        {
            int full = Mathf.Clamp(Mathf.RoundToInt(stars), 0, 5);
            return new string('★', full) + new string('☆', 5 - full) + " " + stars.ToString("0.0");
        }
    }

    /// <summary>The list of AI referees you can hire. Saved between sessions so ratings build up over time.</summary>
    public static class RefereeMarket
    {
        const string SaveKey = "AirsoftArena.Referees.v1";

        [System.Serializable]
        class SaveData { public List<RefereeProfile> referees = new List<RefereeProfile>(); }

        static List<RefereeProfile> referees;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { referees = null; }

        /// <summary>Forget the cached list so it is re-read (or re-seeded) from PlayerPrefs.</summary>
        public static void Reload() { referees = null; }

        public static List<RefereeProfile> All
        {
            get
            {
                if (referees == null) Load();
                return referees;
            }
        }

        static void Load()
        {
            string json = PlayerPrefs.GetString(SaveKey, "");
            if (!string.IsNullOrEmpty(json))
            {
                var data = JsonUtility.FromJson<SaveData>(json);
                if (data != null && data.referees != null && data.referees.Count > 0)
                {
                    referees = data.referees;
                    return;
                }
            }
            referees = new List<RefereeProfile>
            {
                Seed("Blind Bob", "Forgot his glasses. Again.", 0.22f),
                Seed("Sleepy Sven", "Mostly watches the coffee machine.", 0.4f),
                Seed("Whistle Wendy", "Loud, fair, sometimes too fast.", 0.6f),
                Seed("Hawkeye Harald", "Sees BBs bounce off your goggles from 30 m.", 0.82f),
                Seed("The Ghost", "You never see him. He always sees you.", 0.95f),
            };
            Save();
        }

        public static void Save()
        {
            var data = new SaveData { referees = All };
            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(data));
            PlayerPrefs.Save();
        }

        static RefereeProfile Seed(string name, string tagline, float skill)
        {
            var r = new RefereeProfile { name = name, tagline = tagline, skill = skill };
            int history = Random.Range(6, 20);
            for (int i = 0; i < history; i++) r.AddRating(1f + 4f * skill + Random.Range(-0.8f, 0.8f));
            r.matches = history + Random.Range(0, 30);
            return r;
        }
    }
}
