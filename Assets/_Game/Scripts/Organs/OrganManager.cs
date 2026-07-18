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

        private readonly List<OrganData> organs;
        private readonly HashSet<OrganData> soldOrgans = new();
        private readonly Bank bank;

        public IReadOnlyList<OrganData> Organs => organs;
        public IReadOnlyCollection<OrganData> SoldOrgans => soldOrgans;

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
            var saleValue = GetSaleValue(organ);
            if (saleValue == 0)
            {
                return 0;
            }

            soldOrgans.Add(organ);
            bank?.Deposit(saleValue);
            return saleValue;
        }

        private bool HasSoldOrgan(string organId)
        {
            return soldOrgans.Any(organ => organ.id == organId);
        }
    }
}
