using System;
using UnityEngine;

namespace TD.Managers
{
    /// <summary>
    /// Handles UI screen transitions, HUD updates, and input prompts.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public event Action<string> ScreenShown;
        public event Action<string> ScreenHidden;

        [SerializeField] private Canvas mainCanvas;
        [SerializeField] private RectTransform hudRoot;
        [SerializeField] private RectTransform menuRoot;

        public Canvas MainCanvas => mainCanvas;
        public RectTransform HudRoot => hudRoot;
        public RectTransform MenuRoot => menuRoot;

        public void Initialize()
        {
            // TODO: Cache UI references and subscribe to manager events.
        }

        public void ShowScreen(string screenId)
        {
            // TODO: Activate the requested screen and deactivate others.
        }

        public void HideScreen(string screenId)
        {
            // TODO: Hide the specified screen.
        }

        public void UpdateCurrencyDisplay(int amount)
        {
            // TODO: Update HUD currency text.
        }

        public void UpdateLivesDisplay(int lives)
        {
            // TODO: Update HUD life indicator.
        }

        public void ShowWaveIncoming(int waveIndex)
        {
            // TODO: Display wave notification to the player.
        }

        public void ShowResult(bool victory)
        {
            // TODO: Display victory/defeat panels.
        }
    }
}
