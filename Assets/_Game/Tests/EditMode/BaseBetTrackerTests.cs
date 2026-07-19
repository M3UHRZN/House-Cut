using Game.Data;
using Game.Economy;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.EditMode
{
    public class BaseBetTrackerTests
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

        private static int BaseBetAtHand(BaseBetTracker tracker, int handNumber)
        {
            for (int hand = 1; hand < handNumber; hand++)
            {
                tracker.AdvanceHand();
            }

            return tracker.Current;
        }

        [Test]
        public void Hand1_Is700()
        {
            var tracker = new BaseBetTracker(config);

            Assert.AreEqual(700, BaseBetAtHand(tracker, 1));
        }

        [Test]
        public void Hand3_Is1300()
        {
            var tracker = new BaseBetTracker(config);

            Assert.AreEqual(1300, BaseBetAtHand(tracker, 3));
        }

        [Test]
        public void Hand6_Is2200()
        {
            var tracker = new BaseBetTracker(config);

            Assert.AreEqual(2200, BaseBetAtHand(tracker, 6));
        }

        [Test]
        public void KidneyTwoBuff_RaisesEveryTwoHands()
        {
            var tracker = new BaseBetTracker(config, slowerRamp: true);

            Assert.AreEqual(700, tracker.Current);   // hand 1
            tracker.AdvanceHand();
            Assert.AreEqual(700, tracker.Current);   // hand 2 — no rise
            tracker.AdvanceHand();
            Assert.AreEqual(1000, tracker.Current);  // hand 3 — rises
            tracker.AdvanceHand();
            Assert.AreEqual(1000, tracker.Current);  // hand 4 — no rise
            tracker.AdvanceHand();
            Assert.AreEqual(1300, tracker.Current);  // hand 5 — rises
        }

        [Test]
        public void DeferNextRamp_SkipsOneIncrease_ThenResumes()
        {
            var tracker = new BaseBetTracker(config);

            tracker.DeferNextRamp();
            tracker.AdvanceHand();
            Assert.AreEqual(700, tracker.Current);   // increase skipped

            tracker.AdvanceHand();
            Assert.AreEqual(1000, tracker.Current);  // ramp resumes normally

            tracker.AdvanceHand();
            Assert.AreEqual(1300, tracker.Current);
        }

        [Test]
        public void DeferNextRamp_IsSingleUse()
        {
            var tracker = new BaseBetTracker(config);

            tracker.DeferNextRamp();
            tracker.AdvanceHand();   // consumes the defer
            tracker.AdvanceHand();   // normal rise, not skipped again

            Assert.AreEqual(1000, tracker.Current);
        }

        [Test]
        public void HandNumber_StartsAtOne_AndIncrementsEachHand()
        {
            var tracker = new BaseBetTracker(config);

            Assert.AreEqual(1, tracker.HandNumber);
            tracker.AdvanceHand();
            Assert.AreEqual(2, tracker.HandNumber);
            tracker.AdvanceHand();
            Assert.AreEqual(3, tracker.HandNumber);
        }

        [Test]
        public void HandNumber_AdvancesEvenWhenRampIsDeferredOrSlowed()
        {
            // El sayısı bahis rampasından bağımsızdır: erteleme bahsi yükseltmese de
            // el ilerler.
            var deferred = new BaseBetTracker(config);
            deferred.DeferNextRamp();
            deferred.AdvanceHand();
            Assert.AreEqual(700, deferred.Current);   // bahis sabit
            Assert.AreEqual(2, deferred.HandNumber);  // el yine ilerledi

            // Böbrek II (slowerRamp): bahis her 2 elde bir yükselse de el sayısı her el artar.
            var slowed = new BaseBetTracker(config, slowerRamp: true);
            slowed.AdvanceHand();
            slowed.AdvanceHand();
            Assert.AreEqual(3, slowed.HandNumber);
        }
    }
}
