using Birdie.Debug;
using UnityEngine;

namespace Birdie.Managers
{
    /// <summary>
    /// Manages all bird-related logic: spawning, appearance times, weighted selection, pity system.
    /// Responsible for the core loop of birds visiting the habitat.
    /// </summary>
    public class BirdManager : BaseManager
    {
        [Header("Bird Spawning")]
        [SerializeField]
        private float m_spawnCheckInterval = 30f;

        [SerializeField]
        private Transform m_spawnParent;

        private bool m_isSpawningPaused = false;

        public override void Initialize(GameManager gameManager)
        {
            base.Initialize(gameManager);

            DebugBase.Log($"[{nameof(BirdManager)}] Setting up bird spawning system...");
        }

        /// <summary>
        /// Pauses bird spawning (called when minigame starts)
        /// </summary>
        public void PauseBirdSpawning()
        {
            m_isSpawningPaused = true;
            DebugBase.Log($"[{nameof(BirdManager)}] Bird spawning paused");
        }

        /// <summary>
        /// Resumes bird spawning (called when minigame ends)
        /// </summary>
        public void ResumeBirdSpawning()
        {
            m_isSpawningPaused = false;
            DebugBase.Log($"[{nameof(BirdManager)}] Bird spawning resumed");
        }
    }
}
