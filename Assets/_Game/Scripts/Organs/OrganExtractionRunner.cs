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
    /// falls onto the organ bench. When the queue drains it calls
    /// BlackjackLoop.CompleteExtraction() so the flow can proceed (the "flow waits for
    /// animation" rule). The Heart never fires OrganSold, so it is not handled here.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class OrganExtractionRunner : MonoBehaviour
    {
        [SerializeField] private GameFlowRunner flowRunner;
        [SerializeField] private EconomyRunner economyRunner;
        [SerializeField] private BlackjackLoopRunner loopRunner;

        [Header("Anchors")]
        [Tooltip("Organın belirdiği nokta (istediğin yere koy).")]
        [SerializeField] private Transform spawnAnchor;
        [Tooltip("Organın uçarak gittiği ve fiziğe bırakıldığı nokta (tezgahın üstü).")]
        [SerializeField] private Transform dropAnchor;

        [Header("Animation")]
        [SerializeField, Min(0f)] private float travelDuration = 0.6f;
        [SerializeField] private Ease travelEase = Ease.OutQuad;
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

            // Hand-animate while kinematic, even if the prefab author forgot to tick Is Kinematic.
            Rigidbody body = instance.GetComponent<Rigidbody>();
            if (body != null)
            {
                body.isKinematic = true;
            }

            Tween move = instance.transform
                .DOMove(dropAnchor.position, travelDuration)
                .SetEase(travelEase)
                .SetLink(instance);

            yield return move.WaitForCompletion();

            // Release to physics: it drops the last bit and lands on the bench collider.
            if (body != null)
            {
                body.isKinematic = false;
            }
        }
    }
}
