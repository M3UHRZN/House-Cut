using System;
using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Game.Data;

namespace Game.Tests.EditMode
{
    public class OrganDataTests
    {
        [Test]
        public void OrganData_ExposesTheGddSchema()
        {
            Type organDataType = AppDomain.CurrentDomain
                .GetAssemblies()
                .Select(assembly => assembly.GetType("Game.Data.OrganData"))
                .FirstOrDefault(type => type != null);

            Assert.That(organDataType, Is.Not.Null);
            Assert.That(typeof(ScriptableObject).IsAssignableFrom(organDataType), Is.True);

            string[] expectedFields =
            {
                "id", "displayName", "dollarValue", "quantity",
                "debuffType", "debuffStrength", "buffType", "buffStrength",
                "icon", "extractionSfx", "postProcessProfile", "extractionPrefab"
            };

            CollectionAssert.IsSubsetOf(
                expectedFields,
                organDataType.GetFields().Select(field => field.Name).ToArray());
        }

        [Test]
        public void OrganAssets_MatchTheGddMatrix()
        {
            // Spleen's buffStrength is 0.5, not the GDD-transcription placeholder 1
            // every other not-yet-quantified buff uses: the GDD only describes
            // LowerCriticalThreshold in prose ("Kritik Hâl eşiği düşer"), no exact
            // number, but unlike Ear/Stomach/LeftEye's buffs this one IS wired up
            // (OrganManager.IsCriticalState reads buffStrength as the threshold
            // multiplier) — leaving it at 1 made the buff a no-op (multiplier
            // unchanged from the no-Spleen default of 1). 0.5 halves the money
            // trigger threshold, matching the buff's "more resistant" description.
            var expectedOrgans = new[]
            {
                ("Ear", "ear", "Ear", 1000, OrganDebuffType.MonoAudio, 1f, OrganBuffType.DealerCardHint, 1f),
                ("Spleen", "spleen", "Spleen", 1000, OrganDebuffType.WorseSaleRate, 300f, OrganBuffType.LowerCriticalThreshold, 0.5f),
                ("KidneyI", "kidney_i", "Kidney I", 1000, OrganDebuffType.MinorVignette, 1f, OrganBuffType.BetterSaleRate, 0.2f),
                ("KidneyII", "kidney_ii", "Kidney II", 1000, OrganDebuffType.CashBleedPerHand, 400f, OrganBuffType.SlowerBetRamp, 2f),
                ("Stomach", "stomach", "Stomach", 2000, OrganDebuffType.NauseaOnHit, 0.6f, OrganBuffType.ShowHandDistance, 1f),
                ("LeftEye", "left_eye", "Left Eye", 2000, OrganDebuffType.LeftEyeMask, 0.3f, OrganBuffType.PeekHoleCard, 1f),
                ("Lung", "lung", "Lung", 3000, OrganDebuffType.ReducedDecisionTime, 5f, OrganBuffType.ExtraDecisionTime, 5f),
                ("Liver", "liver", "Liver", 3000, OrganDebuffType.CashBleedPerHand, 600f, OrganBuffType.CashRegenOnWin, 800f),
                ("Heart", "heart", "Heart", 5000, OrganDebuffType.HeartEnd, 1f, OrganBuffType.None, 0f)
            };

            foreach (var expected in expectedOrgans)
            {
                OrganData organ = AssetDatabase.LoadAssetAtPath<OrganData>(
                    $"Assets/_Game/Data/Organs/{expected.Item1}.asset");

                Assert.That(organ, Is.Not.Null, $"Missing {expected.Item1} asset.");
                Assert.That(organ.id, Is.EqualTo(expected.Item2));
                Assert.That(organ.displayName, Is.EqualTo(expected.Item3));
                Assert.That(organ.dollarValue, Is.EqualTo(expected.Item4));
                Assert.That(organ.quantity, Is.EqualTo(1));
                Assert.That(organ.debuffType, Is.EqualTo(expected.Item5));
                Assert.That(organ.debuffStrength, Is.EqualTo(expected.Item6));
                Assert.That(organ.buffType, Is.EqualTo(expected.Item7));
                Assert.That(organ.buffStrength, Is.EqualTo(expected.Item8));
            }
        }
    }
}
