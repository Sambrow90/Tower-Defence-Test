using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TD.UI
{
    /// <summary>
    /// Provides runtime controls for audio and graphics settings.
    /// </summary>
    public class SettingsView : MonoBehaviour
    {
        public event System.Action BackRequested;

        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider musicVolumeSlider;
        [SerializeField] private TMP_Dropdown qualityDropdown;
        [SerializeField] private Button backButton;

        private bool suppressCallbacks;

        public void Initialize(UIManager uiManager)
        {
            _ = uiManager;

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveListener(HandleMasterVolumeChanged);
                masterVolumeSlider.onValueChanged.AddListener(HandleMasterVolumeChanged);
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.onValueChanged.RemoveListener(HandleMusicVolumeChanged);
                musicVolumeSlider.onValueChanged.AddListener(HandleMusicVolumeChanged);
            }

            if (qualityDropdown != null)
            {
                qualityDropdown.onValueChanged.RemoveListener(HandleQualityChanged);
                qualityDropdown.onValueChanged.AddListener(HandleQualityChanged);
            }

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(HandleBackClicked);
                backButton.onClick.AddListener(HandleBackClicked);
            }

            SyncFromSettings();
        }

        public void SyncFromSettings()
        {
            suppressCallbacks = true;

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.value = AudioListener.volume;
            }

            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.value = PlayerPrefs.GetFloat("MusicVolume", 1f);
            }

            if (qualityDropdown != null)
            {
                var options = new List<string>(QualitySettings.names);
                qualityDropdown.ClearOptions();
                qualityDropdown.AddOptions(options);
                qualityDropdown.value = Mathf.Clamp(QualitySettings.GetQualityLevel(), 0, options.Count - 1);
            }

            suppressCallbacks = false;
        }

        private void HandleMasterVolumeChanged(float value)
        {
            if (suppressCallbacks)
            {
                return;
            }

            AudioListener.volume = Mathf.Clamp01(value);
        }

        private void HandleMusicVolumeChanged(float value)
        {
            if (suppressCallbacks)
            {
                return;
            }

            PlayerPrefs.SetFloat("MusicVolume", Mathf.Clamp01(value));
        }

        private void HandleQualityChanged(int index)
        {
            if (suppressCallbacks)
            {
                return;
            }

            QualitySettings.SetQualityLevel(Mathf.Clamp(index, 0, QualitySettings.names.Length - 1));
        }

        private void HandleBackClicked()
        {
            BackRequested?.Invoke();
        }
    }
}
