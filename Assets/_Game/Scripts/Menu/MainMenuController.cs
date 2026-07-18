using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Menu
{
    /// <summary>
    /// Ana menü ekranı: Play / Ayarlar / Credits / Çıkış panelleri arasında
    /// geçiş yapar. Play, oyun sahnesine (Table) geçer. Çıkış bir onay
    /// pop-up'ı açar; Evet uygulamayı kapatır, Hayır ana menüye döner.
    ///
    /// Buton tıklamaları kasıtlı olarak burada kod ile (AddListener)
    /// bağlanıyor — sahne dosyasındaki UnityEvent persistent-call YAML'ını
    /// canlı Editör olmadan elle yazmak riskliydi (küçük bir hata sessizce
    /// "tıklayınca hiçbir şey olmayan" bir buton üretirdi); bu yol derleyici
    /// tarafından doğrulanıyor.
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Paneller")]
        [SerializeField] private GameObject mainPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject creditsPanel;
        [SerializeField] private GameObject quitConfirmPanel;

        [Header("Ana Menü Butonları")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button creditsButton;
        [SerializeField] private Button quitButton;

        [Header("Geri Butonları")]
        [SerializeField] private Button settingsBackButton;
        [SerializeField] private Button creditsBackButton;

        [Header("Çıkış Onayı")]
        [SerializeField] private Button quitYesButton;
        [SerializeField] private Button quitNoButton;

        [SerializeField] private string gameSceneName = "Table";

        private void Awake()
        {
            if (playButton != null) playButton.onClick.AddListener(Play);
            if (settingsButton != null) settingsButton.onClick.AddListener(OpenSettings);
            if (creditsButton != null) creditsButton.onClick.AddListener(OpenCredits);
            if (quitButton != null) quitButton.onClick.AddListener(OpenQuitConfirm);

            if (settingsBackButton != null) settingsBackButton.onClick.AddListener(ShowMain);
            if (creditsBackButton != null) creditsBackButton.onClick.AddListener(ShowMain);

            if (quitYesButton != null) quitYesButton.onClick.AddListener(ConfirmQuit);
            if (quitNoButton != null) quitNoButton.onClick.AddListener(ShowMain);

            ShowMain();
        }

        public void Play()
        {
            SceneManager.LoadScene(gameSceneName);
        }

        public void OpenSettings() => Show(settingsPanel);

        public void OpenCredits() => Show(creditsPanel);

        public void OpenQuitConfirm() => Show(quitConfirmPanel);

        public void ShowMain() => Show(mainPanel);

        /// <summary>Çıkış onayı: Evet — uygulamayı kapatır (Editor'de Play modundan çıkar).</summary>
        public void ConfirmQuit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void Show(GameObject panel)
        {
            if (mainPanel != null) mainPanel.SetActive(panel == mainPanel);
            if (settingsPanel != null) settingsPanel.SetActive(panel == settingsPanel);
            if (creditsPanel != null) creditsPanel.SetActive(panel == creditsPanel);
            if (quitConfirmPanel != null) quitConfirmPanel.SetActive(panel == quitConfirmPanel);
        }
    }
}
