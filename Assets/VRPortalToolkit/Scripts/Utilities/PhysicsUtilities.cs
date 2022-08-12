using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace VRPortalToolkit.Utilities
{
    public static class PhysicsUtilities
    {
        public static void GetColliders(this GameObject gameObject, List<Collider> results)
            => GetColliders(gameObject, false, results);

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

        public static void GetColliders(this Rigidbody rigidbody, List<Collider> results)
            => GetColliders(rigidbody, false, results);

        public static void GetColliders(this Rigidbody rigidbody, bool includeInactive, List<Collider> results)
        {
            if (results == null) return;

            Collider collider;
            int startCount = results.Count;

            rigidbody.GetComponentsInChildren<Collider>(includeInactive, results);

            for (int i = startCount; i < results.Count;)
            {
                collider = results[i];

                if (results[i].attachedRigidbody != rigidbody)
                    results.RemoveAt(i);
                else
                    i++;
            }
        }

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