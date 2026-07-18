using Game.Cards;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class HandTests
    {
        [Test]
        public void AcePlusKing_IsBlackjack()
        {
            var hand = new Hand();
            hand.AddCard(new Card(Suit.Spades, Rank.Ace));
            hand.AddCard(new Card(Suit.Hearts, Rank.King));

            Assert.AreEqual(21, hand.Total);
            Assert.IsTrue(hand.IsBlackjack);
            Assert.IsFalse(hand.IsBust);
        }

        [Test]
        public void AcePlusNinePlusFive_AceCountsAsOne()
        {
            var hand = new Hand();
            hand.AddCard(new Card(Suit.Spades, Rank.Ace));
            hand.AddCard(new Card(Suit.Hearts, Rank.Nine));
            hand.AddCard(new Card(Suit.Clubs, Rank.Five));

            Assert.AreEqual(15, hand.Total);
            Assert.IsFalse(hand.IsBust);
            Assert.IsFalse(hand.IsBlackjack);
        }

        [Test]
        public void TenPlusTenPlusTwo_IsBust()
        {
            var hand = new Hand();
            hand.AddCard(new Card(Suit.Spades, Rank.Ten));
            hand.AddCard(new Card(Suit.Hearts, Rank.Ten));
            hand.AddCard(new Card(Suit.Clubs, Rank.Two));

            Assert.AreEqual(22, hand.Total);
            Assert.IsTrue(hand.IsBust);
            Assert.IsFalse(hand.IsBlackjack);
        }

        [Test]
        public void TwoAces_SecondAceAlsoDowngrades()
        {
            // A + A + 9 + 5 = 11 + 1 + 9 + 5 = 26 with both soft -> both must
            // downgrade to 1: 1 + 1 + 9 + 5 = 16.
            var hand = new Hand();
            hand.AddCard(new Card(Suit.Spades, Rank.Ace));
            hand.AddCard(new Card(Suit.Hearts, Rank.Ace));
            hand.AddCard(new Card(Suit.Clubs, Rank.Nine));
            hand.AddCard(new Card(Suit.Diamonds, Rank.Five));

            Assert.AreEqual(16, hand.Total);
            Assert.IsFalse(hand.IsBust);
        }

        [Test]
        public void ThreeCardTwentyOne_IsNotBlackjack()
        {
            // GDD: Blackjack only applies to the first two cards.
            var hand = new Hand();
            hand.AddCard(new Card(Suit.Spades, Rank.Seven));
            hand.AddCard(new Card(Suit.Hearts, Rank.Seven));
            hand.AddCard(new Card(Suit.Clubs, Rank.Seven));

            Assert.AreEqual(21, hand.Total);
            Assert.IsFalse(hand.IsBlackjack);
        }
    }
}
