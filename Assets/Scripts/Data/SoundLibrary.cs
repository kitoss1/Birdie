using System.Collections.Generic;
using Birdie.Debug;
using UnityEngine;

namespace Birdie.Data
{
    /// <summary>
    /// ScriptableObject that maps string keys to AudioClips.
    /// Used by SoundManager to look up sounds by name instead of direct clip references.
    /// </summary>
    [CreateAssetMenu(fileName = "SoundLibrary", menuName = "Birdie/Sound Library")]
    public class SoundLibrary : ScriptableObject
    {
        [SerializeField] private List<SoundEntry> m_sounds = new List<SoundEntry>();

        private Dictionary<string, AudioClip> m_lookupCache;

        /// <summary>
        /// Builds the internal lookup dictionary from the serialized list.
        /// Must be called once before using GetClip.
        /// </summary>
        public void Initialize()
        {
            m_lookupCache = new Dictionary<string, AudioClip>();

            foreach (SoundEntry entry in m_sounds)
            {
                if (string.IsNullOrEmpty(entry.Key) || entry.Clip == null)
                {
                    DebugBase.LogWarning($"[{nameof(SoundLibrary)}] Skipping invalid entry: key='{entry.Key}'");
                    continue;
                }

                if (!m_lookupCache.TryAdd(entry.Key, entry.Clip))
                {
                    DebugBase.LogWarning($"[{nameof(SoundLibrary)}] Duplicate key: '{entry.Key}'");
                }
            }

            DebugBase.Log($"[{nameof(SoundLibrary)}] Initialized with {m_lookupCache.Count} entries");
        }

        /// <summary>
        /// Retrieves an AudioClip by its string key.
        /// Returns null if the key is not found.
        /// </summary>
        public AudioClip GetClip(string key)
        {
            if (m_lookupCache == null)
            {
                DebugBase.LogError($"[{nameof(SoundLibrary)}] Not initialized! Call Initialize() first.");
                return null;
            }

            if (m_lookupCache.TryGetValue(key, out AudioClip clip))
            {
                return clip;
            }

            DebugBase.LogWarning($"[{nameof(SoundLibrary)}] Clip not found for key: '{key}'");
            return null;
        }

        /// <summary>
        /// Checks if a key exists in the library.
        /// </summary>
        public bool HasClip(string key)
        {
            return m_lookupCache != null && m_lookupCache.ContainsKey(key);
        }
    }
}
