using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Core
{
    /// <summary>
    /// Scene-visible host for one CameraLookController. Reads mouse delta through the
    /// Input System (this project's activeInputHandler is the new Input System only —
    /// legacy Input.GetAxis does not work) and applies the clamped yaw/pitch to the
    /// camera's local rotation, offset from whatever orientation it was placed at in
    /// the scene (the "resting at the table" pose).
    /// </summary>
    public class CameraLookRunner : MonoBehaviour
    {
        [Tooltip("GDD §06: yaklaşık 35° sağ/sol.")]
        [SerializeField] private float yawLimitDegrees = 35f;
        [Tooltip("GDD §06: yaklaşık 25° aşağı/yukarı.")]
        [SerializeField] private float pitchLimitDegrees = 25f;
        [SerializeField] private float sensitivity = 0.05f;

        private Quaternion restRotation;
        private CameraLookController controller;

        public CameraLookController Controller => controller;

        private void Awake()
        {
            restRotation = transform.localRotation;
            controller = new CameraLookController(yawLimitDegrees, pitchLimitDegrees);
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null)
            {
                return;
            }

            Vector2 delta = mouse.delta.ReadValue() * sensitivity;
            controller.ApplyDelta(delta.x, delta.y);

            transform.localRotation = restRotation * Quaternion.Euler(-controller.Pitch, controller.Yaw, 0f);
        }
    }
}
