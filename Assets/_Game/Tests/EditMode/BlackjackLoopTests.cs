using System;
using System.Linq;
using Game.Cards;
using Game.Core;
using Game.Data;
using Game.Economy;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class BlackjackLoopTests
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
            UnityEngine.Object.DestroyImmediate(config);
        }

        [Test]
        public void ConfirmBet_DealsTwoCardsToEachHand_AndStartsPlayerTurn()
        {
            Type loopType = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType("Game.Core.BlackjackLoop"))
                .FirstOrDefault(type => type != null);

            Assert.That(loopType, Is.Not.Null);

            var flow = new GameFlow();
            var economy = new EconomyController(config);
            economy.BeginHand();

            dynamic loop = Activator.CreateInstance(
                loopType,
                new object[] { flow, economy, new Deck(seed: 42), config.decisionTime });

            loop.Begin();

            Assert.That(flow.CurrentState, Is.EqualTo(GameState.BetCheck));
            Assert.That((bool)loop.ConfirmBet(), Is.True);
            Assert.That(flow.CurrentState, Is.EqualTo(GameState.PlayerTurn));
            Assert.That(((Hand)loop.PlayerHand).Cards.Count, Is.EqualTo(2));
            Assert.That(((Hand)loop.DealerHand).Cards.Count, Is.EqualTo(2));
        }

        [Test]
        public void AdvanceToNextHand_RampsBaseBet_ViaSingleConductor()
        {
            var economy = new EconomyController(config);
            var loop = new BlackjackLoop(new GameFlow(), economy, new Deck(seed: 42), config.decisionTime);

            loop.Begin();
            Assert.AreEqual(700, economy.CurrentBaseBet);

            Assert.IsTrue(loop.ConfirmBet());
            loop.Stand();
            Assert.AreEqual(GameState.Resolve, loop.State);

            Assert.IsTrue(loop.AdvanceToNextHand());
            Assert.AreEqual(1000, economy.CurrentBaseBet);
        }

        [Test]
        public void RaiseBet_ThenConfirm_LocksRaisedBet()
        {
            var economy = new EconomyController(config);
            var loop = new BlackjackLoop(new GameFlow(), economy, new Deck(seed: 42), config.decisionTime);

            loop.Begin();
            loop.RaiseBet();   // 700 -> 750
            loop.RaiseBet();   // 750 -> 800
            Assert.AreEqual(800, loop.CurrentBet);

            Assert.IsTrue(loop.ConfirmBet());
            Assert.AreEqual(800, economy.LockedBet);
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

        private static Hand DealerNineteen() =>
            HandOf((Suit.Clubs, Rank.Ten), (Suit.Diamonds, Rank.Nine));
    }
}
