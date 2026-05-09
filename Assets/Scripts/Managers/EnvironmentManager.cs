using Birdie.Debug;
using Birdie.Save;
using System.Collections.Generic;
using UnityEngine;

namespace Birdie.Managers
{
    /// <summary>
    /// Manages all interactive bird objects in the scene.
    /// Provides centralized access to feeders, baths, toys, etc.
    /// Objects automatically register/unregister themselves.
    /// Accessed via GameManager.Instance.EnvironmentManager.
    /// </summary>
    public class EnvironmentManager : BaseManager
    {
        [Header("Movement Bounds")]
        [SerializeField]
        [Tooltip("Left boundary transform used to constrain bird and object movement")]
        private Transform m_leftBound;

        [SerializeField]
        [Tooltip("Right boundary transform used to constrain bird and object movement")]
        private Transform m_rightBound;

        private readonly List<Birds.BirdObject> m_allObjects = new List<Birds.BirdObject>();
        private readonly HashSet<Birds.BirdObject> m_registeredObjects = new HashSet<Birds.BirdObject>();

        public override void Initialize(SaveManager saveManager = null)
        {
            base.Initialize(saveManager);
            DebugBase.Log($"[{nameof(EnvironmentManager)}] Initialized", DebugCategory.Managers);
        }

        /// <summary>
        /// Registers a bird object with the manager.
        /// Called automatically by BirdObject on OnEnable.
        /// </summary>
        public void RegisterObject(Birds.BirdObject obj)
        {
            if (obj == null)
            {
                return;
            }

            if (m_registeredObjects.Add(obj))
            {
                m_allObjects.Add(obj);
                DebugBase.Log($"[{nameof(EnvironmentManager)}] Registered {obj.ObjectType}: {obj.ObjectID} at {obj.transform.position}", DebugCategory.Managers);
            }
        }

        /// <summary>
        /// Unregisters a bird object from the manager.
        /// Called automatically by BirdObject on OnDisable.
        /// </summary>
        public void UnregisterObject(Birds.BirdObject obj)
        {
            if (obj == null)
            {
                return;
            }

            if (m_registeredObjects.Remove(obj))
            {
                m_allObjects.Remove(obj);
                DebugBase.Log($"[{nameof(EnvironmentManager)}] Unregistered {obj.ObjectType}: {obj.ObjectID}", DebugCategory.Managers);
            }
        }

        /// <summary>
        /// Gets all objects of a specific type.
        /// </summary>
        public List<Birds.BirdObject> GetObjectsOfType(Birds.BirdObjectType type)
        {
            List<Birds.BirdObject> result = new List<Birds.BirdObject>();

            foreach (Birds.BirdObject obj in m_allObjects)
            {
                if (obj != null && obj.ObjectType == type)
                {
                    result.Add(obj);
                }
            }

            return result;
        }

        /// <summary>
        /// Gets all registered objects in the scene.
        /// </summary>
        public IReadOnlyList<Birds.BirdObject> GetAllObjects()
        {
            return m_allObjects;
        }

        /// <summary>
        /// Checks if at least one object of the specified type exists.
        /// </summary>
        public bool HasObjectOfType(Birds.BirdObjectType type)
        {
            foreach (Birds.BirdObject obj in m_allObjects)
            {
                if (obj != null && obj.ObjectType == type)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Gets the count of objects of a specific type.
        /// </summary>
        public int GetObjectCount(Birds.BirdObjectType type)
        {
            int count = 0;

            foreach (Birds.BirdObject obj in m_allObjects)
            {
                if (obj != null && obj.ObjectType == type)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// Finds the nearest object of a specific type to a given position.
        /// </summary>
        public Birds.BirdObject GetNearestObject(Birds.BirdObjectType type, Vector3 position)
        {
            Birds.BirdObject nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (Birds.BirdObject obj in m_allObjects)
            {
                // Only consider objects of the specified type
                if (obj == null || obj.ObjectType != type)
                {
                    continue;
                }

                float distance = Vector3.Distance(position, obj.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = obj;
                }
            }

            return nearest;
        }

        /// <summary>
        /// Returns the world-space X movement bounds defined by the left and right boundary transforms.
        /// Returns false if either boundary is not assigned.
        /// </summary>
        public bool TryGetMovementBoundsWorldX(out float minX, out float maxX)
        {
            if (m_leftBound != null && m_rightBound != null)
            {
                minX = Mathf.Min(m_leftBound.position.x, m_rightBound.position.x);
                maxX = Mathf.Max(m_leftBound.position.x, m_rightBound.position.x);
                return true;
            }

            minX = 0f;
            maxX = 0f;
            return false;
        }

        /// <summary>
        /// Finds the nearest object of a specific type that the bird can use.
        /// </summary>
        public Birds.BirdObject GetNearestUsableObject(Birds.BirdObjectType type, Birds.Bird bird)
        {
            Birds.BirdObject nearest = null;
            float nearestDistance = float.MaxValue;

            foreach (Birds.BirdObject obj in m_allObjects)
            {
                // Only consider objects of the specified type
                if (obj == null || obj.ObjectType != type)
                {
                    continue;
                }

                // Check if bird can use this object
                if (!obj.CanBeUsedBy(bird))
                {
                    continue;
                }

                float distance = Vector3.Distance(bird.transform.position, obj.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = obj;
                }
            }

            return nearest;
        }
    }
}
