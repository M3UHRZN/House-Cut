using Game.Core;
using UnityEngine;
using UnityEngine.Audio;

namespace Game.Audio
{
    /// <summary>
    /// Ses Facade'ı: BlackjackLoopRunner ve GameFlowRunner event'lerine abone olup
    /// ilgili AudioCue'ları çalar. Runner'ları Start()'ta çözer (Awake sırası
    /// belirsiz — bkz. OrganExtractionRunner), OnDestroy'da bırakır.
    /// Cue slotları boşken (klipler henüz eklenmemişken) tüm çağrılar no-op'tur.
    /// </summary>
    public class AudioDirector : MonoBehaviour
    {
        [Header("Mixer (opsiyonel — atanmazsa AudioListener.volume tek kontrol kalır)")]
        [SerializeField] private AudioMixerGroup sfxGroup;
        [SerializeField] private AudioMixerGroup musicGroup;
        [SerializeField] private AudioMixerGroup ambientGroup;

        [Header("Sahne referansları")]
        [SerializeField] private BlackjackLoopRunner loopRunner;
        [SerializeField] private GameFlowRunner flowRunner;

        [Header("Cues — Kart / El")]
        [SerializeField] private AudioCue cardDeal;
        [SerializeField] private AudioCue cardFlip;
        [SerializeField] private AudioCue win;
        [SerializeField] private AudioCue lose;
        [SerializeField] private AudioCue bust;
        [SerializeField] private AudioCue gameOver;

        [Header("Cues — Ortam")]
        [SerializeField] private AudioCue ambientLoop;
        [SerializeField] private AudioCue music;

        private AudioSource sfxSource;
        private AudioSource musicSource;
        private AudioSource ambientSource;

        private void Awake()
        {
            sfxSource = CreateSource("SfxSource", sfxGroup, loop: false);
            musicSource = CreateSource("MusicSource", musicGroup, loop: true);
            ambientSource = CreateSource("AmbientSource", ambientGroup, loop: true);
        }

        private AudioSource CreateSource(string sourceName, AudioMixerGroup group, bool loop)
        {
            var sourceObject = new GameObject(sourceName);
            sourceObject.transform.SetParent(transform, false);

            AudioSource source = sourceObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.loop = loop;
            source.outputAudioMixerGroup = group;
            return source;
        }

        private void Start()
        {
            if (loopRunner == null)
            {
                loopRunner = FindAnyObjectByType<BlackjackLoopRunner>();
            }

            if (flowRunner == null)
            {
                flowRunner = FindAnyObjectByType<GameFlowRunner>();
            }

            if (loopRunner != null)
            {
                loopRunner.CardDealtVisual += OnCardDealtVisual;
                loopRunner.HoleCardFlipped += OnHoleCardFlipped;
                loopRunner.HandSettled += OnHandSettled;
                loopRunner.PlayerBusted += OnPlayerBusted;
            }
            else
            {
                Debug.LogWarning($"{nameof(AudioDirector)}: BlackjackLoopRunner bulunamadı; kart/el sesleri devre dışı.", this);
            }

            if (flowRunner != null && flowRunner.Flow != null)
            {
                flowRunner.Flow.StateChanged += OnStateChanged;
            }
            else
            {
                Debug.LogWarning($"{nameof(AudioDirector)}: GameFlowRunner bulunamadı; state-bağlı sesler devre dışı.", this);
            }

            ambientLoop?.PlayLoop(ambientSource);
            music?.PlayLoop(musicSource);
        }

        private void OnCardDealtVisual() => cardDeal?.PlayOneShot(sfxSource);

        private void OnHoleCardFlipped() => cardFlip?.PlayOneShot(sfxSource);

        private void OnPlayerBusted() => bust?.PlayOneShot(sfxSource);

        private void OnHandSettled(int delta)
        {
            // GDD §03.6: berabere yok — HandResolver her zaman kazan/kaybet döndürür (delta asla 0 değil).
            AudioCue cue = delta > 0 ? win : lose;
            cue?.PlayOneShot(sfxSource);
        }

        private void OnStateChanged(GameState previous, GameState next)
        {
            if (next == GameState.GameOver)
            {
                gameOver?.PlayOneShot(sfxSource);
            }
        }

        private void OnDestroy()
        {
            if (loopRunner != null)
            {
                loopRunner.CardDealtVisual -= OnCardDealtVisual;
                loopRunner.HoleCardFlipped -= OnHoleCardFlipped;
                loopRunner.HandSettled -= OnHandSettled;
                loopRunner.PlayerBusted -= OnPlayerBusted;
            }

            if (flowRunner != null && flowRunner.Flow != null)
            {
                flowRunner.Flow.StateChanged -= OnStateChanged;
            }
        }
    }
}
