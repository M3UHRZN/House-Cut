using System.Collections;
using Game.Data;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Game.Organs
{
    /// <summary>
    /// Single organ tile on the beden defteri grid: click toggles selection,
    /// click pushes this organ's details into the runner's panel. Border color
    /// lerps between idle/selected/sold per the "Kasap
    /// Defteri" palette (OrganSaleRunner.*Color) — no DOTween in the project,
    /// so a plain Coroutine drives the tween.
    /// </summary>
    public class OrganTileView : MonoBehaviour, IPointerClickHandler
    {
        private const float ColorLerpDuration = 0.12f;

        [SerializeField] private Image border;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text valueText;
        [SerializeField] private GameObject soldMark;
        [Tooltip("Satılmış organın soluk görünmesini sağlayan CanvasGroup. Bu referansı organ görsel objesine Inspector'dan ata.")]
        [SerializeField] private CanvasGroup soldVisualGroup;
        [SerializeField, Range(0f, 1f)] private float soldOpacity = 0.28f;

        private OrganSaleRunner runner;
        private Coroutine colorRoutine;

        public OrganData Organ { get; private set; }

        public void Bind(OrganData organ, OrganSaleRunner owner)
        {
            Organ = organ;
            runner = owner;

            if (nameText != null)
            {
                nameText.text = organ.displayName;
            }
        }

        public void Refresh()
        {
            if (Organ == null || runner == null)
            {
                return;
            }

            bool sold = runner.OrganManager.IsSold(Organ);
            bool selected = runner.Controller.IsSelected(Organ);

            if (valueText != null)
            {
                valueText.text = "$" + runner.OrganManager.GetSaleValue(Organ);
            }

            if (soldMark != null)
            {
                soldMark.SetActive(sold);
            }

            if (soldVisualGroup != null)
            {
                soldVisualGroup.alpha = sold ? soldOpacity : 1f;
                soldVisualGroup.interactable = !sold;
                soldVisualGroup.blocksRaycasts = !sold;
            }

            Color target = sold
                ? OrganSaleRunner.SoldColor
                : selected
                    ? OrganSaleRunner.SelectedColor
                    : OrganSaleRunner.IdleBorderColor;
            SetBorderColor(target);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (runner == null || Organ == null || runner.OrganManager.IsSold(Organ))
            {
                return;
            }

            runner.ToggleOrgan(Organ);
        }

        private void SetBorderColor(Color target)
        {
            if (border == null)
            {
                return;
            }

            if (colorRoutine != null)
            {
                StopCoroutine(colorRoutine);
            }

            colorRoutine = StartCoroutine(LerpBorderColor(target));
        }

        private IEnumerator LerpBorderColor(Color target)
        {
            Color start = border.color;
            float t = 0f;
            while (t < ColorLerpDuration)
            {
                t += Time.unscaledDeltaTime;
                border.color = Color.Lerp(start, target, t / ColorLerpDuration);
                yield return null;
            }

            border.color = target;
        }
    }
}
