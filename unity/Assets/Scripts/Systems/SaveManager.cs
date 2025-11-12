using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace TD.Systems
{
    /// <summary>
    /// Provides persistence for player progression and settings.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }

        public event Action<SaveData> SaveCompleted;
        public event Action<SaveData> LoadCompleted;

        [SerializeField] private string saveFileName = "tower_defence_save.json";
        [SerializeField] private string backupFileName = "tower_defence_save_backup.json";

        private const string PlayerPrefsFallbackKey = "TD_Save_Fallback";

        public SaveData CurrentSave { get; private set; }

        private string SaveFilePath => Path.Combine(Application.persistentDataPath, saveFileName);
        private string BackupFilePath => Path.Combine(Application.persistentDataPath, backupFileName);

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

        /// <summary>
        /// Loads the player's progress from disk. Call during game boot.
        /// </summary>
        public void Initialize()
        {
            LoadProgress();
        }

        /// <summary>
        /// Loads progression and settings from storage. Returns defaults when no save exists.
        /// </summary>
        public SaveData LoadProgress()
        {
            CurrentSave = LoadFromStorage() ?? SaveData.CreateDefault();
            CurrentSave.Validate();

            LoadCompleted?.Invoke(CurrentSave);
            return CurrentSave;
        }

        /// <summary>
        /// Serialises the current save state and writes it to storage.
        /// </summary>
        public void SaveProgress()
        {
            if (CurrentSave == null)
            {
                CurrentSave = SaveData.CreateDefault();
            }

            CurrentSave.Validate();

            string json = JsonUtility.ToJson(CurrentSave, true);
            EnsureSaveDirectory();

            try
            {
                File.WriteAllText(SaveFilePath, json);
                File.Copy(SaveFilePath, BackupFilePath, overwrite: true);
                PlayerPrefs.SetString(PlayerPrefsFallbackKey, json);
                PlayerPrefs.Save();
                SaveCompleted?.Invoke(CurrentSave);
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to save progress: {ex.Message}\n{ex.StackTrace}");
            }
        }

        /// <summary>
        /// Overwrites the current save with the provided data and persists it.
        /// </summary>
        public void SaveProgress(SaveData newSave)
        {
            CurrentSave = newSave ?? SaveData.CreateDefault();
            SaveProgress();
        }

        /// <summary>
        /// Removes stored progress and resets to defaults.
        /// </summary>
        public void ResetProgress()
        {
            CurrentSave = SaveData.CreateDefault();

            try
            {
                if (File.Exists(SaveFilePath))
                {
                    File.Delete(SaveFilePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to delete save file: {ex.Message}");
            }

            try
            {
                if (File.Exists(BackupFilePath))
                {
                    File.Delete(BackupFilePath);
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to delete backup save file: {ex.Message}");
            }

            PlayerPrefs.DeleteKey(PlayerPrefsFallbackKey);
            PlayerPrefs.Save();

            SaveProgress();
        }

        /// <summary>
        /// Marks the given level as completed, updates the best score and optionally unlocks the next level.
        /// </summary>
        public void RegisterLevelCompletion(string levelId, int achievedScore, string nextLevelId = null)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                Debug.LogWarning("Cannot register completion for an empty level id.");
                return;
            }

            EnsureCurrentSave();

            LevelProgressEntry levelProgress = CurrentSave.GetOrCreateLevelEntry(levelId);
            levelProgress.Unlocked = true;
            levelProgress.BestScore = Mathf.Max(levelProgress.BestScore, Mathf.Max(0, achievedScore));

            if (!string.IsNullOrEmpty(nextLevelId))
            {
                LevelProgressEntry nextLevelProgress = CurrentSave.GetOrCreateLevelEntry(nextLevelId);
                if (!nextLevelProgress.Unlocked)
                {
                    nextLevelProgress.Unlocked = true;
                }
            }

            SaveProgress();
        }

        /// <summary>
        /// Marks the given level as unlocked without modifying the best score.
        /// </summary>
        public void UnlockLevel(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                Debug.LogWarning("Cannot unlock an empty level id.");
                return;
            }

            EnsureCurrentSave();
            LevelProgressEntry levelProgress = CurrentSave.GetOrCreateLevelEntry(levelId);
            if (!levelProgress.Unlocked)
            {
                levelProgress.Unlocked = true;
                SaveProgress();
            }
        }

        /// <summary>
        /// Returns whether the given level is currently unlocked in the save data.
        /// </summary>
        public bool IsLevelUnlocked(string levelId)
        {
            EnsureCurrentSave();
            return CurrentSave.IsLevelUnlocked(levelId);
        }

        /// <summary>
        /// Gets the best score achieved for the given level. Returns 0 if not played yet.
        /// </summary>
        public int GetBestScore(string levelId)
        {
            EnsureCurrentSave();
            return CurrentSave.GetBestScore(levelId);
        }

        /// <summary>
        /// Updates the master audio volume stored in the save file.
        /// </summary>
        public void UpdateAudioVolume(float volume)
        {
            EnsureCurrentSave();
            CurrentSave.Settings.MasterVolume = Mathf.Clamp01(volume);
            SaveProgress();
        }

        /// <summary>
        /// Updates the language stored in the save file.
        /// </summary>
        public void UpdateLanguage(string languageCode)
        {
            if (string.IsNullOrWhiteSpace(languageCode))
            {
                Debug.LogWarning("Cannot set an empty language code.");
                return;
            }

            EnsureCurrentSave();
            CurrentSave.Settings.Language = languageCode.Trim();
            SaveProgress();
        }

        /// <summary>
        /// Updates the graphics quality stored in the save file.
        /// </summary>
        public void UpdateGraphicsQuality(GraphicsQuality quality)
        {
            EnsureCurrentSave();
            CurrentSave.Settings.Quality = quality;
            SaveProgress();
        }

        private void EnsureCurrentSave()
        {
            if (CurrentSave == null)
            {
                CurrentSave = SaveData.CreateDefault();
            }

            CurrentSave.Validate();
        }

        private SaveData LoadFromStorage()
        {
            if (TryLoadFromFile(SaveFilePath, out SaveData data))
            {
                return data;
            }

            if (TryLoadFromFile(BackupFilePath, out data))
            {
                Debug.LogWarning("Primary save file failed to load. Loaded backup instead.");
                return data;
            }

            if (PlayerPrefs.HasKey(PlayerPrefsFallbackKey))
            {
                string fallbackJson = PlayerPrefs.GetString(PlayerPrefsFallbackKey);
                data = Deserialize(fallbackJson);
                if (data != null)
                {
                    Debug.LogWarning("Loaded save data from PlayerPrefs fallback.");
                    return data;
                }
            }

            return null;
        }

        private bool TryLoadFromFile(string path, out SaveData data)
        {
            data = null;

            try
            {
                if (!File.Exists(path))
                {
                    return false;
                }

                string json = File.ReadAllText(path);
                data = Deserialize(json);
                return data != null;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to load save file at '{path}': {ex.Message}");
                return false;
            }
        }

        private SaveData Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return null;
            }

            try
            {
                return JsonUtility.FromJson<SaveData>(json);
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"Failed to deserialize save data: {ex.Message}");
                return null;
            }
        }

        private void EnsureSaveDirectory()
        {
            string directory = Path.GetDirectoryName(SaveFilePath);
            if (string.IsNullOrEmpty(directory))
            {
                return;
            }

            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }

    [Serializable]
    public class SaveData
    {
        [SerializeField] private List<LevelProgressEntry> levels = new();
        [SerializeField] private SettingsData settings = SettingsData.CreateDefault();

        public IReadOnlyList<LevelProgressEntry> Levels => levels;
        public SettingsData Settings
        {
            get => settings;
            set => settings = value ?? SettingsData.CreateDefault();
        }

        public static SaveData CreateDefault()
        {
            return new SaveData
            {
                levels = new List<LevelProgressEntry>(),
                settings = SettingsData.CreateDefault()
            };
        }

        public void Validate()
        {
            if (levels == null)
            {
                levels = new List<LevelProgressEntry>();
            }

            Dictionary<string, LevelProgressEntry> unique = new();
            foreach (LevelProgressEntry entry in levels)
            {
                if (entry == null || string.IsNullOrEmpty(entry.LevelId))
                {
                    continue;
                }

                if (!unique.TryGetValue(entry.LevelId, out LevelProgressEntry existing))
                {
                    unique.Add(entry.LevelId, entry);
                }
                else
                {
                    existing.Unlocked |= entry.Unlocked;
                    existing.BestScore = Mathf.Max(existing.BestScore, entry.BestScore);
                }
            }

            levels = new List<LevelProgressEntry>(unique.Values);

            settings ??= SettingsData.CreateDefault();
            settings.Clamp();
        }

        public LevelProgressEntry GetOrCreateLevelEntry(string levelId)
        {
            Validate();

            foreach (LevelProgressEntry entry in levels)
            {
                if (entry.LevelId == levelId)
                {
                    return entry;
                }
            }

            LevelProgressEntry newEntry = new(levelId);
            levels.Add(newEntry);
            return newEntry;
        }

        public bool IsLevelUnlocked(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                return false;
            }

            foreach (LevelProgressEntry entry in levels)
            {
                if (entry.LevelId == levelId)
                {
                    return entry.Unlocked;
                }
            }

            return false;
        }

        public int GetBestScore(string levelId)
        {
            if (string.IsNullOrEmpty(levelId))
            {
                return 0;
            }

            foreach (LevelProgressEntry entry in levels)
            {
                if (entry.LevelId == levelId)
                {
                    return Mathf.Max(0, entry.BestScore);
                }
            }

            return 0;
        }
    }

    [Serializable]
    public class LevelProgressEntry
    {
        [SerializeField] private string levelId;
        [SerializeField] private bool unlocked;
        [SerializeField] private int bestScore;

        public string LevelId
        {
            get => levelId;
            set => levelId = value;
        }

        public bool Unlocked
        {
            get => unlocked;
            set => unlocked = value;
        }

        public int BestScore
        {
            get => bestScore;
            set => bestScore = Mathf.Max(0, value);
        }

        public LevelProgressEntry()
        {
            levelId = string.Empty;
            unlocked = false;
            bestScore = 0;
        }

        public LevelProgressEntry(string levelId)
        {
            this.levelId = levelId;
            unlocked = false;
            bestScore = 0;
        }
    }

    [Serializable]
    public class SettingsData
    {
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 0.8f;
        [SerializeField] private string language = "en";
        [SerializeField] private GraphicsQuality quality = GraphicsQuality.Medium;

        public float MasterVolume
        {
            get => masterVolume;
            set => masterVolume = Mathf.Clamp01(value);
        }

        public string Language
        {
            get => language;
            set => language = string.IsNullOrWhiteSpace(value) ? "en" : value.Trim();
        }

        public GraphicsQuality Quality
        {
            get => quality;
            set => quality = value;
        }

        public static SettingsData CreateDefault()
        {
            return new SettingsData
            {
                masterVolume = 0.8f,
                language = "en",
                quality = GraphicsQuality.Medium
            };
        }

        public void Clamp()
        {
            masterVolume = Mathf.Clamp01(masterVolume);
            if (string.IsNullOrWhiteSpace(language))
            {
                language = "en";
            }

            if (!Enum.IsDefined(typeof(GraphicsQuality), quality))
            {
                quality = GraphicsQuality.Medium;
            }
        }
    }

    public enum GraphicsQuality
    {
        Low = 0,
        Medium = 1,
        High = 2
    }
}
