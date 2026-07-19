using System.Collections.Generic;
using Game.Core;
using Game.Data;
using Game.Economy;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Organs
{
    /// <summary>
    /// Scene-visible host for the "beden defteri" (organ sale) screen — GDD
    /// §04 Organ Satışı. Reads the single shared OrganManager/Bank off
    /// EconomyRunner.Economy — the same instance BlackjackLoop, CriticalStateRunner
    /// and EffectStack/LungTimerEffect all observe — so a sale here is visible
    /// everywhere else in the scene (GDD §08). Shows itself when GameFlow enters
    /// OrganSale; SAT confirms the selection and drives GameFlow into Extraction.
    /// </summary>
    public class OrganSaleRunner : MonoBehaviour
    {
        public static readonly Color BaseColor = new Color32(0x12, 0x0D, 0x09, 0xFF);
        public static readonly Color PanelColor = new Color32(0x24, 0x1B, 0x14, 0xFF);
        public static readonly Color InkColor = new Color32(0xD8, 0xCB, 0xB4, 0xFF);
        public static readonly Color IdleBorderColor = new Color32(0x8A, 0x6A, 0x3B, 0xFF);
        public static readonly Color SelectedColor = new Color32(0xC9, 0xA2, 0x27, 0xFF);
        public static readonly Color BuffColor = new Color32(0x6B, 0x8E, 0x4E, 0xFF);
        public static readonly Color DebuffColor = new Color32(0xB3, 0x22, 0x1A, 0xFF);
        public static readonly Color SoldColor = new Color32(0x4A, 0x40, 0x38, 0xFF);

        [SerializeField] private GameConfigProvider configProvider;
        [SerializeField] private GameFlowRunner flowRunner;
        [SerializeField] private EconomyRunner economyRunner;
        [SerializeField] private OrganData[] organs;

        [Header("UI")]
        [SerializeField] private GameObject root;
        [SerializeField] private RectTransform gridContainer;
        [SerializeField] private OrganTileView tileTemplate;
        [SerializeField] private TMP_Text warningText;
        [SerializeField] private TMP_Text totalText;
        [SerializeField] private Button satButton;
        [SerializeField] private GameObject tooltipEmptyState;
        [SerializeField] private GameObject tooltipFilledState;
        [SerializeField] private TMP_Text tooltipNameText;
        [SerializeField] private TMP_Text tooltipValueText;
        [SerializeField] private TMP_Text tooltipDebuffText;
        [SerializeField] private TMP_Text tooltipBuffText;
        [SerializeField] private TMP_Text satButtonLabel;

        private readonly List<OrganTileView> tiles = new();
        private Bank bank;
        private OrganData focusedOrgan;

        public OrganManager OrganManager { get; private set; }
        public OrganSaleController Controller { get; private set; }

        /// <summary>
        /// The bet the selection must cover. Defaults to GameConfig.minBet;
        /// a future issue that wires EconomyController can drive this from
        /// EconomyController.RequiredBet instead.
        /// </summary>
        public int RequiredBet { get; set; }

        private void Awake()
        {
            if (satButton != null)
            {
                satButton.onClick.AddListener(OnSatClicked);
            }

            Hide();
        }

        private void Start()
        {
            if (economyRunner == null)
            {
                economyRunner = FindAnyObjectByType<EconomyRunner>();
            }

            if (economyRunner == null || economyRunner.Economy == null)
            {
                Debug.LogError($"{nameof(OrganSaleRunner)}: Oyunun EconomyRunner bileşeni bulunamadı.", this);
                enabled = false;
                return;
            }

            bank = economyRunner.Economy.MoneyBank;
            OrganManager = economyRunner.Economy.OrganManager;
            Controller = new OrganSaleController(OrganManager, bank);
            RequiredBet = economyRunner.Economy.RequiredBet;

            BuildTiles();
            HideTooltip();

            // Subscribe in Start so GameFlowRunner.Awake has already created
            // Flow (Awake order between components is undefined).
            if (flowRunner != null && flowRunner.Flow != null)
            {
                flowRunner.Flow.StateChanged += OnStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (flowRunner != null && flowRunner.Flow != null)
            {
                flowRunner.Flow.StateChanged -= OnStateChanged;
            }

        }

        private void OnStateChanged(GameState previous, GameState next)
        {
            if (next == GameState.OrganSale)
            {
                RequiredBet = economyRunner.Economy.RequiredBet;
                Show();
            }
        }

        public void Show()
        {
            if (root != null)
            {
                root.SetActive(true);
            }

            RefreshAll();
        }

        public void Hide()
        {
            if (root != null)
            {
                root.SetActive(false);
            }
        }

        public void ToggleOrgan(OrganData organ)
        {
            Controller.ToggleSelection(organ);
            focusedOrgan = organ;
            ShowTooltip(organ);
            RefreshAll();
        }

        public void ShowTooltip(OrganData organ)
        {
            if (tooltipNameText != null)
            {
                tooltipNameText.text = organ.displayName;
            }

            if (tooltipValueText != null)
            {
                tooltipValueText.gameObject.SetActive(false);
            }

            if (tooltipDebuffText != null)
            {
                tooltipDebuffText.text = BuildDescription(organ);
            }

            if (tooltipBuffText != null)
            {
                tooltipBuffText.gameObject.SetActive(false);
            }
        }

        public void HideTooltip()
        {
            // The selection panel stays visible at all times so the player
            // understands where the clicked organ's details will appear.
            if (tooltipNameText != null)
            {
                tooltipNameText.text = "BİR ORGAN SEÇ";
            }

            if (tooltipDebuffText != null)
            {
                tooltipDebuffText.text = "Bir organ seç. Bedeli burada görünecek; satış fiyatı SAT butonunda yazacak.";
            }
        }

        private static string BuildDescription(OrganData organ)
        {
            string debuff = string.IsNullOrWhiteSpace(organ.debuffDescription)
                ? "Bu organın belirgin bir bedeli yok."
                : organ.debuffDescription;

            if (string.IsNullOrWhiteSpace(organ.buffDescription))
            {
                return debuff;
            }

            return $"{debuff}\n\nKARŞILIK: {organ.buffDescription}";
        }

        private void BuildTiles()
        {
            if (tileTemplate == null || gridContainer == null || organs == null)
            {
                return;
            }

            tileTemplate.gameObject.SetActive(false);

            foreach (var organ in organs)
            {
                if (organ == null)
                {
                    continue;
                }

                OrganTileView instance = Instantiate(tileTemplate, gridContainer);
                instance.gameObject.SetActive(true);
                instance.Bind(organ, this);
                tiles.Add(instance);
            }
        }

        private void RefreshAll()
        {
            var available = new HashSet<OrganData>(Controller.AvailableOrgans);
            foreach (var tile in tiles)
            {
                bool visible = available.Contains(tile.Organ) || OrganManager.IsSold(tile.Organ);
                tile.gameObject.SetActive(visible);
                if (visible)
                {
                    tile.Refresh();
                }
            }

            if (warningText != null)
            {
                warningText.text = $"PARA YETMİYOR — min bahis: ${RequiredBet}";
            }

            if (totalText != null)
            {
                int projected = bank.Balance + Controller.SelectedTotal;
                totalText.text =
                    $"Seçili: ${Controller.SelectedTotal}  →  Yeni bakiye: ${projected} / gereken: ${RequiredBet}";
            }

            if (satButton != null)
            {
                satButton.interactable = Controller.CanConfirm(RequiredBet);
            }

            if (satButtonLabel != null)
            {
                int selectedTotal = Controller.SelectedTotal;
                satButtonLabel.text = selectedTotal > 0 ? $"SAT  ·  ${selectedTotal}" : "SAT";
            }
        }

        private void OnSatClicked()
        {
            int total = Controller.Confirm(RequiredBet);
            Debug.Log($"[BedenDefteri] Organ satışı: +${total}, yeni bakiye ${bank.Balance}");

            if (flowRunner != null && flowRunner.Flow != null)
            {
                flowRunner.Flow.SignalTransitionReady();
                GameState nextState = OrganManager.IsGameOver ? GameState.GameOver : GameState.Extraction;
                if (!flowRunner.Flow.TryTransition(nextState))
                {
                    Debug.LogWarning($"[BedenDefteri] GameFlow {nextState} geçişi başarısız (state uyumsuz).");
                }
            }

            RefreshAll();
            Hide();
        }
    }
}
