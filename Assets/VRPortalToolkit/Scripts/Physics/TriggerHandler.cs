using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit
{
    public abstract class TriggerHandler : MonoBehaviour
    {
        private Dictionary<Collider, int> _activeCollisions = new Dictionary<Collider, int>();

        protected int colliderCount => _activeCollisions.Count;

        protected int CollisionCount(Collider collider)
        {
            if (isActiveAndEnabled && _activeCollisions.TryGetValue(collider, out int count))
                return count;

            return 0;
        }

        protected IEnumerable<Collider> GetColliders()
        {
            if (isActiveAndEnabled)
            {
                foreach (Collider collider in _activeCollisions.Keys)
                    yield return collider;
            }
        }

        protected virtual void OnEnable()
        {
            foreach (Collider collider in _activeCollisions.Keys)
                OnTriggerFirstEnter(collider);
        }

        protected virtual void OnDisable()
        {
            foreach (Collider collider in _activeCollisions.Keys)
                OnTriggerLastExit(collider);
        }

        protected virtual bool Contains(Collider collider)
        {
            return (isActiveAndEnabled && _activeCollisions.ContainsKey(collider));
        }

        protected virtual void OnTriggerFirstEnter(Collider collider) { }

        protected virtual void OnTriggerEnter(Collider collider)
        {
            if (_activeCollisions.TryGetValue(collider, out int count))
                _activeCollisions[collider] = count + 1;
            else
            {
                _activeCollisions[collider] = 1;

                if (isActiveAndEnabled)
                    OnTriggerFirstEnter(collider);
            }
        }

        protected virtual void OnTriggerExit(Collider collider)
        {
            if (_activeCollisions.TryGetValue(collider, out int count))
            {
                if (count > 1)
                    _activeCollisions[collider] = count - 1;
                else
                {
                    _activeCollisions.Remove(collider);

                    if (isActiveAndEnabled)
                        OnTriggerLastExit(collider);
                }
            }
        }

        protected virtual void OnTriggerLastExit(Collider collider) { }
        
        private List<Collider> _clearList;
        protected virtual void CleanupDeactiveAndDestroyed()
        {
            if (_clearList == null) _clearList = new List<Collider>();
            else _clearList.Clear();

            foreach (Collider collider in _activeCollisions.Keys)
                if (!collider || !collider.enabled) _clearList.Add(collider);

            foreach (Collider collider in _clearList)
                OnTriggerExit(collider);
        }
    }
}
