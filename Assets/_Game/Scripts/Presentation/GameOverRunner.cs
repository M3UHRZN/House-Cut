using System.Collections;
using Game.Core;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Presentation
{
    /// <summary>
    /// GameOver state'inin bitiş sunumu: tam ekran siyaha faded, kısa bir kapanış
    /// metni gösterir, sonra ana menüye döner. UI, Table.unity içinde
    /// GameOverRunner GameObject'inin altında (GameOverCanvas) sahne-authored
    /// olarak durur; bu script yalnızca CanvasGroup alpha'sını ve metni yönetir.
    /// </summary>
    public class GameOverRunner : MonoBehaviour
    {
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        [Header("Bağımlılıklar (opsiyonel — atanmazsa sahneden bulunur)")]
        [SerializeField] private GameFlowRunner flowRunner;
        [SerializeField] private BlackjackLoopRunner loopRunner;

        [Header("Sahne Referansları (Hierarchy: GameOverCanvas altında)")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Header("Zamanlama")]
        [SerializeField, Min(0f)] private float fadeDuration = 1.5f;
        [SerializeField, Min(0f)] private float holdDuration = 2.5f;

        [Header("Metinler — Kaybetme")]
        [SerializeField] private string loseTitle = "OYUN BİTTİ";
        [SerializeField] private string loseSubtitle = "Ev her zaman kazanır.";
        [SerializeField] private Color loseSubtitleColor = new Color32(0xB3, 0x22, 0x1A, 0xFF);

        [Header("Metinler — Kazanma (İngilizce)")]
        [SerializeField] private string winTitle = "YOU WIN";
        [SerializeField] private string winSubtitle = "You will pay a price.";
        [SerializeField] private Color winSubtitleColor = new Color32(0x6B, 0x8E, 0x4E, 0xFF);

        private void Awake()
        {
            if (flowRunner == null) flowRunner = FindAnyObjectByType<GameFlowRunner>();
            if (loopRunner == null) loopRunner = FindAnyObjectByType<BlackjackLoopRunner>();
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }

        private void Start()
        {
            if (flowRunner == null || flowRunner.Flow == null)
            {
                Debug.LogError($"{nameof(GameOverRunner)}: sahnede GameFlowRunner bulunamadı.", this);
                enabled = false;
                return;
            }

            flowRunner.Flow.StateChanged += OnStateChanged;
        }

        private void OnDestroy()
        {
            if (flowRunner != null && flowRunner.Flow != null)
            {
                flowRunner.Flow.StateChanged -= OnStateChanged;
            }
        }

        private void OnStateChanged(GameState previous, GameState next)
        {
            if (next == GameState.GameOver)
            {
                PlayEnding();
            }
        }

        private void PlayEnding()
        {
            bool won = loopRunner != null && loopRunner.Loop != null && loopRunner.Loop.Won;
            ApplyEndingText(won);

            if (canvasGroup == null)
            {
                SceneManager.LoadScene(mainMenuSceneName);
                return;
            }

            canvasGroup.blocksRaycasts = true;
            StartCoroutine(FadeThenReturnToMenu());
        }

        private void ApplyEndingText(bool won)
        {
            if (titleText != null)
            {
                titleText.text = won ? winTitle : loseTitle;
            }

            if (subtitleText != null)
            {
                subtitleText.text = won ? winSubtitle : loseSubtitle;
                subtitleText.color = won ? winSubtitleColor : loseSubtitleColor;
            }
        }

        private IEnumerator FadeThenReturnToMenu()
        {
            float elapsed = 0f;
            while (elapsed < fadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = fadeDuration <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeDuration);
                yield return null;
            }

            canvasGroup.alpha = 1f;
            yield return new WaitForSeconds(holdDuration);
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }
}
