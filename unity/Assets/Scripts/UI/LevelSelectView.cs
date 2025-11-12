using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TD.Data;
using TD.Managers;

namespace TD.UI
{
    /// <summary>
    /// Builds the list of unlocked levels and forwards player selections.
    /// </summary>
    public class LevelSelectView : MonoBehaviour
    {
        public event Action<int> LevelRequested;
        public event Action BackRequested;

        [SerializeField] private Transform contentRoot;
        [SerializeField] private LevelButtonWidget levelButtonPrefab;
        [SerializeField] private Button backButton;
        [SerializeField] private TMP_Text headerLabel;

        private readonly List<LevelButtonWidget> spawnedButtons = new();

        private LevelManager levelManager;
        private SaveManager saveManager;

        public void Initialize(UIManager uiManager, LevelManager lm, SaveManager sm)
        {
            _ = uiManager;
            levelManager = lm;
            saveManager = sm;

            if (backButton != null)
            {
                backButton.onClick.RemoveListener(HandleBackClicked);
                backButton.onClick.AddListener(HandleBackClicked);
            }

            Refresh();
        }

        public void Refresh()
        {
            ClearButtons();

            if (contentRoot == null || levelButtonPrefab == null || levelManager == null)
            {
                return;
            }

            var levels = levelManager.AvailableLevels;
            int highestUnlockedIndex = GetHighestUnlockedIndex(levels.Count);

            for (int i = 0; i < levels.Count; i++)
            {
                var levelData = levels[i];
                if (levelData == null)
                {
                    continue;
                }

                bool unlocked = i <= highestUnlockedIndex;
                var widget = Instantiate(levelButtonPrefab, contentRoot);
                widget.Initialize(levelData, i, unlocked);
                widget.LevelChosen += HandleLevelChosen;
                spawnedButtons.Add(widget);
            }

            if (headerLabel != null)
            {
                headerLabel.text = $"Unlocked Levels: {Mathf.Clamp(highestUnlockedIndex + 1, 0, levels.Count)}/{levels.Count}";
            }
        }

        private int GetHighestUnlockedIndex(int levelCount)
        {
            if (saveManager != null && saveManager.CurrentSave != null)
            {
                return Mathf.Clamp(saveManager.CurrentSave.HighestUnlockedLevel, 0, Mathf.Max(0, levelCount - 1));
            }

            return Mathf.Max(0, levelCount - 1);
        }

        private void ClearButtons()
        {
            foreach (var widget in spawnedButtons)
            {
                if (widget != null)
                {
                    widget.LevelChosen -= HandleLevelChosen;
                    Destroy(widget.gameObject);
                }
            }

            spawnedButtons.Clear();
        }

        private void HandleLevelChosen(int levelIndex)
        {
            LevelRequested?.Invoke(levelIndex);
        }

        private void HandleBackClicked()
        {
            BackRequested?.Invoke();
        }
    }
}
