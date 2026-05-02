using Birdie.Debug;
using Birdie.Managers;
using Birdie.Missions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI
{
    /// <summary>
    /// UI component for a single daily mission slot.
    /// Bound to a slot index via Initialize(); call Refresh() to sync display state.
    /// </summary>
    public class MissionEntryUI : MonoBehaviour
    {
        [Header("Labels")]
        [SerializeField]
        [Tooltip("Text showing the mission description with progress appended (e.g. 'Visit 3 birds (2/3)')")]
        private TMP_Text m_descriptionText;

        [Header("State Visuals")]
        [SerializeField]
        [Tooltip("Object shown when the reward has already been claimed")]
        private GameObject m_claimedIndicator;

        [Header("Claim Button")]
        [SerializeField]
        [Tooltip("Button shown when mission is complete and reward unclaimed")]
        private Button m_claimButton;

        [SerializeField]
        [Tooltip("Text on the claim button showing the reward amount")]
        private TMP_Text m_claimButtonText;

        private int m_slotIndex;

        /// <summary>
        /// Binds this entry to a mission slot. Must be called once before Refresh().
        /// </summary>
        public void Initialize(int slotIndex)
        {
            m_slotIndex = slotIndex;

            if (m_claimButton != null)
            {
                m_claimButton.onClick.AddListener(OnClaimButtonClicked);
            }
        }

        private void OnDestroy()
        {
            if (m_claimButton != null)
            {
                m_claimButton.onClick.RemoveListener(OnClaimButtonClicked);
            }
        }

        /// <summary>
        /// Reads the current mission state and updates all UI elements.
        /// </summary>
        public void Refresh()
        {
            DailyMissionManager manager = GameManager.Instance?.DailyMissionManager;
            if (manager == null)
            {
                return;
            }

            DailyMissionDefinition mission = manager.GetMission(m_slotIndex);
            if (mission == null)
            {
                DebugBase.LogWarning($"[{nameof(MissionEntryUI)}] No mission data for slot {m_slotIndex} — check the mission pool in DailyMissionManager");
                return;
            }

            int progress = manager.GetProgress(m_slotIndex);
            bool isComplete = manager.IsMissionComplete(m_slotIndex);
            bool isClaimed = manager.IsMissionClaimed(m_slotIndex);

            UpdateDescription(mission.Description, progress, mission.TargetCount, isClaimed);
            UpdateClaimButton(isComplete, isClaimed, mission.GoldenSeedsReward);
            UpdateClaimedIndicator(isClaimed);
        }

        private void UpdateDescription(string description, int progress, int target, bool isClaimed)
        {
            if (m_descriptionText == null)
            {
                DebugBase.LogWarning($"[{nameof(MissionEntryUI)}] Description Text is not assigned on slot {m_slotIndex}");
                return;
            }

            m_descriptionText.text = $"{description} ({progress}/{target})";
            m_descriptionText.fontStyle = isClaimed ? FontStyles.Strikethrough : FontStyles.Normal;
        }

        private void UpdateClaimButton(bool isComplete, bool isClaimed, int reward)
        {
            if (m_claimButton != null)
            {
                m_claimButton.gameObject.SetActive(isComplete);
                m_claimButton.enabled = !isClaimed;
            }

            if (m_claimButtonText != null)
            {
                m_claimButtonText.text = $"+{reward}";
            }
        }

        private void UpdateClaimedIndicator(bool isClaimed)
        {
            if (m_claimedIndicator != null)
            {
                m_claimedIndicator.SetActive(isClaimed);
            }
        }

        private void OnClaimButtonClicked()
        {
            DailyMissionManager manager = GameManager.Instance?.DailyMissionManager;
            if (manager == null)
            {
                return;
            }

            if (manager.ClaimReward(m_slotIndex))
            {
                Refresh();
                DebugBase.Log($"[{nameof(MissionEntryUI)}] Claimed reward for slot {m_slotIndex}");
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_descriptionText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(MissionEntryUI)}] Description Text reference is missing!", this);
            }

            if (m_claimButton == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(MissionEntryUI)}] Claim Button reference is missing!", this);
            }
        }
#endif
    }
}
