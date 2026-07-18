using Game.Cards;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class PlayerTurnControllerTests
    {
        [Test]
        public void Hit_AddsCardToHand()
        {
            var hand = new Hand();
            var controller = new PlayerTurnController(hand, decisionTime: 15f);

            controller.Hit(new Card(Suit.Spades, Rank.Five));

            Assert.AreEqual(1, hand.Cards.Count);
            Assert.IsTrue(controller.IsAwaitingDecision);
        }

        [Test]
        public void Hit_ResetsDecisionTimer()
        {
            var hand = new Hand();
            var controller = new PlayerTurnController(hand, decisionTime: 15f);

            controller.Tick(10f);
            Assert.AreEqual(5f, controller.RemainingTime, 0.001f);

            controller.Hit(new Card(Suit.Spades, Rank.Two));

            Assert.AreEqual(15f, controller.RemainingTime, 0.001f);
        }

        [Test]
        public void Tick_TimerExpires_AutoStands()
        {
            var hand = new Hand();
            var controller = new PlayerTurnController(hand, decisionTime: 15f);
            PlayerTurnOutcome? outcome = null;
            controller.TurnEnded += o => outcome = o;

            controller.Tick(15f);

            Assert.AreEqual(PlayerTurnOutcome.Stand, outcome);
            Assert.IsFalse(controller.IsAwaitingDecision);
        }

        [Test]
        public void Tick_BeforeTimerExpires_StaysAwaitingDecision()
        {
            var hand = new Hand();
            var controller = new PlayerTurnController(hand, decisionTime: 15f);

            controller.Tick(14.9f);

            Assert.IsTrue(controller.IsAwaitingDecision);
        }

        [Test]
        public void Hit_OverTwentyOne_EndsTurnAsBustImmediately()
        {
            var hand = new Hand();
            hand.AddCard(new Card(Suit.Spades, Rank.Ten));
            hand.AddCard(new Card(Suit.Hearts, Rank.Ten));
            var controller = new PlayerTurnController(hand, decisionTime: 15f);
            PlayerTurnOutcome? outcome = null;
            controller.TurnEnded += o => outcome = o;

            controller.Hit(new Card(Suit.Clubs, Rank.Two));

            Assert.AreEqual(PlayerTurnOutcome.Bust, outcome);
            Assert.IsFalse(controller.IsAwaitingDecision);
        }

        [Test]
        public void Stand_EndsTurnWithStandOutcome()
        {
            var hand = new Hand();
            var controller = new PlayerTurnController(hand, decisionTime: 15f);
            PlayerTurnOutcome? outcome = null;
            controller.TurnEnded += o => outcome = o;

            controller.Stand();

            Assert.AreEqual(PlayerTurnOutcome.Stand, outcome);
            Assert.IsFalse(controller.IsAwaitingDecision);
        }

        [Test]
        public void AfterTurnEnded_FurtherHitsAndTicksAreIgnored()
        {
            var hand = new Hand();
            var controller = new PlayerTurnController(hand, decisionTime: 15f);
            controller.Stand();

            controller.Hit(new Card(Suit.Spades, Rank.King));
            controller.Tick(100f);

            Assert.AreEqual(0, hand.Cards.Count);
            Assert.IsFalse(controller.IsAwaitingDecision);
        }
    }
}
