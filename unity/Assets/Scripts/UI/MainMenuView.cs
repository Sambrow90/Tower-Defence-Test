using System;
using UnityEngine;
using UnityEngine.UI;

namespace TD.UI
{
    /// <summary>
    /// Handles button wiring for the main menu screen.
    /// </summary>
    public class MainMenuView : MonoBehaviour
    {
        public event Action PlayRequested;
        public event Action LevelSelectRequested;
        public event Action SettingsRequested;
        public event Action QuitRequested;

        [SerializeField] private Button playButton;
        [SerializeField] private Button levelSelectButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        public void Initialize(UIManager uiManager, TD.Managers.GameManager gameManager)
        {
            _ = uiManager;
            _ = gameManager;

            if (playButton != null)
            {
                playButton.onClick.RemoveListener(HandlePlayClicked);
                playButton.onClick.AddListener(HandlePlayClicked);
            }

            if (levelSelectButton != null)
            {
                levelSelectButton.onClick.RemoveListener(HandleLevelSelectClicked);
                levelSelectButton.onClick.AddListener(HandleLevelSelectClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.RemoveListener(HandleSettingsClicked);
                settingsButton.onClick.AddListener(HandleSettingsClicked);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(HandleQuitClicked);
                quitButton.onClick.AddListener(HandleQuitClicked);
            }
        }

        private void HandlePlayClicked()
        {
            PlayRequested?.Invoke();
        }

        private void HandleLevelSelectClicked()
        {
            LevelSelectRequested?.Invoke();
        }

        private void HandleSettingsClicked()
        {
            SettingsRequested?.Invoke();
        }

        private void HandleQuitClicked()
        {
            QuitRequested?.Invoke();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
