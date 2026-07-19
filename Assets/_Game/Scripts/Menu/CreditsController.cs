using UnityEngine;
using UnityEngine.UI;

namespace Game.Menu
{
    /// <summary>
    /// Emek verenlerin isim listesini kurar: her entry için sahnede authored duran
    /// RowTemplate'in (NamesContainer altında, Hierarchy'den elle konum/boyut/renk/
    /// font ayarlanabilir) bir kopyasını (Instantiate) üretip CreditRowView.Bind()
    /// ile doldurur. İsim eklemek/çıkarmak için sahneye dokunmaya gerek yok, sadece
    /// Inspector'daki entries dizisini büyüt/küçült.
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
        [SerializeField] private CreditRowView rowTemplate;
        [SerializeField]
        private CreditEntry[] entries =
        {
            new CreditEntry { personName = "Metin Enes Ufuk", linkLabel = "GitHub", url = "https://github.com/M3UHRZN/House-Cut" },
        };

        [SerializeField] private Button githubButton;

        private void Awake()
        {
            BuildNameRows();

            // Eski tek büyük "GitHub" butonu artık kullanılmıyor — her satırın kendi linki var.
            if (githubButton != null)
            {
                githubButton.gameObject.SetActive(false);
            }
        }

        private void BuildNameRows()
        {
            if (namesContainer == null || rowTemplate == null || entries == null)
            {
                return;
            }

            foreach (var entry in entries)
            {
                var row = Instantiate(rowTemplate, namesContainer);
                row.gameObject.SetActive(true);
                row.Bind(entry.personName, entry.linkLabel, entry.url);
            }
        }
    }
}
