using Game.Cards;
using Game.Data;

namespace Game.Economy
{
    /// <summary>
    /// Single coordinator that owns the money economy and drives the per-hand
    /// lifecycle by composing the pieces built in the Money System group:
    /// Bank (money authority), BaseBetTracker (ramping base bet), BetSelector
    /// (per-hand bet choice) and DefermentToken (blackjack "Erteleme" reward).
    /// Plain C# so it is unit-testable; the scene-side EconomyRunner hooks it
    /// into GameFlow.StateChanged.
    /// </summary>
    public class EconomyController
    {
        private readonly GameConfig config;
        private readonly Bank bank;
        private readonly BaseBetTracker baseBet;
        private readonly DefermentToken deferment = new DefermentToken();

        private BetSelector activeSelector;
        private int lockedBet;

        public EconomyController(GameConfig config, bool slowerRamp = false)
        {
            this.config = config;
            bank = new Bank(config);
            baseBet = new BaseBetTracker(config, slowerRamp);
        }

        public int Balance => bank.Balance;
        public int CurrentBaseBet => baseBet.Current;
        public int LockedBet => lockedBet;
        public bool HasDeferment => deferment.Has;

        /// <summary>The base bet the player must be able to cover to keep playing.</summary>
        public int RequiredBet => baseBet.Current;

        /// <summary>BetCheck decision: is there enough money for this hand's base bet?</summary>
        public bool HasFundsForBaseBet => bank.Balance >= baseBet.Current;

        /// <summary>
        /// Starts a hand: opens a fresh bet selector between the current base bet
        /// and the money on hand, and seeds the locked bet at the base. Returns the
        /// selector so the up/down arrows can drive it before the bet is locked.
        /// </summary>
        public BetSelector BeginHand()
        {
            activeSelector = new BetSelector(config, baseBet.Current, bank.Balance);
            lockedBet = baseBet.Current;
            return activeSelector;
        }

        /// <summary>Locks the selector's current value as the bet for this hand (at Deal).</summary>
        public void LockBet()
        {
            if (activeSelector != null)
            {
                lockedBet = activeSelector.Current;
            }
        }

        /// <summary>
        /// Resolves the finished hand into money on the Bank using the locked bet,
        /// awards a Deferment token on a blackjack win (GDD §03), and returns the
        /// signed money delta that was applied.
        /// </summary>
        public int SettleHand(Hand player, Hand dealer)
        {
            HandOutcome outcome = HandResolver.Resolve(player, dealer);
            int delta = Settlement.Settle(bank, player, dealer, lockedBet, config);

            if (outcome == HandOutcome.PlayerWins && player.IsBlackjack)
            {
                deferment.Grant();
            }

            return delta;
        }

        /// <summary>Player spends the Erteleme (if held) to skip the next base-bet increase.</summary>
        public void UseDeferment()
        {
            if (deferment.Consume())
            {
                baseBet.DeferNextRamp();
            }
        }

        /// <summary>Advances to the next hand, ramping the base bet per the Doom-engine rules.</summary>
        public void AdvanceToNextHand()
        {
            baseBet.AdvanceHand();
        }
    }
}
