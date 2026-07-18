using Game.Data;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class GameConfigTests
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
        public void Defaults_MatchGddSpec()
        {
            Assert.AreEqual(1000, config.startMoney);
            Assert.AreEqual(700, config.minBet);
            Assert.AreEqual(50, config.betStep);
            Assert.AreEqual(300, config.betRamp);
            Assert.AreEqual(15f, config.decisionTime);
            Assert.AreEqual(1.5f, config.blackjackPayout);
        }

        [Test]
        public void Fields_AreEditableAtRuntime()
        {
            config.startMoney = 2500;
            config.minBet = 900;

            Assert.AreEqual(2500, config.startMoney);
            Assert.AreEqual(900, config.minBet);
        }
    }
}
