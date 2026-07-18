using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Core
{
    /// <summary>
    /// #42 (GDD §11): Esc ile oyun-içi duraklatma menüsü. Ana menüdeki "Ayarlar"
    /// ile aynı içerik (ses seviyesi) + "Ana Menüye Dön" butonu; dön butonu bir
    /// "emin misin?" onayı açar (Evet ana menüye döner, Hayır duraklatmaya geri).
    ///
    /// UI, Credits panelindeki gibi çalışma zamanında koddan kuruluyor — böylece
    /// zaten büyük olan Table.unity'ye elle riskli YAML eklemeye gerek kalmıyor ve
    /// her buton bağlantısı derleyici tarafından doğrulanıyor.
    ///
    /// Duraklatınca Time.timeScale=0 (el/karar zamanlayıcısı dahil tüm oyun döngüsü
    /// donar), imleç serbest bırakılır ve CameraLookRunner tamamen kapatılır (fare
    /// deltasının birikip devam ederken kameranın zıplamasını önlemek için). Devam
    /// edince hepsi normal oyun durumuna geri alınır.
    /// </summary>
    public class PauseController : MonoBehaviour
    {
        [SerializeField] private string mainMenuSceneName = "MainMenu";
        [Tooltip("Opsiyonel — atanmazsa TMP varsayılan fontu kullanılır.")]
        [SerializeField] private TMP_FontAsset titleFont;
        [Tooltip("Opsiyonel — atanmazsa TMP varsayılan fontu kullanılır.")]
        [SerializeField] private TMP_FontAsset bodyFont;

        // GDD "Beden Defteri" paleti (OrganSaleRunner ile aynı) — tema tutarlılığı.
        private static readonly Color ColBase = new Color32(0x12, 0x0D, 0x09, 0xFF);
        private static readonly Color ColPanel = new Color32(0x24, 0x1B, 0x14, 0xFF);
        private static readonly Color ColButton = new Color32(0x3A, 0x2C, 0x1E, 0xFF);
        private static readonly Color ColInk = new Color32(0xD8, 0xCB, 0xB4, 0xFF);
        private static readonly Color ColAccent = new Color32(0xC9, 0xA2, 0x27, 0xFF);
        private static readonly Color ColAccentPressed = new Color32(0xA5, 0x84, 0x1F, 0xFF);
        private static readonly Color ColDim = new Color(0f, 0f, 0f, 0.82f);

        private const string VolumeKey = "MasterVolume";

        private GameObject root;
        private GameObject pausePanel;
        private GameObject confirmPanel;
        private Slider volumeSlider;

        private CameraLookRunner cameraLook;
        private GameFlowRunner flowRunner;
        private bool isPaused;

        private void Awake()
        {
            cameraLook = FindAnyObjectByType<CameraLookRunner>();
            flowRunner = FindAnyObjectByType<GameFlowRunner>();

            EnsureEventSystem();
            BuildUi();
            root.SetActive(false);
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
            else if (confirmPanel.activeSelf)
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
            root.SetActive(true);
            pausePanel.SetActive(true);
            confirmPanel.SetActive(false);

            Time.timeScale = 0f;
            SetCursorFree(true);
            if (cameraLook != null) cameraLook.enabled = false;

            SyncVolumeSlider();
        }

        private void Resume()
        {
            isPaused = false;
            root.SetActive(false);

            Time.timeScale = 1f;
            RestoreGameplayCursor();
            if (cameraLook != null) cameraLook.enabled = true;
        }

        private void OpenConfirm()
        {
            pausePanel.SetActive(false);
            confirmPanel.SetActive(true);
        }

        private void CloseConfirm()
        {
            confirmPanel.SetActive(false);
            pausePanel.SetActive(true);
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

        // ---- UI inşası (çalışma zamanı) ---------------------------------------

        private void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
            {
                return;
            }

            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
            es.transform.SetParent(transform, false);
        }

        private void BuildUi()
        {
            root = new GameObject("PauseMenuCanvas",
                typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            root.transform.SetParent(transform, false);
            root.layer = LayerMask.NameToLayer("UI");

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000; // organ satış Canvas'ının üstünde kalsın

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var rootRect = (RectTransform)root.transform;

            BuildPausePanel(rootRect);
            BuildConfirmPanel(rootRect);
        }

        private void BuildPausePanel(RectTransform parent)
        {
            pausePanel = CreateFullOverlay(parent, "PausePanel", ColDim);
            var panelRect = (RectTransform)pausePanel.transform;

            var box = CreateBox(panelRect, "PauseBox", new Vector2(600f, 560f), ColPanel);

            CreateText(box, "Title", "DURAKLATILDI", titleFont, 56, ColAccent,
                TextAlignmentOptions.Center, new Vector2(0f, 210f), new Vector2(560f, 90f));

            CreateText(box, "VolumeLabel", "SES SEVİYESİ", bodyFont, 26, ColInk,
                TextAlignmentOptions.Center, new Vector2(0f, 90f), new Vector2(500f, 40f));

            volumeSlider = CreateVolumeSlider(box, new Vector2(0f, 40f), new Vector2(440f, 36f));
            float saved = PlayerPrefs.GetFloat(VolumeKey, 1f);
            volumeSlider.minValue = 0f;
            volumeSlider.maxValue = 1f;
            volumeSlider.SetValueWithoutNotify(saved);
            volumeSlider.onValueChanged.AddListener(OnVolumeChanged);

            CreateButton(box, "ResumeButton", "DEVAM ET", new Vector2(0f, -60f),
                new Vector2(360f, 64f), 28, Resume);

            CreateButton(box, "MainMenuButton", "ANA MENÜYE DÖN", new Vector2(0f, -140f),
                new Vector2(360f, 64f), 26, OpenConfirm);
        }

        private void BuildConfirmPanel(RectTransform parent)
        {
            confirmPanel = CreateFullOverlay(parent, "ConfirmPanel", ColDim);
            var panelRect = (RectTransform)confirmPanel.transform;
            confirmPanel.SetActive(false);

            var box = CreateBox(panelRect, "ConfirmBox", new Vector2(560f, 280f), ColPanel);

            CreateText(box, "ConfirmText",
                "Ana menüye dönmek istediğine emin misin?\nBu eldeki ilerleme kaybolur.",
                bodyFont, 26, ColInk, TextAlignmentOptions.Center,
                new Vector2(0f, 60f), new Vector2(500f, 120f));

            CreateButton(box, "YesButton", "EVET", new Vector2(-130f, -70f),
                new Vector2(220f, 64f), 28, ReturnToMainMenu);

            CreateButton(box, "NoButton", "HAYIR", new Vector2(130f, -70f),
                new Vector2(220f, 64f), 28, CloseConfirm);
        }

        // ---- UI yardımcıları ---------------------------------------------------

        private static GameObject CreateFullOverlay(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            Stretch(rect);

            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = true; // arkadaki oyuna tıklamayı engeller
            return go;
        }

        private static RectTransform CreateBox(RectTransform parent, string name, Vector2 size, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.color = color;
            img.raycastTarget = true;
            return rect;
        }

        private void CreateText(RectTransform parent, string name, string content, TMP_FontAsset font,
            float size, Color color, TextAlignmentOptions alignment, Vector2 pos, Vector2 dims)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = dims;

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.fontSize = size;
            text.color = color;
            text.alignment = alignment;
            text.raycastTarget = false;
            if (font != null)
            {
                text.font = font;
            }
        }

        private Button CreateButton(RectTransform parent, string name, string label, Vector2 pos,
            Vector2 size, float fontSize, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            var rect = (RectTransform)go.transform;
            rect.SetParent(parent, false);
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = pos;
            rect.sizeDelta = size;

            var img = go.GetComponent<Image>();
            img.color = ColButton;

            var button = go.GetComponent<Button>();
            button.targetGraphic = img;
            var colors = button.colors;
            colors.normalColor = ColButton;
            colors.highlightedColor = ColAccent;
            colors.pressedColor = ColAccentPressed;
            colors.selectedColor = ColButton;
            colors.disabledColor = ColBase;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;
            button.onClick.AddListener(onClick);

            CreateText(rect, "Label", label, bodyFont, fontSize, ColInk,
                TextAlignmentOptions.Center, Vector2.zero, size);

            return button;
        }

        private Slider CreateVolumeSlider(RectTransform parent, Vector2 pos, Vector2 size)
        {
            var sliderGo = new GameObject("VolumeSlider", typeof(RectTransform), typeof(Slider));
            var sliderRect = (RectTransform)sliderGo.transform;
            sliderRect.SetParent(parent, false);
            sliderRect.anchorMin = sliderRect.anchorMax = new Vector2(0.5f, 0.5f);
            sliderRect.pivot = new Vector2(0.5f, 0.5f);
            sliderRect.anchoredPosition = pos;
            sliderRect.sizeDelta = size;

            // Background
            var bg = CreateChildImage(sliderRect, "Background", ColBase);
            var bgRect = (RectTransform)bg.transform;
            bgRect.anchorMin = new Vector2(0f, 0.25f);
            bgRect.anchorMax = new Vector2(1f, 0.75f);
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // Fill Area -> Fill
            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            var fillAreaRect = (RectTransform)fillArea.transform;
            fillAreaRect.SetParent(sliderRect, false);
            fillAreaRect.anchorMin = new Vector2(0f, 0.25f);
            fillAreaRect.anchorMax = new Vector2(1f, 0.75f);
            fillAreaRect.offsetMin = new Vector2(10f, 0f);
            fillAreaRect.offsetMax = new Vector2(-10f, 0f);

            var fill = CreateChildImage(fillAreaRect, "Fill", ColAccent);
            var fillRect = (RectTransform)fill.transform;
            fillRect.anchorMin = new Vector2(0f, 0f);
            fillRect.anchorMax = new Vector2(1f, 1f);
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
            fillRect.pivot = new Vector2(0f, 0.5f);

            // Handle Slide Area -> Handle
            var handleArea = new GameObject("Handle Slide Area", typeof(RectTransform));
            var handleAreaRect = (RectTransform)handleArea.transform;
            handleAreaRect.SetParent(sliderRect, false);
            handleAreaRect.anchorMin = new Vector2(0f, 0f);
            handleAreaRect.anchorMax = new Vector2(1f, 1f);
            handleAreaRect.offsetMin = new Vector2(10f, 0f);
            handleAreaRect.offsetMax = new Vector2(-10f, 0f);

            var handle = CreateChildImage(handleAreaRect, "Handle", ColInk);
            var handleRect = (RectTransform)handle.transform;
            handleRect.anchorMin = new Vector2(0f, 0f);
            handleRect.anchorMax = new Vector2(0f, 1f);
            handleRect.sizeDelta = new Vector2(24f, 0f);
            handleRect.pivot = new Vector2(0.5f, 0.5f);

            var slider = sliderGo.GetComponent<Slider>();
            slider.fillRect = fillRect;
            slider.handleRect = handleRect;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.direction = Slider.Direction.LeftToRight;
            slider.transition = Selectable.Transition.ColorTint;
            var colors = slider.colors;
            colors.normalColor = ColInk;
            colors.highlightedColor = ColAccent;
            colors.pressedColor = ColAccentPressed;
            slider.colors = colors;

            return slider;
        }

        private static GameObject CreateChildImage(RectTransform parent, string name, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            go.GetComponent<Image>().color = color;
            return go;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }
    }
}
