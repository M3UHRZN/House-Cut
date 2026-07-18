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
        private readonly float decisionTime;

        private PlayerTurnController playerTurn;
        private BetSelector activeSelector;

        public Hand PlayerHand { get; private set; }
        public Hand DealerHand { get; private set; }
        public GameState State => flow.CurrentState;

        public event Action<Card, bool, bool> CardDealt;
        public event Action<Card> DealerHoleCardRevealed;
        public event Action<int> HandSettled;

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

            playerTurn = new PlayerTurnController(PlayerHand, decisionTime);
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
