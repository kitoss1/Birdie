using System;
using Birdie.Managers;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Birdie.Birds
{
    /// <summary>
    /// Base class for all interactive objects that birds can interact with.
    /// Examples: bird feeders, bird baths, toys, decorations, etc.
    /// Automatically registers with EnvironmentManager on Enable.
    /// </summary>
    public abstract class BirdObject : MonoBehaviour, IPointerClickHandler
    {
        [Header("Object Settings")]
        [SerializeField]
        [Tooltip("Unique identifier for this object type")]
        private string m_objectID;

        [SerializeField]
        [Tooltip("Type of interaction this object provides")]
        private BirdObjectType m_objectType;

        [SerializeField]
        [Tooltip("How attractive this object is to birds (affects behavior weight)")]
        [Range(1, 100)]
        private int m_attractiveness = 50;

        [SerializeField]
        [Tooltip("Whether this object can be clicked by the player to open its context menu")]
        private bool m_isClickable = false;

        [Header("Position Settings")]
        [SerializeField]
        [Tooltip("Position where bird should move to interact with this object")]
        private Transform m_interactionPoint;

        private int m_interactingBirdCount;
        private bool m_isBeingMoved;

        public static event Action<BirdObject> ObjectClicked;

        // Properties
        public string ObjectID => m_objectID;
        public BirdObjectType ObjectType => m_objectType;
        public int Attractiveness => m_attractiveness;
        public Vector3 InteractionPosition => m_interactionPoint != null ? m_interactionPoint.position : transform.position;
        public RectTransform InteractionRectTransform => m_interactionPoint != null
            ? m_interactionPoint as RectTransform
            : transform as RectTransform;
        public bool IsBeingUsed => m_interactingBirdCount > 0;
        public bool IsBeingMoved => m_isBeingMoved;

        public void SetBeingMoved(bool isBeingMoved)
        {
            m_isBeingMoved = isBeingMoved;
        }

        /// <summary>
        /// Called when a bird starts interacting with this object.
        /// </summary>
        public virtual void OnBirdStartInteraction(Bird bird)
        {
            m_interactingBirdCount++;
        }

        /// <summary>
        /// Called when a bird stops interacting with this object.
        /// </summary>
        public virtual void OnBirdEndInteraction(Bird bird)
        {
            m_interactingBirdCount--;

            if (m_interactingBirdCount < 0)
            {
                m_interactingBirdCount = 0;
            }
        }

        /// <summary>
        /// Checks if this object can be used by the bird.
        /// </summary>
        public virtual bool CanBeUsedBy(Bird bird)
        {
            return !m_isBeingMoved;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (m_isClickable)
            {
                ObjectClicked?.Invoke(this);
            }
        }

        protected virtual void OnEnable()
        {
            // Register with EnvironmentManager
            if (GameManager.Instance?.EnvironmentManager != null)
            {
                GameManager.Instance.EnvironmentManager.RegisterObject(this);
            }
        }

        protected virtual void OnDisable()
        {
            // Unregister from EnvironmentManager
            if (GameManager.Instance?.EnvironmentManager != null)
            {
                GameManager.Instance.EnvironmentManager.UnregisterObject(this);
            }
        }

        private void OnDrawGizmos()
        {
            // Draw interaction point in editor
            if (m_interactionPoint != null)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawWireSphere(m_interactionPoint.position, 0.2f);
            }
        }
    }

    /// <summary>
    /// Types of bird objects that can be placed in the scene.
    /// </summary>
    public enum BirdObjectType
    {
        Feeder,
        BirdBath,
        Decoration
    }
}
