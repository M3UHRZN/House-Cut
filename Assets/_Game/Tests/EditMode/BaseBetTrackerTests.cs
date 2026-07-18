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
    }
}
