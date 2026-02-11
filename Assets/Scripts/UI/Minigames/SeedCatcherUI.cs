using System;
using System.Collections.Generic;
using System.Threading;
using Birdie.Debug;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Birdie.UI.Minigames
{
    /// <summary>
    /// Main controller for the Seed Catcher minigame.
    /// Manages bag intro animation, seed spawning, collision detection, timer, scoring,
    /// and game over flow. Handles touch/drag input via pointer event interfaces.
    /// </summary>
    public sealed class SeedCatcherUI : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [Header("UI References")]
        [SerializeField]
        [Tooltip("The seed bag image shown during intro animation")]
        private Image m_bagImage;

        [SerializeField]
        [Tooltip("HUD text displaying current score")]
        private TextMeshProUGUI m_scoreText;

        [SerializeField]
        [Tooltip("HUD text displaying remaining time")]
        private TextMeshProUGUI m_timerText;

        [SerializeField]
        [Tooltip("Container transform where seeds are spawned")]
        private Transform m_seedContainer;

        [SerializeField]
        [Tooltip("The basket controller")]
        private SeedCatcherBasket m_basket;

        [SerializeField]
        [Tooltip("Inactive seed template to clone for each spawned seed")]
        private GameObject m_seedTemplate;

        [SerializeField]
        [Tooltip("Death zone that destroys seeds that fall past the basket")]
        private SeedCatcherDeathZone m_deathZone;

        [Header("Game Over")]
        [SerializeField]
        [Tooltip("Panel shown when the timer runs out")]
        private GameObject m_gameOverPanel;

        [SerializeField]
        [Tooltip("Text showing the final score on the game over panel")]
        private TextMeshProUGUI m_finalScoreText;

        [SerializeField]
        [Tooltip("Button to restart the game")]
        private Button m_playAgainButton;

        [Header("Game Settings")]
        [SerializeField]
        [Tooltip("Game duration in seconds")]
        private float m_gameDuration = 30f;

        [SerializeField]
        [Tooltip("Time between seed spawns in seconds")]
        private float m_seedSpawnInterval = 0.5f;

        [Header("Bag Intro Animation")]
        [SerializeField]
        [Tooltip("Duration for bag scale up")]
        private float m_bagScaleUpDuration = 0.3f;

        [SerializeField]
        [Tooltip("Peak scale during bag intro")]
        private float m_bagScaleUpAmount = 1.2f;

        [SerializeField]
        [Tooltip("Duration for bag shake rotation")]
        private float m_bagShakeDuration = 0.4f;

        [SerializeField]
        [Tooltip("Duration for bag burst animation (scale to 0 + fade)")]
        private float m_bagBurstDuration = 0.3f;

        private readonly List<SeedCatcherSeed> m_activeSeeds = new List<SeedCatcherSeed>();
        private RectTransform m_rectTransform;
        private RectTransform m_seedContainerRect;
        private int m_score;
        private float m_remainingTime;
        private SeedCatcherState m_currentState;
        private CancellationToken m_destroyCancellation;

        private enum SeedCatcherState
        {
            WaitingToStart,
            BagIntro,
            Playing,
            GameOver,
        }

        private void Awake()
        {
            m_destroyCancellation = this.GetCancellationTokenOnDestroy();
            m_rectTransform = GetComponent<RectTransform>();

            if (m_seedContainer != null)
            {
                m_seedContainerRect = m_seedContainer.GetComponent<RectTransform>();
            }

            if (m_playAgainButton != null)
            {
                m_playAgainButton.onClick.AddListener(OnPlayAgainClicked);
            }

            if (m_basket != null)
            {
                m_basket.SeedCaught += OnSeedCaught;
            }

            if (m_deathZone != null)
            {
                m_deathZone.SeedDestroyed += OnSeedMissed;
            }
        }

        private void Start()
        {
            StartGame();
        }

        private void Update()
        {
            if (m_currentState != SeedCatcherState.Playing)
            {
                return;
            }

            m_remainingTime -= Time.deltaTime;
            UpdateTimerDisplay();

            if (m_remainingTime <= 0f)
            {
                OnTimeUp();
            }
        }

        private void OnDestroy()
        {
            if (m_playAgainButton != null)
            {
                m_playAgainButton.onClick.RemoveListener(OnPlayAgainClicked);
            }

            if (m_basket != null)
            {
                m_basket.SeedCaught -= OnSeedCaught;
            }

            if (m_deathZone != null)
            {
                m_deathZone.SeedDestroyed -= OnSeedMissed;
            }

            if (m_bagImage != null)
            {
                m_bagImage.DOKill();
                m_bagImage.transform.DOKill();
            }

            CleanupAllSeeds();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            HandlePointerInput(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            HandlePointerInput(eventData);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (m_basket != null)
            {
                m_basket.ClearTarget();
            }
        }

        private void HandlePointerInput(PointerEventData eventData)
        {
            if (m_currentState != SeedCatcherState.Playing || m_basket == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                m_rectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint);

            m_basket.SetTargetX(localPoint.x);
        }

        private void StartGame()
        {
            m_score = 0;
            m_remainingTime = m_gameDuration;
            m_currentState = SeedCatcherState.WaitingToStart;

            UpdateScoreDisplay();
            UpdateTimerDisplay();
            HideGameOver();
            CleanupAllSeeds();

            if (m_basket != null)
            {
                m_basket.SetInputEnabled(false);
            }

            DebugBase.Log($"[{nameof(SeedCatcherUI)}] Game started", DebugCategory.UI);

            PlayBagIntroAsync().Forget();
        }

        private async UniTaskVoid PlayBagIntroAsync()
        {
            m_currentState = SeedCatcherState.BagIntro;

            if (m_bagImage == null)
            {
                StartPlaying();
                return;
            }

            m_bagImage.DOKill();
            m_bagImage.transform.DOKill();

            m_bagImage.gameObject.SetActive(true);
            m_bagImage.color = new Color(m_bagImage.color.r, m_bagImage.color.g, m_bagImage.color.b, 1f);
            m_bagImage.transform.localScale = Vector3.one;
            m_bagImage.transform.localRotation = Quaternion.identity;

            DebugBase.Log($"[{nameof(SeedCatcherUI)}] Bag intro started", DebugCategory.UI);

            // Scale up
            await m_bagImage.transform
                .DOScale(m_bagScaleUpAmount, m_bagScaleUpDuration)
                .SetEase(Ease.OutBack)
                .AsyncWaitForCompletion();

            // Shake rotation
            await m_bagImage.transform
                .DOLocalRotate(new Vector3(0f, 0f, 10f), m_bagShakeDuration / 4f)
                .SetLoops(4, LoopType.Yoyo)
                .SetEase(Ease.InOutSine)
                .AsyncWaitForCompletion();

            // Burst: scale to 0 + fade out simultaneously
            m_bagImage.DOFade(0f, m_bagBurstDuration);
            await m_bagImage.transform
                .DOScale(0f, m_bagBurstDuration)
                .SetEase(Ease.InBack)
                .AsyncWaitForCompletion();

            m_bagImage.gameObject.SetActive(false);

            DebugBase.Log($"[{nameof(SeedCatcherUI)}] Bag intro complete", DebugCategory.UI);

            StartPlaying();
        }

        private void StartPlaying()
        {
            m_currentState = SeedCatcherState.Playing;

            if (m_basket != null)
            {
                m_basket.SetInputEnabled(true);
            }

            DebugBase.Log($"[{nameof(SeedCatcherUI)}] Playing", DebugCategory.UI);

            SpawnSeedsAsync().Forget();
        }

        private async UniTaskVoid SpawnSeedsAsync()
        {
            while (m_currentState == SeedCatcherState.Playing)
            {
                SpawnSeed();

                await UniTask.Delay(
                    TimeSpan.FromSeconds(m_seedSpawnInterval),
                    cancellationToken: m_destroyCancellation);
            }
        }

        private void SpawnSeed()
        {
            if (m_seedTemplate == null || m_seedContainer == null || m_seedContainerRect == null)
            {
                return;
            }

            GameObject seedObj = Instantiate(m_seedTemplate, m_seedContainer);
            seedObj.SetActive(true);

            var seed = seedObj.GetComponent<SeedCatcherSeed>();
            if (seed == null)
            {
                Destroy(seedObj);
                return;
            }

            float halfWidth = m_seedContainerRect.rect.width / 2f;
            float topY = m_seedContainerRect.rect.height / 2f;
            float randomX = UnityEngine.Random.Range(-halfWidth + 30f, halfWidth - 30f);

            seed.RectTransform.anchoredPosition = new Vector2(randomX, topY);
            m_activeSeeds.Add(seed);
        }

        private void OnSeedCaught(SeedCatcherSeed seed)
        {
            if (m_currentState != SeedCatcherState.Playing)
            {
                return;
            }

            m_activeSeeds.Remove(seed);
            m_score++;
            UpdateScoreDisplay();
            Destroy(seed.gameObject);
        }

        private void OnSeedMissed(SeedCatcherSeed seed)
        {
            m_activeSeeds.Remove(seed);
        }

        private void CleanupAllSeeds()
        {
            foreach (SeedCatcherSeed seed in m_activeSeeds)
            {
                if (seed != null)
                {
                    Destroy(seed.gameObject);
                }
            }

            m_activeSeeds.Clear();
        }

        private void OnTimeUp()
        {
            m_remainingTime = 0f;
            m_currentState = SeedCatcherState.GameOver;

            if (m_basket != null)
            {
                m_basket.SetInputEnabled(false);
            }

            UpdateTimerDisplay();

            DebugBase.Log(
                $"[{nameof(SeedCatcherUI)}] Time's up! Final score: {m_score}",
                DebugCategory.UI);

            ShowGameOver();
        }

        private void ShowGameOver()
        {
            if (m_gameOverPanel != null)
            {
                m_gameOverPanel.transform.SetAsLastSibling();
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

        private void UpdateTimerDisplay()
        {
            if (m_timerText != null)
            {
                int seconds = Mathf.CeilToInt(Mathf.Max(m_remainingTime, 0f));
                m_timerText.text = $"Time: {seconds}";
            }
        }

        private void OnPlayAgainClicked()
        {
            DebugBase.Log($"[{nameof(SeedCatcherUI)}] Play again clicked", DebugCategory.UI);
            StartGame();
        }
    }
}
