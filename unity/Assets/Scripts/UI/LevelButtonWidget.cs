using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using TD.Data;

namespace TD.UI
{
    /// <summary>
    /// Represents a single entry in the level selection list.
    /// </summary>
    public class LevelButtonWidget : MonoBehaviour
    {
        public event Action<int> LevelChosen;

        [SerializeField] private Button button;
        [SerializeField] private TMP_Text titleLabel;
        [SerializeField] private TMP_Text subtitleLabel;

        private int levelIndex;
        private LevelData levelData;

        public void Initialize(LevelData level, int index, bool unlocked)
        {
            levelData = level;
            levelIndex = index;

            if (titleLabel != null)
            {
                titleLabel.text = level != null ? level.DisplayName : $"Level {index + 1}";
            }

            if (subtitleLabel != null)
            {
                if (level != null)
                {
                    subtitleLabel.text = unlocked
                        ? $"Scene: {level.SceneName}"
                        : "Locked";
                }
                else
                {
                    subtitleLabel.text = unlocked ? string.Empty : "Locked";
                }
            }

            if (button != null)
            {
                button.interactable = unlocked;
                button.onClick.RemoveListener(HandleClick);
                button.onClick.AddListener(HandleClick);
            }
        }

        public void SetInteractable(bool unlocked)
        {
            if (button != null)
            {
                button.interactable = unlocked;
            }

            if (subtitleLabel != null && levelData != null)
            {
                subtitleLabel.text = unlocked
                    ? $"Scene: {levelData.SceneName}"
                    : "Locked";
            }
        }

        private void HandleClick()
        {
            LevelChosen?.Invoke(levelIndex);
        }
    }
}
