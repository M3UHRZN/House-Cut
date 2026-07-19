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
        private bool slowerRamp;

        private int handsTowardsRamp;
        private bool deferNextRamp;

        public int Current { get; private set; }

        /// <summary>
        /// Kaçıncı el oynanıyor (ilk el = 1) — GDD §06 "El sayacı" ve kademe (I–V)
        /// göstergesi bundan türetilir. Bahis rampasından bağımsızdır: erteleme ya da
        /// Böbrek II'nin yavaş rampası bahsi yükseltmese bile el sayısı yine ilerler.
        /// </summary>
        public int HandNumber { get; private set; } = 1;

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

        /// <summary>
        /// Böbrek II buff (GDD §05, OrganBuffType.SlowerBetRamp): flips the ramp
        /// to every-2-hands once that organ is sold mid-run. Idempotent/one-way —
        /// the buff is permanent for the rest of the run once granted.
        /// </summary>
        public void SetSlowerRamp(bool value)
        {
            slowerRamp = value;
        }

        /// <summary>Advances to the next hand, raising the base bet per the ramp rules.</summary>
        public void AdvanceHand()
        {
            // El sayısı her çağrıda ilerler — aşağıdaki erken return'ler yalnızca
            // bahsin yükselip yükselmeyeceğini belirler, elin oynanmış olmasını değil.
            HandNumber++;

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
