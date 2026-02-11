using System;

namespace Birdie.Save
{
    /// <summary>
    /// Main save data container for the entire game.
    /// Contains all subsystem save data.
    /// </summary>
    [Serializable]
    public class SaveData
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;
        public long lastSaveTimestamp;
        public string lastSaveDateString;

        public DiarySaveData diary = new DiarySaveData();
        public EconomySaveData economy = new EconomySaveData();
        public FriendshipSaveData friendship = new FriendshipSaveData();
        public AudioSaveData audio = new AudioSaveData();

        /// <summary>
        /// Creates a new save data with default values.
        /// </summary>
        public SaveData()
        {
            version = CurrentVersion;
            UpdateSaveTimestamp();
        }

        /// <summary>
        /// Updates the save timestamp to the current time.
        /// </summary>
        public void UpdateSaveTimestamp()
        {
            DateTime now = DateTime.Now;
            lastSaveTimestamp = now.ToBinary();
            lastSaveDateString = now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// Validates the entire save data structure.
        /// </summary>
        public bool IsValid()
        {
            return version > 0 &&
                   diary != null && diary.IsValid() &&
                   economy != null && economy.IsValid() &&
                   friendship != null && friendship.IsValid() &&
                   audio != null && audio.IsValid();
        }

        /// <summary>
        /// Gets the last save date as a DateTime.
        /// </summary>
        public DateTime GetLastSaveDate()
        {
            try
            {
                return DateTime.FromBinary(lastSaveTimestamp);
            }
            catch
            {
                return DateTime.MinValue;
            }
        }
    }
}
