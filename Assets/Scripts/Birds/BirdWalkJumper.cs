using Birdie.Data;
using Birdie.Debug;
using UnityEngine;

namespace Birdie.Birds
{
    /// <summary>
    /// Applies occasional small hops to the visual root while the bird is walking.
    /// Driven synchronously by BirdBehaviorState via Bird.SampleAndApplyWalkHop / ResetWalkHop.
    /// Hop parameters are configured per species in BirdData.
    /// </summary>
    public sealed class BirdWalkJumper : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("The child transform that holds the sprite/rig (same as Bird's Visual Root).")]
        private Transform m_visualRoot;

        private float m_groundY;
        private float m_hopPhaseTimer = -1f;
        private float m_hopIntervalTimer = 0.5f;

        private void Awake()
        {
            if (m_visualRoot != null)
            {
                m_groundY = m_visualRoot.localPosition.y;
            }
            else
            {
                DebugBase.LogWarning($"[{nameof(BirdWalkJumper)}] Visual root is not assigned.", DebugCategory.Birds);
            }
        }

        private void OnDisable()
        {
            m_hopPhaseTimer = -1f;
            RestoreGroundY();
        }

        /// <summary>
        /// Advances the hop state and immediately applies the resulting Y offset to the visual root.
        /// Call this every frame while the bird is walking.
        /// </summary>
        public void SampleAndApply(float deltaTime, BirdData birdData)
        {
            if (m_visualRoot == null || birdData == null)
            {
                return;
            }

            float yOffset = AdvanceAndComputeOffset(deltaTime, birdData);
            ApplyHopOffset(yOffset);
        }

        /// <summary>
        /// Resets hop state and restores the visual root to its ground position.
        /// Call this when the bird stops walking.
        /// </summary>
        public void Reset(BirdData birdData)
        {
            m_hopPhaseTimer = -1f;
            m_hopIntervalTimer = birdData != null
                ? Random.Range(birdData.WalkHopIntervalMin, birdData.WalkHopIntervalMax)
                : 1f;
            RestoreGroundY();
        }

        private float AdvanceAndComputeOffset(float deltaTime, BirdData birdData)
        {
            if (m_hopPhaseTimer >= 0f)
            {
                return AdvanceHopPhase(deltaTime, birdData);
            }

            m_hopIntervalTimer -= deltaTime;

            if (m_hopIntervalTimer <= 0f)
            {
                m_hopPhaseTimer = 0f;
            }

            return 0f;
        }

        private float AdvanceHopPhase(float deltaTime, BirdData birdData)
        {
            m_hopPhaseTimer += deltaTime;

            if (m_hopPhaseTimer >= birdData.WalkHopDuration)
            {
                m_hopPhaseTimer = -1f;
                m_hopIntervalTimer = Random.Range(birdData.WalkHopIntervalMin, birdData.WalkHopIntervalMax);
                return 0f;
            }

            float t = m_hopPhaseTimer / birdData.WalkHopDuration;
            return birdData.WalkHopHeight * Mathf.Sin(t * Mathf.PI);
        }

        private void ApplyHopOffset(float yOffset)
        {
            Vector3 pos = m_visualRoot.localPosition;
            pos.y = m_groundY + yOffset;
            m_visualRoot.localPosition = pos;
        }

        private void RestoreGroundY()
        {
            if (m_visualRoot == null)
            {
                return;
            }

            Vector3 pos = m_visualRoot.localPosition;
            pos.y = m_groundY;
            m_visualRoot.localPosition = pos;
        }
    }
}
