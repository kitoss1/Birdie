using Birdie.Debug;
using UnityEngine;

namespace Birdie.Birds.Behaviors
{
    /// <summary>
    /// Shared base for behaviors that fly the bird between two world positions (arriving and leaving).
    /// Handles position setup, the lerp + sway flight loop, and tilt correction for mirrored sprites.
    /// Subclasses define their own phase transitions and completion conditions.
    /// </summary>
    public abstract class FlyingBehaviorBase : BirdBehaviorState
    {
        [Header("Flight Settings")]
        [SerializeField]
        [Tooltip("Speed at which the bird travels between the two positions (units per second)")]
        private float m_flySpeed = 200f;

        [SerializeField]
        [Tooltip("How far the bird sways left and right during flight (canvas units). Set to 0 to fly straight.")]
        private float m_swayAmplitude = 80f;

        [SerializeField]
        [Tooltip("Number of full S-curves during flight. 1 = one smooth S, 2 = two waves.")]
        private float m_swayFrequency = 1f;

        [SerializeField]
        [Tooltip("Maximum tilt angle in degrees applied to the visual root based on movement direction.")]
        private float m_maxTiltAngle = 25f;

        // Local-space positions used when the bird is inside a RectTransform hierarchy.
        private Vector2 m_startLocalPosition;
        private Vector2 m_endLocalPosition;

        // World-space positions used for non-UI birds.
        private Vector3 m_startWorldPosition;
        private Vector3 m_endWorldPosition;

        private bool m_isRectTransform;

        /// <summary>Current normalised flight progress [0, 1].</summary>
        protected float m_progress;

        /// <summary>
        /// Resolves start and end positions into either local or world space, detects the
        /// RectTransform path, and sets the bird's initial facing direction.
        /// Call this from OnEnter before starting the flight.
        /// </summary>
        protected void SetupFlight(Bird bird, Vector3 startWorld, Vector3 endWorld)
        {
            m_progress = 0f;

            RectTransform birdRect = bird.transform as RectTransform;
            m_isRectTransform = birdRect != null;

            if (m_isRectTransform)
            {
                RectTransform parentRect = birdRect.parent as RectTransform;
                if (parentRect != null)
                {
                    m_startLocalPosition = parentRect.InverseTransformPoint(startWorld);
                    m_endLocalPosition = parentRect.InverseTransformPoint(endWorld);
                }
                else
                {
                    m_startLocalPosition = startWorld;
                    m_endLocalPosition = endWorld;
                }
            }
            else
            {
                m_startWorldPosition = startWorld;
                m_endWorldPosition = endWorld;
            }

            float directionX = m_isRectTransform
                ? m_endLocalPosition.x - m_startLocalPosition.x
                : m_endWorldPosition.x - m_startWorldPosition.x;
            bird.SetFacingDirection(directionX);
        }

        /// <summary>
        /// Advances the flight by one frame: updates progress, moves the bird with sway, and applies tilt.
        /// Progress is advanced using the path derivative so the bird travels at a constant screen-space
        /// speed regardless of how much sway is applied at that point.
        /// </summary>
        /// <returns>True when progress has reached 1 (destination reached).</returns>
        protected bool StepFlight(Bird bird)
        {
            m_progress += (m_flySpeed * Time.deltaTime) / PathDerivativeMagnitude(m_progress);
            m_progress = Mathf.Clamp01(m_progress);

            // Sway fades out toward the destination so the bird arrives/departs flying straight.
            float swayFade = 1f - m_progress;
            float xSway = Mathf.Sin(m_progress * Mathf.PI * 2f * m_swayFrequency) * m_swayAmplitude * swayFade;

            if (m_isRectTransform)
            {
                RectTransform birdRect = bird.transform as RectTransform;
                if (birdRect == null)
                {
                    return m_progress >= 1f;
                }

                Vector2 basePos = Vector2.Lerp(m_startLocalPosition, m_endLocalPosition, m_progress);
                Vector2 prevPos = birdRect.localPosition;
                birdRect.localPosition = new Vector2(basePos.x + xSway, basePos.y);

                ApplyTilt(bird, (Vector2)birdRect.localPosition - prevPos);
            }
            else
            {
                Vector3 basePos = Vector3.Lerp(m_startWorldPosition, m_endWorldPosition, m_progress);
                Vector3 prevPos = bird.transform.position;
                bird.transform.position = new Vector3(basePos.x + xSway, basePos.y, basePos.z);

                ApplyTilt(bird, bird.transform.position - prevPos);
            }

            return m_progress >= 1f;
        }

        /// <summary>
        /// Returns the magnitude of the flight path's derivative at progress t.
        /// The path is P(t) = lerp(start, end, t) + (sin(t·2π·f)·A·(1-t), 0), so its derivative is:
        /// P'(t) = (end - start) + (cos(t·2π·f)·2π·f·A·(1-t) − sin(t·2π·f)·A, 0)
        /// Dividing flySpeed by this magnitude instead of straight-line distance keeps
        /// the bird's screen-space velocity constant through the sway.
        /// </summary>
        private float PathDerivativeMagnitude(float t)
        {
            float angle = t * Mathf.PI * 2f * m_swayFrequency;
            float swayDerivativeX = Mathf.Cos(angle) * Mathf.PI * 2f * m_swayFrequency * m_swayAmplitude * (1f - t)
                                  - Mathf.Sin(angle) * m_swayAmplitude;

            Vector2 baseDelta = m_isRectTransform
                ? m_endLocalPosition - m_startLocalPosition
                : (Vector2)(m_endWorldPosition - m_startWorldPosition);

            return Mathf.Max(new Vector2(baseDelta.x + swayDerivativeX, baseDelta.y).magnitude, 0.001f);
        }

        /// <summary>
        /// Updates the bird's facing direction and tilts its visual root to match the flight angle.
        /// Compensates for the Z-rotation reversal that occurs when the sprite is horizontally mirrored.
        /// </summary>
        private void ApplyTilt(Bird bird, Vector2 velocity)
        {
            if (velocity.sqrMagnitude < 0.0001f)
            {
                return;
            }

            bird.SetFacingDirection(velocity.x);

            float tilt = Mathf.Clamp(
                Mathf.Atan2(velocity.y, Mathf.Abs(velocity.x)) * Mathf.Rad2Deg,
                -m_maxTiltAngle,
                m_maxTiltAngle);

            // When flying left the sprite is mirrored (scale.x positive) so Z-rotation
            // direction is visually reversed; negate the tilt to compensate.
            if (velocity.x < 0f)
            {
                tilt = -tilt;
            }

            bird.TiltVisual(tilt);
        }
    }
}
