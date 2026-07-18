using System.Collections.Generic;
using System.Linq;
using Game.Data;
using Game.Economy;
using Game.Organs;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Heart safety / soft-lock prevention (GDD §04): the Heart auto-unlocks only
    /// when money is below the min bet and every other organ is sold; giving it is
    /// the final $5.000 payment and ends the run.
    /// </summary>
    public class HeartSafetyTests
    {
        private const int MinBet = 700;

        private readonly List<OrganData> created = new();
        private GameConfig config;
        private Bank bank;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<GameConfig>();
            config.startMoney = 0;   // broke: below the min bet
            bank = new Bank(config);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var organ in created)
            {
                UnityEngine.Object.DestroyImmediate(organ);
            }

            created.Clear();
            UnityEngine.Object.DestroyImmediate(config);
        }

        private OrganData Organ(string id, int dollarValue)
        {
            var organ = ScriptableObject.CreateInstance<OrganData>();
            organ.id = id;
            organ.dollarValue = dollarValue;
            created.Add(organ);
            return organ;
        }

        private OrganData Heart() => Organ("heart", 5000);

        private List<OrganData> EightOthers() =>
            Enumerable.Range(0, 8).Select(i => Organ($"organ_{i}", 50)).ToList();

        [Test]
        public void EightOrgansSold_MoneyShort_HeartUnlocks_ThenGameOver()
        {
            var heart = Heart();
            var others = EightOthers();
            var manager = new OrganManager(new List<OrganData>(others) { heart }, bank);

            foreach (var organ in others)
            {
                manager.Sell(organ);
            }

            Assert.Less(bank.Balance, MinBet);                 // still can't cover the bet
            Assert.IsTrue(manager.IsHeartUnlocked(MinBet));    // only heart left -> unlocks

            int payout = manager.SellHeart(MinBet);

            Assert.AreEqual(5000, payout);                     // fixed final payment
            Assert.AreEqual(5000 + 8 * 50, bank.Balance);      // deposited into the bank
            Assert.IsTrue(manager.IsGameOver);                 // run ends
            Assert.IsTrue(manager.IsSold(heart));
        }

        [Test]
        public void SellHeart_RaisesHeartExtracted()
        {
            var heart = Heart();
            var manager = new OrganManager(new List<OrganData> { heart }, bank);

            bool raised = false;
            manager.HeartExtracted += () => raised = true;

            Assert.IsTrue(manager.IsHeartUnlocked(MinBet));
            manager.SellHeart(MinBet);

            Assert.IsTrue(raised);
        }

        [Test]
        public void Heart_StaysLocked_WhenMoneyCoversMinBet()
        {
            var heart = Heart();
            var others = EightOthers();
            var manager = new OrganManager(new List<OrganData>(others) { heart }, bank);

            foreach (var organ in others)
            {
                manager.Sell(organ);
            }

            bank.Deposit(MinBet);   // money now covers the bet -> no fallback needed

            Assert.IsFalse(manager.IsHeartUnlocked(MinBet));
            Assert.AreEqual(0, manager.SellHeart(MinBet));
            Assert.IsFalse(manager.IsGameOver);
        }

        [Test]
        public void Heart_StaysLocked_WhileOtherOrgansRemain()
        {
            var heart = Heart();
            var others = EightOthers();
            var manager = new OrganManager(new List<OrganData>(others) { heart }, bank);

            for (int i = 0; i < 7; i++)   // one organ still unsold
            {
                manager.Sell(others[i]);
            }

            Assert.IsFalse(manager.IsHeartUnlocked(MinBet));
            Assert.AreEqual(0, manager.SellHeart(MinBet));
            Assert.IsFalse(manager.IsGameOver);
        }

        [Test]
        public void NormalSell_RefusesHeart()
        {
            var heart = Heart();
            var manager = new OrganManager(new List<OrganData> { heart }, bank);

            Assert.AreEqual(0, manager.Sell(heart));
            Assert.IsFalse(manager.IsSold(heart));
            Assert.IsFalse(manager.IsGameOver);
        }
    }
}
