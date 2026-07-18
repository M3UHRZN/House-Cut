using Game.Data;
using Game.Economy;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class BetSelectorTests
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

        [Test]
        public void StartsAtBaseBet()
        {
            var selector = new BetSelector(config, baseBet: 700, availableMoney: 1000);

            Assert.AreEqual(700, selector.Current);
        }

        [Test]
        public void Increase_AddsFifty()
        {
            var selector = new BetSelector(config, baseBet: 700, availableMoney: 1000);

            selector.Increase();

            Assert.AreEqual(750, selector.Current);
        }

        [Test]
        public void Decrease_SubtractsFifty()
        {
            var selector = new BetSelector(config, baseBet: 700, availableMoney: 1000);

            selector.Increase();
            selector.Increase();
            selector.Decrease();

            Assert.AreEqual(750, selector.Current);
        }

        [Test]
        public void Decrease_NeverGoesBelowBaseBet()
        {
            var selector = new BetSelector(config, baseBet: 700, availableMoney: 1000);

            selector.Decrease();
            selector.Decrease();

            Assert.AreEqual(700, selector.Current);
        }

        [Test]
        public void Increase_NeverExceedsAvailableMoney()
        {
            var selector = new BetSelector(config, baseBet: 700, availableMoney: 800);

            selector.Increase();   // 750
            selector.Increase();   // would be 800
            selector.Increase();   // would be 850 -> capped at 800

            Assert.AreEqual(800, selector.Current);
        }
    }
}
