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

        /// <summary>The chart's note beside a place's name: nothing until you've found one, then "3/6" (gold when all are found).</summary>
        public static string ChartNote(Place place, Upgrades u)
        {
            int n = Found(place, u);
            if (n == 0) return "";
            return n == PerPlace ? "  <color=#b8860b><size=11>★ " + n + "/" + PerPlace + "</size></color>" : "  <size=11>" + n + "/" + PerPlace + "</size>";
        }
    }
}
