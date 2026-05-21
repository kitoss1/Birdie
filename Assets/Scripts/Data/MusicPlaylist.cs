using System.Collections.Generic;
using UnityEngine;

namespace Birdie.Data
{
    /// <summary>
    /// ScriptableObject that holds a list of background music tracks and playback settings.
    /// Assign to SoundManager to auto-start on game load.
    /// </summary>
    [CreateAssetMenu(fileName = "MusicPlaylist", menuName = "Birdie/Music Playlist")]
    public class MusicPlaylist : ScriptableObject
    {
        [SerializeField] private List<AudioClip> m_tracks = new List<AudioClip>();
        [SerializeField] private bool m_shuffle;

        public int TrackCount => m_tracks.Count;

        public bool Shuffle => m_shuffle;

        /// <summary>
        /// Returns the track at the given index, or null if out of range.
        /// </summary>
        public AudioClip GetTrack(int index)
        {
            if (index < 0 || index >= m_tracks.Count)
            {
                return null;
            }

            return m_tracks[index];
        }
    }
}
