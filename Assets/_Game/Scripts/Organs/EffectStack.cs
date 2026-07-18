using System.Collections.Generic;
using Game.Cards;
using Game.Data;
using Game.Economy;

namespace Game.Organs
{
    /// <summary>
    /// Permanent per-run effects from sold organs (GDD §08 EffectStack): every
    /// sold organ's debuff/buff stays active for the rest of the run and is
    /// reapplied every hand. Scoped here to the money-affecting effects —
    /// CashBleedPerHand (Böbrek II, Karaciğer) and CashRegenOnWin (Karaciğer) —
    /// dispatched by enum rather than organ id. Presentation-only effects
    /// (shader/audio/timer modifiers) are read straight off the stacked
    /// OrganData by whichever future system owns them.
    /// </summary>
    public class EffectStack
    {
        private readonly List<OrganData> stack = new();
        private readonly Bank bank;

        public EffectStack(Bank bank)
        {
            this.bank = bank;
        }

        public IReadOnlyList<OrganData> Stack => stack;

        /// <summary>Adds a sold organ's permanent effect to the stack. Organs stack — e.g. selling both Böbrek II and Karaciğer applies both bleeds every hand.</summary>
        public void AddOrgan(OrganData organ)
        {
            if (organ != null)
            {
                stack.Add(organ);
            }
        }

        /// <summary>
        /// Applies every stacked organ's end-of-hand effect once (GDD §03/§08
        /// RESOLVE step): bleed always, regen only on a player win.
        /// </summary>
        public void TickHandEnd(HandOutcome outcome)
        {
            foreach (var organ in stack)
            {
                ApplyDebuff(organ);

                if (outcome == HandOutcome.PlayerWins)
                {
                    ApplyBuff(organ);
                }
            }
        }

        private void ApplyDebuff(OrganData organ)
        {
            switch (organ.debuffType)
            {
                case OrganDebuffType.CashBleedPerHand:
                    bank.Withdraw((int)organ.debuffStrength);
                    break;
            }
        }

        private void ApplyBuff(OrganData organ)
        {
            switch (organ.buffType)
            {
                case OrganBuffType.CashRegenOnWin:
                    bank.Deposit((int)organ.buffStrength);
                    break;
            }
        }
    }
}
