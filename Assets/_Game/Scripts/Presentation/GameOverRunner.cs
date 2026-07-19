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

        [Header("Sahne Referansları (Hierarchy: GameOverCanvas altında)")]
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI subtitleText;

        [Header("Zamanlama")]
        [SerializeField, Min(0f)] private float fadeDuration = 1.5f;
        [SerializeField, Min(0f)] private float holdDuration = 2.5f;

        private void Awake()
        {
            if (flowRunner == null) flowRunner = FindAnyObjectByType<GameFlowRunner>();
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
            if (canvasGroup == null)
            {
                SceneManager.LoadScene(mainMenuSceneName);
                return;
            }

            canvasGroup.blocksRaycasts = true;
            StartCoroutine(FadeThenReturnToMenu());
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
