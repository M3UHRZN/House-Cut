using Game.Data;
using Game.Economy;
using Game.Organs;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class OrganSaleControllerTests
    {
        private GameConfig config;
        private OrganData ear;
        private OrganData spleen;
        private OrganData kidneyI;
        private OrganData heart;
        private Bank bank;
        private OrganManager organManager;
        private OrganSaleController controller;

        [SetUp]
        public void SetUp()
        {
            config = ScriptableObject.CreateInstance<GameConfig>();
            config.startMoney = 100;

            ear = CreateOrgan("ear", 500);
            spleen = CreateOrgan("spleen", 1000, OrganDebuffType.WorseSaleRate, OrganBuffType.None, 300f);
            kidneyI = CreateOrgan("kidney_i", 1000, OrganDebuffType.MinorVignette, OrganBuffType.BetterSaleRate, 0f, 0.2f);
            heart = CreateOrgan("heart", 5000, OrganDebuffType.HeartEnd);

            bank = new Bank(config);
            organManager = new OrganManager(new[] { ear, spleen, kidneyI, heart }, bank);
            controller = new OrganSaleController(organManager, bank);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(config);
            Object.DestroyImmediate(ear);
            Object.DestroyImmediate(spleen);
            Object.DestroyImmediate(kidneyI);
            Object.DestroyImmediate(heart);
        }

        [Test]
        public void AvailableOrgans_HidesHeart_UntilItIsTheOnlyOneLeft()
        {
            CollectionAssert.AreEquivalent(new[] { ear, spleen, kidneyI }, controller.AvailableOrgans);

            organManager.Sell(ear);
            organManager.Sell(spleen);
            organManager.Sell(kidneyI);

            CollectionAssert.AreEquivalent(new[] { heart }, controller.AvailableOrgans);
        }

        [Test]
        public void ToggleSelection_AddsAndRemoves()
        {
            controller.ToggleSelection(ear);
            Assert.IsTrue(controller.IsSelected(ear));

            controller.ToggleSelection(ear);
            Assert.IsFalse(controller.IsSelected(ear));
        }

        [Test]
        public void ToggleSelection_HiddenHeart_IsNoOp()
        {
            controller.ToggleSelection(heart);

            Assert.IsFalse(controller.IsSelected(heart));
            Assert.AreEqual(0, controller.Selected.Count);
        }

        [Test]
        public void ToggleSelection_AlreadySoldOrgan_IsNoOp()
        {
            organManager.Sell(ear);

            controller.ToggleSelection(ear);

            Assert.IsFalse(controller.IsSelected(ear));
        }

        [Test]
        public void SelectedTotal_SumsCurrentSaleValueOfSelection()
        {
            controller.ToggleSelection(ear);
            controller.ToggleSelection(kidneyI);

            Assert.AreEqual(1500, controller.SelectedTotal);
        }

        [Test]
        public void CanConfirm_FalseWithNoSelection()
        {
            Assert.IsFalse(controller.CanConfirm(700));
        }

        [Test]
        public void CanConfirm_FalseBelowThreshold_TrueOnceCrossed()
        {
            // Bank = $100, required = $700.
            controller.ToggleSelection(ear);           // +500 -> projected $600
            Assert.IsFalse(controller.CanConfirm(700));

            controller.ToggleSelection(kidneyI);        // +1000 -> projected $1600
            Assert.IsTrue(controller.CanConfirm(700));
        }

        [Test]
        public void Confirm_SellsSelectionInOrder_AppliesModifiersSequentially()
        {
            // Spleen sold first at full price, then Kidney I's sale value
            // reflects Spleen's already-applied -$300 kur penalty.
            controller.ToggleSelection(spleen);
            controller.ToggleSelection(kidneyI);

            int total = controller.Confirm();

            Assert.AreEqual(1700, total);   // 1000 (spleen) + 700 (kidney I after -$300)
            Assert.AreEqual(1800, bank.Balance);   // 100 start + 1700
            Assert.IsTrue(organManager.IsSold(spleen));
            Assert.IsTrue(organManager.IsSold(kidneyI));
            Assert.AreEqual(0, controller.Selected.Count);
        }

        private static OrganData CreateOrgan(
            string id,
            int dollarValue,
            OrganDebuffType debuffType = OrganDebuffType.None,
            OrganBuffType buffType = OrganBuffType.None,
            float debuffStrength = 0f,
            float buffStrength = 0f)
        {
            var organ = ScriptableObject.CreateInstance<OrganData>();
            organ.id = id;
            organ.dollarValue = dollarValue;
            organ.debuffType = debuffType;
            organ.buffType = buffType;
            organ.debuffStrength = debuffStrength;
            organ.buffStrength = buffStrength;
            return organ;
        }
    }
}
