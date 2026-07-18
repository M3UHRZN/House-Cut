using System;
using System.Collections.Generic;

namespace Game.Cards
{
    public class Deck
    {
        public const int ReshuffleThreshold = 15;

        private readonly List<Card> cards = new List<Card>(52);
        private readonly Random random;

        public int Count => cards.Count;

        public Deck(int? seed = null)
        {
            random = seed.HasValue ? new Random(seed.Value) : new Random();
            Reset();
        }

        /// <summary>Refills the deck with all 52 unique cards and shuffles it.</summary>
        public void Reset()
        {
            cards.Clear();
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    cards.Add(new Card(suit, rank));
                }
            }

            Shuffle();
        }

        /// <summary>Fisher-Yates shuffle of the current cards in place.</summary>
        public void Shuffle()
        {
            for (int i = cards.Count - 1; i > 0; i--)
            {
                int j = random.Next(i + 1);
                (cards[i], cards[j]) = (cards[j], cards[i]);
            }
        }

        /// <summary>
        /// Draws the top card. If the deck drops below <see cref="ReshuffleThreshold"/>
        /// after the draw, it is automatically refilled and reshuffled (GDD §03.1 rule 7).
        /// </summary>
        public Card Draw()
        {
            if (cards.Count == 0)
            {
                throw new InvalidOperationException("Cannot draw from an empty deck.");
            }

            int lastIndex = cards.Count - 1;
            Card drawn = cards[lastIndex];
            cards.RemoveAt(lastIndex);

            if (cards.Count < ReshuffleThreshold)
            {
                Reset();
            }

            return drawn;
        }
    }
}
