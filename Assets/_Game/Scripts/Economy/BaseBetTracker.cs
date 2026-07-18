using Game.Data;

namespace Game.Economy
{
    /// <summary>
    /// Tracks the per-hand base (minimum) bet that ramps up every hand — the
    /// "Doom engine" that guarantees eventual bankruptcy (GDD §03: 1. el $700,
    /// 3. el $1.300, 6. el $2.200…). Base = minBet + hands×betRamp, read from
    /// GameConfig. Stateful accumulation so it composes with two GDD rules:
    ///  - Böbrek II buff (slowerRamp): base rises every 2 hands, not every hand (§05).
    ///  - Erteleme (DeferNextRamp): skips a single upcoming increase, then the
    ///    ramp resumes normally (§03).
    /// </summary>
    public class BaseBetTracker
    {
        private readonly int minBet;
        private readonly int betRamp;
        private readonly bool slowerRamp;

        private int handsTowardsRamp;
        private bool deferNextRamp;

        public int Current { get; private set; }

        public BaseBetTracker(GameConfig config, bool slowerRamp = false)
        {
            minBet = config.minBet;
            betRamp = config.betRamp;
            this.slowerRamp = slowerRamp;
            Current = minBet;
        }

        /// <summary>Erteleme: the next AdvanceHand skips its increase (one-shot).</summary>
        public void DeferNextRamp()
        {
            deferNextRamp = true;
        }

        /// <summary>Advances to the next hand, raising the base bet per the ramp rules.</summary>
        public void AdvanceHand()
        {
            if (deferNextRamp)
            {
                deferNextRamp = false;
                return;
            }

            if (slowerRamp)
            {
                handsTowardsRamp++;
                if (handsTowardsRamp < 2)
                {
                    return;
                }

                handsTowardsRamp = 0;
            }

            Current += betRamp;
        }
    }
}
