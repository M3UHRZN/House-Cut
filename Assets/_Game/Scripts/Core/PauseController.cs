using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Core
{
    /// <summary>
    /// #42 (GDD §11): Esc ile oyun-içi duraklatma menüsü. UI, Table.unity içinde
    /// PauseController GameObject'inin altında (PauseMenuCanvas) sahne-authored
    /// olarak durur — Hierarchy'den seçilip konum/boyut elle ayarlanabilir. Bu
    /// script yalnızca davranışı yönetir: panel açma/kapama, ses değeri, kamera/
    /// imleç durumu, ana menüye dönüş.
    /// </summary>
    public class PauseController : MonoBehaviour
    {
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Kamera / Akış (opsiyonel — atanmazsa sahneden bulunur)")]
        [SerializeField] private CameraLookRunner cameraLook;
        [SerializeField] private GameFlowRunner flowRunner;

        [Header("Sahne Referansları (Hierarchy: PauseMenuCanvas altında)")]
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button mainMenuButton;
        [SerializeField] private Button confirmYesButton;
        [SerializeField] private Button confirmNoButton;

        private const string VolumeKey = "MasterVolume";
        private bool isPaused;

        private void Awake()
        {
            if (cameraLook == null) cameraLook = FindAnyObjectByType<CameraLookRunner>();
            if (flowRunner == null) flowRunner = FindAnyObjectByType<GameFlowRunner>();

            if (resumeButton != null) resumeButton.onClick.AddListener(Resume);
            if (mainMenuButton != null) mainMenuButton.onClick.AddListener(OpenConfirm);
            if (confirmYesButton != null) confirmYesButton.onClick.AddListener(ReturnToMainMenu);
            if (confirmNoButton != null) confirmNoButton.onClick.AddListener(CloseConfirm);
            if (volumeSlider != null) volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

            if (pausePanel != null) pausePanel.SetActive(false);
            if (confirmPanel != null) confirmPanel.SetActive(false);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null || !keyboard.escapeKey.wasPressedThisFrame)
            {
                return;
            }

            if (!isPaused)
            {
                OpenPause();
            }
            else if (confirmPanel != null && confirmPanel.activeSelf)
            {
                CloseConfirm();
            }
            else
            {
                Resume();
            }
        }

        private void OnDestroy()
        {
            // Sahne bu obje yok edilirken duraklatmadaysa timeScale sıfırda kalmasın.
            if (isPaused)
            {
                Time.timeScale = 1f;
            }
        }

        // ---- durum geçişleri --------------------------------------------------

        private void OpenPause()
        {
            isPaused = true;
            if (pausePanel != null) pausePanel.SetActive(true);
            if (confirmPanel != null) confirmPanel.SetActive(false);

            Time.timeScale = 0f;
            SetCursorFree(true);
            if (cameraLook != null) cameraLook.enabled = false;

            SyncVolumeSlider();
        }

        private void Resume()
        {
            isPaused = false;
            if (pausePanel != null) pausePanel.SetActive(false);
            if (confirmPanel != null) confirmPanel.SetActive(false);

            Time.timeScale = 1f;
            RestoreGameplayCursor();
            if (cameraLook != null) cameraLook.enabled = true;
        }

        private void OpenConfirm()
        {
            if (pausePanel != null) pausePanel.SetActive(false);
            if (confirmPanel != null) confirmPanel.SetActive(true);
        }

        private void CloseConfirm()
        {
            if (confirmPanel != null) confirmPanel.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(true);
        }

        private void ReturnToMainMenu()
        {
            // timeScale devam etmeden önce mutlaka geri alınmalı, yoksa yüklenen
            // ana menü sahnesi donmuş halde açılır.
            Time.timeScale = 1f;
            SceneManager.LoadScene(mainMenuSceneName);
        }

        // ---- imleç -------------------------------------------------------------

        private static void SetCursorFree(bool free)
        {
            Cursor.lockState = free ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = free;
        }

        private void RestoreGameplayCursor()
        {
            // Organ satışı/çıkarma sırasında normalde imleç serbesttir; onun dışında
            // oyun imleci kilitli ve gizlidir (CameraLookRunner ile aynı mantık).
            bool freeDuringPlay = flowRunner != null && flowRunner.Flow != null &&
                (flowRunner.Flow.CurrentState == GameState.OrganSale ||
                 flowRunner.Flow.CurrentState == GameState.Extraction);
            SetCursorFree(freeDuringPlay);
        }

        // ---- ses ---------------------------------------------------------------

        private void SyncVolumeSlider()
        {
            float saved = PlayerPrefs.GetFloat(VolumeKey, 1f);
            AudioListener.volume = saved;
            if (volumeSlider != null)
            {
                volumeSlider.SetValueWithoutNotify(saved);
            }
        }

        private void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat(VolumeKey, value);
        }
    }
}
