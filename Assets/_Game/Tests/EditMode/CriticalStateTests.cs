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
    /// Kritik Hâl (GDD §04): triggers when money is below the min-bet threshold
    /// AND 2 or fewer sellable organs remain (Heart excluded). Spleen's buff
    /// lowers the money threshold (harder to trigger) once sold.
    /// </summary>
    public class CriticalStateTests
    {
        private const int MinBet = 700;

        private readonly List<OrganData> created = new();
        private GameConfig config;
        private Bank bank;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<GameConfig>();
            config.startMoney = 0;
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

        private OrganData Organ(string id, float buffStrength = 0f)
        {
            var organ = ScriptableObject.CreateInstance<OrganData>();
            organ.id = id;
            organ.dollarValue = 50;
            organ.buffStrength = buffStrength;
            created.Add(organ);
            return organ;
        }

        private List<OrganData> NineOrgans() =>
            Enumerable.Range(0, 8).Select(i => Organ($"organ_{i}")).Append(Organ("heart")).ToList();

        [Test]
        public void NotCritical_WhenMoneyCoversMinBet()
        {
            var organs = NineOrgans();
            var manager = new OrganManager(organs, bank);
            bank.Deposit(MinBet);

            Assert.IsFalse(manager.IsCriticalState(MinBet));
        }

        [Test]
        public void NotCritical_WhenMoneyShort_ButManyOrgansRemain()
        {
            var organs = NineOrgans();
            var manager = new OrganManager(organs, bank);
            // money stays 0 (below MinBet), but all 8 non-heart organs are unsold

            Assert.IsFalse(manager.IsCriticalState(MinBet));
        }

        [Test]
        public void Critical_WhenMoneyShort_AndOnlyTwoSellableOrgansRemain()
        {
            var organs = NineOrgans();
            var manager = new OrganManager(organs, bank);

            foreach (var organ in organs.Where(o => o.id != "heart").Take(6))
            {
                manager.Sell(organ); // 6 sold, 2 non-heart organs remain
            }

            Assert.IsTrue(manager.IsCriticalState(MinBet));
        }

        [Test]
        public void HeartNeverCountsTowardSellableOrgans()
        {
            // Only the Heart left unsold besides one other organ -> 1 sellable remains
            var organs = NineOrgans();
            var manager = new OrganManager(organs, bank);

            foreach (var organ in organs.Where(o => o.id != "heart").Take(7))
            {
                manager.Sell(organ);
            }

            Assert.IsTrue(manager.IsCriticalState(MinBet));
        }

        [Test]
        public void SpleenBuff_LowersMoneyThreshold()
        {
            var spleen = Organ("spleen", buffStrength: 0.5f);
            var organs = new List<OrganData> { spleen, Organ("heart") };
            var manager = new OrganManager(organs, bank);

            manager.Sell(spleen);
            bank.Deposit(400); // now at 400: below plain MinBet(700), above 0.5x threshold (350)

            Assert.IsFalse(manager.IsCriticalState(MinBet));
        }

        [Test]
        public void SpleenBuff_StillTriggersBelowLoweredThreshold()
        {
            var spleen = Organ("spleen", buffStrength: 0.5f);
            var organs = new List<OrganData> { spleen, Organ("heart") };
            var manager = new OrganManager(organs, bank);

            manager.Sell(spleen);
            bank.Deposit(300); // below the lowered 0.5x350 threshold, 0 sellable organs remain

            Assert.IsTrue(manager.IsCriticalState(MinBet));
        }
    }
}
