using System;
using System.Collections.Generic;
using System.Linq;
using Game.Data;
using Game.Economy;
using UnityEngine;

namespace Game.Organs
{
    public class OrganManager
    {
        private const int SpleenSalePenalty = 300;
        private const float KidneyISaleBonus = 0.2f;
        private const string HeartId = "heart";

        private readonly List<OrganData> organs;
        private readonly HashSet<OrganData> soldOrgans = new();
        private readonly Bank bank;

        public IReadOnlyList<OrganData> Organs => organs;
        public IReadOnlyCollection<OrganData> SoldOrgans => soldOrgans;

        /// <summary>True once the Heart has been given — the run is over (GDD §04).</summary>
        public bool IsGameOver { get; private set; }

        /// <summary>Raised when the Heart is extracted (final payment); the run ends.</summary>
        public event Action HeartExtracted;

        /// <summary>Raised on every normal sale (not the Heart) so EffectStack can pick up the organ's permanent effect.</summary>
        public event Action<OrganData> OrganSold;

        public OrganManager(IEnumerable<OrganData> organs)
            : this(organs, null)
        {
        }

        public OrganManager(IEnumerable<OrganData> organs, Bank bank)
        {
            this.bank = bank;
            this.organs = organs
                .Where(organ => organ != null)
                .Distinct()
                .ToList();
        }

        public bool IsSold(OrganData organ)
        {
            return organ != null && soldOrgans.Contains(organ);
        }

        public int GetSaleValue(OrganData organ)
        {
            if (organ == null || IsSold(organ))
            {
                return 0;
            }

            var saleValue = organ.dollarValue;

            if (HasSoldOrgan("kidney_i"))
            {
                saleValue = Mathf.RoundToInt(saleValue * (1f + KidneyISaleBonus));
            }

            if (HasSoldOrgan("spleen"))
            {
                saleValue -= SpleenSalePenalty;
            }

            return Mathf.Max(0, saleValue);
        }

        public int Sell(OrganData organ)
        {
            // The Heart is never a normal sale — it is the soft-lock fallback and
            // is given only through SellHeart, which ends the run (GDD §04).
            if (organ != null && organ.id == HeartId)
            {
                return 0;
            }

            var saleValue = GetSaleValue(organ);
            if (saleValue == 0)
            {
                return 0;
            }

            soldOrgans.Add(organ);
            bank?.Deposit(saleValue);
            OrganSold?.Invoke(organ);
            return saleValue;
        }

        /// <summary>
        /// Heart safety (GDD §04): the Heart auto-unlocks only when money is below
        /// the min bet AND every other organ has already been sold, so the sale
        /// screen can never soft-lock.
        /// </summary>
        public bool IsHeartUnlocked(int minBet)
        {
            var heart = organs.FirstOrDefault(organ => organ.id == HeartId);
            if (heart == null || IsSold(heart))
            {
                return false;
            }

            int money = bank?.Balance ?? 0;
            if (money >= minBet)
            {
                return false;
            }

            return organs.All(organ => organ.id == HeartId || IsSold(organ));
        }

        /// <summary>
        /// Gives the Heart as the final $5.000 payment (fixed, no sale modifiers)
        /// when it is unlocked, then ends the run. Returns 0 (no-op) while locked.
        /// </summary>
        public int SellHeart(int minBet)
        {
            if (!IsHeartUnlocked(minBet))
            {
                return 0;
            }

            var heart = organs.First(organ => organ.id == HeartId);
            soldOrgans.Add(heart);

            int payout = heart.dollarValue;
            bank?.Deposit(payout);

            IsGameOver = true;
            HeartExtracted?.Invoke();
            return payout;
        }

        private bool HasSoldOrgan(string organId)
        {
            return soldOrgans.Any(organ => organ.id == organId);
        }
    }
}
