namespace OdinsCoin
{
    /// <summary>
    /// Bjorn's dice game in the mead hall. You and Bjorn each roll three dice; the higher hand wins the stake.
    /// Any triple beats any sum (higher triple wins). A tie gives your stake back. It's an even game:
    /// neither side has an edge, unless Odin's Favour lets you reroll your lowest die.
    /// </summary>
    public static class MeadDice
    {
        public static readonly int[] Stakes = { 10, 25, 50, 100 };

        /// <summary>A comparable value for a hand of three dice: sums 3..18, triples 101..106.</summary>
        public static int Rank(int[] dice)
        {
            if (dice[0] == dice[1] && dice[1] == dice[2]) return 100 + dice[0];
            return dice[0] + dice[1] + dice[2];
        }

        /// <summary>+1 you win, -1 Bjorn wins, 0 a tie.</summary>
        public static int Compare(int[] you, int[] bjorn)
        {
            int a = Rank(you), b = Rank(bjorn);
            return a > b ? 1 : a < b ? -1 : 0;
        }

        public static string Describe(int[] dice)
        {
            int r = Rank(dice);
            return r > 100 ? "three " + (r - 100) + "s!" : r.ToString();
        }

        /// <summary>Index of the lowest die (the one Odin's Favour lets you reroll).</summary>
        public static int Lowest(int[] dice)
        {
            int i = 0;
            for (int k = 1; k < dice.Length; k++) if (dice[k] < dice[i]) i = k;
            return i;
        }

        public static int[] Roll(System.Random rng) { return new[] { rng.Next(1, 7), rng.Next(1, 7), rng.Next(1, 7) }; }

        /// <summary>Settle the stake. Returns the change in gold.</summary>
        public static int Settle(Fortune fortune, int stake, int outcome)
        {
            int change = outcome * stake;
            fortune.Gold += change;
            if (outcome > 0) fortune.DiceWon++;
            else if (outcome < 0) fortune.DiceLost++;
            return change;
        }
    }
}
