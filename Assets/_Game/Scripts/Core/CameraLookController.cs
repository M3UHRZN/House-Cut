using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Clamped look-around for the table-fixed first-person camera (GDD §06:
    /// "Kamera masaya oturur; oyuncu yalnızca yaklaşık 35° sağ/sol ve 25° aşağı/yukarı
    /// bakabilir"). No translation — only yaw/pitch offsets from a resting orientation,
    /// clamped to the configured limits. Plain C# so it is unit-testable without
    /// Unity's frame loop; CameraLookRunner feeds it mouse deltas and applies rotation.
    /// </summary>
    public class CameraLookController
    {
        private readonly float yawLimitDegrees;
        private readonly float pitchLimitDegrees;

        public float Yaw { get; private set; }
        public float Pitch { get; private set; }

        /// <summary>
        /// While false, ApplyDelta is ignored (e.g. during organ sale, where the GDD
        /// says the camera auto-turns to the body book and free look is disabled).
        /// </summary>
        public bool FreeLookEnabled { get; private set; } = true;

        public CameraLookController(float yawLimitDegrees, float pitchLimitDegrees)
        {
            this.yawLimitDegrees = yawLimitDegrees;
            this.pitchLimitDegrees = pitchLimitDegrees;
        }

        /// <summary>
        /// Disabling also snaps yaw/pitch back to center (GDD §06: "kamera
        /// otomatik olarak beden defterine döner") — a hard reset, not a smooth
        /// blend, matching the project's "kapsam hilesi" simplicity elsewhere.
        /// </summary>
        public void SetFreeLookEnabled(bool enabled)
        {
            FreeLookEnabled = enabled;
            if (!enabled)
            {
                Yaw = 0f;
                Pitch = 0f;
            }
        }

        /// <summary>Accumulates a mouse-driven look delta (in degrees), clamped to the limits.</summary>
        public void ApplyDelta(float yawDelta, float pitchDelta)
        {
            if (!FreeLookEnabled)
            {
                return;
            }

            Yaw = Mathf.Clamp(Yaw + yawDelta, -yawLimitDegrees, yawLimitDegrees);
            Pitch = Mathf.Clamp(Pitch + pitchDelta, -pitchLimitDegrees, pitchLimitDegrees);
        }
    }
}
