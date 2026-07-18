using System;

namespace Game.Cards
{
    public enum HandOutcome
    {
        PlayerWins,
        DealerWins
    }

    /// <summary>
    /// Resolves a finished player hand against the dealer's hand and computes
    /// the resulting money delta (GDD §03.6). Ties go to the house — there is
    /// no push. Blackjack (first-two-card 21) pays out at blackjackPayout
    /// (3:2 by GDD default) instead of the normal 1:1.
    /// </summary>
    public static class HandResolver
    {
        public static HandOutcome Resolve(Hand player, Hand dealer)
        {
            if (player.IsBust)
            {
                return HandOutcome.DealerWins;
            }

            if (dealer.IsBust)
            {
                return HandOutcome.PlayerWins;
            }

            return player.Total > dealer.Total ? HandOutcome.PlayerWins : HandOutcome.DealerWins;
        }

        /// <summary>Money delta to apply to the player's balance for this hand.</summary>
        public static int CalculatePayout(Hand player, HandOutcome outcome, int bet, float blackjackPayout)
        {
            if (outcome == HandOutcome.DealerWins)
            {
                return -bet;
            }

            if (player.IsBlackjack)
            {
                return (int)Math.Round(bet * blackjackPayout, MidpointRounding.AwayFromZero);
            }

            return bet;
        }
    }
}
