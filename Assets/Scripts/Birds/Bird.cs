using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Birdie.Data;
using Birdie.Debug;
using Birdie.Managers;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Birdie.Birds
{
    /// <summary>
    /// Controls individual bird behavior including state management,
    /// interactions, and lifetime during a visit.
    /// Now uses a behavior system with ScriptableObject states.
    /// </summary>
    public class Bird : MonoBehaviour
    {
        public static event Action<Bird> BirdClicked;
        public static event Action<Bird> BirdLeaving;

        [Header("Bird Configuration")]
        [SerializeField]
        private BirdData m_birdData;

        private float m_maxVisitDuration;
        private BirdState m_currentState = BirdState.Appearing;
        private BirdBehaviorState m_currentBehavior;
        private float m_behaviorTimer = 0f;
        private float m_behaviorDuration = 0f;
        private float m_visitEndTime;
        private bool m_isInteractable = false;
        private bool m_hasBeenClickedThisVisit = false;
        private BirdState m_stateBeforePause;
        private float m_remainingVisitTime;

        // Cached environment data
        private List<BirdObject> m_nearbyObjects = new List<BirdObject>();

        public BirdData BirdData
        {
            get => m_birdData;
            set => m_birdData = value;
        }

        public BirdState CurrentState => m_currentState;

        public bool IsInteractable => m_isInteractable;

        public IReadOnlyList<BirdObject> NearbyObjects => m_nearbyObjects;

        private void Start()
        {
            if (m_birdData == null)
            {
                DebugBase.LogError($"[{nameof(Bird)}] BirdData is not assigned!", DebugCategory.Birds);
                Destroy(gameObject);
                return;
            }

            if (m_birdData.PossibleBehaviors == null || m_birdData.PossibleBehaviors.Count == 0)
            {
                DebugBase.LogError($"[{nameof(Bird)}] No behaviors assigned! Bird will not function properly.", DebugCategory.Birds);
            }

            StartVisitAsync().Forget();
        }

        private void Update()
        {
            // Execute current behavior
            if (m_currentBehavior != null && m_currentState == BirdState.Idle)
            {
                m_currentBehavior.Execute(this);

                // Update behavior timer
                m_behaviorTimer += Time.deltaTime;
                if (m_behaviorTimer >= m_behaviorDuration)
                {
                    // Behavior finished, pick next one
                    TransitionToNextBehavior();
                }
            }
        }

        /// <summary>
        /// Initializes the bird with specific BirdData.
        /// Called by BirdManager when spawning.
        /// </summary>
        public void Initialize(BirdData birdData)
        {
            m_birdData = birdData;
            DebugBase.Log($"[{nameof(Bird)}] Initialized: {birdData.BirdName}", DebugCategory.Birds);
        }

        /// <summary>
        /// Starts the bird's visit lifecycle.
        /// </summary>
        private async UniTaskVoid StartVisitAsync()
        {
            await AppearAsync();
            ScanEnvironment();
            await VisitAsync();
            await LeaveAsync();
        }

        /// <summary>
        /// Handles the appearing animation and state.
        /// </summary>
        private async UniTask AppearAsync()
        {
            m_currentState = BirdState.Appearing;
            DebugBase.Log($"[{nameof(Bird)}] {m_birdData.BirdName} is appearing", DebugCategory.Birds);

            // TODO: Play appear animation
            await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: this.GetCancellationTokenOnDestroy());

            m_isInteractable = true;
        }

        /// <summary>
        /// Scans the environment for interactive objects using EnvironmentManager.
        /// Called when the bird spawns to detect feeders, baths, etc.
        /// </summary>
        private void ScanEnvironment()
        {
            m_nearbyObjects.Clear();

            if (GameManager.Instance?.EnvironmentManager == null)
            {
                DebugBase.LogWarning($"[{nameof(Bird)}] EnvironmentManager not found!", DebugCategory.Birds);
                CalculateVisitDuration();
                return;
            }

            // Get all objects from EnvironmentManager
            IReadOnlyList<BirdObject> allObjects = GameManager.Instance.EnvironmentManager.GetAllObjects();

            foreach (BirdObject obj in allObjects)
            {
                if (obj.CanBeUsedBy(this))
                {
                    m_nearbyObjects.Add(obj);
                    DebugBase.Log($"[{nameof(Bird)}] Found nearby object: {obj.ObjectType} (attractiveness: {obj.Attractiveness})", DebugCategory.Birds);
                }
            }

            // Calculate visit duration based on environment richness
            CalculateVisitDuration();
        }

        /// <summary>
        /// Calculates how long the bird will stay based on environment and friendship.
        /// </summary>
        private void CalculateVisitDuration()
        {
            float baseDuration = UnityEngine.Random.Range(m_birdData.VisitDurationMin, m_birdData.VisitDurationMax);

            // Increase duration if there are interesting objects
            float objectBonus = m_nearbyObjects.Count * m_birdData.ObjectBonusSeconds;
            baseDuration += objectBonus;

            // TODO: Add friendship level bonus
            // int friendshipBonus = GetFriendshipLevel() * 3;
            // baseDuration += friendshipBonus;

            m_maxVisitDuration = Mathf.Min(baseDuration, m_birdData.VisitDurationMax);
            DebugBase.Log($"[{nameof(Bird)}] Visit duration calculated: {m_maxVisitDuration}s", DebugCategory.Birds);
        }

        /// <summary>
        /// Main visit loop where the bird performs behaviors.
        /// </summary>
        private async UniTask VisitAsync()
        {
            m_currentState = BirdState.Idle;
            m_visitEndTime = Time.time + m_maxVisitDuration;

            DebugBase.Log($"[{nameof(Bird)}] {m_birdData.BirdName} starting visit behaviors", DebugCategory.Birds);

            // Start first behavior
            TransitionToNextBehavior();

            // Wait until visit time is over
            await UniTask.WaitUntil(() => Time.time >= m_visitEndTime, cancellationToken: this.GetCancellationTokenOnDestroy());

            // Exit current behavior before leaving
            if (m_currentBehavior != null)
            {
                m_currentBehavior.OnExit(this);
                m_currentBehavior = null;
            }
        }

        /// <summary>
        /// Transitions from current behavior to next behavior.
        /// </summary>
        private void TransitionToNextBehavior()
        {
            // Exit current behavior
            if (m_currentBehavior != null)
            {
                m_currentBehavior.OnExit(this);
            }

            // Pick next behavior
            BirdBehaviorState nextBehavior = PickNextBehavior();

            if (nextBehavior != null)
            {
                m_currentBehavior = nextBehavior;
                m_behaviorTimer = 0f;
                m_behaviorDuration = UnityEngine.Random.Range(nextBehavior.MinDuration, nextBehavior.MaxDuration);

                DebugBase.Log($"[{nameof(Bird)}] Starting behavior: {nextBehavior.name} for {m_behaviorDuration}s", DebugCategory.Birds);

                m_currentBehavior.OnEnter(this);
            }
            else
            {
                DebugBase.LogWarning($"[{nameof(Bird)}] No valid behavior found!", DebugCategory.Birds);
            }
        }

        /// <summary>
        /// Picks the next behavior using weighted random selection.
        /// </summary>
        private BirdBehaviorState PickNextBehavior()
        {
            // Filter behaviors that can be executed
            List<BirdBehaviorState> availableBehaviors = m_birdData.PossibleBehaviors
                .Where(b => b != null && b.CanExecute(this))
                .ToList();

            if (availableBehaviors.Count == 0)
            {
                DebugBase.LogWarning($"[{nameof(Bird)}] No available behaviors!", DebugCategory.Birds);
                return null;
            }

            // Calculate weights for each behavior
            List<int> weights = new List<int>();
            int totalWeight = 0;

            foreach (BirdBehaviorState behavior in availableBehaviors)
            {
                int weight = behavior.CalculateWeight(this);
                weights.Add(weight);
                totalWeight += weight;
            }

            // Weighted random selection
            int randomValue = UnityEngine.Random.Range(0, totalWeight);
            int cumulativeWeight = 0;

            for (int i = 0; i < availableBehaviors.Count; i++)
            {
                cumulativeWeight += weights[i];
                if (randomValue < cumulativeWeight)
                {
                    return availableBehaviors[i];
                }
            }

            // Fallback to first behavior
            return availableBehaviors[0];
        }

        /// <summary>
        /// Handles the leaving animation and cleanup.
        /// </summary>
        private async UniTask LeaveAsync()
        {
            m_currentState = BirdState.Leaving;
            m_isInteractable = false;
            BirdLeaving?.Invoke(this);

            DebugBase.Log($"[{nameof(Bird)}] {m_birdData.BirdName} is leaving", DebugCategory.Birds);

            // TODO: Play leave animation
            await UniTask.Delay(TimeSpan.FromSeconds(1f), cancellationToken: this.GetCancellationTokenOnDestroy());

            Destroy(gameObject);
        }

        /// <summary>
        /// Called when the bird is clicked/tapped.
        /// </summary>
        public void OnBirdClicked()
        {
            if (!m_isInteractable)
            {
                return;
            }

            DebugBase.Log($"[{nameof(Bird)}] {m_birdData.BirdName} was clicked", DebugCategory.Birds);

            if (!m_hasBeenClickedThisVisit)
            {
                m_hasBeenClickedThisVisit = true;

                if (GameManager.Instance != null && GameManager.Instance.DiaryManager != null)
                {
                    GameManager.Instance.DiaryManager.RecordBirdEncounter(m_birdData);
                }
                else
                {
                    DebugBase.LogWarning($"[{nameof(Bird)}] GameManager or DiaryManager is null, cannot record encounter", DebugCategory.Birds);
                }
            }

            BirdClicked?.Invoke(this);
        }

        /// <summary>
        /// Detects mouse clicks on the bird (for testing).
        /// </summary>
        private void OnMouseDown()
        {
            OnBirdClicked();
        }

        /// <summary>
        /// Called when the bird is fed.
        /// </summary>
        public void OnBirdFed(DietType foodType)
        {
            if (!m_isInteractable)
            {
                return;
            }

            bool isPreferredFood = foodType == m_birdData.DietType;
            DebugBase.Log($"[{nameof(Bird)}] {m_birdData.BirdName} was fed {foodType}. Preferred: {isPreferredFood}", DebugCategory.Birds);

            if (isPreferredFood)
            {
                // TODO: Play happy animation
                // TODO: Increase friendship points
                // TODO: Extend visit duration
            }
            else
            {
                // TODO: Play neutral animation
                // TODO: Small friendship increase
            }
        }

        /// <summary>
        /// Forces the bird to leave immediately.
        /// </summary>
        public void ForceDeparture()
        {
            if (m_currentState == BirdState.Leaving)
            {
                return;
            }

            DebugBase.Log($"[{nameof(Bird)}] {m_birdData.BirdName} forced to leave", DebugCategory.Birds);
            m_visitEndTime = Time.time;
        }

        /// <summary>
        /// Pauses the bird during a minigame.
        /// Freezes visit timer, stops behavior execution, and disables interaction.
        /// </summary>
        public void Pause()
        {
            if (m_currentState == BirdState.Paused || m_currentState == BirdState.Leaving)
            {
                return;
            }

            m_stateBeforePause = m_currentState;
            m_remainingVisitTime = m_visitEndTime - Time.time;
            m_currentState = BirdState.Paused;
            m_isInteractable = false;

            DebugBase.Log($"[{nameof(Bird)}] {m_birdData.BirdName} paused", DebugCategory.Birds);
        }

        /// <summary>
        /// Resumes the bird after a minigame ends.
        /// Restores previous state and recalculates visit end time.
        /// </summary>
        public void Resume()
        {
            if (m_currentState != BirdState.Paused)
            {
                return;
            }

            m_currentState = m_stateBeforePause;
            m_visitEndTime = Time.time + m_remainingVisitTime;
            m_isInteractable = m_currentState != BirdState.Appearing && m_currentState != BirdState.Leaving;

            DebugBase.Log($"[{nameof(Bird)}] {m_birdData.BirdName} resumed", DebugCategory.Birds);
        }

        /// <summary>
        /// Called when a new object is added to the scene while bird is visiting.
        /// </summary>
        public void OnObjectAddedToScene(BirdObject newObject)
        {
            if (!newObject.CanBeUsedBy(this))
            {
                return;
            }

            DebugBase.Log($"[{nameof(Bird)}] New object detected: {newObject.ObjectType}", DebugCategory.Birds);

            m_nearbyObjects.Add(newObject);

            // Check if we should interrupt current behavior for this new object
            // For example, if a feeder is placed and bird isn't eating
            if (m_currentBehavior != null && m_currentBehavior.CanBeInterrupted)
            {
                // TODO: Add logic to check if new object is more attractive
                // and potentially interrupt current behavior
            }
        }

        private void OnDestroy()
        {
            BirdLeaving?.Invoke(this);
            DebugBase.Log($"[{nameof(Bird)}] {m_birdData?.BirdName ?? "Unknown"} destroyed", DebugCategory.Birds);
        }

        public void PlaySong()
        {
            if (m_birdData.BirdSong == null)
            {
                DebugBase.LogWarning($"[{nameof(Bird)}] {m_birdData.BirdName} has no song assigned", DebugCategory.Birds);
                return;
            }

            if (GameManager.Instance?.SoundManager == null)
            {
                DebugBase.LogWarning($"[{nameof(Bird)}] SoundManager not available, cannot play song", DebugCategory.Birds);
                return;
            }

            GameManager.Instance.SoundManager.PlaySFX(m_birdData.BirdSong);
            DebugBase.Log($"[{nameof(Bird)}] Playing song for {m_birdData.BirdName}", DebugCategory.Birds);
        }
    }
}
