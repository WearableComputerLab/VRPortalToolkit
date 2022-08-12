using System.Collections.Generic;

namespace VRPortalToolkit.Utilities
{
    public static class DictionaryUtility
    {
        public static bool TryGetValue<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary, TKey key, out TValue value)
        {
            foreach (var pair in dictionary)
            {
                if (pair.Key.Equals(key))
                {
                    value = pair.Value;
                    return true;
                }
            }

            value = default(TValue);
            return false;
        }

        public static bool TryGetKey<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary, TValue value, out TKey key)
        {
            foreach (var pair in dictionary)
            {
                if (pair.Value.Equals(value))
                {
                    key = pair.Key;
                    return true;
                }
            }

            key = default(TKey);
            return false;
        }

        public static LinkedListNode<KeyValuePair<TKey, TValue>> GetNode<TKey, TValue>(this LinkedList<KeyValuePair<TKey, TValue>> dictionary, TKey key)
        {
            var current = dictionary.First;

            while (current != null && !current.Value.Key.Equals(key))
                current = current.Next;
            
            return current;
        }

        public static bool TryGetNode<TKey, TValue>(this LinkedList<KeyValuePair<TKey, TValue>> dictionary, TKey key, out LinkedListNode<KeyValuePair<TKey, TValue>> node)
        {
            node = dictionary.GetNode(key);
            return node != null;
        }

        public static TValue GetValue<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary, TKey key)
        {
            dictionary.TryGetValue(key, out TValue value);
            return value;
        }

        public static TKey GetKey<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary, TValue value)
        {
            dictionary.TryGetKey(value, out TKey key);
            return key;
        }

        public static IEnumerable<TValue> GetEnumerableValues<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>> dictionary, TKey key)
        {
            foreach (var pair in dictionary)
            {
                if (pair.Key.Equals(key))
                {
                    foreach (var value in pair.Value)
                        yield return value;

                    break;
                }
            }
        }

        public static bool ContainsKey<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary, TKey key)
        {
            foreach (var pair in dictionary)
            {
                if (pair.Key.Equals(key))
                    return true;
            }

            return false;
        }

        public static bool ContainsValue<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary, TValue value)
        {
            foreach (var pair in dictionary)
            {
                if (pair.Value.Equals(value))
                    return true;
            }

            return false;
        }

        public static IEnumerator<TKey> Keys<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary)
        {
            foreach (var pair in dictionary)
                yield return pair.Key;
        }

        public static IEnumerator<TValue> Values<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> dictionary)
        {
            foreach (var pair in dictionary)
                yield return pair.Value;
        }

        public static IEnumerator<TValue> Values<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, IEnumerable<TValue>>> dictionary)
        {
            foreach (var pair in dictionary)
            {
                foreach (var value in pair.Value)
                    yield return value;
            }
        }

        public static void Add<TKey,TValue>(this LinkedList<KeyValuePair<TKey, TValue>> dictionary, TKey key, TValue value)
        {
            if (dictionary.TryGetNode(key, out LinkedListNode<KeyValuePair<TKey, TValue>> node))
                node.Value = new KeyValuePair<TKey, TValue>(key, value);
            else
                dictionary.AddLast(new KeyValuePair<TKey, TValue>(key, value));
        }

        public static bool Remove<TKey,TValue>(this LinkedList<KeyValuePair<TKey, TValue>> dictionary, TKey key)
        {
            if (dictionary.TryGetNode(key, out LinkedListNode<KeyValuePair<TKey, TValue>> node))
            {
                dictionary.Remove(node);
                return true;
            }

            return false;
        }
    }
}