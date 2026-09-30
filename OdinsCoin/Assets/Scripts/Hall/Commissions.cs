using System.Collections.Generic;
using UnityEngine;

namespace OdinsCoin
{
    /// <summary>A task Bjorn gives from the mead hall: plunder a named place, for a bonus when it's done.</summary>
    public struct Commission
    {
        public Place place;
        public int reward;
    }

    /// <summary>
    /// Commissions from the mead hall: raids on the real places that aren't plundered yet, each paying a bonus that
    /// grows with how far it is from home and how hard it's held. One at a time; it's paid the moment the place is
    /// plundered.
    /// </summary>
    public static class Commissions
    {
        /// <summary>Gold per kilometre from home, and on top for each guard.</summary>
        public const float PerKm = 0.6f, PerGuard = 25f;

        /// <summary>What a raid on this place is worth to Bjorn.</summary>
        public static int Reward(WorldMap map, Place place, Vector3 homeGlobal)
        {
            float km = Vector3.Distance(Places.Position(map, place), homeGlobal) / 1000f;
            return Mathf.RoundToInt((100f + km * PerKm + PlaceLife.PlunderOf(place.kind).guards * PerGuard) / 10f) * 10;
        }

        /// <summary>Up to <paramref name="count"/> raids on offer: places with plunder that aren't stripped yet, a mix of near and far.</summary>
        public static List<Commission> Offers(WorldMap map, Vector3 homeGlobal, ICollection<string> raided, int count, int seed)
        {
            var open = new List<Place>();
            foreach (var p in Places.All)
                if (PlaceLife.PlunderOf(p.kind).chests > 0 && !raided.Contains(p.name)) open.Add(p);
            open.Sort((a, b) => (Places.Position(map, a) - homeGlobal).sqrMagnitude.CompareTo((Places.Position(map, b) - homeGlobal).sqrMagnitude));
            var rng = new System.Random(seed);
            var picks = new List<Commission>();
            // The nearest one, then others from further and further afield.
            for (int i = 0; i < count && open.Count > 0; i++)
            {
                int band = Mathf.Min(open.Count - 1, i == 0 ? 0 : rng.Next(Mathf.Min(open.Count, 3 + i * 3)));
                var place = open[band];
                open.RemoveAt(band);
                picks.Add(new Commission { place = place, reward = Reward(map, place, homeGlobal) });
            }
            return picks;
        }
    }
}
