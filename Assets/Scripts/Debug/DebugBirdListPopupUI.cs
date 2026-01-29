using UnityEngine;
using UnityEngine.UI;

namespace Birdie.Debug
{
    /// <summary>
    /// UI component holding references for the debug bird list popup.
    /// </summary>
    public class DebugBirdListPopupUI : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Parent transform for bird buttons (ScrollView content)")]
        private Transform m_contentParent;

        [SerializeField]
        [Tooltip("Button to close the popup")]
        private Button m_closeButton;

        [SerializeField]
        [Tooltip("Prefab for bird selection buttons")]
        private DebugButton m_buttonPrefab;

        public Transform ContentParent => m_contentParent;
        public Button CloseButton => m_closeButton;
        public DebugButton ButtonPrefab => m_buttonPrefab;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_contentParent == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(DebugBirdListPopupUI)}] Content Parent reference is missing!", this);
            }

            if (m_closeButton == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(DebugBirdListPopupUI)}] Close Button reference is missing!", this);
            }

            if (m_buttonPrefab == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(DebugBirdListPopupUI)}] Button Prefab reference is missing!", this);
            }
        }
#endif
    }
}
