using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VRPortalToolkit.Utilities
{
    /// <summary>
    /// Provides utility methods for physics-related operations.
    /// </summary>
    public static class PhysicsUtilities
    {
        /// <summary>
        /// Gets all colliders attached to a GameObject.
        /// </summary>
        /// <param name="gameObject">The GameObject to get colliders from.</param>
        /// <param name="results">The list to store the found colliders.</param>
        public static void GetColliders(this GameObject gameObject, List<Collider> results)
            => GetColliders(gameObject, false, results);

        /// <summary>
        /// Gets all colliders attached to a GameObject with an option to include inactive colliders.
        /// </summary>
        /// <param name="gameObject">The GameObject to get colliders from.</param>
        /// <param name="includeInactive">Whether to include inactive colliders.</param>
        /// <param name="results">The list to store the found colliders.</param>
        public static void GetColliders(this GameObject gameObject, bool includeInactive, List<Collider> results)
        {
            if (results == null) return;

            Rigidbody rigidbody = gameObject.GetComponent<Rigidbody>();

            if (rigidbody)
                GetColliders(rigidbody, includeInactive, results);
            else
            {
                Collider collider = gameObject.GetComponent<Collider>();
                if (collider) results.Add(collider);
            }
        }

        /// <summary>
        /// Gets all colliders attached to a Rigidbody.
        /// </summary>
        /// <param name="rigidbody">The Rigidbody to get colliders from.</param>
        /// <param name="results">The list to store the found colliders.</param>
        public static void GetColliders(this Rigidbody rigidbody, List<Collider> results)
            => GetColliders(rigidbody, false, results);

        /// <summary>
        /// Gets all colliders attached to a Rigidbody with an option to include inactive colliders.
        /// </summary>
        /// <param name="rigidbody">The Rigidbody to get colliders from.</param>
        /// <param name="includeInactive">Whether to include inactive colliders.</param>
        /// <param name="results">The list to store the found colliders.</param>
        public static void GetColliders(this Rigidbody rigidbody, bool includeInactive, List<Collider> results)
        {
            if (results == null) return;

            Collider collider;
            int startCount = results.Count;

            rigidbody.GetComponentsInChildren(includeInactive, results);

            for (int i = startCount; i < results.Count;)
            {
                collider = results[i];

                if (results[i].attachedRigidbody != rigidbody)
                    results.RemoveAt(i);
                else
                    i++;
            }
        }

        /// <summary>
        /// Maintains a list of colliders for a GameObject, updating the reference to a Rigidbody if needed.
        /// </summary>
        /// <param name="gameObject">The GameObject to maintain colliders for.</param>
        /// <param name="includeInactive">Whether to include inactive colliders.</param>
        /// <param name="rigidbody">Reference to the Rigidbody that will be updated if needed.</param>
        /// <param name="colliders">The list to store and maintain the colliders.</param>
        public static void MaintainColliders(GameObject gameObject, bool includeInactive, ref Rigidbody rigidbody, List<Collider> colliders)
        {
            if (!rigidbody || rigidbody.gameObject != gameObject)
                rigidbody = gameObject.GetComponent<Rigidbody>();


            if (rigidbody)
            {
                colliders.Clear();
                GetColliders(rigidbody, includeInactive, colliders);
            }
            else
            {
                if (colliders.Count == 1 && colliders[0].gameObject == gameObject)
                    return;
                else
                {
                    colliders.Clear();
                    Collider collider = gameObject.GetComponent<Collider>();
                    if (collider) colliders.Add(collider);
                }
            }
        }
    }
}