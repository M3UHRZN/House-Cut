using System.Globalization;
using Game.Core;
using Game.Economy;
using Game.Organs;
using TMPro;
using UnityEngine;

namespace Game.Presentation
{
    /// <summary>
    /// #26 / GDD §06 HUD: tek kaynak (para) + durum göstergeleri. UI, Table.unity
    /// içinde HudRunner GameObject'inin altında (HudCanvas) sahne-authored olarak
    /// durur — Hierarchy'den seçilip konum/boyut/renk/font elle ayarlanabilir. Bu
    /// script yalnızca değerleri günceller (metin/renk), pozisyona hiç dokunmaz.
    ///
    /// Gösterilenler:
    ///  - Para ($): kalıcı tek sayaç, değeri hedefe doğru yumuşak sayılır (GDD:
    ///    "her el sonucunda anime olur").
    ///  - El sonucu banner'ı: BlackjackLoopRunner.HandSettled event'iyle KAZANDIN /
    ///    KAYBETTİN / BERABERE olarak kısa süre gösterilip solar.
    ///  - Bahis / min bahis: güncel bahis (BetCheck'te ok tuşlarıyla anlık) + taban min bahis.
    ///  - El sayacı + kademe (I–V): GDD §06 kademe tablosundan türetilir.
    ///  - Organ (doom): bedende kalan / toplam organ; azaldıkça kırmızıya döner.
    /// </summary>
    public class HudRunner : MonoBehaviour
    {
        [Header("Bağımlılıklar (opsiyonel — atanmazsa sahneden bulunur)")]
        [SerializeField] private EconomyRunner economyRunner;
        [SerializeField] private BlackjackLoopRunner loopRunner;
        [SerializeField] private OrganSaleRunner organSaleRunner;

        [Header("Sahne Referansları (Hierarchy: HudCanvas altında)")]
        [SerializeField] private TextMeshProUGUI moneyText;
        [SerializeField] private TextMeshProUGUI betText;
        [SerializeField] private TextMeshProUGUI minBetText;
        [SerializeField] private TextMeshProUGUI handText;
        [SerializeField] private TextMeshProUGUI organText;
        [SerializeField] private CanvasGroup bannerGroup;
        [SerializeField] private TextMeshProUGUI bannerText;

        [Header("Zamanlama")]
        [Tooltip("Para sayacının hedefe yaklaşma hızı (yüksek = daha hızlı).")]
        [SerializeField] private float moneyLerpRate = 8f;
        [Tooltip("El sonucu banner'ının ekranda kalma süresi (sn).")]
        [SerializeField] private float bannerSeconds = 1.5f;

        [Header("Metinler")]
        [SerializeField] private string betPrefix = "BAHİS ";
        [SerializeField] private string minBetPrefix = "min ";
        [SerializeField] private string handPrefix = "TUR ";
        [SerializeField] private string tierSeparator = " · KADEME ";
        [SerializeField] private string organPrefix = "ORGAN ";
        [SerializeField] private string winPrefix = "KAZANDIN  +";
        [SerializeField] private string losePrefix = "KAYBETTİN  ";
        [SerializeField] private string drawText = "BERABERE";
        [SerializeField] private string currencySymbol = "$";

        [Header("Kademe Ayarları (GDD §06)")]
        [Tooltip("Her kademe kaç tur sürer (GDD: I 1-3, II 4-6 ... = 3).")]
        [SerializeField] private int handsPerTier = 3;
        [Tooltip("GDD §06 kademe etiketleri, sırayla (I, II, III, IV, V...).")]
        [SerializeField] private string[] tierLabels = { "I", "II", "III", "IV", "V" };
        [Tooltip("Kalan organ bu sayının altına inince doom göstergesi kırmızıya döner.")]
        [SerializeField] private int organDangerThreshold = 2;

        [Header("Renkler (dinamik durum renkleri)")]
        [SerializeField] private Color colorInk = new Color32(0xD8, 0xCB, 0xB4, 0xFF);
        [SerializeField] private Color colorAccent = new Color32(0xC9, 0xA2, 0x27, 0xFF);
        [SerializeField] private Color colorWin = new Color32(0x6B, 0x8E, 0x4E, 0xFF);
        [SerializeField] private Color colorDanger = new Color32(0xB3, 0x22, 0x1A, 0xFF);

        private static readonly NumberFormatInfo MoneyFmt =
            new NumberFormatInfo { NumberGroupSeparator = ".", NumberDecimalDigits = 0 };

        private float bannerTimer;
        private float displayedMoney;
        private bool initialized;

        private void Awake()
        {
            if (economyRunner == null) economyRunner = FindAnyObjectByType<EconomyRunner>();
            if (loopRunner == null) loopRunner = FindAnyObjectByType<BlackjackLoopRunner>();
            if (organSaleRunner == null) organSaleRunner = FindAnyObjectByType<OrganSaleRunner>();

            if (loopRunner != null)
            {
                loopRunner.HandSettled += OnHandSettled;
            }

            if (bannerGroup != null)
            {
                bannerGroup.alpha = 0f;
            }
        }

        private void OnDestroy()
        {
            if (loopRunner != null)
            {
                loopRunner.HandSettled -= OnHandSettled;
            }
        }

        private void Update()
        {
            EconomyController economy = economyRunner != null ? economyRunner.Economy : null;
            if (economy == null)
            {
                return;
            }

            int balance = economy.Balance;
            if (!initialized)
            {
                // Açılışta 0'dan sayılmasın — sayaç doğrudan mevcut bakiyeye otursun.
                displayedMoney = balance;
                initialized = true;
            }

            UpdateMoney(balance);
            UpdateBet(economy);
            UpdateHandAndTier(economy);
            UpdateOrganDoom();
            UpdateBanner();
        }

        private void OnHandSettled(int delta)
        {
            if (bannerText == null)
            {
                return;
            }

            string message = delta > 0 ? $"{winPrefix}{FormatMoney(delta)}"
                : delta < 0 ? $"{losePrefix}{FormatMoney(delta)}"
                : drawText;

            bannerText.text = message;
            bannerText.color = delta > 0 ? colorWin : delta < 0 ? colorDanger : colorAccent;
            bannerTimer = bannerSeconds;
        }

        private void UpdateMoney(int balance)
        {
            if (moneyText == null)
            {
                return;
            }

            float t = 1f - Mathf.Exp(-moneyLerpRate * Time.deltaTime);
            displayedMoney = Mathf.Lerp(displayedMoney, balance, t);
            if (Mathf.Abs(displayedMoney - balance) < 1f)
            {
                displayedMoney = balance;
            }

            moneyText.text = FormatMoney(Mathf.RoundToInt(displayedMoney));
            moneyText.color = balance <= 0 ? colorDanger : colorAccent;
        }

        private void UpdateBet(EconomyController economy)
        {
            int baseBet = economy.CurrentBaseBet;
            int bet = loopRunner != null ? loopRunner.CurrentBet : 0;
            if (bet <= 0)
            {
                bet = baseBet;
            }

            if (betText != null) betText.text = betPrefix + FormatMoney(bet);
            if (minBetText != null) minBetText.text = minBetPrefix + FormatMoney(baseBet);
        }

        private void UpdateHandAndTier(EconomyController economy)
        {
            if (handText == null)
            {
                return;
            }

            int hand = economy.HandNumber;
            // GDD §06: I 1–3, II 4–6, III 7–9, IV 10–12, V 13+ (handsPerTier=3 varsayılan).
            int tierIndex = tierLabels != null && tierLabels.Length > 0
                ? Mathf.Clamp((hand - 1) / Mathf.Max(1, handsPerTier), 0, tierLabels.Length - 1)
                : 0;
            string tierLabel = tierLabels != null && tierLabels.Length > 0 ? tierLabels[tierIndex] : "?";
            handText.text = $"{handPrefix}{hand}{tierSeparator}{tierLabel}";
        }

        private void UpdateOrganDoom()
        {
            if (organText == null)
            {
                return;
            }

            OrganManager organs = organSaleRunner != null ? organSaleRunner.OrganManager : null;
            if (organs == null)
            {
                organText.text = string.Empty;
                return;
            }

            int total = organs.Organs.Count;
            int remaining = total - organs.SoldOrgans.Count;
            organText.text = $"{organPrefix}{remaining}/{total}";
            organText.color = remaining <= organDangerThreshold ? colorDanger : colorInk;
        }

        private void UpdateBanner()
        {
            if (bannerGroup == null)
            {
                return;
            }

            if (bannerTimer <= 0f)
            {
                if (bannerGroup.alpha != 0f) bannerGroup.alpha = 0f;
                return;
            }

            bannerTimer -= Time.deltaTime;
            // Son ~%40'ta yumuşak sol; öncesinde tam görünür.
            bannerGroup.alpha = Mathf.Clamp01(bannerTimer / (bannerSeconds * 0.4f));
        }

        private string FormatMoney(int value)
        {
            string sign = value < 0 ? "-" : string.Empty;
            return sign + currencySymbol + Mathf.Abs(value).ToString("N0", MoneyFmt);
        }
    }
}
