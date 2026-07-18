using Game.Cards;
using Game.Data;
using Game.Economy;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class SettlementTests
    {
        private GameConfig config;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<GameConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
        }

        private static Hand HandOf(params (Suit suit, Rank rank)[] cardSpecs)
        {
            var hand = new Hand();
            foreach (var (suit, rank) in cardSpecs)
            {
                hand.AddCard(new Card(suit, rank));
            }

            return hand;
        }

        [Test]
        public void Win_AddsBet()
        {
            var bank = new Bank(config);
            var player = HandOf((Suit.Spades, Rank.Ten), (Suit.Hearts, Rank.King)); // 20
            var dealer = HandOf((Suit.Clubs, Rank.Ten), (Suit.Diamonds, Rank.Nine)); // 19

            int delta = Settlement.Settle(bank, player, dealer, bet: 700, config);

            Assert.AreEqual(700, delta);
            Assert.AreEqual(1700, bank.Balance);
        }

        [Test]
        public void Blackjack_AddsBetTimesOnePointFive()
        {
            var bank = new Bank(config);
            var player = HandOf((Suit.Spades, Rank.Ace), (Suit.Hearts, Rank.King)); // blackjack
            var dealer = HandOf((Suit.Clubs, Rank.Ten), (Suit.Diamonds, Rank.Nine)); // 19

            int delta = Settlement.Settle(bank, player, dealer, bet: 700, config);

            Assert.AreEqual(1050, delta);
            Assert.AreEqual(2050, bank.Balance);
        }

        [Test]
        public void Lose_SubtractsBet()
        {
            var bank = new Bank(config);
            var player = HandOf((Suit.Spades, Rank.Ten), (Suit.Hearts, Rank.Nine)); // 19
            var dealer = HandOf((Suit.Clubs, Rank.Ten), (Suit.Diamonds, Rank.King)); // 20

            int delta = Settlement.Settle(bank, player, dealer, bet: 700, config);

            Assert.AreEqual(-700, delta);
            Assert.AreEqual(300, bank.Balance);
        }

        [Test]
        public void Push_GoesToHouse_SubtractsBet()
        {
            var bank = new Bank(config);
            var player = HandOf((Suit.Spades, Rank.Ten), (Suit.Hearts, Rank.Nine)); // 19
            var dealer = HandOf((Suit.Clubs, Rank.Ten), (Suit.Diamonds, Rank.Nine)); // 19 (tie)

            int delta = Settlement.Settle(bank, player, dealer, bet: 700, config);

            Assert.AreEqual(-700, delta);
            Assert.AreEqual(300, bank.Balance);
        }

        [Test]
        public void Loss_NeverDrivesBalanceNegative()
        {
            var bank = new Bank(config);
            bank.Withdraw(600); // balance = 400, below the bet
            var player = HandOf((Suit.Spades, Rank.Ten), (Suit.Hearts, Rank.Nine)); // 19
            var dealer = HandOf((Suit.Clubs, Rank.Ten), (Suit.Diamonds, Rank.King)); // 20

            Settlement.Settle(bank, player, dealer, bet: 700, config);

            Assert.AreEqual(0, bank.Balance);
        }
    }
}
