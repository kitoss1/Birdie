using Birdie.Debug;
using UnityEngine;

namespace Birdie.Birds
{
    /// <summary>
    /// Sits on the child GameObject that owns the Animator and forwards
    /// Animation Events to the Bird component on the parent.
    /// Add this component to the same GameObject as the Animator.
    /// </summary>
    public class BirdAnimationEventReceiver : MonoBehaviour
    {
        private Bird m_bird;

        private void Awake()
        {
            m_bird = GetComponentInParent<Bird>();
            if (m_bird == null)
            {
                DebugBase.LogError($"[{nameof(BirdAnimationEventReceiver)}] No Bird component found in parent hierarchy", DebugCategory.Birds);
            }
        }

        /// <summary>
        /// Called by Animation Events on the singing animation.
        /// Plays a random clip from the bird's song list.
        /// </summary>
        public void PlayRandomSongPart()
        {
            m_bird?.PlayRandomSongPart();
        }
    }
}
