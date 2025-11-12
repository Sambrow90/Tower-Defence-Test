using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TD.Managers;

namespace TD.UI
{
    /// <summary>
    /// Handles runtime HUD updates and button interactions.
    /// </summary>
    public class HudView : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField] private TMP_Text livesLabel;
        [SerializeField] private TMP_Text currencyLabel;
        [SerializeField] private TMP_Text waveLabel;
        [SerializeField] private TMP_Text levelNameLabel;
        [SerializeField] private TMP_Text waveMessageLabel;

        [Header("Buttons")]
        [SerializeField] private Button pauseButton;
        [SerializeField] private TMP_Text pauseButtonLabel;
        [SerializeField] private Button speedNormalButton;
        [SerializeField] private Button speedFastButton;

        [Header("Panels")]
        [SerializeField] private GameObject pauseOverlay;
        [SerializeField] private GameObject victoryPanel;
        [SerializeField] private GameObject defeatPanel;

        [Header("Configuration")]
        [SerializeField] private float waveMessageDuration = 2f;

        private GameManager gameManager;
        private Coroutine waveMessageRoutine;
        private int totalWaves;

        public void Initialize(GameManager gm)
        {
            gameManager = gm;
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveListener(OnPauseButtonClicked);
                pauseButton.onClick.AddListener(OnPauseButtonClicked);
            }

            if (speedNormalButton != null)
            {
                speedNormalButton.onClick.RemoveListener(OnSpeedNormalClicked);
                speedNormalButton.onClick.AddListener(OnSpeedNormalClicked);
            }

            if (speedFastButton != null)
            {
                speedFastButton.onClick.RemoveListener(OnSpeedFastClicked);
                speedFastButton.onClick.AddListener(OnSpeedFastClicked);
            }

            ShowPauseOverlay(false);
            ShowResultPanel(null);
            RefreshSpeedButtons(gameManager != null ? gameManager.CurrentGameSpeed : 1f);
        }

        public void SetLives(int lives)
        {
            if (livesLabel != null)
            {
                livesLabel.text = $"Lives: {Mathf.Max(0, lives)}";
            }
        }

        public void SetCurrency(int amount)
        {
            if (currencyLabel != null)
            {
                currencyLabel.text = $"Gold: {Mathf.Max(0, amount)}";
            }
        }

        public void SetLevelName(string levelName)
        {
            if (levelNameLabel != null)
            {
                levelNameLabel.text = string.IsNullOrEmpty(levelName) ? "" : levelName;
            }
        }

        public void SetTotalWaves(int waveCount)
        {
            totalWaves = Mathf.Max(0, waveCount);
            UpdateWaveLabel(0);
        }

        public void ShowWaveIncoming(int waveNumber, int totalWaveCount)
        {
            totalWaves = Mathf.Max(totalWaveCount, totalWaves);
            UpdateWaveLabel(waveNumber);

            if (waveMessageLabel == null)
            {
                return;
            }

            if (waveMessageRoutine != null)
            {
                StopCoroutine(waveMessageRoutine);
            }

            waveMessageRoutine = StartCoroutine(ShowWaveMessageRoutine(waveNumber));
        }

        public void ShowResult(bool victory)
        {
            ShowResultPanel(victory);
        }

        public void RefreshSpeedButtons(float speed)
        {
            if (speedNormalButton != null)
            {
                speedNormalButton.interactable = speed > 1f;
            }

            if (speedFastButton != null)
            {
                speedFastButton.interactable = speed <= 1f;
            }
        }

        public void ShowPauseOverlay(bool show)
        {
            if (pauseOverlay != null)
            {
                pauseOverlay.SetActive(show);
            }

            if (pauseButtonLabel != null)
            {
                pauseButtonLabel.text = show ? "Resume" : "Pause";
            }
        }

        private void OnPauseButtonClicked()
        {
            if (gameManager == null)
            {
                return;
            }

            if (gameManager.IsPaused)
            {
                gameManager.ResumeGame();
                ShowPauseOverlay(false);
            }
            else
            {
                gameManager.PauseGame();
                ShowPauseOverlay(true);
            }
        }

        private void OnSpeedNormalClicked()
        {
            gameManager?.SetGameSpeed(1f);
        }

        private void OnSpeedFastClicked()
        {
            gameManager?.SetGameSpeed(2f);
        }

        private IEnumerator ShowWaveMessageRoutine(int waveNumber)
        {
            waveMessageLabel.gameObject.SetActive(true);
            waveMessageLabel.text = $"Wave {waveNumber}/{Mathf.Max(totalWaves, 1)}";
            yield return new WaitForSecondsRealtime(waveMessageDuration);
            waveMessageLabel.gameObject.SetActive(false);
        }

        private void UpdateWaveLabel(int currentWave)
        {
            if (waveLabel == null)
            {
                return;
            }

            if (totalWaves <= 0)
            {
                waveLabel.text = string.Empty;
            }
            else
            {
                waveLabel.text = $"Wave {Mathf.Max(0, currentWave)}/{totalWaves}";
            }
        }

        private void ShowResultPanel(bool? victory)
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(victory == true);
            }

            if (defeatPanel != null)
            {
                defeatPanel.SetActive(victory == false);
            }
        }
    }
}
