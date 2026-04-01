using Birdie.Debug;
using UnityEngine;

namespace Birdie.Birds.Behaviors
{
    /// <summary>
    /// Singing behavior where the bird performs a song.
    /// Stops any in-progress audio when the behavior exits.
    /// </summary>
    [CreateAssetMenu(fileName = "SingingBehavior", menuName = "Birdie/Bird Behaviors/Singing Behavior")]
    public class SingingBehavior : BirdBehaviorState
    {
        public override void OnEnter(Bird bird)
        {
            base.OnEnter(bird);
            DebugBase.Log($"[{nameof(SingingBehavior)}] {bird.BirdData?.BirdName} started singing", DebugCategory.Birds);

            // Ensure an AudioSource is present so Animation Events can play song parts immediately.
            if (bird.GetComponent<AudioSource>() == null)
            {
                bird.gameObject.AddComponent<AudioSource>();
            }
        }

        public override void Execute(Bird bird)
        {
            // Audio is driven by Animation Events calling Bird.PlaySongPart(index).
        }

        public override void OnExit(Bird bird)
        {
            DebugBase.Log($"[{nameof(SingingBehavior)}] {bird.BirdData?.BirdName} stopped singing", DebugCategory.Birds);

            AudioSource audioSource = bird.GetComponent<AudioSource>();
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
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
