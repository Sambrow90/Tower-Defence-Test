using System;
using UnityEngine;

namespace TD.Systems
{
    /// <summary>
    /// Provides persistence for player progression and settings.
    /// </summary>
    public class SaveManager : MonoBehaviour
    {
        public event Action SaveCompleted;
        public event Action LoadCompleted;

        private const string SaveSlotKey = "TD_SaveSlot";

        public SaveData CurrentSave { get; private set; }

        public void Initialize()
        {
            // TODO: Load save data from disk or cloud storage.
        }

        public void Save()
        {
            // TODO: Serialize CurrentSave and persist to storage.
        }

        public void Load()
        {
            // TODO: Deserialize save data and populate CurrentSave.
        }

        public void ResetProgress()
        {
            // TODO: Clear progression and create a new save state.
        }
    }

    [Serializable]
    public class SaveData
    {
        public int HighestUnlockedLevel;
        public int PlayerExperience;
        public int SoftCurrency;
        public int HardCurrency;
        public bool TutorialCompleted;
    }
}
