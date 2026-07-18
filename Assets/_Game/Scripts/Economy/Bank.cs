using Game.Data;

namespace Game.Economy
{
    /// <summary>
    /// Single authority over the player's money (GDD §04: para tek takip edilen
    /// sayıdır — hem bahis birimi hem "can"). Starts from GameConfig.startMoney
    /// and can never drop below zero — the player never goes into debt.
    /// Plain C# so it is unit-testable without Unity's frame loop.
    /// </summary>
    public class Bank
    {
        public int Balance { get; private set; }

        public Bank(GameConfig config)
        {
            Balance = config.startMoney;
        }

        /// <summary>Adds money (win / organ sale). Ignores negatives.</summary>
        public void Deposit(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Balance += amount;
        }

        /// <summary>Removes money, clamped so the balance never goes negative.</summary>
        public void Withdraw(int amount)
        {
            if (amount <= 0)
            {
                return;
            }

            Balance = amount >= Balance ? 0 : Balance - amount;
        }

        /// <summary>
        /// Applies a signed delta (payout from HandResolver). Positive deposits,
        /// negative withdraws; result is always clamped to >= 0.
        /// </summary>
        public void Apply(int delta)
        {
            if (delta >= 0)
            {
                Deposit(delta);
            }
            else
            {
                Withdraw(-delta);
            }
        }
    }
}
