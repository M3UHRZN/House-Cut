using Game.Core;
using Game.Data;
using UnityEngine;

namespace Game.Economy
{
    /// <summary>
    /// Scene-visible host that constructs and exposes the money economy. It no
    /// longer subscribes to GameFlow.StateChanged: BlackjackLoop is the single
    /// deterministic conductor (GDD §08) and drives the per-hand economy calls
    /// (BeginHand on BetCheck, LockBet on Deal, AdvanceToNextHand on NextHand).
    /// This component owns the EconomyController instance — including its single
    /// shared OrganManager, built from the organs assigned here — so OrganSaleRunner,
    /// CriticalStateRunner and anything else in the scene that needs organ state
    /// read/sell through EconomyController.OrganManager instead of building
    /// their own. Böbrek II's slower ramp is no longer a manual Inspector flag:
    /// EconomyController flips it itself the moment that organ is actually sold.
    /// </summary>
    public class EconomyRunner : MonoBehaviour
    {
        [SerializeField] private GameConfigProvider configProvider;
        [SerializeField] private OrganData[] organs;

        public EconomyController Economy { get; private set; }

        private void Awake()
        {
            if (configProvider == null || configProvider.Config == null)
            {
                Debug.LogError($"{nameof(EconomyRunner)}: GameConfigProvider atanmamış.", this);
                return;
            }

            Economy = new EconomyController(configProvider.Config, organs);
            Debug.Log($"Economy hazır — para={Economy.Balance}, taban bahis={Economy.CurrentBaseBet}");
        }
    }
}
