using System;
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
    }
}
