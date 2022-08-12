using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.EditorHelpers
{
    [Serializable]
    public class SerializedDictionary<TKey, TValue> : SerializedList, ICollection<KeyValuePair<TKey, TValue>>, IEnumerable<KeyValuePair<TKey, TValue>>, IEnumerable, IDictionary<TKey, TValue>, IReadOnlyCollection<KeyValuePair<TKey, TValue>>, IReadOnlyDictionary<TKey, TValue>
    {
        public TValue this[TKey key] {
            get {
                Deserialize();
                return dictionary[key];
            }
            set => Add(key, value);
        }

        public ICollection<TKey> Keys {
            get {
                Deserialize();
                return dictionary.Keys;
            }
        }

        public ICollection<TValue> Values {
            get {
                Deserialize();
                return dictionary.Values;
            }
        }

        public override int Count {
            get {
                Deserialize();
                return dictionary.Count;
            }
        }

        public override bool IsReadOnly => false;

        IEnumerable<TKey> IReadOnlyDictionary<TKey, TValue>.Keys {
            get {
                Deserialize();
                return dictionary.Keys;
            }
        }

        IEnumerable<TValue> IReadOnlyDictionary<TKey, TValue>.Values {
            get {
                Deserialize();
                return dictionary.Values;
            }
        }


        [Conversion(nameof(Pair.key), nameof(Pair.value), false)]
        [SerializeField] private List<Pair> list = new List<Pair>();

        private Dictionary<TKey, TValue> dictionary = new Dictionary<TKey, TValue>();

        [Serializable]
        private struct Pair
        {
            public TKey key;
            public TValue value;

            public Pair(TKey key, TValue value)
            {
                this.key = key;
                this.value = value;
            }
        }

        private bool isDirty = true;

        public SerializedDictionary()
        {
            Serialize();
        }

        public SerializedDictionary(IDictionary<TKey, TValue> dictionary)
        {
            this.dictionary = new Dictionary<TKey, TValue>(dictionary);
            Serialize();
        }

        public SerializedDictionary(IEqualityComparer<TKey> comparer)
        {
            dictionary = new Dictionary<TKey, TValue>(comparer);
            Serialize();
        }

        public SerializedDictionary(int capacity)
        {
            dictionary = new Dictionary<TKey, TValue>(capacity);
            Serialize();
        }

        public SerializedDictionary(IDictionary<TKey, TValue> dictionary, IEqualityComparer<TKey> comparer)
        {
            this.dictionary = new Dictionary<TKey, TValue>(dictionary, comparer);
            Serialize();
        }

        public SerializedDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            dictionary = new Dictionary<TKey, TValue>(capacity, comparer);
            Serialize();
        }

        public override void Validate()
        {
            isDirty = true;
        }

        protected virtual void Deserialize()
        {
            if (isDirty)
            {
                dictionary.Clear();

                foreach (Pair pair in list)
                    if (pair.key != null) dictionary.Add(pair.key, pair.value);

                isDirty = false;
            }
        }

        protected virtual void Serialize()
        {
            list.Clear();

            foreach (KeyValuePair<TKey, TValue> pair in dictionary)
                list.Add(new Pair() { key = pair.Key, value = pair.Value });
        }

        public void Add(TKey key, TValue value)
        {
            dictionary.Add(key, value);

            for (int i = list.Count - 1; i >= 0; i++)
            {
                Pair other = list[i];

                if (Equals(other.key, key))
                {
                    list[i] = new Pair(key, value);
                    return;
                }
            }

            list.Add(new Pair(key, value));
        }

        public virtual void Add(KeyValuePair<TKey, TValue> item)
            => Add(item.Key, item.Value);

        protected virtual bool Equals(TKey x, TKey y) => EqualityComparer<TKey>.Default.Equals(x, y);

        protected virtual bool Equals(TValue x, TValue y) => EqualityComparer<TValue>.Default.Equals(x, y);

        public override void Clear()
        {
            dictionary.Clear();
            list.Clear();
        }

        public virtual bool Contains(KeyValuePair<TKey, TValue> item)
        {
            Deserialize();
            return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Contains(item);
        }

        public virtual bool ContainsKey(TKey key)
        {
            Deserialize();
            return dictionary.ContainsKey(key);
        }

        // TODO: this shouldn't use list as list can have doubles
        public virtual void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
        {
            Deserialize();
            ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).CopyTo(array, arrayIndex);
        }

        public virtual IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
        {
            Deserialize();
            return dictionary.GetEnumerator();
        }

        public virtual bool Remove(TKey key)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Pair other = list[i];

                if (Equals(other.key, key))
                {
                    list.RemoveAt(i);
                    i--;
                }
            }

            return dictionary.Remove(key);
        }

        public virtual bool Remove(KeyValuePair<TKey, TValue> item)
        {
            for (int i = 0; i < list.Count; i++)
            {
                Pair other = list[i];

                if (Equals(other.key, item.Key) && Equals(other.value, item.Value))
                {
                    list.RemoveAt(i);
                    i--;
                }
            }

            return ((ICollection<KeyValuePair<TKey, TValue>>)dictionary).Remove(item);
        }

        public virtual bool TryGetValue(TKey key, out TValue value)
        {
            Deserialize();
            return dictionary.TryGetValue(key, out value);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
