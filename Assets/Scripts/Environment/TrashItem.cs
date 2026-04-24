using System;
using Birdie.Birds;
using Birdie.Debug;
using Birdie.Managers;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Birdie.Environment
{
    /// <summary>
    /// A clickable piece of trash on the windowsill.
    /// Plays a pop animation and fades out when clicked, then notifies the manager.
    /// </summary>
    public class TrashItem : MonoBehaviour, IClickable
    {
        [SerializeField] private AudioClip m_clickSound;

        private int m_prefabIndex;
        private Image m_image;
        private bool m_isBeingRemoved;

        public int PrefabIndex => m_prefabIndex;

        public event Action<TrashItem> OnRemoved;

        private void Awake()
        {
            m_image = GetComponent<Image>();
        }

        public void Initialize(int prefabIndex)
        {
            m_prefabIndex = prefabIndex;
        }

        public void OnClicked()
        {
            if (m_isBeingRemoved)
            {
                return;
            }

            if (!GameManager.Instance.IsOnMainView())
            {
                return;
            }

            Remove();
        }

        private void Remove()
        {
            m_isBeingRemoved = true;

            GameManager gameManager = GameManager.Instance;
            if (m_clickSound != null && gameManager != null)
            {
                gameManager.SoundManager.PlaySFX(m_clickSound);
            }

            Sequence seq = DOTween.Sequence();
            seq.SetLink(gameObject);
            seq.Append(transform.DOPunchScale(Vector3.one * 0.4f, 0.15f, 1));
            seq.Append(m_image.DOFade(0f, 0.2f));
            seq.OnComplete(() =>
            {
                OnRemoved?.Invoke(this);
                Destroy(gameObject);
            });

            DebugBase.Log($"[{nameof(TrashItem)}] Trash clicked, removing");
        }

        private void OnDestroy()
        {
            DOTween.Kill(transform);

            if (m_image != null)
            {
                DOTween.Kill(m_image);
            }
        }
    }
}
