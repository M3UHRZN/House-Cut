using System.Collections;
using System.Collections.Generic;
using Game.Cards;
using Game.Economy;
using UnityEngine;

namespace Game.Core
{
    public class BlackjackLoopRunner : MonoBehaviour
    {
        [SerializeField] private GameConfigProvider configProvider;
        [SerializeField] private GameFlowRunner flowRunner;
        [SerializeField] private EconomyRunner economyRunner;
        [SerializeField] private Transform cardRoot;
        [SerializeField] private GameObject cardPrefab;
        [SerializeField] private Transform[] playerSlots;
        [SerializeField] private Transform[] dealerSlots;
        [SerializeField, Min(0f)] private float resultDisplaySeconds = 1.5f;

        private readonly List<CardVisual> playerCards = new();
        private readonly List<CardVisual> dealerCards = new();
        private BlackjackLoop loop;

        private void Awake()
        {
            if (configProvider == null || configProvider.Config == null || flowRunner == null || economyRunner == null)
            {
                Debug.LogError($"{nameof(BlackjackLoopRunner)}: Config, GameFlowRunner ve EconomyRunner atanmalı.", this);
                enabled = false;
                return;
            }

            loop = new BlackjackLoop(flowRunner.Flow, economyRunner.Economy, new Deck(), configProvider.Config.decisionTime);
            loop.CardDealt += OnCardDealt;
            loop.DealerHoleCardRevealed += OnDealerHoleCardRevealed;
            loop.HandSettled += _ => StartCoroutine(AdvanceAfterResult());
        }

        private IEnumerator Start()
        {
            yield return null;
            loop?.Begin();
        }

        private void Update()
        {
            loop?.Tick(Time.deltaTime);

            if (Input.GetKeyDown(KeyCode.Return))
            {
                ConfirmBet();
            }
            else if (Input.GetKeyDown(KeyCode.H))
            {
                Hit();
            }
            else if (Input.GetKeyDown(KeyCode.Space))
            {
                Stand();
            }
            else if (Input.GetKeyDown(KeyCode.UpArrow))
            {
                RaiseBet();
            }
            else if (Input.GetKeyDown(KeyCode.DownArrow))
            {
                LowerBet();
            }
            else if (Input.GetKeyDown(KeyCode.D))
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
            Transform[] slots = isDealer ? dealerSlots : playerSlots;
            CardVisual view = CreateCardView(views.Count, slots, isDealer);
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

        private CardVisual CreateCardView(int index, Transform[] slots, bool isDealer)
        {
            Transform parent = cardRoot != null ? cardRoot : transform;
            GameObject cardObject = cardPrefab != null
                ? Instantiate(cardPrefab, parent)
                : GameObject.CreatePrimitive(PrimitiveType.Cube);

            if (cardPrefab == null)
            {
                cardObject.transform.SetParent(parent, false);
                cardObject.transform.localScale = new Vector3(1.2f, 0.08f, 1.8f);
            }

            Transform slot = slots != null && index < slots.Length ? slots[index] : null;
            if (slot != null)
            {
                cardObject.transform.SetPositionAndRotation(slot.position, slot.rotation);
            }
            else
            {
                float row = isDealer ? 1.2f : -1.2f;
                cardObject.transform.localPosition = new Vector3((index - 1) * 1.4f, 0.1f, row);
                cardObject.transform.localRotation = Quaternion.identity;
            }

            return cardObject.GetComponent<CardVisual>() ?? cardObject.AddComponent<CardVisual>();
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
