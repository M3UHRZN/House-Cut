using System;
using Game.Cards;

namespace Game.Core
{
    public enum PlayerTurnOutcome
    {
        Stand,
        Bust
    }

    /// <summary>
    /// Drives the player's Hit/Stand decision loop for one hand, including the
    /// decision timer that auto-Stands when it expires (GDD §02.3 rule 3) and
    /// the immediate-loss bust check (GDD §03 rule 4). Time-agnostic: callers
    /// drive it via Tick(deltaTime) so it is testable without Unity's frame loop.
    /// </summary>
    public class PlayerTurnController
    {
        private readonly Hand hand;
        private readonly float decisionTime;

        public float RemainingTime { get; private set; }
        public bool IsAwaitingDecision { get; private set; }

        public event Action<PlayerTurnOutcome> TurnEnded;

        public PlayerTurnController(Hand hand, float decisionTime)
        {
            this.hand = hand;
            this.decisionTime = decisionTime;
            RemainingTime = decisionTime;
            IsAwaitingDecision = true;
        }

        public void Tick(float deltaTime)
        {
            if (!IsAwaitingDecision)
            {
                return;
            }

            RemainingTime -= deltaTime;
            if (RemainingTime <= 0f)
            {
                RemainingTime = 0f;
                Stand();
            }
        }

        public void Hit(Card card)
        {
            if (!IsAwaitingDecision)
            {
                return;
            }

            hand.AddCard(card);

            if (hand.IsBust)
            {
                End(PlayerTurnOutcome.Bust);
                return;
            }

            RemainingTime = decisionTime;
        }

        public void Stand()
        {
            if (!IsAwaitingDecision)
            {
                return;
            }

            End(PlayerTurnOutcome.Stand);
        }

        private void End(PlayerTurnOutcome outcome)
        {
            IsAwaitingDecision = false;
            TurnEnded?.Invoke(outcome);
        }
    }
}
