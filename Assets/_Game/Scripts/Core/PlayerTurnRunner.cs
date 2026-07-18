using Game.Cards;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Scene-visible host for one PlayerTurnController. Hit()/Stand() are meant
    /// to be wired to the table's Kart Çek / Kal buttons (GDD §02.3); Update()
    /// drives the 15s decision timer. Later issues (GameFlow, Bank) will hook
    /// into TurnEnded instead of talking to this runner directly.
    /// </summary>
    public class PlayerTurnRunner : MonoBehaviour
    {
        [SerializeField] private GameConfigProvider configProvider;

        private Deck deck;
        private Hand hand;
        private PlayerTurnController controller;

        public Hand Hand => hand;
        public PlayerTurnController Controller => controller;

        private void Awake()
        {
            if (configProvider == null || configProvider.Config == null)
            {
                Debug.LogError($"{nameof(PlayerTurnRunner)}: GameConfigProvider atanmamış.", this);
                return;
            }

            deck = new Deck();
            StartTurn();
        }

        public void StartTurn()
        {
            hand = new Hand();
            controller = new PlayerTurnController(hand, configProvider.Config.decisionTime);
            controller.TurnEnded += OnTurnEnded;
        }

        public void Hit()
        {
            controller?.Hit(deck.Draw());
        }

        public void Stand()
        {
            controller?.Stand();
        }

        private void Update()
        {
            if (controller != null && controller.IsAwaitingDecision)
            {
                controller.Tick(Time.deltaTime);
            }
        }

        private void OnTurnEnded(PlayerTurnOutcome outcome)
        {
            Debug.Log($"PlayerTurn: {outcome} (total={hand.Total})");
        }
    }
}
