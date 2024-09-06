using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.XR.Interaction.Toolkit;
using VRPortalToolkit.Utilities;

namespace VRPortalToolkit
{
    public class TriggerHandler<TValue> : IEnumerable<KeyValuePair<Collider, TValue>>
    {
        public event Action<TValue> valueAdded;

        public event Action<TValue> valueRemoved;

        public IEnumerable<Collider> Colliders => _valueByCollider.Keys;

        public IEnumerable<TValue> Values => _valueCount.Keys;

        public int Count => _nullCount > 0 ? (_valueCount.Count + 1) : _valueCount.Count;

        readonly Dictionary<Collider, TValue> _valueByCollider = new Dictionary<Collider, TValue>();
        readonly Dictionary<TValue, int> _valueCount = new Dictionary<TValue, int>();
        int _nullCount = 0;

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

            if (value == null)
            {
                _nullCount++;

                if (_nullCount == 1)
                    valueAdded?.Invoke(value);
            }
            else if (_valueCount.TryGetValue(value, out int count))
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
                if (value == null)
                {
                    _nullCount--;

                    if (_nullCount == 0)
                        valueRemoved?.Invoke(value);
                }
                else if (_valueCount.TryGetValue(value, out int count))
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

                _valueByCollider.Remove(collider);
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

        public bool HasCollider(Collider key) => _valueByCollider.ContainsKey(key);

        public bool HasValue(TValue value)
        {
            if (value == null)
                return _nullCount > 0;

            return _valueCount.ContainsKey(value);
        }

        public IEnumerator<KeyValuePair<Collider, TValue>> GetEnumerator() => _valueByCollider.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _valueByCollider.GetEnumerator();
    }

    public class TriggerHandler<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        public event Action<TValue> valueAdded;

        public event Action<TValue> valueRemoved;

        public IEnumerable<TKey> Keys => _valueByKey.Keys;

        public IEnumerable<TValue> Values => _valueCount.Keys;
        public int Count => _nullCount > 0 ? (_valueCount.Count + 1) : _valueCount.Count;

        readonly Dictionary<TKey, TValue> _valueByKey = new Dictionary<TKey, TValue>();
        readonly Dictionary<TValue, int> _valueCount = new Dictionary<TValue, int>();
        int _nullCount = 0;

        static readonly List<TKey> _exited = new List<TKey>();

        public void Add(TKey key, TValue value)
        {
            RemoveKey(key);
            ForceAdd(key, value);
        }

        public void TryAdd(TKey key, TValue value)
        {
            if (!HasKey(key)) ForceAdd(key, value);
        }

        private void ForceAdd(TKey key, TValue value)
        {
            _valueByKey[key] = value;

            if (value == null)
            {
                _nullCount++;

                if (_nullCount == 1)
                    valueAdded?.Invoke(value);
            }
            else if (_valueCount.TryGetValue(value, out int count))
                _valueCount[value] = count + 1;
            else
            {
                _valueCount[value] = 1;
                valueAdded?.Invoke(value);
            }
        }

        public void RemoveKey(TKey key)
        {
            if (_valueByKey.TryGetValue(key, out TValue value))
            {
                if (value == null)
                {
                    _nullCount--;

                    if (_nullCount == 0)
                        valueRemoved?.Invoke(value);
                }
                else if (_valueCount.TryGetValue(value, out int count))
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

                _valueByKey.Remove(key);
            }
        }

        public void UpdateKeys(HashSet<TKey> remainingKeys)
        {
            foreach (TKey key in _valueByKey.Keys)
            {
                if (!remainingKeys.Contains(key))
                    _exited.Add(key);
                //else
                //    remainingKeys.Remove(key);
            }

            foreach (TKey source in _exited)
                RemoveKey(source);

            _exited.Clear();
        }

        public bool HasKey(TKey key) => _valueByKey.ContainsKey(key);

        public bool HasValue(TValue value)
        {
            if (value == null)
                return _nullCount > 0;

            return _valueCount.ContainsKey(value);
        }

        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _valueByKey.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _valueByKey.GetEnumerator();
    }
}
