using System;
using System.Collections.Generic;
using System.Threading;
using Birdie.Data;
using Birdie.Debug;
using Cysharp.Threading.Tasks;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Birdie.UI.Minigames
{
    /// <summary>
    /// Main controller for the Seed Catcher minigame.
    /// Manages bag intro animation, seed spawning, collision detection, timer, scoring,
    /// and game over flow. Handles touch/drag input via pointer event interfaces.
    /// </summary>
    public sealed class SeedCatcherUI : MonoBehaviour, IMinigame, IPointerDownHandler, IDragHandler, IPointerUpHandler
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
        [Tooltip("Display component showing heart icons for remaining lives")]
        private SeedCatcherLivesDisplay m_livesDisplay;

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
        [Tooltip("Inactive spike template to clone for each spawned spike")]
        private GameObject m_spikeTemplate;

        [SerializeField]
        [Tooltip("Death zone that destroys seeds that fall past the basket")]
        private SeedCatcherDeathZone m_deathZone;

        [Header("Reward Bar")]
        [SerializeField]
        [Tooltip("Progress bar showing score thresholds and friendship rewards")]
        private MinigameRewardBar m_rewardBar;

        [Header("Game Over")]
        [SerializeField]
        [Tooltip("Panel shown when the timer runs out")]
        private GameObject m_gameOverPanel;

        [SerializeField]
        [Tooltip("Text showing the final score on the game over panel")]
        private TextMeshProUGUI m_finalScoreText;

        [SerializeField]
        [FormerlySerializedAs("m_playAgainButton")]
        [Tooltip("Button to close the minigame after game over")]
        private Button m_closeButton;

        [Header("Game Settings")]
        [SerializeField]
        [Tooltip("Game duration in seconds")]
        private float m_gameDuration = 30f;

        [Header("Difficulty Progression")]
        [SerializeField]
        [Tooltip("Spawn interval at the start of the game (easy)")]
        private float m_initialSpawnInterval = 1.0f;

        [SerializeField]
        [Tooltip("Spawn interval at the end of the game (hard)")]
        private float m_finalSpawnInterval = 0.3f;

        [SerializeField]
        [Tooltip("Seed fall speed at the start of the game (easy)")]
        private float m_initialFallSpeed = 200f;

        [SerializeField]
        [Tooltip("Seed fall speed at the end of the game (hard)")]
        private float m_finalFallSpeed = 500f;

        [SerializeField]
        [Tooltip("Chance to spawn a spike instead of a seed at the start (0-1)")]
        [Range(0f, 1f)]
        private float m_initialSpikeChance = 0.1f;

        [SerializeField]
        [Tooltip("Chance to spawn a spike instead of a seed at the end (0-1)")]
        [Range(0f, 1f)]
        private float m_finalSpikeChance = 0.4f;

        [Header("Lives")]
        [SerializeField]
        [Tooltip("Number of lives the player starts with")]
        private int m_initialLives = 3;

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
        private readonly List<SeedCatcherSpike> m_activeSpikes = new List<SeedCatcherSpike>();

        public event Action GameClosed;

        public int FriendshipReward => MinigameRewardTier.ResolveReward(m_rewardTiers, m_score);

        private MinigameRewardTier[] m_rewardTiers;
        private RectTransform m_rectTransform;
        private RectTransform m_seedContainerRect;
        private int m_score;
        private int m_currentLives;
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

            if (m_closeButton != null)
            {
                m_closeButton.onClick.AddListener(OnCloseClicked);
            }

            if (m_basket != null)
            {
                m_basket.SeedCaught += OnSeedCaught;
                m_basket.SpikeCaught += OnSpikeCaught;
            }

            if (m_deathZone != null)
            {
                m_deathZone.SeedDestroyed += OnSeedMissed;
                m_deathZone.SpikeDestroyed += OnSpikeMissed;
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
            if (m_closeButton != null)
            {
                m_closeButton.onClick.RemoveListener(OnCloseClicked);
            }

            if (m_basket != null)
            {
                m_basket.SeedCaught -= OnSeedCaught;
                m_basket.SpikeCaught -= OnSpikeCaught;
            }

            if (m_deathZone != null)
            {
                m_deathZone.SeedDestroyed -= OnSeedMissed;
                m_deathZone.SpikeDestroyed -= OnSpikeMissed;
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
            m_currentLives = m_initialLives;
            m_remainingTime = m_gameDuration;
            m_currentState = SeedCatcherState.WaitingToStart;

            UpdateScoreDisplay();
            UpdateTimerDisplay();
            m_livesDisplay?.Initialize(m_initialLives);
            m_rewardBar?.UpdateScore(0);
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

        private float GetDifficultyProgress()
        {
            float elapsed = m_gameDuration - m_remainingTime;
            return Mathf.Clamp01(elapsed / m_gameDuration);
        }

        private async UniTaskVoid SpawnSeedsAsync()
        {
            while (m_currentState == SeedCatcherState.Playing)
            {
                float progress = GetDifficultyProgress();
                float spikeChance = Mathf.Lerp(m_initialSpikeChance, m_finalSpikeChance, progress);

                if (m_spikeTemplate != null && UnityEngine.Random.value < spikeChance)
                {
                    SpawnSpike();
                }
                else
                {
                    SpawnSeed();
                }

                float interval = Mathf.Lerp(m_initialSpawnInterval, m_finalSpawnInterval, progress);

                await UniTask.Delay(
                    TimeSpan.FromSeconds(interval),
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

            float progress = GetDifficultyProgress();
            float fallSpeed = Mathf.Lerp(m_initialFallSpeed, m_finalFallSpeed, progress);
            seed.SetFallSpeed(fallSpeed);

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
            m_rewardBar?.UpdateScore(m_score);
            Destroy(seed.gameObject);
        }

        private void OnSeedMissed(SeedCatcherSeed seed)
        {
            m_activeSeeds.Remove(seed);
        }

        private void SpawnSpike()
        {
            if (m_spikeTemplate == null || m_seedContainer == null || m_seedContainerRect == null)
            {
                return;
            }

            GameObject spikeObj = Instantiate(m_spikeTemplate, m_seedContainer);
            spikeObj.SetActive(true);

            var spike = spikeObj.GetComponent<SeedCatcherSpike>();
            if (spike == null)
            {
                Destroy(spikeObj);
                return;
            }

            float halfWidth = m_seedContainerRect.rect.width / 2f;
            float topY = m_seedContainerRect.rect.height / 2f;
            float randomX = UnityEngine.Random.Range(-halfWidth + 30f, halfWidth - 30f);

            float progress = GetDifficultyProgress();
            float fallSpeed = Mathf.Lerp(m_initialFallSpeed, m_finalFallSpeed, progress);
            spike.SetFallSpeed(fallSpeed);

            spike.RectTransform.anchoredPosition = new Vector2(randomX, topY);
            m_activeSpikes.Add(spike);
        }

        private void OnSpikeCaught(SeedCatcherSpike spike)
        {
            if (m_currentState != SeedCatcherState.Playing)
            {
                return;
            }

            m_activeSpikes.Remove(spike);
            Destroy(spike.gameObject);

            m_currentLives--;
            m_livesDisplay?.LoseLife();

            DebugBase.Log(
                $"[{nameof(SeedCatcherUI)}] Spike caught! Lives remaining: {m_currentLives}",
                DebugCategory.UI);

            if (m_currentLives <= 0)
            {
                OnLivesLost();
            }
        }

        private void OnSpikeMissed(SeedCatcherSpike spike)
        {
            m_activeSpikes.Remove(spike);
        }

        private void OnLivesLost()
        {
            m_currentState = SeedCatcherState.GameOver;

            if (m_basket != null)
            {
                m_basket.SetInputEnabled(false);
            }

            DebugBase.Log(
                $"[{nameof(SeedCatcherUI)}] No lives left! Final score: {m_score}",
                DebugCategory.UI);

            ShowGameOver();
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

            foreach (SeedCatcherSpike spike in m_activeSpikes)
            {
                if (spike != null)
                {
                    Destroy(spike.gameObject);
                }
            }

            m_activeSpikes.Clear();
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

        public void SetRewardTiers(MinigameRewardTier[] rewardTiers)
        {
            m_rewardTiers = rewardTiers;

            if (m_rewardBar != null)
            {
                m_rewardBar.Initialize(rewardTiers);
            }
        }

        public void SetDifficulty(MinigameDifficultySettings settings)
        {
            if (settings is SeedCatcherDifficultySettings seedCatcherSettings)
            {
                m_gameDuration = seedCatcherSettings.GameDuration;
                m_initialSpawnInterval = seedCatcherSettings.InitialSpawnInterval;
                m_finalSpawnInterval = seedCatcherSettings.FinalSpawnInterval;
                m_initialFallSpeed = seedCatcherSettings.InitialFallSpeed;
                m_finalFallSpeed = seedCatcherSettings.FinalFallSpeed;
                m_initialSpikeChance = seedCatcherSettings.InitialSpikeChance;
                m_finalSpikeChance = seedCatcherSettings.FinalSpikeChance;
                m_initialLives = seedCatcherSettings.InitialLives;
            }
            else if (settings != null)
            {
                DebugBase.LogWarning(
                    $"[{nameof(SeedCatcherUI)}] Received unexpected difficulty settings type: {settings.GetType().Name}",
                    DebugCategory.UI);
            }
        }

        private void OnCloseClicked()
        {
            DebugBase.Log($"[{nameof(SeedCatcherUI)}] Close clicked after game over", DebugCategory.UI);
            GameClosed?.Invoke();
        }
    }
}
