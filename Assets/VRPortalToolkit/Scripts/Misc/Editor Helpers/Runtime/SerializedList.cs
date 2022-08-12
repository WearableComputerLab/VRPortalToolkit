using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc.EditorHelpers
{
    [Serializable] public abstract class SerializedList
    {
        public abstract bool IsReadOnly { get; }

        public abstract int Count { get; }

        public abstract void Clear();

        public abstract void Validate();
    }

    [Serializable] public class PropertyList<T> : SerializedList, ICollection<T>, IEnumerable<T>, IEnumerable, IList<T>, IReadOnlyCollection<T>, IReadOnlyList<T>
    {
        [SerializeField] protected List<T> list = new List<T>();

        public PropertyList() { }

        public PropertyList(int capacity)
        {
            list = new List<T>(capacity);
        }

        public PropertyList(IEnumerable<T> collection) => list = new List<T>(collection);

        T IList<T>.this[int index] { get => list[index]; set => list[index] = value; }

        T IReadOnlyList<T>.this[int index] => list[index];

        public virtual bool IsFixedSize => false;

        public override bool IsReadOnly => false;

        public override int Count => list.Count;

        public virtual void Add(T item) => list.Add(item);

        public override void Clear() => list.Clear();

        public virtual bool Contains(T item) => list.Contains(item);

        public virtual void CopyTo(T[] array, int arrayIndex) => list.CopyTo(array, arrayIndex);

        public virtual IEnumerator<T> GetEnumerator() => list.GetEnumerator();

        public virtual int IndexOf(T item) => IndexOf(item);

        public virtual void Insert(int index, T item) => Insert(index, item);

        public virtual bool Remove(T item) => list.Remove(item);

        public virtual void RemoveAt(int index) => list.RemoveAt(index);

        public override void Validate() { }

        IEnumerator IEnumerable.GetEnumerator() => list.GetEnumerator();
    }
}
