using Game.Presentation;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Core
{
    /// <summary>
    /// Scene-visible host for one CameraLookController. Reads mouse delta through the
    /// Input System (this project's activeInputHandler is the new Input System only —
    /// legacy Input.GetAxis does not work) and applies the clamped yaw/pitch to the
    /// camera's local rotation, offset from whatever orientation it was placed at in
    /// the scene (the "resting at the table" pose). The applied rotation is
    /// exponentially smoothed (frame-rate independent) so mouse look doesn't snap
    /// 1:1 with raw pixel deltas.
    ///
    /// GDD §06 "Kamera rig": also locks free look and recenters whenever GameFlow
    /// enters OrganSale/Extraction (the "beden defteri" sequence) — same
    /// GameFlow.StateChanged seam EconomyRunner/OrganSaleRunner already use. The
    /// OS cursor is locked/hidden during normal play and released for that UI
    /// sequence (OrganSaleRunner's Canvas needs a free cursor to click buttons).
    ///
    /// Right mouse button zooms (narrows FOV), smoothed. This is the sole writer
    /// of CinemachineCamera.Lens.FieldOfView: CriticalStateRunner used to also
    /// write it for its own narrowing, and two independent per-frame writers to
    /// the same field fought each other (flicker). CriticalStateRunner now only
    /// exposes IsCritical/NarrowedFovDelta, which this script folds into its own
    /// zoom target.
    /// </summary>
    public class CameraLookRunner : MonoBehaviour
    {
        [Tooltip("GDD §06: yaklaşık 35° sağ/sol.")]
        [SerializeField] private float yawLimitDegrees = 35f;
        [Tooltip("GDD §06: yaklaşık 25° aşağı/yukarı.")]
        [SerializeField] private float pitchLimitDegrees = 25f;
        [SerializeField] private float sensitivity = 0.05f;
        [Tooltip("Yüksek = daha ani/keskin, düşük = daha yumuşak/gecikmeli.")]
        [SerializeField] private float lookSmoothing = 12f;
        [SerializeField] private GameFlowRunner flowRunner;

        [Header("Zoom (sağ tık)")]
        [SerializeField] private CinemachineCamera cinemachineCamera;
        [Tooltip("Zoom'da hedef FOV. Ne kadar küçükse o kadar yakınlaşır.")]
        [SerializeField] private float zoomFieldOfView = 35f;
        [Tooltip("FOV'un ne kadar hızlı yumuşayarak hedefe ulaşacağı.")]
        [SerializeField] private float zoomSmoothing = 10f;
        [Tooltip("Opsiyonel — Kritik Hâl'in FOV daraltmasıyla birleştirmek için.")]
        [SerializeField] private CriticalStateRunner criticalStateRunner;

        private Quaternion restRotation;
        private CameraLookController controller;
        private float currentYaw;
        private float currentPitch;
        private float restFieldOfView;
        private float currentFieldOfView;

        public CameraLookController Controller => controller;

        private void Awake()
        {
            restRotation = transform.localRotation;
            controller = new CameraLookController(yawLimitDegrees, pitchLimitDegrees);

            if (cinemachineCamera != null)
            {
                restFieldOfView = cinemachineCamera.Lens.FieldOfView;
                currentFieldOfView = restFieldOfView;
            }
        }

        private void Start()
        {
            // Subscribe in Start so GameFlowRunner.Awake has already created Flow
            // (Awake order between components is undefined).
            if (flowRunner != null && flowRunner.Flow != null)
            {
                flowRunner.Flow.StateChanged += OnStateChanged;
            }

            LockCursor();
        }

        private void OnStateChanged(GameState previous, GameState next)
        {
            bool organSaleSequence = next == GameState.OrganSale || next == GameState.Extraction;
            controller.SetFreeLookEnabled(!organSaleSequence);

            if (organSaleSequence)
            {
                UnlockCursor();
            }
            else
            {
                LockCursor();
            }
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private static void UnlockCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse != null)
            {
                Vector2 delta = mouse.delta.ReadValue() * sensitivity;
                controller.ApplyDelta(delta.x, delta.y);
            }

            // Exponential smoothing: frame-rate independent, always converges
            // toward the controller's clamped target instead of snapping to it.
            float t = 1f - Mathf.Exp(-lookSmoothing * Time.deltaTime);
            currentYaw = Mathf.Lerp(currentYaw, controller.Yaw, t);
            currentPitch = Mathf.Lerp(currentPitch, controller.Pitch, t);

            transform.localRotation = restRotation * Quaternion.Euler(-currentPitch, currentYaw, 0f);

            ApplyZoom(mouse);
        }

        private void ApplyZoom(Mouse mouse)
        {
            if (cinemachineCamera == null)
            {
                return;
            }

            bool zooming = mouse != null && mouse.rightButton.isPressed;
            float narrowing = criticalStateRunner != null && criticalStateRunner.IsCritical
                ? criticalStateRunner.NarrowedFovDelta
                : 0f;
            float target = (zooming ? zoomFieldOfView : restFieldOfView) - narrowing;

            float zoomT = 1f - Mathf.Exp(-zoomSmoothing * Time.deltaTime);
            currentFieldOfView = Mathf.Lerp(currentFieldOfView, target, zoomT);

            LensSettings lens = cinemachineCamera.Lens;
            lens.FieldOfView = currentFieldOfView;
            cinemachineCamera.Lens = lens;
        }

        private void OnDestroy()
        {
            if (flowRunner != null && flowRunner.Flow != null)
            {
                flowRunner.Flow.StateChanged -= OnStateChanged;
            }
        }
    }
}
