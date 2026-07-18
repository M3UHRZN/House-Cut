using Game.Data;
using Game.Economy;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class DefermentTokenTests
    {
        [Test]
        public void StartsEmpty()
        {
            var token = new DefermentToken();

            Assert.IsFalse(token.Has);
        }

        [Test]
        public void Grant_AwardsToken()
        {
            var token = new DefermentToken();

            token.Grant();

            Assert.IsTrue(token.Has);
        }

        [Test]
        public void Grant_DoesNotStack()
        {
            var token = new DefermentToken();

            token.Grant();
            token.Grant();

            Assert.IsTrue(token.Has);
            Assert.IsTrue(token.Consume());  // first consume spends the only token
            Assert.IsFalse(token.Has);       // nothing left — did not stack to 2
            Assert.IsFalse(token.Consume());
        }

        [Test]
        public void Consume_ClearsTheToken()
        {
            var token = new DefermentToken();
            token.Grant();

            bool spent = token.Consume();

            Assert.IsTrue(spent);
            Assert.IsFalse(token.Has);
        }

        [Test]
        public void Consume_WhenEmpty_ReturnsFalse()
        {
            var token = new DefermentToken();

            Assert.IsFalse(token.Consume());
        }

        [Test]
        public void ConsumedToken_DefersNextRamp_ThenRampReturnsToNormal()
        {
            var config = ScriptableObject.CreateInstance<GameConfig>();
            try
            {
                var tracker = new BaseBetTracker(config);
                var token = new DefermentToken();
                token.Grant();

                // Player uses the Erteleme before the next hand.
                if (token.Consume())
                {
                    tracker.DeferNextRamp();
                }

                tracker.AdvanceHand();
                Assert.AreEqual(700, tracker.Current);   // next hand: base does not rise

                tracker.AdvanceHand();
                Assert.AreEqual(1000, tracker.Current);  // system returns to normal
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}
