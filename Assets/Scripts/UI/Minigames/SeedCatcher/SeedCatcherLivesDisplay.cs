using System.Collections.Generic;
using Birdie.Debug;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.UI.Minigames
{
    /// <summary>
    /// Displays lives as heart icons that animate out when lost.
    /// Uses a template-based approach to support variable life counts.
    /// </summary>
    public sealed class SeedCatcherLivesDisplay : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("Inactive heart Image template to clone for each life")]
        private Image m_heartTemplate;

        [SerializeField]
        [Tooltip("Parent transform where heart icons are spawned")]
        private Transform m_heartContainer;

        [SerializeField]
        [Tooltip("Duration of the heart loss animation")]
        private float m_heartLossDuration = 0.25f;

        private readonly List<Image> m_spawnedHearts = new List<Image>();

        /// <summary>
        /// Clears existing hearts and spawns the specified number of new ones.
        /// </summary>
        public void Initialize(int lifeCount)
        {
            ClearHearts();
            SpawnHearts(lifeCount);

            DebugBase.Log(
                $"[{nameof(SeedCatcherLivesDisplay)}] Initialized with {lifeCount} lives",
                DebugCategory.UI);
        }

        /// <summary>
        /// Removes the last visible heart with a shrink and fade animation.
        /// </summary>
        public void LoseLife()
        {
            if (m_spawnedHearts.Count == 0)
            {
                return;
            }

            int lastIndex = m_spawnedHearts.Count - 1;
            Image heart = m_spawnedHearts[lastIndex];
            m_spawnedHearts.RemoveAt(lastIndex);

            if (heart == null)
            {
                return;
            }

            AnimateHeartLoss(heart);
        }

        private void OnDestroy()
        {
            ClearHearts();
        }

        private void SpawnHearts(int count)
        {
            if (m_heartTemplate == null || m_heartContainer == null)
            {
                return;
            }

            for (int i = 0; i < count; i++)
            {
                Image heart = Instantiate(m_heartTemplate, m_heartContainer);
                heart.gameObject.SetActive(true);
                m_spawnedHearts.Add(heart);
            }
        }

        private void ClearHearts()
        {
            foreach (Image heart in m_spawnedHearts)
            {
                if (heart != null)
                {
                    heart.DOKill();
                    heart.transform.DOKill();
                    Destroy(heart.gameObject);
                }
            }

            m_spawnedHearts.Clear();
        }

        private void AnimateHeartLoss(Image heart)
        {
            heart.DOKill();

            heart.DOFade(0f, m_heartLossDuration)
                .SetEase(Ease.InQuad);
        }
    }
}
