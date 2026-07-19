using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Menu
{
    /// <summary>
    /// Sahnedeki tüm Button'ları tarayıp üzerlerine gelince/hover durumuna göre
    /// donanım imlecini (Cursor.SetCursor) değiştirir. Normal: taş uç, Hover: kemik el.
    /// Yeni butonlar sahneye eklendiğinde otomatik yakalanır (Start'ta taranır).
    /// </summary>
    public class MenuCursorController : MonoBehaviour
    {
        [SerializeField] private Texture2D normalCursor;
        [SerializeField] private Texture2D hoverCursor;
        [SerializeField] private Vector2 normalHotspot = Vector2.zero;
        [SerializeField] private Vector2 hoverHotspot = Vector2.zero;

        private void Awake()
        {
            ApplyCursor(normalCursor, normalHotspot);
        }

        private void Start()
        {
            // CreditsController gibi bazı butonlar (isim satırlarındaki link butonları)
            // kendi Awake'inde çalışma zamanında oluşturuluyor — bu yüzden Start'ta tarıyoruz,
            // tüm Awake'ler bittikten sonra çalışır.
            var buttons = FindObjectsByType<Button>(FindObjectsInactive.Include);
            foreach (var button in buttons)
            {
                AttachHoverEvents(button.gameObject);
            }
        }

        private void AttachHoverEvents(GameObject target)
        {
            var trigger = target.GetComponent<EventTrigger>();
            if (trigger == null)
            {
                trigger = target.AddComponent<EventTrigger>();
            }

            AddEntry(trigger, EventTriggerType.PointerEnter, () => ApplyCursor(hoverCursor, hoverHotspot));
            AddEntry(trigger, EventTriggerType.PointerExit, () => ApplyCursor(normalCursor, normalHotspot));
        }

        private static void AddEntry(EventTrigger trigger, EventTriggerType type, UnityEngine.Events.UnityAction action)
        {
            var entry = new EventTrigger.Entry { eventID = type };
            entry.callback.AddListener(_ => action());
            trigger.triggers.Add(entry);
        }

        private static void ApplyCursor(Texture2D texture, Vector2 hotspot)
        {
            if (texture == null)
            {
                Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);
                return;
            }

            Cursor.SetCursor(texture, hotspot, CursorMode.Auto);
        }
    }
}
