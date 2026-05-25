using System;

namespace Birdie.Save
{
    /// <summary>
    /// Save data for the audio system.
    /// Tracks volume levels and mute states for all audio channels.
    /// </summary>
    [Serializable]
    public class AudioSaveData
    {
        public float masterVolume = 1f;
        public float sfxVolume = 1f;
        public float musicVolume = 1f;
        public bool masterMuted = false;
        public bool sfxMuted = false;
        public bool musicMuted = false;

        /// <summary>
        /// Creates audio save data with default values.
        /// </summary>
        public AudioSaveData()
        {
        }

        /// <summary>
        /// Validates the save data integrity.
        /// </summary>
        public bool IsValid()
        {
            return masterVolume >= 0f && masterVolume <= 1f &&
                   sfxVolume >= 0f && sfxVolume <= 1f &&
                   musicVolume >= 0f && musicVolume <= 1f;
        }
    }
}
