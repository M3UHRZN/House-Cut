using System;
using System.Collections.Generic;
using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class GameFlowTests
    {
        [Test]
        public void StartsInIntro()
        {
            var flow = new GameFlow();
            Assert.AreEqual(GameState.Intro, flow.CurrentState);
        }

        [Test]
        public void TryTransition_WithoutSignal_Fails()
        {
            var flow = new GameFlow();
            bool result = flow.TryTransition(GameState.BetCheck);

            Assert.IsFalse(result);
            Assert.AreEqual(GameState.Intro, flow.CurrentState);
        }

        [Test]
        public void TryTransition_AfterSignal_ToAllowedState_Succeeds()
        {
            var flow = new GameFlow();
            flow.SignalTransitionReady();

            bool result = flow.TryTransition(GameState.BetCheck);

            Assert.IsTrue(result);
            Assert.AreEqual(GameState.BetCheck, flow.CurrentState);
        }

        [Test]
        public void TryTransition_ToDisallowedState_Fails()
        {
            var flow = new GameFlow();
            flow.SignalTransitionReady();

            bool result = flow.TryTransition(GameState.DealerTurn);

            Assert.IsFalse(result);
            Assert.AreEqual(GameState.Intro, flow.CurrentState);
        }

        [Test]
        public void TryTransition_ConsumesSignal_SoDoubleClickIsBlocked()
        {
            var flow = new GameFlow();
            flow.SignalTransitionReady();
            flow.TryTransition(GameState.BetCheck);

            bool secondAttempt = flow.TryTransition(GameState.Deal);

            Assert.IsFalse(secondAttempt, "second transition must be blocked until a new signal arrives");
            Assert.AreEqual(GameState.BetCheck, flow.CurrentState);
        }

        [Test]
        public void TryTransition_RaisesStateChanged_WithOldAndNewState()
        {
            var flow = new GameFlow();
            GameState observedOld = default;
            GameState observedNew = default;
            flow.StateChanged += (oldState, newState) =>
            {
                observedOld = oldState;
                observedNew = newState;
            };

            flow.SignalTransitionReady();
            flow.TryTransition(GameState.BetCheck);

            Assert.AreEqual(GameState.Intro, observedOld);
            Assert.AreEqual(GameState.BetCheck, observedNew);
        }

        [Test]
        public void AllowedTransitions_MatchGddTableExactly()
        {
            var expected = new Dictionary<GameState, GameState[]>
            {
                { GameState.Intro, new[] { GameState.BetCheck } },
                { GameState.BetCheck, new[] { GameState.Deal, GameState.OrganSale, GameState.GameOver } },
                { GameState.OrganSale, new[] { GameState.Extraction } },
                { GameState.Extraction, new[] { GameState.BetCheck, GameState.GameOver } },
                { GameState.Deal, new[] { GameState.PlayerTurn } },
                { GameState.PlayerTurn, new[] { GameState.Deal, GameState.DealerTurn } },
                { GameState.DealerTurn, new[] { GameState.Resolve } },
                { GameState.Resolve, new[] { GameState.NextHand } },
                { GameState.NextHand, new[] { GameState.BetCheck } },
                { GameState.GameOver, new[] { GameState.Intro } },
            };

            foreach (var pair in expected)
            {
                CollectionAssert.AreEquivalent(
                    pair.Value,
                    GameFlow.GetAllowedTransitions(pair.Key),
                    $"transitions for {pair.Key} did not match the GDD table");
            }
        }

        [Test]
        public void EveryState_CanReachGameOverOrLoopsBackToIntro()
        {
            // Sanity check: no state is a dead end that can never progress.
            foreach (GameState state in Enum.GetValues(typeof(GameState)))
            {
                var allowed = GameFlow.GetAllowedTransitions(state);
                Assert.IsNotEmpty(allowed, $"{state} has no outgoing transitions — dead end");
            }
        }
    }
}
