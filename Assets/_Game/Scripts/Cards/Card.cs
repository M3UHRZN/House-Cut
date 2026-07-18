namespace Game.Cards
{
    public readonly struct Card
    {
        public Suit Suit { get; }
        public Rank Rank { get; }

        public Card(Suit suit, Rank rank)
        {
            Suit = suit;
            Rank = rank;
        }

        /// <summary>
        /// Base blackjack value: 2-10 own value, J/Q/K = 10, Ace = 11.
        /// Ace's soft/hard (11 vs 1) resolution is hand-total logic, see issue #6.
        /// </summary>
        public int BaseValue
        {
            get
            {
                switch (Rank)
                {
                    case Rank.Ace: return 11;
                    case Rank.Jack:
                    case Rank.Queen:
                    case Rank.King: return 10;
                    default: return (int)Rank + 1;
                }
            }
        }

        public override string ToString() => $"{Rank} of {Suit}";
    }
}
