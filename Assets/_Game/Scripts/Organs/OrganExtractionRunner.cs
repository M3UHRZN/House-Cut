using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using Game.Core;
using Game.Data;
using Game.Economy;
using UnityEngine;

namespace Game.Organs
{
    /// <summary>
    /// Plays the organ extraction visual (GDD §04/§05). Observes OrganManager.OrganSold
    /// (fired once per sold organ during OrganSaleRunner.Confirm, before Extraction is
    /// entered) to queue organs, then on GameState.Extraction spawns each organ's prefab
    /// at a spawn anchor, tweens it to a drop point, and releases its Rigidbody so it
    /// falls onto the organ bench. While each organ is in flight it locks
    /// CameraLookRunner onto it (look-at), releasing the lock once the queue drains.
    /// When the queue drains it calls BlackjackLoop.CompleteExtraction() so the flow
    /// can proceed (the "flow waits for animation" rule). The Heart never fires
    /// OrganSold, so it is not handled here.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class OrganExtractionRunner : MonoBehaviour
    {
        [SerializeField] private GameFlowRunner flowRunner;
        [SerializeField] private EconomyRunner economyRunner;
        [SerializeField] private BlackjackLoopRunner loopRunner;
        [Tooltip("Atanırsa, uçuş boyunca kamera bu spawn edilen organa kilitlenir.")]
        [SerializeField] private CameraLookRunner cameraLookRunner;

        [Header("Anchors")]
        [Tooltip("Organın belirdiği nokta (istediğin yere koy).")]
        [SerializeField] private Transform spawnAnchor;
        [Tooltip("Organın uçarak gittiği ve fiziğe bırakıldığı nokta (tezgahın üstü).")]
        [SerializeField] private Transform dropAnchor;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float travelDuration = 0.6f;
        [SerializeField] private Ease travelEase = Ease.OutQuad;
        [Tooltip("Spawn ile drop noktası arasındaki kavisin ne kadar yükseleceği (düz çizgi yerine fırlatılmış gibi görünmesi için).")]
        [SerializeField, Min(0f)] private float arcHeight = 1.2f;
        [Tooltip("Uçuş sırasında organın havada döneceği toplam açı aralığı (derece). Her eksen için ayrı rastgele bir değer seçilir.")]
        [SerializeField, Min(0f)] private float minTumbleDegrees = 360f;
        [SerializeField, Min(0f)] private float maxTumbleDegrees = 720f;
        [Tooltip("Bir organ düştükten sonra bir sonrakini spawnlamadan önceki bekleme.")]
        [SerializeField, Min(0f)] private float settleDelay = 0.4f;

        private AudioSource audioSource;
        private OrganManager organManager;
        private readonly Queue<OrganData> pending = new();

        private void Awake()
        {
            audioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            if (economyRunner == null)
            {
                economyRunner = FindAnyObjectByType<EconomyRunner>();
            }

            if (loopRunner == null)
            {
                loopRunner = FindAnyObjectByType<BlackjackLoopRunner>();
            }

            if (cameraLookRunner == null)
            {
                cameraLookRunner = FindAnyObjectByType<CameraLookRunner>();
            }

            if (economyRunner == null || economyRunner.Economy == null || flowRunner == null || flowRunner.Flow == null)
            {
                Debug.LogError($"{nameof(OrganExtractionRunner)}: EconomyRunner ve GameFlowRunner atanmalı.", this);
                enabled = false;
                return;
            }

            organManager = economyRunner.Economy.OrganManager;
            organManager.OrganSold += OnOrganSold;
            flowRunner.Flow.StateChanged += OnStateChanged;
        }

        private void OnDestroy()
        {
            if (organManager != null)
            {
                organManager.OrganSold -= OnOrganSold;
            }

            if (flowRunner != null && flowRunner.Flow != null)
            {
                flowRunner.Flow.StateChanged -= OnStateChanged;
            }
        }

        // Queued as the sale is confirmed; drained when Extraction is entered.
        private void OnOrganSold(OrganData organ) => pending.Enqueue(organ);

        private void OnStateChanged(GameState previous, GameState next)
        {
            if (next == GameState.Extraction)
            {
                StartCoroutine(PlayExtractionQueue());
            }
        }

        private IEnumerator PlayExtractionQueue()
        {
            while (pending.Count > 0)
            {
                yield return SpawnAndDrop(pending.Dequeue());

                if (settleDelay > 0f)
                {
                    yield return new WaitForSeconds(settleDelay);
                }
            }

            // Release the camera lock before handing the flow back.
            if (cameraLookRunner != null)
            {
                cameraLookRunner.SetExtractionTarget(null);
            }

            // Animation finished → let the game flow leave Extraction.
            if (loopRunner != null && loopRunner.Loop != null)
            {
                loopRunner.Loop.CompleteExtraction();
            }
        }

        private IEnumerator SpawnAndDrop(OrganData organ)
        {
            if (organ == null)
            {
                yield break;
            }

            if (organ.extractionSfx != null)
            {
                audioSource.PlayOneShot(organ.extractionSfx);
            }

            if (organ.extractionPrefab == null || spawnAnchor == null || dropAnchor == null)
            {
                Debug.LogWarning($"{nameof(OrganExtractionRunner)}: {organ.displayName} için prefab/anchor eksik; animasyon atlandı.", this);
                yield break;
            }

            GameObject instance = Instantiate(organ.extractionPrefab, spawnAnchor.position, spawnAnchor.rotation);

            if (cameraLookRunner != null)
            {
                cameraLookRunner.SetExtractionTarget(instance.transform);
            }

            // Hand-animate while kinematic, even if the prefab author forgot to tick Is Kinematic.
            Rigidbody body = instance.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = true;
            }

            // A single lofted waypoint (path midpoint, raised by arcHeight) turns the
            // straight-line DOMove into a thrown/lobbed arc instead of a robotic slide.
            Vector3 midpoint = Vector3.Lerp(spawnAnchor.position, dropAnchor.position, 0.5f) + Vector3.up * arcHeight;
            Vector3[] path = { midpoint, dropAnchor.position };

            Vector3 tumble = new Vector3(
                Random.Range(minTumbleDegrees, maxTumbleDegrees) * (Random.value < 0.5f ? -1f : 1f),
                Random.Range(minTumbleDegrees, maxTumbleDegrees) * (Random.value < 0.5f ? -1f : 1f),
                Random.Range(minTumbleDegrees, maxTumbleDegrees) * (Random.value < 0.5f ? -1f : 1f));

            Sequence flight = DOTween.Sequence();
            flight.Join(instance.transform.DOPath(path, travelDuration, PathType.CatmullRom).SetEase(travelEase));
            flight.Join(instance.transform.DORotate(tumble, travelDuration, RotateMode.FastBeyond360).SetEase(Ease.Linear));
            flight.SetLink(instance);

            yield return flight.WaitForCompletion();

            // Release to physics: it drops the last bit and lands on the bench collider.
            if (body != null)
            {
                body.isKinematic = false;
            }
        }
    }
}
