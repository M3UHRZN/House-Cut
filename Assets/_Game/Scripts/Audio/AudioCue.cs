using UnityEngine;

namespace Game.Audio
{
    /// <summary>
    /// Bir ses olayının (kart çekme, kazanma/kaybetme, ortam loop'u vb.) veri tanımı.
    /// Diğer SO'lar gibi (GameConfig, OrganData) tasarımcı Inspector'dan doldurur.
    /// Klip dizisi boşken tüm Play* metotları no-op'tur — sistem klipler eklenmeden
    /// önce de derlenip hatasız çalışsın diye (House Cut sesi henüz prodüksiyonda değil, §07/#34).
    /// </summary>
    [CreateAssetMenu(menuName = "House Cut/Audio/Audio Cue", fileName = "AudioCue")]
    public class AudioCue : ScriptableObject
    {
        [Header("Klipler")]
        [SerializeField] private AudioClip[] clips;

        [Header("Varyasyon")]
        [SerializeField, Range(0f, 1f)] private float volume = 1f;
        [SerializeField] private Vector2 pitchRange = new Vector2(0.95f, 1.05f);

        private static readonly System.Random SharedRandom = new System.Random();

        public bool HasClips => clips != null && clips.Length > 0;

        /// <summary>Saf seçim mantığı — EditMode testinde belirli bir Random ile doğrulanabilir.</summary>
        public AudioClip SelectClip(System.Random random)
        {
            if (!HasClips)
            {
                return null;
            }

            if (clips.Length == 1)
            {
                return clips[0];
            }

            random ??= SharedRandom;
            return clips[random.Next(clips.Length)];
        }

        /// <summary>Saf pitch hesabı — t=[0,1] içinde pitchRange'e lerp'lenir.</summary>
        public float ResolvePitch(float t) => Mathf.Lerp(pitchRange.x, pitchRange.y, Mathf.Clamp01(t));

        /// <summary>Rastgele bir klibi, rastgele pitch varyasyonuyla bir kez çalar.</summary>
        public void PlayOneShot(AudioSource source)
        {
            if (source == null)
            {
                return;
            }

            AudioClip clip = SelectClip(SharedRandom);
            if (clip == null)
            {
                return;
            }

            source.pitch = ResolvePitch((float)SharedRandom.NextDouble());
            source.PlayOneShot(clip, volume);
        }

        /// <summary>İlk klibi loop olarak kaynağa bağlayıp çalar (ambient/müzik).</summary>
        public void PlayLoop(AudioSource source)
        {
            if (source == null || !HasClips)
            {
                return;
            }

            source.clip = clips[0];
            source.loop = true;
            source.volume = volume;
            source.pitch = 1f;
            source.Play();
        }
    }
}
