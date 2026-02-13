using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI.Minigames
{
    /// <summary>
    /// Reusable game over panel for minigames.
    /// Displays score, friendship reward, and a contextual title.
    /// </summary>
    public sealed class MinigameGameOverPanel : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Title text (e.g. 'Congratulations!' or 'Good luck next time!')")]
        private TextMeshProUGUI m_titleText;

        [SerializeField]
        [Tooltip("Text displaying the final score")]
        private TextMeshProUGUI m_scoreText;

        [SerializeField]
        [Tooltip("Text displaying the friendship reward earned")]
        private TextMeshProUGUI m_friendshipWonText;

        [SerializeField]
        [Tooltip("Button to close the minigame after game over")]
        private Button m_closeButton;

        public event Action CloseClicked;

        private void Awake()
        {
            if (m_closeButton != null)
            {
                m_closeButton.onClick.AddListener(OnCloseClicked);
            }
        }

        private void OnDestroy()
        {
            if (m_closeButton != null)
            {
                m_closeButton.onClick.RemoveListener(OnCloseClicked);
            }
        }

        public void Show(int score, int friendshipReward)
        {
            transform.SetAsLastSibling();
            gameObject.SetActive(true);

            if (m_titleText != null)
            {
                m_titleText.text = friendshipReward > 0 ? "Congratulations!" : "Good luck next time!";
            }

            if (m_scoreText != null)
            {
                m_scoreText.text = $"Score: {score}";
            }

            if (m_friendshipWonText != null)
            {
                m_friendshipWonText.text = friendshipReward > 0 ? $"+{friendshipReward}" : "0";
            }
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnCloseClicked()
        {
            CloseClicked?.Invoke();
        }
    }
}
