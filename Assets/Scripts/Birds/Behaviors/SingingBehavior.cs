using Birdie.Debug;
using UnityEngine;

namespace Birdie.Birds.Behaviors
{
    /// <summary>
    /// Singing behavior where the bird performs a song.
    /// Plays animation and audio clip of the bird's song.
    /// </summary>
    [CreateAssetMenu(fileName = "SingingBehavior", menuName = "Birdie/Bird Behaviors/Singing Behavior")]
    public class SingingBehavior : BirdBehaviorState
    {
        [Header("Singing Settings")]
        [SerializeField]
        [Tooltip("Volume multiplier for the bird song")]
        [Range(0f, 1f)]
        private float m_volumeMultiplier = 1f;

        private AudioSource m_audioSource;

        public override void OnEnter(Bird bird)
        {
            DebugBase.Log($"[{nameof(SingingBehavior)}] {bird.BirdData?.BirdName} started singing", DebugCategory.Birds);

            // TODO: Play singing animation on Spine skeleton
            // Example: bird.SpineSkeleton.AnimationState.SetAnimation(0, "singing", true);

            // Play bird song audio if available
            if (bird.BirdData != null && bird.BirdData.BirdSong != null)
            {
                m_audioSource = bird.GetComponent<AudioSource>();
                if (m_audioSource == null)
                {
                    m_audioSource = bird.gameObject.AddComponent<AudioSource>();
                }

                m_audioSource.clip = bird.BirdData.BirdSong;
                m_audioSource.volume = m_volumeMultiplier;
                m_audioSource.Play();
            }
        }

        public override void Execute(Bird bird)
        {
            // Singing behavior is mostly passive - animation and audio handle it
            // Could add visual effects like music notes here
        }

        public override void OnExit(Bird bird)
        {
            DebugBase.Log($"[{nameof(SingingBehavior)}] {bird.BirdData?.BirdName} stopped singing", DebugCategory.Birds);

            // Stop audio if still playing
            if (m_audioSource != null && m_audioSource.isPlaying)
            {
                m_audioSource.Stop();
            }
        }

        public override int CalculateWeight(Bird bird)
        {
            int weight = base.CalculateWeight(bird);

            // TODO: Increase weight if wind chimes are nearby
            // Example: if (FindWindChimesNearby(bird)) weight += 30;

            return weight;
        }
    }
}
