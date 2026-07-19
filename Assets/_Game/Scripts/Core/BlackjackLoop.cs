using System;
using Game.Cards;
using Game.Economy;

namespace Game.Core
{
    public class BlackjackLoop
    {
        private readonly GameFlow flow;
        private readonly EconomyController economy;
        private readonly Deck deck;

        // Retained for the constructor signature (existing callers/tests pass
        // GameConfig.decisionTime here), but no longer read directly — each hand
        // now sources its decision time from economy.CurrentDecisionTime instead,
        // so a sold Lung's timer debuff/buff (GDD §05) takes effect immediately.
        private readonly float decisionTime;

        private PlayerTurnController playerTurn;
        private BetSelector activeSelector;

        public Hand PlayerHand { get; private set; }
        public Hand DealerHand { get; private set; }
        public GameState State => flow.CurrentState;

        public event Action<Card, bool, bool> CardDealt;
        public event Action<Card> DealerHoleCardRevealed;
        public event Action<int> HandSettled;

        /// <summary>Ses için: oyuncu batınca (kartlar toplanmadan önce) tetiklenir.</summary>
        public event Action PlayerBusted;

        public BlackjackLoop(GameFlow flow, EconomyController economy, Deck deck, float decisionTime)
        {
            this.flow = flow;
            this.economy = economy;
            this.deck = deck;
            this.decisionTime = decisionTime;
            flow.StateChanged += OnStateChanged;
        }

        public void Begin()
        {
            Transition(GameState.BetCheck);
        }

        public int CurrentBet => activeSelector?.Current ?? 0;

        public void RaiseBet()
        {
            if (State == GameState.BetCheck)
            {
                activeSelector?.Increase();
            }
        }

        public void LowerBet()
        {
            if (State == GameState.BetCheck)
            {
                activeSelector?.Decrease();
            }
        }

        public void UseDeferment()
        {
            if (State == GameState.BetCheck)
            {
                economy.UseDeferment();
            }
        }

        public bool ConfirmBet()
        {
            if (State != GameState.BetCheck)
            {
                return false;
            }

            if (!economy.HasFundsForBaseBet)
            {
                return Transition(GameState.OrganSale);
            }

            return Transition(GameState.Deal);
        }

        public void Hit()
        {
            if (State != GameState.PlayerTurn || playerTurn == null || !playerTurn.IsAwaitingDecision)
            {
                return;
            }

            Card card = deck.Draw();
            CardDealt?.Invoke(card, false, true);
            playerTurn.Hit(card);
        }

        public void Stand()
        {
            if (State == GameState.PlayerTurn && playerTurn != null && playerTurn.IsAwaitingDecision)
            {
                playerTurn.Stand();
            }
        }

        public void Tick(float deltaTime)
        {
            if (State != GameState.PlayerTurn || playerTurn == null || !playerTurn.IsAwaitingDecision)
            {
                return;
            }

            playerTurn.Tick(deltaTime);
        }

        public bool AdvanceToNextHand()
        {
            return State == GameState.Resolve && Transition(GameState.NextHand) && Transition(GameState.BetCheck);
        }

        /// <summary>
        /// Called by the scene-side organ-extraction animation once the drop finishes.
        /// Performs the advance that used to run synchronously in OnStateChanged(Extraction):
        /// return to BetCheck, and deal immediately if the sale restored enough money.
        /// Kept out of OnStateChanged so GameFlow can hold in Extraction while the
        /// drop animation plays (GDD anti-softlock: no transition without a finished signal).
        /// </summary>
        public bool CompleteExtraction()
        {
            if (State != GameState.Extraction)
            {
                return false;
            }

            bool advanced = Transition(GameState.BetCheck);
            if (advanced && economy.HasFundsForBaseBet)
            {
                Transition(GameState.Deal);
            }

            return advanced;
        }

        private void OnStateChanged(GameState previous, GameState next)
        {
            switch (next)
            {
                case GameState.BetCheck:
                    activeSelector = economy.BeginHand();
                    break;
                case GameState.Deal:
                    economy.LockBet();
                    DealInitialCards();
                    Transition(GameState.PlayerTurn);
                    if (PlayerHand.IsBlackjack)
                    {
                        playerTurn.Stand();
                    }
                    break;
                case GameState.DealerTurn:
                    PlayDealerTurn();
                    break;
                case GameState.Resolve:
                    ResolveHand();
                    break;
                case GameState.NextHand:
                    economy.AdvanceToNextHand();
                    break;
            }
        }

        private void DealInitialCards()
        {
            PlayerHand = new Hand();
            DealerHand = new Hand();

            DealToPlayer();
            DealToDealer(true);
            DealToPlayer();
            DealToDealer(false);

            playerTurn = new PlayerTurnController(PlayerHand, economy.CurrentDecisionTime);
            playerTurn.TurnEnded += _ => EndPlayerTurn();

        }

        private void DealToPlayer()
        {
            Card card = deck.Draw();
            PlayerHand.AddCard(card);
            CardDealt?.Invoke(card, false, true);
        }

        private void DealToDealer(bool faceUp)
        {
            Card card = deck.Draw();
            DealerHand.AddCard(card);
            CardDealt?.Invoke(card, true, faceUp);
        }

        private void EndPlayerTurn()
        {
            if (State == GameState.PlayerTurn)
            {
                if (PlayerHand.IsBust)
                {
                    PlayerBusted?.Invoke();
                }

                Transition(GameState.DealerTurn);
            }
        }

        private void PlayDealerTurn()
        {
            if (DealerHand.Cards.Count > 1)
            {
                DealerHoleCardRevealed?.Invoke(DealerHand.Cards[1]);
            }

            if (!PlayerHand.IsBust)
            {
                while (DealerRules.ShouldHit(DealerHand))
                {
                    DealToDealer(true);
                }
            }

            Transition(GameState.Resolve);
        }

        private void ResolveHand()
        {
            HandSettled?.Invoke(economy.SettleHand(PlayerHand, DealerHand));
        }

        private bool Transition(GameState target)
        {
            flow.SignalTransitionReady();
            return flow.TryTransition(target);
        }
    }
}
