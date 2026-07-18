using Game.Core;
using UnityEngine;

namespace Game.Economy
{
    /// <summary>
    /// Scene-visible host that constructs and exposes the money economy. It no
    /// longer subscribes to GameFlow.StateChanged: BlackjackLoop is the single
    /// deterministic conductor (GDD §08) and drives the per-hand economy calls
    /// (BeginHand on BetCheck, LockBet on Deal, AdvanceToNextHand on NextHand).
    /// This component only owns the EconomyController instance.
    /// </summary>
    public class EconomyRunner : MonoBehaviour
    {
        [SerializeField] private GameConfigProvider configProvider;
        [Tooltip("Böbrek II buff: taban bahis her el yerine her 2 elde bir yükselir (GDD §05).")]
        [SerializeField] private bool slowerRamp;

        public EconomyController Economy { get; private set; }

        private void Awake()
        {
            if (configProvider == null || configProvider.Config == null)
            {
                Debug.LogError($"{nameof(EconomyRunner)}: GameConfigProvider atanmamış.", this);
                return;
            }

            Economy = new EconomyController(configProvider.Config, slowerRamp);
            Debug.Log($"Economy hazır — para={Economy.Balance}, taban bahis={Economy.CurrentBaseBet}");
        }
    }
}
