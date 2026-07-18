using Game.Data;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Sahnedeki tek GameConfig referansı; diğer sistemler tuning değerlerini
    /// sabit sayı gömmek yerine buradan okur.
    /// </summary>
    public class GameConfigProvider : MonoBehaviour
    {
        [SerializeField] private GameConfig config;

        public GameConfig Config => config;

        private void Awake()
        {
            if (config == null)
            {
                Debug.LogError($"{nameof(GameConfigProvider)}: GameConfig atanmamış.", this);
                return;
            }

            Debug.Log($"GameConfig yüklendi — startMoney={config.startMoney}, minBet={config.minBet}");
        }
    }
}
