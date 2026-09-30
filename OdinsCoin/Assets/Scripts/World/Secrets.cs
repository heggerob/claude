namespace OdinsCoin
{
    /// <summary>
    /// Every place's little secrets, counted for the chart: its rune ring woken, its hoard dug up, its cave emptied
    /// and its raven feathers found.
    /// </summary>
    public static class Secrets
    {
        public const int PerPlace = 3 + Feathers.PerPlace;

        /// <summary>How many of a place's secrets you've found.</summary>
        public static int Found(Place place, Upgrades u)
        {
            int n = 0;
            if (u.Shrines.Contains(place.name)) n++;
            if (u.Dug.Contains(place.name)) n++;
            if (u.Caves.Contains(place.name)) n++;
            for (int i = 0; i < Feathers.PerPlace; i++) if (u.Feathers.Contains(Feathers.Key(place, i))) n++;
            return n;
        }

        /// <summary>Odin's reward when the last of a place's secrets is found (gold).</summary>
        public const int CompleteGold = 200;

        /// <summary>The reward a find just earned: <see cref="CompleteGold"/> if it was the place's last secret, else 0.</summary>
        public static int Reward(Place place, Upgrades u) { return place != null && Found(place, u) == PerPlace ? CompleteGold : 0; }

        /// <summary>How many places you've found every secret of.</summary>
        public static int Completed(Upgrades u)
        {
            int n = 0;
            foreach (var p in Places.All) if (Found(p, u) == PerPlace) n++;
            return n;
        }

        /// <summary>
        /// A secret of the named place was just found (call once, when it's first recorded): if it was the last,
        /// the reward is paid and a banner says so (over the find's own banner).
        /// </summary>
        public static void Found(string placeName)
        {
            var u = Upgrades.Current;
            int gold = Reward(Places.Find(placeName), u);
            if (gold == 0) return;
            Fortune.Current.Gold += gold;
            CombatHud.Banner("EVERY SECRET OF " + placeName.ToUpperInvariant(), "Ring, hoard, cave and feathers, all yours: " + gold + " gold. (" + Completed(u) + " of " + Places.All.Length + " places.)");
        }

        /// <summary>The chart's note beside a place's name: nothing until you've found one, then "3/6" (gold when all are found).</summary>
        public static string ChartNote(Place place, Upgrades u)
        {
            int n = Found(place, u);
            if (n == 0) return "";
            return n == PerPlace ? "  <color=#b8860b><size=11>★ " + n + "/" + PerPlace + "</size></color>" : "  <size=11>" + n + "/" + PerPlace + "</size>";
        }
    }
}
