using System;
using System.Linq;
using Game.Data;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class OrganManagerTests
    {
        private OrganData ear;
        private OrganData spleen;
        private OrganData kidneyI;
        private OrganData liver;

        [SetUp]
        public void SetUp()
        {
            ear = CreateOrgan("ear", 1000);
            spleen = CreateOrgan("spleen", 1000, OrganDebuffType.WorseSaleRate);
            kidneyI = CreateOrgan("kidney_i", 1000, OrganDebuffType.MinorVignette, OrganBuffType.BetterSaleRate);
            liver = CreateOrgan("liver", 3000);
        }

        [TearDown]
        public void TearDown()
        {
            UnityEngine.Object.DestroyImmediate(ear);
            UnityEngine.Object.DestroyImmediate(spleen);
            UnityEngine.Object.DestroyImmediate(kidneyI);
            UnityEngine.Object.DestroyImmediate(liver);
        }

        [Test]
        public void Sell_AppliesPersistentPriceModifiers_AndPreventsResale()
        {
            Type managerType = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType("Game.Organs.OrganManager"))
                .FirstOrDefault(type => type != null);

            Assert.That(managerType, Is.Not.Null);

            dynamic manager = Activator.CreateInstance(
                managerType,
                new object[] { new[] { ear, spleen, kidneyI, liver } });

            Assert.That((int)manager.Sell(spleen), Is.EqualTo(1000));
            Assert.That((int)manager.Sell(kidneyI), Is.EqualTo(700));
            Assert.That((int)manager.Sell(liver), Is.EqualTo(3300));
            Assert.That((int)manager.Sell(liver), Is.EqualTo(0));
            Assert.That((bool)manager.IsSold(liver), Is.True);
        }

        private static OrganData CreateOrgan(
            string id,
            int dollarValue,
            OrganDebuffType debuffType = OrganDebuffType.None,
            OrganBuffType buffType = OrganBuffType.None)
        {
            var organ = ScriptableObject.CreateInstance<OrganData>();
            organ.id = id;
            organ.dollarValue = dollarValue;
            organ.debuffType = debuffType;
            organ.buffType = buffType;
            return organ;
        }
    }
}
