using System;
using UnityEngine;

namespace Birdie.UI.Minigames
{
    /// <summary>
    /// Trigger zone at the bottom of the Seed Catcher play area.
    /// Destroys seeds that fall past the basket.
    /// </summary>
    public sealed class SeedCatcherDeathZone : MonoBehaviour
    {
        /// <summary>
        /// Fired when a seed enters the death zone, just before it is destroyed.
        /// </summary>
        public event Action<SeedCatcherSeed> SeedDestroyed;

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.TryGetComponent<SeedCatcherSeed>(out SeedCatcherSeed seed))
            {
                SeedDestroyed?.Invoke(seed);
                Destroy(seed.gameObject);
            }
        }
    }
}
