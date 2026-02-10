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

        public string MinigameName => m_minigameName;
        public GameObject MinigamePrefab => m_minigamePrefab;
        public Sprite Icon => m_icon;

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
