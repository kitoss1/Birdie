using System;
using System.Collections.Generic;
using System.Threading;
using Birdie.Data;
using Birdie.Debug;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Birdie.UI.Minigames
{
    /// <summary>
    /// Main controller for the Simon Says minigame.
    /// Manages game state, sequence generation, input validation, scoring, and game over flow.
    /// </summary>
    public sealed class SimonSaysUI : MonoBehaviour, IMinigame
    {
        [Header("Buttons")]
        [SerializeField]
        [Tooltip("The four color buttons (indices 0-3)")]
        private SimonSaysButton[] m_buttons;

        [Header("Score")]
        [SerializeField]
        [Tooltip("Text displaying the current score during gameplay")]
        private TextMeshProUGUI m_scoreText;

        [Header("Game Over")]
        [SerializeField]
        [Tooltip("Panel shown when the player makes a mistake")]
        private GameObject m_gameOverPanel;

        [SerializeField]
        [Tooltip("Text displaying the final score on the game over panel")]
        private TextMeshProUGUI m_finalScoreText;

        [SerializeField]
        [FormerlySerializedAs("m_playAgainButton")]
        [Tooltip("Button to close the minigame after game over")]
        private Button m_closeButton;

        [Header("Timing")]
        [SerializeField]
        [Tooltip("Delay before the first sequence plays after starting")]
        private float m_sequenceStartDelay = 1f;

        [SerializeField]
        [Tooltip("Gap between each button highlight in the sequence")]
        private float m_gapBetweenHighlights = 0.2f;

        [SerializeField]
        [Tooltip("Delay before the next round starts after a correct sequence")]
        private float m_nextRoundDelay = 0.8f;

        public event Action GameClosed;

        public int FriendshipReward => MinigameRewardTier.ResolveReward(m_rewardTiers, m_score);

        private MinigameRewardTier[] m_rewardTiers;
        private readonly List<int> m_sequence = new List<int>();
        private int m_currentInputIndex;
        private int m_score;
        private SimonState m_currentState;
        private CancellationToken m_destroyCancellation;

        private enum SimonState
        {
            WaitingToStart,
            PlayingSequence,
            WaitingForInput,
            GameOver,
        }

        private void Awake()
        {
            m_destroyCancellation = this.GetCancellationTokenOnDestroy();

            if (m_closeButton != null)
            {
                m_closeButton.onClick.AddListener(OnCloseClicked);
            }

            foreach (SimonSaysButton button in m_buttons)
            {
                if (button != null)
                {
                    button.Pressed += OnButtonPressed;
                }
            }
        }

        private void Start()
        {
            StartGame();
        }

        private void OnDestroy()
        {
            if (m_closeButton != null)
            {
                m_closeButton.onClick.RemoveListener(OnCloseClicked);
            }

            foreach (SimonSaysButton button in m_buttons)
            {
                if (button != null)
                {
                    button.Pressed -= OnButtonPressed;
                }
            }

            foreach (SimonSaysButton button in m_buttons)
            {
                if (button != null)
                {
                    button.GetComponent<Image>()?.DOKill();
                }
            }
        }

        private void StartGame()
        {
            m_sequence.Clear();
            m_score = 0;
            m_currentInputIndex = 0;
            m_currentState = SimonState.WaitingToStart;

            UpdateScoreDisplay();
            HideGameOver();
            SetAllButtonsInteractable(false);

            DebugBase.Log($"[{nameof(SimonSaysUI)}] Game started", DebugCategory.UI);

            PlayNextRoundAsync().Forget();
        }

        private async UniTaskVoid PlayNextRoundAsync()
        {
            m_currentState = SimonState.PlayingSequence;
            SetAllButtonsInteractable(false);

            await UniTask.Delay(
                TimeSpan.FromSeconds(m_sequenceStartDelay),
                cancellationToken: m_destroyCancellation);

            int randomIndex = UnityEngine.Random.Range(0, m_buttons.Length);
            m_sequence.Add(randomIndex);

            DebugBase.Log(
                $"[{nameof(SimonSaysUI)}] Round {m_sequence.Count}: sequence length = {m_sequence.Count}",
                DebugCategory.UI);

            await PlaySequenceAsync();

            m_currentInputIndex = 0;
            m_currentState = SimonState.WaitingForInput;
            SetAllButtonsInteractable(true);
        }

        private async UniTask PlaySequenceAsync()
        {
            for (int i = 0; i < m_sequence.Count; i++)
            {
                int buttonIndex = m_sequence[i];

                if (buttonIndex >= 0 && buttonIndex < m_buttons.Length && m_buttons[buttonIndex] != null)
                {
                    await m_buttons[buttonIndex].PlayHighlightAsync();
                }

                if (i < m_sequence.Count - 1)
                {
                    await UniTask.Delay(
                        TimeSpan.FromSeconds(m_gapBetweenHighlights),
                        cancellationToken: m_destroyCancellation);
                }
            }
        }

        private void OnButtonPressed(int colorIndex)
        {
            if (m_currentState != SimonState.WaitingForInput)
            {
                return;
            }

            if (colorIndex >= 0 && colorIndex < m_buttons.Length && m_buttons[colorIndex] != null)
            {
                m_buttons[colorIndex].PlayPressAnimation();
            }

            if (m_sequence[m_currentInputIndex] == colorIndex)
            {
                m_currentInputIndex++;

                if (m_currentInputIndex >= m_sequence.Count)
                {
                    OnRoundComplete();
                }
            }
            else
            {
                OnWrongInput();
            }
        }

        private void OnRoundComplete()
        {
            m_score++;
            UpdateScoreDisplay();

            DebugBase.Log(
                $"[{nameof(SimonSaysUI)}] Round complete! Score: {m_score}",
                DebugCategory.UI);

            SetAllButtonsInteractable(false);
            StartNextRoundAfterDelayAsync().Forget();
        }

        private async UniTaskVoid StartNextRoundAfterDelayAsync()
        {
            await UniTask.Delay(
                TimeSpan.FromSeconds(m_nextRoundDelay),
                cancellationToken: m_destroyCancellation);

            PlayNextRoundAsync().Forget();
        }

        private void OnWrongInput()
        {
            m_currentState = SimonState.GameOver;
            SetAllButtonsInteractable(false);

            DebugBase.Log(
                $"[{nameof(SimonSaysUI)}] Game over! Final score: {m_score}",
                DebugCategory.UI);

            ShowGameOver();
        }

        private void ShowGameOver()
        {
            if (m_gameOverPanel != null)
            {
                m_gameOverPanel.SetActive(true);
            }

            if (m_finalScoreText != null)
            {
                m_finalScoreText.text = $"Score: {m_score}";
            }
        }

        private void HideGameOver()
        {
            if (m_gameOverPanel != null)
            {
                m_gameOverPanel.SetActive(false);
            }
        }

        private void UpdateScoreDisplay()
        {
            if (m_scoreText != null)
            {
                m_scoreText.text = $"Score: {m_score}";
            }
        }

        private void SetAllButtonsInteractable(bool interactable)
        {
            foreach (SimonSaysButton button in m_buttons)
            {
                if (button != null)
                {
                    button.SetInteractable(interactable);
                }
            }
        }

        public void SetRewardTiers(MinigameRewardTier[] rewardTiers)
        {
            m_rewardTiers = rewardTiers;
        }

        private void OnCloseClicked()
        {
            DebugBase.Log($"[{nameof(SimonSaysUI)}] Close clicked after game over", DebugCategory.UI);
            GameClosed?.Invoke();
        }
    }
}
