using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Menu
{
    /// <summary>
    /// Emek verenlerin isim listesini çalışma zamanında satır satır oluşturur —
    /// her satır isim + yanında tıklanabilir link olarak dizilir. İsim eklemek/çıkarmak
    /// için sahneye dokunmaya gerek yok, sadece Inspector'daki diziyi büyüt/küçült.
    /// </summary>
    public class CreditsController : MonoBehaviour
    {
        [System.Serializable]
        private class CreditEntry
        {
            public string personName = "İsim";
            public string linkLabel = "GitHub";
            public string url = "https://github.com/M3UHRZN/House-Cut";
        }

        [SerializeField] private RectTransform namesContainer;
        [SerializeField]
        private CreditEntry[] entries =
        {
            new CreditEntry { personName = "Metin Enes Ufuk", linkLabel = "GitHub", url = "https://github.com/M3UHRZN/House-Cut" },
        };
        [SerializeField] private TMP_FontAsset nameFont;
        [SerializeField] private Color nameColor = new Color(0.847f, 0.796f, 0.706f);
        [SerializeField] private Color linkColor = new Color(0.788f, 0.635f, 0.153f);
        [SerializeField] private float nameFontSize = 28f;
        [SerializeField] private float rowHeight = 48f;
        [SerializeField] private float rowSpacing = 18f;

        [SerializeField] private Button githubButton;
        [SerializeField] private float nameLinkGap = 40f;
        [SerializeField] private TextAnchor rowAlignment = TextAnchor.MiddleLeft;
        [SerializeField] private float nameColumnWidth = 280f;

        private void Awake()
        {
            // Sahnede baked olan VerticalLayoutGroup spacing'i çok sıkıydı (10px) —
            // satırlar birbirine yapışık duruyordu, burada override ediyoruz.
            if (namesContainer != null)
            {
                var containerLayout = namesContainer.GetComponent<VerticalLayoutGroup>();
                if (containerLayout != null)
                {
                    containerLayout.spacing = rowSpacing;
                }
            }

            BuildNameRows();

            // Eski tek büyük "GitHub" butonu artık kullanılmıyor — her satırın kendi linki var.
            if (githubButton != null)
            {
                githubButton.gameObject.SetActive(false);
            }
        }

        private void BuildNameRows()
        {
            if (namesContainer == null || entries == null)
            {
                return;
            }

            foreach (var entry in entries)
            {
                BuildRow(entry);
            }
        }

        private void BuildRow(CreditEntry entry)
        {
            var rowObject = new GameObject($"Name_{entry.personName}", typeof(RectTransform));
            rowObject.transform.SetParent(namesContainer, false);
            var rowRect = (RectTransform)rowObject.transform;
            rowRect.sizeDelta = new Vector2(0f, rowHeight);

            // Genişlikleri elle hesaplamak yerine Unity'nin kendi layout sistemine
            // bırakıyoruz — TMP_Text, ILayoutElement'i doğru şekilde uygular ve
            // font/atlas hazır olduğunda gerçek metin genişliğini kullanır.
            var rowLayout = rowObject.AddComponent<HorizontalLayoutGroup>();
            rowLayout.childAlignment = rowAlignment;
            rowLayout.spacing = nameLinkGap;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = true;
            rowLayout.childControlWidth = true;
            rowLayout.childControlHeight = true;

            var nameText = CreateLabel(rowRect, "NameLabel", entry.personName, nameColor);
            nameText.alignment = TextAlignmentOptions.MidlineRight;

            // Sabit sütun genişliği: isim uzunluğu ne olursa olsun link her satırda
            // aynı x konumunda başlasın diye TMP'nin kendi otomatik genişliğini eziyoruz.
            var nameLayoutElement = nameText.gameObject.AddComponent<LayoutElement>();
            nameLayoutElement.preferredWidth = nameColumnWidth;
            nameLayoutElement.flexibleWidth = 0f;

            var linkText = CreateLabel(rowRect, "LinkLabel", $"<u>{entry.linkLabel}</u>", linkColor);
            linkText.raycastTarget = true;

            var linkButton = linkText.gameObject.AddComponent<Button>();
            linkButton.targetGraphic = linkText;
            linkButton.onClick.AddListener(() => Application.OpenURL(entry.url));
        }

        private TextMeshProUGUI CreateLabel(RectTransform parent, string objectName, string content, Color color)
        {
            var go = new GameObject(objectName, typeof(RectTransform));
            go.transform.SetParent(parent, false);

            var text = go.AddComponent<TextMeshProUGUI>();
            text.text = content;
            text.alignment = TextAlignmentOptions.Midline;
            text.fontSize = nameFontSize;
            text.color = color;
            text.raycastTarget = false;
            text.enableWordWrapping = false;
            text.overflowMode = TextOverflowModes.Overflow;
            if (nameFont != null)
            {
                text.font = nameFont;
            }

            return text;
        }
    }
}
