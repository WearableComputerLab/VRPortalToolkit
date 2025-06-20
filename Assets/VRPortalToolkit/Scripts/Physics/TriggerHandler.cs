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
    /// <summary>
    /// Handles trigger events and manages values associated with colliders.
    /// </summary>
    /// <typeparam name="TValue">The value type associated with colliders.</typeparam>
    public class TriggerHandler<TValue> : IEnumerable<KeyValuePair<Collider, TValue>>
    {
        /// <summary>
        /// Invoked when a value is added.
        /// </summary>
        public event Action<TValue> valueAdded;

        /// <summary>
        /// Invoked when a value is removed.
        /// </summary>
        public event Action<TValue> valueRemoved;

        /// <summary>
        /// Gets all colliders currently tracked.
        /// </summary>
        public IEnumerable<Collider> Colliders => _valueByCollider.Keys;

        /// <summary>
        /// Gets all values currently tracked.
        /// </summary>
        public IEnumerable<TValue> Values => _valueCount.Keys;

        /// <summary>
        /// Gets the number of unique values currently tracked.
        /// </summary>
        public int Count => _nullCount > 0 ? (_valueCount.Count + 1) : _valueCount.Count;

        readonly Dictionary<Collider, TValue> _valueByCollider = new Dictionary<Collider, TValue>();
        readonly Dictionary<TValue, int> _valueCount = new Dictionary<TValue, int>();
        int _nullCount = 0;

        static readonly List<Collider> _exited = new List<Collider>();

        /// <summary>
        /// Adds a collider and its associated value.
        /// </summary>
        /// <param name="collider">The collider to add.</param>
        /// <param name="value">The value associated with the collider.</param>
        public void Add(Collider collider, TValue value)
        {
            RemoveCollider(collider);
            ForceAdd(collider, value);
        }

        /// <summary>
        /// Tries to add a collider and its associated value if the collider is not already tracked.
        /// </summary>
        /// <param name="collider">The collider to add.</param>
        /// <param name="value">The value associated with the collider.</param>
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

        /// <summary>
        /// Removes a collider and its associated value.
        /// </summary>
        /// <param name="collider">The collider to remove.</param>
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

        /// <summary>
        /// Updates the colliders being tracked based on a set of remaining colliders.
        /// </summary>
        /// <param name="remainingColliders">The set of colliders to retain.</param>
        public void UpdateColliders(HashSet<Collider> remainingColliders)
        {
            _exited.Clear();

            foreach (Collider key in _valueByCollider.Keys)
            {
                if (!remainingColliders.Contains(key))
                    _exited.Add(key);
            }

            foreach (var source in _exited)
                RemoveCollider(source);

            _exited.Clear();
        }

        /// <summary>
        /// Checks if a collider is being tracked.
        /// </summary>
        /// <param name="key">The collider to check.</param>
        /// <returns>True if the collider is being tracked, otherwise false.</returns>
        public bool HasCollider(Collider key) => _valueByCollider.ContainsKey(key);

        /// <summary>
        /// Checks if a value is being tracked.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value is being tracked, otherwise false.</returns>
        public bool HasValue(TValue value)
        {
            if (value == null)
                return _nullCount > 0;

            return _valueCount.ContainsKey(value);
        }

        public IEnumerator<KeyValuePair<Collider, TValue>> GetEnumerator() => _valueByCollider.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _valueByCollider.GetEnumerator();
    }

    /// <summary>
    /// Handles trigger events and manages values associated with keys.
    /// </summary>
    /// <typeparam name="TKey">The key type.</typeparam>
    /// <typeparam name="TValue">The value type associated with keys.</typeparam>
    public class TriggerHandler<TKey, TValue> : IEnumerable<KeyValuePair<TKey, TValue>>
    {
        /// <summary>
        /// Invoked when a value is added.
        /// </summary>
        public event Action<TValue> valueAdded;

        /// <summary>
        /// Invoked when a value is removed.
        /// </summary>
        public event Action<TValue> valueRemoved;

        /// <summary>
        /// Gets all keys currently tracked.
        /// </summary>
        public IEnumerable<TKey> Keys => _valueByKey.Keys;

        /// <summary>
        /// Gets all values currently tracked.
        /// </summary>
        public IEnumerable<TValue> Values => _valueCount.Keys;

        /// <summary>
        /// Gets the number of unique values currently tracked.
        /// </summary>
        public int Count => _nullCount > 0 ? (_valueCount.Count + 1) : _valueCount.Count;

        readonly Dictionary<TKey, TValue> _valueByKey = new Dictionary<TKey, TValue>();
        readonly Dictionary<TValue, int> _valueCount = new Dictionary<TValue, int>();
        int _nullCount = 0;

        static readonly List<TKey> _exited = new List<TKey>();

        /// <summary>
        /// Adds a key and its associated value.
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value associated with the key.</param>
        public void Add(TKey key, TValue value)
        {
            RemoveKey(key);
            ForceAdd(key, value);
        }

        /// <summary>
        /// Tries to add a key and its associated value if the key is not already tracked.
        /// </summary>
        /// <param name="key">The key to add.</param>
        /// <param name="value">The value associated with the key.</param>
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

        /// <summary>
        /// Removes a key and its associated value.
        /// </summary>
        /// <param name="key">The key to remove.</param>
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

        /// <summary>
        /// Updates the keys being tracked based on a set of remaining keys.
        /// </summary>
        /// <param name="remainingKeys">The set of keys to retain.</param>
        public void UpdateKeys(HashSet<TKey> remainingKeys)
        {
            foreach (TKey key in _valueByKey.Keys)
            {
                if (!remainingKeys.Contains(key))
                    _exited.Add(key);
            }

            foreach (TKey source in _exited)
                RemoveKey(source);

            _exited.Clear();
        }

        /// <summary>
        /// Checks if a key is being tracked.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if the key is being tracked, otherwise false.</returns>
        public bool HasKey(TKey key) => _valueByKey.ContainsKey(key);

        /// <summary>
        /// Checks if a value is being tracked.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if the value is being tracked, otherwise false.</returns>
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
