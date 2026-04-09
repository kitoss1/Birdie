using Birdie.Debug;
using Birdie.Managers;
using Birdie.Save;
using System.Collections.Generic;
using UnityEngine;

namespace Birdie.Birds
{
    /// <summary>
    /// Bird feeder object that provides food for birds.
    /// Birds with eating behavior will be attracted to this object.
    /// </summary>
    public class BirdFeeder : BirdObject
    {
        [Header("Feeder Settings")]
        [SerializeField]
        [Tooltip("Visual elements per food level, index 0 is the first to be hidden")]
        private GameObject[] m_foodLevelVisuals;

        private int m_currentFoodLevel;

        public bool HasFoodAvailable => m_currentFoodLevel > 0;
        public int CurrentFoodLevel => m_currentFoodLevel;
        public int MaxFoodLevel => m_foodLevelVisuals?.Length ?? 0;

        private void Awake()
        {
            m_currentFoodLevel = MaxFoodLevel;
            UpdateFoodVisuals();

            if (string.IsNullOrEmpty(ObjectID))
            {
                DebugBase.Log($"[{nameof(BirdFeeder)}] Feeder initialized at {transform.position}", DebugCategory.Birds);
            }
        }

        private void Start()
        {
            LoadFromSaveData();
        }

        public override void OnBirdStartInteraction(Bird bird)
        {
            base.OnBirdStartInteraction(bird);

            DebugBase.Log($"[{nameof(BirdFeeder)}] {bird.BirdData?.BirdName} started eating from feeder", DebugCategory.Birds);

            // TODO: Play feeder animation (seeds visible, etc.)
            // TODO: Spawn particle effects (seeds falling)
        }

        public override void OnBirdEndInteraction(Bird bird)
        {
            base.OnBirdEndInteraction(bird);

            DebugBase.Log($"[{nameof(BirdFeeder)}] {bird.BirdData?.BirdName} finished eating from feeder", DebugCategory.Birds);

            m_currentFoodLevel--;
            DebugBase.Log($"[{nameof(BirdFeeder)}] Food level: {m_currentFoodLevel}/{MaxFoodLevel}", DebugCategory.Birds);

            UpdateFoodVisuals();
            SaveToSaveData();
        }

        public override bool CanBeUsedBy(Bird bird)
        {
            if (!HasFoodAvailable)
            {
                return false;
            }

            return base.CanBeUsedBy(bird);
        }

        /// <summary>
        /// Refills the feeder with food.
        /// Called when player upgrades or refills the feeder.
        /// </summary>
        public void Refill()
        {
            m_currentFoodLevel = MaxFoodLevel;
            UpdateFoodVisuals();
            SaveToSaveData();
            DebugBase.Log($"[{nameof(BirdFeeder)}] Feeder refilled to {MaxFoodLevel}", DebugCategory.Birds);

            // TODO: Play refill animation/effects
        }

        private void LoadFromSaveData()
        {
            SaveManager saveManager = GameManager.Instance?.SaveManager;

            if (saveManager?.CurrentSaveData == null)
            {
                DebugBase.LogWarning($"[{nameof(BirdFeeder)}] SaveManager not available, using default food level", DebugCategory.Birds);
                return;
            }

            List<FeederFoodEntry> entries = saveManager.CurrentSaveData.economy.feederFoodLevels;
            FeederFoodEntry entry = entries.Find(e => e.feederID == ObjectID);

            if (entry != null)
            {
                m_currentFoodLevel = entry.foodLevel;
                UpdateFoodVisuals();
                DebugBase.Log($"[{nameof(BirdFeeder)}] Loaded food level: {m_currentFoodLevel}/{MaxFoodLevel}", DebugCategory.Birds);
            }
        }

        private void SaveToSaveData()
        {
            SaveManager saveManager = GameManager.Instance?.SaveManager;

            if (saveManager?.CurrentSaveData == null)
            {
                DebugBase.LogWarning($"[{nameof(BirdFeeder)}] SaveManager not available, cannot save food level", DebugCategory.Birds);
                return;
            }

            List<FeederFoodEntry> entries = saveManager.CurrentSaveData.economy.feederFoodLevels;
            FeederFoodEntry entry = entries.Find(e => e.feederID == ObjectID);

            if (entry != null)
            {
                entry.foodLevel = m_currentFoodLevel;
            }
            else
            {
                entries.Add(new FeederFoodEntry { feederID = ObjectID, foodLevel = m_currentFoodLevel });
            }

            saveManager.SaveGame();
        }

        private void UpdateFoodVisuals()
        {
            if (m_foodLevelVisuals == null)
            {
                return;
            }

            for (int i = 0; i < m_foodLevelVisuals.Length; i++)
            {
                if (m_foodLevelVisuals[i] != null)
                {
                    m_foodLevelVisuals[i].SetActive(i < m_currentFoodLevel);
                }
            }
        }
    }
}
