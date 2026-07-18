namespace Game.Cards
{
    /// <summary>
    /// The dealer's fixed, transparent draw rule (GDD §03.5): draw until the
    /// hand totals at least 17. A soft 17 (e.g. Ace+6) still stands.
    /// </summary>
    public static class DealerRules
    {
        public static bool ShouldHit(Hand hand) => hand.Total < 17;
    }
}
