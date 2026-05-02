using Birdie.Debug;
using Birdie.Managers;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI
{
    /// <summary>
    /// UI component for the Daily Missions menu panel.
    /// Owns three MissionEntryUI slots and reacts to DailyMissionManager events.
    /// </summary>
    public class DailyMissionsMenuUI : MonoBehaviour
    {
        [Header("Controls")]
        [SerializeField]
        [Tooltip("Button to close the daily missions menu")]
        private Button m_closeButton;

        [Header("Mission Entries")]
        [SerializeField]
        [Tooltip("UI components for each of the three daily mission slots (must have exactly 3)")]
        private MissionEntryUI[] m_missionEntries;

        private void Awake()
        {
            SetupListeners();
            InitializeMissionEntries();
        }

        private void OnEnable()
        {
            SubscribeToEvents();

            if (GameManager.Instance?.IsInitializationComplete == true)
            {
                RefreshUI();
            }
        }

        private void OnDisable()
        {
            UnsubscribeFromEvents();
        }

        private void OnDestroy()
        {
            RemoveListeners();
        }

        private void SetupListeners()
        {
            if (m_closeButton != null)
            {
                m_closeButton.onClick.AddListener(OnCloseButtonClicked);
            }
        }

        private void RemoveListeners()
        {
            if (m_closeButton != null)
            {
                m_closeButton.onClick.RemoveListener(OnCloseButtonClicked);
            }
        }

        private void InitializeMissionEntries()
        {
            if (m_missionEntries == null)
            {
                return;
            }

            for (int i = 0; i < m_missionEntries.Length; i++)
            {
                m_missionEntries[i]?.Initialize(i);
            }
        }

        private void SubscribeToEvents()
        {
            DailyMissionManager manager = GameManager.Instance?.DailyMissionManager;
            if (manager != null)
            {
                manager.OnMissionProgressChanged += OnMissionProgressChanged;
                manager.OnMissionCompleted += OnMissionCompleted;
                manager.OnDailyMissionsRefreshed += RefreshUI;
            }
        }

        private void UnsubscribeFromEvents()
        {
            DailyMissionManager manager = GameManager.Instance?.DailyMissionManager;
            if (manager != null)
            {
                manager.OnMissionProgressChanged -= OnMissionProgressChanged;
                manager.OnMissionCompleted -= OnMissionCompleted;
                manager.OnDailyMissionsRefreshed -= RefreshUI;
            }
        }

        private void RefreshUI()
        {
            if (m_missionEntries == null)
            {
                return;
            }

            foreach (MissionEntryUI entry in m_missionEntries)
            {
                entry?.Refresh();
            }
        }

        private void OnMissionProgressChanged(int missionIndex)
        {
            RefreshEntry(missionIndex);
        }

        private void OnMissionCompleted(int missionIndex)
        {
            RefreshEntry(missionIndex);
        }

        private void RefreshEntry(int missionIndex)
        {
            if (m_missionEntries == null || missionIndex >= m_missionEntries.Length)
            {
                return;
            }

            m_missionEntries[missionIndex]?.Refresh();
        }

        private void OnCloseButtonClicked()
        {
            GameManager.Instance?.MenuManager?.CloseCurrentMenu();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_closeButton == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(DailyMissionsMenuUI)}] Close Button reference is missing!", this);
            }

            if (m_missionEntries == null || m_missionEntries.Length != 3)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(DailyMissionsMenuUI)}] Mission Entries array must have exactly 3 entries!", this);
            }
        }
#endif
    }
}
