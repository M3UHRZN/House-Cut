using Game.Cards;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class HandResolverTests
    {
        private static Hand HandWithTotal(params (Suit suit, Rank rank)[] cardSpecs)
        {
            var hand = new Hand();
            foreach (var (suit, rank) in cardSpecs)
            {
                hand.AddCard(new Card(suit, rank));
            }

            return hand;
        }

        [Test]
        public void EqualTotal_DealerWins()
        {
            var player = HandWithTotal((Suit.Spades, Rank.Ten), (Suit.Hearts, Rank.Nine));
            var dealer = HandWithTotal((Suit.Clubs, Rank.Ten), (Suit.Diamonds, Rank.Nine));

            Assert.AreEqual(19, player.Total);
            Assert.AreEqual(19, dealer.Total);
            Assert.AreEqual(HandOutcome.DealerWins, HandResolver.Resolve(player, dealer));
        }

        [Test]
        public void PlayerTwenty_VsDealerNineteen_PlayerWins()
        {
            var player = HandWithTotal((Suit.Spades, Rank.Ten), (Suit.Hearts, Rank.King));
            var dealer = HandWithTotal((Suit.Clubs, Rank.Ten), (Suit.Diamonds, Rank.Nine));

            Assert.AreEqual(20, player.Total);
            Assert.AreEqual(19, dealer.Total);
            Assert.AreEqual(HandOutcome.PlayerWins, HandResolver.Resolve(player, dealer));
        }

        [Test]
        public void PlayerBust_DealerWinsRegardlessOfDealerTotal()
        {
            var player = HandWithTotal((Suit.Spades, Rank.Ten), (Suit.Hearts, Rank.Ten), (Suit.Clubs, Rank.Five));
            var dealer = HandWithTotal((Suit.Clubs, Rank.Two), (Suit.Diamonds, Rank.Three));

            Assert.IsTrue(player.IsBust);
            Assert.AreEqual(HandOutcome.DealerWins, HandResolver.Resolve(player, dealer));
        }

        [Test]
        public void DealerBust_PlayerWins()
        {
            var player = HandWithTotal((Suit.Spades, Rank.Ten), (Suit.Hearts, Rank.Seven));
            var dealer = HandWithTotal((Suit.Clubs, Rank.Ten), (Suit.Diamonds, Rank.Ten), (Suit.Hearts, Rank.Five));

            Assert.IsTrue(dealer.IsBust);
            Assert.AreEqual(HandOutcome.PlayerWins, HandResolver.Resolve(player, dealer));
        }

        [Test]
        public void NormalWin_PaysOneToOne()
        {
            var player = HandWithTotal((Suit.Spades, Rank.Ten), (Suit.Hearts, Rank.King));
            var dealer = HandWithTotal((Suit.Clubs, Rank.Ten), (Suit.Diamonds, Rank.Nine));

            int payout = HandResolver.CalculatePayout(player, HandOutcome.PlayerWins, bet: 700, blackjackPayout: 1.5f);

            Assert.AreEqual(700, payout);
        }

        [Test]
        public void Loss_PaysNegativeBet()
        {
            var player = HandWithTotal((Suit.Spades, Rank.Ten), (Suit.Hearts, Rank.Nine));

            int payout = HandResolver.CalculatePayout(player, HandOutcome.DealerWins, bet: 700, blackjackPayout: 1.5f);

            Assert.AreEqual(-700, payout);
        }

        [Test]
        public void Blackjack_PaysOnePointFiveTimesBet()
        {
            var player = HandWithTotal((Suit.Spades, Rank.Ace), (Suit.Hearts, Rank.King));

            int payout = HandResolver.CalculatePayout(player, HandOutcome.PlayerWins, bet: 700, blackjackPayout: 1.5f);

            Assert.AreEqual(1050, payout);
        }
    }
}
