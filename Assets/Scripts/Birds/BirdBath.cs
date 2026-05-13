using Birdie.Debug;
using Birdie.Managers;
using Birdie.Save;
using System.Collections.Generic;
using UnityEngine;

namespace Birdie.Birds
{
    /// <summary>
    /// Bird bath object where birds can bathe and drink.
    /// The bath has water for a single use and must be refilled by the player.
    /// </summary>
    public class BirdBath : BirdObject
    {
        [Header("Bath Settings")]
        [SerializeField]
        [Tooltip("Visual representation of water, hidden when the bath is empty")]
        private GameObject m_waterVisual;

        [SerializeField]
        [Tooltip("Position inside the bath where the bird stands while bathing. Used by BathingBehavior for the jump-in phase.")]
        private Transform m_bathingPosition;

        private bool m_hasWater;

        public bool HasWater => m_hasWater;
        public Vector3 BathingPosition => m_bathingPosition != null ? m_bathingPosition.position : InteractionPosition;

        private void Awake()
        {
            m_hasWater = true;
            UpdateWaterVisual();

            if (string.IsNullOrEmpty(ObjectID))
            {
                DebugBase.Log($"[{nameof(BirdBath)}] Bird bath initialized at {transform.position}", DebugCategory.Birds);
            }
        }

        private void Start()
        {
            LoadFromSaveData();
        }

        public override void OnBirdStartInteraction(Bird bird)
        {
            base.OnBirdStartInteraction(bird);

            DebugBase.Log($"[{nameof(BirdBath)}] {bird.BirdData?.BirdName} started using bird bath", DebugCategory.Birds);
        }

        public override void OnBirdEndInteraction(Bird bird)
        {
            base.OnBirdEndInteraction(bird);

            DebugBase.Log($"[{nameof(BirdBath)}] {bird.BirdData?.BirdName} finished using bird bath", DebugCategory.Birds);
        }

        public void ConsumeWater()
        {
            m_hasWater = false;
            DebugBase.Log($"[{nameof(BirdBath)}] Bath is now empty", DebugCategory.Birds);
            UpdateWaterVisual();
            SaveToSaveData();
        }

        public override bool CanBeUsedBy(Bird bird)
        {
            if (!m_hasWater)
            {
                return false;
            }

            return base.CanBeUsedBy(bird);
        }

        /// <summary>
        /// Refills the bath with water.
        /// Called when the player refills it.
        /// </summary>
        public void Refill()
        {
            m_hasWater = true;
            UpdateWaterVisual();
            SaveToSaveData();
            DebugBase.Log($"[{nameof(BirdBath)}] Bird bath refilled", DebugCategory.Birds);
        }

        private void LoadFromSaveData()
        {
            SaveManager saveManager = GameManager.Instance?.SaveManager;

            if (saveManager?.CurrentSaveData == null)
            {
                DebugBase.LogWarning($"[{nameof(BirdBath)}] SaveManager not available, using default water state", DebugCategory.Birds);
                return;
            }

            List<BathWaterEntry> entries = saveManager.CurrentSaveData.economy.bathWaterStates;
            BathWaterEntry entry = entries.Find(e => e.bathID == ObjectID);

            if (entry != null)
            {
                m_hasWater = entry.hasWater;
                UpdateWaterVisual();
                DebugBase.Log($"[{nameof(BirdBath)}] Loaded water state: {m_hasWater}", DebugCategory.Birds);
            }
        }

        private void SaveToSaveData()
        {
            SaveManager saveManager = GameManager.Instance?.SaveManager;

            if (saveManager?.CurrentSaveData == null)
            {
                DebugBase.LogWarning($"[{nameof(BirdBath)}] SaveManager not available, cannot save water state", DebugCategory.Birds);
                return;
            }

            List<BathWaterEntry> entries = saveManager.CurrentSaveData.economy.bathWaterStates;
            BathWaterEntry entry = entries.Find(e => e.bathID == ObjectID);

            if (entry != null)
            {
                entry.hasWater = m_hasWater;
            }
            else
            {
                entries.Add(new BathWaterEntry { bathID = ObjectID, hasWater = m_hasWater });
            }

            saveManager.SaveGame();
        }

        private void UpdateWaterVisual()
        {
            if (m_waterVisual != null)
            {
                m_waterVisual.SetActive(m_hasWater);
            }
        }
    }
}
