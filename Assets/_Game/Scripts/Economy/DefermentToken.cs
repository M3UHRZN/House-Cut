namespace Game.Economy
{
    /// <summary>
    /// The single-use "Erteleme" reward earned on a blackjack (GDD §03):
    /// grant one on blackjack, it does not stack, and consuming it skips the
    /// next base-bet increase (wire Consume() to BaseBetTracker.DeferNextRamp()).
    /// Plain C# so it is unit-testable without Unity's frame loop.
    /// </summary>
    public class DefermentToken
    {
        public bool Has { get; private set; }

        /// <summary>Awards a token. Does not stack — stays capped at a single token.</summary>
        public void Grant()
        {
            Has = true;
        }

        /// <summary>
        /// Spends the token if one is held; returns true when spent so the caller
        /// can defer the next ramp. Returns false (and does nothing) when empty.
        /// </summary>
        public bool Consume()
        {
            if (!Has)
            {
                return false;
            }

            Has = false;
            return true;
        }
    }
}
