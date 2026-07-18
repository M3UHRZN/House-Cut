using System.Collections.Generic;
using System.Linq;
using Game.Data;
using Game.Economy;

namespace Game.Organs
{
    /// <summary>
    /// Selection/threshold state for the "beden defteri" screen (GDD §04 Organ
    /// Satışı): multi-select organs, track the running total against the
    /// required bet, and confirm the batch as sequential OrganManager.Sell
    /// calls. Plain C# so it is unit-testable without Unity's frame loop; the
    /// scene-side OrganSaleRunner binds this to the Canvas.
    /// </summary>
    public class OrganSaleController
    {
        private const string HeartId = "heart";

        private readonly OrganManager organManager;
        private readonly Bank bank;
        private readonly List<OrganData> selected = new();

        public OrganSaleController(OrganManager organManager, Bank bank)
        {
            this.organManager = organManager;
            this.bank = bank;
        }

        /// <summary>
        /// Sellable organs for this screen. Kalp (Heart) stays hidden until it
        /// is the only unsold organ left (GDD §04 Kalp Güvenliği — soft-lock
        /// prevention: the last-resort organ never competes with the rest).
        /// </summary>
        public IEnumerable<OrganData> AvailableOrgans
        {
            get
            {
                var unsold = organManager.Organs.Where(organ => !organManager.IsSold(organ)).ToList();
                bool onlyHeartLeft = unsold.All(organ => organ.id == HeartId);
                return unsold.Where(organ => organ.id != HeartId || onlyHeartLeft);
            }
        }

        public IReadOnlyList<OrganData> Selected => selected;

        public bool IsSelected(OrganData organ) => organ != null && selected.Contains(organ);

        public void ToggleSelection(OrganData organ)
        {
            if (organ == null || !AvailableOrgans.Contains(organ))
            {
                return;
            }

            if (!selected.Remove(organ))
            {
                selected.Add(organ);
            }
        }

        /// <summary>Sum of the selected organs' current sale values.</summary>
        public int SelectedTotal => selected.Sum(organ => organManager.GetSaleValue(organ));

        /// <summary>SAT is only enabled once at least one organ is selected and the projected balance covers the required bet.</summary>
        public bool CanConfirm(int requiredBet)
        {
            if (selected.Count == 1 && selected[0].id == HeartId)
            {
                return organManager.IsHeartUnlocked(requiredBet);
            }

            return selected.Count > 0 && bank.Balance + SelectedTotal >= requiredBet;
        }

        /// <summary>Sells every selected organ, in selection order, and clears the selection. Returns the total gained.</summary>
        public int Confirm()
        {
            return Confirm(0);
        }

        /// <summary>Sells the selection, routing the final Heart through its game-ending sale path.</summary>
        public int Confirm(int requiredBet)
        {
            int total = 0;
            foreach (var organ in selected.ToList())
            {
                total += organ.id == HeartId
                    ? organManager.SellHeart(requiredBet)
                    : organManager.Sell(organ);
            }

            selected.Clear();
            return total;
        }
    }
}
