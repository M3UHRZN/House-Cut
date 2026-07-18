using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Scene-visible host for the single GameFlow instance. Later issues (Bank,
    /// Deck, Dealer, Presentation) will hook into GameFlow.StateChanged instead
    /// of talking to each other directly.
    /// </summary>
    public class GameFlowRunner : MonoBehaviour
    {
        public GameFlow Flow { get; private set; }

        private void Awake()
        {
            Flow = new GameFlow();
            Flow.StateChanged += OnStateChanged;
        }

        private void OnStateChanged(GameState previous, GameState next)
        {
            Debug.Log($"GameFlow: {previous} -> {next}");
        }

        private void OnDestroy()
        {
            if (Flow != null)
            {
                Flow.StateChanged -= OnStateChanged;
            }
        }
    }
}
