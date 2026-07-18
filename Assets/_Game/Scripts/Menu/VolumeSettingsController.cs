using UnityEngine;
using UnityEngine.UI;

namespace Game.Menu
{
    /// <summary>
    /// Ana ses seviyesi ayarı: slider'ı AudioListener.volume'a bağlar ve
    /// PlayerPrefs ile oturumlar arası hatırlar.
    /// </summary>
    public class VolumeSettingsController : MonoBehaviour
    {
        private const string PrefsKey = "MasterVolume";

        [SerializeField] private Slider volumeSlider;

        private void Start()
        {
            float saved = PlayerPrefs.GetFloat(PrefsKey, 1f);
            AudioListener.volume = saved;

            if (volumeSlider != null)
            {
                volumeSlider.SetValueWithoutNotify(saved);
                volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
            }
        }

        private void OnVolumeChanged(float value)
        {
            AudioListener.volume = value;
            PlayerPrefs.SetFloat(PrefsKey, value);
        }
    }
}
