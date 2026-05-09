using System;
using Birdie.Debug;
using Birdie.Save;
using UnityEngine;

namespace Birdie.Managers
{
    /// <summary>
    /// Manages the game's economy: golden seeds, rewards calculation, purchases.
    /// Handles the dual reward system (golden seeds + friendship points).
    /// </summary>
    public class EconomyManager : BaseManager
    {
        private int m_goldenSeeds = 0;

        /// <summary>
        /// Event fired when the golden seeds balance changes.
        /// Parameters: newBalance, changeAmount (positive for added, negative for spent).
        /// </summary>
        public event Action<int, int> OnGoldenSeedsChanged;

        public int GoldenSeeds => m_goldenSeeds;

        public override void Initialize(SaveManager saveManager = null)
        {
            base.Initialize(saveManager);
            if (m_saveManager != null)
                LoadFromSaveData();
            DebugBase.Log($"[{nameof(EconomyManager)}] Economy system initialized");
        }

        /// <summary>
        /// Adds golden seeds to the player's balance.
        /// </summary>
        public void AddGoldenSeeds(int amount)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            if (amount <= 0)
            {
                DebugBase.LogWarning($"[{nameof(EconomyManager)}] Cannot add non-positive amount: {amount}");
                return;
            }

            m_goldenSeeds += amount;
            DebugBase.Log($"[{nameof(EconomyManager)}] Added {amount} golden seeds. Total: {m_goldenSeeds}");

            OnGoldenSeedsChanged?.Invoke(m_goldenSeeds, amount);
            SaveToSaveData();
        }

        /// <summary>
        /// Sets the golden seeds to a specific value. Primarily used for debugging.
        /// </summary>
        public void SetGoldenSeeds(int amount)
        {
            if (!EnsureInitialized())
            {
                return;
            }

            int previousBalance = m_goldenSeeds;
            m_goldenSeeds = Mathf.Max(0, amount);
            int change = m_goldenSeeds - previousBalance;

            DebugBase.Log($"[{nameof(EconomyManager)}] Set golden seeds to {m_goldenSeeds}");

            OnGoldenSeedsChanged?.Invoke(m_goldenSeeds, change);
            SaveToSaveData();
        }

        /// <summary>
        /// Checks if the player can afford a purchase.
        /// </summary>
        public bool CanAfford(int cost)
        {
            return m_goldenSeeds >= cost;
        }

        /// <summary>
        /// Attempts to make a purchase.
        /// </summary>
        public bool TryPurchase(int cost)
        {
            if (!EnsureInitialized())
            {
                return false;
            }

            if (cost <= 0)
            {
                DebugBase.LogWarning($"[{nameof(EconomyManager)}] Invalid purchase cost: {cost}");
                return false;
            }

            if (CanAfford(cost))
            {
                m_goldenSeeds -= cost;
                DebugBase.Log($"[{nameof(EconomyManager)}] Purchase successful. Remaining: {m_goldenSeeds}");

                OnGoldenSeedsChanged?.Invoke(m_goldenSeeds, -cost);
                SaveToSaveData();
                return true;
            }

            DebugBase.LogWarning($"[{nameof(EconomyManager)}] Insufficient golden seeds. Required: {cost}, Available: {m_goldenSeeds}");
            return false;
        }

        /// <summary>
        /// Loads economy data from the save manager.
        /// </summary>
        private void LoadFromSaveData()
        {
            if (m_saveManager == null || m_saveManager.CurrentSaveData == null)
            {
                DebugBase.LogWarning($"[{nameof(EconomyManager)}] SaveManager or SaveData is null, cannot load");
                return;
            }

            EconomySaveData economyData = m_saveManager.CurrentSaveData.economy;

            m_goldenSeeds = economyData.goldenSeeds;

            DebugBase.Log($"[{nameof(EconomyManager)}] Loaded economy data. Golden seeds: {m_goldenSeeds}");

            OnGoldenSeedsChanged?.Invoke(m_goldenSeeds, 0);
        }

        /// <summary>
        /// Saves economy data to the save manager.
        /// </summary>
        private void SaveToSaveData()
        {
            if (m_saveManager == null || m_saveManager.CurrentSaveData == null)
            {
                DebugBase.LogWarning($"[{nameof(EconomyManager)}] SaveManager or SaveData is null, cannot save");
                return;
            }

            EconomySaveData economyData = m_saveManager.CurrentSaveData.economy;
            economyData.goldenSeeds = m_goldenSeeds;

            m_saveManager.SaveGame();

            DebugBase.Log($"[{nameof(EconomyManager)}] Saved economy data. Golden seeds: {m_goldenSeeds}");
        }
    }
}
