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

        [Header("Rewards")]
        [SerializeField]
        [Tooltip("Friendship reward awarded just for completing the minigame regardless of score")]
        [Min(0)]
        private int m_completionReward;

        [Header("Difficulty")]
        [SerializeReference]
        [Tooltip("Difficulty settings per friendship level (index 0 = level 0, etc.). " +
                 "If friendship level exceeds array length, the last entry is used.")]
        private MinigameDifficultySettings[] m_difficultyPerLevel;

        public string MinigameName => m_minigameName;
        public GameObject MinigamePrefab => m_minigamePrefab;
        public int CompletionReward => m_completionReward;

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
        }
#endif
    }
}
