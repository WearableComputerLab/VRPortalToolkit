using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using UnityEngine;

namespace VRPortalToolkit
{
    /*public readonly struct PortalColliderPair
    {
        public readonly Portal[] portals;
        public readonly Collider collider;

        public PortalColliderPair(Portal[] portals, Collider collider)
        {
            this.portals = portals;
            this.collider = collider;
        }
    }

    // TODO: This needs to get much smarter
    public class CloneTriggerHandler<TValue>
    {
        public event Action<TValue> firstValueAdded;

        public event Action<TValue, Portal[]> valueAdded;

        public event Action<TValue, Portal[]> valueRemoved;

        public event Action<TValue> lastValueRemoved;

        public IEnumerable<PortalColliderPair> Colliders => _valueByCollider.Keys;

        public IEnumerable<TValue> Values => _valueCount.Keys;

        private readonly struct PortalValuePair
        {
            public readonly Portal[] portals;
            public readonly TValue value;

            public PortalValuePair(Portal[] portals, TValue value)
            {
                this.portals = portals;
                this.value = value;
            }
        }

        readonly Dictionary<PortalColliderPair, TValue> _valueByCollider = new Dictionary<PortalColliderPair, TValue>();
        readonly Dictionary<PortalValuePair, int> _portalValueCount = new Dictionary<PortalValuePair, int>();
        readonly Dictionary<TValue, int> _valueCount = new Dictionary<TValue, int>();

        static readonly List<Collider> _exited = new List<Collider>();

        public void Add(Collider collider, TValue value)
        {
            RemoveCollider(collider);
            ForceAdd(collider, value);
        }

        public void TryAdd(Collider collider, TValue value)
        {
            if (!HasCollider(collider)) ForceAdd(collider, value);
        }

        private void ForceAdd(Collider collider, TValue value)
        {
            _valueByCollider[collider] = value;

            if (_valueCount.TryGetValue(value, out int count))
                _valueCount[value] = count + 1;
            else
            {
                _valueCount[value] = 1;
                valueAdded?.Invoke(value);
            }
        }

        public void RemoveCollider(Collider collider)
        {
            if (_valueByCollider.TryGetValue(collider, out TValue value))
            {
                if (_valueCount.TryGetValue(value, out int count))
                {
                    count--;

                    if (count == 0)
                    {
                        _valueCount.Remove(value);
                        valueRemoved?.Invoke(value);
                    }
                    else
                        _valueCount[value] = count;
                }
            }
        }

        public void UpdateColliders(HashSet<Collider> remainingColliders)
        {
            _exited.Clear();

            foreach (Collider key in _valueByCollider.Keys)
            {
                if (!remainingColliders.Contains(key))
                    _exited.Add(key);
                //else
                //    remainingKeys.Remove(key);
            }

            foreach (var source in _exited)
                RemoveCollider(source);

            _exited.Clear();
        }

        public bool HasCollider(Collider key)
        {
            return _valueByCollider.ContainsKey(key);
        }

        public bool HasValue(TValue value)
        {
            return _valueCount.ContainsKey(value);
        }
    }*/
}