using System;
using UnityEngine;

namespace Birdie.Data
{
    /// <summary>
    /// Serializable entry pairing a string key with an AudioClip.
    /// Used by SoundLibrary for inspector configuration.
    /// </summary>
    [Serializable]
    public class SoundEntry
    {
        [SerializeField] private string m_key;
        [SerializeField] private AudioClip m_clip;

        public string Key => m_key;

        public AudioClip Clip => m_clip;
    }
}
