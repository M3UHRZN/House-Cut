using Game.Data;
using Game.Economy;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class BankTests
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
        public void StartingBalance_IsOneThousandFromConfig()
        {
            var bank = new Bank(config);

            Assert.AreEqual(1000, bank.Balance);
        }

        [Test]
        public void StartingBalance_FollowsConfigStartMoney()
        {
            config.startMoney = 2500;

            var bank = new Bank(config);

            Assert.AreEqual(2500, bank.Balance);
        }

        [Test]
        public void Deposit_IncreasesBalance()
        {
            var bank = new Bank(config);

            bank.Deposit(700);

            Assert.AreEqual(1700, bank.Balance);
        }

        [Test]
        public void Withdraw_DecreasesBalance()
        {
            var bank = new Bank(config);

            bank.Withdraw(300);

            Assert.AreEqual(700, bank.Balance);
        }

        [Test]
        public void Withdraw_NeverGoesNegative()
        {
            var bank = new Bank(config);

            bank.Withdraw(5000);

            Assert.AreEqual(0, bank.Balance);
        }

        [Test]
        public void Apply_NegativeDelta_ClampsAtZero()
        {
            var bank = new Bank(config);

            bank.Apply(-5000);

            Assert.AreEqual(0, bank.Balance);
        }

        [Test]
        public void Apply_PositiveDelta_AddsMoney()
        {
            var bank = new Bank(config);

            bank.Apply(1050);

            Assert.AreEqual(2050, bank.Balance);
        }
    }
}
