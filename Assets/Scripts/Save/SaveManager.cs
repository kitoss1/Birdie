using Birdie.Debug;
using System;
using System.IO;
using UnityEngine;

namespace Birdie.Save
{
    /// <summary>
    /// Manages game save/load operations with file I/O and backup system.
    /// Handles auto-save, manual save, and data persistence.
    /// </summary>
    public class SaveManager
    {
        private const string SaveFileName = "save.json";
        private const string BackupFileName = "save_backup.json";
        private const string SaveFolderName = "Birdie";

        private SaveData m_currentSaveData;
        private string m_saveFolderPath;
        private string m_saveFilePath;
        private string m_backupFilePath;

        public SaveData CurrentSaveData => m_currentSaveData;

        public event Action OnSaveCompleted;
        public event Action OnLoadCompleted;
        public event Action<SaveData> OnNewGameStarted;

        /// <summary>
        /// Initializes the save manager and sets up file paths.
        /// </summary>
        public void Initialize()
        {
            SetupFilePaths();
            DebugBase.Log($"[{nameof(SaveManager)}] Initialized. Save path: {m_saveFilePath}", DebugCategory.General);
        }

        /// <summary>
        /// Sets up the save file directory and paths.
        /// </summary>
        private void SetupFilePaths()
        {
            m_saveFolderPath = Path.Combine(Application.persistentDataPath, SaveFolderName);
            m_saveFilePath = Path.Combine(m_saveFolderPath, SaveFileName);
            m_backupFilePath = Path.Combine(m_saveFolderPath, BackupFileName);

            if (!Directory.Exists(m_saveFolderPath))
            {
                Directory.CreateDirectory(m_saveFolderPath);
                DebugBase.Log($"[{nameof(SaveManager)}] Created save directory: {m_saveFolderPath}", DebugCategory.General);
            }
        }

        /// <summary>
        /// Loads the game save data. Creates new save if none exists.
        /// </summary>
        public bool LoadGame()
        {
            if (!File.Exists(m_saveFilePath))
            {
                DebugBase.Log($"[{nameof(SaveManager)}] No save file found, creating new game", DebugCategory.General);
                CreateNewSave();
                return true;
            }

            try
            {
                string jsonData = File.ReadAllText(m_saveFilePath);
                SaveData loadedData = JsonUtility.FromJson<SaveData>(jsonData);

                if (loadedData == null || !loadedData.IsValid())
                {
                    DebugBase.LogWarning($"[{nameof(SaveManager)}] Save data is invalid, attempting backup restore", DebugCategory.General);
                    return TryRestoreFromBackup();
                }

                m_currentSaveData = loadedData;
                DebugBase.Log($"[{nameof(SaveManager)}] Game loaded successfully. Last save: {loadedData.lastSaveDateString}", DebugCategory.General);
                OnLoadCompleted?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                DebugBase.LogError($"[{nameof(SaveManager)}] Failed to load save: {e.Message}", DebugCategory.General);
                return TryRestoreFromBackup();
            }
        }

        /// <summary>
        /// Attempts to restore save data from backup file.
        /// </summary>
        private bool TryRestoreFromBackup()
        {
            if (!File.Exists(m_backupFilePath))
            {
                DebugBase.LogWarning($"[{nameof(SaveManager)}] No backup file found, creating new game", DebugCategory.General);
                CreateNewSave();
                return true;
            }

            try
            {
                string backupJson = File.ReadAllText(m_backupFilePath);
                SaveData backupData = JsonUtility.FromJson<SaveData>(backupJson);

                if (backupData == null || !backupData.IsValid())
                {
                    DebugBase.LogError($"[{nameof(SaveManager)}] Backup is also corrupted, creating new game", DebugCategory.General);
                    CreateNewSave();
                    return true;
                }

                m_currentSaveData = backupData;
                SaveGame();
                DebugBase.Log($"[{nameof(SaveManager)}] Restored from backup successfully", DebugCategory.General);
                OnLoadCompleted?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                DebugBase.LogError($"[{nameof(SaveManager)}] Failed to restore from backup: {e.Message}", DebugCategory.General);
                CreateNewSave();
                return false;
            }
        }

        /// <summary>
        /// Saves the current game data to file with backup.
        /// </summary>
        public bool SaveGame()
        {
            if (m_currentSaveData == null)
            {
                DebugBase.LogError($"[{nameof(SaveManager)}] Cannot save: SaveData is null", DebugCategory.General);
                return false;
            }

            try
            {
                m_currentSaveData.UpdateSaveTimestamp();

                string jsonData = JsonUtility.ToJson(m_currentSaveData, true);

                CreateBackup();

                File.WriteAllText(m_saveFilePath, jsonData);

                DebugBase.Log($"[{nameof(SaveManager)}] Game saved successfully", DebugCategory.General);
                OnSaveCompleted?.Invoke();
                return true;
            }
            catch (Exception e)
            {
                DebugBase.LogError($"[{nameof(SaveManager)}] Failed to save game: {e.Message}", DebugCategory.General);
                return false;
            }
        }

        /// <summary>
        /// Creates a backup of the current save file.
        /// </summary>
        private void CreateBackup()
        {
            if (File.Exists(m_saveFilePath))
            {
                try
                {
                    File.Copy(m_saveFilePath, m_backupFilePath, true);
                    DebugBase.Log($"[{nameof(SaveManager)}] Backup created", DebugCategory.General);
                }
                catch (Exception e)
                {
                    DebugBase.LogWarning($"[{nameof(SaveManager)}] Failed to create backup: {e.Message}", DebugCategory.General);
                }
            }
        }

        /// <summary>
        /// Creates a new save file with default values.
        /// </summary>
        public void CreateNewSave()
        {
            m_currentSaveData = new SaveData();
            SaveGame();
            DebugBase.Log($"[{nameof(SaveManager)}] New save created", DebugCategory.General);
            OnNewGameStarted?.Invoke(m_currentSaveData);
        }

        /// <summary>
        /// Deletes the current save and backup files.
        /// </summary>
        public void DeleteSave()
        {
            try
            {
                if (File.Exists(m_saveFilePath))
                {
                    File.Delete(m_saveFilePath);
                }

                if (File.Exists(m_backupFilePath))
                {
                    File.Delete(m_backupFilePath);
                }

                CreateNewSave();
                DebugBase.Log($"[{nameof(SaveManager)}] Save deleted and reset", DebugCategory.General);
            }
            catch (Exception e)
            {
                DebugBase.LogError($"[{nameof(SaveManager)}] Failed to delete save: {e.Message}", DebugCategory.General);
            }
        }

        /// <summary>
        /// Checks if a save file exists.
        /// </summary>
        public bool SaveFileExists()
        {
            return File.Exists(m_saveFilePath);
        }

        /// <summary>
        /// Gets the save file path for debugging purposes.
        /// </summary>
        public string GetSaveFilePath()
        {
            return m_saveFilePath;
        }

        /// <summary>
        /// Opens the save folder in the system file explorer.
        /// </summary>
        public void OpenSaveFolder()
        {
            if (Directory.Exists(m_saveFolderPath))
            {
                Application.OpenURL("file://" + m_saveFolderPath);
            }
        }
    }
}
