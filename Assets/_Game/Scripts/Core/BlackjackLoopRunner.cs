using System.Collections;
using System.Collections.Generic;
using Game.Cards;
using Game.Economy;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Core
{
    public class BlackjackLoopRunner : MonoBehaviour
    {
        [SerializeField] private GameConfigProvider configProvider;
        [SerializeField] private GameFlowRunner flowRunner;
        [SerializeField] private EconomyRunner economyRunner;
        [SerializeField] private Transform cardRoot;
        [SerializeField] private CardPrefabCatalog cardPrefabCatalog;
        [SerializeField] private Transform playerCardAnchor;
        [SerializeField] private Transform dealerCardAnchor;
        [SerializeField, Min(0f)] private float cardSpacing = 0.38f;
        [SerializeField, Min(0.01f)] private float cardScale = 0.65f;
        [SerializeField, Min(0f)] private float resultDisplaySeconds = 1.5f;

        private readonly List<CardVisual> playerCards = new();
        private readonly List<CardVisual> dealerCards = new();
        private BlackjackLoop loop;

        private void Awake()
        {
            if (configProvider == null || configProvider.Config == null || flowRunner == null || economyRunner == null ||
                cardPrefabCatalog == null || playerCardAnchor == null || dealerCardAnchor == null)
            {
                Debug.LogError($"{nameof(BlackjackLoopRunner)}: Config, akış, ekonomi, kart kataloğu ve Player/Dealer anchor atanmalı.", this);
                enabled = false;
            }
        }

        private IEnumerator Start()
        {
            // Build the loop in Start (not Awake): GameFlowRunner.Awake sets Flow
            // and EconomyRunner.Awake sets Economy, and Awake order between
            // components is undefined — reading them here guarantees both exist.
            loop = new BlackjackLoop(flowRunner.Flow, economyRunner.Economy, new Deck(), configProvider.Config.decisionTime);
            loop.CardDealt += OnCardDealt;
            loop.DealerHoleCardRevealed += OnDealerHoleCardRevealed;
            loop.HandSettled += _ => StartCoroutine(AdvanceAfterResult());

            yield return null;
            loop.Begin();
        }

        private void Update()
        {
            loop?.Tick(Time.deltaTime);

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.enterKey.wasPressedThisFrame)
            {
                ConfirmBet();
            }
            else if (keyboard.hKey.wasPressedThisFrame)
            {
                Hit();
            }
            else if (keyboard.spaceKey.wasPressedThisFrame)
            {
                Stand();
            }
            else if (keyboard.upArrowKey.wasPressedThisFrame)
            {
                RaiseBet();
            }
            else if (keyboard.downArrowKey.wasPressedThisFrame)
            {
                LowerBet();
            }
            else if (keyboard.dKey.wasPressedThisFrame)
            {
                UseDeferment();
            }
        }

        public void ConfirmBet() => loop?.ConfirmBet();
        public void Hit() => loop?.Hit();
        public void Stand() => loop?.Stand();

        public void RaiseBet()
        {
            loop?.RaiseBet();
            if (loop != null)
            {
                Debug.Log($"Bahis: ${loop.CurrentBet}");
            }
        }

        public void LowerBet()
        {
            loop?.LowerBet();
            if (loop != null)
            {
                Debug.Log($"Bahis: ${loop.CurrentBet}");
            }
        }

        public void UseDeferment() => loop?.UseDeferment();

        private void OnCardDealt(Card card, bool isDealer, bool faceUp)
        {
            List<CardVisual> views = isDealer ? dealerCards : playerCards;
            CardVisual view = CreateCardView(card, views.Count, isDealer);
            view.SetCard(card, faceUp);
            views.Add(view);
        }

        private void OnDealerHoleCardRevealed(Card card)
        {
            if (dealerCards.Count > 1)
            {
                dealerCards[1].SetCard(card, true);
            }
        }

        private CardVisual CreateCardView(Card card, int index, bool isDealer)
        {
            Transform parent = cardRoot != null ? cardRoot : transform;
            Transform anchor = isDealer ? dealerCardAnchor : playerCardAnchor;
            GameObject prefab = cardPrefabCatalog.GetPrefab(card);

            if (prefab == null)
            {
                Debug.LogWarning($"{nameof(BlackjackLoopRunner)}: {card} için prefab bulunamadı; placeholder kullanılıyor.", this);
                GameObject cardObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cardObject.transform.SetParent(parent, false);
                cardObject.transform.localScale = new Vector3(1.2f, 0.08f, 1.8f);
                cardObject.transform.SetPositionAndRotation(
                    CardPlacement.GetPosition(anchor, index, cardSpacing),
                    anchor.rotation);
                return cardObject.AddComponent<CardVisual>();
            }

            var viewObject = new GameObject($"{(isDealer ? "Dealer" : "Player")}_{card}");
            viewObject.transform.SetParent(parent, false);
            viewObject.transform.SetPositionAndRotation(
                CardPlacement.GetPosition(anchor, index, cardSpacing),
                anchor.rotation);

            GameObject model = Instantiate(prefab, viewObject.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localScale *= cardScale;

            CardVisual view = viewObject.AddComponent<CardVisual>();
            view.Initialize(model.transform);
            return view;
        }

        private IEnumerator AdvanceAfterResult()
        {
            yield return new WaitForSeconds(resultDisplaySeconds);
            ClearCards();
            loop?.AdvanceToNextHand();
        }

        private void ClearCards()
        {
            foreach (CardVisual card in playerCards)
            {
                Destroy(card.gameObject);
            }

            foreach (CardVisual card in dealerCards)
            {
                Destroy(card.gameObject);
            }

            playerCards.Clear();
            dealerCards.Clear();
        }

        private void OnDestroy()
        {
            if (loop == null)
            {
                return;
            }

            loop.CardDealt -= OnCardDealt;
            loop.DealerHoleCardRevealed -= OnDealerHoleCardRevealed;
        }
    }
}
