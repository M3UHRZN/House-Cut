using Game.Cards;
using Game.Data;
using Game.Economy;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class EconomyControllerTests
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

        private static Hand Blackjack() =>
            HandOf((Suit.Spades, Rank.Ace), (Suit.Hearts, Rank.King));

        private static Hand Total(int _, params (Suit suit, Rank rank)[] cardSpecs) =>
            HandOf(cardSpecs);

        private static Hand DealerNineteen() =>
            HandOf((Suit.Clubs, Rank.Ten), (Suit.Diamonds, Rank.Nine));

        private static Hand DealerTwenty() =>
            HandOf((Suit.Clubs, Rank.Ten), (Suit.Diamonds, Rank.King));

        [Test]
        public void StartingState_MatchesConfig()
        {
            var economy = new EconomyController(config);

            Assert.AreEqual(1000, economy.Balance);
            Assert.AreEqual(700, economy.CurrentBaseBet);
            Assert.IsFalse(economy.HasDeferment);
        }

        [Test]
        public void BeginHand_SelectorStartsAtBaseBet_FundsEnough()
        {
            var economy = new EconomyController(config);

            BetSelector selector = economy.BeginHand();

            Assert.AreEqual(700, selector.Current);
            Assert.AreEqual(700, economy.LockedBet);
            Assert.IsTrue(economy.HasFundsForBaseBet);
        }

        [Test]
        public void LockBet_SnapshotsSelectorAfterRaising()
        {
            var economy = new EconomyController(config);
            BetSelector selector = economy.BeginHand();

            selector.Increase();   // 750
            selector.Increase();   // 800
            economy.LockBet();

            Assert.AreEqual(800, economy.LockedBet);
        }

        [Test]
        public void SettleHand_Win_AddsLockedBet()
        {
            var economy = new EconomyController(config);
            economy.BeginHand();   // locked = 700

            int delta = economy.SettleHand(HandOf((Suit.Spades, Rank.Ten), (Suit.Hearts, Rank.King)), DealerNineteen());

            Assert.AreEqual(700, delta);
            Assert.AreEqual(1700, economy.Balance);
        }

        [Test]
        public void SettleHand_Blackjack_PaysOnePointFive_AndGrantsDeferment()
        {
            var economy = new EconomyController(config);
            economy.BeginHand();

            int delta = economy.SettleHand(Blackjack(), DealerNineteen());

            Assert.AreEqual(1050, delta);
            Assert.AreEqual(2050, economy.Balance);
            Assert.IsTrue(economy.HasDeferment);
        }

        [Test]
        public void SettleHand_Loss_SubtractsLockedBet_NeverNegative()
        {
            var economy = new EconomyController(config);
            economy.BeginHand();

            economy.SettleHand(HandOf((Suit.Spades, Rank.Ten), (Suit.Hearts, Rank.Nine)), DealerTwenty());

            Assert.AreEqual(300, economy.Balance);
            Assert.IsFalse(economy.HasDeferment);
        }

        [Test]
        public void AdvanceToNextHand_RampsBaseBet()
        {
            var economy = new EconomyController(config);

            economy.AdvanceToNextHand();
            economy.AdvanceToNextHand();

            Assert.AreEqual(1300, economy.CurrentBaseBet);  // hand 3
        }

        [Test]
        public void UseDeferment_SkipsNextRamp_ThenReturnsToNormal()
        {
            var economy = new EconomyController(config);
            economy.BeginHand();
            economy.SettleHand(Blackjack(), DealerNineteen());   // grants token

            economy.UseDeferment();
            economy.AdvanceToNextHand();
            Assert.AreEqual(700, economy.CurrentBaseBet);        // skipped
            Assert.IsFalse(economy.HasDeferment);                // consumed

            economy.AdvanceToNextHand();
            Assert.AreEqual(1000, economy.CurrentBaseBet);       // back to normal
        }

        [Test]
        public void UseDeferment_WithoutToken_DoesNothing()
        {
            var economy = new EconomyController(config);

            economy.UseDeferment();       // no token held
            economy.AdvanceToNextHand();

            Assert.AreEqual(1000, economy.CurrentBaseBet);  // ramp not skipped
        }

        [Test]
        public void FullHandCycle_WinThenRampUp()
        {
            var economy = new EconomyController(config);

            // Hand 1: bet base 700, win.
            economy.BeginHand();
            economy.LockBet();
            economy.SettleHand(Total(20, (Suit.Spades, Rank.Ten), (Suit.Hearts, Rank.King)), DealerNineteen());
            Assert.AreEqual(1700, economy.Balance);

            // Advance to hand 2.
            economy.AdvanceToNextHand();
            Assert.AreEqual(1000, economy.CurrentBaseBet);

            // Hand 2 selector now floors at the higher base bet.
            BetSelector selector = economy.BeginHand();
            Assert.AreEqual(1000, selector.Current);
        }
    }
}
