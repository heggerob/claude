namespace OdinsCoin
{
    /// <summary>
    /// A new player's first voyage, one hint at a time: go aboard, take the helm, get under way, find a chest,
    /// bring it aboard, and stake it at Odin's altar or sell it. Each hint goes as soon as it's done (or skipped
    /// past by doing something later), and once the first chest is staked or sold the hints are gone for good.
    /// Pure logic; the ship's HUD feeds it what's happening and shows the current hint.
    /// </summary>
    public static class FirstSteps
    {
        public enum Step { Board, Helm, Sail, Plunder, Stow, Stake, Done }

        /// <summary>What's happening now, as far as the hints care.</summary>
        public struct State
        {
            public bool onShip, atHelm, carrying, openedChest, chestOnDeck, stakedOrSold;
            public float knots;
        }

        /// <summary>Speed that counts as under way (knots).</summary>
        public const float UnderWay = 2f;

        /// <summary>Set when a stake is thrown at the altar (the HUD can't see that happen).</summary>
        public static bool Staked;

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Staked = false; }

        /// <summary>The step reached from <paramref name="at"/> given what's happening: as far as it goes.</summary>
        public static Step Next(Step at, State s)
        {
            if (s.stakedOrSold) return Step.Done;
            // Treasure found or already aboard counts, whatever came before it.
            if (at < Step.Stow && (s.openedChest || s.carrying)) at = Step.Stow;
            if (at < Step.Stake && s.chestOnDeck) at = Step.Stake;
            if (at == Step.Board && s.onShip) at = Step.Helm;
            if (at == Step.Helm && s.atHelm) at = Step.Sail;
            if (at == Step.Sail && s.knots >= UnderWay) at = Step.Plunder;
            return at;
        }

        public static string Hint(Step s)
        {
            switch (s)
            {
                case Step.Board: return "Walk down the jetty and step aboard your longship (from the water, E beside her climbs up).";
                case Step.Helm: return "Go aft to the steering oar and press E to take the helm.";
                case Step.Sail: return "Press W to get under way (W again for more sail), A/D to steer. M opens the chart.";
                case Step.Plunder: return "Sail to a town and find its treasure. Ask the townsfolk (E) where the riches are.";
                case Step.Stow: return "Carry the chest (E) back to your ship and set it down on deck.";
                case Step.Stake: return "Stake it at Odin's altar on deck, double or nothing, or sell it to Gunnar in the Home Fjord.";
                default: return null;
            }
        }

        /// <summary>The hint as the HUD shows it, with how far along you are, or null when there's none.</summary>
        public static string Line(Step s)
        {
            var hint = Hint(s);
            return hint == null ? null : "<b>First voyage " + ((int)s + 1) + "/" + (int)Step.Done + ":</b> " + hint;
        }
    }
}
