using Game.Data;
using Game.Organs;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    /// <summary>
    /// Lung timer effect (GDD §05): debuff lowers decision time (15→10s once
    /// sold), buff adds +5s every 3rd hand, combined with GameConfig's base.
    /// </summary>
    public class LungTimerEffectTests
    {
        private OrganData lung;

        [SetUp]
        public void SetUp()
        {
            lung = ScriptableObject.CreateInstance<OrganData>();
            lung.id = "lung";
            lung.debuffType = OrganDebuffType.ReducedDecisionTime;
            lung.debuffStrength = 5f;
            lung.buffType = OrganBuffType.ExtraDecisionTime;
            lung.buffStrength = 5f;
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(lung);
        }

        [Test]
        public void NoOrganSold_ReturnsBaseTimeUnchanged()
        {
            var effect = new LungTimerEffect();

            Assert.AreEqual(15f, effect.GetDecisionTime(15f));
        }

        [Test]
        public void LungSold_LowersDecisionTime()
        {
            var effect = new LungTimerEffect();
            effect.AddOrgan(lung);

            Assert.AreEqual(10f, effect.GetDecisionTime(15f));
        }

        [Test]
        public void EveryThirdHand_AddsBuffBonus()
        {
            var effect = new LungTimerEffect();
            effect.AddOrgan(lung);

            effect.AdvanceToNextHand(); // hand 2
            effect.AdvanceToNextHand(); // hand 3

            Assert.AreEqual(15f, effect.GetDecisionTime(15f)); // 15 - 5 + 5
        }

        [Test]
        public void NonThirdHand_NoBuffBonus()
        {
            var effect = new LungTimerEffect();
            effect.AddOrgan(lung);

            effect.AdvanceToNextHand(); // hand 2

            Assert.AreEqual(10f, effect.GetDecisionTime(15f)); // 15 - 5, no bonus
        }

        [Test]
        public void SixthHand_BuffBonusRetriggers()
        {
            var effect = new LungTimerEffect();
            effect.AddOrgan(lung);

            for (int i = 0; i < 5; i++)
            {
                effect.AdvanceToNextHand(); // hand 6
            }

            Assert.AreEqual(15f, effect.GetDecisionTime(15f));
        }

        [Test]
        public void NeverGoesBelowZero()
        {
            var effect = new LungTimerEffect();
            effect.AddOrgan(lung);

            Assert.AreEqual(0f, effect.GetDecisionTime(3f)); // 3 - 5 would be negative
        }

        [Test]
        public void IgnoresUnrelatedOrganEffects()
        {
            var other = ScriptableObject.CreateInstance<OrganData>();
            other.id = "ear";
            other.debuffType = OrganDebuffType.MonoAudio;
            other.buffType = OrganBuffType.DealerCardHint;

            var effect = new LungTimerEffect();
            effect.AddOrgan(other);

            Assert.AreEqual(15f, effect.GetDecisionTime(15f));

            Object.DestroyImmediate(other);
        }
    }
}
