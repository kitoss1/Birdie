using Birdie.Data;
using Birdie.Debug;
using Birdie.Managers;
using Cysharp.Threading.Tasks;
using System;
using UnityEngine;

namespace Birdie.Birds
{
    /// <summary>
    /// Controls individual bird behavior including state management,
    /// interactions, and lifetime during a visit.
    /// </summary>
    public class Bird : MonoBehaviour
    {
        [Header("Bird Configuration")]
        [SerializeField]
        private BirdData m_birdData;

        private BirdState m_currentState = BirdState.Appearing;
        private float m_visitEndTime;
        private bool m_isInteractable = false;
        private bool m_hasBeenClickedThisVisit = false;

        public BirdData BirdData
        {
            get => m_birdData;
            set => m_birdData = value;
        }

        public BirdState CurrentState => m_currentState;

        public bool IsInteractable => m_isInteractable;

        private void Start()
        {
            if (m_birdData == null)
            {
                DebugBase.LogError($"[{nameof(Bird)}] BirdData is not assigned!", DebugCategory.Birds);
                Destroy(gameObject);
                return;
            }

            StartVisitAsync().Forget();
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
            await IdleAsync();
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
            await UniTask.Delay(TimeSpan.FromSeconds(1f));

            m_isInteractable = true;
        }

        /// <summary>
        /// Handles the idle state where bird stays and can be interacted with.
        /// </summary>
        private async UniTask IdleAsync()
        {
            m_currentState = BirdState.Idle;

            float visitDuration = UnityEngine.Random.Range(m_birdData.VisitDurationMin, m_birdData.VisitDurationMax);
            m_visitEndTime = Time.time + visitDuration;

            DebugBase.Log($"[{nameof(Bird)}] {m_birdData.BirdName} is idle for {visitDuration:F1}s", DebugCategory.Birds);

            // Wait until visit time is over
            while (Time.time < m_visitEndTime)
            {
                await UniTask.Yield();
            }
        }

        /// <summary>
        /// Handles the leaving animation and cleanup.
        /// </summary>
        private async UniTask LeaveAsync()
        {
            m_currentState = BirdState.Leaving;
            m_isInteractable = false;

            DebugBase.Log($"[{nameof(Bird)}] {m_birdData.BirdName} is leaving", DebugCategory.Birds);

            // TODO: Play leave animation
            await UniTask.Delay(TimeSpan.FromSeconds(1f));

            Destroy(gameObject);
        }

        /// <summary>
        /// Called when the bird is clicked/tapped.
        /// </summary>
        public void OnBirdClicked()
        {
            if (!m_isInteractable || m_hasBeenClickedThisVisit)
            {
                return;
            }

            if (GameManager.Instance == null || GameManager.Instance.DiaryManager == null)
            {
                DebugBase.LogWarning($"[{nameof(Bird)}] GameManager or DiaryManager is null, cannot record encounter", DebugCategory.Birds);
                return;
            }

            m_hasBeenClickedThisVisit = true;

            DebugBase.Log($"[{nameof(Bird)}] {m_birdData.BirdName} was clicked", DebugCategory.Birds);

            GameManager.Instance.DiaryManager.RecordBirdEncounter(m_birdData);
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

        private void OnDestroy()
        {
            DebugBase.Log($"[{nameof(Bird)}] {m_birdData?.BirdName ?? "Unknown"} destroyed", DebugCategory.Birds);
        }
    }
}
