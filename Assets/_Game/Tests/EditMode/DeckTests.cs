using System.Collections.Generic;
using Game.Cards;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class DeckTests
    {
        [Test]
        public void Reset_Creates52UniqueCards()
        {
            var deck = new Deck(seed: 1);

            Assert.AreEqual(52, deck.Count);

            var seen = new HashSet<(Suit, Rank)>();
            // Draw exactly down to the reshuffle threshold so no auto-refill
            // happens mid-loop; every drawn card in this range must be unique.
            int drawCount = 52 - Deck.ReshuffleThreshold;
            for (int i = 0; i < drawCount; i++)
            {
                Card card = deck.Draw();
                seen.Add((card.Suit, card.Rank));
            }

            Assert.AreEqual(drawCount, seen.Count);
        }

        [Test]
        public void Draw_ReducesDeckCount()
        {
            var deck = new Deck(seed: 2);
            int before = deck.Count;

            deck.Draw();

            Assert.AreEqual(before - 1, deck.Count);
        }

        [Test]
        public void Draw_BelowThreshold_TriggersAutoReshuffle()
        {
            var deck = new Deck(seed: 3);

            // Draw down to exactly the threshold; the next draw crosses below it
            // and must trigger an automatic reshuffle back to a full 52-card deck.
            while (deck.Count > Deck.ReshuffleThreshold)
            {
                deck.Draw();
            }

            Assert.AreEqual(Deck.ReshuffleThreshold, deck.Count);

            deck.Draw();

            Assert.AreEqual(52, deck.Count, "deck must auto-refill and reshuffle once it drops below the threshold");
        }

        [Test]
        public void Draw_FromFreshDeck_NeverThrows()
        {
            var deck = new Deck(seed: 4);

            Assert.DoesNotThrow(() =>
            {
                for (int i = 0; i < 200; i++)
                {
                    deck.Draw();
                }
            });
        }

        [Test]
        public void CardValues_MatchGddSpec()
        {
            Assert.AreEqual(2, new Card(Suit.Spades, Rank.Two).BaseValue);
            Assert.AreEqual(10, new Card(Suit.Spades, Rank.Ten).BaseValue);
            Assert.AreEqual(10, new Card(Suit.Spades, Rank.Jack).BaseValue);
            Assert.AreEqual(10, new Card(Suit.Spades, Rank.Queen).BaseValue);
            Assert.AreEqual(10, new Card(Suit.Spades, Rank.King).BaseValue);
            Assert.AreEqual(11, new Card(Suit.Spades, Rank.Ace).BaseValue);
        }
    }
}
