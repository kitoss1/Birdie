using UnityEngine;

namespace Birdie.Data
{
    /// <summary>
    /// ScriptableObject that defines a minigame available for birds to play.
    /// Each bird species can reference one or more of these in its available minigames list.
    /// </summary>
    [CreateAssetMenu(fileName = "New Minigame", menuName = "Birdie/Minigame Data")]
    public sealed class MinigameData : ScriptableObject
    {
        [Header("Basic Information")]
        [SerializeField]
        [Tooltip("Display name of the minigame")]
        private string m_minigameName;

        [Header("Visual")]
        [SerializeField]
        [Tooltip("Prefab instantiated when this minigame is selected")]
        private GameObject m_minigamePrefab;

        [SerializeField]
        [Tooltip("Icon for UI display")]
        private Sprite m_icon;

        [Header("Rewards")]
        [SerializeField]
        [Tooltip("Score thresholds and friendship rewards (three tiers, lowest to highest)")]
        private MinigameRewardTier[] m_rewardTiers;

        [Header("Difficulty")]
        [SerializeReference]
        [Tooltip("Difficulty settings per friendship level (index 0 = level 0, etc.). " +
                 "If friendship level exceeds array length, the last entry is used.")]
        private MinigameDifficultySettings[] m_difficultyPerLevel;

        public string MinigameName => m_minigameName;
        public GameObject MinigamePrefab => m_minigamePrefab;
        public Sprite Icon => m_icon;
        public MinigameRewardTier[] RewardTiers => m_rewardTiers;

        public MinigameDifficultySettings GetDifficultyForLevel(int friendshipLevel)
        {
            if (m_difficultyPerLevel == null || m_difficultyPerLevel.Length == 0)
            {
                return null;
            }

            int clampedIndex = Mathf.Clamp(friendshipLevel, 0, m_difficultyPerLevel.Length - 1);
            return m_difficultyPerLevel[clampedIndex];
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrEmpty(m_minigameName))
            {
                UnityEngine.Debug.LogWarning($"[{nameof(MinigameData)}] Minigame name is empty on {name}");
            }

            if (m_minigamePrefab == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(MinigameData)}] Minigame prefab is not assigned on {name}");
            }

            ValidateRewardTiers();
        }

        private void ValidateRewardTiers()
        {
            if (m_rewardTiers == null || m_rewardTiers.Length == 0)
            {
                return;
            }

            int previousThreshold = -1;

            for (int i = 0; i < m_rewardTiers.Length; i++)
            {
                MinigameRewardTier tier = m_rewardTiers[i];
                if (tier == null)
                {
                    continue;
                }

                if (tier.ScoreThreshold <= 0)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[{nameof(MinigameData)}] Reward tier {i} on {name} has a non-positive score threshold ({tier.ScoreThreshold})");
                }

                if (tier.FriendshipReward <= 0)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[{nameof(MinigameData)}] Reward tier {i} on {name} has a non-positive friendship reward ({tier.FriendshipReward})");
                }

                if (tier.ScoreThreshold <= previousThreshold)
                {
                    UnityEngine.Debug.LogWarning(
                        $"[{nameof(MinigameData)}] Reward tiers on {name} are not sorted by ascending score threshold " +
                        $"(tier {i} threshold {tier.ScoreThreshold} <= previous {previousThreshold})");
                }

                previousThreshold = tier.ScoreThreshold;
            }
        }
#endif
    }
}
