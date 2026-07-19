using DG.Tweening;
using Game.Cards;
using UnityEngine;

namespace Game.Core
{
    public class CardVisual : MonoBehaviour
    {
        [SerializeField] private Transform model;

        private Renderer cardRenderer;
        private TextMesh faceLabel;
        // The prefab's unrotated local orientation is the card FACE, not the back — confirmed visually
        // (an unflipped model shows the card's face; a 180°-about-X flip shows the generic back texture).
        private Quaternion baseRotation;
        private bool hasModelRotation;

        public void Initialize(Transform cardModel)
        {
            model = cardModel;
            if (model == null)
            {
                hasModelRotation = false;
                return;
            }

            baseRotation = model.localRotation;
            hasModelRotation = true;
        }

        public void SetCard(Card card, bool faceUp)
        {
            if (model != null)
            {
                if (!hasModelRotation)
                {
                    baseRotation = model.localRotation;
                    hasModelRotation = true;
                }

                model.localRotation = baseRotation * Quaternion.Euler(faceUp ? Vector3.zero : new Vector3(180f, 0f, 0f));
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

        /// <summary>
        /// Flies the card in from <paramref name="startPos"/> (the deck's top) to its table slot, then
        /// invokes <paramref name="onArrived"/> (e.g. to reveal the face). Callers must not attach their
        /// own via Tween.OnComplete() — DOTween's OnComplete replaces rather than chains, and
        /// AnimationQueue already claims it to advance the queue, so it would silently discard this one.
        /// </summary>
        public Tween AnimateDealFrom(Vector3 startPos, Vector3 endPos, Quaternion endRot, float duration, Ease ease, TweenCallback onArrived = null)
        {
            transform.SetPositionAndRotation(startPos, endRot);

            Sequence sequence = DOTween.Sequence();
            sequence.Append(transform.DOMove(endPos, duration).SetEase(ease));
            if (onArrived != null)
            {
                sequence.AppendCallback(onArrived);
            }

            sequence.SetLink(gameObject);
            return sequence;
        }

        /// <summary>Tweens the model between its face-down and face-up rotation ("düz dönme" flip).</summary>
        public Tween FlipTo(bool faceUp, float duration, Ease ease)
        {
            if (model == null)
            {
                return null;
            }

            if (!hasModelRotation)
            {
                baseRotation = model.localRotation;
                hasModelRotation = true;
            }

            Quaternion target = baseRotation * Quaternion.Euler(faceUp ? Vector3.zero : new Vector3(180f, 0f, 0f));
            Tween tween = model.DOLocalRotateQuaternion(target, duration).SetEase(ease);
            tween.SetLink(gameObject);
            return tween;
        }

        /// <summary>
        /// Sweeps the card back into the deck stack and destroys it on arrival. Uses AppendCallback
        /// rather than OnComplete for the same reason as AnimateDealFrom — AnimationQueue overwrites
        /// OnComplete on every step's tween to advance itself.
        /// </summary>
        public Tween AnimateCollectTo(Vector3 deckTop, float duration, Ease ease)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Append(transform.DOMove(deckTop, duration).SetEase(ease));
            sequence.Join(transform.DOScale(Vector3.zero, duration).SetEase(ease));
            sequence.AppendCallback(() => Destroy(gameObject));
            sequence.SetLink(gameObject);
            return sequence;
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
