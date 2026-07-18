using Game.Cards;
using Game.Data;

namespace Game.Economy
{
    /// <summary>
    /// Turns a finished hand into a money change on the Bank (GDD §03 ödeme
    /// tablosu). Reuses HandResolver for the outcome + payout — win +bet,
    /// blackjack +bet×1.5, loss/push -bet (tie is the house edge, no push) —
    /// then applies it through the Bank, which clamps the balance at zero.
    /// </summary>
    public static class Settlement
    {
        /// <summary>
        /// Resolves the hand, applies the payout to the bank, and returns the
        /// signed money delta that was applied (for scoring / UI).
        /// </summary>
        public static int Settle(Bank bank, Hand player, Hand dealer, int bet, GameConfig config)
        {
            HandOutcome outcome = HandResolver.Resolve(player, dealer);
            int delta = HandResolver.CalculatePayout(player, outcome, bet, config.blackjackPayout);
            bank.Apply(delta);
            return delta;
        }
    }
}
