using Game.Core;
using NUnit.Framework;

namespace Game.Tests.EditMode
{
    public class CameraLookControllerTests
    {
        [Test]
        public void StartsCentered()
        {
            var controller = new CameraLookController(35f, 25f);

            Assert.AreEqual(0f, controller.Yaw);
            Assert.AreEqual(0f, controller.Pitch);
        }

        [Test]
        public void ApplyDelta_AccumulatesWithinLimits()
        {
            var controller = new CameraLookController(35f, 25f);

            controller.ApplyDelta(10f, 5f);
            controller.ApplyDelta(10f, 5f);

            Assert.AreEqual(20f, controller.Yaw);
            Assert.AreEqual(10f, controller.Pitch);
        }

        [Test]
        public void Yaw_ClampsAtPositiveLimit()
        {
            var controller = new CameraLookController(35f, 25f);

            controller.ApplyDelta(1000f, 0f);

            Assert.AreEqual(35f, controller.Yaw);
        }

        [Test]
        public void Yaw_ClampsAtNegativeLimit()
        {
            var controller = new CameraLookController(35f, 25f);

            controller.ApplyDelta(-1000f, 0f);

            Assert.AreEqual(-35f, controller.Yaw);
        }

        [Test]
        public void Pitch_ClampsAtLimits()
        {
            var controller = new CameraLookController(35f, 25f);

            controller.ApplyDelta(0f, 1000f);
            Assert.AreEqual(25f, controller.Pitch);

            controller.ApplyDelta(0f, -2000f);
            Assert.AreEqual(-25f, controller.Pitch);
        }

        [Test]
        public void FreeLookDisabled_IgnoresDelta()
        {
            var controller = new CameraLookController(35f, 25f);
            controller.ApplyDelta(10f, 10f);

            controller.SetFreeLookEnabled(false);
            controller.ApplyDelta(10f, 10f);

            Assert.AreEqual(10f, controller.Yaw);
            Assert.AreEqual(10f, controller.Pitch);
        }

        [Test]
        public void FreeLookReEnabled_ResumesAccumulating()
        {
            var controller = new CameraLookController(35f, 25f);
            controller.SetFreeLookEnabled(false);
            controller.ApplyDelta(10f, 10f);

            controller.SetFreeLookEnabled(true);
            controller.ApplyDelta(5f, 5f);

            Assert.AreEqual(5f, controller.Yaw);
            Assert.AreEqual(5f, controller.Pitch);
        }
    }
}
