using Birdie.Debug;
using UnityEngine;

namespace Birdie.Birds.Behaviors
{
    /// <summary>
    /// Singing behavior where the bird performs a song.
    /// Waits for the current animation loop to complete before allowing a state transition,
    /// so audio driven by Animation Events is never cut mid-phrase.
    /// Stops any in-progress audio when the behavior exits.
    /// </summary>
    [CreateAssetMenu(fileName = "SingingBehavior", menuName = "Birdie/Bird Behaviors/Singing Behavior")]
    public class SingingBehavior : BirdBehaviorState
    {
        private float m_lastNormalizedTime;
        private bool m_loopCompleted;

        public override void OnEnter(Bird bird)
        {
            base.OnEnter(bird);
            m_lastNormalizedTime = 0f;
            m_loopCompleted = false;

            DebugBase.Log($"[{nameof(SingingBehavior)}] {bird.BirdData?.BirdName} started singing", DebugCategory.Birds);

            // Ensure an AudioSource is present so Animation Events can play song parts immediately.
            if (bird.GetComponent<AudioSource>() == null)
            {
                bird.gameObject.AddComponent<AudioSource>();
            }
        }

        public override void Execute(Bird bird)
        {
            if (m_loopCompleted || bird.BehaviorTimer < bird.BehaviorDuration)
            {
                return;
            }

            // Duration has expired — wait for the current loop cycle to finish.
            float normalizedTime = bird.GetCurrentAnimationNormalizedTime() % 1f;
            if (normalizedTime < m_lastNormalizedTime)
            {
                m_loopCompleted = true;
                DebugBase.Log($"[{nameof(SingingBehavior)}] {bird.BirdData?.BirdName} finished singing loop", DebugCategory.Birds);
            }

            m_lastNormalizedTime = normalizedTime;
        }

        public override bool IsBehaviorComplete(Bird bird)
        {
            return m_loopCompleted;
        }

        public override void OnExit(Bird bird)
        {
            DebugBase.Log($"[{nameof(SingingBehavior)}] {bird.BirdData?.BirdName} stopped singing", DebugCategory.Birds);

            AudioSource audioSource = bird.GetComponent<AudioSource>();
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            m_lastNormalizedTime = 0f;
            m_loopCompleted = false;
        }

        public override int CalculateWeight(Bird bird, int baseWeight)
        {
            int weight = base.CalculateWeight(bird, baseWeight);

            // TODO: Increase weight if wind chimes are nearby
            // Example: if (FindWindChimesNearby(bird)) weight += 30;

            return weight;
        }
    }
}
