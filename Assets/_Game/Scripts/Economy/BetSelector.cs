using System;
using Game.Data;

namespace Game.Economy
{
    /// <summary>
    /// Lets the player set the bet for a hand between the current base bet and
    /// their available money (GDD §03: bahsi ayarlama ±$50/adım, min bahis ↔
    /// eldeki para). Up-arrow raises by betStep, down-arrow lowers by betStep;
    /// the bet can never go below the base bet nor above the money on hand.
    /// Plain C# so it is unit-testable without Unity's frame loop.
    /// </summary>
    public class BetSelector
    {
        private readonly int step;
        private readonly int baseBet;
        private readonly int maxBet;

        public int Current { get; private set; }

        public BetSelector(GameConfig config, int baseBet, int availableMoney)
        {
            step = config.betStep;
            this.baseBet = baseBet;
            maxBet = availableMoney;
            Current = baseBet;
        }

        /// <summary>Up arrow: +betStep, capped at the available money.</summary>
        public void Increase()
        {
            Current = Math.Min(Current + step, maxBet);
        }

        /// <summary>Down arrow: -betStep, floored at the base bet.</summary>
        public void Decrease()
        {
            Current = Math.Max(Current - step, baseBet);
        }
    }
}
