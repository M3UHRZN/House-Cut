using Game.Core;
using Game.Data;
using Game.Economy;
using Game.Organs;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Game.Presentation
{
    /// <summary>
    /// Kritik Hâl sunum katmanı (GDD §04): OrganManager.IsCriticalState olunca
    /// nabız sesi, daralan FOV, hafif kamera titremesi ve kırmızı vignette
    /// devreye girer, durum düzelince tabana yumuşakça döner. EconomyRunner'ın
    /// paylaşılan EconomyController.OrganManager/Bank'ını okur — OrganSaleRunner
    /// ile aynı instance, böylece satış ekranında satılan bir organ buradaki
    /// kritik-hâl hesabına da hemen yansır (GDD §08). GameFlow'a burada
    /// dokunulmuyor.
    ///
    /// Titreme kasıtlı olarak Camera.fieldOfView / transform yerine
    /// CinemachineCamera.Lens / transform üzerinden uygulanır: sahnede bir
    /// CinemachineBrain aktif ve Main Camera'yı her LateUpdate'te vcam'a göre
    /// eziyor — doğrudan Camera'ya yazmak CameraLookRunner'da yaşanan "hiç
    /// değişmiyor" sorununu pozisyon için de tekrarlardı.
    ///
    /// FOV'u bu script YAZMAZ — CameraLookRunner de (zoom için) aynı
    /// Lens.FieldOfView'a yazdığından iki yazıcı birbirini ezip titremeye
    /// sebep olurdu. Bunun yerine NarrowedFovDelta'yı ve IsCritical'ı
    /// açığa çıkarır; tek yazıcı olan CameraLookRunner bunu kendi zoom
    /// hedefinden düşerek uygular.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class CriticalStateRunner : MonoBehaviour
    {
        [SerializeField] private GameConfigProvider configProvider;
        [SerializeField] private EconomyRunner economyRunner;
        [SerializeField] private Volume globalVolume;
        [SerializeField] private CinemachineCamera cinemachineCamera;
        [SerializeField] private AudioClip heartbeatClip;

        [Header("Sunum Ayarları")]
        [SerializeField] private float narrowedFovDelta = 10f;
        [SerializeField] private float shakeAmplitude = 0.01f;
        [SerializeField] private float shakeFrequency = 12f;
        [SerializeField] private Color criticalVignetteColor = new Color(0.5f, 0f, 0f);
        [SerializeField] private float criticalVignetteIntensity = 0.45f;
        [Tooltip("GDD: 'renklerin bozulması' — kritik hâlde kromatik ayrışma şiddeti.")]
        [SerializeField] private float criticalChromaticAberrationIntensity = 0.6f;
        [SerializeField] private float blendSpeed = 3f;

        [Header("Periyodik Nefes (yalnızca Kritik Hâl sırasında, opsiyonel)")]
        [Tooltip("Kapatırsan periyodik nefes hareketi hiç oynamaz.")]
        [SerializeField] private bool periodicShakeEnabled = true;
        [Tooltip("Kaç saniyede bir derin nefes alınsın.")]
        [SerializeField] private float periodicShakeInterval = 15f;
        [Tooltip("Bir nefesin ne kadar sürsün (saniye) — yumuşak yükselip inme.")]
        [SerializeField] private float periodicShakeDuration = 2.5f;
        [Tooltip("Nefesin ne kadar belirgin olacağı — küçük tut, oyuncuyu rahatsız etmesin.")]
        [SerializeField] private float periodicShakeAmplitude = 0.015f;

        private GameConfig runtimeConfig;
        private VolumeProfile runtimeProfile;
        private Bank bank;
        private OrganManager organManager;
        private AudioSource audioSource;
        private Vignette vignette;
        private ChromaticAberration chromaticAberration;

        private Color baseVignetteColor;
        private float baseVignetteIntensity;
        private float baseChromaticAberrationIntensity;
        private Vector3 cinemachineRestPosition;
        private Vector3 smoothedJitterBase;
        private float periodicShakeTimer;

        public bool IsCritical { get; private set; }

        /// <summary>How much CameraLookRunner should narrow its FOV target while critical.</summary>
        public float NarrowedFovDelta => narrowedFovDelta;

        /// <summary>Exposed read-only so a test/editor harness can drive the shared OrganManager/Bank in Play mode.</summary>
        public OrganManager OrganManager => organManager;

        /// <summary>Exposed read-only so a test/editor harness can drive the shared OrganManager/Bank in Play mode.</summary>
        public Bank Bank => bank;

        private void Awake()
        {
            if (configProvider == null || configProvider.Config == null)
            {
                runtimeConfig = ScriptableObject.CreateInstance<GameConfig>();
            }

            audioSource = GetComponent<AudioSource>();
            audioSource.clip = heartbeatClip;
            audioSource.loop = true;

            if (globalVolume != null && globalVolume.profile != null)
            {
                // Clone before reading/writing: mutating the shared project
                // VolumeProfile asset directly at runtime can leave it altered
                // after Play mode ends (Unity doesn't reliably revert in-place
                // edits to a shared ScriptableObject asset the way it does for
                // scene objects). The clone is a Play-mode-only instance.
                runtimeProfile = Instantiate(globalVolume.profile);
                globalVolume.profile = runtimeProfile;
                runtimeProfile.TryGet(out vignette);
                runtimeProfile.TryGet(out chromaticAberration);
            }

            if (vignette != null)
            {
                baseVignetteColor = vignette.color.value;
                baseVignetteIntensity = vignette.intensity.value;
            }

            if (chromaticAberration != null)
            {
                baseChromaticAberrationIntensity = chromaticAberration.intensity.value;
            }

            if (cinemachineCamera != null)
            {
                cinemachineRestPosition = cinemachineCamera.transform.localPosition;
                smoothedJitterBase = cinemachineRestPosition;
            }
        }

        private void Start()
        {
            // Resolved in Start (not Awake): EconomyRunner.Awake sets Economy and
            // component Awake order is undefined, same reasoning as
            // BlackjackLoopRunner/OrganSaleRunner reading it after the fact.
            if (economyRunner == null)
            {
                economyRunner = FindAnyObjectByType<EconomyRunner>();
            }

            if (economyRunner == null || economyRunner.Economy == null)
            {
                Debug.LogError($"{nameof(CriticalStateRunner)}: Sahnenin EconomyRunner bileşeni bulunamadı.", this);
                enabled = false;
                return;
            }

            bank = economyRunner.Economy.MoneyBank;
            organManager = economyRunner.Economy.OrganManager;
        }

        private bool forceCritical;

        /// <summary>Test-only override (Editor harness button): while true, always reads as critical regardless of the real Bank/OrganManager state.</summary>
        public void SetForceCritical(bool value)
        {
            forceCritical = value;
        }

        private void Update()
        {
            int minBet = configProvider != null && configProvider.Config != null
                ? configProvider.Config.minBet
                : runtimeConfig.minBet;

            bool critical = forceCritical || organManager.IsCriticalState(minBet);
            if (critical != IsCritical)
            {
                IsCritical = critical;
                ToggleHeartbeat();
            }

            ApplyPresentation(Time.deltaTime);
        }

        private void ToggleHeartbeat()
        {
            if (audioSource.clip == null)
            {
                return;
            }

            if (IsCritical)
            {
                audioSource.Play();
            }
            else
            {
                audioSource.Stop();
            }
        }

        private void ApplyPresentation(float deltaTime)
        {
            float blend = blendSpeed * deltaTime;

            if (cinemachineCamera != null)
            {
                // Critical-state jitter blends in/out slowly like the other
                // presentation values. The breath offset is applied straight on
                // top, undamped, so its sine curve's own up-then-back-down shape
                // isn't lagged/asymmetrized by the blend (that lag was the bug
                // that made it look like it "only goes up").
                Vector3 criticalJitter = IsCritical
                    ? new Vector3(
                        (Mathf.PerlinNoise(Time.time * shakeFrequency, 0f) - 0.5f) * shakeAmplitude,
                        (Mathf.PerlinNoise(0f, Time.time * shakeFrequency) - 0.5f) * shakeAmplitude,
                        0f)
                    : Vector3.zero;
                smoothedJitterBase = Vector3.Lerp(smoothedJitterBase, cinemachineRestPosition + criticalJitter, blend);

                Vector3 breathOffset = ComputePeriodicShakeJitter(deltaTime);
                cinemachineCamera.transform.localPosition = smoothedJitterBase + breathOffset;
            }

            if (vignette != null)
            {
                Color targetColor = IsCritical ? criticalVignetteColor : baseVignetteColor;
                float targetIntensity = IsCritical ? criticalVignetteIntensity : baseVignetteIntensity;
                vignette.color.value = Color.Lerp(vignette.color.value, targetColor, blend);
                vignette.intensity.value = Mathf.Lerp(vignette.intensity.value, targetIntensity, blend);
            }

            if (chromaticAberration != null)
            {
                float targetChromaticAberration = IsCritical
                    ? criticalChromaticAberrationIntensity
                    : baseChromaticAberrationIntensity;
                chromaticAberration.intensity.value = Mathf.Lerp(
                    chromaticAberration.intensity.value,
                    targetChromaticAberration,
                    blend);
            }
        }

        /// <summary>
        /// Panikte nefes alır gibi ambient bir hareket — yalnızca Kritik Hâl
        /// sırasında çalışır: her periodicShakeInterval saniyede bir,
        /// periodicShakeDuration boyunca kamera yumuşakça yükselip yavaşça iner.
        /// Raised-cosine (Hann) eğrisi kullanılır — başta ve sonda hız sıfır
        /// olduğu için düz sinüse göre daha yumuşak açılıp kapanır (jitter/gürültü
        /// yok). Kritik Hâl bitince zamanlayıcı sıfırlanır; her yeni kritik anda
        /// nefes baştan başlar. periodicShakeEnabled false ise hiç tetiklenmez.
        /// </summary>
        private Vector3 ComputePeriodicShakeJitter(float deltaTime)
        {
            if (!periodicShakeEnabled || !IsCritical)
            {
                periodicShakeTimer = 0f;
                return Vector3.zero;
            }

            periodicShakeTimer += deltaTime;

            bool inBreath = periodicShakeTimer >= periodicShakeInterval;
            if (periodicShakeTimer >= periodicShakeInterval + periodicShakeDuration)
            {
                periodicShakeTimer = 0f;
                inBreath = false;
            }

            if (!inBreath)
            {
                return Vector3.zero;
            }

            float breathProgress = (periodicShakeTimer - periodicShakeInterval) / periodicShakeDuration;
            // Raised cosine: 0 at start/end with zero velocity, smooth peak at midpoint.
            float breathCurve = 0.5f - 0.5f * Mathf.Cos(breathProgress * Mathf.PI * 2f);
            return new Vector3(0f, breathCurve * periodicShakeAmplitude, 0f);
        }

        private void OnDestroy()
        {
            if (runtimeConfig != null)
            {
                Destroy(runtimeConfig);
            }

            if (runtimeProfile != null)
            {
                Destroy(runtimeProfile);
            }
        }
    }
}
