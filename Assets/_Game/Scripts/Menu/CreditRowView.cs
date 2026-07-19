using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Menu
{
    /// <summary>
    /// Tek bir "Emek Verenler" satırı (isim solda + link sağda, sabit sütun
    /// hizalı). Sahnede bir kez authored template olarak durur (RowTemplate,
    /// NamesContainer altında, inaktif) — CreditsController her entry için bunun
    /// bir kopyasını (Instantiate) üretip Bind() ile doldurur. Konum/boyut/renk/
    /// font gibi her şey template'in kendi Hierarchy objesinden elle ayarlanabilir.
    /// </summary>
    public class CreditRowView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private TextMeshProUGUI linkLabel;
        [SerializeField] private Button linkButton;

        public void Bind(string personName, string linkText, string url)
        {
            if (nameLabel != null)
            {
                nameLabel.text = personName;
            }

            if (linkLabel != null)
            {
                linkLabel.text = $"<u>{linkText}</u>";
            }

            if (linkButton != null)
            {
                linkButton.onClick.RemoveAllListeners();
                linkButton.onClick.AddListener(() => Application.OpenURL(url));
            }

            gameObject.name = $"Name_{personName}";
        }
    }
}
