using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
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

        [Header("Card Layout Gizmos")]
        [SerializeField] private bool showCardGizmos = true;
        [SerializeField, Range(1, 10)] private int gizmoPreviewCardCount = 6;

        [Header("Animation")]
        [SerializeField] private DeckVisual deckVisual;
        [SerializeField, Min(0f)] private float dealDuration = 0.35f;
        [SerializeField, Min(0f)] private float flipDuration = 0.4f;
        [SerializeField, Min(0f)] private float collectDuration = 0.3f;
        [SerializeField] private Ease dealEase = Ease.OutQuad;
        [SerializeField] private Ease flipEase = Ease.InOutQuad;
        [SerializeField] private Ease collectEase = Ease.InQuad;

        private readonly List<CardVisual> playerCards = new();
        private readonly List<CardVisual> dealerCards = new();
        private readonly AnimationQueue animQueue = new();
        private BlackjackLoop loop;
        private string handResultMessage;
        private Color handResultColor;
        private bool isShowingHandResult;

        private void Awake()
        {
            if (configProvider == null || configProvider.Config == null || flowRunner == null || economyRunner == null ||
                cardPrefabCatalog == null || playerCardAnchor == null || dealerCardAnchor == null)
            {
                Debug.LogError($"{nameof(BlackjackLoopRunner)}: Config, akış, ekonomi, kart kataloğu ve Player/Dealer anchor atanmalı.", this);
                enabled = false;
            }

            if (deckVisual == null)
            {
                Debug.LogWarning($"{nameof(BlackjackLoopRunner)}: DeckVisual atanmadı; kartlar deste animasyonu olmadan dağıtılacak.", this);
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
            loop.HandSettled += OnHandSettled;

            yield return null;
            loop.Begin();
        }

        private void Update()
        {
            // Duraklatma menüsü açıkken (PauseController Time.timeScale=0 yapar) oyun
            // döngüsü ve klavye girdisi işlenmesin — yoksa menü arkasında el çekileb/
            // durulabilir ve karar zamanlayıcısı yanlış ilerlerdi.
            if (Time.timeScale == 0f)
            {
                return;
            }

            loop?.Tick(Time.deltaTime);

            // Organ satışı (market) / çıkarma sekansı ekrandayken klavye girdisi
            // işlenmesin. Her aksiyonun kendi state kontrolü zaten no-op yapıyor ama
            // ok tuşları yine de "Bahis: $..." log'u basıyordu ve market açıkken
            // klavyenin hiç tepki vermemesi beklenir.
            if (loop != null && (loop.State == GameState.OrganSale || loop.State == GameState.Extraction))
            {
                return;
            }

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
                if (animQueue.IsIdle)
                {
                    Hit();
                }
            }
            else if (keyboard.spaceKey.wasPressedThisFrame)
            {
                if (animQueue.IsIdle)
                {
                    Stand();
                }
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

        public static string FormatHandResult(int delta)
        {
            if (delta > 0)
            {
                return $"KAZANDIN  +${delta}";
            }

            if (delta < 0)
            {
                return $"KAYBETTİN  -${-delta}";
            }

            return "BERABERE";
        }

        private void OnHandSettled(int delta)
        {
            animQueue.WhenIdle(() => ShowHandResult(delta));
        }

        private void ShowHandResult(int delta)
        {
            handResultMessage = FormatHandResult(delta);
            handResultColor = delta > 0
                ? new Color(0.35f, 1f, 0.45f)
                : delta < 0
                    ? new Color(1f, 0.3f, 0.25f)
                    : new Color(1f, 0.85f, 0.3f);
            isShowingHandResult = true;
            StartCoroutine(AdvanceAfterResult());
        }

        private void OnCardDealt(Card card, bool isDealer, bool faceUp)
        {
            bool isFirstCardOfHand = playerCards.Count == 0 && dealerCards.Count == 0;
            if (isFirstCardOfHand && deckVisual != null)
            {
                animQueue.Enqueue(() => deckVisual.PlaceOnTable());
            }

            List<CardVisual> views = isDealer ? dealerCards : playerCards;
            Transform anchor = isDealer ? dealerCardAnchor : playerCardAnchor;
            Vector3 targetPosition = CardPlacement.GetPosition(anchor, views.Count, cardSpacing);
            Quaternion targetRotation = anchor.rotation;
            Vector3 spawnPosition = deckVisual != null ? deckVisual.RestTopSpawnPosition : targetPosition;

            CardVisual view = CreateCardView(card, isDealer, spawnPosition, targetRotation);
            view.SetCard(card, false); // spawns face-down at the deck; revealed on arrival
            views.Add(view);

            animQueue.Enqueue(() =>
                view.AnimateDealFrom(spawnPosition, targetPosition, targetRotation, dealDuration, dealEase,
                    () => view.SetCard(card, faceUp)));
        }

        private void OnDealerHoleCardRevealed(Card card)
        {
            if (dealerCards.Count > 1)
            {
                CardVisual holeCard = dealerCards[1];
                animQueue.Enqueue(() => holeCard.FlipTo(true, flipDuration, flipEase));
            }
        }

        private CardVisual CreateCardView(Card card, bool isDealer, Vector3 spawnPosition, Quaternion spawnRotation)
        {
            Transform parent = cardRoot != null ? cardRoot : transform;
            GameObject prefab = cardPrefabCatalog.GetPrefab(card);

            if (prefab == null)
            {
                Debug.LogWarning($"{nameof(BlackjackLoopRunner)}: {card} için prefab bulunamadı; placeholder kullanılıyor.", this);
                GameObject cardObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
                cardObject.transform.SetParent(parent, false);
                cardObject.transform.localScale = new Vector3(1.2f, 0.08f, 1.8f);
                cardObject.transform.SetPositionAndRotation(spawnPosition, spawnRotation);
                return cardObject.AddComponent<CardVisual>();
            }

            var viewObject = new GameObject($"{(isDealer ? "Dealer" : "Player")}_{card}");
            viewObject.transform.SetParent(parent, false);
            viewObject.transform.SetPositionAndRotation(spawnPosition, spawnRotation);

            GameObject model = Instantiate(prefab, viewObject.transform);
            model.transform.localPosition = Vector3.zero;
            model.transform.localScale *= cardScale;

            // Kartları "Cards" layer'ına al: PS2 pikselasyon tam-ekran efekti bu
            // layer'ı hariç tutup (renderer'daki Render Objects feature'ı ile) kartları
            // net çizecek, böylece kart yüzleri okunur kalır. Model instantiate
            // edildikten SONRA, tüm çocuklar dahil ata.
            AssignCardsLayer(viewObject);

            CardVisual view = viewObject.AddComponent<CardVisual>();
            view.Initialize(model.transform);
            return view;
        }

        // "Cards" layer'ı Project Settings'te tanımlı değilse (-1) sessizce atla.
        private static void AssignCardsLayer(GameObject root)
        {
            int cardsLayer = LayerMask.NameToLayer("Cards");
            if (cardsLayer < 0)
            {
                return;
            }

            SetLayerRecursively(root, cardsLayer);
        }

        private static void SetLayerRecursively(GameObject go, int layer)
        {
            go.layer = layer;
            foreach (Transform child in go.transform)
            {
                SetLayerRecursively(child.gameObject, layer);
            }
        }

        private IEnumerator AdvanceAfterResult()
        {
            yield return new WaitForSeconds(resultDisplaySeconds);
            isShowingHandResult = false;

            Vector3 deckTop = deckVisual != null ? deckVisual.RestTopSpawnPosition : transform.position;
            EnqueueCollect(playerCards, deckTop);
            EnqueueCollect(dealerCards, deckTop);

            animQueue.WhenIdle(() =>
            {
                ClearCards();
                loop?.AdvanceToNextHand();
            });
        }

        private void EnqueueCollect(List<CardVisual> cards, Vector3 deckTop)
        {
            foreach (CardVisual card in cards)
            {
                CardVisual capturedCard = card;
                animQueue.Enqueue(() => capturedCard.AnimateCollectTo(deckTop, collectDuration, collectEase));
            }
        }

        private void OnGUI()
        {
            if (!isShowingHandResult)
            {
                return;
            }

            float width = Mathf.Min(620f, Screen.width * 0.7f);
            float height = Mathf.Max(90f, Screen.height * 0.14f);
            var panel = new Rect((Screen.width - width) * 0.5f, Screen.height * 0.12f, width, height);

            Color previousColor = GUI.color;
            GUI.color = new Color(0.02f, 0.015f, 0.01f, 0.88f);
            GUI.Box(panel, GUIContent.none);
            GUI.color = previousColor;

            var style = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.Clamp(Mathf.RoundToInt(Screen.height * 0.055f), 28, 64),
                fontStyle = FontStyle.Bold
            };
            style.normal.textColor = handResultColor;
            GUI.Label(panel, handResultMessage, style);
        }

        private void ClearCards()
        {
            // Cards already swept into the deck by AnimateCollectTo self-destroy on arrival;
            // the Unity "is this destroyed" equality check keeps this loop safe either way.
            foreach (CardVisual card in playerCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            foreach (CardVisual card in dealerCards)
            {
                if (card != null)
                {
                    Destroy(card.gameObject);
                }
            }

            playerCards.Clear();
            dealerCards.Clear();
        }

        private void OnDestroy()
        {
            animQueue.KillAll();

            if (loop == null)
            {
                return;
            }

            loop.CardDealt -= OnCardDealt;
            loop.DealerHoleCardRevealed -= OnDealerHoleCardRevealed;
            loop.HandSettled -= OnHandSettled;
        }

        private void OnDrawGizmos()
        {
            if (!showCardGizmos)
            {
                return;
            }

            DrawHandGizmos(playerCardAnchor, new Color(0.15f, 1f, 0.35f, 0.9f), "PLAYER");
            DrawHandGizmos(dealerCardAnchor, new Color(1f, 0.25f, 0.2f, 0.9f), "DEALER");
        }

        private void DrawHandGizmos(Transform anchor, Color color, string label)
        {
            if (anchor == null)
            {
                return;
            }

            Gizmos.color = color;
            Gizmos.DrawSphere(anchor.position, 0.04f);

            Vector3 cardSize = CardPlacement.GetGizmoSize(cardScale);
            Vector3 previousPosition = anchor.position;

            for (int i = 0; i < gizmoPreviewCardCount; i++)
            {
                Vector3 position = CardPlacement.GetPosition(anchor, i, cardSpacing);
                Gizmos.matrix = Matrix4x4.TRS(position, anchor.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, cardSize);
                Gizmos.matrix = Matrix4x4.identity;

                if (i > 0)
                {
                    Gizmos.DrawLine(previousPosition, position);
                }

                previousPosition = position;
            }

#if UNITY_EDITOR
            UnityEditor.Handles.color = color;
            UnityEditor.Handles.Label(anchor.position + Vector3.up * 0.08f, $"{label}  •  {cardSpacing:0.00}m");
#endif
        }
    }
}
