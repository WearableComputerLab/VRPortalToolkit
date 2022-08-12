using Misc;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace VRPortalToolkit
{
    // TODO: There is no way to know if one of our colliders was disabled :(

    public abstract class ComponentTriggerHandler<TComponent> : MonoBehaviour
    {
        private Dictionary<TComponent, Dictionary<Collider, int>> _activeCollisions = new Dictionary<TComponent, Dictionary<Collider, int>>();

        private ObjectPool<Dictionary<Collider, int>> _dictionaryPool = new ObjectPool<Dictionary<Collider, int>>(
            () => new Dictionary<Collider, int>(), null, i => i.Clear(), null, 1);

        protected virtual void OnEnable()
        {
            foreach (var componentPair in _activeCollisions)
            {
                OnComponentFirstEnter(componentPair.Key);

                foreach (Collider collider in componentPair.Value.Keys)
                    OnComponentTriggerFirstEnter(componentPair.Key, collider);
            }
        }

        protected virtual void OnDisable()
        {
            foreach (var componentPair in _activeCollisions)
            {
                foreach (Collider collider in componentPair.Value.Keys)
                    OnComponentTriggerLastExit(componentPair.Key, collider);

                OnComponentLastExit(componentPair.Key);
            }
        }

        protected virtual void OnComponentFirstEnter(TComponent component) { }

        protected virtual void OnComponentTriggerFirstEnter(TComponent component, Collider collider) { }

        protected virtual void OnComponentTriggerLastExit(TComponent component, Collider collider) { }

        protected virtual void OnComponentLastExit(TComponent component) { }

        protected virtual void OnTriggerEnter(Collider collider)
        {
            GameObject source = collider.attachedRigidbody ? collider.attachedRigidbody.gameObject : collider.gameObject;

            if (source.TryGetComponent(out TComponent component))
                AddTriggerComponentCollider(component, collider);
        }

        protected virtual void AddTriggerComponentCollider(TComponent component, Collider collider)
        {
            if (_activeCollisions.TryGetValue(component, out Dictionary<Collider, int> dictionary))
            {
                if (dictionary.TryGetValue(collider, out int count))
                    dictionary[collider] = count + 1;
                else
                {
                    dictionary[collider] = 1;

                    if (isActiveAndEnabled) OnComponentTriggerFirstEnter(component, collider);
                }
            }
            else
            {
                _activeCollisions[component] = dictionary = _dictionaryPool.Get();
                dictionary[collider] = 1;

                if (isActiveAndEnabled)
                {
                    OnComponentFirstEnter(component);
                    OnComponentTriggerFirstEnter(component, collider);
                }
            }

            //if (isActiveAndEnabled)
            //    OnComponentTriggerEnter(component, collider);
        }

        protected virtual void OnTriggerExit(Collider collider)
        {
            GameObject source = collider.attachedRigidbody ? collider.attachedRigidbody.gameObject : collider.gameObject;

            if (source.TryGetComponent(out TComponent component))
                RemoveTriggerComponentCollider(component, collider);
        }

        protected virtual void RemoveTriggerComponentCollider(TComponent component, Collider collider)
        {
            if (_activeCollisions.TryGetValue(component, out Dictionary<Collider, int> dictionary))
            {
                if (dictionary.TryGetValue(collider, out int count))
                {

                    if (count > 1)
                        dictionary[collider] = count - 1;
                    else
                    {
                        dictionary.Remove(collider);

                        if (isActiveAndEnabled) OnComponentTriggerLastExit(component, collider);

                        if (dictionary.Count == 0)
                        {
                            _activeCollisions.Remove(component);
                            _dictionaryPool.Release(dictionary);

                            if (isActiveAndEnabled) OnComponentLastExit(component);
                        }
                    }
                }
            }
        }

        protected int portalTransitionsCount => _activeCollisions.Count;

        protected IEnumerable<TComponent> GetTriggerComponents()
        {
            if (isActiveAndEnabled)
                foreach (TComponent component in _activeCollisions.Keys)
                    yield return component;
        }

        protected virtual bool ComponentIsActiveAndEnabled(TComponent component)
        {
            if (component is Component asComponent)
            {
                if (!asComponent || asComponent.gameObject || !asComponent.gameObject.activeSelf) return false;

                if (component is Behaviour behaviour)
                    return behaviour.enabled;

                if (component is Renderer renderer)
                    return renderer.enabled;

                if (component is Collider collider)
                    return collider.enabled;
            }

            return false;
        }

        protected IEnumerable<Collider> GetColliders(TComponent component)
        {
            if (isActiveAndEnabled && _activeCollisions.TryGetValue(component, out Dictionary<Collider, int> dictionary))
            {
                foreach (Collider collider in dictionary.Keys)
                    yield return collider;
            }
        }

        protected bool HasTriggerComponent(TComponent component) => _activeCollisions.ContainsKey(component);

        protected bool Contains(TComponent portalSpace, Collider collider)
            => _activeCollisions.TryGetValue(portalSpace, out Dictionary<Collider, int> dictionary) && dictionary.ContainsKey(collider);

        protected bool Contains(Collider collider)
        {
            if (!isActiveAndEnabled) return false;

            foreach (Dictionary<Collider, int> dictionary in _activeCollisions.Values)
                if (dictionary.ContainsKey(collider)) return true;

            return false;
        }

        protected int CollisionCount(TComponent component, Collider collider)
        {
            if (isActiveAndEnabled && _activeCollisions.TryGetValue(component, out Dictionary<Collider, int> dictionary))
            {
                if (dictionary.TryGetValue(collider, out int count))
                    return count;
            }

            return 0;
        }

        protected int CollisionCount(Collider collider)
        {
            if (!isActiveAndEnabled) return 0;

            int count = 0, subCount;

            foreach (Dictionary<Collider, int> dictionary in _activeCollisions.Values)
            {
                if (dictionary.TryGetValue(collider, out subCount))
                    count += subCount;
            }

            return count;
        }

        private List<KeyValuePair<TComponent, Collider>> _clearList;
        protected virtual void CleanupDeactiveAndDestroyed()
        {
            if (_clearList == null) _clearList = new List<KeyValuePair<TComponent, Collider>>();
            else _clearList.Clear();

            foreach (var dictionary in _activeCollisions)
            {
                if (ComponentIsActiveAndEnabled(dictionary.Key))
                {
                    foreach (Collider collider in dictionary.Value.Keys)
                        if (!collider || !collider.enabled)
                            _clearList.Add(new KeyValuePair<TComponent, Collider>(dictionary.Key, collider));
                }
                else
                {
                    foreach (Collider collider in dictionary.Value.Keys)
                        _clearList.Add(new KeyValuePair<TComponent, Collider>(dictionary.Key, collider));
                }
            }

            foreach (var pair in _clearList)
                RemoveTriggerComponentCollider(pair.Key, pair.Value);
        }
    }
}
