using System;
using UnityEngine;
using TD.Systems;

namespace TD.Managers
{
    /// <summary>
    /// Coordinates high-level game flow and cross-manager communication.
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        public event Action<GameState> GameStateChanged;
        public event Action<int> PlayerLivesChanged;
        public event Action<int> PlayerCurrencyChanged;

        [SerializeField] private LevelManager levelManager;
        [SerializeField] private WaveManager waveManager;
        [SerializeField] private TowerManager towerManager;
        [SerializeField] private EnemyManager enemyManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private SaveManager saveManager;

        public GameState CurrentState { get; private set; } = GameState.Bootstrapping;
        public int PlayerLives { get; private set; }
        public int PlayerCurrency { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Start()
        {
            // TODO: Initialize managers and load persistent data.
        }

        public void InitializeGame()
        {
            // TODO: Set up initial state, load level data, and prepare systems.
        }

        public void StartGame()
        {
            // TODO: Begin gameplay by starting level and waves.
        }

        public void PauseGame()
        {
            // TODO: Pause gameplay and notify interested systems.
        }

        public void ResumeGame()
        {
            // TODO: Resume gameplay from a paused state.
        }

        public void EndGame(bool victory)
        {
            // TODO: Handle game conclusion logic.
        }

        public void AddCurrency(int amount)
        {
            // TODO: Update player currency and trigger events/UI updates.
        }

        public void RemoveLife(int amount)
        {
            // TODO: Decrement player lives and check for defeat.
        }

        public void ChangeState(GameState newState)
        {
            // TODO: Transition between game states and notify listeners.
        }
    }

    public enum GameState
    {
        Bootstrapping,
        MainMenu,
        PreparingLevel,
        Playing,
        Paused,
        Victory,
        Defeat
    }
}
