using DG.Tweening;
using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// The physical closed-card stack that sits on the table. Cards dealt by
    /// <see cref="BlackjackLoopRunner"/> fly out of <see cref="TopSpawn"/>; collected cards fly back
    /// into it. The stack itself is decorative (fixed height, does not shrink as cards are drawn).
    /// </summary>
    public class DeckVisual : MonoBehaviour
    {
        [SerializeField] private GameObject cardPrefab;
        [SerializeField, Min(1)] private int stackCount = 14;
        [SerializeField, Min(0f)] private float cardThickness = 0.004f;
        [SerializeField, Min(0.01f)] private float cardScale = 0.65f;

        [Header("Place On Table")]
        [SerializeField] private float placeDropHeight = 0.6f;
        [SerializeField, Min(0f)] private float placeDuration = 0.5f;
        [SerializeField] private Ease placeEase = Ease.OutBack;

        private Transform topSpawn;
        private Vector3 restPosition;

        public Transform TopSpawn => topSpawn != null ? topSpawn : transform;

        /// <summary>
        /// World position of the stack's top, captured once at rest. PlaceOnTable() teleports the
        /// whole deck (and therefore TopSpawn) into the air before tweening it back down, so callers
        /// that need "where a card visually comes from" must use this stable point instead of
        /// TopSpawn.position, which would otherwise read mid-air positions on the first card of a hand.
        /// </summary>
        public Vector3 RestTopSpawnPosition { get; private set; }

        private void Awake()
        {
            restPosition = transform.position;
            BuildStack();
            RestTopSpawnPosition = TopSpawn.position;
        }

        private void BuildStack()
        {
            var spawn = new GameObject("TopSpawn");
            spawn.transform.SetParent(transform, false);
            topSpawn = spawn.transform;

            if (cardPrefab == null)
            {
                Debug.LogWarning($"{nameof(DeckVisual)}: cardPrefab atanmadı, deste yığını görselsiz kalacak.", this);
                topSpawn.localPosition = Vector3.zero;
                return;
            }

            // Cards lie flat on the table (whatever world rotation the deck object itself has, matching
            // the player/dealer anchors), so the stack must grow along world-up, not a fixed local axis
            // — converting Vector3.up into this transform's local space keeps it correct regardless of
            // how the deck GameObject is oriented in the scene.
            Vector3 stackAxis = transform.InverseTransformDirection(Vector3.up);

            for (int i = 0; i < stackCount; i++)
            {
                GameObject model = Instantiate(cardPrefab, transform);
                model.transform.localPosition = stackAxis * (i * cardThickness);
                model.transform.localRotation = Quaternion.identity;
                model.transform.localScale *= cardScale;
            }

            topSpawn.localPosition = stackAxis * (stackCount * cardThickness);
        }

        /// <summary>Drops the stack onto the table (or gives it a short settle "tap" if already placed).</summary>
        public Tween PlaceOnTable()
        {
            transform.DOKill();
            transform.position = restPosition + Vector3.up * placeDropHeight;
            Vector3 baseScale = transform.localScale;

            Sequence sequence = DOTween.Sequence();
            sequence.Append(transform.DOMove(restPosition, placeDuration).SetEase(placeEase));
            sequence.Join(transform.DOPunchScale(baseScale * 0.08f, placeDuration, 4, 0.6f));
            sequence.SetLink(gameObject);
            return sequence;
        }
    }
}
