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

        [Header("Claim Button")]
        [SerializeField]
        [Tooltip("Button shown when mission is complete and reward unclaimed")]
        private Button m_claimButton;

        [SerializeField]
        [Tooltip("Sprite used on the claim button when the mission is incomplete or already claimed")]
        private Sprite m_disabledButtonSprite;

        [SerializeField]
        [Tooltip("Text on the claim button showing the reward amount")]
        private TMP_Text m_claimButtonText;

        private int m_slotIndex;
        private Sprite m_defaultButtonSprite;

        /// <summary>
        /// Binds this entry to a mission slot. Must be called once before Refresh().
        /// </summary>
        public void Initialize(int slotIndex)
        {
            m_slotIndex = slotIndex;

            if (m_claimButton != null)
            {
                m_claimButton.onClick.AddListener(OnClaimButtonClicked);

                Image buttonImage = m_claimButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    m_defaultButtonSprite = buttonImage.sprite;
                }
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
                bool canClaim = isComplete && !isClaimed;
                m_claimButton.interactable = canClaim;

                Image buttonImage = m_claimButton.GetComponent<Image>();
                if (buttonImage != null)
                {
                    buttonImage.sprite = canClaim || m_disabledButtonSprite == null
                        ? m_defaultButtonSprite
                        : m_disabledButtonSprite;
                }
            }

            if (m_claimButtonText != null)
            {
                m_claimButtonText.text = $"+{reward}";
                m_claimButtonText.fontStyle = isClaimed ? FontStyles.Strikethrough : FontStyles.Normal;
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

            if (m_disabledButtonSprite == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(MissionEntryUI)}] Disabled Button Sprite is not assigned — button will keep its default sprite when incomplete or claimed!", this);
            }
        }
#endif
    }
}
