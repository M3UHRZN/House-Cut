using System.Collections.Generic;
using Game.Cards;
using Game.Data;
using Game.Organs;

namespace Game.Economy
{
    /// <summary>
    /// Single coordinator that owns the money economy and drives the per-hand
    /// lifecycle by composing the pieces built in the Money System group:
    /// Bank (money authority), BaseBetTracker (ramping base bet), BetSelector
    /// (per-hand bet choice) and DefermentToken (blackjack "Erteleme" reward).
    /// Also owns the single shared OrganManager plus its two dependent
    /// permanent-effect stacks (EffectStack for money bleed/regen, LungTimerEffect
    /// for the decision timer) so every Runner in the scene reads/sells organs
    /// through the same instance instead of each spinning up its own (GDD §08).
    /// Plain C# so it is unit-testable; the scene-side EconomyRunner hooks it
    /// into GameFlow.StateChanged.
    /// </summary>
    public class EconomyController
    {
        private readonly GameConfig config;
        private readonly Bank bank;
        private readonly BaseBetTracker baseBet;
        private readonly DefermentToken deferment = new DefermentToken();
        private readonly EffectStack effectStack;
        private readonly LungTimerEffect lungTimer = new LungTimerEffect();

        private BetSelector activeSelector;
        private int lockedBet;

        public EconomyController(GameConfig config, IEnumerable<OrganData> organs = null)
        {
            this.config = config;
            bank = new Bank(config);
            baseBet = new BaseBetTracker(config);
            effectStack = new EffectStack(bank);
            OrganManager = new OrganManager(organs ?? System.Array.Empty<OrganData>(), bank);
            OrganManager.OrganSold += OnOrganSold;
        }

        /// <summary>The single shared organ manager — OrganSaleRunner, CriticalStateRunner and
        /// anything else that needs sold-organ state must read this instance, not build its own.</summary>
        public OrganManager OrganManager { get; }

        public int Balance => bank.Balance;
        public Bank MoneyBank => bank;
        public int CurrentBaseBet => baseBet.Current;
        public int LockedBet => lockedBet;
        public bool HasDeferment => deferment.Has;

        /// <summary>Kaçıncı el oynanıyor (ilk el = 1) — HUD'daki el sayacı (GDD §06).</summary>
        public int HandNumber => baseBet.HandNumber;

        /// <summary>Effective decision time for the current hand, after Lung's sold debuff/buff (GDD §05).</summary>
        public float CurrentDecisionTime => lungTimer.GetDecisionTime(config.decisionTime);

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

            effectStack.TickHandEnd(outcome);

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
            lungTimer.AdvanceToNextHand();
        }

        /// <summary>
        /// Feeds every organ sale into the two permanent-effect stacks (GDD §08):
        /// EffectStack for the money bleed/regen, LungTimerEffect for the decision
        /// timer. Also flips Böbrek II's slower base-bet ramp the moment it sells.
        /// </summary>
        private void OnOrganSold(OrganData organ)
        {
            effectStack.AddOrgan(organ);
            lungTimer.AddOrgan(organ);

            if (organ.buffType == OrganBuffType.SlowerBetRamp)
            {
                baseBet.SetSlowerRamp(true);
            }
        }
    }
}
