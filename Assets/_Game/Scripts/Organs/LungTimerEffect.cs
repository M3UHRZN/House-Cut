using System.Collections.Generic;
using Game.Data;
using UnityEngine;

namespace Game.Organs
{
    /// <summary>
    /// Lung's decision-timer effect (GDD §05): once sold, permanently lowers the
    /// base decision time (ReducedDecisionTime, 15→10s), and every 3rd hand grants
    /// +5s on that hand's decision (ExtraDecisionTime). Mirrors EffectStack's
    /// per-organ dispatch but for the decision timer instead of money — kept as
    /// its own calculator since PlayerTurnController isn't wired to any per-organ
    /// system yet. Whoever wires the turn calls GetDecisionTime(baseTime) to get
    /// the effective value for the current hand.
    /// </summary>
    public class LungTimerEffect
    {
        private readonly List<OrganData> stack = new();

        public int HandNumber { get; private set; } = 1;

        /// <summary>Adds a sold organ's permanent timer effect to the stack.</summary>
        public void AddOrgan(OrganData organ)
        {
            if (organ != null)
            {
                stack.Add(organ);
            }
        }

        /// <summary>Advances the hand counter used for the buff's every-3rd-hand cadence.</summary>
        public void AdvanceToNextHand()
        {
            HandNumber++;
        }

        /// <summary>
        /// Effective decision time for the current hand: GameConfig's base time,
        /// reduced by any sold ReducedDecisionTime debuff, plus an
        /// ExtraDecisionTime buff bonus on every 3rd hand. Never goes below zero.
        /// </summary>
        public float GetDecisionTime(float baseDecisionTime)
        {
            float time = baseDecisionTime;

            foreach (var organ in stack)
            {
                if (organ.debuffType == OrganDebuffType.ReducedDecisionTime)
                {
                    time -= organ.debuffStrength;
                }
            }

            if (HandNumber % 3 == 0)
            {
                foreach (var organ in stack)
                {
                    if (organ.buffType == OrganBuffType.ExtraDecisionTime)
                    {
                        time += organ.buffStrength;
                    }
                }
            }

            return Mathf.Max(time, 0f);
        }
    }
}
