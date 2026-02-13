using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Birdie.UI.Minigames
{
    /// <summary>
    /// Individual draggable seed for the Seed Sorting minigame.
    /// Handles drag input and notifies the controller when dropped on a valid zone.
    /// </summary>
    public sealed class SeedSortingSeed : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [SerializeField]
        [Tooltip("Image component used to display the seed sprite")]
        private Image m_image;

        [SerializeField]
        [Tooltip("Scale multiplier when the seed is picked up")]
        private float m_pickupScale = 1.2f;

        [SerializeField]
        [Tooltip("Duration of the pickup scale animation")]
        private float m_pickupScaleDuration = 0.15f;

        [SerializeField]
        [Tooltip("Duration of the snap-back animation when dropped outside a zone")]
        private float m_snapBackDuration = 0.25f;

        private RectTransform m_rectTransform;
        private Canvas m_parentCanvas;
        private Vector2 m_originalPosition;
        private bool m_isDragging;
        private bool m_inputEnabled;

        public event Action<SeedSortingSeed, Vector2> Dropped;

        public int SeedTypeIndex { get; private set; }

        public bool IsLiked { get; private set; }

        public RectTransform RectTransform
        {
            get
            {
                if (m_rectTransform == null)
                {
                    m_rectTransform = GetComponent<RectTransform>();
                }

                return m_rectTransform;
            }
        }

        public void Initialize(int seedTypeIndex, bool isLiked, Sprite sprite)
        {
            SeedTypeIndex = seedTypeIndex;
            IsLiked = isLiked;

            if (m_image != null)
            {
                m_image.sprite = sprite;
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            m_inputEnabled = enabled;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (!m_inputEnabled)
            {
                return;
            }

            m_isDragging = true;
            m_originalPosition = RectTransform.anchoredPosition;
            transform.SetAsLastSibling();

            RectTransform.DOKill();
            RectTransform.DOScale(m_pickupScale, m_pickupScaleDuration).SetEase(Ease.OutBack);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!m_isDragging)
            {
                return;
            }

            if (m_parentCanvas == null)
            {
                return;
            }

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                RectTransform.parent as RectTransform,
                eventData.position,
                eventData.pressEventCamera,
                out Vector2 localPoint);

            RectTransform.anchoredPosition = localPoint;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!m_isDragging)
            {
                return;
            }

            m_isDragging = false;

            RectTransform.DOKill();
            RectTransform.DOScale(1f, m_pickupScaleDuration).SetEase(Ease.OutBack);

            Dropped?.Invoke(this, eventData.position);
        }

        public void SnapBack()
        {
            RectTransform.DOKill();
            RectTransform.DOAnchorPos(m_originalPosition, m_snapBackDuration).SetEase(Ease.OutBack);
        }

        public void AnimateRemoval(Action onComplete)
        {
            RectTransform.DOKill();

            Sequence sequence = DOTween.Sequence();
            sequence.Append(RectTransform.DOScale(0f, 0.2f).SetEase(Ease.InBack));

            if (m_image != null)
            {
                sequence.Join(m_image.DOFade(0f, 0.2f));
            }

            sequence.OnComplete(() => onComplete?.Invoke());
        }

        private void Awake()
        {
            m_rectTransform = GetComponent<RectTransform>();
            m_parentCanvas = GetComponentInParent<Canvas>();
        }

        private void OnDestroy()
        {
            RectTransform.DOKill();

            if (m_image != null)
            {
                m_image.DOKill();
            }
        }
    }
}
