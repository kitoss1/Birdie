using System.Collections.Generic;
using Birdie.Data;
using Birdie.Debug;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI.Minigames
{
    /// <summary>
    /// Displays a progress bar with threshold markers showing the friendship reward
    /// the player will earn based on their current minigame score.
    /// </summary>
    public sealed class MinigameRewardBar : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Fill-type image representing score progress")]
        private Image m_fillBar;

        [SerializeField]
        [Tooltip("Text showing the current friendship reward (e.g. '+10')")]
        private TextMeshProUGUI m_friendshipRewardText;

        [SerializeField]
        [Tooltip("Inactive template for threshold markers placed inside the bar")]
        private RectTransform m_thresholdMarkerTemplate;

        [SerializeField]
        [Tooltip("Parent transform for spawned threshold markers")]
        private RectTransform m_markerContainer;


        private MinigameRewardTier[] m_rewardTiers;
        private int m_maxScore;
        private int m_completionReward;
        private bool m_reversed;
        private readonly List<RectTransform> m_spawnedMarkers = new List<RectTransform>();

        /// <summary>
        /// Stores the reward tiers, computes the max score, and spawns threshold markers.
        /// Pass <paramref name="maxScore"/> to override the max score derived from tier thresholds —
        /// required when the game's scoring range doesn't match the highest tier threshold (e.g. Sliding Puzzle).
        /// </summary>
        public void Initialize(MinigameRewardTier[] tiers, int completionReward = 0, bool reversed = false, int maxScore = 0)
        {
            m_rewardTiers = tiers;
            m_completionReward = completionReward;
            m_reversed = reversed;
            m_maxScore = maxScore > 0 ? maxScore : MinigameRewardTier.ComputeMaxScore(tiers);

            ClearMarkers();
            RectTransform completionMarker = m_reversed ? SpawnCompletionMarker() : null;
            SpawnMarkers(completionMarker);
            UpdateScore(0);

            DebugBase.Log(
                $"[{nameof(MinigameRewardBar)}] Initialized with {tiers?.Length ?? 0} tiers, " +
                $"completion reward {m_completionReward}, max score {m_maxScore}",
                DebugCategory.UI);
        }

        /// <summary>
        /// Updates the fill bar and friendship reward text based on the current score.
        /// </summary>
        public void UpdateScore(int score)
        {
            if (m_fillBar != null)
            {
                float fill = m_maxScore > 0 ? Mathf.Clamp01((float)score / m_maxScore) : 0f;
                m_fillBar.fillAmount = fill;
            }

            if (m_friendshipRewardText != null)
            {
                int reward = MinigameRewardTier.ResolveReward(m_rewardTiers, score, m_completionReward);
                m_friendshipRewardText.text = reward > 0 ? $"+{reward}" : "0";
            }
        }

        private void OnDestroy()
        {
            ClearMarkers();
        }

        private RectTransform SpawnCompletionMarker()
        {
            if (m_completionReward <= 0)
            {
                return null;
            }

            if (m_thresholdMarkerTemplate == null || m_markerContainer == null)
            {
                return null;
            }

            var marker = Instantiate(m_thresholdMarkerTemplate, m_markerContainer);
            marker.gameObject.SetActive(true);
            marker.anchorMin = new Vector2(0f, 0f);
            marker.anchorMax = new Vector2(0f, 1f);
            marker.anchoredPosition = Vector2.zero;

            var label = marker.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
            {
                label.text = $"+{m_completionReward}";
            }

            m_spawnedMarkers.Add(marker);
            return marker;
        }

        private void SpawnMarkers(RectTransform completionMarker)
        {
            if (m_thresholdMarkerTemplate == null || m_markerContainer == null || m_rewardTiers == null)
            {
                return;
            }

            if (m_maxScore <= 0)
            {
                return;
            }

            RectTransform leftmostMarker = null;
            RectTransform rightmostMarker = null;
            float leftmostPos = float.MaxValue;
            float rightmostPos = float.MinValue;

            foreach (MinigameRewardTier tier in m_rewardTiers)
            {
                if (tier == null)
                {
                    continue;
                }

                float normalizedPosition = (float)tier.ScoreThreshold / m_maxScore;

                var marker = Instantiate(m_thresholdMarkerTemplate, m_markerContainer);
                marker.gameObject.SetActive(true);
                marker.anchorMin = new Vector2(normalizedPosition, 0f);
                marker.anchorMax = new Vector2(normalizedPosition, 1f);
                marker.anchoredPosition = Vector2.zero;

                var label = marker.GetComponentInChildren<TextMeshProUGUI>();
                if (label != null)
                {
                    label.text = $"+{tier.FriendshipReward}";
                }

                m_spawnedMarkers.Add(marker);

                if (normalizedPosition < leftmostPos)
                {
                    leftmostPos = normalizedPosition;
                    leftmostMarker = marker;
                }

                if (normalizedPosition > rightmostPos)
                {
                    rightmostPos = normalizedPosition;
                    rightmostMarker = marker;
                }
            }

            if (m_reversed)
            {
                if (rightmostMarker != null && Mathf.Approximately(rightmostPos, 1f))
                {
                    rightmostMarker.gameObject.SetActive(false);
                }

                RectTransform visualLeftmost = completionMarker ?? leftmostMarker;
                if (visualLeftmost != null)
                {
                    var line = visualLeftmost.GetComponentInChildren<Image>();
                    if (line != null)
                    {
                        line.enabled = false;
                    }
                }
            }
            else
            {
                if (rightmostMarker != null)
                {
                    var line = rightmostMarker.GetComponentInChildren<Image>();
                    if (line != null)
                    {
                        line.enabled = false;
                    }
                }
            }
        }

        private void ClearMarkers()
        {
            foreach (RectTransform marker in m_spawnedMarkers)
            {
                if (marker != null)
                {
                    Destroy(marker.gameObject);
                }
            }

            m_spawnedMarkers.Clear();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_fillBar == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"[{nameof(MinigameRewardBar)}] Fill bar Image is not assigned", this);
            }

        }
#endif
    }
}
