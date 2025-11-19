using System;
using UnityEngine;

namespace Birdie.Data
{
    [Serializable]
    public sealed class FriendshipLevelInfo
    {
        [SerializeField]
        private int m_level;

        [SerializeField]
        [Tooltip("Title of the unlock (e.g., 'First Contact', 'Known', 'Friend')")]
        private string m_levelTitle;

        [SerializeField]
        [TextArea(2, 4)]
        [Tooltip("Information or anecdote unlocked at this level")]
        private string m_unlockedInfo;

        [SerializeField]
        [Tooltip("Does this level unlock gifts?")]
        private bool m_unlocksGifts = false;

        public int Level
        {
            get => m_level;
            set => m_level = value;
        }

        public string LevelTitle
        {
            get => m_levelTitle;
            set => m_levelTitle = value;
        }

        public string UnlockedInfo
        {
            get => m_unlockedInfo;
            set => m_unlockedInfo = value;
        }

        public bool UnlocksGifts
        {
            get => m_unlocksGifts;
            set => m_unlocksGifts = value;
        }
    }
}
