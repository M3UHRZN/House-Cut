using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Game.Core
{
    /// <summary>
    /// #42 (GDD §11): Esc ile duraklat/çıkış. Şimdilik minimal — Ana Menü'ye
    /// döner (tam oyun-içi duraklatma paneli ayrı bir "oyun içi UI" işi olarak
    /// bilinçli ertelendi). Ana Menü'nün Play'i taze bir Table sahnesi
    /// yükleyerek hızlı (&lt;5sn) yeniden başlatmayı sağlar.
    /// </summary>
    public class PauseController : MonoBehaviour
    {
        [SerializeField] private string mainMenuSceneName = "MainMenu";

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }
    }
}
