using System;
using System.Collections.Generic;

namespace Game.Core
{
    public class GameFlow
    {
        private static readonly Dictionary<GameState, GameState[]> Transitions = new()
        {
            { GameState.Intro, new[] { GameState.BetCheck } },
            { GameState.BetCheck, new[] { GameState.Deal, GameState.OrganSale, GameState.GameOver } },
            { GameState.OrganSale, new[] { GameState.Extraction } },
            { GameState.Extraction, new[] { GameState.BetCheck, GameState.GameOver } },
            { GameState.Deal, new[] { GameState.PlayerTurn } },
            { GameState.PlayerTurn, new[] { GameState.Deal, GameState.DealerTurn } },
            { GameState.DealerTurn, new[] { GameState.Resolve } },
            { GameState.Resolve, new[] { GameState.NextHand } },
            { GameState.NextHand, new[] { GameState.BetCheck } },
            { GameState.GameOver, new[] { GameState.Intro } },
        };

        public GameState CurrentState { get; private set; } = GameState.Intro;
        public bool SignalReady { get; private set; }

        public event Action<GameState, GameState> StateChanged;

        public void SignalTransitionReady()
        {
            SignalReady = true;
        }

        public bool TryTransition(GameState target)
        {
            if (!SignalReady)
            {
                return false;
            }

            if (!IsAllowed(CurrentState, target))
            {
                return false;
            }

            var previous = CurrentState;
            CurrentState = target;
            SignalReady = false;
            StateChanged?.Invoke(previous, target);
            return true;
        }

        public static IReadOnlyList<GameState> GetAllowedTransitions(GameState from)
        {
            return Transitions[from];
        }

        private static bool IsAllowed(GameState from, GameState to)
        {
            return Array.IndexOf(Transitions[from], to) >= 0;
        }
    }
}
