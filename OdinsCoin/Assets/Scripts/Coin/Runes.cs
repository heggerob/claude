namespace OdinsCoin
{
    public enum RuneEffect
    {
        Odds,             // Ansuz: Odin's rune, better odds
        Ward,             // Algiz: curses end sooner
        Wealth,           // Fehu: heads pays more
        LongBlessings,    // Thurisaz: blessings last longer
        Favour,           // Raidho: Odin's favour fills faster
        Hail,             // Hagalaz: worse odds, much bigger payout
    }

    public class RuneCard
    {
        public string id;
        public string glyph;      // the Elder Futhark letter
        public string name;
        public string description;
        public RuneEffect effect;
        /// <summary>Gold to have it carved into the coin.</summary>
        public int cost;
    }

    /// <summary>Runes that can be carved into Odin's coin. Three fit on the rim.</summary>
    public static class Runes
    {
        public const int Slots = 3;

        public static readonly RuneCard[] All =
        {
            new RuneCard { id = "ansuz",    glyph = "ᚨ", name = "Ansuz",    effect = RuneEffect.Odds,          cost = 250,
                description = "Odin's own rune. +6% chance of Odin's eye." },
            new RuneCard { id = "algiz",    glyph = "ᛉ", name = "Algiz",    effect = RuneEffect.Ward,          cost = 150,
                description = "The elk-sedge wards you: curses last 30% shorter." },
            new RuneCard { id = "fehu",     glyph = "ᚠ", name = "Fehu",     effect = RuneEffect.Wealth,        cost = 200,
                description = "Cattle and gold: Odin's eye pays 2.25× instead of 2×." },
            new RuneCard { id = "thurisaz", glyph = "ᚦ", name = "Thurisaz", effect = RuneEffect.LongBlessings, cost = 150,
                description = "Thor's thorn: blessings last 30% longer." },
            new RuneCard { id = "raidho",   glyph = "ᚱ", name = "Raidho",   effect = RuneEffect.Favour,        cost = 100,
                description = "The long road: Odin's favour fills 50% faster." },
            new RuneCard { id = "hagalaz",  glyph = "ᚺ", name = "Hagalaz",  effect = RuneEffect.Hail,          cost = 120,
                description = "Hail! -10% chance of Odin's eye, but it pays 2.75×. For the bold." },
        };

        public static RuneCard Find(string id)
        {
            foreach (var r in All) if (r.id == id) return r;
            return null;
        }
    }
}
