using Game.Cards;
using Game.Core;
using Game.Data;
using UnityEngine;

namespace Game.Economy
{
    /// <summary>
    /// Play-mode sandbox for the money economy. Drop this on any GameObject and
    /// press Play: an on-screen (IMGUI) panel lets you adjust the bet and simulate
    /// win / loss / blackjack outcomes so you can watch Balance, the ramping base
    /// bet and the Deferment token behave live — without a dealer, deck or UI
    /// (those are out of scope). Zero setup: if no GameConfigProvider is assigned
    /// it spins up a default GameConfig at runtime. Tooling only, not shipped loop.
    /// </summary>
    public class EconomyDemoHarness : MonoBehaviour
    {
        [SerializeField] private GameConfigProvider configProvider;

        private GameConfig runtimeConfig;
        private EconomyController economy;
        private BetSelector selector;
        private int handNumber = 1;
        private string lastResult = "-";

        private void Awake()
        {
            GameConfig config = configProvider != null ? configProvider.Config : null;
            if (config == null)
            {
                runtimeConfig = ScriptableObject.CreateInstance<GameConfig>();
                config = runtimeConfig;
            }

            economy = new EconomyController(config);
            selector = economy.BeginHand();
        }

        private void OnDestroy()
        {
            if (runtimeConfig != null)
            {
                Destroy(runtimeConfig);
            }
        }

        private void OnGUI()
        {
            GUILayout.BeginArea(new Rect(20, 20, 360, 430), GUI.skin.box);

            GUILayout.Label("ANTE / MORTEM — Ekonomi Demo");
            GUILayout.Space(6);
            GUILayout.Label($"El no        : {handNumber}");
            GUILayout.Label($"Para         : ${economy.Balance}");
            GUILayout.Label($"Taban bahis  : ${economy.CurrentBaseBet}");
            GUILayout.Label($"Seçili bahis : ${selector.Current}");
            GUILayout.Label($"Erteleme     : {(economy.HasDeferment ? "VAR" : "yok")}");
            GUILayout.Label(economy.HasFundsForBaseBet
                ? "Durum        : oynanabilir"
                : "Durum        : PARA YETMİYOR (organ fazı sınırı)");
            GUILayout.Label($"Son sonuç    : {lastResult}");

            GUILayout.Space(10);
            GUILayout.Label("Bahis ayarı (min bahis ↔ para):");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Bahis -50"))
            {
                selector.Decrease();
            }
            if (GUILayout.Button("Bahis +50"))
            {
                selector.Increase();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label("El sonucu (simüle):");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Kazan"))
            {
                Settle(win: true, blackjack: false);
            }
            if (GUILayout.Button("Kaybet"))
            {
                Settle(win: false, blackjack: false);
            }
            if (GUILayout.Button("Blackjack"))
            {
                Settle(win: true, blackjack: true);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            if (GUILayout.Button("Ertelemeyi kullan"))
            {
                economy.UseDeferment();
            }
            if (GUILayout.Button("Sonraki el"))
            {
                NextHand();
            }

            GUILayout.EndArea();
        }

        private void Settle(bool win, bool blackjack)
        {
            economy.LockBet();

            Hand player;
            if (blackjack)
            {
                player = HandOf((Suit.Spades, Rank.Ace), (Suit.Hearts, Rank.King));   // 21 in two cards
            }
            else if (win)
            {
                player = HandOf((Suit.Spades, Rank.Ten), (Suit.Hearts, Rank.King));   // 20
            }
            else
            {
                player = HandOf((Suit.Spades, Rank.Ten), (Suit.Hearts, Rank.Nine));   // 19
            }

            Hand dealer = win
                ? HandOf((Suit.Clubs, Rank.Ten), (Suit.Diamonds, Rank.Nine))          // 19
                : HandOf((Suit.Clubs, Rank.Ten), (Suit.Diamonds, Rank.King));         // 20

            int delta = economy.SettleHand(player, dealer);
            lastResult = $"{(delta >= 0 ? "+" : "")}{delta} (bahis ${economy.LockedBet}) → para ${economy.Balance}";
            Debug.Log($"[EkonomiDemo] El {handNumber}: {lastResult}, erteleme={(economy.HasDeferment ? "VAR" : "yok")}");
        }

        private void NextHand()
        {
            economy.AdvanceToNextHand();
            handNumber++;
            selector = economy.BeginHand();
            Debug.Log($"[EkonomiDemo] Sonraki el → el {handNumber}, taban bahis ${economy.CurrentBaseBet}");
        }

        private static Hand HandOf(params (Suit suit, Rank rank)[] cardSpecs)
        {
            var hand = new Hand();
            foreach (var (suit, rank) in cardSpecs)
            {
                hand.AddCard(new Card(suit, rank));
            }

            return hand;
        }
    }
}
