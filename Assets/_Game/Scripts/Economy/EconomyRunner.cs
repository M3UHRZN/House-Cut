using Game.Core;
using UnityEngine;

namespace Game.Economy
{
    /// <summary>
    /// Scene-visible host that wires the money economy into the game loop by
    /// hooking GameFlow.StateChanged — the seam GameFlowRunner reserves for
    /// "Later issues (Bank, Deck, Dealer…) will hook into GameFlow.StateChanged".
    /// Reacts to the economy-owned states (bet setup on BetCheck, lock on Deal,
    /// base-bet ramp on NextHand). Settlement is left as an explicit SettleHand
    /// call for whoever holds both hands (future DealerTurn wiring), since no
    /// dealer-hand producer exists yet.
    /// </summary>
    public class EconomyRunner : MonoBehaviour
    {
        [SerializeField] private GameConfigProvider configProvider;
        [SerializeField] private GameFlowRunner flowRunner;
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

        private void Start()
        {
            // Subscribe in Start so GameFlowRunner.Awake has already created Flow
            // (Awake order between components is undefined).
            if (flowRunner == null || flowRunner.Flow == null)
            {
                Debug.LogError($"{nameof(EconomyRunner)}: GameFlowRunner atanmamış.", this);
                return;
            }

            flowRunner.Flow.StateChanged += OnStateChanged;
        }

        private void OnStateChanged(GameState previous, GameState next)
        {
            if (Economy == null)
            {
                return;
            }

            switch (next)
            {
                case GameState.BetCheck:
                    Economy.BeginHand();
                    Debug.Log($"Economy: yeni el — taban={Economy.CurrentBaseBet}, para={Economy.Balance}, yeterli mi={Economy.HasFundsForBaseBet}");
                    break;

                case GameState.Deal:
                    Economy.LockBet();
                    Debug.Log($"Economy: bahis kilitlendi = {Economy.LockedBet}");
                    break;

                case GameState.NextHand:
                    Economy.AdvanceToNextHand();
                    Debug.Log($"Economy: sonraki el taban bahis = {Economy.CurrentBaseBet}");
                    break;
            }
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
