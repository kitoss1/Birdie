using System;
using System.Collections.Generic;
using System.Threading;
using Birdie.Data;
using Birdie.Debug;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
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
        [Tooltip("Reusable score display component")]
        private MinigameScoreDisplay m_scoreDisplay;

        [Header("Reward Bar")]
        [SerializeField]
        [Tooltip("Progress bar showing score thresholds and friendship rewards")]
        private MinigameRewardBar m_rewardBar;

        [Header("Game Over")]
        [SerializeField]
        [Tooltip("Reusable game over panel component")]
        private MinigameGameOverPanel m_gameOverPanel;

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

        public int FriendshipReward => MinigameRewardTier.ResolveReward(m_rewardTiers, m_score, m_completionReward);

        private MinigameRewardTier[] m_rewardTiers;
        private int m_completionReward;
        private readonly List<int> m_sequence = new List<int>();
        private int m_currentInputIndex;
        private int m_score;
        private int m_maxScore;
        private int m_difficultyMaxRounds;
        private SimonState m_currentState;
        private CancellationToken m_destroyCancellation;
        private Action<int>[] m_buttonHandlers;

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

            if (m_gameOverPanel != null)
            {
                m_gameOverPanel.CloseClicked += OnCloseClicked;
            }

            m_buttonHandlers = new Action<int>[m_buttons.Length];

            for (int i = 0; i < m_buttons.Length; i++)
            {
                if (m_buttons[i] != null)
                {
                    int arrayIndex = i;
                    m_buttonHandlers[i] = _ => OnButtonPressed(arrayIndex);
                    m_buttons[i].Pressed += m_buttonHandlers[i];
                }
            }
        }

        private void OnDestroy()
        {
            if (m_gameOverPanel != null)
            {
                m_gameOverPanel.CloseClicked -= OnCloseClicked;
            }

            for (int i = 0; i < m_buttons.Length; i++)
            {
                if (m_buttons[i] != null && m_buttonHandlers != null && m_buttonHandlers[i] != null)
                {
                    m_buttons[i].Pressed -= m_buttonHandlers[i];
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

        public void StartGame()
        {
            m_sequence.Clear();
            m_score = 0;
            m_currentInputIndex = 0;
            m_currentState = SimonState.WaitingToStart;

            m_scoreDisplay?.UpdateScore(m_score);
            m_rewardBar?.UpdateScore(0);
            m_gameOverPanel?.Hide();
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

        private void OnButtonPressed(int arrayIndex)
        {
            if (m_currentState != SimonState.WaitingForInput)
            {
                return;
            }

            if (arrayIndex >= 0 && arrayIndex < m_buttons.Length && m_buttons[arrayIndex] != null)
            {
                m_buttons[arrayIndex].PlayPressAnimation();
            }

            if (m_sequence[m_currentInputIndex] == arrayIndex)
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
            m_scoreDisplay?.UpdateScore(m_score);
            m_rewardBar?.UpdateScore(m_score);

            DebugBase.Log(
                $"[{nameof(SimonSaysUI)}] Round complete! Score: {m_score}",
                DebugCategory.UI);

            SetAllButtonsInteractable(false);

            if (m_maxScore > 0 && m_score >= m_maxScore)
            {
                DebugBase.Log(
                    $"[{nameof(SimonSaysUI)}] Max score reached! Final score: {m_score}",
                    DebugCategory.UI);

                m_currentState = SimonState.GameOver;
                m_gameOverPanel?.Show(m_score, FriendshipReward);
                return;
            }

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

            m_gameOverPanel?.Show(m_score, FriendshipReward);
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

        public void SetRewardTiers(MinigameRewardTier[] rewardTiers, int completionReward)
        {
            m_rewardTiers = rewardTiers;
            m_completionReward = completionReward;
            RefreshMaxScore();

            if (rewardTiers != null)
            {
                m_rewardBar?.Initialize(rewardTiers, completionReward);
            }
        }

        public void SetDifficulty(MinigameDifficultySettings settings)
        {
            if (settings is SimonSaysDifficultySettings simonSettings)
            {
                m_sequenceStartDelay = simonSettings.SequenceStartDelay;
                m_gapBetweenHighlights = simonSettings.GapBetweenHighlights;
                m_nextRoundDelay = simonSettings.NextRoundDelay;
                m_difficultyMaxRounds = simonSettings.MaxRounds;
                RefreshMaxScore();
            }
            else if (settings != null)
            {
                DebugBase.LogWarning(
                    $"[{nameof(SimonSaysUI)}] Received unexpected difficulty settings type: {settings.GetType().Name}",
                    DebugCategory.UI);
            }
        }

        private void RefreshMaxScore()
        {
            m_maxScore = m_difficultyMaxRounds > 0
                ? m_difficultyMaxRounds
                : MinigameRewardTier.ComputeMaxScore(m_rewardTiers);
        }

        private void OnCloseClicked()
        {
            DebugBase.Log($"[{nameof(SimonSaysUI)}] Close clicked after game over", DebugCategory.UI);
            GameClosed?.Invoke();
        }
    }
}
