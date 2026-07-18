using Game.Cards;
using UnityEngine;

namespace Game.Core
{
    public class CardVisual : MonoBehaviour
    {
        [SerializeField] private Transform model;

        private Renderer cardRenderer;
        private TextMesh faceLabel;
        private Quaternion faceUpRotation;
        private bool hasModelRotation;

        public void Initialize(Transform cardModel)
        {
            model = cardModel;
            if (model == null)
            {
                hasModelRotation = false;
                return;
            }

            faceUpRotation = model.localRotation;
            hasModelRotation = true;
        }

        public void SetCard(Card card, bool faceUp)
        {
            if (model != null)
            {
                if (!hasModelRotation)
                {
                    faceUpRotation = model.localRotation;
                    hasModelRotation = true;
                }

                model.localRotation = faceUpRotation * Quaternion.Euler(faceUp ? Vector3.zero : new Vector3(180f, 0f, 0f));
                return;
            }

            EnsureVisuals();
            if (cardRenderer != null)
            {
                cardRenderer.material.color = faceUp ? Color.white : new Color(0.08f, 0.12f, 0.35f);
            }

            faceLabel.text = faceUp ? card.ToString() : "?";
            faceLabel.color = faceUp ? Color.black : Color.white;
        }

        private void EnsureVisuals()
        {
            if (cardRenderer == null)
            {
                cardRenderer = GetComponent<Renderer>();
            }

            if (faceLabel != null)
            {
                return;
            }

            var label = new GameObject("FaceLabel");
            label.transform.SetParent(transform, false);
            label.transform.localPosition = new Vector3(0f, 0.055f, 0f);
            label.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            label.transform.localScale = Vector3.one * 0.22f;
            faceLabel = label.AddComponent<TextMesh>();
            faceLabel.anchor = TextAnchor.MiddleCenter;
            faceLabel.alignment = TextAlignment.Center;
            faceLabel.characterSize = 0.25f;
            faceLabel.fontSize = 48;
        }
    }
}
