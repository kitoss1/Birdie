using UnityEngine;

namespace Birdie.Managers
{
    /// <summary>
    /// Manages the game's economy: golden seeds, rewards calculation, purchases.
    /// Handles the dual reward system (golden seeds + friendship points).
    /// </summary>
    public class EconomyManager : BaseManager
    {
        [Header("Economy")]
        [SerializeField]
        private int m_goldenSeeds = 0;

        public int GoldenSeeds => m_goldenSeeds;

        public override void Initialize(GameManager gameManager)
        {
            base.Initialize(gameManager);
            Debug.Log($"[{nameof(EconomyManager)}] Economy system initialized");
        }

        /// <summary>
        /// Adds golden seeds to the player's balance
        /// </summary>
        public void AddGoldenSeeds(int amount)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            m_goldenSeeds += amount;
            Debug.Log($"[{nameof(EconomyManager)}] Added {amount} golden seeds. Total: {m_goldenSeeds}");
        }

        /// <summary>
        /// Checks if the player can afford a purchase
        /// </summary>
        public bool CanAfford(int cost)
        {
            return m_goldenSeeds >= cost;
        }

        /// <summary>
        /// Attempts to make a purchase
        /// </summary>
        public bool TryPurchase(int cost)
        {
            if (!EnsureInitialized())
            {
                return false;
            }

            if (CanAfford(cost))
            {
                m_goldenSeeds -= cost;
                Debug.Log($"[{nameof(EconomyManager)}] Purchase successful. Remaining: {m_goldenSeeds}");
                return true;
            }

            Debug.LogWarning($"[{nameof(EconomyManager)}] Insufficient golden seeds");
            return false;
        }
    }
}
