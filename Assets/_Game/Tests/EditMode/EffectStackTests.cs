using System.Collections.Generic;
using Game.Cards;
using Game.Data;
using Game.Economy;
using Game.Organs;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// EffectStack (GDD §08): sold organs' money-affecting permanent effects —
    /// CashBleedPerHand and CashRegenOnWin — dispatched by enum on every
    /// end-of-hand tick, and stacking when multiple such organs are sold.
    /// </summary>
    public class EffectStackTests
    {
        private readonly List<OrganData> created = new();
        private GameConfig config;
        private Bank bank;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<GameConfig>();
            config.startMoney = 5000;
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

        private OrganData Organ(
            string id,
            OrganDebuffType debuffType = OrganDebuffType.None,
            float debuffStrength = 0f,
            OrganBuffType buffType = OrganBuffType.None,
            float buffStrength = 0f)
        {
            var organ = ScriptableObject.CreateInstance<OrganData>();
            organ.id = id;
            organ.debuffType = debuffType;
            organ.debuffStrength = debuffStrength;
            organ.buffType = buffType;
            organ.buffStrength = buffStrength;
            created.Add(organ);
            return organ;
        }

        [Test]
        public void AddOrgan_AppendsToStack()
        {
            var effects = new EffectStack(bank);
            var kidneyII = Organ("kidney_ii", OrganDebuffType.CashBleedPerHand, 400f);

            effects.AddOrgan(kidneyII);

            CollectionAssert.AreEqual(new[] { kidneyII }, effects.Stack);
        }

        [Test]
        public void TickHandEnd_AppliesCashBleed_RegardlessOfOutcome()
        {
            var effects = new EffectStack(bank);
            effects.AddOrgan(Organ("kidney_ii", OrganDebuffType.CashBleedPerHand, 400f));

            effects.TickHandEnd(HandOutcome.DealerWins);

            Assert.AreEqual(4600, bank.Balance);
        }

        [Test]
        public void TickHandEnd_AppliesCashRegen_OnlyOnPlayerWin()
        {
            var effects = new EffectStack(bank);
            effects.AddOrgan(Organ("liver", buffType: OrganBuffType.CashRegenOnWin, buffStrength: 800f));

            effects.TickHandEnd(HandOutcome.DealerWins);
            Assert.AreEqual(5000, bank.Balance, "no regen on a loss");

            effects.TickHandEnd(HandOutcome.PlayerWins);
            Assert.AreEqual(5800, bank.Balance);
        }

        [Test]
        public void TickHandEnd_StacksMultipleBleedingOrgans()
        {
            var effects = new EffectStack(bank);
            effects.AddOrgan(Organ("kidney_ii", OrganDebuffType.CashBleedPerHand, 400f));
            effects.AddOrgan(Organ("liver", OrganDebuffType.CashBleedPerHand, 600f, OrganBuffType.CashRegenOnWin, 800f));

            effects.TickHandEnd(HandOutcome.DealerWins);

            Assert.AreEqual(4000, bank.Balance); // 5000 - 400 - 600
        }

        [Test]
        public void TickHandEnd_LiverOnWin_BleedsAndRegensInSameTick()
        {
            var effects = new EffectStack(bank);
            effects.AddOrgan(Organ("liver", OrganDebuffType.CashBleedPerHand, 600f, OrganBuffType.CashRegenOnWin, 800f));

            effects.TickHandEnd(HandOutcome.PlayerWins);

            Assert.AreEqual(5200, bank.Balance); // 5000 - 600 + 800
        }

        [Test]
        public void TickHandEnd_IgnoresNonMoneyEffects()
        {
            var effects = new EffectStack(bank);
            effects.AddOrgan(Organ("ear", OrganDebuffType.MonoAudio, 1f, OrganBuffType.DealerCardHint, 1f));

            effects.TickHandEnd(HandOutcome.PlayerWins);

            Assert.AreEqual(5000, bank.Balance);
        }

        [Test]
        public void TickHandEnd_BleedClampsBankAtZero()
        {
            config.startMoney = 200;
            bank = new Bank(config);
            var effects = new EffectStack(bank);
            effects.AddOrgan(Organ("liver", OrganDebuffType.CashBleedPerHand, 600f));

            effects.TickHandEnd(HandOutcome.DealerWins);

            Assert.AreEqual(0, bank.Balance);
        }

        [Test]
        public void OrganManager_Sell_RaisesOrganSold_ForEffectStackToPickUp()
        {
            var kidneyII = Organ("kidney_ii", OrganDebuffType.CashBleedPerHand, 400f);
            kidneyII.dollarValue = 1000;
            var manager = new OrganManager(new[] { kidneyII }, bank);
            var effects = new EffectStack(bank);
            manager.OrganSold += effects.AddOrgan;

            manager.Sell(kidneyII);

            CollectionAssert.AreEqual(new[] { kidneyII }, effects.Stack);
        }
    }
}
