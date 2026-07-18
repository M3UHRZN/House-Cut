using System.Collections.Generic;

namespace Game.Cards
{
    /// <summary>
    /// A blackjack hand. Resolves Ace as 11 (soft) unless that would bust,
    /// in which case Aces count as 1 one at a time until the total holds
    /// (GDD §03.2, rule 2) or all Aces have been downgraded.
    /// </summary>
    public class Hand
    {
        private readonly List<Card> cards = new List<Card>();

        public IReadOnlyList<Card> Cards => cards;

        public void AddCard(Card card) => cards.Add(card);

        public int Total
        {
            get
            {
                int total = 0;
                int aceCount = 0;

                foreach (Card card in cards)
                {
                    total += card.BaseValue;
                    if (card.Rank == Rank.Ace)
                    {
                        aceCount++;
                    }
                }

                while (total > 21 && aceCount > 0)
                {
                    total -= 10;
                    aceCount--;
                }

                return total;
            }
        }

        public bool IsBust => Total > 21;

        public bool IsBlackjack => cards.Count == 2 && Total == 21;
    }
}
