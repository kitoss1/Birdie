using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.Debug
{
    /// <summary>
    /// UI component holding references for the debug bird detail popup.
    /// </summary>
    public class DebugBirdDetailPopupUI : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Header text displaying the bird name")]
        private TextMeshProUGUI m_headerText;

        [SerializeField]
        [Tooltip("Text displaying current friendship points")]
        private TextMeshProUGUI m_currentFriendshipText;

        [SerializeField]
        [Tooltip("Text displaying current friendship level")]
        private TextMeshProUGUI m_currentLevelText;

        [SerializeField]
        [Tooltip("Input field for entering new friendship value")]
        private TMP_InputField m_friendshipInput;

        [SerializeField]
        [Tooltip("Button to apply the new friendship value")]
        private Button m_applyButton;

        [SerializeField]
        [Tooltip("Button to go back to the bird list")]
        private Button m_backButton;

        [SerializeField]
        [Tooltip("Button to close all popups")]
        private Button m_closeButton;

        [SerializeField]
        [Tooltip("Button to add 50 friendship points to the selected bird")]
        private Button m_addFiftyButton;

        public TextMeshProUGUI HeaderText => m_headerText;
        public TextMeshProUGUI CurrentFriendshipText => m_currentFriendshipText;
        public TextMeshProUGUI CurrentLevelText => m_currentLevelText;
        public TMP_InputField FriendshipInput => m_friendshipInput;
        public Button ApplyButton => m_applyButton;
        public Button BackButton => m_backButton;
        public Button CloseButton => m_closeButton;
        public Button AddFiftyButton => m_addFiftyButton;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (m_headerText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(DebugBirdDetailPopupUI)}] Header Text reference is missing!", this);
            }

            if (m_currentFriendshipText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(DebugBirdDetailPopupUI)}] Current Friendship Text reference is missing!", this);
            }

            if (m_currentLevelText == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(DebugBirdDetailPopupUI)}] Current Level Text reference is missing!", this);
            }

            if (m_friendshipInput == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(DebugBirdDetailPopupUI)}] Friendship Input reference is missing!", this);
            }

            if (m_applyButton == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(DebugBirdDetailPopupUI)}] Apply Button reference is missing!", this);
            }

            if (m_backButton == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(DebugBirdDetailPopupUI)}] Back Button reference is missing!", this);
            }

            if (m_closeButton == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(DebugBirdDetailPopupUI)}] Close Button reference is missing!", this);
            }

            if (m_addFiftyButton == null)
            {
                UnityEngine.Debug.LogWarning($"[{nameof(DebugBirdDetailPopupUI)}] Add Fifty Button reference is missing!", this);
            }
        }
#endif
    }
}
