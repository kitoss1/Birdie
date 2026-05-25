using TMPro;
using UnityEngine;

namespace Birdie.UI.Minigames
{
    /// <summary>
    /// Reusable score display for minigames.
    /// Wraps a TextMeshProUGUI and provides a simple UpdateScore method.
    /// </summary>
    public sealed class MinigameScoreDisplay : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Text displaying the current score")]
        private TextMeshProUGUI m_scoreText;

        public void UpdateScore(int score)
        {
            if (m_scoreText != null)
            {
                m_scoreText.text = $"Puntuación: {score}";
            }
        }
    }
}
