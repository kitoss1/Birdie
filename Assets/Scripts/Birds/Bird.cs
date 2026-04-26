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
    public class Bird : MonoBehaviour, IClickable
    {
        public static event Action<Bird> BirdClicked;
        public static event Action<Bird> BirdLanded;
        public static event Action<Bird> BirdLeaving;

        [Header("Bird Configuration")]
        [SerializeField] private BirdData m_birdData;
        [SerializeField] private Animator m_animator;
        [SerializeField]
        [Tooltip("The child transform that holds the sprite/rig. Its localScale.x is flipped to change facing direction.")]
        private Transform m_visualRoot;
        [SerializeField] private BirdWalkJumper m_walkJumper;


        private float m_maxVisitDuration;
        private Vector3 m_landingWorldPosition;
        private Vector3 m_spawnWorldPosition;
        private BirdState m_currentState = BirdState.Appearing;
        private BirdBehaviorState m_currentBehavior;
        private float m_behaviorTimer = 0f;
        private float m_behaviorDuration = 0f;
        private float m_visitEndTime;
        private bool m_isInteractable = false;
        private bool m_hasBeenClickedThisVisit = false;
        private bool m_hasPlayedMinigameThisVisit = false;
        private float m_minigameCooldownEndTime;
        private BirdState m_stateBeforePause;
        private float m_remainingVisitTime;

        // Cached environment data
        private List<BirdObject> m_nearbyObjects = new List<BirdObject>();

        // Cooldown tracking: maps each behavior to the Time.time when it becomes available again
        private Dictionary<BirdBehaviorState, float> m_behaviorCooldowns = new Dictionary<BirdBehaviorState, float>();

        public BirdData BirdData
        {
            get => m_birdData;
            set => m_birdData = value;
        }

        public BirdState CurrentState => m_currentState;

        public bool IsInteractable => m_isInteractable;

        public bool CanPlayMinigame => !m_hasPlayedMinigameThisVisit;

        public float MinigameCooldownRemaining => m_hasPlayedMinigameThisVisit
            ? Mathf.Max(0f, m_minigameCooldownEndTime - Time.time)
            : 0f;

        public bool AllowClickWhileActive => m_currentBehavior == null || m_currentBehavior.CanBeInterrupted;

        public Vector3 LandingWorldPosition => m_landingWorldPosition;

        public Vector3 SpawnWorldPosition => m_spawnWorldPosition;

        public float BehaviorTimer => m_behaviorTimer;

        public float BehaviorDuration => m_behaviorDuration;

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
                DebugBase.LogError($"[{nameof(Bird)}] No behavior entries assigned! Bird will not function properly.", DebugCategory.Birds);
            }

            StartVisitAsync().Forget();
        }

        private void Update()
        {
            if (m_hasPlayedMinigameThisVisit && m_birdData.MinigameCooldownDuration > 0f && Time.time >= m_minigameCooldownEndTime)
            {
                AllowMinigameReplay();
            }

            // Execute current behavior
            if (m_currentBehavior != null && (m_currentState == BirdState.Visiting || m_currentState == BirdState.Leaving))
            {
                m_currentBehavior.Execute(this);

                // Timer and transition are only managed during Idle.
                // During Leaving, LeaveAsync owns the lifecycle — Execute runs but nothing transitions.
                if (m_currentState == BirdState.Visiting)
                {
                    // Only tick the timer once the behavior signals it is ready (e.g. after arriving at a target).
                    if (m_currentBehavior.IsTimerActive(this))
                    {
                        m_behaviorTimer += Time.deltaTime;
                    }

                    if (m_currentBehavior.IsBehaviorComplete(this))
                    {
                        TransitionToNextBehavior();
                    }
                }
            }
        }

        /// <summary>
        /// Initializes the bird with specific BirdData.
        /// Called by BirdManager when spawning.
        /// </summary>
        public void Initialize(BirdData birdData, Vector3 landingWorldPosition = default, Vector3 spawnWorldPosition = default)
        {
            m_birdData = birdData;
            m_landingWorldPosition = landingWorldPosition;
            m_spawnWorldPosition = spawnWorldPosition;
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
        /// Handles the appearing state. The visual fly-in is driven by ArrivingBehavior in VisitAsync.
        /// </summary>
        private UniTask AppearAsync()
        {
            m_currentState = BirdState.Appearing;
            m_isInteractable = true;
            DebugBase.Log($"[{nameof(Bird)}] {m_birdData.BirdName} is appearing", DebugCategory.Birds);
            return UniTask.CompletedTask;
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
            m_currentState = BirdState.Visiting;
            m_visitEndTime = Time.time + m_maxVisitDuration;

            BirdLanded?.Invoke(this);
            DebugBase.Log($"[{nameof(Bird)}] {m_birdData.BirdName} starting visit behaviors", DebugCategory.Birds);

            // Start with the arriving behavior if configured, otherwise pick normally.
            TransitionToNextBehavior(m_birdData.ArrivingBehavior);

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
        /// Pass an override to force a specific behavior instead of using random selection.
        /// </summary>
        private void TransitionToNextBehavior(BirdBehaviorState overrideBehavior = null)
        {
            // Exit current behavior
            BirdBehaviorState forcedNext = null;
            if (m_currentBehavior != null)
            {
                forcedNext = m_currentBehavior.ForcedNextBehavior;

                m_currentBehavior.OnExit(this);

                if (m_currentBehavior.CooldownDuration > 0f)
                {
                    m_behaviorCooldowns[m_currentBehavior] = Time.time + m_currentBehavior.CooldownDuration;
                    DebugBase.Log($"[{nameof(Bird)}] {m_currentBehavior.name} on cooldown for {m_currentBehavior.CooldownDuration}s", DebugCategory.Birds);
                }
            }

            // Priority: caller override → ForcedNextBehavior → random pick
            BirdBehaviorState nextBehavior = overrideBehavior ?? forcedNext ?? PickNextBehavior();

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
        /// Returns true if the given behavior is currently waiting out its cooldown.
        /// </summary>
        private bool IsBehaviorOnCooldown(BirdBehaviorState behavior)
        {
            return m_behaviorCooldowns.TryGetValue(behavior, out float availableAt) && Time.time < availableAt;
        }

        /// <summary>
        /// Picks the next behavior using weighted random selection.
        /// Weights are defined per species in BirdData, with environmental modifiers applied by the behavior.
        /// </summary>
        private BirdBehaviorState PickNextBehavior()
        {
            // Filter entries whose behavior can execute and is not on cooldown
            List<Data.BirdBehaviorEntry> availableEntries = m_birdData.PossibleBehaviors
                .Where(e => e?.Behavior != null && e.Behavior.CanExecute(this) && !IsBehaviorOnCooldown(e.Behavior))
                .ToList();

            if (availableEntries.Count == 0)
            {
                DebugBase.LogWarning($"[{nameof(Bird)}] No available behaviors!", DebugCategory.Birds);
                return null;
            }

            // Calculate final weights (base from BirdData + environmental modifiers from behavior)
            List<int> weights = new List<int>();
            int totalWeight = 0;

            foreach (Data.BirdBehaviorEntry entry in availableEntries)
            {
                int weight = entry.Behavior.CalculateWeight(this, entry.Weight);
                weights.Add(weight);
                totalWeight += weight;
            }

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"[{nameof(Bird)}] {m_birdData?.BirdName} picking behavior (total weight: {totalWeight}):");
            for (int i = 0; i < availableEntries.Count; i++)
            {
                sb.AppendLine($"  {availableEntries[i].Behavior.name} — weight {weights[i]}");
            }
            DebugBase.Log(sb.ToString(), DebugCategory.Birds);

            // Weighted random selection
            int randomValue = UnityEngine.Random.Range(0, totalWeight);
            int cumulativeWeight = 0;

            for (int i = 0; i < availableEntries.Count; i++)
            {
                cumulativeWeight += weights[i];
                if (randomValue < cumulativeWeight)
                {
                    DebugBase.Log($"[{nameof(Bird)}] Selected: {availableEntries[i].Behavior.name} (roll: {randomValue})", DebugCategory.Birds);
                    return availableEntries[i].Behavior;
                }
            }

            // Fallback to first available behavior
            return availableEntries[0].Behavior;
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

            if (m_birdData.LeavingBehavior != null)
            {
                m_currentBehavior = m_birdData.LeavingBehavior;
                m_behaviorTimer = 0f;
                m_behaviorDuration = 0f;
                m_currentBehavior.OnEnter(this);

                await UniTask.WaitUntil(
                    () => m_currentBehavior.IsBehaviorComplete(this),
                    cancellationToken: this.GetCancellationTokenOnDestroy());

                m_currentBehavior.OnExit(this);
                m_currentBehavior = null;
            }

            Destroy(gameObject);
        }

        /// <inheritdoc/>
        public void OnClicked() => OnBirdClicked();

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

            if (m_animator != null)
            {
                m_animator.speed = 0f;
            }

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

            if (m_animator != null)
            {
                m_animator.speed = 1f;
            }

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

        /// <summary>
        /// Advances the walk hop state and applies the resulting Y offset to the visual root.
        /// Call every frame while the bird is moving.
        /// </summary>
        public void SampleAndApplyWalkHop(float deltaTime)
        {
            m_walkJumper?.SampleAndApply(deltaTime, m_birdData);
        }

        /// <summary>
        /// Resets the walk hop state and restores the visual root to its ground Y.
        /// Call when the bird stops moving.
        /// </summary>
        public void ResetWalkHop()
        {
            m_walkJumper?.Reset(m_birdData);
        }

        /// <summary>
        /// Tilts the visual root by the given angle on the Z axis.
        /// Call with 0 to reset to upright.
        /// </summary>
        public void TiltVisual(float zAngle)
        {
            Transform target = m_visualRoot != null ? m_visualRoot : transform;
            Vector3 euler = target.localEulerAngles;
            euler.z = zAngle;
            target.localEulerAngles = euler;
        }

        /// <summary>
        /// Flips the bird to face left (negative X) or right (positive X).
        /// Uses localScale.x so the entire bone rig mirrors correctly.
        /// </summary>
        public void SetFacingDirection(float directionX)
        {
            if (directionX == 0f)
            {
                return;
            }

            Transform target = m_visualRoot != null ? m_visualRoot : transform;
            Vector3 scale = target.localScale;
            float absX = Mathf.Abs(scale.x);
            scale.x = directionX > 0f ? -absX : absX;
            target.localScale = scale;
        }

        /// <summary>
        /// Returns the normalizedTime of the currently playing Animator state on layer 0.
        /// For looping clips this keeps incrementing past 1.0 — use % 1f to get the in-cycle fraction.
        /// </summary>
        public float GetCurrentAnimationNormalizedTime()
        {
            if (m_animator == null)
            {
                return 0f;
            }

            return m_animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
        }

        /// <summary>
        /// Crossfades to the given Animator state.
        /// </summary>
        public void PlayAnimation(string stateName, float crossFadeDuration = 0.1f)
        {
            if (m_animator == null)
            {
                DebugBase.LogWarning($"[{nameof(Bird)}] Animator not assigned on {m_birdData?.BirdName}", DebugCategory.Birds);
                return;
            }

            if (string.IsNullOrEmpty(stateName))
            {
                return;
            }

            int stateHash = Animator.StringToHash(stateName);
            if (!m_animator.HasState(0, stateHash))
            {
                DebugBase.LogWarning($"[{nameof(Bird)}] Animation state '{stateName}' not found in Animator on {m_birdData?.BirdName}", DebugCategory.Birds);
                return;
            }

            m_animator.CrossFade(stateName, crossFadeDuration);
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

        /// <summary>
        /// Picks a random song part and plays it through this bird's AudioSource.
        /// Called by Animation Events on the singing animation.
        /// The AudioSource is guaranteed to exist while SingingBehavior is active.
        /// </summary>
        public void PlayRandomSongPart()
        {
            if (m_birdData?.SongParts == null || m_birdData.SongParts.Count == 0)
            {
                DebugBase.LogWarning($"[{nameof(Bird)}] {m_birdData?.BirdName} has no song parts assigned", DebugCategory.Birds);
                return;
            }

            AudioSource audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                DebugBase.LogWarning($"[{nameof(Bird)}] No AudioSource found on {m_birdData?.BirdName} — ensure SingingBehavior is active", DebugCategory.Birds);
                return;
            }

            AudioClip clip = m_birdData.SongParts[UnityEngine.Random.Range(0, m_birdData.SongParts.Count)];
            if (clip == null)
            {
                DebugBase.LogWarning($"[{nameof(Bird)}] {m_birdData.BirdName} has a song entry with no clip assigned", DebugCategory.Birds);
                return;
            }

            audioSource.clip = clip;
            audioSource.volume = GameManager.Instance?.SoundManager != null
                ? GameManager.Instance.SoundManager.GetEffectiveSfxVolume(1f)
                : 1f;
            audioSource.Play();

            DebugBase.Log($"[{nameof(Bird)}] {m_birdData.BirdName} playing random song clip '{clip.name}'", DebugCategory.Birds);
        }

        /// <summary>
        /// Marks that this bird has played a minigame during this visit.
        /// </summary>
        public void MarkMinigamePlayed()
        {
            m_hasPlayedMinigameThisVisit = true;
            m_minigameCooldownEndTime = Time.time + m_birdData.MinigameCooldownDuration;
            DebugBase.Log($"[{nameof(Bird)}] {m_birdData.BirdName} marked as having played a minigame this visit", DebugCategory.Birds);
        }

        /// <summary>
        /// Re-enables minigame play for this visit.
        /// Used by future features that grant additional plays.
        /// </summary>
        public void AllowMinigameReplay()
        {
            m_hasPlayedMinigameThisVisit = false;
            DebugBase.Log($"[{nameof(Bird)}] {m_birdData.BirdName} minigame replay allowed", DebugCategory.Birds);
        }
    }
}
